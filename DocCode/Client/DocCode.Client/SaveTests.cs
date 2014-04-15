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
    public class SaveTests
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
        public async Task SaveNewEntity() {
            var entityManager = await TestFns.NewEm(_serviceName);

            // Create a new customer
            var customer = new Customer();
            customer.CustomerID = Guid.NewGuid();
            customer.CompanyName ="Test1 " + DateTime.Now.ToString();
            entityManager.AddEntity(customer);

            try {
                var saveResult = await entityManager.SaveChanges();
            } catch (Exception e) {
                var message = "Server should not have rejected save of Customer entity with the error " + e.Message;
                Assert.Fail(message);
            }
        }

        [TestMethod]
        public async Task SaveModifiedEntity() {
            var entityManager = await TestFns.NewEm(_serviceName);

            // Create a new customer
            var customer = new Customer { CustomerID = Guid.NewGuid() };
            entityManager.AddEntity(customer);
            customer.CompanyName = "Test2A " + DateTime.Now.ToString();

            try {
                var saveResult = await entityManager.SaveChanges();
                var savedEntity = saveResult.Entities[0];
                Assert.IsTrue(savedEntity is Customer && savedEntity == customer, "After save, added entity should still exist");

                // Modify customer
                customer.CompanyName = "Test2M " + DateTime.Now.ToString();

                saveResult = await entityManager.SaveChanges();
                savedEntity = saveResult.Entities[0];
                Assert.IsTrue(savedEntity is Customer && savedEntity == customer, "After save, modified entity should still exist");

            } catch (Exception e) {
                var message = string.Format("Save of customer {0} should have succeeded;  Received {1}: {2}", 
                                            customer.CompanyName, e.GetType().Name, e.Message);
                Assert.Fail(message);
            }
        }
    
        [TestMethod]
        public async Task SaveDeletedEntity() {
            var entityManager = await TestFns.NewEm(_serviceName);
        
            // Create a new customer
            var customer = new Customer { CustomerID = Guid.NewGuid() };
            entityManager.AddEntity(customer);
            customer.CompanyName = "Test3A " + DateTime.Now.ToString();

            try {
                var saveResult = await entityManager.SaveChanges();
                var savedEntity = saveResult.Entities[0];
                Assert.IsTrue(savedEntity is Customer && savedEntity == customer, "After save, added entity should still exist");

                // Delete customer
                customer.EntityAspect.Delete();
                Assert.IsTrue(customer.EntityAspect.EntityState == EntityState.Deleted, 
                              "After delete, entity state should be deleted, not " + customer.EntityAspect.EntityState.ToString());
                saveResult = await entityManager.SaveChanges();
                savedEntity = saveResult.Entities[0];
                Assert.IsTrue(savedEntity.EntityAspect.EntityState == EntityState.Detached, 
                              "After save of deleted entity, entity state should be detached, not " + savedEntity.EntityAspect.EntityState.ToString());

            } catch (Exception e) {
                var message = string.Format("Save of deleted customer {0} should have succeeded;  Received {1}: {2}", 
                                            customer.CompanyName, e.GetType().Name, e.Message);
                Assert.Fail(message);
            }
        }

    
    /*
     * This test removed when we made InternationalOrder a subclass of Order
     * Restore it if/when decide to demo IO as a separate entity related in 1..(0,1)
     * 
    asyncTest("can save a new Northwind Order & InternationalOrder [1..(0,1) relationship]", 2, function () {
        // Create and initialize entity to save
        var em = newNorthwindEm();

        var order = em.createEntity('Order', {
            CustomerID: testFns.wellKnownData.alfredsID,
            EmployeeID: testFns.wellKnownData.nancyID,
            ShipName: "Test " + new Date().toISOString()
        });

        var internationalOrder = em.createEntity('InternationalOrder', {
            Order: order, // sets OrderID and pulls it into the order's manager
            CustomsDescription: "rare, exotic birds"
        });

        em.saveChanges()
            .then(successfulSave).fail(handleSaveFailed).fin(start);

        function successfulSave(saveResults) {
            var orderId = order.OrderID();
            var internationalOrderID = internationalOrder.OrderID();

            equal(internationalOrderID, orderId,
                "the new internationalOrder should have the same OrderID as its new parent Order, " + orderId);
            ok(orderId > 0, "the OrderID is positive, indicating it is a permanent order");
        }

    });
     */

        [TestMethod]
        public async Task DeleteClearsRelatedParent() {
            var entityManager = await TestFns.NewEm(_serviceName);

            var products = await new EntityQuery<Product>().Take(1).Expand("Category").Execute(entityManager);
            Assert.IsTrue(products.Count() == 1, "Should receive single entity from Take(1) query of products");
            var product = products.First();
            Assert.IsNotNull(product.Category, "Product should have a Category before delete");

            // Delete the product
            product.EntityAspect.Delete();

            Assert.IsNull(product.Category, "Product should NOT have a Category after product deleted");
            // FKs of principle related entities are retained. Should they be cleared too?
            Assert.IsTrue(product.CategoryID != 0, "Product should have a non-zero CategoryID after product deleted");
        }
    
        [TestMethod]
        public async Task DeleteClearsRelatedChildren() {
            var entityManager = await TestFns.NewEm(_serviceName);

            var orders = await new EntityQuery<Order>().Take(1).Expand("Customer, Employee, OrderDetails").Execute(entityManager);
            Assert.IsTrue(orders.Count() == 1, "Should receive single entity from Take(1) query of orders");
            var order = orders.First();

            Assert.IsNotNull(order.Customer, "Order should have a Customer before delete");
            Assert.IsNotNull(order.Employee, "order should have a Employee before delete");
            
            var details = order.OrderDetails;
            Assert.IsTrue(details.Any(), "Order should have OrderDetails before delete");

            order.EntityAspect.Delete();

            Assert.IsNull(order.Customer, "Order should NOT have a Customer after order deleted");
            Assert.IsNull(order.Employee, "order should NOT have a Employee after order deleted");

            // FK values should still be present
            Assert.IsTrue(order.CustomerID != null && order.CustomerID != Guid.Empty, "Order should still have a non-zero CustomerID after order deleted");
            Assert.IsTrue(order.EmployeeID != null && order.CustomerID != Guid.Empty, "Order should still have a non-zero EmployeeID after order deleted");

            Assert.IsFalse(order.OrderDetails.Any(), "Order should NOT have OrderDetails after delete");

            Assert.IsTrue(details.All(od => od.OrderID == 0), "OrderID of every original detail should be zero after order deleted");
        }
    }
}


