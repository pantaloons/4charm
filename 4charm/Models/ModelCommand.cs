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

#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
        public bool CanExecute(object parameter)
        {
            return true;
        }
    }

    class ModelCommand<T> : ICommand
    {
        private readonly Action<T> _execute = null;
        public ModelCommand(Action<T> execute)
        {
            _execute = execute;
        }

        public void Execute(object parameter)
        {
            if (_execute != null) _execute((T)parameter);
        }

#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
        public bool CanExecute(object parameter)
        {
            return true;
        }
    }
}
