using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Widget;
using System;
using Todo.Models;
using TodoBreezeSharpAndroid.Services;

namespace TodoBreezeSharpAndroid {
  [Activity(Label = "Todo Breeze# Android", MainLauncher = true, Icon = "@drawable/icon")]
  public class MainActivity : Activity {
    private Adapters.TodoGridAdapter todoGridAdapter;
    private GridView todosGridView;
    private TextView newTodoView;
    private List<TodoViewModel> todos;
    private IDataContext _dataContext;
    private ILogger _logger;
    private const string _knownServiceAddress = 
        "http://67.169.112.221:49798/android/breeze/todos/";

    protected override void OnCreate(Bundle bundle) {
      base.OnCreate(bundle);

      _logger = new Logger(this);
      SetContentView(Resource.Layout.Main);

      todosGridView = FindViewById<GridView>(Resource.Id.gridview);

      newTodoView = FindViewById<TextView>(Resource.Id.NewTodoText);
      newTodoView.FocusChange += AddTodo;
      GetAllTodos();
    }

    private async void AddTodo(object sender, Android.Views.View.FocusChangeEventArgs e)
    {
      var description = newTodoView.Text.Trim();
      if (String.IsNullOrEmpty(description) || todos == null) { return; }

      newTodoView.Text = String.Empty;
      var item = new TodoItem {Description = description};
      _dataContext.AddTodo(item);
      var vm = new TodoViewModel(item);
      todos.Insert(0, vm);                    // front of the list
      todoGridAdapter.NotifyDataSetChanged(); // redraw
      await _dataContext.Save();
    }

    public async void GetAllTodos()
    {
      _dataContext = new DataContext(_logger, _knownServiceAddress);
      var items = await _dataContext.getAllTodos();

      todos = items.Select(t => new TodoViewModel(t)).ToList();
      todoGridAdapter = new Adapters.TodoGridAdapter(this, todos, _dataContext);
      todosGridView.Adapter = todoGridAdapter;
    }
  }

  internal class TodoViewModel
  {
    private static int nextKey;

    public TodoViewModel(TodoItem todo)
    {
      ViewKey = nextKey++;
      Todo = todo;
    }
    public int ViewKey { get; private set; }
    public TodoItem Todo { get; private set; }
  }
}

