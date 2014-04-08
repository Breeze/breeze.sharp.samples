using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Input;

namespace Todo_Net
{
    // Simple ICommand implementation
    public class Command : ICommand
    {
        private Action<object> _action;
        private Func<object, bool> _canExecute;

        public Command(Action<object> action, Func<object, bool> canExecute = null) {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) {
            return (_canExecute == null) ? true : _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged() {
            if (CanExecuteChanged != null) {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        public void Execute(object parameter) {
            if (_action == null) return;
            _action(parameter);
        }
    }
}
