using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

using Breeze.Sharp.Core;
using Breeze.Sharp;

using Northwind.Models;

namespace Test_NetClient
{
    [TestClass]
    public class ParallelTests
    {
        // Useful well-known data
        private readonly Guid _alfredsID = Guid.Parse("785efa04-cbf2-4dd7-a7de-083ee17b6ad2");

        private String _serviceName;

        [TestInitialize]
        public void TestInitializeMethod() {
            Configuration.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            _serviceName = "http://localhost:56337/breeze/Northwind/";
        }

        [TestCleanup]
        public void TearDown() {
        }

        #region Parallel queries

        [TestMethod]
        public async Task ParallelQueries() {
            var entityManager = new EntityManager(_serviceName);
            await entityManager.FetchMetadata();
            var idQuery = new EntityQuery<Customer>();
            var entities = await entityManager.ExecuteQuery(idQuery);

            var n = 20;
            var ids = entities.Select(c => c.CustomerID).Take(n).ToArray();

            // In case there aren't n customers available
            var actualCustomers = ids.Count();
            var numExecutions = 0;

            var tasks = ids.Select(id =>
                {
                    ++numExecutions;
                    var query = new EntityQuery<Customer>().Where(c => c.CustomerID == ids[0]);
                    return entityManager.ExecuteQuery(query);

                    // Best practice is to apply ToList() or ToArray() to the task collection immediately
                    // Realizing the collection more than once causes multiple executions of the anon method
                }).ToList();

            var numTasks = tasks.Count();

            // Result of WhenAll is an array of results from the individual anonymous method executions
            // Each individual result is a collection of customers 
            // (only one in this case because of the condition on CustomerId)
            var results = await Task.WhenAll(tasks);
            var numCustomers = results.Sum(customers => customers.Count());

            Assert.AreEqual(actualCustomers, numTasks, "Number of tasks should be " + actualCustomers + ", not " + numTasks);
            Assert.AreEqual(actualCustomers, numExecutions, "Number of excutions should be " + actualCustomers + ", not " + numExecutions);
            Assert.AreEqual(actualCustomers, numCustomers, actualCustomers + " customers should be returned, not " + numCustomers);
        }

        [TestMethod]
        public async Task ParallelQueriesBatched() {
            var entityManager = new EntityManager(_serviceName);
            await entityManager.FetchMetadata();
            var idQuery = new EntityQuery<Customer>();
            var entities = await entityManager.ExecuteQuery(idQuery);

            var n = 20;
            var ids = entities.Select(c => c.CustomerID).Take(n).ToArray();

            // In case there aren't n customers available
            var actualCustomers = ids.Count();
            int allowedParallelQueries = 5;
            int numActiveQueries = 0;
            int maxActiveQueries = 0;

            var semaphore = new SemaphoreSlim(allowedParallelQueries, allowedParallelQueries);
            var tasks = ids.Select(async id =>
            {
                await semaphore.WaitAsync();
                var query = new EntityQuery<Customer>().Where(c => c.CustomerID == ids[0]);

                // Place the release of the semaphore in a finally block to ensure it always gets released
                try {
                    maxActiveQueries = Math.Max(maxActiveQueries, ++numActiveQueries);
                    return await entityManager.ExecuteQuery(query);
                }
                catch (Exception e) {
                    Assert.Fail("ExecuteQuery() threw " + e.GetType().Name + ": " + e.Message);
                    return new Customer[0];
                }
                finally {
                    --numActiveQueries;
                    semaphore.Release();
                }
                // Best practice is to apply ToList() to the task collection immediately
                // Realizing the collection more than once causes multiple executions of the anon method
            }).ToList();

            // Result of WhenAll is an array of results from the individual anonymous method executions
            // Each individual result is a collection of customers 
            // (only one in this case because of the condition on CustomerId)
            var results = await Task.WhenAll(tasks);
            var numQueriedCustomers = results.Sum(customers => customers.Count());

            Assert.IsTrue(maxActiveQueries <= allowedParallelQueries, "Number of active queries should not exceed " + allowedParallelQueries + ". The max was " + maxActiveQueries);
            Assert.AreEqual(actualCustomers, numQueriedCustomers, actualCustomers + " customers should be returned, not " + numQueriedCustomers);
        }

        #endregion Parallel queries

        #region General parallel Operations

