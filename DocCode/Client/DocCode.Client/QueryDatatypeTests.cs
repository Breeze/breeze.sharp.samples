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
            Configuration.Instance.ProbeAssemblies(typeof(Role).Assembly);
            _serviceName = "http://localhost:56337/breeze/Northwind/";
            //_serviceName = "http://localhost:7150/breeze/NorthwindIBModel";
        }

        [TestMethod]
        public async Task QueryRoles()
        {
            //Assert.Inconclusive("See comments in the QueryRoles test");

            // Issues:
            //
            // 1.  When the RoleType column was not present in the Role entity in the database (NorthwindIB.sdf), 
            //     the query failed with "Internal Server Error".  A more explanatory message would be nice.  
            //     The RoleType column has been added (nullable int), so this error no longer occurs.
            //
            // 2.  Comment out the RoleType property in the client model (Model.cs in Client\Model_Northwind.Sharp project lines 514-517).
            //     In this case, the client throws a null reference exception in CsdlMetadataProcessor.cs.
            //     This is the FIRST problem reported by the user.  A more informative message would be helpful.
            //
            // Note that this condition causes many other tests to fail as well.
            //
            // 3.  Uncomment the RoleType property in the client model.  Then the client throws in JsonEntityConverter.cs.
            //     This is the SECOND problem reported by the user.  This looks like a genuine bug that should be fixed.
            //

            var manager = new EntityManager(_serviceName);
                // Metadata must be fetched before CreateEntity() can be called
                await manager.FetchMetadata();

                var query = new EntityQuery<Role>();
                var allRoles = await manager.ExecuteQuery(query);

                Assert.IsTrue(allRoles.Any(), "There should be some roles defined");
        }
    }
}
