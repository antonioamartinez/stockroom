using Stockroom.Presentation.Mvvm;

namespace Stockroom.Tests.Mvvm;

public class ValidatableObjectTests
{
    private sealed class Form : ValidatableObject
    {
        public void Set(string property, params string[] errors) => SetErrors(property, errors);
    }

    [Fact]
    public void Tracks_errors_per_property_and_in_total()
    {
        var form = new Form();

        form.Set("Name", "Required");
        form.Set("Sku", "Too long", "Bad characters");

        Assert.True(form.HasErrors);
        Assert.Equal(["Required"], form.GetErrors("Name"));
        Assert.Equal(3, form.GetErrors(null).Count);
    }

    [Fact]
    public void Clearing_the_last_error_clears_HasErrors_and_notifies()
    {
        var form = new Form();
        form.Set("Name", "Required");
        var changedProperties = new List<string?>();
        var errorsChanged = new List<string?>();
        form.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
        form.ErrorsChanged += (_, e) => errorsChanged.Add(e.PropertyName);

        form.Set("Name");

        Assert.False(form.HasErrors);
        Assert.Equal(["Name"], errorsChanged);
        Assert.Contains(nameof(ValidatableObject.HasErrors), changedProperties);
    }

    [Fact]
    public void Setting_the_same_errors_again_does_not_raise_ErrorsChanged()
    {
        var form = new Form();
        form.Set("Name", "Required");
        var raised = 0;
        form.ErrorsChanged += (_, _) => raised++;

        form.Set("Name", "Required");

        Assert.Equal(0, raised);
    }
}
