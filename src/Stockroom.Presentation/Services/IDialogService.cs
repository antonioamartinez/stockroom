namespace Stockroom.Presentation.Services;

/// <summary>
/// Message boxes behind an interface. The WPF app shows real dialogs; tests use a fake that
/// records what was asked and answers on cue.
/// </summary>
public interface IDialogService
{
    bool Confirm(string title, string message);

    void ShowError(string title, string message);
}

public static class DialogServiceExtensions
{
    /// <summary>The handler every async command uses for unexpected failures.</summary>
    public static Action<Exception> ErrorHandler(this IDialogService dialogs) =>
        ex => dialogs.ShowError("Something went wrong", ex.Message);
}
