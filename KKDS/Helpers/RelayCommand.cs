using System;
using System.Windows.Input;

namespace KKDS.Helpers
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?>? _executeParam;
        private readonly Action? _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        { _execute = execute; _canExecute = canExecute; }

        public RelayCommand(Action<object?> execute)
        { _executeParam = execute; }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter)
        {
            if (_executeParam != null) _executeParam(parameter);
            else _execute?.Invoke();
        }
    }
}
