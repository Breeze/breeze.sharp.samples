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
using Todo.Models;

namespace Test_NetClient
{
    [TestClass]
    public class SaveTests
    {
        // Useful well-known data
        private readonly Guid _alfredsID = Guid.Parse("785efa04-cbf2-4dd7-a7de-083ee17b6ad2");

        private String _northwindServiceName;
        private String _todosServiceName;

        [TestInitialize]
        public void TestInitializeMethod() {
            Configuration.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            Configuration.Instance.ProbeAssemblies(typeof(TodoItem).Assembly);
            _northwindServiceName = "http://localhost:56337/breeze/Northwind/";
            _todosServiceName = "http://localhost:56337/breeze/Todos/";
        }

        [TestCleanup]
        public void TearDown() {
        }

        [TestMethod]
        public async Task SaveNewEntity() {
            var entityManager = await TestFns.NewEm(_northwindServiceName);

            // Create a new customer
            var customer            = new Customer();
            customer.CustomerID     = Guid.NewGuid();
            customer.CompanyName    ="Test1 " + DateTime.Now.ToString();
            entityManager.AddEntity(customer);
            Assert.IsTrue(customer.EntityAspect.EntityState == EntityState.Added, "State of new entity should be Added");

            try {
                var saveResult = await entityManager.SaveChanges();
                Assert.IsTrue(customer.EntityAspect.EntityState == EntityState.Unchanged, "State of saved entity should be Unchanged");
            }
            catch (Exception e) {
                var message = "Server should not have rejected save of Customer entity with the error " + e.Message;
                Assert.Fail(message);
            }
        }

        [TestMethod]
        public async Task SaveModifiedEntity() {
            var entityManager = await TestFns.NewEm(_northwindServiceName);

            // Create a new customer
            var customer = new Customer { CustomerID = Guid.NewGuid() };
            entityManager.AddEntity(customer);
            customer.CompanyName = "Test2A " + DateTime.Now.ToString();
            Assert.IsTrue(customer.EntityAspect.EntityState == EntityState.Added, "State of new entity should be Added");

            try {
                var saveResult = await entityManager.SaveChanges();
                var savedEntity = saveResult.Entities[0];
                Assert.IsTrue(savedEntity is Customer && savedEntity == customer, "After save, added entity should still exist");
                Assert.IsTrue(customer.EntityAspect.EntityState == EntityState.Unchanged, "State of saved entity should be Unchanged");

                // Modify customer
                customer.CompanyName = "Test2M " + DateTime.Now.ToString();
                Assert.IsTrue(customer.EntityAspect.EntityState == EntityState.Modified, "State of modified entity should be Modified");

                saveResult = await entityManager.SaveChanges();
                savedEntity = saveResult.Entities[0];
                Assert.IsTrue(savedEntity is Customer && savedEntity == customer, "After save, modified entity should still exist");
                Assert.IsTrue(customer.EntityAspect.EntityState == EntityState.Unchanged, "State of saved entity should be Unchanged");

            } catch (Exception e) {
                var message = string.Format("Save of customer {0} should have succeeded;  Received {1}: {2}", 
                                            customer.CompanyName, e.GetType().Name, e.Message);
                Assert.Fail(message);
            }
        }
    
