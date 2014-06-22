using System.Globalization;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using Todo.Services;

namespace Todo.Android.Adapters
{
  class TodoGridAdapter : BaseAdapter
  {
    private readonly Activity _context;
    private readonly IDataContext _dataContext;
    private readonly List<TodoViewModel> _todoVms;

    public TodoGridAdapter(Activity context, List<TodoViewModel> todoVms, IDataContext dataContext)
    {
      _context = context;
      _dataContext = dataContext;
      _todoVms = todoVms;
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
      var vm = _todoVms[position];
      var item = vm.Todo;
      view.Tag = vm.ViewKey.ToString(CultureInfo.InvariantCulture);
      description.Text = item.Description;
      isDone.Checked = item.IsDone;

      if (isNewView) {
        // add event handlers to new views only
        isDone.Click += (sender, e) => IsDoneClicked(view, isDone);
        description.TextChanged += (sender, e) => DescriptionChanged(view, description);
        delete.Click += (sender, e) => DeleteClicked(view);
      }
      return view;
    }

    private void DescriptionChanged(View view, TextView description)
    {
      GetViewVm(view).Todo.Description = description.Text;
    }

    private void DeleteClicked(View view)
    {
        var vm = GetViewVm(view);
        _dataContext.DeleteTodo(vm.Todo);
        _todoVms.Remove(vm);
        NotifyDataSetChanged(); // trigger view reset so item disappears
    }

    private void IsDoneClicked(View view, CheckBox isDone)
    {
      GetViewVm(view).Todo.IsDone = isDone.Checked;
    }

    private TodoViewModel GetViewVm(View view)
    {
      if (view == null) { return null; }
      var key = Int32.Parse(view.Tag.ToString());
      var vm = _todoVms.Single(x => x.ViewKey == key);
      return vm;
    }

    public override int Count
    {
      get { return _todoVms.Count; }
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