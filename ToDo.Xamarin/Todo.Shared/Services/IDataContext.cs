using System.Collections.Generic;
using System.Threading.Tasks;
using Breeze.Sharp;
using Todo.Models;

namespace Todo.Services
{
  public interface IDataContext
  {
    void AddTodo(TodoItem todo);
    void DeleteTodo(TodoItem todo);
    Task<List<TodoItem>> GetAllTodos();
    bool HasChanges { get; } 
    Task<SaveResult> Save();
  }
}