        [TestMethod]
        public async Task SaveDeletedEntity() {
            var entityManager = await TestFns.NewEm(_northwindServiceName);
        
            // Create a new customer
            var customer = new Customer { CustomerID = Guid.NewGuid() };
            entityManager.AddEntity(customer);
            customer.CompanyName = "Test3A " + DateTime.Now.ToString();
            Assert.IsTrue(customer.EntityAspect.EntityState == EntityState.Added, "State of new entity should be Added");

            try {
                var saveResult = await entityManager.SaveChanges();
                var savedEntity = saveResult.Entities[0];
                Assert.IsTrue(savedEntity is Customer && savedEntity == customer, "After save, added entity should still exist");
                Assert.IsTrue(customer.EntityAspect.EntityState == EntityState.Unchanged, "State of saved entity should be Unchanged");

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
            var entityManager = await TestFns.NewEm(_northwindServiceName);

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
            var entityManager = await TestFns.NewEm(_northwindServiceName);

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

        [TestMethod]
        public async Task SaveWithDefaultBooleanvalues() {
          var entityManager = await TestFns.NewEm(_todosServiceName);

          var newTodo = entityManager.CreateEntity<TodoItem>();
          var tempId = newTodo.Id;
          newTodo.IsDone = false;
          var tempIsDone = newTodo.IsDone;
          var description = "Save todo in Breeze";
          newTodo.Description = description;
          

          try {
            await entityManager.SaveChanges();
          } catch (Exception e) {
            var message = "SaveChanges should not fail with the error " + e.Message;
            Assert.Fail(message);
          }

          var isDone = newTodo.IsDone;
          Assert.AreEqual(tempIsDone, isDone, "Values should match");
        }

        [TestMethod]
        public async Task SaveWithAutoIdGeneration() {
            var entityManager = await TestFns.NewEm(_todosServiceName);

            var newTodo         = entityManager.CreateEntity<TodoItem>();
            var tempId          = newTodo.Id;
            var description     = "Save todo in Breeze";
            newTodo.Description = description;

            try {
                await entityManager.SaveChanges();
            }
            catch (Exception e) {
                var message = "SaveChanges should not fail with the error " + e.Message;
                Assert.Fail(message);
            }

            var id = newTodo.Id; // permanent id is now known
            Assert.AreNotEqual(tempId, id, "New permanent Id value should be populated in entity by SaveChanges()");

            // Clear local cache and re-query from database to confirm it really did get saved
            entityManager.Clear();
            var query = new EntityQuery<TodoItem>().Where(td => td.Id == id);
            var todos1 = await entityManager.ExecuteQuery(query);
            Assert.IsTrue(todos1.Count() == 1, "Requery of saved Todo should yield one item");
            var todo1 = todos1.First();
            Assert.IsTrue(todo1.Description == description, "Requeried entity should have saved values");

            // Requery into new entity manager
            var entityManager2 = await TestFns.NewEm(_todosServiceName);
            var todos2 = await entityManager2.ExecuteQuery(query);
            Assert.IsTrue(todos2.Count() == 1, "Requery of saved Todo should yield one item");
            var todo2 = todos2.First();
            Assert.IsTrue(todo2.Description == description, "Requeried entity should have saved values");

            Assert.AreNotSame(todo1, todo2, "Objects in different entity managers should not be the same object");
        }

        [TestMethod]
        public async Task AddUpdateAndDeleteInBatch() {
            var entityManager = await TestFns.NewEm(_todosServiceName);

            // Be sure there are at least two Todos in the database so we have something to update and delete
            var now             = DateTime.Now;
            var todo            = entityManager.CreateEntity<TodoItem>();
            todo.Description    = "OLD1: " + now;
            todo.CreatedAt      = now;
            todo                = entityManager.CreateEntity<TodoItem>();
            todo.Description    = "OLD2: " + now;
            todo.CreatedAt      = now;
            await entityManager.SaveChanges();

            // Add a new Todo
            var newTodo         = entityManager.CreateEntity<TodoItem>();
            newTodo.Description = "NEW: " + now;

            // Get the two Todos to modify and delete
            var twoQuery        = new EntityQuery<TodoItem>().Where(td => td.CreatedAt == now).Take(2);
            var todos           = await entityManager.ExecuteQuery(twoQuery);
            Assert.IsTrue(todos.Count() == 2, "Take(2) query should return the two items");

            var updateTodo          = todos.First();
            updateTodo.Description  = "UPDATE: " + now;

            var deleteTodo = todos.Skip(1).First();
            deleteTodo.EntityAspect.Delete();

            var numChanges = entityManager.GetChanges().Count();
            Assert.AreEqual(numChanges, 3, "There should be three changed entities in the cache");

            try {
                var saveResult = await entityManager.SaveChanges();
                Assert.AreEqual(saveResult.Entities.Count(), 3, "There should be three saved entities");
                Assert.IsTrue(saveResult.Entities.All(e => e.EntityAspect.EntityState.IsUnchanged() || e.EntityAspect.EntityState.IsDetached()), 
                              "All saved entities should be in unchanged state");
            }
            catch (Exception e) {
                var message = "Server should not have rejected save of TodoItem entities with the error " + e.Message;
                Assert.Fail(message);
            }

            var entitiesInCache = entityManager.GetEntities();
            Assert.AreEqual(entitiesInCache.Count(), 2, "There should be only two entities in cache after save of deleted entity");

            Assert.IsTrue(!entitiesInCache.Any(td => td == deleteTodo), "Deleted entity should not be in cache");
        }

        [TestMethod]
        public async Task HasChangesChangedEvent() {
            var entityManager = await TestFns.NewEm(_todosServiceName);

            int eventCount = 0;
            var lastEventArgs = new EntityManagerHasChangesChangedEventArgs(entityManager);
            entityManager.HasChangesChanged += (s, e) => { lastEventArgs = e; ++eventCount; };

            // Add a new Todo
            var newTodo         = entityManager.CreateEntity<TodoItem>();
            newTodo.Description = "New Todo Item";
            newTodo.CreatedAt = DateTime.Today;
            Assert.AreEqual(1, eventCount, "Only one HasChangedChanged event should be signalled when entity added");
            Assert.IsTrue(lastEventArgs.HasChanges, "HasChanagesChanged should signal true after new entity added");
            eventCount = 0;

            // Discard the added Todo
            try {
                entityManager.RejectChanges();
            }
            catch (Exception e) {
                var message = "RejectChanges() should not fail with the error " + e.Message;
                Assert.Fail(message);
            }

            Assert.AreEqual(1, eventCount, "Only one HasChangedChanged event should be signalled on RejectChanges() call");
            Assert.IsFalse(lastEventArgs.HasChanges, "HasChanagesChanged should signal false after RejectChanges() call");
            Assert.IsFalse(entityManager.HasChanges(), "EntityManager should have no pending changes after RejectChanges() call");
            eventCount = 0;

            // Add another new Todo
            var newTodo2         = entityManager.CreateEntity<TodoItem>();
            newTodo2.Description  = "New Todo Item2";
            // newTodo2.CreatedAt = DateTime.Today;
            Assert.AreEqual(1, eventCount, "Only one HasChangedChanged event should be signalled when entity added");
            Assert.IsTrue(lastEventArgs.HasChanges, "HasChangesChanged should signal true after new entity added");
            eventCount = 0;

            // Save changes
            try { 
                await entityManager.SaveChanges();
            }
            catch (SaveException e) {
                 var message = "SaveChanges() should not fail with the error " + e.Message;
                 e.ValidationErrors.ForEach(ve =>
                 {
                     message += Environment.NewLine + "    " + ve.Message;
                 });
                Assert.Fail(message);
            }

            Assert.AreEqual(1, eventCount, "Only one HasChangedChanged event should be signalled on SaveChanges() call");
            Assert.IsFalse(lastEventArgs.HasChanges, "HasChanagesChanged should signal false after SaveChanges() call");
            Assert.IsFalse(entityManager.HasChanges(), "EntityManager should have no pending changes after SaveChanges() call");
            eventCount = 0;

        }

        [TestMethod]
        public async Task RemovingNavigationProperty() {
          var entityManager = await TestFns.NewEm(_northwindServiceName);

          var employee = new Employee() {
            FirstName = "First",
            LastName = "Employee"
          };
          entityManager.AddEntity(employee);


          var manager = new Employee() {
            FirstName = "First",
            LastName = "Manager"
          };
          entityManager.AddEntity(manager);
          employee.Manager = manager;

          try {
            var saveResult = await entityManager.SaveChanges();

            // Now reverse everything
            manager.EntityAspect.Delete();
            employee.Manager = null;

            employee.EntityAspect.Delete();

            saveResult = await entityManager.SaveChanges();

          } catch (Exception e) {
            var message = string.Format("Save should have succeeded;  Received {0}: {1}",
                                        e.GetType().Name, e.Message);
            Assert.Fail(message);
          }

        }


        #region Queued saves

        //[TestMethod]
        //public async Task QueuedSaves() {
        //    var entityManager = await TestFns.NewEm(_todosServiceName);

        //    /*********************************************************
        //    * QueuedSaves options ensures saves executed sequentially,
        //    * each saving pending changes at the moment it is started
        //    *********************************************************/

        //    //entityManager.EnableSaveQueueing = true;
        //    Task saveTask1 = null;
        //    Task saveTask2 = null;
        //    Task saveTask3 = null;

        //    try {
        //    var todo1 = entityManager.CreateEntity<TodoItem>();
        //    todo1.Description = DateTime.Now.ToString(); ;
        //    saveTask1 = entityManager.SaveChanges();

        //    var todo2 = entityManager.CreateEntity<TodoItem>();
        //    todo2.Description = DateTime.Now.ToString(); ;
        //    saveTask2 = entityManager.SaveChanges();

        //    var todo3 = entityManager.CreateEntity<TodoItem>();
        //    todo3.Description = DateTime.Now.ToString(); ;
        //    saveTask3 = entityManager.SaveChanges();
        //    } catch (Exception e) {
        //        Assert.Fail("With save queueing enabled, concurrent SaveChanges() calls should not fail with message: " + e.Message);
        //    }

        //    await Task.WhenAll(saveTask1, saveTask2, saveTask3);
                   
        //    Assert.IsFalse(entityManager.HasChanges(), "Entity manager should not have pending changes after all saves have completed");
        //}

        #endregion Queued saves

        #region Unmapped Properties

        /*********************************************************
        * can save entity with an unmapped property
        * The unmapped property is sent to the server where it is unknown to the Todo class
        * but the server safely ignores it.
        *********************************************************/

        /*
        test("can save TodoItem defined with an unmapped property", 4, function () {
            var store = cloneTodosMetadataStore();

            var TodoItemCtor = function () {
                this.foo = "Foo"; // unmapped properties
                this.bar = "Bar";
            };

            store.registerEntityTypeCtor('TodoItem', TodoItemCtor);

            var todoType = store.getEntityType('TodoItem');
            var fooProp = todoType.getProperty('foo');
            var barProp = todoType.getProperty('bar');

            // Breeze identified the properties as "unmapped"
            ok(fooProp.isUnmapped,"'foo' should an unmapped property");
            ok(barProp.isUnmapped, "'bar' should an unmapped property");
        
            // EntityManager using the extended metadata
            var em = new breeze.EntityManager({
                serviceName: todosServiceName,
                metadataStore: store
            });

            var todo = em.createEntity('TodoItem', {Description:"Save 'foo'"});

            equal(todo.foo(), "Foo", "unmapped 'foo' property returns expected value");
        
            stop();
            em.saveChanges().then(saveSuccess).fail(saveError).fin(start);
        
            function saveSuccess(saveResult) {
                ok(true, "saved TodoItem which has an unmapped 'foo' property.");
            }
            function saveError(error) {
                var message = error.message;
                ok(false, "Save failed: " + message);
            }

        });

        // Test Helpers
        function void entityManager_HasChangesChanged(object sender, EntityManagerHasChangesChangedEventArgs e)
        {
 	        throw new NotImplementedException();
        } 
         * 
        cloneTodosMetadataStore() {
            var metaExport = newTodosEm.options.metadataStore.exportMetadata();
            return new breeze.MetadataStore().importMetadata(metaExport);
        }
    */

        #endregion Unmapped Properties
    }
}


// Removed tests - may be added back later 
// - Concurrent save restriction not YET implemented in Breeze.Sharp

