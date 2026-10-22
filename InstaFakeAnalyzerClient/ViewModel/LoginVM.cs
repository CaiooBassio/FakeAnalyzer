using GalaSoft.MvvmLight.Command;
using InstaFakeAnalyzerClient.ApiServices;
using InstaFakeAnalyzerClient.Models;
using InstaFakeAnalyzerClient.View;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InstaFakeAnalyzerClient.ViewModel
{
    public class LoginVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? RequestClose;
        public RelayCommand LoginCommand { get; }
        private readonly AuthService _authService;
        private string _usuario;
        public string NomeUsuario
        {
            get { return _usuario; }
            set
            {
                _usuario = value;
                OnPropertyChanged(nameof(NomeUsuario));
            }
        }
        private string _senha;
        public string Senha
        {
            get { return _senha; }
            set
            {
                _senha = value;
                OnPropertyChanged(nameof(Senha));
            }
        }

        public LoginVM(AuthService authService) 
        {
            _authService = authService;
            LoginCommand = new RelayCommand(async () => await LoginAsync());
        }

        private async Task LoginAsync()
        {
            Usuario usuario = new Usuario() 
            {
                NomeUsuario = NomeUsuario, 
                Senha = Senha 
            };
            try
            {
                if (await _authService.FazerLoginAsync(usuario))
                {
                    var telaDashboard = App.ServiceProvider.GetRequiredService<ViewDashboard>();
                    telaDashboard.Show();
                    RequestClose?.Invoke();
                }
                else
                {
                    MessageBox.Show("Usuário ou senha inválidos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao fazer login: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
