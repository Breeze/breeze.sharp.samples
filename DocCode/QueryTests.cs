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
    public class QueryTests
    {
        // Useful well-known data
        private readonly Guid _alfredsID = Guid.Parse("785efa04-cbf2-4dd7-a7de-083ee17b6ad2");

        private String _serviceName;

        [TestInitialize]
        public void TestInitializeMethod() {
            MetadataStore.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            _serviceName = "http://localhost:56337/breeze/Northwind/";
        }

        [TestCleanup]
        public void TearDown() {
        }

        [TestMethod]
        public async Task RequerySameEntity() {
          var entityManager = await TestFns.NewEm(_serviceName);

          // Orders with freight cost over 100.
          var query = new EntityQuery<Order>().Where(o => o.Freight > 100);
          var orders100 = await entityManager.ExecuteQuery(query);
          Assert.IsTrue(orders100.Any(), "There should be orders with freight cost > 100");

          var query2 = new EntityQuery<Order>().Where(o => o.Freight > 50);
          var orders50 = await entityManager.ExecuteQuery(query2);
          Assert.IsTrue(orders50.Any(), "There should be orders with freight cost > 50");

          Assert.IsTrue(orders50.Count() >= orders100.Count(), "There should be more orders with freight > 50 than 100");
        }


        //*********************************************************
        // Metadata necessary to get entity key
        // Must be first test, since CanFetchMetadata() below fetches 
        // metadata into static instance of MetadataStore
        //
        //*********************************************************
        [TestMethod]
        public async Task MetadataNeededToGetEntityKey() {
            var entityManager = new EntityManager(_serviceName);
            var customerType = MetadataStore.Instance.GetEntityType(typeof(Customer));
            Assert.IsNotNull(customerType);

            try {
                var key = new EntityKey(customerType, _alfredsID);
                Assert.Fail("EntityKey constructor should fail if metadata not fetched");
            }
            catch (Exception e) {
                Assert.IsTrue(e is ArgumentOutOfRangeException, "Thrown exception should be ArgumentOutOfRangeException.  Type = " + e.GetType().Name + ": " + e.Message);
                Assert.Fail("Missing metadata should throw with more informative message");
            }
        }

        //*********************************************************
        // Confirm the metadata can be fetched from the server
        //
        //*********************************************************
        [TestMethod]
        public async Task CanGetMetadata() {
            var entityManager = new EntityManager(_serviceName);
            var dataService = await entityManager.FetchMetadata();
            Assert.IsNotNull(dataService);
        }

        //*********************************************************
        // Fetch same entity twice
        //
        //*********************************************************
        [TestMethod]
        public async Task CanFetchEntityTwice() {
            var entityManager = new EntityManager(_serviceName);
            await entityManager.FetchMetadata();
            var customerType = MetadataStore.Instance.GetEntityType(typeof(Customer));

            var key = new EntityKey(customerType, _alfredsID);

            var result = await entityManager.FetchEntityByKey(key);
            Assert.IsNotNull(result.Entity, "Entity fetched by key should not be null");
            Assert.IsFalse(result.FromCache, "Entity fetched remotely should not be from cache");

            result = await entityManager.FetchEntityByKey(key);
            Assert.IsNotNull(result.Entity, "Entity re-fetched by key should not be null");
            Assert.IsFalse(result.FromCache, "Entity re-fetched remotely should not be from cache");
        }

        //*********************************************************
        // Query entity twice
        //
        //*********************************************************
        [TestMethod]
        public async Task QueryEntityTwice() {
            var entityManager = new EntityManager(_serviceName);
            await entityManager.FetchMetadata();

            var query1 = new EntityQuery<Customer>().Where(c => c.CustomerID == _alfredsID);
            var alfred1 = (await entityManager.ExecuteQuery(query1)).FirstOrDefault();
            Assert.IsNotNull(alfred1, "Alfred should be found by Id");

            var query2 = new EntityQuery<Customer>().Where(c => c.CompanyName == alfred1.CompanyName);
            var alfred2 = (await entityManager.ExecuteQuery(query2)).FirstOrDefault();
            Assert.IsTrue(ReferenceEquals(alfred1, alfred2), "Successive queried should return same entity");
        }

        //*********************************************************
        // Query all instances of an entity (Customer)
        // Execute the query via a test helper method that encapsulates the ceremony
        //
        //*********************************************************

        [TestMethod]
        public async Task AllCustomers_Concise()
        {
            var query = EntityQuery.From<Customer>();                       // One way to create a basic EntityQuery
            await TestFns.VerifyQuery(query, _serviceName, "All customers");
        }

        //*********************************************************
        // Query all instances of an entity (Customer)
        // Handle async Task results explicitly
        //
        //*********************************************************
        
        [TestMethod]
        public async Task AllCustomers_Task()
        {
            var query = new EntityQuery<Customer>();                        // Alternate way to create a basic EntityQuery
            var entityManager = new EntityManager(_serviceName);
            await entityManager.ExecuteQuery(query).ContinueWith(task =>
                {
                    if (task.IsFaulted) {
                        var message = TestFns.FormatException(task.Exception);
                        Assert.Fail(message);
                    }
                    else {
                        var count = task.Result.Count();
                        Assert.IsTrue(count > 0, "Customer query returned " + count + " customers");
                    }
                });
        }

        //*********************************************************
        // Query all instances of an entity (Customer)
        // Capture result using try-catch
        //
        //*********************************************************
        
        [TestMethod]
        public async Task AllCustomers_Exceptions() {
            var query = new EntityQuery<Customer>(); 
            var entityManager = new EntityManager(_serviceName);
            try {
                var results = await entityManager.ExecuteQuery(query);
                var count = results.Count();
                Assert.IsTrue(count > 0, "Customer query returned " + count + " customers");
            } catch (Exception e) {
                var message = TestFns.FormatException(e);
                Assert.Fail(message);
            }
        }

        //*********************************************************
        // Query an entity (Supplier) which holds a complex type (Location)
        //
        //*********************************************************

        [TestMethod]
        public async Task ComplexType() {
            try {
                var entityManager = await TestFns.NewEm(_serviceName);
                var data = await EntityQuery.From<Supplier>().Take(1).Execute(entityManager);
                var supplier = data.FirstOrDefault();
                Assert.IsNotNull(supplier, "Supplier should be non-null");
                Assert.IsNotNull(supplier.Location, "Supplier location should be non-null");
                Assert.IsFalse(string.IsNullOrEmpty(supplier.Location.Address), "Supplier address should be non-null and not empty");
            }
            catch (Exception e) {
                var message = TestFns.FormatException(e);
                Assert.Fail(message);
            }
        }

        //*********************************************************
        // Requerying same entity
        //
        //*********************************************************
        [TestMethod]
        public async Task RequerySameEntity() {
            var entityManager = await TestFns.NewEm(_serviceName);

            // Orders with freight cost over 100.
            var query = new EntityQuery<Order>().Where(o => o.Freight > 100);
            var orders100 = await entityManager.ExecuteQuery(query);
            Assert.IsTrue(orders100.Any(), "There should be orders with freight cost > 100");

            var query2 = new EntityQuery<Order>().Where(o => o.Freight > 50);
            var orders50 = await entityManager.ExecuteQuery(query2);
            Assert.IsTrue(orders50.Any(), "There should be orders with freight cost > 50");

            Assert.IsTrue(orders50.Count() >= orders100.Count(), "There should be more orders with freight > 50 than 100");
        }


        //*********************************************************
        // Queries involving single conditions
        //
        //*********************************************************
        [TestMethod]
        public async Task SingleConditions() {
            try {
                var entityManager = await TestFns.NewEm(_serviceName);

                //  Customers starting w/ 'A' (string comparison)
                var query1 = new EntityQuery<Customer>().Where(c => c.CompanyName.StartsWith("A"))
                                                        .OrderBy(c => c.CompanyName);
                var customers = await entityManager.ExecuteQuery(query1);
                Assert.IsTrue(customers.Any(), "There should be customers whose name begins with A");

                // Orders with freight cost over 100.
                var query2 = new EntityQuery<Order>().Where(o => o.Freight > 100);
                var orders = await entityManager.ExecuteQuery(query2);
                Assert.IsTrue(orders.Any(), "There should be orders with freight cost > 100");

                // Temporarily use a new entity manager
                entityManager = new EntityManager(_serviceName);

                // Orders placed on or after 1/1/1998.
                var testDate = new DateTime(1998, 1, 3);
                var query3 = new EntityQuery<Order>().Where(o => o.OrderDate >= testDate);
                orders = await entityManager.ExecuteQuery(query3);
                Assert.IsTrue(orders.Any(), "There should be orders placed after 1/1/1998");

                // Temporarily use a new entity manager
                entityManager = new EntityManager(_serviceName);

                // Orders placed on 1/1/1998.
                var query4 = new EntityQuery<Order>().Where(o => o.OrderDate == testDate);
                orders = await entityManager.ExecuteQuery(query4);
                Assert.IsTrue(!orders.Any(), "There should no orders placed on 1/2/1998.  There are " + orders.Count());
            }
            catch (Exception e) {
                var message = TestFns.FormatException(e);
                Assert.Fail(message);
            }
        }

        //*********************************************************
        // Queries involving expansion to related entities
        //
        //*********************************************************

        [TestMethod]
        public async Task Expansions() {

            // Alfreds orders expanded with their OrderDetails
            try {
                var entityManager = await TestFns.NewEm(_serviceName);

                var query = new EntityQuery<Order>().Where(o => o.CustomerID == _alfredsID).Expand("OrderDetails");
                var orders = await entityManager.ExecuteQuery(query);
                AssertGotOrderDetails(entityManager, orders);
            } 
            catch (Exception e) {
                var message = TestFns.FormatException(e);
                Assert.Fail(message);
            }

            // Alfreds orders expanded with their parent Customers and OrderDetails 
            try {
                var entityManager = await TestFns.NewEm(_serviceName);

                var query = new EntityQuery<Order>().Where(o => o.CustomerID == _alfredsID).Expand("Customer").Expand("OrderDetails");
                var orders = await entityManager.ExecuteQuery(query);
                AssertGotOrderDetails(entityManager, orders);
                AssertGotCustomerByExpand(entityManager, orders);
            } 
            catch (Exception e) {
                var message = TestFns.FormatException(e);
                Assert.Fail(message);
            }

            // Alfreds orders, including their OrderDetails, and the Products of those details, 
            try {
                var entityManager = await TestFns.NewEm(_serviceName);

                var query = new EntityQuery<Order>().Where(o => o.CustomerID == _alfredsID).Expand("Customer").Expand("OrderDetails.Product");
                var orders = await entityManager.ExecuteQuery(query);
                AssertGotOrderDetails(entityManager, orders);
                AssertGotCustomerByExpand(entityManager, orders);
                AssertGotProductsByExpand(entityManager, orders);
            } 
            catch (Exception e) {
                var message = TestFns.FormatException(e);
                Assert.Fail(message);
            }

            // Products with related Supplier entity with complex type, 
            try {
                var entityManager = await TestFns.NewEm(_serviceName);

                var query = new EntityQuery<Product>().Take(1).Expand("Supplier");
                var products = await entityManager.ExecuteQuery(query);
                var product = products.FirstOrDefault();
                Assert.IsNotNull(product, "A product should be returned");
                Assert.IsNotNull(product.Supplier, "A product should have a supplier");
            }
            catch (Exception e) {
                var message = TestFns.FormatException(e);
                Assert.Fail(message);
            }
        }

        private void AssertGotOrderDetails(EntityManager entityManager, IEnumerable<Order> orders)
        {
            var odType = MetadataStore.Instance.GetEntityType("OrderDetail");

            // Check that there are order details in cache
            var odsInCache = entityManager.GetEntities<OrderDetail>();
            Assert.IsTrue(odsInCache.Any(), "Should have OrderDetails in cache; got " + odsInCache.Count());

            // Check the first order was fixed up properly
            var firstOrder = orders.FirstOrDefault();
            Assert.IsNotNull(firstOrder, "There should be at least one order returned");
            Assert.IsTrue(firstOrder.OrderDetails.Any(), "First order should have order details");

            // To manually confirm these results, run this SQL:
            // select count(*) from OrderDetail where OrderID in 
            //   (select OrderID from [Order] where CustomerID = '785efa04-cbf2-4dd7-a7de-083ee17b6ad2')
        }

        private void AssertGotCustomerByExpand(EntityManager entityManager, IEnumerable<Order> orders)
        {
            var firstOrder = orders.FirstOrDefault();
            Assert.IsNotNull(firstOrder, "There should be at least one order returned");
            Assert.IsNotNull(firstOrder.Customer, "Related customer should be returned");
        }

        private void AssertGotProductsByExpand(EntityManager entityManager, IEnumerable<Order> orders)
        {
            var firstOrder = orders.FirstOrDefault();
            Assert.IsNotNull(firstOrder, "There should be at least one order returned");
            var firstOrderDetail = firstOrder.OrderDetails.FirstOrDefault();
            Assert.IsNotNull(firstOrderDetail, "There should be at least one order detail returned");
            Assert.IsNotNull(firstOrderDetail.Product, "Related product should be returned");
        }

        //*********************************************************
        // Queries using specialized server controller methods
        //
        //*********************************************************

        [TestMethod]
        public async Task SpecializedMethods() {
            try {
                var entityManager = await TestFns.NewEm(_serviceName);

                // CustomersAsHRM returns an HTTPResponseMessage
                // can filter, select, and expand 
                var query = EntityQuery.From<Customer>("CustomersAsHRM")
                                       .Where(c => c.CustomerID == _alfredsID)
                                       .Select(c => new {
                                           CustomerID = c.CustomerID,
                                           CompanyName = c.CompanyName,
                                       });
                var items = await entityManager.ExecuteQuery(query);
                Assert.IsTrue(items.Count() == 1, "Should return one customer projection item");
                var item = items.FirstOrDefault();

            }
            catch (Exception e) {
                var message = TestFns.FormatException(e);
                Assert.Fail(message);
            }
        }


//=======================================================================================================================================

        //*********************************************************
        // Original contents of Breeze.sharp.internal.tests QueryTests.cs
        //
        //*********************************************************

        [TestMethod]
        public async Task SimpleQuery() {
            var em1     = await TestFns.NewEm(_serviceName);
            var q       = new EntityQuery<Customer>();
            var results = await em1.ExecuteQuery(q);
            Assert.IsTrue(results.Cast<Object>().Count() > 0);
        }

        [TestMethod]
        public async Task SimpleEntitySelect() {
            Assert.Inconclusive("Known issue with OData - use an anon projection instead");

            var em1 = await TestFns.NewEm(_serviceName);

            var q1 = new EntityQuery<Order>().Where(o => true).Select(o => o.Customer).Take(5);
            var r1 = await q1.Execute(em1);
            Assert.IsTrue(r1.Count() == 5);
            var ok = r1.All(r => r.GetType() == typeof(Customer));
            Assert.IsTrue(ok);


        }

        [TestMethod]
        public async Task SimpleAnonEntitySelect() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q1 = new EntityQuery<Order>().Select(o => new { o.Customer }).Take(5);
            var r1 = await q1.Execute(em1);
            Assert.IsTrue(r1.Count() == 5);
            var ok = r1.All(r => r.Customer.GetType() == typeof(Customer));
            Assert.IsTrue(ok);


        }

        [TestMethod]
        public async Task SimpleAnonEntityCollectionSelect() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q1 = new EntityQuery<Customer>().Where(c => c.CompanyName.StartsWith("C")).Select(c => new { c.Orders });
            var r1 = await q1.Execute(em1);
            Assert.IsTrue(r1.Count() > 0);
            var ok = r1.All(r => r.Orders.Count() > 0);
            Assert.IsTrue(ok);


        }


        [TestMethod]
        public async Task NonGenericQuery() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C")).Take(3);
            var q3 = (EntityQuery)q2;

            var results = await em1.ExecuteQuery(q3);

            Assert.IsTrue(results.Cast<Object>().Count() == 3);

        }

        [TestMethod]
        public async Task InlineCount() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C"));
            var q3 = q2.InlineCount();

            var results = await q3.Execute(em1);

            var count = ((IHasInlineCount)results).InlineCount;

            Assert.IsTrue(results.Count() > 0);
            Assert.IsTrue(results.Count() == count, "counts should be the same");
            Assert.IsTrue(results.All(r1 => r1.GetType() == typeof(Customer)), "should all get customers");
        }

        [TestMethod]
        public async Task InlineCount2() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C")).Take(2);
            var q3 = q2.InlineCount();

            var results = await q3.Execute(em1);

            var count = ((IHasInlineCount)results).InlineCount;

            Assert.IsTrue(results.Count() == 2);
            Assert.IsTrue(results.Count() < count, "counts should be the same");
            Assert.IsTrue(results.All(r1 => r1.GetType() == typeof(Customer)), "should all get customers");
        }

        [TestMethod]
        public async Task WhereAnyOrderBy() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>();
            var q2 = q.Where(c => c.CompanyName.StartsWith("C") && c.Orders.Any(o => o.Freight > 10));
            var q3 = q2.OrderBy(c => c.CompanyName).Skip(2);

            var results = await q3.Execute(em1);

            Assert.IsTrue(results.Count() > 0);
            Assert.IsTrue(results.All(r1 => r1.GetType() == typeof(Customer)), "should all get customers");
            var cust = results.First();
            var companyName = cust.CompanyName;
            var custId = cust.CustomerID;
            var orders = cust.Orders;
        }

        [TestMethod]
        public async Task WithOverwriteChanges() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C"));
            var q3 = q2.OrderBy(c => c.CompanyName).Take(2);
            var results = await q3.Execute(em1);

            Assert.IsTrue(results.Count() == 2);
            results.ForEach(r =>
            {
                r.City = "xxx";
                r.CompanyName = "xxx";
            });
            var results2 = await q3.With(MergeStrategy.OverwriteChanges).Execute(em1);
            // contents of results2 should be exactly the same as results
            Assert.IsTrue(results.Count() == 2);

        }

        [TestMethod]
        public async Task WithEntityManager() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C"));
            var q3 = q2.OrderBy(c => c.CompanyName).Take(2);
            var results = await q3.With(em1).Execute();

            Assert.IsTrue(results.Count() == 2);

        }

        [TestMethod]
        public async Task WhereOrderByTake() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C"));
            var q3 = q2.OrderBy(c => c.CompanyName).Take(2);
            var results = await q3.Execute(em1);

            Assert.IsTrue(results.Count() == 2);
            Assert.IsTrue(results.All(r1 => r1.GetType() == typeof(Customer)), "should all get customers");
        }

        [TestMethod]
        public async Task SelectAnonWithEntityCollection() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C"));
            var q3 = q2.Select(c => new { Orders = c.Orders });
            var results = await q3.Execute(em1);

            Assert.IsTrue(results.Count() > 0);
            var ok = results.All(r1 => (r1.Orders.Any()) );
            Assert.IsTrue(ok, "every item of anon should contain a collection of Orders");
        }

        [TestMethod]
        public async Task SelectAnonWithScalarAndEntityCollection() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C"));
            var q3 = q2.Select(c => new { c.CompanyName, c.Orders });
            var results = await q3.Execute(em1);

            Assert.IsTrue(results.Count() > 0);
            var ok = results.All(r1 => (r1.Orders.Count() > 0));
            Assert.IsTrue(ok, "every item of anon should contain a collection of Orders");
            ok = results.All(r1 => r1.CompanyName.Length > 0);
            Assert.IsTrue(ok, "anon type should have a populated company name");

        }

        [TestMethod]
        public async Task SelectAnonWithScalarEntity() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Order>("Orders");
            var q2 = q.Where(c => c.Freight > 500);
            var q3 = q2.Select(c => new { c.Customer, c.Freight });
            var results = await q3.Execute(em1);

            Assert.IsTrue(results.Count() > 0);
            var ok = results.All(r1 => r1.Freight > 500);
            Assert.IsTrue(ok, "anon type should the right freight");
            ok = results.All(r1 => r1.Customer.GetType() == typeof(Customer));
            Assert.IsTrue(ok, "anon type should have a populated 'Customer'");
        }

        [TestMethod]
        public async Task SelectAnonWithScalarSelf() {
            Assert.Inconclusive("OData doesn't support this kind of query (I think)");
            return;

            // Pretty sure this is an issue with OData not supporting this syntax.
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C"));
            var q3 = q2.Select(c => new { c.CompanyName, c });
            var results = await q3.Execute(em1);

            Assert.IsTrue(results.Count() > 0);
            var ok = results.All(r1 => r1.CompanyName.Length > 0);
            Assert.IsTrue(ok, "anon type should have a populated company name");
            ok = results.All(r1 => r1.c.GetType() == typeof(Customer));
            Assert.IsTrue(ok, "anon type should have a populated 'Customer'");
        }

        [TestMethod]
        public async Task ExpandNonScalar() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C"));
            var q3 = q2.Expand(c => c.Orders);
            var results = await q3.Execute(em1);

            Assert.IsTrue(results.Count() > 0);
            var ok = results.All(r1 =>
              r1.GetType() == typeof(Customer) &&
              r1.Orders.Any() &&
              r1.Orders.All(o => o.Customer == r1));
            Assert.IsTrue(ok, "every Customer should contain a collection of Orders");
            ok = results.All(r1 => r1.CompanyName.Length > 0);
            Assert.IsTrue(ok, "and should have a populated company name");
        }

        [TestMethod]
        public async Task ExpandScalar() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Order>("Orders");
            var q2 = q.Where(o => o.Freight > 500);
            var q3 = q2.Expand(o => o.Customer);
            var results = await q3.Execute(em1);

            Assert.IsTrue(results.Count() > 0);
            var ok = results.All(r1 =>
              r1.Customer.GetType() == typeof(Customer));
            Assert.IsTrue(ok, "every Order should have a customer");
            ok = results.All(r1 => r1.Freight > 500);
            Assert.IsTrue(ok, "and should have the right freight");
        }

        [TestMethod]
        public async Task SelectIntoCustom() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>("Customers");
            var q2 = q.Where(c => c.CompanyName.StartsWith("C"));
            var q3 = q2.Select(c => new Dummy() { CompanyName = c.CompanyName, Orders = c.Orders });
            var results = await q3.Execute(em1);

            Assert.IsTrue(results.Count() > 0);
          var ok = results.All(r1 =>
            r1.GetType() == typeof (Dummy) && r1.Orders.Any());
            
            Assert.IsTrue(ok, "every Dummy should contain a collection of Orders");
            ok = results.All(r1 => r1.CompanyName.Length > 0);
            Assert.IsTrue(ok, "and should have a populated company name");
        }

        public class Dummy
        {
            public String CompanyName;
            public IEnumerable<Order> Orders;
        }

        [TestMethod]
        public async Task GuidQuery() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>().Where(c => c.CustomerID.Equals(Guid.NewGuid())); // && true);
            var rp = q.GetResourcePath();
            var r = await em1.ExecuteQuery(q);
            Assert.IsTrue(!r.Any(), "should be no results");

        }

        [TestMethod]
        public async Task GuidQuery2() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Order>().Where(o => o.CustomerID == Guid.NewGuid()); // && true);
            var rp = q.GetResourcePath();
            var r = await em1.ExecuteQuery(q);
            Assert.IsTrue(!r.Any(), "should be no results");

        }

        [TestMethod]
        public async Task EntityKeyQuery() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>().Take(1);

            var r = await em1.ExecuteQuery(q);
            var customer = r.First();
            var q1 = new EntityQuery<Customer>().Where(c => c.CustomerID == customer.CustomerID);
            var r1 = await em1.ExecuteQuery(q1);
            Assert.IsTrue(r1.First() == customer);
            var ek = customer.EntityAspect.EntityKey;
            var q2 = ek.ToQuery();
            var r2 = await em1.ExecuteQuery(q2);
            Assert.IsTrue(r2.Cast<Customer>().First() == customer);
        }

        [TestMethod]
        public async Task QuerySameFieldTwice() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q0 = EntityQuery.From<Order>().Where(o => o.Freight > 100 && o.Freight < 200);
            var r0 = await q0.Execute(em1);
            Assert.IsTrue(r0.Any());
            Assert.IsTrue(r0.All(r => r.Freight > 100 && r.Freight < 200), "should match query criteria");
        }

        [TestMethod]
        public async Task QueryWithYearFn() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q0 = new EntityQuery<Employee>().Where(e => e.HireDate.Value.Year > 1993);
            var r0 = await q0.Execute(em1);
            Assert.IsTrue(r0.Count() > 0);
            Assert.IsTrue(r0.All(r => r.HireDate.Value.Year > 1993));
            var r1 = q0.ExecuteLocally(em1);
            Assert.IsTrue(r1.Count() == r0.Count());
        }

        [TestMethod]
        public async Task QueryWithMonthFn() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q0 = new EntityQuery<Employee>().Where(e => e.HireDate.Value.Month > 6 && e.HireDate.Value.Month < 11);
            var r0 = await q0.Execute(em1);
            Assert.IsTrue(r0.Count() > 0);
            Assert.IsTrue(r0.All(e => e.HireDate.Value.Month > 6 && e.HireDate.Value.Month < 11));
            var r1 = q0.ExecuteLocally(em1);
            Assert.IsTrue(r1.Count() == r0.Count());
        }

        [TestMethod]
        public async Task QueryWithAddFn() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q0 = new EntityQuery<Employee>().Where(e => e.EmployeeID + e.ReportsToEmployeeID.Value > 3);
            var r0 = await q0.Execute(em1);
            Assert.IsTrue(r0.Count() > 0);
            Assert.IsTrue(r0.All(e => e.EmployeeID + e.ReportsToEmployeeID > 3));
            var r1 = q0.ExecuteLocally(em1);
            Assert.IsTrue(r1.Count() == r0.Count());
        }

        [TestMethod]
        public async Task QueryWithBadResourceName() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q0 = new EntityQuery<Customer>("Error").Where(c => c.CompanyName.StartsWith("P"));
            try {
                var r0 = await q0.Execute(em1);
                Assert.Fail("shouldn't get here");
            }
            catch (Exception e) {
                Assert.IsTrue(e.Message.Contains("found"), "should be the right message");
            }

        }

        [TestMethod]
        public async Task Take0() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q0 = new EntityQuery<Customer>().Take(0);

            var r0 = await q0.Execute(em1);
            Assert.IsTrue(r0.Count() == 0);

        }

        [TestMethod]
        public async Task Take0WithInlineCount() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q0 = new EntityQuery<Customer>().Take(0).InlineCount();

            var r0 = await q0.Execute(em1);
            Assert.IsTrue(r0.Count() == 0);
            var count = ((IHasInlineCount)r0).InlineCount;
            Assert.IsTrue(count > 0);
        }

        [TestMethod]
        public async Task NestedExpand() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q0 = new EntityQuery<OrderDetail>().Take(5).Expand(od => od.Order.Customer);

            var r0 = await q0.Execute(em1);
            Assert.IsTrue(r0.Count() > 0, "should have returned some orderDetails");
            Assert.IsTrue(r0.All(od => od.Order != null && od.Order.Customer != null));

        }

        [TestMethod]
        public async Task NestedExpand3Levels() {
            var em1 = await TestFns.NewEm(_serviceName);

            var q0 = new EntityQuery<Order>().Take(5).Expand("OrderDetails.Product.Category");

            var r0 = await q0.Execute(em1);
            Assert.IsTrue(r0.Count() > 0, "should have returned some orders");
            Assert.IsTrue(r0.All(o => o.OrderDetails.Any(od => od.Product.Category != null)));

        }

        //test("query with take, orderby and expand", function () {
        //    if (testFns.DEBUG_MONGO) {
        //        ok(true, "NA for Mongo - expand not yet supported");
        //        return;
        //    }
        //    var em = newEm();
        //    var q1 = EntityQuery.from("Products")
        //        .expand("category")
        //        .orderBy("category.categoryName desc, productName");
        //    stop();
        //    var topTen;
        //    em.executeQuery(q1).then(function (data) {
        //        topTen = data.results.slice(0, 10);
        //        var q2 = q1.take(10);
        //        return em.executeQuery(q2);
        //    }).then(function (data2) {
        //        var topTenAgain = data2.results;
        //        for(var i=0; i<10; i++) {
        //            ok(topTen[i] === topTenAgain[i]);
        //        }
        //    }).fail(testFns.handleFail).fin(start);

        //});


        //test("query with take, skip, orderby and expand", function () {
        //    if (testFns.DEBUG_MONGO) {
        //        ok(true, "NA for Mongo - expand not yet supported");
        //        return;
        //    }

        //    var em = newEm();
        //    var q1 = EntityQuery.from("Products")
        //        .expand("category")
        //        .orderBy("category.categoryName, productName");
        //    stop();
        //    var nextTen;
        //    em.executeQuery(q1).then(function (data) {
        //        nextTen = data.results.slice(10, 20);
        //        var q2 = q1.skip(10).take(10);
        //        return em.executeQuery(q2);
        //    }).then(function (data2) {
        //        var nextTenAgain = data2.results;
        //        for (var i = 0; i < 10; i++) {
        //            ok(nextTen[i] === nextTenAgain[i], extractDescr(nextTen[i]) + " -- " + extractDescr(nextTenAgain[i]));
        //        }
        //    }).fail(testFns.handleFail).fin(start);

        //});

        //function extractDescr(product) {
        //    var cat =  product.getProperty("category");
        //    return cat && cat.getProperty("categoryName") + ":" + product.getProperty("productName");
        //}

        //test("query with quotes", function () {
        //    var em = newEm();

        //    var q = EntityQuery.from("Customers")
        //        .where("companyName", 'contains', "'")
        //        .using(em);
        //    stop();

        //    q.execute().then(function (data) {
        //        ok(data.results.length > 0);
        //        var r = em.executeQueryLocally(q);
        //        ok(r.length === data.results.length, "local query should return same subset");
        //    }).fail(testFns.handleFail).fin(start);

        //});

        //test("bad query test", function () {
        //    var em = newEm();

        //    var q = EntityQuery.from("EntityThatDoesnotExist")
        //        .using(em);
        //    stop();

        //    q.execute().then(function (data) {
        //        ok(false, "should not get here");
        //    }).fail(function (e) {
        //        ok(e.message && e.message.toLowerCase().indexOf("entitythatdoesnotexist") >= 0, e.message);
        //    }).fin(function(x) {
        //        start();
        //    });
        //});




        //test("OData predicate - add combined with regular predicate", function () {
        //    if (testFns.DEBUG_MONGO) {
        //        ok(true, "Mongo does not yet support the 'add' OData predicate");
        //        return;
        //    }
        //    var manager = newEm();
        //    var predicate = Predicate.create("EmployeeID add ReportsToEmployeeID gt 3").and("employeeID", "<", 9999);

        //    var query = new breeze.EntityQuery()
        //        .from("Employees")
        //        .where(predicate);
        //    stop();
        //    manager.executeQuery(query).then(function (data) {
        //        ok(data.results.length > 0, "there should be records returned");
        //        try {
        //            manager.executeQueryLocally(query);
        //            ok(false, "shouldn't get here");
        //        } catch (e) {
        //            ok(e, "should throw an exception");
        //        }
        //    }).fail(testFns.handleFail).fin(start);
        //});



        //test("select with inlinecount", function () {
        //    var manager = newEm();
        //    var query = new breeze.EntityQuery()
        //        .from("Customers")
        //        .select("companyName, region, city")
        //        .inlineCount();
        //    stop();
        //    manager.executeQuery(query).then(function (data) {
        //        ok(data.results.length == data.inlineCount, "inlineCount should match return count");

        //    }).fail(testFns.handleFail).fin(start);
        //});

        //test("select with inlinecount and take", function () {
        //    var manager = newEm();
        //    var query = new breeze.EntityQuery()
        //        .from("Customers")
        //        .select("companyName, region, city")
        //        .take(5)
        //        .inlineCount();
        //    stop();
        //    manager.executeQuery(query).then(function (data) {
        //        ok(data.results.length == 5, "should be 5 records returned");
        //        ok(data.inlineCount > 5, "should have an inlinecount > 5");
        //    }).fail(testFns.handleFail).fin(start);
        //});

        //test("select with inlinecount and take and orderBy", function () {
        //    var manager = newEm();
        //    var query = new breeze.EntityQuery()
        //        .from("Customers")
        //        .select("companyName, region, city")
        //        .orderBy("city, region")
        //        .take(5)
        //        .inlineCount();
        //    stop();
        //    manager.executeQuery(query).then(function (data) {
        //        ok(data.results.length == 5, "should be 5 records returned");
        //        ok(data.inlineCount > 5, "should have an inlinecount > 5");
        //    }).fail(testFns.handleFail).fin(start);
        //});


        //test("expand not working with paging or inlinecount", function () {
        //    var manager = newEm();
        //    var predicate = Predicate.create(testFns.orderKeyName, "<", 10500);
        //    stop();
        //    var query = new breeze.EntityQuery()
        //        .from("Orders")
        //        .expand("orderDetails, orderDetails.product")
        //        .where(predicate)
        //        .inlineCount()
        //        .orderBy("orderDate")
        //        .take(2)
        //        .skip(1)
        //        .using(manager)
        //        .execute()
        //        .then(function (data) {
        //            ok(data.inlineCount > 0, "should have an inlinecount");

        //            var localQuery = breeze.EntityQuery
        //                .from('OrderDetails');

        //            // For ODATA this is a known bug: https://aspnetwebstack.codeplex.com/workitem/1037
        //            // having to do with mixing expand and inlineCount 
        //            // it sounds like it might already be fixed in the next major release but not yet avail.

        //            var orderDetails = manager.executeQueryLocally(localQuery);
        //            ok(orderDetails.length > 0, "should not be empty");

        //            var localQuery2 = breeze.EntityQuery
        //                .from('Products');

        //            var products = manager.executeQueryLocally(localQuery2);
        //            ok(products.length > 0, "should not be empty");
        //        }).fail(testFns.handleFail).fin(start);
        //});

        //test("test date in projection", function () {

        //    var manager = newEm();
        //    var query = new breeze.EntityQuery()
        //        .from("Orders")
        //        .where("orderDate", "!=", null)
        //        .orderBy("orderDate")
        //        .take(3);

        //    var orderDate;
        //    var orderDate2;
        //    stop();
        //    manager.executeQuery(query).then(function (data) {
        //        var result = data.results[0];
        //        orderDate = result.getProperty("orderDate");
        //        ok(core.isDate(orderDate), "orderDate should be of 'Date type'");
        //        var manager2 = newEm();
        //        var query = new breeze.EntityQuery()
        //            .from("Orders")
        //            .where("orderDate", "!=", null)
        //            .orderBy("orderDate")
        //            .take(3)
        //            .select("orderDate");
        //        return manager2.executeQuery(query);
        //    }).then(function (data2) {
        //        orderDate2 = data2.results[0].orderDate;
        //        if (testFns.DEBUG_ODATA) {
        //            ok(core.isDate(orderDate2), "orderDate2 is not a date - ugh'");
        //            var orderDate2a = orderDate2;
        //        } else {
        //            ok(!core.isDate(orderDate2), "orderDate pojection should not be a date except with ODATA'");
        //            var orderDate2a = breeze.DataType.parseDateFromServer(orderDate2);
        //        }
        //        ok(orderDate.getTime() === orderDate2a.getTime(), "should be the same date");
        //    }).fail(testFns.handleFail).fin(start);

        //});

    }
}


