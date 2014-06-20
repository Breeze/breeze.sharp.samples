using System;
using Android.App;
using Android.Views;
using Android.Widget;

namespace TodoBreezeSharpAndroid.Services
{
  public interface ILogger
  {
    void Error(Exception ex);
    void Error(string message);
    void Info(string message);
    void Log(string message);
    void Warning(string message);
  }

  public class Logger : ILogger
  {
    private readonly Activity _activity;

    public Logger(Activity activity)
    {
      _activity = activity;
    }

    public void Error(Exception ex)
    {
      Error(ex.Message); 
      Console.WriteLine(ex);
    }

    public virtual void Error(string message)
    {
      ShowToast("Error: " + message);
      Console.WriteLine("Error: " + message);
    }
    public virtual void Info(string message)
    {
      ShowToast("Info: " + message);
      Console.WriteLine("Info: " + message);
    }
    public void Log(string message)
    {
      Console.WriteLine("Log: " + message);
    }
    public virtual void Warning(string message)
    {
      ShowToast("Warning: " + message);
      Console.WriteLine("Warning: " + message);
    }

    protected virtual void ShowToast(string message)
    {
      var toast = Toast.MakeText(_activity, message, ToastLength.Short);
      toast.SetGravity(GravityFlags.Top | GravityFlags.Center, 0, 100);
      toast.Show();
    }
  }
}