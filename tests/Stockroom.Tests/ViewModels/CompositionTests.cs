using Microsoft.Extensions.DependencyInjection;
using Stockroom.Presentation.ViewModels;
using Stockroom.Tests.TestDoubles;

namespace Stockroom.Tests.ViewModels;

public class CompositionTests
{
    [Fact]
    public void Every_view_model_resolves_from_the_container()
    {
        // ValidateOnBuild in TestApp already fails on a missing dependency; this also proves lifetimes.
        using var app = new TestApp();

        Assert.Same(app.Services.GetRequiredService<ShellViewModel>(), app.Services.GetRequiredService<ShellViewModel>());
        Assert.NotSame(app.Services.GetRequiredService<ItemEditorViewModel>(), app.Services.GetRequiredService<ItemEditorViewModel>());
        Assert.NotNull(app.Services.GetRequiredService<DashboardViewModel>());
        Assert.NotNull(app.Services.GetRequiredService<ItemListViewModel>());
    }
}
