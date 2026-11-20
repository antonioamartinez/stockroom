namespace Stockroom.Presentation.Mvvm;

/// <summary>
/// A command that runs async work.
/// <list type="bullet">
/// <item>It reports <see cref="IsRunning"/> and cannot execute again while running, so a double click cannot save twice.</item>
/// <item><see cref="ExecuteAsync"/> returns the task, so tests await the real work instead of sleeping.</item>
/// <item>ICommand.Execute must return void, so <see cref="Execute"/> is the one async void method. Exceptions go to
/// the error handler when one is supplied; without one they rethrow on the UI thread and crash loudly rather than vanish.</item>
/// </list>
/// </summary>
public sealed class AsyncRelayCommand : ObservableObject, IRelayCommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception>? _onError;
    private bool _isRunning;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onError = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        _onError = onError;
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter) => await ExecuteAsync();

    public async Task ExecuteAsync()
    {
        if (!CanExecute(null))
        {
            return;
        }

        IsRunning = true;
        try
        {
            await _execute();
        }
        catch (Exception ex) when (_onError is not null)
        {
            _onError(ex);
        }
        finally
        {
            IsRunning = false;
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
