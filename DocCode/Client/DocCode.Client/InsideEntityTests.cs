using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Breeze.Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Northwind.Models;

namespace Test_NetClient
{
    [TestClass]
    public class InsideEntityTests
    {
        private String _serviceName;

        [TestInitialize]
        public void TestInitializeMethod()
        {
            Configuration.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            _serviceName = "http://localhost:56337/breeze/Northwind/";
        }

        /// <summary>
        /// Once you’ve changed an entity, it stays in a changed state
        /// … even if you manually restore the original values.
        /// </summary>
        [TestMethod]
        public async Task EntityStaysModified()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var customer = manager.GetEntities<Customer>().First();

            var oldCompanyName = customer.CompanyName;  // assume existing "Unchanged" entity
            Assert.AreEqual(EntityState.Unchanged, customer.EntityAspect.EntityState);

            customer.CompanyName = "Something new";     // EntityState becomes "Modified"
            Assert.AreEqual(EntityState.Modified, customer.EntityAspect.EntityState);

            customer.CompanyName = oldCompanyName;      // EntityState is still "Modified
            Assert.AreEqual(EntityState.Modified, customer.EntityAspect.EntityState);
        }

        /// <summary>
        /// Call RejectChanges to cancel pending changes, 
        /// revert properties to their prior values, 
        /// and set the entityState to "Unchanged".
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task CancelWithRejectChanges()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var customer = manager.GetEntities<Customer>().First();

            var oldCompanyName = customer.CompanyName;  // assume existing "Unchanged" entity
            Assert.AreEqual(EntityState.Unchanged, customer.EntityAspect.EntityState);

            customer.CompanyName = "Something new";     // EntityState becomes "Modified"
            Assert.AreEqual(EntityState.Modified, customer.EntityAspect.EntityState);

            customer.EntityAspect.RejectChanges();      // EntityState restored to "Unchanged”
                                                        // customer.CompanyName = oldCompanyName
            Assert.AreEqual(EntityState.Unchanged, customer.EntityAspect.EntityState);
            Assert.AreEqual(oldCompanyName, customer.CompanyName);
        }

        /// <summary>
        /// You can also call rejectChanges on the EntityManager 
        /// to cancel and revert pending changes for every entity in cache.
        /// </summary>
        [TestMethod]
        public async Task RevertAllPendingChangesInCache()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var customer1 = manager.GetEntities<Customer>().First();
            var customer2 = manager.GetEntities<Customer>().Last();

            var oldCompanyName1 = customer1.CompanyName;  // assume existing "Unchanged" entity
            var oldCompanyName2 = customer2.CompanyName;
            Assert.AreEqual(EntityState.Unchanged, customer1.EntityAspect.EntityState);
            Assert.AreEqual(EntityState.Unchanged, customer2.EntityAspect.EntityState);

            customer1.CompanyName = "Something new";     // EntityState becomes "Modified"
            customer2.CompanyName = "Something different";
            Assert.AreEqual(EntityState.Modified, customer1.EntityAspect.EntityState);
            Assert.AreEqual(EntityState.Modified, customer2.EntityAspect.EntityState);

            manager.RejectChanges(); // revert all pending changes in cache
            Assert.AreEqual(EntityState.Unchanged, customer1.EntityAspect.EntityState);
            Assert.AreEqual(oldCompanyName1, customer1.CompanyName);
            Assert.AreEqual(EntityState.Unchanged, customer2.EntityAspect.EntityState);
            Assert.AreEqual(oldCompanyName2, customer2.CompanyName);
        }

        [TestMethod]
        public async Task OriginalValuesMapUpdatesWithPropertyChanges()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var customer = manager.GetEntities<Customer>().First();

            var oldCompanyName = customer.CompanyName;  // assume existing "Unchanged" entity

            // The OriginalValuesMap is an empty object while the entity is in the "Unchanged" state.
            Assert.AreEqual(0, customer.EntityAspect.OriginalValuesMap.Count);

            // When you change an entity property for the first time... 
            customer.CompanyName = "Something new";

            // ...Breeze adds the pre-change value to the OriginalValuesMap... 
            Assert.AreEqual(1, customer.EntityAspect.OriginalValuesMap.Count, "The OriginalValuesMap should have added an entry for the CompanyName change.");
            Assert.AreEqual(oldCompanyName, customer.EntityAspect.OriginalValuesMap.First().Value, "The old company name should have been added to the OriginalValuesMap.");
            // ...using the property name as the key.
            var names = GetOriginalValuesPropertyNames(customer);
            Assert.AreEqual("CompanyName", names.First(), "'CompanyName' should be the key in the OriginalValuesMap.");

            // Breeze replaces EntityAspect.OriginalValuesMap with a new empty hash 
            // when any operation restores the entity to the "Unchanged" state.
            customer.EntityAspect.RejectChanges();
            Assert.AreEqual(0, customer.EntityAspect.OriginalValuesMap.Count, "The OriginalValuesMap should be empty after RejectChanges.");
            Assert.AreEqual(EntityState.Unchanged, customer.EntityAspect.EntityState);
        }

        public IEnumerable<string> GetOriginalValuesPropertyNames(IEntity entity)
        {
            var names = new List<string>();
            foreach (var name in entity.EntityAspect.OriginalValuesMap)
            {
                names.Add(name.Key);
            }
            return names;
        }

        [TestMethod]
        public async Task ChangingPropertyRaisesPropertyChanged()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var customer = manager.GetEntities<Customer>().First();

            // get ready for propertyChanged event after property change
            var aspectPropertyChangedEventCount = 0;
            customer.EntityAspect.PropertyChanged += (sender, args) => ++aspectPropertyChangedEventCount;

            // make a change
            customer.CompanyName = "Something new";

            Assert.IsTrue(aspectPropertyChangedEventCount > 0, "The PropertyChanged event should have fired after changing the CompanyName and updated the event counter.");
        }

        [TestMethod]
        public async Task CanGetEntityMetadataFromEntityType()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);

            var customerType = manager.MetadataStore.GetEntityType(typeof(Customer));
            var customer = customerType.CreateEntity();
            // customer.EntityAspect.EntityType == customerType
            Assert.AreEqual(customer.EntityAspect.EntityType, customerType, "an entity's entityType should be the same type that created it");

            customer.EntityAspect.GetValue("CompanyName");
        }

        [TestMethod]
        public async Task GetSetValue()
        {
            var manager = new EntityManager(_serviceName);
            await PrimeCache(manager);
            var customer = manager.GetEntities<Customer>().First();

            var setName = "Ima Something Corp";
            customer.EntityAspect.SetValue("CompanyName", setName);
            var getName = customer.EntityAspect.GetValue("CompanyName");
            // getName == setName
            Assert.AreEqual(setName, getName);
 
        }

        private async Task PrimeCache(EntityManager manager)
        {
            var q = new EntityQuery<Customer>().Take(5);
            await q.Execute(manager);
        }

    }
}
