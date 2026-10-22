using InstaFakeAnalyzerClient.ApiServices;
using InstaFakeAnalyzerClient.Models;
using InstaFakeAnalyzerClient.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace InstaFakeAnalyzerClient.View
{
    /// <summary>
    /// Lógica interna para ViewCadastroUsuario.xaml
    /// </summary>
    public partial class ViewCadastroUsuario : Window
    {
        public ViewCadastroUsuario(CadastroVM vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.RequestClose += () => this.Close();
        }

        private void txtSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is CadastroVM vm)
            {
                vm.Senha = ((PasswordBox)sender).Password;
            }
        }

    }
}
