using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Breeze.Sharp;
using Todo.Models;

namespace TodoBreezeSharpAndroid.Services
{
  public interface IDataContext
  {
    TodoItem AddTodo(TodoItem todo);
    Task<List<TodoItem>> getAllTodos();
    bool HasChanges { get; } 
    Task<SaveResult> Save();
  }

  public class DataContext : IDataContext
  {
    private readonly EntityManager _em;
    private readonly ILogger _logger;

    public DataContext(ILogger logger, string serviceAddress)
    {
      // Tell Breeze where to find model types
      var assembly = typeof(TodoItem).Assembly;
      Configuration.Instance.ProbeAssemblies(assembly);

      _em = new EntityManager(serviceAddress);
      _logger = logger;
    }

    public TodoItem AddTodo(TodoItem todo)
    {
      _em.AddEntity(todo);
      return todo;
    }

    public async Task<List<TodoItem>>  getAllTodos()
    {
      var query = new EntityQuery<TodoItem>();

      try
      {
        var result = await _em.ExecuteQuery(query);
        return result.ToList();
      }
      catch (Exception e)
      {
        _logger.Error(e);
        throw;
      }
    }

    public bool HasChanges { get { return _em.HasChanges(); } }

    public bool IsSavePending { get; private set; }

    public async Task<SaveResult> Save()
    {
      SaveResult result = null;
      if (IsSavePending)
      {
        _saveQueued = true;
        return null; // don't save now
      }
      IsSavePending = true;
      try
      {
        while (IsSavePending)
        {
          _saveQueued = false;
          if (_em.HasChanges())
          {
            _logger.Log("Saving ...");
            result = await _em.SaveChanges();
            _logger.Log("Saved ...");           
          }
          IsSavePending = _saveQueued;
        }
        return result;
      }
      catch (Exception e)
      {
        _logger.Error(e);
        throw;     
      }
    }

    private bool _saveQueued;

  }
}