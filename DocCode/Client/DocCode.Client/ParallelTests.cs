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

namespace Test_NetClient {
  [TestClass]
  public class ParallelTests {
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
        var ids             = entities.Select(c => c.CustomerID).Take(n).ToArray();

        // In case there aren't n customers available
        var actualCustomers = ids.Count();
        var numExecutions   = 0;

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
        var actualCustomers         = ids.Count();
        int allowedParallelQueries  = 5;
        int numActiveQueries        = 0;
        int maxActiveQueries        = 0;

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
  }
}


