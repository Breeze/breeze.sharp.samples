using System;
using Android.App;
using Util = Android.Util;
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
      Util.Log.Info(LOG_TAG, "Logger created");
    }

    public void Error(Exception ex)
    {
      Error(ex.Message); 
      Console.WriteLine(ex);
    }

    public virtual void Error(string message)
    {
      ShowToast("Error: " + message);
      Util.Log.Error(LOG_TAG, message);
      Console.WriteLine("Error: " + message);
    }
    public virtual void Info(string message)
    {
      ShowToast("Info: " + message);
      Util.Log.Info(LOG_TAG, message);
      Console.WriteLine("Info: " + message);
    }
    public void Log(string message)
    {
      Util.Log.Info(LOG_TAG, message);
      Console.WriteLine("Log: " + message);
    }
    public virtual void Warning(string message)
    {
      ShowToast("Warning: " + message);
      Util.Log.Warn(LOG_TAG, message);
      Console.WriteLine("Warning: " + message);
    }

    protected virtual void ShowToast(string message)
    {
      var toast = Toast.MakeText(_activity, message, ToastLength.Short);
      toast.SetGravity(GravityFlags.Top | GravityFlags.Center, 0, 100);
      toast.Show();
    }

    private const string LOG_TAG = "BZ#";
  }
}