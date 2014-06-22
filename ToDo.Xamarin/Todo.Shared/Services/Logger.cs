using System;

namespace Todo.Services
{
  public class Logger : ILogger
  {
    private readonly ISysLog _syslog;
    private readonly IToast _toast;

    public Logger(ISysLog syslog, IToast toast)
    {
      _toast = toast;
      _syslog = syslog;
      _syslog.Info(LOG_TAG, "Logger created");
    }

    public void Error(Exception ex)
    {
      Error(ex.Message); 
      Console.WriteLine(ex);
    }

    public virtual void Error(string message)
    {
      _toast.Show("Error: " + message);
      _syslog.Error(LOG_TAG, message);
      Console.WriteLine("Error: " + message);
    }
    public virtual void Info(string message)
    {
      _toast.Show("Info: " + message);
      _syslog.Info(LOG_TAG, message);
      Console.WriteLine("Info: " + message);
    }
    public void Log(string message)
    {
      _syslog.Info(LOG_TAG, message);
      Console.WriteLine("Log: " + message);
    }
    public virtual void Warning(string message)
    {
      _toast.Show("Warning: " + message);
      _syslog.Warn(LOG_TAG, message);
      Console.WriteLine("Warning: " + message);
    }

    private const string LOG_TAG = "BZ#";
  }
}