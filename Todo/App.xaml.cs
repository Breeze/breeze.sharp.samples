using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Breeze.Core;
using Breeze.Sharp;

using Todo.Models;

namespace Todo_Net
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TodoViewModel _mainViewModel;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create the Breeze entity manager
            var serviceAddress = "http://localhost:63030/breeze/Todos/";
            var assembly = typeof(TodoItem).Assembly;
            var rslt = MetadataStore.Instance.ProbeAssemblies(assembly);
            var entityManager = new EntityManager(serviceAddress);

            // Create the main viewModel and view
            _mainViewModel = new TodoViewModel(entityManager);
        }

        /// <summary>
        /// Supplies the main view when requested by the main window
        /// </summary>
        public UserControl MainView 
        {
            get { return _mainViewModel.View; }
        }
    }
}
