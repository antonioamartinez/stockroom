using Stockroom.Presentation.Mvvm;

namespace Stockroom.Tests.Mvvm;

public class AsyncRelayCommandTests
{
    [Fact]
    public async Task Cannot_run_again_while_already_running()
    {
        var gate = new TaskCompletionSource();
        var runs = 0;
        var command = new AsyncRelayCommand(async () =>
        {
            runs++;
            await gate.Task;
        });

        var first = command.ExecuteAsync();
        Assert.True(command.IsRunning);
        Assert.False(command.CanExecute(null));

        await command.ExecuteAsync();
        gate.SetResult();
        await first;

        Assert.Equal(1, runs);
        Assert.False(command.IsRunning);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task Raises_CanExecuteChanged_when_it_starts_and_when_it_finishes()
    {
        var gate = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => gate.Task);
        var raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;

        var running = command.ExecuteAsync();
        Assert.Equal(1, raised);
        gate.SetResult();
        await running;

        Assert.Equal(2, raised);
    }

    [Fact]
    public async Task Sends_exceptions_to_the_error_handler_and_resets_IsRunning()
    {
        Exception? handled = null;
        var command = new AsyncRelayCommand(
            () => throw new InvalidOperationException("disk full"),
            onError: ex => handled = ex);

        await command.ExecuteAsync();

        Assert.Equal("disk full", handled?.Message);
        Assert.False(command.IsRunning);
    }

    [Fact]
    public async Task Rethrows_when_there_is_no_error_handler()
    {
        var command = new AsyncRelayCommand(() => throw new InvalidOperationException("disk full"));

        await Assert.ThrowsAsync<InvalidOperationException>(command.ExecuteAsync);
        Assert.False(command.IsRunning);
    }

    [Fact]
    public async Task Respects_the_can_execute_predicate()
    {
        var runs = 0;
        var command = new AsyncRelayCommand(() => { runs++; return Task.CompletedTask; }, () => false);

        await command.ExecuteAsync();

        Assert.Equal(0, runs);
    }
}
