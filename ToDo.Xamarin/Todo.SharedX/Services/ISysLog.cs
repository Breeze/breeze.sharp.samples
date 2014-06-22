namespace Todo.Services
{
    public interface ISysLog
    {
     void Error(string tag, string message);
     void Info(string tag, string message);
     void Warn(string tag, string message);
    }
}
