using Stockroom.Presentation.Mvvm;

namespace Stockroom.Tests.Mvvm;

public class ObservableObjectTests
{
    private sealed class Person : ObservableObject
    {
        private string _name = "";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }

    [Fact]
    public void Raises_PropertyChanged_with_the_property_name_when_the_value_changes()
    {
        var person = new Person();
        var raised = new List<string?>();
        person.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        person.Name = "Ada";

        Assert.Equal([nameof(Person.Name)], raised);
    }

    [Fact]
    public void Does_not_raise_PropertyChanged_when_the_value_is_the_same()
    {
        var person = new Person { Name = "Ada" };
        var raised = 0;
        person.PropertyChanged += (_, _) => raised++;

        person.Name = "Ada";

        Assert.Equal(0, raised);
    }
}
