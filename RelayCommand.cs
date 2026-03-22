using System;
using System.Windows.Input;

namespace Loto7SmartPicker
{
    /// <summary>
    /// MVVM パターン用の汎用コマンド実装。
    /// CanExecute が変化したときは CommandManager.InvalidateRequerySuggested() を呼ぶか
    /// RaiseCanExecuteChanged() を直接呼び出すこと。
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // CommandManager に委譲することで UI の変化に自動追従させる
        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute == null || _canExecute();

        public void Execute(object? parameter) =>
            _execute();
    }
}
