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
        /// This test demonstrates interception of entity save requests on the server side.
        /// On the server, the TodosRepository attaches a method to the data context as the BeforeEntitySaveDelegate:
        /// <code>
        ///     public TodosRepository() {
        ///         _contextProvider.BeforeSaveEntityDelegate = BeforeSaveEntity;
        ///     }
        /// </code>
        /// The delegate function modifies the description of the entity being saved before saving it
        /// and returning it to the client:
        /// <code>
        ///     private bool BeforeSaveEntity(EntityInfo arg) {
        ///         var todo = arg.Entity as TodoItem;
        ///         if (todo != null && todo.Description.Contains("INTERCEPT")) {
        ///             todo.Description += " SAVED!";
        ///         }
        ///         return true;
        ///     }
        /// </code>
        /// The modification is automatically merged back into the entity on the client side.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BeforeSaveEntityTest() {

            var entityManager = await Test_NetClient.TestFns.NewEm(_todosServiceName);
            var todo = entityManager.CreateEntity<TodoItem>();
            todo.Initialize();
            todo.Description = "INTERCEPT";
            var saveResult = await entityManager.SaveChanges();
            var description = todo.Description;
            Assert.IsTrue(description.Contains("SAVED!"), "Saved Todo should contain the string 'SAVED!'");
        }
    }
}


