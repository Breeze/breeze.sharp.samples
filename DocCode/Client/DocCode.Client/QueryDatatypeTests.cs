using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using Breeze.Sharp;
using Breeze.Sharp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Northwind.Models;

namespace Test_NetClient
{
    [TestClass]
    public class QueryDatatypeTests
    {
        private String _serviceName;

        [TestInitialize]
        public void TestInitializeMethod()
        {
            Configuration.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            _serviceName = "http://localhost:56337/breeze/Northwind/";
        }

        [TestMethod]
        public async Task QueryRoles()
        {
            Assert.Inconclusive("Query Roles fails with Internal Server Error");

            var manager = new EntityManager(_serviceName);

            try {
                // Metadata must be fetched before CreateEntity() can be called
                await manager.FetchMetadata();

                var query = new EntityQuery<Role>();
                var allRoles = await manager.ExecuteQuery(query);

                Assert.IsTrue(allRoles.Any(), "There should be some roles defined");
            }
            catch (Exception e) {
                var exceptionType = e.GetType().Name;
                var message = e.Message;
                Assert.Fail(exceptionType + ": " + message);
            }
        }
    }
}
