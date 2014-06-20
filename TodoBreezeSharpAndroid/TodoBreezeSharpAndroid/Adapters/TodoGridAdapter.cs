using System.Globalization;
using Android.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using Todo.Models;
using TodoBreezeSharpAndroid.Services;

namespace TodoBreezeSharpAndroid.Adapters
{

  class TodoGridAdapter : BaseAdapter
  {
    private readonly Activity _context;
    private readonly IDataContext _dataContext;
    private readonly List<TodoItem> _todoItems;

    public TodoGridAdapter(Activity context, List<TodoItem> todoItems, IDataContext dataContext)
    {
      _context = context;
      _dataContext = dataContext;
      _todoItems = todoItems;
    }

    public override View GetView(int position, View convertView, ViewGroup parent)
    {
      var isNewView = convertView == null;

      var view = convertView ?? _context.LayoutInflater.Inflate(
            Resource.Layout.TodoGrid,
            parent,
            false) as GridLayout;

      System.Diagnostics.Debug.Assert(view != null, "TodoItem view is null?!?");

      // View Controls
      var delete = view.FindViewById<Button>(Resource.Id.DeleteButton);
      var description = view.FindViewById<TextView>(Resource.Id.DescriptionText);
      var isDone = view.FindViewById<CheckBox>(Resource.Id.IsDoneCheckbox);

      // Copy item data to the controls
      var item = _todoItems[position];
      view.Tag = position.ToString(CultureInfo.InvariantCulture);
      description.SetText(item.Description, TextView.BufferType.Normal);
      isDone.Checked = item.IsDone;

      if (isNewView) {
        // add event handlers to new views only
        isDone.Click += (sender, e) => IsDoneClicked(view, isDone);
        description.FocusChange += (sender, e) => DescriptionUpdate(view, description);
        delete.Click += (sender, e) => DeleteClicked(view);        
      }
      return view;
    }

    private void DescriptionUpdate(View view, TextView description)
    {
      var item = GetViewItem(view);
      item.Description = description.Text;
      if (_dataContext.HasChanges) { _dataContext.Save(); }
    }

    private void DeleteClicked(View view)
    {
        var item = GetViewItem(view);
        item.EntityAspect.Delete();
        _todoItems.Remove(item);
        NotifyDataSetChanged(); // trigger view reset so item disappears
        _dataContext.Save();
    }

    private void IsDoneClicked(View view, CheckBox isDone)
    {
      var item = GetViewItem(view);
      item.IsDone = isDone.Checked;
      _dataContext.Save();
    }

    private TodoItem GetViewItem(View view)
    {
      if (view == null) { return null; }
      var ix = Int32.Parse(view.Tag.ToString());
      return _todoItems[ix];
    }

    public override int Count
    {
      get { return _todoItems.Count; }
    }

    // Overriding GetItem and GetItemId to do type conversion for Android 
    // Is this really necessary???
    public override Java.Lang.Object GetItem(int position)
    {
      return position;
    }

    public override long GetItemId(int position)
    {
      return position;
    }

  }
}