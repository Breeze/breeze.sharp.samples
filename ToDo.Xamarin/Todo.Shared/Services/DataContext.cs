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
    private const string _knownServiceAddress =
      //"http://67.169.112.221:49798/android/breeze/todos/";
      "http://xamarintasky.azurewebsites.net/breeze/todos/";

    public DataContext(ILogger logger) {
      // Tell Breeze where to find model types
      var assembly = typeof(TodoItem).Assembly;
      Configuration.Instance.ProbeAssemblies(assembly);

      _em = new EntityManager(_knownServiceAddress);
      _em.EntityChanged += EntityChanged;
      SetTimeout(90); // longer at start to allow server (Azure) warmup
      _logger = logger;
    }

    public void AddTodo(TodoItem todo) {
      try {
        _em.AddEntity(todo);
      } catch (Exception e) {
        // most likely cause is tried to add entity before there is metadata
        _logger.Error(e);
        throw;
      }
    }

    // Cancel a prior save delay (if there is one)
    private void CancelDelay() {
      if (_saveChangeTokenSource != null) {
        _saveChangeTokenSource.Cancel();
        _saveChangeTokenSource = null;
      }
    }

    public void DeleteTodo(TodoItem todo) {
      todo.EntityAspect.Delete();
    }

    private async void EntityChanged(object sender, EntityChangedEventArgs e) {
      if (_ignoreEntityChanged) { return; }

      // Save on propertyChanged (after brief delay)
      if (e.Action == EntityAction.PropertyChange) {
        try {
          CancelDelay();
          // Delay 1 second to let more changes (keystrokes) arrive
          // then save if there are still unsaved changes.
          _saveChangeTokenSource = new CancellationTokenSource();
          await Task.Delay(1000, _saveChangeTokenSource.Token);
          _saveChangeTokenSource = null;
          if (HasChanges) { await Save(); }
        } 
        catch (TaskCanceledException) { } 
        catch (Exception ex) { _logger.Error(ex); }
      }
        // Save Added or Deleted entity immediately
      else if (e.Action == EntityAction.EntityStateChange &&
              ((e.EntityAspect.EntityState & ADD_DELETE) != 0)) {
        CancelDelay();
        try {
          await Save();
          // ReSharper disable once EmptyGeneralCatchClause
          // already handled within Save
        } catch (Exception) {
        }
      }
    }

    public async Task<List<TodoItem>> GetAllTodos()
    {
      try {
        // ignore the flurry of events when query results arrive
        _ignoreEntityChanged = true;
        var query = new EntityQuery<TodoItem>();
        var qr = await _em.ExecuteQuery(query);
        _ignoreEntityChanged = false;
        var result = qr.ToList();
        _logger.Info("Got " + result.Count + " todos from the server");
        SetTimeout(); // revert to default timeout
        return result;

      } catch (Exception e) {
        _ignoreEntityChanged = false;
        _logger.Error(e);
        //throw; // if we want caller to hear it. But no caller is listening in this sample  
        return new List<TodoItem>(); // return empty result instead
      }
    }

    public bool HasChanges { get { return _em.HasChanges(); } }

    public bool IsSavePending { get; private set; }

    // Simplified save queuing, appropriate for this sample:
    // - always saves everything 
    // - does not accept options
    // - "loops" until there are no more queued saves
    // - returns results of the last save to all queued callers
    public async Task<SaveResult> Save() {
      if (IsSavePending) {
        _saveQueued = true;
      } else {
        IsSavePending = true;
        _saveTcs = new TaskCompletionSource<SaveResult>();
        try {
          _saveTcs.SetResult(await TrySave());
        } catch (OperationCanceledException) {
          _saveTcs.SetCanceled();
          _logger.Error("Save canceled ... probably timed-out ... do you have a server connection?");
          IsSavePending = false;
        } catch (Exception ex) {
          _saveTcs.SetException(ex);
          _logger.Error(ex);
          IsSavePending = false;
        }
      }
      return await _saveTcs.Task;
    }

    private async Task<SaveResult> TrySave() {
      SaveResult saveResult = null;
      while (IsSavePending) {
        _saveQueued = false;
        saveResult = await _em.SaveChanges();
        LogSaveResult(saveResult);
        IsSavePending = _saveQueued; // true if someone tried to save while we were waiting
      }
      return saveResult;
    }

    private void LogSaveResult(SaveResult result) {
      var descs = String.Join(", ",
     result.Entities
           .Select(x => (x is TodoItem) ? (x as TodoItem).Description : "")
           .ToArray());
      _logger.Info("Saved " + result.Entities.Count + " change(s): " + descs);
    }

    private void SetTimeout(int seconds = DEFAULT_TIMEOUT) {
      _em.DataService.HttpClient.Timeout = new TimeSpan(0, 0, seconds);
    }

    private const int DEFAULT_TIMEOUT = 10; // secs;
    private const EntityState ADD_DELETE = EntityState.Added | EntityState.Deleted;
    private readonly EntityManager _em;
    private bool _ignoreEntityChanged;
    private readonly ILogger _logger;
    private CancellationTokenSource _saveChangeTokenSource;
    private TaskCompletionSource<SaveResult> _saveTcs;
    private bool _saveQueued;
  }
}