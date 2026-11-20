namespace Stockroom.Presentation.Mvvm;

/// <summary>A synchronous command without a parameter.</summary>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : IRelayCommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            execute();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// A synchronous command with a typed parameter. A parameter of the wrong type makes the command
/// unavailable instead of throwing an InvalidCastException from inside a binding.
/// </summary>
public sealed class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : IRelayCommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) =>
        TryGetParameter(parameter, out var value) && (canExecute?.Invoke(value) ?? true);

    public void Execute(object? parameter)
    {
        if (TryGetParameter(parameter, out var value) && (canExecute?.Invoke(value) ?? true))
        {
            execute(value);
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private static bool TryGetParameter(object? parameter, out T? value)
    {
        if (parameter is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return parameter is null && default(T) is null;
    }
}
