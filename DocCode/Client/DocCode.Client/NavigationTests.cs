using System;
using System.ComponentModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

using Breeze.Sharp.Core;
using Breeze.Sharp;

using Northwind.Models;
using Todo.Models;

namespace Test_NetClient
{
    [TestClass]
    public class NavigationTests
    {
        // Useful well-known data
        private readonly Guid _alfredsID = Guid.Parse("785efa04-cbf2-4dd7-a7de-083ee17b6ad2");

        private String _northwindServiceName;
        private String _todosServiceName;

        // Useful utility method
        private Customer CreateFakeExistingCustomer(EntityManager entityManager, string companyName = "Existing Customer") {
            var customer = new Customer();
            customer.CompanyName = companyName;
            customer.CustomerID = Guid.NewGuid();
            entityManager.AttachEntity(customer);
            return customer;
        }

        // Another useful utility method
        private Order CreateOrder(string shipName = "New Order") {
        var order       = new Order();
        order.ShipName = shipName;
        return order;
    }

        [TestInitialize]
        public void TestInitializeMethod() {
            MetadataStore.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            MetadataStore.Instance.ProbeAssemblies(typeof(TodoItem).Assembly);
            _northwindServiceName = "http://localhost:56337/breeze/Northwind/";
            _todosServiceName = "http://localhost:56337/breeze/Todos/";
        }

        [TestCleanup]
        public void TearDown() {
        }

        [TestMethod]
        public async Task SimpleNavigation() {
            var entityManager = await TestFns.NewEm(_northwindServiceName);

            /*********************************************************
            * get Customer info of Alfreds 1st order via navigation
            * Use case: 
            *   Eagerly load parent Customer with child order
            *********************************************************/

            var ordersWithCustomerQuery = new EntityQuery<Order>().Where(o => o.CustomerID ==_alfredsID).Expand("Customer");
            var ordersByQuery           = await entityManager.ExecuteQuery(ordersWithCustomerQuery);
            Assert.IsTrue(ordersByQuery.Any(), "Should have retrieved some orders");

            var lastOrder               = ordersByQuery.Last();
            var customer                = lastOrder.Customer;
            Assert.IsNotNull(customer, "Customer for queried orders should be in cache");

            var ordersByNavigation      = customer.Orders;
            Assert.AreEqual(ordersByQuery.Count() ,ordersByNavigation.Count(), "Should get same number of orders by navigating back from parent Customer");

            Assert.IsFalse(lastOrder.OrderDetails.Any(), "An order's details should not be available because they were not included in query");

            /*********************************************************
            * get OrderDetails of Alfreds 1st order via navigation
            * Use case: 
            *   Eagerly load OrderDetails with parent Order
            *********************************************************/

            var ordersWithDetails = await new EntityQuery<Order>().Where(o => o.CustomerID ==_alfredsID).Expand("OrderDetails").Take(1).Execute(entityManager);
            Assert.AreEqual(1, ordersWithDetails.Count(), "Take(1) query should retrieve one order");

            var firstOrder              = ordersWithDetails.First();
            var detailsByNavigation     = firstOrder.OrderDetails;
            Assert.IsTrue(detailsByNavigation.Any(), "Should have loaded OrderDetails into cache");

            var firstDetail             = detailsByNavigation.First();
            var orderByNavigation       = firstDetail.Order;
            Assert.AreEqual(firstOrder, orderByNavigation, "OrderDetail should navigate back to parent order");

            Assert.IsNull(firstDetail.Product, "An OrderDetail's Product should not be available because Products were not included in query");
        }

