using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using Breeze.Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Northwind.Models;

namespace Test_NetClient
{
    [TestClass]
    public class ExportImportTests
    {
        private String _serviceName;

        [TestInitialize]
        public void TestInitializeMethod()
        {
            MetadataStore.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            _serviceName = "http://localhost:56337/breeze/Northwind/";
        }

        [TestMethod]
        public async Task ExportEntitiesToFile()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);

            var exportData = manager.ExportEntities();

            var mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            File.WriteAllText(mydocpath + @"\ExportEntities.txt", exportData);

            Assert.IsTrue(File.Exists(mydocpath + @"\ExportEntities.txt"), "ExportEntities.txt should have been created");
        }

        [TestMethod]
        public async Task ExportEntitiesToFileWithoutMetadata()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var entitiesToExport = manager.GetEntities<Customer>();

            var exportData = manager.ExportEntities(entitiesToExport, false);

            var mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            File.WriteAllText(mydocpath + @"\ExportEntitiesWithoutMetadata.txt", exportData);

            Assert.IsTrue(File.Exists(mydocpath + @"\ExportEntitiesWithoutMetadata.txt"), "ExportEntitiesWithoutMetadata.txt should have been created");
        }

        [TestMethod]
        public async Task ExportEntitiesToIsolatedStorage()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);

            var exportData = manager.ExportEntities();

            var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            using (var isoStream = new IsolatedStorageFileStream("ExportEntities.txt", FileMode.OpenOrCreate, isoStore))
            {
                using (var writer = new StreamWriter(isoStream))
                {
                    writer.Write(exportData);
                }
            }

            Assert.IsTrue(isoStore.FileExists("ExportEntities.txt"), "ExportEntities.txt should exist in Isolated Storage");
        }

        [TestMethod]
        public async Task ExportImportFromIsolatedStorage()
        {
            var manager1 = new EntityManager(_serviceName);
            await PrimeCache(manager1);
            var expectedEntityCount = manager1.GetEntities().Count();

            var exportData = manager1.ExportEntities();

            var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            string importData;
            using (var isoStream = new IsolatedStorageFileStream("ExportEntities.txt", FileMode.Create, isoStore))
            {
                using (var writer = new StreamWriter(isoStream))
                {
                    writer.Write(exportData);
                }
            }

            using (var isoStream = new IsolatedStorageFileStream("ExportEntities.txt", FileMode.Open, isoStore))
            {
                using (var reader = new StreamReader(isoStream))
                {
                    importData = reader.ReadToEnd();
                }
            }
            
            // import into a new EntityManager
            var manager2 = new EntityManager(_serviceName);
            manager2.ImportEntities(importData);

            Assert.AreEqual(expectedEntityCount, manager2.GetEntities().Count());
        }

        [TestMethod]
        public async Task ExportImportEntities()
        {
            var manager1 = new EntityManager(_serviceName);
            await PrimeCache(manager1);
            var expectedEntityCount = manager1.GetEntities().Count();

            var exportData = manager1.ExportEntities();

            // import into a new EntityManager
            var manager2 = new EntityManager(_serviceName);
            manager2.ImportEntities(exportData);

            Assert.AreEqual(expectedEntityCount, manager2.GetEntities().Count());
        }

        [TestMethod]
        public async Task ExportImportEntitiesPreserveChanges()
        {
            var manager1 = new EntityManager(_serviceName);
            await PrimeCache(manager1);

            // modify all the customers in manager1
            var parisCustomers = manager1.GetEntities<Customer>().ToList();
            parisCustomers.ForEach(c => c.City = "Paris");

            var exportData = manager1.ExportEntities();

            var manager2 = new EntityManager(_serviceName);
            await PrimeCache(manager2);

            // modify all the "same" customers in manager2 
            var londonCustomers = manager2.GetEntities<Customer>().ToList();
            londonCustomers.ForEach(c => c.City = "London");

            // changes in manager2 are preserved by default
            var importResult = manager2.ImportEntities(exportData);

            // so nothing should have been imported
            Assert.AreEqual(0, importResult.ImportedEntities.Count);
            Assert.AreEqual(londonCustomers.Count, manager2.GetEntities().Count());
            // all the customers should still be in "London"
            Assert.IsTrue(manager2.GetEntities<Customer>().All(c => c.City == "London"), "All cities should = London");
        }

        [TestMethod]
        public async Task ExportImportEntitiesOverwriteChanges()
        {
            var manager1 = new EntityManager(_serviceName);
            await PrimeCache(manager1);

            // modify all the customers in manager1
            var parisCustomers = manager1.GetEntities<Customer>().ToList();
            parisCustomers.ForEach(c => c.City = "Paris");

            var exportData = manager1.ExportEntities();

            var manager2 = new EntityManager(_serviceName);
            await PrimeCache(manager2);

            // modify all the "same" customers in manager2 
            var londonCustomers = manager2.GetEntities<Customer>().ToList();
            londonCustomers.ForEach(c => c.City = "London");

            // overwrite changes in manager2 with the exported data
            manager2.ImportEntities(exportData, new ImportOptions(MergeStrategy.OverwriteChanges));
            // all the customers should now be in "Paris"
            Assert.IsTrue(manager2.GetEntities<Customer>().All(c => c.City == "Paris"), "All cities should = Paris");
        }

        [TestMethod]
        public async Task ExportImportSelectedEntities()
        {
            var manager = new EntityManager(_serviceName);
            var manager2 = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var customers = manager.GetEntities<Customer>().ToList();

            var exportData1 = manager.ExportEntities(new IEntity[] {customers[0]}); // array with 1 customer
            var importData1 = manager2.ImportEntities(exportData1);
            Assert.AreEqual(1, importData1.ImportedEntities.Count);
            manager2.Clear();

            var exportData2 = manager.ExportEntities(new IEntity[] {customers[0], customers[1]}); // array of 2 customers
            var importData2 = manager2.ImportEntities(exportData2);
            Assert.AreEqual(2, importData2.ImportedEntities.Count);
            manager2.Clear();

            Assert.AreEqual(0, manager.GetChanges().Count());
            customers.First().City = "Paris";
            var exportData3 = manager.ExportEntities(manager.GetChanges()); // all pending changes
            var importData3 = manager2.ImportEntities(exportData3);
            Assert.AreEqual(1, importData3.ImportedEntities.Count);
            manager2.Clear();

            var selectedCustomerQuery = new EntityQuery<Customer>().Where(customer => customer.City.StartsWith("P"));
            var selectedCustomers = manager.ExecuteQueryLocally(selectedCustomerQuery); // cache-only query returns synchronously
            var exportData4 = manager.ExportEntities(selectedCustomers); // the 'P' customers 
            var importData4 = manager2.ImportEntities(exportData4);
            Assert.AreEqual(1, importData4.ImportedEntities.Count);
        }

        [TestMethod]
        public async Task ExportImportEntitiesWithoutMetadata()
        {
            var manager1 = new EntityManager(_serviceName);
            await PrimeCache(manager1);
            var selectedEntities = manager1.GetEntities<Customer>().ToList();

            // export the selected entities without metadata
            var exportData = manager1.ExportEntities(selectedEntities, false);

            //TODO: What is the equivalent in breeze.sharp?
            // a virginal manager would throw exception on import
            // because it lacks the metadata
            // var em2 = new EntityManager(); 
            
            // creates a new EntityManager with the same configuration as another EntityManager but without any entities
            var manager2 = new EntityManager(manager1);
            var importResult = manager2.ImportEntities(exportData);

            Assert.AreEqual(selectedEntities.Count(), importResult.ImportedEntities.Count);
        }

        [TestMethod]
        public async Task TemporaryKeyNotPreservedOnImport()
        {
            var manager1 = new EntityManager(_serviceName);
            await manager1.FetchMetadata(); // Metadata must be fetched before CreateEntity() can be called

            // Create a new Order. The Order key is store-generated.
            // Until saved, the new Order has a temporary key such as '-1'.
            var acme1 = manager1.CreateEntity<Order>(new {ShipName = "Acme"});
 
            // export without metadata
            var exported = manager1.ExportEntities(new IEntity[] {acme1}, false);
 
            // ... much time passes 
            // ... the client app is re-launched
            // ... the seed for the temporary id generator was reset
            SimulateResetTempKeyGeneratorSeed();

            // Create a new manager2 with metadata
            var manager2 = new EntityManager(manager1);

            // Add a new order to manager2
            // This new order has a temporary key.
            // That key could be '-1' ... the same key as acme1!!!
            var beta = (Order) manager2.CreateEntity(typeof (Order), new {ShipName = "Beta"});

            // Its key will be '-1' ... the same key as acme1!!!
            Assert.AreEqual(-1, beta.OrderID);
 
            // Import the the exported acme1 from manager1
            // and get the newly merged instance from manager2
            var imported = manager2.ImportEntities(exported);
            var acme2 = imported.ImportedEntities.Cast<Order>().First();
 
            // compare the "same" order as it is in managers #1 and #2  
            var isSameName = acme1.ShipName == acme2.ShipName; // true
            Assert.IsTrue(isSameName, "ShipNames should be the same");

            // breeze had to update the acme key in manager2 because 'beta' already has ID==-1   
            var isSameId = acme1.OrderID == acme2.OrderID; // false; temporary keys are different
            Assert.IsFalse(isSameId, "OrderIDs (temporary keys) should be different");

        }
        
        private void SimulateResetTempKeyGeneratorSeed()
        {
            // A relaunch of the client would reset the temporary key generator
            // Simulate that for test purposes ONLY with an internal seed reset 
            // that no one should know about or ever use.
            // SHHHHHHHH!
            // NEVER DO THIS IN YOUR PRODUCTION CODE
            Breeze.Sharp.DataType.NextNumber = (long) (-1);
        }

        [TestMethod]
        public void ValidateExportedEntitiesUponImport()
        {
            Assert.Inconclusive("Feature: Requires ImportEntities(string exportedString, object config)");
        }

        [TestMethod]
        public async Task ImportChangedEntityAndRestoreItsOriginalState()
        {
            var manager = new EntityManager(_serviceName);
            await manager.FetchMetadata(); // Metadata must be fetched before CreateEntity() can be called

            // Suppose we are editing a customer
            const string originalCustomerName = "Foo";
            var customer =
                manager.CreateEntity<Customer>(new {CustomerID = Guid.NewGuid(), CompanyName = originalCustomerName},
                                               EntityState.Unchanged);

            // We change his CompanyName
            customer.CompanyName = "Bar";

            // We export and stash these changes offline
            // because we are not ready to save them
            // (in the test we just export)
            var exportData = manager.ExportEntities();

            // We re-run the app later with a clean manager
            manager.Clear();

            var imported = manager.ImportEntities(exportData);
            customer = imported.ImportedEntities.Cast<Customer>().First();

            // We want to revert our changes and restore the customer to its original state
            customer.EntityAspect.RejectChanges();

            Assert.AreEqual(originalCustomerName, customer.CompanyName);
        }

        [TestMethod]
        public async Task ExportImportUnchangedEntities()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);

            // modify one of the entities
            var customer = manager.GetEntities<Customer>().First();
            customer.City = "Paris";
            var expectedUnchangedEntityCount = manager.GetEntities().Count() - 1;

            // export only Unchanged entities
            var exportData = manager.ExportEntities(manager.GetEntities(EntityState.Unchanged));

            // import into a new EntityManager
            var manager2 = new EntityManager(_serviceName);
            manager2.ImportEntities(exportData);

            Assert.AreEqual(expectedUnchangedEntityCount, manager2.GetEntities().Count());
        }

        private async Task PrimeCache(EntityManager manager)
        {
            var q = new EntityQuery<Customer>().Take(5);
            await q.Execute(manager);
        }


    }
}
