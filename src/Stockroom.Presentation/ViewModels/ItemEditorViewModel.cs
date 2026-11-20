using Stockroom.Core.Models;
using Stockroom.Core.Services;
using Stockroom.Presentation.Mvvm;
using Stockroom.Presentation.Navigation;
using Stockroom.Presentation.Services;

namespace Stockroom.Presentation.ViewModels;

/// <summary>
/// Creates or edits one item. Fields are validated as they change; untouched fields on a new form
/// are only checked when the user tries to save, so a blank form does not open covered in errors.
/// Leaving with unsaved changes asks first.
/// </summary>
public sealed class ItemEditorViewModel : ValidatableObject, INavigationAware, INavigationGuard
{
    private const int MaxSkuLength = 20;
    private const int MaxNameLength = 80;
    private const int MaxCategoryLength = 40;

    private readonly InventoryService _inventory;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;

    private Guid _itemId = Guid.NewGuid();
    private bool _isNew = true;
    private bool _isDirty;
    private bool _isLoading;
    private string _sku = "";
    private string _name = "";
    private string _category = "";
    private string _reorderLevel = "0";
    private string _unitCost = "";
    private string _openingQuantity = "0";
    private StockAdjustmentViewModel? _stock;

    public ItemEditorViewModel(InventoryService inventory, INavigationService navigation, IDialogService dialogs)
    {
        _inventory = inventory;
        _navigation = navigation;
        _dialogs = dialogs;
        var onError = dialogs.ErrorHandler();

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => IsDirty && !HasErrors, onError);
        CancelCommand = new AsyncRelayCommand(CloseAsync, onError: onError);
    }

    public bool IsNew
    {
        get => _isNew;
        private set
        {
            if (SetProperty(ref _isNew, value))
            {
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    public string Title => IsNew ? "New item" : "Edit item";

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Sku
    {
        get => _sku;
        set => SetField(ref _sku, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string Category
    {
        get => _category;
        set => SetField(ref _category, value);
    }

    public string ReorderLevel
    {
        get => _reorderLevel;
        set => SetField(ref _reorderLevel, value);
    }

    public string UnitCost
    {
        get => _unitCost;
        set => SetField(ref _unitCost, value);
    }

    /// <summary>Only used for a new item. After that, quantity changes through the stock panel.</summary>
    public string OpeningQuantity
    {
        get => _openingQuantity;
        set => SetField(ref _openingQuantity, value);
    }

    /// <summary>The stock panel. Null for a new item, which has no stock history yet.</summary>
    public StockAdjustmentViewModel? Stock
    {
        get => _stock;
        private set => SetProperty(ref _stock, value);
    }

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand CancelCommand { get; }

    public async Task OnNavigatedToAsync(object? parameter)
    {
        if (parameter is not Guid id)
        {
            return;
        }

        var item = await _inventory.GetItemAsync(id);
        if (item is null)
        {
            _dialogs.ShowError("Item not found", "This item no longer exists. It may have been deleted.");
            await _navigation.GoBackAsync();
            return;
        }

        Load(item);
        Stock = new StockAdjustmentViewModel(_inventory, item, _dialogs);
        await Stock.LoadAsync();
    }

    public bool CanNavigateAway() =>
        !IsDirty || _dialogs.Confirm("Discard changes?", "You have unsaved changes to this item. Leave and discard them?");

    protected override void OnErrorsChanged() => SaveCommand.RaiseCanExecuteChanged();

    private void SetField(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return;
        }

        Validate(propertyName);
        if (!_isLoading)
        {
            IsDirty = true;
        }
    }

    private void Load(InventoryItem item)
    {
        _isLoading = true;
        try
        {
            _itemId = item.Id;
            IsNew = false;
            Sku = item.Sku;
            Name = item.Name;
            Category = item.Category;
            ReorderLevel = InputParsing.Format(item.ReorderLevel);
            UnitCost = InputParsing.Format(item.UnitCost);
        }
        finally
        {
            _isLoading = false;
        }

        IsDirty = false;
    }

    private void Validate(string propertyName)
    {
        var error = propertyName switch
        {
            nameof(Sku) => ValidateSku(Sku),
            nameof(Name) => ValidateText(Name, "Name", MaxNameLength, required: true),
            nameof(Category) => ValidateText(Category, "Category", MaxCategoryLength, required: false),
            nameof(ReorderLevel) => ValidateWholeNumber(ReorderLevel),
            nameof(UnitCost) => InputParsing.TryParseAmount(UnitCost, out var cost) && cost >= 0
                ? null
                : "Enter a cost of 0 or more.",
            nameof(OpeningQuantity) => IsNew ? ValidateWholeNumber(OpeningQuantity) : null,
            _ => null,
        };

        SetError(propertyName, error);
    }

    private void ValidateAll()
    {
        foreach (var property in new[] { nameof(Sku), nameof(Name), nameof(Category), nameof(ReorderLevel), nameof(UnitCost), nameof(OpeningQuantity) })
        {
            Validate(property);
        }
    }

    private static string? ValidateSku(string sku) =>
        ValidateText(sku, "SKU", MaxSkuLength, required: true)
        ?? (sku.Trim().All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ? null : "SKU can only contain letters, digits and dashes.");

    private static string? ValidateText(string value, string label, int maxLength, bool required)
    {
        var trimmed = value.Trim();
        if (required && trimmed.Length == 0)
        {
            return $"{label} is required.";
        }

        return trimmed.Length > maxLength ? $"{label} must be {maxLength} characters or fewer." : null;
    }

    private static string? ValidateWholeNumber(string text) =>
        InputParsing.TryParseWholeNumber(text, out var value) && value >= 0 ? null : "Enter a whole number, 0 or more.";

    private async Task SaveAsync()
    {
        ValidateAll();
        if (HasErrors)
        {
            return;
        }

        InputParsing.TryParseWholeNumber(ReorderLevel, out var reorderLevel);
        InputParsing.TryParseAmount(UnitCost, out var unitCost);
        InputParsing.TryParseWholeNumber(OpeningQuantity, out var openingQuantity);

        var item = new InventoryItem
        {
            Id = _itemId,
            Sku = Sku,
            Name = Name,
            Category = Category,
            ReorderLevel = reorderLevel,
            UnitCost = unitCost,
            QuantityOnHand = IsNew ? openingQuantity : 0,
        };

        var result = await _inventory.SaveItemAsync(item);
        if (!result.Succeeded)
        {
            ShowSaveError(result);
            return;
        }

        IsDirty = false;
        await CloseAsync();
    }

    private void ShowSaveError(OperationResult<InventoryItem> result)
    {
        // Business rule failures land on the field they are about, like any other validation error.
        var field = result.MemberName switch
        {
            nameof(InventoryItem.Sku) => nameof(Sku),
            nameof(InventoryItem.QuantityOnHand) => nameof(OpeningQuantity),
            _ => null,
        };

        if (field is null)
        {
            _dialogs.ShowError("Could not save", result.Error ?? "The item could not be saved.");
        }
        else
        {
            SetError(field, result.Error);
        }
    }

    private Task CloseAsync() =>
        _navigation.CanGoBack ? _navigation.GoBackAsync() : _navigation.NavigateToAsync<ItemListViewModel>();
}
