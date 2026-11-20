# Stockroom

A small WPF inventory app for a workshop stockroom. You keep a list of parts and supplies, receive and issue stock against them, and see what needs reordering.

The app is deliberately small. It exists to show how I structure an MVVM desktop application: view model base classes, commands, validation, navigation, dependency injection and tests, with no code-behind anywhere. Every line is meant to be explainable, which is also why the MVVM plumbing is hand-written instead of pulled from a library.

## Running it

The app needs Windows and the .NET 10 SDK.

```
dotnet run --project src/Stockroom.Wpf
```

Data lives in `%LOCALAPPDATA%\Stockroom\inventory.json`. The first run adds a few sample items. Delete the file to start again.

The tests run on Windows, Linux or macOS:

```
dotnet test
```

## How it is laid out

| Project | Target | What it holds |
| --- | --- | --- |
| `Stockroom.Core` | `net10.0` | The item and stock movement records, the business rules in `InventoryService`, and an in-memory and a JSON file repository. No UI, no packages. |
| `Stockroom.Presentation` | `net10.0` | The MVVM toolkit (`Mvvm/`), navigation, the dialog interface and every view model. It cannot reference WPF, which is the point. |
| `Stockroom.Wpf` | `net10.0-windows` | XAML, the composition root in `Program.cs`, and the WPF implementation of the dialog service. |
| `Stockroom.Tests` | `net10.0` | xUnit tests for the rules, the file store, the toolkit, navigation and the view models. |

CI builds and tests on both Ubuntu and Windows. The Ubuntu run is the proof that nothing below the views depends on WPF.

## Design choices

### No code-behind, including the window and the app

There is no `App.xaml` and not a single `.xaml.cs` file. `Program.cs` builds the service container, merges the theme and view dictionaries into a plain `Application`, loads `ShellWindow.xaml` and sets its `DataContext` to `ShellViewModel`. That file is the composition root, the one place that knows every layer.

None of the XAML files declare `x:Class`. Each view is a `DataTemplate` keyed by its view model type, kept in its own resource dictionary under `Views/`. The shell has a `ContentControl` bound to the current view model, and WPF picks the template. Navigation therefore works with view model types only, and no view model can reach a view.

Events that need to reach the view model go through behaviors from `Microsoft.Xaml.Behaviors.Wpf`: `Loaded` on the window runs `InitializeCommand`, and `MouseDoubleClick` on a grid runs the open or edit command. Keyboard shortcuts use plain `KeyBinding`s.

The cost is that some things a designer would do in code-behind need a behavior or a view model property instead. I think that cost is worth paying because every piece of UI logic ends up somewhere a test can reach it.

### Commands that are honest about async work

`ICommand.Execute` returns `void`, so async commands are usually `async void` lambdas that lose exceptions and allow double submits. `AsyncRelayCommand` fixes both:

- It exposes `IsRunning` and returns false from `CanExecute` while running, so a double click on Save cannot write twice.
- Its one `async void` method wraps `ExecuteAsync`, which returns the real task. Tests await that task instead of sleeping.
- Exceptions go to an error handler passed in by the view model (every view model uses the dialog service for this). If no handler is given the exception rethrows on the UI thread, where it is loud.

Commands raise `CanExecuteChanged` themselves through `RaiseCanExecuteChanged`. WPF's `CommandManager.RequerySuggested` would do it automatically, but it lives in WPF and re-queries every command on every input event. Raising the event explicitly keeps `Stockroom.Presentation` free of WPF and makes the timing something a test can assert.

### Validation belongs to the view model

`ValidatableObject` implements `INotifyDataErrorInfo`. WPF's binding engine picks the errors up and the theme shows the message under the field. The view model owns the rules, and the same errors are visible to tests through `GetErrors`.

Numeric inputs such as reorder level and unit cost are `string` properties on the editor. If they were `int` and `decimal`, typing "abc" would fail inside the binding converter, the view model would never hear about it, and Save could still be enabled. As strings, bad input becomes an ordinary validation error and Save stays disabled.

A new form opens without errors. Fields are checked as they change, and untouched fields are only checked when Save is pressed. Rules that need the data, such as a unique SKU, live in `InventoryService`. It returns an `OperationResult` naming the field at fault, and the editor shows that message on the field like any other validation error.

### Smaller choices worth asking about

**Navigation remembers types, not instances.** The back stack holds the view model type and its parameter. Going back resolves a fresh view model, so it shows current data and no stale page stays in memory. A page can block navigation by implementing `INavigationGuard`, which the editor uses to ask before discarding unsaved changes.

**Quantity on hand only changes through stock movements.** Saving an existing item keeps the stored quantity whatever the form sends. A new item's starting quantity is recorded as an opening balance. The movement history therefore always explains the number on screen.

**Expected failures are results, exceptions are for the unexpected.** Issuing more stock than is on hand returns a failed `OperationResult`. A disk error throws, and the command's error handler reports it.

**Items are immutable records.** Lists bind straight to `InventoryItem` because nothing can change one in place. Edits go through the editor view model and a `with` expression.

**The JSON store writes to a temporary file and swaps it in.** A crash during a write leaves the previous file intact. If a write fails, the cached copy is dropped so the next read reloads from disk.

**Tests use the real service.** View model tests build the same container the app builds, with the file store, clock and dialogs replaced by an in-memory repository, a fixed clock and a fake that records what it was asked. They drive the app through navigation the way a user would, rather than checking which mock was called.

**The toolkit is hand-written.** On a team I would most likely use CommunityToolkit.Mvvm and its source generators. Here the mechanics are the point, and the whole toolkit is under 250 lines in `src/Stockroom.Presentation/Mvvm`.

## Known limits

- The file store is single-user. The unique SKU check reads then writes, which is fine for one desktop process and wrong for several.
- Filtering clears and refills an `ObservableCollection`. That is fine for hundreds of items. For tens of thousands I would swap the collection in one change and rely on the grid's row virtualisation.
- There is no undo, no import or export, and no UI automation tests. View model tests cover the behaviour; nothing checks the XAML at runtime except running the app.

## License

MIT. See `LICENSE`.
