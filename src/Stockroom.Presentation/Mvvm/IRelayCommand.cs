using System.Windows.Input;

namespace Stockroom.Presentation.Mvvm;

/// <summary>
/// A command whose owner says when <see cref="ICommand.CanExecute"/> may have changed.
/// WPF's CommandManager.RequerySuggested would do this automatically, but it lives in WPF and
/// re-queries every command on every input event. Raising the event explicitly keeps this layer
/// free of WPF and makes the timing testable.
/// </summary>
public interface IRelayCommand : ICommand
{
    void RaiseCanExecuteChanged();
}
