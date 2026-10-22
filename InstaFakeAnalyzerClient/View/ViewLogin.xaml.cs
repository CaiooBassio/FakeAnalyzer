using InstaFakeAnalyzerClient.ApiServices;
using InstaFakeAnalyzerClient.Models;
using InstaFakeAnalyzerClient.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace InstaFakeAnalyzerClient.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ViewLogin : Window
    {
        public ViewLogin(LoginVM vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.RequestClose += () => this.Close();
        }

        private void txtSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            placeholderSenha.Visibility = string.IsNullOrWhiteSpace(txtSenha.Password)
            ? Visibility.Visible
            : Visibility.Collapsed;
            if (DataContext is LoginVM vm)
            {
                vm.Senha = ((PasswordBox)sender).Password;
            }
        }

        private void txtUsuario_TextChanged(object sender, TextChangedEventArgs e)
        {
            placeholderUsuario.Visibility = string.IsNullOrWhiteSpace(txtUsuario.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}