using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using System.Windows.Input;

using Breeze.Sharp.Core;
using Breeze.Sharp;

using Todo.Models;

namespace Todo_Net
{
    // Message severity
    public enum Severity
    {
        Info,
        Warning,
        Error,
    }

    public class TodoViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private EntityManager _em;

        #region Ctor

        public TodoViewModel(EntityManager em) {
            _em = em;
            _em.EntityChanged += _em_EntityChanged;
            _em.EntityChanging += _em_EntityChanging;

            SetupCommands();

            View = new TodoView();
            View.DataContext = this;

            QueryAllTodos();
            //NewTodo = CreateTodoItem();
        }

        public TodoView View { get; private set; }

        #endregion Ctor

        #region Bindable properties

        public string NewTodoDescription {
            get { return _newTodoDescription; }
            set {
                _newTodoDescription = value;
                RaisePropertyChanged("NewTodoDescription");
                if (!string.IsNullOrEmpty(_newTodoDescription)) {
                    AddNewTodo(_newTodoDescription);
                }
            }
        }
        private string _newTodoDescription;

        public bool ShowArchived {
            get { return _showArchived; }
            set {
                _showArchived = value;
                RaisePropertyChanged("ShowArchived");

                QueryAllTodos();
            }
        }
        private bool _showArchived;

        public IEnumerable<TodoItem> Items {
            get { return _items; }
            set {
                _items = value;
                RaisePropertyChanged("Items");
            }
        }
        private IEnumerable<TodoItem> _items = new TodoItem[0];

        public TodoItem SelectedItem {
            get { return _selectedItem; }
            set {
                _selectedItem = value;
                RaisePropertyChanged("SelectedItem");
            }
        }
        private TodoItem _selectedItem;

        public int NumUncompleted {
            get { return Items.Count(i => !i.IsDone); }
        }

        public int NumArchivable {
            get { return Items.Count(i => i.IsDone && !i.IsArchived); }
        }

        // Used for save, warning and error messages
        public string Message {
            get { return _message; }
            set {
                _message = value;
                RaisePropertyChanged("Message");
            }
        }
        private string _message;

        // Used for save, warning and error messages
        public Severity Severity {
            get { return _severity; }
            set {
                _severity = value;
                RaisePropertyChanged("Severity");
            }
        }
        private Severity _severity;

        // Used for fetch notifications 
        public string Message2 {
            get { return _message2; }
            set {
                _message2 = value;
                RaisePropertyChanged("Message2");
            }
        }
        private string _message2;

        // Used for fetch notifications
        public Severity Severity2 {
            get { return _severity2; }
            set {
                _severity2 = value;
                RaisePropertyChanged("Severity2");
            }
        }
        private Severity _severity2;

        #endregion Bindable properties

        #region Toaster methods

        private void ShowToast(Severity severity, string message) {
            Severity = severity;
            Message = message;
        }

        private void ShowToast2(Severity severity, string message) {
            Severity2 = severity;
            Message2 = message;
        }

        #endregion Toaster methods

        #region Commands

        public Command MarkAllAsDoneCommand { get; set; }
        public Command PurgeCommand { get; set; }
        public Command ResetCommand { get; set; }
        public Command ArchiveCommand { get; set; }
        public Command DeleteCommand { get; set; }

        private void SetupCommands() {
            MarkAllAsDoneCommand = new Command(MarkAllAsDone);
            PurgeCommand = new Command(Purge);
            ResetCommand = new Command(Reset);
            ArchiveCommand = new Command(Archive, (obj) => (NumArchivable > 0));
            DeleteCommand = new Command(Delete);
        }

        private void MarkAllAsDone(object obj) {
            if (NumUncompleted <= 0) {
                ShowToast(Severity.Warning, "There are no unarchived uncompleted items");
                return;
            }

            DisableAutoSave();
            Items.Where(i => !i.IsDone).ForEach(i => i.IsDone = true);
            SaveChanges();
            EnableAutoSave();
        }

        private void Purge(object obj) {
            DoServerFunction("Purge", "purged");
            QueryAllTodos();
        }

        private void Reset(object obj) {
            DoServerFunction("Reset", "reset");
            QueryAllTodos();
        }

        // Invoke a function on the server
        private void DoServerFunction(string functionName, string expectedResponse) {
            byte[] response = new byte[0];
            string responseString;

            var success = InvokeServerFunction(functionName, out response, out responseString);
            var severity = (responseString == "\"" + expectedResponse + "\"") ? Severity.Info : Severity.Error;
            if (success) {
                ShowToast(severity, responseString);
            }
            else {
                ShowToast(Severity.Error, string.Format("Invocation of {0} function failed due to {1}", functionName, responseString));
            }
        }

        // TODO:  Move this or something like it to a persistence layer
        private bool InvokeServerFunction(string functionName, out byte[] response, out string responseString, params object[] parameters) {
            response = new byte[0];
            responseString = string.Empty;
            using (var webClient = new WebClient()) {
                var data = new NameValueCollection();
                int i = 0;
                parameters.ForEach(p =>
                    {
                        var parameterName = string.Format("p{0}", i++);
                        data.Add(parameterName, p as string);
                    });
                try {
                    var serviceAddress = _em.DefaultDataService.ServiceName;
                    response = webClient.UploadValues(serviceAddress + "/" + functionName, "POST", data);
                    responseString = Encoding.Default.GetString(response);
                    return true;
                }
                catch (WebException e) {
                    responseString = e.Message;
                    return false;
                }
            }
        }

