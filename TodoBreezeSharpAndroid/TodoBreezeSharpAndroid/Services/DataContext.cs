using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Breeze.Sharp;
using Todo.Models;

namespace TodoBreezeSharpAndroid.Services
{
  public interface IDataContext
  {
    void AddTodo(TodoItem todo);
    void DeleteTodo(TodoItem todo);
    Task<List<TodoItem>> GetAllTodos();
    bool HasChanges { get; } 
    Task<SaveResult> Save();
  }

  public class DataContext : IDataContext
  {
    public DataContext(ILogger logger, string serviceAddress)
    {
      // Tell Breeze where to find model types
      var assembly = typeof(TodoItem).Assembly;
      Configuration.Instance.ProbeAssemblies(assembly);

      _em = new EntityManager(serviceAddress);
      _logger = logger;
    }

    public void AddTodo(TodoItem todo)
    {
      _em.AddEntity(todo);
    }

    // Cancel a prior save delay (if there is one)
    private void CancelDelay()
    {
      if (_saveChangeTokenSource != null)
      {
        _saveChangeTokenSource.Cancel();
        _saveChangeTokenSource = null;
      }
    }

    public void DeleteTodo(TodoItem todo)
    {
      todo.EntityAspect.Delete();
    }

    private async void EntityChanged(object sender, EntityChangedEventArgs e)
    {
      // Save on propertyChanged (after brief delay)
      if (e.Action == EntityAction.PropertyChange)
      {
        try
        {        
          CancelDelay();
          // Delay 1/2 second to let more changes (keystrokes) arrive
          // then save if there are still unsaved changes.
          _saveChangeTokenSource = new CancellationTokenSource();
          await Task.Delay(500, _saveChangeTokenSource.Token);
          _saveChangeTokenSource = null;
          if (HasChanges) { await Save(); }
        }
        catch (TaskCanceledException) {}
        catch (Exception ex)  { _logger.Error(ex); } 
      }
      // Save Added or Deleted entity immediately
      else if (e.Action == EntityAction.EntityStateChange &&
              ((e.EntityAspect.EntityState & ADD_DELETE) != 0))
      {
        CancelDelay();
        await Save();
      }
    }

    public async Task<List<TodoItem>>  GetAllTodos()
    {
      var query = new EntityQuery<TodoItem>();

      try
      {
        var result = await _em.ExecuteQuery(query);
        _em.EntityChanged += EntityChanged;
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

    private const EntityState ADD_DELETE = EntityState.Added | EntityState.Deleted;
    private readonly EntityManager _em;
    private readonly ILogger _logger;
    private CancellationTokenSource _saveChangeTokenSource;
    private bool _saveQueued;
  }
}