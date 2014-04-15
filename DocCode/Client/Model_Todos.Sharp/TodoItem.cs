using System;
using System.ComponentModel.DataAnnotations;

using Breeze.Sharp;

namespace Todo.Models
{
    public class TodoItem : BaseEntity
    {
        public int Id                                   // 42
        {
            get { return GetValue<int>(); }
            set { SetValue(value); }
        }

        [Required, StringLength(maximumLength: 30)]    // Validation rules
        public string Description                      // "Get milk"
        {
            get { return GetValue<string>(); }
            set { SetValue(value); }
        }

        public System.DateTime CreatedAt              // 25 August 2012, 9am PST
        {
            get { return GetValue<DateTime>(); }
            set { SetValue(value); }
        }

        public bool IsDone                            // false
        {
            get { return GetValue<bool>(); }
            set { SetValue(value); }
        }

        public bool IsArchived                        // false
        {
            get { return GetValue<bool>(); }
            set { SetValue(value); }
        }
    }
}
