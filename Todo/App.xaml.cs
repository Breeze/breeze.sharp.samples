using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Breeze.Core;
using Breeze.Sharp;

using Todo.Models;

namespace Todo_Net
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, INotifyPropertyChanged
    {
        private EntityManager _em;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MetadataStore.Instance.ProbeAssemblies(typeof(TodoItem).Assembly);
            var serviceName = "http://localhost:63030/breeze/Todos/";
            _em = new EntityManager(serviceName);
            QueryAllTodos();
        }

        private async void QueryAllTodos()
        {
            var query = new EntityQuery<TodoItem>();
            Todos = await _em.ExecuteQuery(query);
        }

        public IEnumerable<TodoItem> Todos
        {
            get { return _todos; }
            set
            {
                _todos = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Todos"));
                }
            }
        }
        private IEnumerable<TodoItem> _todos = new TodoItem[0];

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
