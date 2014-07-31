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
    public class ConcurrencyTests
    {
        private String _northwindServiceName;
        private readonly Guid _alfredsID = Guid.Parse("785efa04-cbf2-4dd7-a7de-083ee17b6ad2");

        [TestInitialize]
        public void TestInitializeMethod() {
            _northwindServiceName = "http://localhost:56337/breeze/Northwind/";
        }

        [TestCleanup]
        public void TearDown() {
        }

        [TestMethod]
        public async Task SimpleConcurrencyFault() {

            Configuration.Instance.ProbeAssemblies(typeof(Customer).Assembly);

            // Query Alfred
            var entityManager1 = new EntityManager(_northwindServiceName);
            var query = new EntityQuery<Customer>().Where(c => c.CustomerID == _alfredsID);
            var alfred1 = (await entityManager1.ExecuteQuery(query)).FirstOrDefault();

            // Query a second copy of Alfred in a second entity manager
            var entityManager2 = new EntityManager(_northwindServiceName);
            var alfred2 = (await entityManager2.ExecuteQuery(query)).FirstOrDefault();

            // Modify and save the first Alfred
            alfred1.ContactTitle += "X";

            // Currently, this throws due to "changes to an original record may not be saved"
            // ...whatever that means
            var saveResult1 = await entityManager1.SaveChanges();

            // Attempt to modify and save the second Alfred
            alfred2.ContactName += "Y";
            try {
                var saveResult2 = await entityManager2.SaveChanges();
            }
            catch (SaveException e) {
                var message = e.Message;
            }
        }
    }
}


