using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using Breeze.Sharp;
using Breeze.Sharp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Todo.Models;

namespace Test_NetClient
{
    [TestClass]
    public class QueryWithAFilterTests
    {
        private String _serviceName;

        [TestInitialize]
        public void TestInitializeMethod()
        {
            Configuration.Instance.ProbeAssemblies(typeof(TodoItem).Assembly);
            _serviceName = "http://localhost:56337/breeze/Todos/";
        }

        [TestMethod]
        public async Task QueryWithAFilter()
        {
            var manager = new EntityManager(_serviceName);

            // Metadata must be fetched before CreateEntity() can be called
            await manager.FetchMetadata();

            // Ensure there is at least one active todo item named Wine in the database
            var wines = await new EntityQuery<TodoItem>().Where(td => td.Description == "Wine" && !td.IsArchived && !td.IsDone).Execute(manager);
            if (!wines.Any()) {
                var newWine = manager.CreateEntity<TodoItem>(new {Description = "Wine"});
                await manager.SaveChanges();
            }
            manager.Clear();

            //Snippet1
            var query1 = new EntityQuery<TodoItem>();
            var allTodos = await manager.ExecuteQuery(query1);
            Assert.IsTrue(allTodos.Any(), "No TodoItems in the database");
            
            //Snippet2
            var query2 = query1.Where(td => !td.IsArchived);
            var unarchivedTodos = await manager.ExecuteQuery(query2);
            Assert.IsTrue(unarchivedTodos.Any(), "No unarchived TodoItems in the database");

            //Snippet3
            var query3 = query1.Where(td => !td.IsArchived && !td.IsDone);
            var activeTodos = await manager.ExecuteQuery(query3);
            Assert.IsTrue(activeTodos.Any(), "No active TodoItems in the database");

            //Snippet4
            var query4 = query1.Where(td => td.Description.Contains("Wine"));
            var wineTodos = await manager.ExecuteQuery(query4);
            Assert.IsTrue(allTodos.Any(), "No Wine TodoItems in the database");

            //Snippet5
            // Execute above query locally
            var localWineTodos = manager.ExecuteQueryLocally(query4);
            Assert.AreEqual(wineTodos.Count(), localWineTodos.Count(), "Got " + localWineTodos.Count() + " local wine TodoItems");
        }
    }
}
