using System.Windows;
using TextTop.Desktop.ViewModels;

namespace TextTop.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.LoginSucceeded += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.LoginCommand.CanExecute(PasswordInput.Password))
        {
            _viewModel.LoginCommand.Execute(PasswordInput.Password);
        }
    }
}
