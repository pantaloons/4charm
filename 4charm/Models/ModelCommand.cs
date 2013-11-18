using System;
using System.Windows.Input;

namespace _4charm.Models
{
    /// <summary>
    /// Default implementation of the ICommand interface.
    /// 
    /// Just a bindable action for buttons and other tap handlers.
    /// </summary>
    class ModelCommand : ICommand
    {
        private readonly Action _execute = null;

        public ModelCommand(Action execute)
        {
            _execute = execute;
        }

        public void Execute(object parameter)
        {
            if (_execute != null) _execute();
        }

        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter)
        {
            return true;
        }
    }
}