        [TestMethod]
        public async Task SettingOfNavigationProperties() {
            var entityManager = await TestFns.NewEm(_northwindServiceName);

            /*********************************************************
            * setting child's parent entity enables parent to navigate to child
            * Use case: 
            *   Creating a new Order and assigning it to an existing Customer
            *********************************************************/
            var existingCustomer = CreateFakeExistingCustomer(entityManager);
            Assert.AreEqual(EntityState.Unchanged, existingCustomer.EntityAspect.EntityState, "Existing customer should be unchanged at start of test");

            var newOrder = CreateOrder();
            entityManager.AddEntity(newOrder);
        
            // The newOrder has no Customer 
            Assert.IsNull(newOrder.Customer, "Newly created order should have no customer");

            // Subscribe to events
            var orderPropertyChangedEventCount              = 0;
            var customerPropertyChangedEventCount           = 0;
            var customerOrdersPropertyChangedEventCount     = 0;
            ((INotifyPropertyChanged) newOrder).PropertyChanged           += (s, e) => ++orderPropertyChangedEventCount;      
            ((INotifyPropertyChanged) existingCustomer).PropertyChanged   += (s, e) => ++customerPropertyChangedEventCount;   
            existingCustomer.Orders.PropertyChanged         += (s, e) => ++customerOrdersPropertyChangedEventCount;

            // Set order's Customer
            newOrder.Customer = existingCustomer;
        
            // The newOrder has a Customer now
            Assert.IsNotNull(newOrder.Customer, "After assignment, new order has a customer");
        
            // Notice that the parent Customer immediately picks up this order!  
            Assert.IsTrue(existingCustomer.Orders.Any(o => o == newOrder), "The newly added order should be among the existing customers orders");
        
            // But the customer entity itself is unchanged
            Assert.AreEqual(EntityState.Unchanged, existingCustomer.EntityAspect.EntityState, "Existing customer should be unchanged by assignment to new order");

            // Verify property changed notifications
            // *** Failing test
            Assert.AreEqual(2, orderPropertyChangedEventCount, "Setting the order's Customer should raise the order's PropertyChanged twice, " +
                                                               "once for FK change and once for the navigation property change");
            Assert.AreEqual(0, customerPropertyChangedEventCount, "Setting the order's customer should NOT raise customer's property changed");

            // *** Failing test
            Assert.AreEqual(1, customerOrdersPropertyChangedEventCount, "Setting the order's customer should raise the Customer.Orders  PropertyChanged event once");

            /*********************************************************
            * changing child's parent entity removes it from old parent
            * Use case: 
            *   reassigning an order to a different Customer
            *********************************************************/
            
            // Accept changes on the new Order
            newOrder.EntityAspect.AcceptChanges();
            Assert.IsTrue(newOrder.EntityAspect.EntityState.IsUnchanged(), "Entity state of order should be unchanged after AcceptChanges()");

            // Create a new customer
            var customer2 = CreateFakeExistingCustomer(entityManager, "Customer 2");
            Assert.IsFalse(customer2.Orders.Any(), "Newly created customer should have no orders before moving");
            Assert.IsTrue(existingCustomer.Orders.Any(o => o == newOrder), "Original customer should have order before moving");
            
            // move order to customer2
            newOrder.Customer = customer2;
            Assert.IsFalse(existingCustomer.Orders.Any(), "Original customer should have no orders after moving");
            Assert.IsTrue(customer2.Orders.Any(o => o == newOrder), "New customer should have order after moving");
            Assert.IsTrue(newOrder.EntityAspect.EntityState.IsModified(), "Entity state of order should be modified after AcceptChanges()");

            /*********************************************************
            * Undoing a change of parent navigation restores original state
            * for both the child entity and the related customers
            *********************************************************/

            // Reject changes to newOrder
            newOrder.EntityAspect.RejectChanges();
            Assert.IsFalse(customer2.Orders.Any(), "Newly created customer should have no orders after RejectChanges()");
            Assert.IsTrue(existingCustomer.Orders.Any(o => o == newOrder), "Original customer should have order after RejectChanges()");
            Assert.IsTrue(newOrder.EntityAspect.EntityState.IsUnchanged(), "Entity state of order should be unchanged after RejectChanges()");
        }

        [TestMethod]
        public async Task AddingToCollectionNavigationProperties() {
            var entityManager = await TestFns.NewEm(_northwindServiceName);

            /*********************************************************
            * Can add to existing customer's orders collection
            *********************************************************/
            var existingCustomer = CreateFakeExistingCustomer(entityManager);
            Assert.AreEqual(EntityState.Unchanged, existingCustomer.EntityAspect.EntityState, "Existing customer should be unchanged at start of test");

            var customerOrdersPropertyChangedEventCount = 0;
            var customerOrdersCollectionChangedEventCount = 0;
            existingCustomer.Orders.PropertyChanged += (s, e) => ++customerOrdersPropertyChangedEventCount;
            existingCustomer.Orders.CollectionChanged += (s, e) => ++customerOrdersCollectionChangedEventCount;

            var newOrder = CreateOrder();
            entityManager.AddEntity(newOrder);
            existingCustomer.Orders.Add(newOrder);

            Assert.IsNotNull(newOrder.Customer, "After assignment, new order has a customer");
            Assert.IsTrue(existingCustomer.Orders.Any(o => o == newOrder), "The newly added order should be among the existing customers orders");
            Assert.AreEqual(EntityState.Unchanged, existingCustomer.EntityAspect.EntityState, "Existing customer should be unchanged by assignment to new order");

            Assert.AreEqual(1, customerOrdersPropertyChangedEventCount, "Adding first order should raise PropertyChanged on Orders property of customer");
            Assert.AreEqual(1, customerOrdersCollectionChangedEventCount, "Adding first order should raise CollectionChanged on Orders property of customer");

            var newOrder2 = CreateOrder();
            entityManager.AddEntity(newOrder2);
            existingCustomer.Orders.Add(newOrder2);

            Assert.AreEqual(2, customerOrdersPropertyChangedEventCount, "Adding second order should raise PropertyChanged on Orders property of customer");
            Assert.AreEqual(2, customerOrdersCollectionChangedEventCount, "Adding first order should raise CollectionChanged on Orders property of customer");
        }
    }
}