        [TestMethod]
        public async Task UncontrolledParallelOperations() {
            var start = DateTime.Now;
            int[] args = { 1000, 2000, 3000, 4000 };
            var tasks = args.Select(a =>
                            {
                                return DoAsyncOperation(a);
                            });
            var results = await Task.WhenAll(tasks.ToArray());

            var elapsed = (DateTime.Now - start).TotalMilliseconds / 1000.0;
            var seconds = Math.Round(elapsed);
            Assert.AreEqual(1, seconds, "All operations in parallel should take approx 1 sec.");

            Assert.AreEqual(args.Length, results.Count(), "There should be " + args.Length + " results");
            Assert.IsTrue(results.All(s => s.Contains("Success")), "All results should contain 'Success'");
        }

        [TestMethod]
        public async Task TwoParallelOperations() {
            var start = DateTime.Now;
            int[] args = { 1000, 2000, 3000, 4000 };
            int n = 2;
            var semaphore = new SemaphoreSlim(n, n);
            var tasks = args.Select(async a =>
            {
                await semaphore.WaitAsync();
                var result = await DoAsyncOperation(a);
                semaphore.Release();
                return result;
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            var elapsed = (DateTime.Now - start).TotalMilliseconds / 1000.0;
            var seconds = Math.Round(elapsed);
            Assert.AreEqual(2, seconds, "Two operations in parallel should take approx 2 sec.");

            Assert.AreEqual(args.Length, results.Count(), "There should be " + args.Length + " results");
            Assert.IsTrue(results.All(s => s.Contains("Success")), "All results should contain 'Success'");
        }

        [TestMethod]
        public async Task ParallelOperationsWithException() {
            var start = DateTime.Now;
            int[] args = { 1000, 2000, 3000, 4000 };
            int n = 2;
            var semaphore = new SemaphoreSlim(n, n);
            var tasks = args.Select(async a =>
            {
                await semaphore.WaitAsync();
                try {
                    var result = await DoAsyncOperation(a, true);
                    return result;
                }
                catch (Exception e) {
                    return e.Message;
                }
                finally {
                    semaphore.Release();
                }
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            var elapsed = (DateTime.Now - start).TotalMilliseconds / 1000.0;
            var seconds = Math.Round(elapsed);
            Assert.AreEqual(2, seconds, "Two operations in parallel should take approx 2 sec.");

            Assert.AreEqual(args.Length, results.Count(), "There should be " + args.Length + " results");
            Assert.AreEqual(3, results.Where(s => s.Contains("Success")).Count(), "Three results should contain 'Success'");
            Assert.AreEqual(1, results.Where(s => s.Contains("Exception")).Count(), "One result should contain 'Exception'");
        }

        [TestMethod]
        public async Task InParallel() {
            var start = DateTime.Now;

            int[] args = { 1000, 2000, 3000, 4000 };
            var results = await args.InParallel(a => DoAsyncOperation(a));

            var elapsed = (DateTime.Now - start).TotalMilliseconds / 1000.0;
            var seconds = Math.Round(elapsed);
            Assert.AreEqual(1, seconds, "All operations in parallel should take approx 1 sec.");

            Assert.AreEqual(args.Length, results.Count(), "There should be " + args.Length + " results");
            Assert.IsTrue(results.All(s => s.Contains("Success")), "All results should contain 'Success'");
        }

        [TestMethod]
        public async Task TwoInParallel() {
            var start = DateTime.Now;

            int[] args = { 1000, 2000, 3000, 4000 };
            var n = 2;
            var results = await args.InParallel(a => DoAsyncOperation(a), n);

            var elapsed = (DateTime.Now - start).TotalMilliseconds / 1000.0;
            var seconds = Math.Round(elapsed);
            Assert.AreEqual(2, seconds, "All operations in parallel should take approx 2 sec.");

            Assert.AreEqual(args.Length, results.Count(), "There should be " + args.Length + " results");
            Assert.IsTrue(results.All(s => s.Contains("Success")), "All results should contain 'Success'");
        }

        [TestMethod]
        public async Task OneAtATime() {
            var start = DateTime.Now;

            int[] args = { 1000, 2000, 3000, 4000 };
            var results = await args.OneAtATime(a => DoAsyncOperation(a));

            var elapsed = (DateTime.Now - start).TotalMilliseconds / 1000.0;
            var seconds = Math.Round(elapsed);
            Assert.AreEqual(4, seconds, "4 operations one at a time should take approx 4 sec.");

            Assert.AreEqual(args.Length, results.Count(), "There should be " + args.Length + " results");
            Assert.IsTrue(results.All(s => s.Contains("Success")), "All results should contain 'Success'");
        }


        private async Task<string> DoAsyncOperation(int arg, bool throwIf3000 = false) {
            
            // Just delay for a second, then return
            await Task.Delay(1000);
            if (throwIf3000 && arg == 3000) {
                throw new ArgumentException("Exception because value is 3000");
            }
            return "Success: key = " + arg; 
        }

        #endregion General parallel Operations
    }
}


