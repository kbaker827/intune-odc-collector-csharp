using System.Windows.Input;
using IntuneODCCollector.ViewModels;
using Xunit;

namespace IntuneODCCollector.Tests;

public class RelayCommandTests
{
    [Fact]
    public void Execute_CallsProvidedAction()
    {
        bool called = false;
        ICommand cmd = new RelayCommand(_ => called = true);

        cmd.Execute(null);

        Assert.True(called);
    }

    [Fact]
    public void CanExecute_ReturnsTrueByDefault()
    {
        ICommand cmd = new RelayCommand(_ => { });

        Assert.True(cmd.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ReturnsResultOfProvidedPredicate()
    {
        bool allow = false;
        ICommand cmd = new RelayCommand(_ => { }, _ => allow);

        Assert.False(cmd.CanExecute(null));

        allow = true;
        Assert.True(cmd.CanExecute(null));
    }

    [Fact]
    public void Execute_PassesParameterToAction()
    {
        object? received = null;
        ICommand cmd = new RelayCommand(p => received = p);

        cmd.Execute("hello");

        Assert.Equal("hello", received);
    }
}