        //[TestMethod]
        //public async Task ConcurrentSaves() {
        //    var entityManager = await TestFns.NewEm(_todosServiceName);
        //    Task saveTask1 = null;
        //    Task saveTask2 = null;

        //    /*********************************************************
        //    * Concurrent save throws exceptions by default
        //    *********************************************************/
        //    var todo = entityManager.CreateEntity<TodoItem>();
        //    var uniqueDescription1 = DateTime.Now.ToString();
        //    todo.Description = uniqueDescription1;


        //    // Default behavior is to disallow concurrent calls to SaveChanges()

        //    try {
        //        saveTask1 = entityManager.SaveChanges();        // Shold succeed
        //        saveTask2 = entityManager.SaveChanges();        // Should throw
        //        Assert.Fail("By default, SaveChanges() call while another is pending should throw");
        //    }
        //    catch (Exception e) {
        //        var message = "Success: Second SaveChanges() threw exception: " + e.Message;
        //        Assert.Fail(message);
        //    }
        //    await Task.WhenAll(saveTask1, saveTask2);

        //    var todos1 = await new EntityQuery<TodoItem>().Where(td => td.Description == uniqueDescription1).Execute(entityManager);
        //    var count1 = todos1.Count();
        //    Assert.AreEqual(1, count1, "After disallowed concurrent save, there should be only one entity in databse");
        //}


