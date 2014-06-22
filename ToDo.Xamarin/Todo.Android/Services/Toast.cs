using Android.App;
using Android.Views;
using Widget = Android.Widget;
using Todo.Services;

namespace Todo.Android.Services
{
  class Toast : IToast
  {
    private readonly Activity _context;
    public Toast(Activity context)
    {
      _context = context;
    }
    public void Show(string message)
    {
      var toast = Widget.Toast.MakeText(_context, message, Widget.ToastLength.Short);
      toast.SetGravity(GravityFlags.Top | GravityFlags.Center, 0, 100);
      toast.Show();
    }
  }
}