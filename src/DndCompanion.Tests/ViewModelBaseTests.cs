using DndCompanion.Core.Mvvm;

namespace DndCompanion.Tests;

public sealed class ViewModelBaseTests
{
    private sealed class TestViewModel : ViewModelBase
    {
        private string? _name;

        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool TrySetName(string? value) => SetProperty(ref _name, value, nameof(Name));
    }

    [Fact]
    public void SetProperty_RaisesPropertyChanged_WhenValueChanges()
    {
        var viewModel = new TestViewModel();
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.Name = "Arkon";

        Assert.Contains(nameof(TestViewModel.Name), changedProperties);
    }

    [Fact]
    public void SetProperty_DoesNotRaise_WhenValueIsUnchanged()
    {
        var viewModel = new TestViewModel { Name = "Arkon" };
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.Name = "Arkon";

        Assert.Empty(changedProperties);
    }

    [Fact]
    public void SetProperty_ReturnsTrue_WhenValueChanges_AndFalse_WhenUnchanged()
    {
        var viewModel = new TestViewModel();

        var firstChange = viewModel.TrySetName("Arkon");
        var secondChange = viewModel.TrySetName("Arkon");

        Assert.True(firstChange);
        Assert.False(secondChange);
    }
}