        //[TestMethod]
        //public async Task AllowConcurrentSaves() {
        //    var entityManager = await TestFns.NewEm(_todosServiceName);
        //    Task saveTask1 = null;
        //    Task saveTask2 = null;

        //    /*********************************************************
        //    * Second save w/ 'allowConcurrentSaves'  - saves a new entity twice!
        //    * That is terrible! 
        //    * DON'T USE THIS FEATURE UNLESS YOU KNOW WHY
        //    *********************************************************/

        //    TestFns.RunInWpfSyncContext(async () =>
        //        {
        //            var now = DateTime.Now;
        //            var todo2 = entityManager.CreateEntity<TodoItem>();
        //            todo2.CreatedAt = now;
        //            var uniqueDescription2 = now.ToString();
        //            todo2.Description = uniqueDescription2;

        //            // AllowConcurrentSaves used to be third parameter but it appears that this is the default anyway
        //            var options = new SaveOptions(null, null, /* true, */ null);

        //            try {
        //                saveTask1 = entityManager.SaveChanges(options);        // Should succeed
        //                saveTask2 = entityManager.SaveChanges(options);        // Should succeed
        //            }
        //            catch (Exception e) {
        //                var message = "With allowConcurrentSaves = true, concurrent SaveChanges() calls should not fail with message: " + e.Message;
        //                Assert.Fail(message);
        //            }
        //            await Task.WhenAll(saveTask1, saveTask2);
        //            var todos2 = await new EntityQuery<TodoItem>().Where(td => td.Description == uniqueDescription2).Execute(entityManager);
        //            var count2 = todos2.Count();
        //            Assert.AreEqual(2, count2, "After concurrent save, there should be two entities in database");
        //            Assert.IsTrue(todos2.All(td => td.Description == uniqueDescription2), "After concurrent save, the two saved entities should be identical");
        //        });

