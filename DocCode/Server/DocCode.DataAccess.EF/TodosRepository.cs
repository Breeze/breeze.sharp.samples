using System;
using System.Collections.Generic;
using System.Linq;

using Breeze.ContextProvider;
using Breeze.ContextProvider.EF6;
using Newtonsoft.Json.Linq;
using Todo.Models;

namespace DocCode.DataAccess
{
    /// <summary>
    /// Repository (a "Unit of Work" really) of Todos models.
    /// </summary>
    public class TodosRepository
    {
        private readonly EFContextProvider<TodosContext> _contextProvider = new EFContextProvider<TodosContext>();

        private TodosContext Context { get { return _contextProvider.Context; } }

        public TodosRepository() {
            _contextProvider.BeforeSaveEntityDelegate = BeforeSaveEntity;
            _contextProvider.BeforeSaveEntitiesDelegate = BeforeSaveEntities;
            _contextProvider.AfterSaveEntitiesDelegate = AfterSaveEntities;
        }

        public string Metadata
        {
            get { return _contextProvider.Metadata(); }
        }

        public SaveResult SaveChanges(JObject saveBundle)
        {
            return _contextProvider.SaveChanges(saveBundle);
        }

        public IQueryable<TodoItem> Todos
        {
            get { return _contextProvider.Context.Todos; }
        }

        #region Purge/Reset

        public string Purge()
        {
            TodosDbInitializer.PurgeDatabase(Context);
            return "purged";
        }

        public string Reset()
        {
            Purge();
            TodosDbInitializer.ResetDatabase(Context);
            return "reset";
        }

        #endregion

        #region Save Interception

        /// <summary>
        /// Demonstration interceptor - called once for each entity to be saved
        /// </summary>
        /// <returns>
        /// True if can save this entity else throw exception
        /// </returns>
        /// <exception cref="System.InvalidOperationException" />
        private bool BeforeSaveEntity(EntityInfo arg) {
            var todo = arg.Entity as TodoItem;
            if (todo != null && todo.Description.Contains("INTERCEPT")) {
                if (todo.Description.Contains("SAVE")) {
                    todo.Description = "INTERCEPT SAVED";
                    return true;
                }
                if (todo.Description.Contains("EXCLUDE")) {
                    todo.Description = "INTERCEPT EXCLUDED";
                    return false;
                }
                if (todo.Description.Contains("FAIL")) {
                    throw new InvalidOperationException("Failed due to description contains INTERCEPT FAIL");
                }
            }
            return true;
        }

        /// <summary>
        /// Demonstration pre-save interceptor called once per save request
        /// </summary>
        /// <param name="entitiesToBeSaved">
        /// Dictionary (indexed by entity type) of entities to be saved
        /// </param>
        /// <returns>
        /// A (possibly modified) dictionary of entities to be saved
        /// </returns>
        private Dictionary<Type, List<EntityInfo>> BeforeSaveEntities(Dictionary<Type, List<EntityInfo>> entitiesToBeSaved) {
            var entityInfos = entitiesToBeSaved[typeof(TodoItem)];
            entityInfos.ForEach(ei =>
            {
                var todo = ei.Entity as TodoItem;
                if (todo.Description.Contains("INTERCEPT")) {
                    todo.Description += " (" + entityInfos.Count + ")";
                }
            });

            return entitiesToBeSaved;
        }

        /// <summary>
        /// Demonstration post-save interceptor - called once per save request
        /// </summary>
        /// <param name="savedEntities">
        /// Dictionary (indexed by entity type) of entities to be saved
        /// </param>
        /// <param name="keyMappings">
        /// List of mappings from temporary to store-generated permanent keys
        /// </param>
        private void AfterSaveEntities(Dictionary<Type, List<EntityInfo>> savedEntities, List<KeyMapping> keyMappings) {
            var entityInfos = savedEntities[typeof(TodoItem)];
            entityInfos.ForEach(ei =>
            {
                var todo = ei.Entity as TodoItem;
                if (todo.Description.Contains("INTERCEPT")) {
                    todo.Description += " (" + entityInfos.Count + ", " + keyMappings.Count + ")";
                }
            });
        }

        #endregion SaveInterception

    }
}
