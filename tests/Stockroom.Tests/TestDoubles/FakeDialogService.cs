using Stockroom.Presentation.Services;

namespace Stockroom.Tests.TestDoubles;

/// <summary>Answers confirmations with <see cref="ConfirmResult"/> and records everything shown.</summary>
public sealed class FakeDialogService : IDialogService
{
    public bool ConfirmResult { get; set; } = true;

    public List<string> Confirmations { get; } = [];

    public List<string> Errors { get; } = [];

    public bool Confirm(string title, string message)
    {
        Confirmations.Add(message);
        return ConfirmResult;
    }

    public void ShowError(string title, string message) => Errors.Add(message);
}
