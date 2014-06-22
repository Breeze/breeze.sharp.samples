using Util = Android.Util;
using Todo.Services;

namespace Todo.Android.Services
{
  public class SysLog : ISysLog
  {
    public void Error(string tag, string message)
    {
      Util.Log.Error(tag, message);
    }

    public void Info(string tag, string message)
    {
      Util.Log.Info(tag, message);
    }

    public void Warn(string tag, string message)
    {
      Util.Log.Warn(tag, message);
    }
  }
}
