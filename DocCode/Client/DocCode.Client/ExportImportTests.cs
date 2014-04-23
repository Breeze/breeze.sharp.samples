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
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var expectedEntityCount = manager.GetEntities().Count();

            var exportData = manager.ExportEntities();

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

            var manager2 = new EntityManager(_serviceName);
            var importResult = manager2.ImportEntities(importData);

            Assert.AreEqual(expectedEntityCount, importResult.ImportedEntities.Count);
        }

        [TestMethod]
        public async Task ExportImportEntities()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var expectedEntityCount = manager.GetEntities().Count();

            var exportData = manager.ExportEntities();

            var manager2 = new EntityManager(_serviceName);
            var importResult = manager2.ImportEntities(exportData);

            Assert.AreEqual(expectedEntityCount, importResult.ImportedEntities.Count);
        }

        [TestMethod]
        public async Task ExportImportEntitiesPreserveChanges()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var parisCustomers = manager.GetEntities<Customer>();
            parisCustomers.ForEach(c => c.City = "Paris");

            var exportData = manager.ExportEntities();

            var manager2 = new EntityManager(manager);
            await PrimeCache(manager2);
            var londonCustomers = manager2.GetEntities<Customer>();
            londonCustomers.ForEach(c => c.City = "London");

            // Changes are preserved by default, so nothing should have been imported
            var importResult = manager2.ImportEntities(exportData);
            Assert.AreEqual(0, importResult.ImportedEntities.Count);
        }

        [TestMethod]
        public async Task ExportImportEntitiesOverwriteChanges()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var parisCustomers = manager.GetEntities<Customer>();
            parisCustomers.ForEach(c => c.City = "Paris");

            var exportData = manager.ExportEntities();

            var manager2 = new EntityManager(manager);
            await PrimeCache(manager2);
            var londonCustomers = manager2.GetEntities<Customer>();
            londonCustomers.ForEach(c => c.City = "London");

            // Overwrite changes with the exported data
            var importResult = manager2.ImportEntities(exportData, new ImportOptions(MergeStrategy.OverwriteChanges));
            Assert.AreEqual(5, importResult.ImportedEntities.Count);

            var importedCustomers = importResult.ImportedEntities;
            importedCustomers.Cast<Customer>().ForEach(c => Assert.AreEqual("Paris", c.City));
        }

        [TestMethod]
        public async Task ExportImportSelectedEntities()
        {
            var manager = new EntityManager(_serviceName);
            var manager2 = new EntityManager(manager);
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

            // creates a new EntityManager with the same configuration as another EntityManager but without any entities
            var manager2 = new EntityManager(manager1);
            var importResult = manager2.ImportEntities(exportData);

            Assert.AreEqual(selectedEntities.Count(), importResult.ImportedEntities.Count);
        }


        private async Task PrimeCache(EntityManager manager)
        {
            var q = new EntityQuery<Customer>().Take(5);
            await q.Execute(manager);
        }
    }
}
