using Android.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using Todo.Models;

namespace TodoBreezeSharpAndroid.Adapters {
  class TodoGridAdapter : BaseAdapter {
    Activity _context = null;
    IList<TodoItem> _todos = new List<TodoItem>();
    Breeze.Sharp.EntityManager _em;
    TodoItem _item;
    TextView _txtDescription;
    CheckBox _checkboxIsDone;
    public Button _deleteButton;

    public TodoGridAdapter(Activity context, IList<TodoItem> todosList, Breeze.Sharp.EntityManager em)
      : base() {
      this._context = context;
      this._todos = todosList;
      this._todos.Add(new TodoItem());
      this._em = em;
    }

    public override int Count {
      get { return _todos.Count; }
    }

    public override Java.Lang.Object GetItem(int position) {
      return position;
    }

    public override long GetItemId(int position) {
      return position;
    }

    public override View GetView(int position, View convertView, ViewGroup parent) {
      _item = _todos[position];
      var gridView = _context.LayoutInflater.Inflate(
        Resource.Layout.TodoGrid,
        parent,
        false) as GridLayout;

      _txtDescription = gridView.FindViewById<TextView>(Resource.Id.DescriptionText);
      _checkboxIsDone = gridView.FindViewById<CheckBox>(Resource.Id.IsDoneCheckbox);
      _deleteButton = gridView.FindViewById<Button>(Resource.Id.DeleteButton);

      _txtDescription.SetText(_item.Description, TextView.BufferType.Normal);
      if (_item.EntityAspect.IsDetached) {
        _txtDescription.Hint = "Add new task";
        _checkboxIsDone.Visibility = ViewStates.Invisible;
        _deleteButton.Visibility = ViewStates.Invisible;
      }
      _checkboxIsDone.Checked = _item.IsDone;

      _txtDescription.TextChanged += async (object sender, Android.Text.TextChangedEventArgs e) => {
        _item = _todos[position];
        _txtDescription = gridView.FindViewById<EditText>(Resource.Id.DescriptionText);
        _item.Description = _txtDescription.Text;
        if (_item.EntityAspect.IsDetached) {
          _em.AddEntity(_item);
          _todos.Add(new TodoItem());
        }
        await _em.SaveChanges();
        //				    this.NotifyDataSetChanged();
      };

      _checkboxIsDone.CheckedChange += async (object sender, CompoundButton.CheckedChangeEventArgs e) => {
        _item = _todos[position];
        _checkboxIsDone = gridView.FindViewById<CheckBox>(Resource.Id.IsDoneCheckbox);
        _item.IsDone = _checkboxIsDone.Checked;
        await _em.SaveChanges();
        //					this.NotifyDataSetChanged();
      };

      _deleteButton.Click += async (object sender, EventArgs e) => {
        _item = _todos[position];
        _item.EntityAspect.Delete();
        _todos.RemoveAt(position);
        await _em.SaveChanges();
        this.NotifyDataSetChanged();
      };
      return gridView;
    }
  }
}