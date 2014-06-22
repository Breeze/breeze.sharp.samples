using System;

namespace Todo.Services
{
  public interface ILogger
  {
    void Error(Exception ex);
    void Error(string message);
    void Info(string message);
    void Log(string message);
    void Warning(string message);
  }
}