using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Widget;
using System;
using Todo.Android.Services;
using Todo.Models;
using Todo.Services;

namespace Todo.Android {
  [Activity(Label = "Todo Breeze# Android", MainLauncher = true, Icon = "@drawable/icon")]
  public class MainActivity : Activity {
    private Adapters.TodoGridAdapter todoGridAdapter;
    private GridView todosGridView;
    private TextView newTodoView;
    private List<TodoViewModel> todos;
    private IDataContext _dataContext;
    private ILogger _logger;

    protected override void OnCreate(Bundle bundle) {
      base.OnCreate(bundle);

      _logger = new Logger(new SysLog(), new Services.Toast(this));
      _dataContext = new DataContext(_logger);

      SetContentView(Resource.Layout.Main);

      todosGridView = FindViewById<GridView>(Resource.Id.gridview);

      newTodoView = FindViewById<TextView>(Resource.Id.NewTodoText);
      newTodoView.FocusChange += AddTodo;

      GetAllTodos();
    }

    private void AddTodo(object sender, EventArgs e)
    {
      var description = newTodoView.Text.Trim();
      if (String.IsNullOrEmpty(description) || todos == null) { return; }

      var item = new TodoItem {Description = description};
      try   { _dataContext.AddTodo(item); }
      catch { return; } //eat the error because logging it at lower level
      newTodoView.Text = String.Empty;
      var vm = new TodoViewModel(item);
      todos.Insert(0, vm);                    // front of the list
      todoGridAdapter.NotifyDataSetChanged(); // redraw   
    }

    public async void GetAllTodos()
    {
      var items = await _dataContext.GetAllTodos();

      todos = items.Select(t => new TodoViewModel(t)).ToList();
      todoGridAdapter = new Adapters.TodoGridAdapter(this, todos, _dataContext);
      todosGridView.Adapter = todoGridAdapter;
    }
  }
}

