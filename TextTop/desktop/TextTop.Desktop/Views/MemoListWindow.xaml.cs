using System.Windows;
using System.Windows.Input;
using TextTop.Desktop.ViewModels;

namespace TextTop.Desktop.Views;

public partial class MemoListWindow : Window
{
    private readonly MemoListViewModel _viewModel;

    public MemoListWindow(MemoListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void MemoList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.OpenCommand.CanExecute(null))
        {
            _viewModel.OpenCommand.Execute(null);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
