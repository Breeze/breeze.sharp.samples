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
  public class QueryTests {
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

    #region Metadata

    [TestMethod]
    public void MetadataNeededToGetEntityKey() {

      // Metadata is necessary to get entity key
      // Must be first test to be meaningful, since CanFetchMetadata() below fetches 
      // metadata into the static instance of MetadataStore
      var entityManager = new EntityManager(_serviceName);

      
      var customerType = entityManager.MetadataStore.GetEntityType(typeof(Customer));
      Assert.IsNotNull(customerType);
      EntityKey key;
      try {
        key = new EntityKey(customerType, _alfredsID);
        Assert.Fail("EntityKey constructor should fail if metadata not fetched");
      } catch (Exception e) {
        Assert.IsTrue(e.Message.Contains("There are no KeyProperties yet defined"), "Thrown exception should indicated key property is not defined.  Instead it says \"" + e.Message + "\"");
      }

      var etb = new EntityTypeBuilder<Customer>(entityManager.MetadataStore);
      etb.DataProperty(c => c.CustomerID).IsPartOfKey();
      key = new EntityKey(customerType, _alfredsID);
    }

    [TestMethod]
    public async Task CanGetMetadata() {

      // Confirm the metadata can be fetched from the server
      var entityManager = new EntityManager(_serviceName);
      var dataService = await entityManager.FetchMetadata();
      Assert.IsNotNull(dataService);
    }

    #endregion Metadata

    #region Basic queries

    [TestMethod]
    public async Task AllCustomers_Concise() {
      // All instances of Customer
      var query = EntityQuery.From<Customer>();                       // One way to create a basic EntityQuery

      // Execute the query via a test helper method that encapsulates the ceremony
      await TestFns.VerifyQuery(query, _serviceName, "All customers");
    }

    [TestMethod]
    public async Task AllCustomers_Task() {
      var entityManager = new EntityManager(_serviceName);

      // All instances of Customer
      var query = new EntityQuery<Customer>();                        // Alternate way to create a basic EntityQuery

      // Handle async Task results explicitly
      await entityManager.ExecuteQuery(query).ContinueWith(task => {
            if (task.IsFaulted) {
              var message = TestFns.FormatException(task.Exception);
              Assert.Fail(message);
            } else {
              var count = task.Result.Count();
              Assert.IsTrue(count > 0, "Customer query returned " + count + " customers");
            }
          });
    }

    [TestMethod]
    public async Task AllCustomers_Exceptions() {
      var entityManager = new EntityManager(_serviceName);

      // All instances of Customer
      var query = new EntityQuery<Customer>();

      // Capture result using try-catch
      try {
        var results = await entityManager.ExecuteQuery(query);
        var count = results.Count();
        Assert.IsTrue(count > 0, "Customer query returned " + count + " customers");
      } catch (Exception e) {
        var message = TestFns.FormatException(e);
        Assert.Fail(message);
      }
    }

    [TestMethod]
    public async Task ComplexType() {
      try {
        var entityManager = await TestFns.NewEm(_serviceName);

        // Query an entity (Supplier) which holds a complex type (Location)
        var suppliers = await EntityQuery.From<Supplier>().Take(1).Execute(entityManager);

        var supplier = suppliers.FirstOrDefault();
        Assert.IsNotNull(supplier, "Supplier should be non-null");
        Assert.IsNotNull(supplier.Location, "Supplier location should be non-null");
        Assert.IsFalse(string.IsNullOrEmpty(supplier.Location.Address), "Supplier address should be non-null and not empty");
      } catch (Exception e) {
        var message = TestFns.FormatException(e);
        Assert.Fail(message);
      }
    }

    [TestMethod]
    public async Task EntityWithEnumProperty() {
      try {
        var entityManager = await TestFns.NewEm(_serviceName);
        var roles = await EntityQuery.From<Role>().Execute(entityManager);
        var roleTypes = roles.Where(r => r.RoleType != null).Select(r => r.RoleType).ToList();
        var isEnum = roleTypes.First().GetType().IsEnum;
        Assert.IsTrue(isEnum);

      } catch (Exception e) {
        var message = TestFns.FormatException(e);
        Assert.Fail(message);
      }
    }

    [TestMethod]
    public async Task CanFetchEntityTwice() {
      var entityManager = new EntityManager(_serviceName);
      await entityManager.FetchMetadata();

      var customerType = entityManager.MetadataStore.GetEntityType(typeof(Customer));
      var key = new EntityKey(customerType, _alfredsID);

      // Fetch same entity twice
      var result = await entityManager.FetchEntityByKey(key);
      Assert.IsNotNull(result.Entity, "Entity fetched by key should not be null");
      Assert.IsFalse(result.FromCache, "Entity fetched remotely should not be from cache");

      result = await entityManager.FetchEntityByKey(key);
      Assert.IsNotNull(result.Entity, "Entity re-fetched by key should not be null");
      Assert.IsFalse(result.FromCache, "Entity re-fetched remotely should not be from cache");
    }

    [TestMethod]
    public async Task QueryEntityTwice() {
      var entityManager = new EntityManager(_serviceName);
      await entityManager.FetchMetadata();

      // Query entity twice
      var query1 = new EntityQuery<Customer>().Where(c => c.CustomerID == _alfredsID);
      var alfred1 = (await entityManager.ExecuteQuery(query1)).FirstOrDefault();
      Assert.IsNotNull(alfred1, "Alfred should be found by Id");

      var query2 = new EntityQuery<Customer>().Where(c => c.CompanyName == alfred1.CompanyName);
      var alfred2 = (await entityManager.ExecuteQuery(query2)).FirstOrDefault();
      Assert.IsTrue(ReferenceEquals(alfred1, alfred2), "Successive queries should return same entity");
    }

    [TestMethod]
    public async Task RequerySameEntity() {
      var entityManager = await TestFns.NewEm(_serviceName);

      // Orders with freight cost over 100.
      var query = new EntityQuery<Order>().Where(o => o.Freight > 100);
      var orders100 = await entityManager.ExecuteQuery(query);
      Assert.IsTrue(orders100.Any(), "There should be orders with freight cost > 100");

      // Requerying same entity
      var query2 = new EntityQuery<Order>().Where(o => o.Freight > 50);
      var orders50 = await entityManager.ExecuteQuery(query2);
      Assert.IsTrue(orders50.Any(), "There should be orders with freight cost > 50");

      Assert.IsTrue(orders50.Count() >= orders100.Count(), "There should be more orders with freight > 50 than 100");
    }

    #endregion Basic queries

    #region Simple single condition queries

    [TestMethod]
    public async Task SingleConditions() {
      try {
        var entityManager = await TestFns.NewEm(_serviceName);

        var query0 = new EntityQuery<OrderDetail>().Where(od => od.Quantity > 3);
        var orderDetails = await entityManager.ExecuteQuery(query0);
        Assert.IsTrue(orderDetails.Any(), "There should be orders with freight cost > 100");

        //  Customers starting w/ 'A' (string comparison)
        var query1 = new EntityQuery<Customer>().Where(c => c.CompanyName.StartsWith("A"))
                                                .OrderBy(c => c.CompanyName);
        var customers = await entityManager.ExecuteQuery(query1);
        Assert.IsTrue(customers.Any(), "There should be customers whose name begins with A");

        // Orders with freight cost over 100.
        var query2 = new EntityQuery<Order>().Where(o => o.Freight > 100);
        var orders = await entityManager.ExecuteQuery(query2);
        Assert.IsTrue(orders.Any(), "There should be orders with freight cost > 100");

        // Orders placed on or after 1/1/1998.
        var testDate = new DateTime(1998, 1, 3);
        var query3 = new EntityQuery<Order>().Where(o => o.OrderDate >= testDate);
        orders = await entityManager.ExecuteQuery(query3);
        Assert.IsTrue(orders.Any(), "There should be orders placed after 1/1/1998");

        // Orders placed on 1/1/1998.
        var query4 = new EntityQuery<Order>().Where(o => o.OrderDate == testDate);
        orders = await entityManager.ExecuteQuery(query4);
        Assert.IsTrue(!orders.Any(), "There should no orders placed on 1/2/1998.  There are " + orders.Count());
      } catch (Exception e) {
        var message = TestFns.FormatException(e);
        Assert.Fail(message);
      }
    }



    #endregion Simple single condition queries

    #region Expansion to related entities

    [TestMethod]
    public async Task Expansions() {

      try {
        var entityManager = await TestFns.NewEm(_serviceName);

        EntityQuery<Order> query;
        IEnumerable<Order> orders;

        // Alfreds orders expanded with their OrderDetails
        query = new EntityQuery<Order>().Where(o => o.CustomerID == _alfredsID).Expand("OrderDetails");
        orders = await entityManager.ExecuteQuery(query);
        AssertGotOrderDetails(entityManager, orders);

        // Alfreds orders expanded with their parent Customers and OrderDetails 
        query = new EntityQuery<Order>().Where(o => o.CustomerID == _alfredsID).Expand("Customer").Expand("OrderDetails");
        orders = await entityManager.ExecuteQuery(query);
        AssertGotOrderDetails(entityManager, orders);
        AssertGotCustomerByExpand(entityManager, orders);

        // Alfreds orders, including their OrderDetails, and the Products of those details, 
        query = new EntityQuery<Order>().Where(o => o.CustomerID == _alfredsID).Expand("Customer").Expand("OrderDetails.Product");
        orders = await entityManager.ExecuteQuery(query);
        AssertGotOrderDetails(entityManager, orders);
        AssertGotCustomerByExpand(entityManager, orders);
        AssertGotProductsByExpand(entityManager, orders);
      } catch (Exception e) {
        var message = TestFns.FormatException(e);
        Assert.Fail(message);
      }

      // Products with related Supplier entity with complex type, 
      try {
        var entityManager = await TestFns.NewEm(_serviceName);

        var query = new EntityQuery<Product>().Take(1).Expand("Supplier");
        var products = await entityManager.ExecuteQuery(query);
        Assert.IsTrue(products.Any(), "A product should be returned");
        var product = products.First();
        Assert.IsNotNull(product.Supplier, "A product should have a supplier");
      } catch (Exception e) {
        var message = TestFns.FormatException(e);
        Assert.Fail(message);
      }
    }

    private void AssertGotOrderDetails(EntityManager entityManager, IEnumerable<Order> orders) {
      var odType = entityManager.MetadataStore.GetEntityType("OrderDetail");

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

    private void AssertGotCustomerByExpand(EntityManager entityManager, IEnumerable<Order> orders) {
      var firstOrder = orders.FirstOrDefault();
      Assert.IsNotNull(firstOrder, "There should be at least one order returned");
      Assert.IsNotNull(firstOrder.Customer, "Related customer should be returned");
    }

    private void AssertGotProductsByExpand(EntityManager entityManager, IEnumerable<Order> orders) {
      Assert.IsTrue(orders.Any(), "There should be at least one order returned");
      var firstOrder = orders.First();

      Assert.IsTrue(firstOrder.OrderDetails.Any(), "There should be at least one order detail returned");
      var firstOrderDetail = firstOrder.OrderDetails.First();
      Assert.IsNotNull(firstOrderDetail.Product, "Related product should be returned");
    }

    #endregion Expansion to related entities

    #region Ordering and Paging

    [TestMethod]
    public async Task OrderingAndPaging() {
      var entityManager = await TestFns.NewEm(_serviceName);
      EntityQuery<Product> query;
      IEnumerable<Product> products;

      try {
        // Products sorted by name
        query = new EntityQuery<Product>().Expand("Category").OrderBy(p => p.ProductName);
        products = await entityManager.ExecuteQuery(query);
        VerifyProductResults(products);

        // Products sorted by name in descending order
        query = new EntityQuery<Product>().Expand("Category").OrderByDescending(p => p.ProductName);
        products = await entityManager.ExecuteQuery(query);
        VerifyProductResults(products);

        // Products sorted by price descending, then name ascending
        query = new EntityQuery<Product>().Expand("Category").OrderBy(p => p.ProductName).OrderByDescending(p => p.ProductName);
        products = await entityManager.ExecuteQuery(query);
        VerifyProductResults(products);

        // look in results for ...
        //    (27) 'Schoggi Schokolade' at $43.9 in 'Confections', 
        //    (63) 'Vegie-spread' at $43.9 in 'Condiments',...

        // Products sorted by related category descending
        query = new EntityQuery<Product>().Expand("Category").OrderByDescending(p => p.Category.CategoryName);
        products = await entityManager.ExecuteQuery(query);
        VerifyProductResults(products);

        // First 5 of products ordered by product name, then expanded to related category
        query = new EntityQuery<Product>().OrderBy(p => p.ProductName).Take(5).Expand("Category");
        products = await entityManager.ExecuteQuery(query);
        VerifyProductResults(products);

        // Skip first 10 of products ordered by product name, then expanded to related category
        query = new EntityQuery<Product>().OrderBy(p => p.ProductName).Skip(10).Expand("Category");
        products = await entityManager.ExecuteQuery(query);
        VerifyProductResults(products);

        // Products paging with skip and take
        query = new EntityQuery<Product>().OrderBy(p => p.ProductName).Skip(10).Take(5).Expand("Category");
        products = await entityManager.ExecuteQuery(query);
        VerifyProductResults(products);

        // Inline count of paged products
        var productQuery = new EntityQuery<Product>().Where(p => p.ProductName.StartsWith("C"));
        var pagedQuery = productQuery.OrderBy(p => p.ProductName).Skip(5).Take(5).InlineCount();

        // Execute in parallel and verify products received
        var productTask = entityManager.ExecuteQuery(productQuery);
        var pagedTask = entityManager.ExecuteQuery(pagedQuery);
        await Task.WhenAll(productTask, pagedTask);

        var productCount = productTask.Result.Count();
        var pageCount = pagedTask.Result.Count();
        var pagedQueryResult = pagedTask.Result as QueryResult<Product>;
        var inlineCount = pagedQueryResult.InlineCount;

        Assert.AreEqual(productCount, inlineCount, "Inline count should return item count excluding skip/take");
        Assert.IsTrue(pageCount <= productCount, "Paged query should return subset of total query");
      } catch (Exception e) {
        var message = TestFns.FormatException(e);
        Assert.Fail(message);
      }
    }


    private void VerifyProductResults(IEnumerable<Product> products) {
      var limit = 15;
      var count = products.Count();
      var results = limit < count ? products.Take(limit) : products;
      results.ForEach(p =>
          Console.WriteLine(string.Format("({0}) '{1}' at ${2} in '{3}'",
                                          p.ProductID, p.ProductName,
                                          p.UnitPrice, p.Category.CategoryName)));
      if (count > results.Count()) {
        Console.WriteLine("...");
      }
    }

    #endregion Ordering and Paging

    #region Projection

    [TestMethod]
    public async Task Projection() {
      try {
        var entityManager = await TestFns.NewEm(_serviceName);
        // Next line does not work because of MS OData restrictions.
        // var productNames = await new EntityQuery<Product>().Select(p => p.ProductName).Execute(entityManager);
        var productNames = await new EntityQuery<Product>().Select(p => new { p.ProductName }).Execute(entityManager);
        Assert.IsTrue(productNames.Any(), "Projection query should return items");
      } catch (Exception e) {
        var message = TestFns.FormatException(e);
        Assert.Fail(message);
      }
    }

    [TestMethod]
    public async Task AnonymousProjection() {
      try {
        var entityManager = await TestFns.NewEm(_serviceName);

        // Anonymous projection of single property
        // Use anonymous type due to raw property projection not supported by OData
        var productNames = await new EntityQuery<Product>().Select(p => new { p.ProductName }).Execute(entityManager);
        Assert.IsTrue(productNames.Any(), "Projection query should return items");

        // Projection of multiple properties with filter condition and ordering
        var query1 = new EntityQuery<Customer>().Where(c => c.CompanyName.StartsWith("C"))
                                                .OrderBy(obj => obj.CompanyName)
                                                .Select(c => new {
                                                  CustomerID = c.CustomerID,
                                                  CompanyName = c.CompanyName,
                                                  ContactName = c.ContactName,
                                                });

        var results1 = await entityManager.ExecuteQuery(query1);
        Assert.IsTrue(results1.Any(), "Orojection query should produce data");
        Assert.IsTrue(results1.Select(obj => obj.CustomerID).OfType<Guid>().Any(), "resulting CustomerIDs should be Guids");
        Assert.IsTrue(results1.Select(obj => obj.CompanyName).OfType<string>().Any(), "resulting CompanyNames should be strings");
        Assert.IsTrue(results1.Select(obj => obj.ContactName).OfType<string>().Any(), "resulting ContactNames should be strings");

        var customersInCache = entityManager.GetEntities<Customer>();
        Assert.IsFalse(customersInCache.Any(), "projection query should not have retrieved customers into cache");

        // Projection containing a collection
        // Note that orders are in cache because they are whole entities
        // Customer names are not entities and are not in cache.
        var query2 = new EntityQuery<Customer>().Where(c => c.CompanyName.StartsWith("C"))
                                                .OrderBy(obj => obj.CompanyName)
                                                .Select(c => new {
                                                  CompanyName = c.CompanyName,
                                                  Orders = c.Orders
                                                });
        var results2 = await entityManager.ExecuteQuery(query2);

        Assert.IsTrue(results2.Any(), "projection query should produce data");
        Assert.IsTrue(results2.Select(obj => obj.CompanyName).OfType<string>().Any(), "resulting CompanyNames should be strings");

        var ordersInCache = entityManager.GetEntities<Order>();
        Assert.IsTrue(ordersInCache.Any(), "projection containing orders should have retrieved orders into cache");
      } catch (Exception e) {
        var message = TestFns.FormatException(e);
        Assert.Fail(message);
      }
    }

    [TestMethod]
    public async Task ServerSideProjection() {

      var entityManager = await TestFns.NewEm(_serviceName);

      // Query from service that projects User onto UserPartial, omitting the Password property
      var query1 = EntityQuery.From("UserPartials", new {
        Id = 0L,
        UserName = "",
        FirstName = "",
        LastName = "",
        Email = ""
        // Even though this works, sending every user's roles seems unwise
        // Roles = user.UserRoles.Select(ur => ur.Role)
      });
      var users1 = await query1.Execute(entityManager);

      Assert.IsTrue(users1.Any(), "UserPartials query should produce data");
      var user1 = users1.First();

      var usersInCache = entityManager.GetEntities<User>();
      Assert.IsFalse(usersInCache.Any(), "UserPartials query should not have retrieved User entities into cacha");

      var rolesInCache = entityManager.GetEntities<Role>();
      Assert.IsFalse(rolesInCache.Any(), "UserPartials query should not have retrieved Role entities into cacha");

      // Query from similar service that accepts parameter
      var query2 = EntityQuery.From("GetUserById", new {
        Id = 0L,
        UserName = "",
        FirstName = "",
        LastName = "",
        Email = "",
        // Roles = Enumerable.Empty<Role>()
        RoleNames = "" 
        // Even though this works, sending every user's roles seems unwise
        // Roles = user.UserRoles.Select(ur => ur.Role)
      }).WithParameter("Id", 3);

      var users2 = await query2.Execute(entityManager);
      Assert.IsTrue(users2.Count() == 1, "GetUserById query should return exactly 1 user");
      var user2 = users2.First();

      var usersInCache2 = entityManager.GetEntities<User>();
      Assert.IsFalse(usersInCache2.Any(), "GetUserById query should not have retrieved User entities into cacha");

      var roleNames = user2.GetPropValue<String>("RoleNames");
      Assert.IsTrue(!String.IsNullOrEmpty(roleNames), "User with Id 3 should have defined roles");


    }

    #endregion Projection

    #region Using specialized server controller methods

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

      } catch (Exception e) {
        var message = TestFns.FormatException(e);
        Assert.Fail(message);
      }
    }

    #endregion Using specialized server controller methods

    #region Original contents of Breeze.sharp.internal.tests QueryTests.cs
    /*
        //=======================================================================================================================================

        //*********************************************************
        // Original contents of Breeze.sharp.internal.tests QueryTests.cs
        //
        //*********************************************************

        [TestMethod]
        public async Task SimpleQuery() {
            var em1 = await TestFns.NewEm(_serviceName);
            var q = new EntityQuery<Customer>();
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
            var ok = results.All(r1 => (r1.Orders.Any()));
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
              r1.GetType() == typeof(Dummy) && r1.Orders.Any());

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
        */

    #endregion Original contents of Breeze.sharp.internal.tests QueryTests.cs
  }
}


