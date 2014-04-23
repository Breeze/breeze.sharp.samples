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

        private async Task PrimeCache(EntityManager manager)
        {
            var q = new EntityQuery<Customer>().Take(5);
            await q.Execute(manager);
        }
    }
}
