using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

using Breeze.Sharp.Core;
using Breeze.Sharp;

// Namespace must be unique to prevent TodoItem type from being ambiguous in other test classes
namespace Test_NetClient_Misc
{
    // Use local partial version of TodoItem
    public class TodoItem : Breeze.Sharp.BaseEntity
    {
        public int Id {
            get { return GetValue<int>(); }
            set { SetValue(value); }
        }

        public string Description {
            get { return GetValue<string>(); }
            set { SetValue(value); }
        }
    }

    [TestClass]
    public class MiscTests
    {
        [TestInitialize]
        public void TestInitializeMethod() {
        }

        [TestCleanup]
        public void TearDown() {
        }

        [TestMethod]
        public async Task NamespaceMismatch() {

            // Allow use of a partial model
            MetadataStore.Instance.AllowedMetadataMismatchTypes = MetadataMismatchType.AllAllowable;

            // This fixes the problem
            //MetadataStore.Instance.NamingConvention.AddClientServerNamespaceMapping("Test_NetClient_Misc", "Todo.Models");

            MetadataStore.Instance.ProbeAssemblies(typeof(TodoItem).Assembly);

            // Create EntityManager
            var publicServiceAddress = "http://sampleservice.breezejs.com/api/todos/";
            var localServiceAddress = "http://localhost:56337/breeze/Todos/";
            var entityManager = new EntityManager(localServiceAddress);

            // Query all TodoItems
            try {
            var result = await new EntityQuery<TodoItem>().Execute(entityManager);
            var itemCount = result.Count();
            Assert.AreEqual(6, itemCount, "Should retrieve 6 TodoItems");
            }
            catch (Exception e) {
                Assert.Fail("Server threw exception with HTML in message: " + e.Message);
            }

        }
    }
}


