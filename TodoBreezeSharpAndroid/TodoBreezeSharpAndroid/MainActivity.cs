using Android.App;
using Android.OS;
using Android.Widget;
using Breeze.Sharp;
using System;
using System.Collections.Generic;
using Todo.Models;

namespace TodoBreezeSharpAndroid {
  [Activity(Label = "Todo Breeze# Android", MainLauncher = true, Icon = "@drawable/icon")]
  public class MainActivity : Activity {
    Adapters.TodoGridAdapter todoGridAdapter;
    IList<TodoItem> todosList = new List<TodoItem>();
    GridView todosGridView;
    EntityManager _em;

    protected override void OnCreate(Bundle bundle) {
      base.OnCreate(bundle);

      SetContentView(Resource.Layout.Main);

      todosGridView = FindViewById<GridView>(Resource.Id.gridview);

      var serviceAddress = "http://67.169.112.221:49798/android/breeze/todos/";
      var assembly = typeof(TodoItem).Assembly;
      var rslt = Configuration.Instance.ProbeAssemblies(assembly);
      _em = new EntityManager(serviceAddress);

      QueryAllTodos();
    }

    public async void QueryAllTodos() {
      //var xx = EntityGroup.Create(typeof(TodoItem), _em);

      var query = new EntityQuery<TodoItem>();

      try {
        var todos = await _em.ExecuteQuery(query);

        foreach (TodoItem todo in todos) {
          todosList.Add(todo);
        }

        todoGridAdapter = new Adapters.TodoGridAdapter(this, todosList, _em);

        todosGridView.Adapter = todoGridAdapter;

      } catch (Exception e) {
        AlertDialog alertMessage = new AlertDialog.Builder(this).Create();
        alertMessage.SetTitle("Error");
        alertMessage.SetMessage(e.Message);
        alertMessage.Show();
      }
    }
  }
}

