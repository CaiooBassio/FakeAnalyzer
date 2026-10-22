using GalaSoft.MvvmLight.Command;
using InstaFakeAnalyzerClient.ApiServices;
using InstaFakeAnalyzerClient.Models;
using InstaFakeAnalyzerClient.View;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace InstaFakeAnalyzerClient.ViewModel
{
    public class DashboardVM : INotifyPropertyChanged
    {
        private readonly UsuarioService _usuarioService;

        public DashboardVM(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
            CadastrarCommand = new RelayCommand(Cadastrar);
            VerificarCommand = new RelayCommand(Verificar);
            FalarComChatBotCommand = new RelayCommand(FalarComChatBot);
            EnviarNoticiaCommand = new RelayCommand(EnviarNoticia);
            LogoutCommand = new RelayCommand(async () => await Logout());
            LoadUsuario();
        }

        public RelayCommand CadastrarCommand { get; }
        public RelayCommand VerificarCommand { get; }
        public RelayCommand FalarComChatBotCommand { get; }
        public RelayCommand EnviarNoticiaCommand { get; }
        public RelayCommand LogoutCommand { get; }

       
        private void Cadastrar()
        {
            var telaCadastro = App.ServiceProvider.GetRequiredService<ViewCadastroUsuario>();
            telaCadastro.Show();
            RequestClose.Invoke();
        }

        private void Verificar()
        {
            var telaVerificarNoticias = App.ServiceProvider.GetRequiredService<ViewVerificarNoticias>();
            telaVerificarNoticias.Show();
            RequestClose.Invoke();
        }

        private void FalarComChatBot()
        {
            var telaChatBot = App.ServiceProvider.GetRequiredService<ViewChatbot>();
            telaChatBot.Show();
            RequestClose.Invoke();
        }

        private void EnviarNoticia()
        {
            var telaInserirNoticia = App.ServiceProvider.GetRequiredService<ViewInserirNoticia>();
            telaInserirNoticia.Show();
            RequestClose.Invoke();
        }
        private async Task Logout()
        {
            var authService = App.ServiceProvider.GetRequiredService<AuthService>();
            await authService.FazerLogoutAsync();
            var loginWindow = App.ServiceProvider.GetRequiredService<ViewLogin>();
            loginWindow.Show();
            RequestClose.Invoke();
        }

        private async void LoadUsuario()
        {
            Usuario usuarioLogado = await _usuarioService.ObterUsuarioLogadoAsync();
            if (usuarioLogado != null)
            {
                Saudacao = $"Bem-vindo, {usuarioLogado.Nome}!";

                switch (usuarioLogado.tipoUsuario)
                {
                    case Usuario.TipoUsuario.Administrador:
                        CadastrarVisibility = Visibility.Visible;
                        VerificarVisibility = Visibility.Visible;
                        break;
                    case Usuario.TipoUsuario.Verificador:
                        CadastrarVisibility = Visibility.Collapsed;
                        VerificarVisibility = Visibility.Visible;
                        break;
                    default:
                        CadastrarVisibility = Visibility.Collapsed;
                        VerificarVisibility = Visibility.Collapsed;
                        break;
                }
            }
            else
            {
                MessageBox.Show("Erro ao carregar usuário logado. Verifique se o login foi efetuado corretamente.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                var loginWindow = App.ServiceProvider.GetRequiredService<ViewLogin>();
                loginWindow.Show();
                RequestClose.Invoke();
            }
            
        }

        private string _saudacao = "";
        public string Saudacao
        {
            get => _saudacao;
            set
            {
                _saudacao = value;
                OnPropertyChanged();
            }
        }

        private Visibility _cadastrarVisibility = Visibility.Collapsed;
        public Visibility CadastrarVisibility
        {
            get => _cadastrarVisibility;
            set
            {
                _cadastrarVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility _verificarVisibility = Visibility.Collapsed;
        public Visibility VerificarVisibility
        {
            get => _verificarVisibility;
            set
            {
                _verificarVisibility = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action RequestClose;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
