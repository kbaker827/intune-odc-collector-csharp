using System.ComponentModel;
using System.Windows;
using IntuneODCCollector.ViewModels;

namespace IntuneODCCollector;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;
        vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.OutputLog))
            OutputScroller.ScrollToBottom();
    }
}
