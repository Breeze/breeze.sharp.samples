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
    public class InterceptionTests
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

        /// <summary>
        /// These tests demonstrate interception of entity save requests on the server side.
        /// On the server, the TodosRepository attaches methods to the data context that modify
        /// the Description fieldsof the entities being saved before and after the save operation.
        /// 
        /// The modified Description field is automatically merged back into the entity on the client side,
        /// allowing it to be tested.
        /// 
        /// See TodosRepository.cs in the DocCode.DataAccess.EF project server-side project.
        /// 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InterceptAndSaveTest() {

            var entityManager = await Test_NetClient.TestFns.NewEm(_todosServiceName);

            var todo = entityManager.CreateEntity<TodoItem>();
            todo.Initialize();
            todo.Description = "INTERCEPT SAVE";

            var saveResult = await entityManager.SaveChanges();
            Assert.IsTrue(todo.Description.Contains("SAVED"), "Saved Todo should contain the string 'SAVED'");

            // The BeforeSaveEntities delegate adds the number of items to be saved to the Description field
            Assert.IsTrue(todo.Description.Contains("(1)"));

            // The AfterSaveEntities delegate add the number of saved items and key mappings to the description field
            Assert.IsTrue(todo.Description.Contains("(1, 1)"));
        }

        /// <summary>
        /// Test the ability of the interceptor to exclude an entity from the save
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InterceptAndExcludeTest() {

            var entityManager = await Test_NetClient.TestFns.NewEm(_todosServiceName);

            var todo = entityManager.CreateEntity<TodoItem>();
            todo.Initialize();
            todo.Description = "INTERCEPT EXCLUDE";

            var saveResult = await entityManager.SaveChanges();

            var description = todo.Description;
            Assert.IsFalse(todo.Description.Contains("SAVED"), "Excluded description should not contain 'SAVED'");
            Assert.IsTrue(todo.Description.Contains("INTERCEPT EXCLUDE"), "Excluded description should be unmodified");
            Assert.IsTrue(todo.Id < 0, "Excluded entity should still have temporary key value");
        }

        /// <summary>
        /// Test the ability of the interceptor to abort the save operation 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InterceptAndFailTest() {

            var entityManager = await Test_NetClient.TestFns.NewEm(_todosServiceName);

            var todo = entityManager.CreateEntity<TodoItem>();
            todo.Initialize();
            todo.Description = "INTERCEPT FAIL";

            try {
                var saveResult = await entityManager.SaveChanges();
                Assert.Fail("Saving of Todo with INTERCEPT FAIL in description should throw an exception");
            }
            catch (Breeze.Sharp.SaveException e) {
                Assert.IsTrue(e.Message.Contains("INTERCEPT FAIL"));
            }
        }

    }
}


