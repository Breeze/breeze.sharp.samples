using System;
using Android.App;

namespace TodoBreezeSharpAndroid.Services
{
  public interface ILogger
  {
    void Error(Exception ex, string title = null);
    void Error(string message, string title = null);
    void Log(string message);
  }

  public class Logger : ILogger
  {
    private readonly Activity _activity;

    public Logger(Activity activity)
    {
      _activity = activity;
    }

    public void Error(Exception ex, string title = null)
    {
      Error(ex.Message); 
      Console.WriteLine(ex);
    }

    public virtual void Error(string message, string title = null)
    {
      Console.WriteLine("Error: " + message);
      var alertMessage = new AlertDialog.Builder(_activity).Create();
      alertMessage.SetTitle(title ?? "Error");
      alertMessage.SetMessage(message);
      alertMessage.Show();
    }

    public void Log(string message)
    {
      Console.WriteLine("Log: " + message);
    }
  }
}