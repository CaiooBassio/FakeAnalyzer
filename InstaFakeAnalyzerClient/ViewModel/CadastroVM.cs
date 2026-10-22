using GalaSoft.MvvmLight.CommandWpf;
using InstaFakeAnalyzerClient.ApiServices;
using InstaFakeAnalyzerClient.Models;
using InstaFakeAnalyzerClient.View;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InstaFakeAnalyzerClient.ViewModel
{
    public class CadastroVM : INotifyPropertyChanged
    {
        // Propriedades ligadas ao formulário de cadastro
        private string _nome = "";
        public string Nome
        {
            get => _nome;
            set
            {
                if (_nome != value)
                {
                    _nome = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _nomeUsuario = "";
        public string NomeUsuario
        {
            get => _nomeUsuario;
            set
            {
                if (_nomeUsuario != value)
                {
                    _nomeUsuario = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _email = "";
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _senha = "";
        public string Senha
        {
            get => _senha;
            set
            {
                if (_senha != value)
                {
                    _senha = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<Usuario.TipoUsuario> TiposUsuario { get; }

        private Usuario.TipoUsuario? _tipoUsuarioSelecionado;
        public Usuario.TipoUsuario? TipoUsuarioSelecionado
        {
            get => _tipoUsuarioSelecionado;
            set
            {
                if (_tipoUsuarioSelecionado != value)
                {
                    _tipoUsuarioSelecionado = value;
                    OnPropertyChanged();
                }
            }
        }

        // Comandos para os botões
        public RelayCommand CommandVoltar { get; }
        public RelayCommand CommandCadastrar { get; }

        private readonly UsuarioService _usuarioService;

        public CadastroVM(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;

            TiposUsuario = Enum.GetValues(typeof(Usuario.TipoUsuario))
                               .Cast<Usuario.TipoUsuario>()
                               .ToList();
            TipoUsuarioSelecionado = TiposUsuario.FirstOrDefault();

            CommandVoltar = new RelayCommand(Voltar);
            CommandCadastrar = new RelayCommand(async () => await Cadastrar());
        }

        private async Task Cadastrar()
        {
            try
            {
                string erro = string.Empty;
                if (string.IsNullOrEmpty(Nome))
                    erro += "Nome não pode ser vazio! ";

                if (string.IsNullOrEmpty(NomeUsuario))
                    erro += "Nome de Usuário não pode ser vazio! ";

                if (string.IsNullOrEmpty(Senha))
                    erro += "Senha não pode ser vazio! ";

                if (!string.IsNullOrEmpty(erro))

                    {
                    MessageBox.Show(erro, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                    }

                var usuario = new Usuario
                {
                    Nome = Nome,
                    NomeUsuario = NomeUsuario,
                    Senha = Senha,
                    DataCadastro = DateTime.Now,
                    tipoUsuario = TipoUsuarioSelecionado ?? Usuario.TipoUsuario.Comum
                };

                await _usuarioService.CadastrarUsuario(usuario);
                MessageBox.Show("Usuário cadastrado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                Voltar();
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Voltar()
        {
            var telaDashboard = App.ServiceProvider.GetRequiredService<ViewDashboard>();
            telaDashboard.Show();

            RequestClose.Invoke();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action RequestClose;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
