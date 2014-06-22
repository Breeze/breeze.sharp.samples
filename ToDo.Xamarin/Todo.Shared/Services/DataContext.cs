using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Breeze.Sharp;
using Todo.Models;

namespace Todo.Services
{
  public class DataContext : IDataContext
  {
    public DataContext(ILogger logger, string serviceAddress)
    {
      // Tell Breeze where to find model types
      var assembly = typeof(TodoItem).Assembly;
      Configuration.Instance.ProbeAssemblies(assembly);

      _em = new EntityManager(serviceAddress);
      _em.EntityChanged += EntityChanged;
      _logger = logger;
    }

    public void AddTodo(TodoItem todo)
    {
      try
      {
        _em.AddEntity(todo);
      }
      catch (Exception e)
      {
        // most likely cause is tried to add entity before there is metadata
        _logger.Error(e);
        throw;
      }
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
      if (_ignoreEntityChanged) { return; }

      // Save on propertyChanged (after brief delay)
      if (e.Action == EntityAction.PropertyChange)
      {
        try
        {        
          CancelDelay();
            // Delay 1 second to let more changes (keystrokes) arrive
          // then save if there are still unsaved changes.
          _saveChangeTokenSource = new CancellationTokenSource();
            await Task.Delay(1000, _saveChangeTokenSource.Token);
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
      try
      {
        // ignore the flurry of events when query results arrive
        _ignoreEntityChanged = true;
        var query = new EntityQuery<TodoItem>();
        var qr = await _em.ExecuteQuery(query);
        _ignoreEntityChanged = false;
        var result = qr.ToList();
        _logger.Info("Got " + result.Count + " todos from the server");
        return result;
      }
      catch (Exception e)
      {
        _logger.Error(e);
        //throw; // if we want caller to hear it. But no caller is listening in this sample  
        return null; // return useless result instead
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
            IsSavePending = _saveQueued;  // true if someone tried to save while we were waiting
            var descs = String.Join(", ", 
                        result.Entities
                              .Select(x => (x is TodoItem) ? (x as TodoItem).Description : "")
                              .ToArray());
            _logger.Info("Saved "+result.Entities.Count+" change(s): "+ descs);           
          } else {
            IsSavePending = false;
          }
        } 
        return result;
      }
      catch (Exception e)
      {
        IsSavePending = false;
        _logger.Error(e);
        //throw; // if we want caller to hear it. But no caller is listening in this sample  
        return result; // return useless result instead.  
      }
    }

    private const EntityState ADD_DELETE = EntityState.Added | EntityState.Deleted;
    private readonly EntityManager _em;
    private bool _ignoreEntityChanged;
    private readonly ILogger _logger;
    private CancellationTokenSource _saveChangeTokenSource;
    private bool _saveQueued;
  }
}