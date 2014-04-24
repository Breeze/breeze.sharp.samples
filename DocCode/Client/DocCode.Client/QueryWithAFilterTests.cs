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
            MetadataStore.Instance.ProbeAssemblies(typeof(TodoItem).Assembly);
            _serviceName = "http://localhost:56337/breeze/Todos/";
        }

        [TestMethod]
        public async Task QueryWithAFilter()
        {
            var manager = new EntityManager(_serviceName);

            // Metadata must be fetched before CreateEntity() can be called
            await manager.FetchMetadata();

            //Snippet1
            var query1 = new EntityQuery<TodoItem>();
            var allTodos = await manager.ExecuteQuery(query1);
            Assert.AreEqual(6, allTodos.Count(), "Got " + allTodos.Count() + " TodoItems");
            
            //Snippet2
            var query2 = query1.Where(td => !td.IsArchived);
            var unarchivedTodos = await manager.ExecuteQuery(query2);
            Assert.AreEqual(3, unarchivedTodos.Count(), "Got " + unarchivedTodos.Count() + " unarchived TodoItems");

            //Snippet3
            var query3 = query1.Where(td => !td.IsArchived && !td.IsDone);
            var activeTodos = await manager.ExecuteQuery(query3);
            Assert.AreEqual(2, activeTodos.Count(), "Got " + activeTodos.Count() + " active TodoItems");

            //Snippet4
            var query4 = query1.Where(td => td.Description.Contains("Wine"));
            var wineTodos = await manager.ExecuteQuery(query4);
            Assert.AreEqual(1, wineTodos.Count(), "Got " + wineTodos.Count() + " wine TodoItems");

            //Snippet5
            var localWineTodos = manager.ExecuteQueryLocally(query4);
            Assert.AreEqual(1, localWineTodos.Count(), "Got " + localWineTodos.Count() + " local wine TodoItems");
        }

    }
}
