using System;
using System.IO;
using System.Collections.Generic;
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
    public class LookupListsTests
    {
        private String _serviceName;

        [TestInitialize]
        public void TestInitializeMethod()
        {
        }

        [TestMethod]
        public async Task LookupLists1() {
            // Snippet1
            _serviceName = "http://localhost:56337/breeze/Northwind/";
            MetadataStore.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            var entityManager = await TestFns.NewEm(_serviceName);

            // Snippet2
            // Query from special purpose server controller method
            // See Lookups() method in NorthwindController.cs in DocCode.Server project
            var query = EntityQuery.From("Lookups", new
                                                        {
                                                            regions = Enumerable.Empty<Region>(),
                                                            territories = Enumerable.Empty<Territory>(),
                                                            categories = Enumerable.Empty<Category>(),
                                                        });

            // Snippet3
            var data = await query.Execute(entityManager);
            Assert.IsTrue(data.Count() == 1, "Lookups query should return single item");

            Assert.IsTrue(entityManager.GetEntities<Region>().Any(), "Regions returned by Lookups should be in cache");
            Assert.IsTrue(entityManager.GetEntities<Territory>().Any(), "Territories returned by Lookups should be in cache");
            Assert.IsTrue(entityManager.GetEntities<Category>().Any(), "Categories returned by Lookups should be in cache");

            var lookups         = data.First();
            var regions         = lookups.GetPropValue<IEnumerable<Region>>("regions");
            var territories     = lookups.GetPropValue<IEnumerable<Territory>>("territories");
            var categories      = lookups.GetPropValue<IEnumerable<Category>>("categories");

            Assert.IsTrue(regions.Any(), "Lookups query should return regions");
            Assert.IsTrue(territories.Any(), "Lookups query should return territories");
            Assert.IsTrue(categories.Any(), "Lookups query should return categories");
            Assert.AreEqual(categories.First().EntityAspect.EntityState, EntityState.Unchanged, "State of first category in cache should be unchanged");

        }

        [TestMethod]
        public async Task LookupLists2() {
            _serviceName = "http://localhost:56337/breeze/Northwind/";
            MetadataStore.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            var entityManager = await TestFns.NewEm(_serviceName);

            // Snippet4
            // Query from special purpose server controller method
            // See Lookups() method in NorthwindController.cs in DocCode.Server project
            var query = EntityQuery.From("Lookups", new
                                                        {
                                                            regions = new List<Region>(),
                                                            territories = new List<Territory>(),
                                                            categories = new List<Category>()
                                                        });

            var data = await query.Execute(entityManager);
            Assert.IsTrue(data.Count() == 1, "Lookups query should return single item");
            Assert.IsTrue(entityManager.GetEntities<Region>().Any(), "Regions returned by Lookups should be in cache");
            Assert.IsTrue(entityManager.GetEntities<Territory>().Any(), "Territories returned by Lookups should be in cache");
            Assert.IsTrue(entityManager.GetEntities<Category>().Any(), "Categories returned by Lookups should be in cache");

            var lookups        = data.First();
            var regions        = lookups.GetPropValue<IEnumerable<Region>>("regions");
            var territories    = lookups.GetPropValue<IEnumerable<Territory>>("territories");
            var categories     = lookups.GetPropValue<IEnumerable<Category>>("categories");

            Assert.IsTrue(regions.Any(), "Lookups query should return regions");
            Assert.IsTrue(territories.Any(), "Lookups query should return territories");
            Assert.IsTrue(categories.Any(), "Lookups query should return categories");
            Assert.AreEqual(categories.First().EntityAspect.EntityState, EntityState.Unchanged, "State of first category in cache should be unchanged");

        }

        [TestMethod]
        public async Task LookupLists3() {
            _serviceName = "http://localhost:56337/breeze/Northwind/";
            MetadataStore.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            var entityManager = await TestFns.NewEm(_serviceName);


            // Snippet5
            // Query from special purpose server controller method
            // See LookupsArray() method in NorthwindController.cs in DocCode.Server project
            var query          = EntityQuery.From("LookupsArray", Enumerable.Empty<BaseEntity>());

            // Snippet6
            var data           = await query.Execute(entityManager);
            Assert.IsTrue(data.Count() > 0, "LookupsArray query should return lists of entities");
            Assert.IsTrue(entityManager.GetEntities<Region>().Any(), "Regions returned by LookupsArray should be in cache");
            Assert.IsTrue(entityManager.GetEntities<Territory>().Any(), "Territories returned by LookupsArray should be in cache");
            Assert.IsTrue(entityManager.GetEntities<Category>().Any(), "Categories returned by LookupsArray should be in cache");

            var dataArray       = data.ToArray();
            var regions        = dataArray[0].OfType<Region>();
            var territories    = dataArray[1].OfType<Territory>();
            var categories     = dataArray[2].OfType<Category>();

            Assert.IsTrue(regions.Any(), "LookupsArray query should return regions");
            Assert.IsTrue(territories.Any(), "LookupsArray query should return territories");
            Assert.IsTrue(categories.Any(), "LookupsArray query should return categories");
            Assert.AreEqual(categories.First().EntityAspect.EntityState, EntityState.Unchanged, "State of first category in cache should be unchanged");

            var theRegions      = entityManager.GetEntities<Region>();
            Assert.AreEqual(theRegions.Count(), regions.Count(), "All regions returned by LookupsArray should be in cache");
        }

        // Snippet7
        private EntityManager manager;
	    public IEnumerable<Category> Categories {
		    get { 
                if (_categories == null) {
                    _categories = manager.GetEntities<Category>();  // Executes locally
                }
                return _categories;
            }
	    }
	    private IEnumerable<Category> _categories = null;

    }
}
