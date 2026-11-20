using Stockroom.Presentation.Mvvm;

namespace Stockroom.Tests.Mvvm;

public class RelayCommandTests
{
    [Fact]
    public void Execute_does_nothing_when_CanExecute_is_false()
    {
        var runs = 0;
        var command = new RelayCommand(() => runs++, () => false);

        command.Execute(null);

        Assert.Equal(0, runs);
    }

    [Fact]
    public void RaiseCanExecuteChanged_raises_the_event()
    {
        var command = new RelayCommand(() => { });
        var raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;

        command.RaiseCanExecuteChanged();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Typed_command_passes_its_parameter_through()
    {
        string? received = null;
        var command = new RelayCommand<string>(value => received = value);

        command.Execute("BOLT-M6");

        Assert.Equal("BOLT-M6", received);
    }

    [Fact]
    public void Typed_command_is_unavailable_for_a_parameter_of_the_wrong_type()
    {
        var command = new RelayCommand<int>(_ => { });

        Assert.False(command.CanExecute("not a number"));
        Assert.False(command.CanExecute(null));
        Assert.True(command.CanExecute(3));
    }
}
