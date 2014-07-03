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
        /// Demonstration interceptor
        /// </summary>
        /// <returns>
        /// True if can save this entity else throw exception
        /// </returns>
        /// <exception cref="System.InvalidOperationException" />
        private bool BeforeSaveEntity(EntityInfo arg) {
            var todo = arg.Entity as TodoItem;
            if (todo != null && todo.Description.Contains("INTERCEPT")) {
                todo.Description += " SAVED!";
            }
            return true;
        }

        #endregion SaveInterception

    }
}
