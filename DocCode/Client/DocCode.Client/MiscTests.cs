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
        private String _todosServiceName;

        [TestInitialize]
        public void TestInitializeMethod() {
            _todosServiceName = "http://localhost:56337/breeze/Todos/";
        }

        [TestCleanup]
        public void TearDown() {
        }

        [TestMethod]
        public async Task NamespaceMismatch() {

            // Allow use of a partial model
            MetadataStore.Instance.AllowedMetadataMismatchTypes = MetadataMismatchType.AllAllowable;
            MetadataStore.Instance.ProbeAssemblies(typeof(TodoItem).Assembly);

            var entityManager = new EntityManager(_todosServiceName);

            // Query all TodoItems - Default resource is "TodoItems" which is und
            try {
                await new EntityQuery<TodoItem>().Execute(entityManager);
                Assert.Fail("Server should throw exception when undefined resource is queried");
            }
            catch (Exception e) {
                Assert.IsFalse(e.Message.Contains("<!DOCTYPE html"), "Exception for undefined resource should be explanatory");
            }
            finally {
                MetadataStore.__Reset();
                Assert.IsFalse(MetadataStore.Instance.EntityTypes.Any(), "After hard reset, there should not be any entity types in the MetadataStore");

            }

        }

        [TestMethod]
        public async Task NamingConvention() {

            // Allow use of a partial model
            MetadataStore.Instance.AllowedMetadataMismatchTypes = MetadataMismatchType.AllAllowable;

            // Inform MetadataStore that our local TodoItem is in a different namespace from server
            MetadataStore.Instance.NamingConvention.AddClientServerNamespaceMapping("Test_NetClient_Misc", "Todo.Models");

            MetadataStore.Instance.ProbeAssemblies(typeof(TodoItem).Assembly);

            var entityManager = new EntityManager(_todosServiceName);

            // Query all TodoItems - Resource will be "Todos" as downloaded with the metadata
            try {
                await new EntityQuery<TodoItem>().Execute(entityManager);
            }
            catch (Exception e) {
                Assert.Fail("Use of NamingConvention should allow client and server entity models in different namespaces");
            }
            finally {
                MetadataStore.__Reset();
                Assert.IsFalse(MetadataStore.Instance.EntityTypes.Any(), "After hard reset, there should not be any entity types in the MetadataStore");
            }

        }
    }
}


