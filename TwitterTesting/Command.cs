using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace TwitterTesting
{
    class Command : ICommand
    {
        private Action<object> execute;

        private bool canExecute;
        public bool IsCanExecute
        {
            get { return canExecute; }
            set { if (canExecute != value) { canExecute = value; CanExecuteChanged(this, new EventArgs()); } }
        }

        public Command(Action<object> execute)
        {
            this.execute = execute;
            this.canExecute = true;
        }

        public Command(Action<object> execute, bool canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return IsCanExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (canExecute)
                execute(parameter);
        }
    }
}
