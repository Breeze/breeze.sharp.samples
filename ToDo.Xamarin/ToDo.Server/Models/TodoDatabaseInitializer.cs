using System;
using System.Data.Entity;

namespace Todo.Models
{
    // DEMONSTRATION/DEVELOPMENT ONLY
    public class TodoDatabaseInitializer:
        //DropCreateDatabaseAlways<TodosContext> // re-creates every time the server starts
        DropCreateDatabaseIfModelChanges<TodosContext> 
    {
        protected override void Seed(TodosContext context)
        {
            SeedDatabase(context);
        }

        public static void SeedDatabase(TodosContext context)
        {
            _baseCreatedAtDate = new DateTime(2012, 8, 22, 9, 0, 0);

            var todos = new[] {
                // Description, Notes, IsDone, IsArchived
                CreateTodo("Food", "Who doesn't like Food?", true, true),
                CreateTodo("Water", "Pure, clean, H20.", true, true),
                CreateTodo("Shelter", "Even a tent under the stars will do.", true, true),
                CreateTodo("Bread", "I love that rough, country bread.", false, false),
                CreateTodo("Cheese", "Who doesn't eat Cheese?", true, false),
                CreateTodo("Wine", "Who doesn't LOVE wine?", false, false)
           };

            Array.ForEach(todos, t => context.Todos.Add(t));

            context.SaveChanges(); // Save 'em
        }

        private static TodoItem CreateTodo(
            string description, string notes, bool isDone, bool isArchived)
        {
            _baseCreatedAtDate = _baseCreatedAtDate.AddMinutes(1);
            return new TodoItem
            {
                CreatedAt = _baseCreatedAtDate,
                Description = description,
                Notes = notes,
                IsDone = isDone,
                IsArchived = isArchived
            };
        }

        private static DateTime _baseCreatedAtDate;

        public static void PurgeDatabase(TodosContext context)
        {
            var todos = context.Todos;
            foreach (var todoItem in todos)
            {
                todos.Remove(todoItem);
            }

            context.SaveChanges();
        }

    }
}