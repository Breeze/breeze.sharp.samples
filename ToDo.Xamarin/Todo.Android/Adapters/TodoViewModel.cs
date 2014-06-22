using Todo.Models;

namespace Todo.Android {

  internal class TodoViewModel {
    private static int nextKey;

    public TodoViewModel(TodoItem todo) {
      ViewKey = nextKey++;
      Todo = todo;
    }
    public int ViewKey { get; private set; }
    public TodoItem Todo { get; private set; }
  }
}