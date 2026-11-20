using System.Collections.ObjectModel;
using Stockroom.Core.Models;
using Stockroom.Core.Services;
using Stockroom.Presentation.Mvvm;
using Stockroom.Presentation.Services;

namespace Stockroom.Presentation.ViewModels;

/// <summary>
/// The stock panel on the item editor: receive or issue stock and see the history. A separate view
/// model so its errors never block saving the item's details, and so it can be tested on its own.
/// </summary>
public sealed class StockAdjustmentViewModel : ValidatableObject
{
    private readonly InventoryService _inventory;
    private readonly Guid _itemId;
    private readonly int _reorderLevel;
    private int _quantityOnHand;
    private string _quantity = "";
    private string _reason = "";

    public StockAdjustmentViewModel(InventoryService inventory, InventoryItem item, IDialogService dialogs)
    {
        _inventory = inventory;
        _itemId = item.Id;
        _reorderLevel = item.ReorderLevel;
        _quantityOnHand = item.QuantityOnHand;

        var onError = dialogs.ErrorHandler();
        ReceiveCommand = new AsyncRelayCommand(() => AdjustAsync(+1), CanAdjust, onError);
        IssueCommand = new AsyncRelayCommand(() => AdjustAsync(-1), CanAdjust, onError);
    }

    public int QuantityOnHand
    {
        get => _quantityOnHand;
        private set
        {
            if (SetProperty(ref _quantityOnHand, value))
            {
                OnPropertyChanged(nameof(IsLowStock));
            }
        }
    }

    public bool IsLowStock => QuantityOnHand <= _reorderLevel;

    /// <summary>How many to receive or issue. Text, so a typo is a validation error rather than a binding failure.</summary>
    public string Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                SetError(nameof(Quantity), ValidateQuantity(value));
                RefreshCommands();
            }
        }
    }

    public string Reason
    {
        get => _reason;
        set
        {
            if (SetProperty(ref _reason, value))
            {
                RefreshCommands();
            }
        }
    }

    public ObservableCollection<StockMovement> Movements { get; } = [];

    public AsyncRelayCommand ReceiveCommand { get; }

    public AsyncRelayCommand IssueCommand { get; }

    public async Task LoadAsync()
    {
        var movements = await _inventory.GetMovementsAsync(_itemId);
        Movements.Clear();
        foreach (var movement in movements)
        {
            Movements.Add(movement);
        }
    }

    private static string? ValidateQuantity(string text) =>
        string.IsNullOrWhiteSpace(text) || (InputParsing.TryParseWholeNumber(text, out var value) && value > 0)
            ? null
            : "Enter a whole number above 0.";

    private bool CanAdjust() =>
        InputParsing.TryParseWholeNumber(Quantity, out var value) && value > 0 && !string.IsNullOrWhiteSpace(Reason);

    private async Task AdjustAsync(int direction)
    {
        if (!InputParsing.TryParseWholeNumber(Quantity, out var quantity))
        {
            return;
        }

        var result = await _inventory.AdjustStockAsync(_itemId, direction * quantity, Reason);
        if (!result.Succeeded)
        {
            SetError(nameof(Quantity), result.Error);
            return;
        }

        QuantityOnHand = result.Value!.QuantityOnHand;
        Quantity = "";
        Reason = "";
        await LoadAsync();
    }

    private void RefreshCommands()
    {
        ReceiveCommand.RaiseCanExecuteChanged();
        IssueCommand.RaiseCanExecuteChanged();
    }
}