        //}


        //[TestMethod]
        //public async Task ConcurrentSavesWithSeparateEMs() {
        //    var entityManager = await TestFns.NewEm(_todosServiceName);
        //    Task saveTask1 = null;
        //    Task saveTask2 = null;

        //    /*********************************************************
        //    * Concurrent save with separate managers is ok
        //    * as if two different users saved concurrently
        //    *********************************************************/
        //    var todo3 = entityManager.CreateEntity<TodoItem>();
        //    var uniqueDescription3 = "T3_" + DateTime.Now.ToString();
        //    todo3.Description = uniqueDescription3;

        //    var entityManager2 = await TestFns.NewEm(_todosServiceName);
        //    var todo4 = entityManager2.CreateEntity<TodoItem>();
        //    var uniqueDescription4 = "T4_" + DateTime.Now.ToString();
        //    todo4.Description = uniqueDescription4;

        //    try {
        //        saveTask1 = entityManager.SaveChanges();        // Shold succeed
        //        saveTask2 = entityManager2.SaveChanges();       // Should succeed
        //    }
        //    catch (Exception e) {
        //        var message = "Concurrent saves to different entity managers should not fail with message:" + e.Message;
        //        Assert.Fail(message);
        //    }

        //    await Task.WhenAll(saveTask1, saveTask2);
        //    var todos3 = await new EntityQuery<TodoItem>().Where(td => td.Description == uniqueDescription3).Execute(entityManager);
        //    var todos4 = await new EntityQuery<TodoItem>().Where(td => td.Description == uniqueDescription4).Execute(entityManager);
        //    Assert.IsTrue(todos3.Count() == 1 && todos4.Count() == 1, "After concurrent save from separate entity managers, both entities should be in database");
        //}