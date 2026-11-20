using Microsoft.Extensions.DependencyInjection;
using Stockroom.Presentation.Navigation;

namespace Stockroom.Tests.Navigation;

public class NavigationServiceTests
{
    private sealed class PageA : INavigationAware
    {
        public object? Parameter { get; private set; }

        public Task OnNavigatedToAsync(object? parameter)
        {
            Parameter = parameter;
            return Task.CompletedTask;
        }
    }

    private sealed class PageB : INavigationGuard
    {
        public bool AllowLeave { get; set; } = true;

        public bool CanNavigateAway() => AllowLeave;
    }

    private sealed class Unregistered;

    private static NavigationService Create() =>
        new(new ServiceCollection().AddTransient<PageA>().AddTransient<PageB>().BuildServiceProvider());

    [Fact]
    public async Task Navigating_shows_a_resolved_view_model_and_passes_the_parameter()
    {
        var navigation = Create();

        await navigation.NavigateToAsync<PageA>(42);

        Assert.Equal(42, Assert.IsType<PageA>(navigation.Current).Parameter);
        Assert.False(navigation.CanGoBack);
    }

    [Fact]
    public async Task Going_back_recreates_the_previous_page_with_its_parameter()
    {
        var navigation = Create();
        await navigation.NavigateToAsync<PageA>("first");
        var firstInstance = navigation.Current;
        await navigation.NavigateToAsync<PageB>();

        Assert.True(navigation.CanGoBack);
        Assert.True(await navigation.GoBackAsync());

        var page = Assert.IsType<PageA>(navigation.Current);
        Assert.NotSame(firstInstance, page);
        Assert.Equal("first", page.Parameter);
        Assert.False(navigation.CanGoBack);
    }

    [Fact]
    public async Task A_guard_that_refuses_keeps_the_user_on_the_page()
    {
        var navigation = Create();
        await navigation.NavigateToAsync<PageA>();
        await navigation.NavigateToAsync<PageB>();
        var guarded = Assert.IsType<PageB>(navigation.Current);
        guarded.AllowLeave = false;

        Assert.False(await navigation.NavigateToAsync<PageA>());
        Assert.False(await navigation.GoBackAsync());
        Assert.Same(guarded, navigation.Current);
    }

    [Fact]
    public async Task Navigating_to_the_page_already_shown_reloads_it_without_adding_history()
    {
        var navigation = Create();
        await navigation.NavigateToAsync<PageA>();
        var first = navigation.Current;

        await navigation.NavigateToAsync<PageA>();

        Assert.NotSame(first, navigation.Current);
        Assert.False(navigation.CanGoBack);
    }

    [Fact]
    public async Task Navigating_to_an_unregistered_view_model_fails_with_a_clear_message()
    {
        var navigation = Create();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => navigation.NavigateToAsync<Unregistered>());

        Assert.Contains(nameof(Unregistered), error.Message);
    }
}
