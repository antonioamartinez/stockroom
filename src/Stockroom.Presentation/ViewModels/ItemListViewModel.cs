using System.Collections.ObjectModel;
using Stockroom.Core.Models;
using Stockroom.Core.Services;
using Stockroom.Presentation.Mvvm;
using Stockroom.Presentation.Navigation;
using Stockroom.Presentation.Services;

namespace Stockroom.Presentation.ViewModels;

/// <summary>
/// Searchable list of items. Filtering happens here rather than in a WPF CollectionView, so it is
/// plain code with tests and no dependency on WPF.
/// </summary>
public sealed class ItemListViewModel : ObservableObject, INavigationAware
{
    private readonly InventoryService _inventory;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private IReadOnlyList<InventoryItem> _allItems = [];
    private string _searchText = "";
    private bool _lowStockOnly;
    private InventoryItem? _selectedItem;

    public ItemListViewModel(InventoryService inventory, INavigationService navigation, IDialogService dialogs)
    {
        _inventory = inventory;
        _navigation = navigation;
        _dialogs = dialogs;
        var onError = dialogs.ErrorHandler();

        AddCommand = new AsyncRelayCommand(() => navigation.NavigateToAsync<ItemEditorViewModel>(), onError: onError);
        EditCommand = new AsyncRelayCommand(EditSelectedAsync, () => SelectedItem is not null, onError);
        DeleteCommand = new AsyncRelayCommand(DeleteSelectedAsync, () => SelectedItem is not null, onError);
        RefreshCommand = new AsyncRelayCommand(LoadAsync, onError: onError);
        ClearSearchCommand = new RelayCommand(() => SearchText = "");
    }

    public ObservableCollection<InventoryItem> Items { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool LowStockOnly
    {
        get => _lowStockOnly;
        set
        {
            if (SetProperty(ref _lowStockOnly, value))
            {
                ApplyFilter();
            }
        }
    }

    public InventoryItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Summary => $"Showing {Items.Count} of {_allItems.Count} items";

    public AsyncRelayCommand AddCommand { get; }

    public AsyncRelayCommand EditCommand { get; }

    public AsyncRelayCommand DeleteCommand { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public RelayCommand ClearSearchCommand { get; }

    public Task OnNavigatedToAsync(object? parameter) => LoadAsync();

    private async Task LoadAsync()
    {
        _allItems = await _inventory.GetItemsAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var selectedId = SelectedItem?.Id;
        var search = SearchText.Trim();

        Items.Clear();
        foreach (var item in _allItems.Where(i => Matches(i, search) && (!LowStockOnly || i.IsLowStock)))
        {
            Items.Add(item);
        }

        SelectedItem = Items.FirstOrDefault(i => i.Id == selectedId);
        OnPropertyChanged(nameof(Summary));
    }

    private static bool Matches(InventoryItem item, string search) =>
        search.Length == 0
        || item.Sku.Contains(search, StringComparison.OrdinalIgnoreCase)
        || item.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
        || item.Category.Contains(search, StringComparison.OrdinalIgnoreCase);

    private Task EditSelectedAsync() =>
        SelectedItem is { } item
            ? _navigation.NavigateToAsync<ItemEditorViewModel>(item.Id)
            : Task.CompletedTask;

    private async Task DeleteSelectedAsync()
    {
        if (SelectedItem is not { } item)
        {
            return;
        }

        if (!_dialogs.Confirm("Delete item", $"Delete {item.Name} ({item.Sku}) and its stock history? This cannot be undone."))
        {
            return;
        }

        await _inventory.DeleteItemAsync(item.Id);
        await LoadAsync();
    }
}