        private void Archive(object obj) {
            DisableAutoSave();
            Items.Where(i => i.IsDone && !i.IsArchived)
                 .ForEach(i => i.IsArchived = true);
            SaveChanges();
            EnableAutoSave();
        }

        private void Delete(object obj) {
            var todo = obj as TodoItem;
            todo.EntityAspect.Delete();
        }

        // Invoked when new description entered
        private void AddNewTodo(string description) {
            var todo = CreateTodoItem();
            todo.Description = description;
            if (ValidateEntity(todo)) {
                _em.AddEntity(todo);
                SaveChanges();
                NewTodoDescription = null;
            }
        }

        private TodoItem CreateTodoItem() {
            var todoType = MetadataStore.Instance.GetEntityType(typeof(TodoItem));
            var todo = todoType.CreateEntity() as TodoItem;
            todo.CreatedAt = DateTime.Now;
            return todo;
        }

        #endregion Commands

        #region Refresh

        private async void QueryAllTodos() {
            var query = new EntityQuery<TodoItem>();
            if (!ShowArchived) {
                query = query.Where(ti => !ti.IsArchived);
            }
            var todos = await _em.ExecuteQuery(query);

            var numTodos = todos.Count();
            var archived = (ShowArchived) ? "including" : "excluding";
            ShowToast2(Severity.Info, string.Format("Fetched {0} Todos {1} archived", numTodos, archived));

            Items = todos;
            RefreshDisplay();
        }

        // Inform view of changed property values
        private void RefreshDisplay() {
            RaisePropertyChanged("NumUncompleted");
            RaisePropertyChanged("NumArchivable");
            MarkAllAsDoneCommand.RaiseCanExecuteChanged();
            ArchiveCommand.RaiseCanExecuteChanged();
        }

        void _em_EntityChanging(object sender, EntityChangingEventArgs e) {
            var todo = e.Entity as TodoItem;
            Console.WriteLine("Changing: " + todo.Description + " " + e.Action + " state = " + e.EntityAspect.EntityState);
        }

        // Handle change in status of any entity
        private void _em_EntityChanged(object sender, EntityChangedEventArgs e) {
            var todo = e.Entity as TodoItem;
            Console.WriteLine("Changed: " + todo.Description + " " + e.Action + " state = " + e.EntityAspect.EntityState);
            if (!_autoSaveEnabled) return;

            if (ValidateEntity(e.Entity)) {
                // Note:  Newly-added entities are saved in the AddNewTodo() method so they don't need to be saved here
                if (e.Action == EntityAction.EntityStateChange && e.EntityAspect.EntityState.IsDeletedOrModified()) {
                    SaveChanges();
                }
            }
            SelectedItem = null;
        }

        #endregion Refresh

        #region Save

        private bool _autoSaveEnabled = true;

        private void DisableAutoSave() {
            _autoSaveEnabled = false;
        }

        private void EnableAutoSave() {
            _autoSaveEnabled = true;
        }

        private async void SaveChanges() {
            try {
                var saveResult = await _em.SaveChanges();
                HandleSaveSuccess(saveResult);
            }
            catch (SaveException ex) {
                HandleSaveFailure(ex);
            }
        }

        private void HandleSaveSuccess(SaveResult saveResult) {
            ShowToast(Severity.Info, string.Format("Saved {0} Todo(s)" + Environment.NewLine, saveResult.Entities.Count()));
            SelectedItem = null;
            QueryAllTodos();
        }

        private void HandleSaveFailure(SaveException e) {
            if (e.ValidationErrors.Any()) {
                var message = FormatValidationErrors(e.ValidationErrors);
                ShowToast(Severity.Warning, message);
            }
            else {
                ShowToast(Severity.Error, e.Message);
            }
        }

        #endregion Save

        #region Validation

        private bool ValidateEntity(IEntity entity) {
            var validationErrors = entity.EntityAspect.Validate();
            if (validationErrors.Any()) {
                var message = FormatValidationErrors(validationErrors);
                ShowToast(Severity.Warning, message);
                return false;
            }
            return true;
        }

        private string FormatValidationErrors(IEnumerable<ValidationError> validationErrors) {
            var message = "Validation errors:" + Environment.NewLine;
            validationErrors.ForEach(ve =>
            {
                message += ve.Message + Environment.NewLine;
            });
            return message;
        }

        #endregion Validation

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName) {
            if (PropertyChanged != null && !string.IsNullOrEmpty(propertyName)) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged implementation

        #region IDataErrorInfo implementation

        public string Error {
            get {
                return null;
            }
        }

        public string this[string columnName] {
            get {
                return (columnName == "NewTodoDescription" && 
                        !string.IsNullOrEmpty(NewTodoDescription) && 
                        NewTodoDescription.Length > 30) ? "Description field must be 30 characters or less" : null;
            }
        }

        #endregion IDataErrorInfo implementation
    }
}
