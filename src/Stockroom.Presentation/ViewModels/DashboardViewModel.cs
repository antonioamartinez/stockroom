using System.Collections.ObjectModel;
using Stockroom.Core.Models;
using Stockroom.Core.Services;
using Stockroom.Presentation.Mvvm;
using Stockroom.Presentation.Navigation;
using Stockroom.Presentation.Services;

namespace Stockroom.Presentation.ViewModels;

/// <summary>Totals across the inventory and the items that need reordering, most urgent first.</summary>
public sealed class DashboardViewModel : ObservableObject, INavigationAware
{
    private readonly InventoryService _inventory;
    private readonly INavigationService _navigation;
    private int _itemCount;
    private int _totalUnits;
    private decimal _stockValue;
    private InventoryItem? _selectedLowStockItem;

    public DashboardViewModel(InventoryService inventory, INavigationService navigation, IDialogService dialogs)
    {
        _inventory = inventory;
        _navigation = navigation;
        OpenSelectedCommand = new AsyncRelayCommand(OpenSelectedAsync, () => SelectedLowStockItem is not null, dialogs.ErrorHandler());
    }

    public int ItemCount
    {
        get => _itemCount;
        private set => SetProperty(ref _itemCount, value);
    }

    public int TotalUnits
    {
        get => _totalUnits;
        private set => SetProperty(ref _totalUnits, value);
    }

    public decimal StockValue
    {
        get => _stockValue;
        private set => SetProperty(ref _stockValue, value);
    }

    public ObservableCollection<InventoryItem> LowStockItems { get; } = [];

    public bool HasLowStock => LowStockItems.Count > 0;

    public InventoryItem? SelectedLowStockItem
    {
        get => _selectedLowStockItem;
        set
        {
            if (SetProperty(ref _selectedLowStockItem, value))
            {
                OpenSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand OpenSelectedCommand { get; }

    public async Task OnNavigatedToAsync(object? parameter)
    {
        var items = await _inventory.GetItemsAsync();

        ItemCount = items.Count;
        TotalUnits = items.Sum(i => i.QuantityOnHand);
        StockValue = items.Sum(i => i.StockValue);

        LowStockItems.Clear();
        foreach (var item in items.Where(i => i.IsLowStock).OrderBy(i => i.QuantityOnHand - i.ReorderLevel))
        {
            LowStockItems.Add(item);
        }

        OnPropertyChanged(nameof(HasLowStock));
    }

    private Task OpenSelectedAsync() =>
        SelectedLowStockItem is { } item
            ? _navigation.NavigateToAsync<ItemEditorViewModel>(item.Id)
            : Task.CompletedTask;
}
