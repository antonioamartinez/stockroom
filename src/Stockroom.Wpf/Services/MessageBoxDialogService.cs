using System.Windows;
using Stockroom.Presentation.Services;

namespace Stockroom.Wpf.Services;

/// <summary>The real dialogs, owned by the main window so they stay on top of it.</summary>
internal sealed class MessageBoxDialogService : IDialogService
{
    public bool Confirm(string title, string message) =>
        Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

    public void ShowError(string title, string message) =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);

    private static MessageBoxResult Show(
        string message, string title, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult defaultResult)
    {
        var owner = Application.Current?.MainWindow;
        return owner is null
            ? MessageBox.Show(message, title, buttons, image, defaultResult)
            : MessageBox.Show(owner, message, title, buttons, image, defaultResult);
    }
}
