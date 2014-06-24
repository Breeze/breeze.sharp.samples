using Breeze.Sharp;
using System;

namespace Todo.Models { 

  public class TodoItem : BaseEntity {
    public TodoItem()
    {
      CreatedAt = DateTime.UtcNow;
    }

    public int Id                           // 42
    {
      get { return GetValue<int>(); }
      set { SetValue(value); }
    }

    public string Description               // "Get milk"
    {
      get { return GetValue<string>(); }
      set { SetValue(value); }
    }

    public string Notes                     // "Get milk"
    {
      get { return GetValue<string>(); }
      set { SetValue(value); }
    }

    public DateTime CreatedAt              // 25 August 2012, 9am PST
    {
      get { return GetValue<DateTime>(); }
      set { SetValue(value); }
    }

    public bool IsDone                     // false
    {
      get { return GetValue<bool>(); }
      set { SetValue(value); }
    }

    public bool IsArchived                // false
    {
      get { return GetValue<bool>(); }
      set { SetValue(value); }
    }
  }
}