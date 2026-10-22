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
using System.Windows.Input;

namespace InstaFakeAnalyzerClient.ViewModel
{
    public class ChatBotVM : INotifyPropertyChanged
    {
        public Action RequestClose { get; internal set; }
        private readonly NoticiaService _noticiaService;

        private string _noticiaInput;
        public string NoticiaInput
        {
            get => _noticiaInput;
            set
            {
                if (_noticiaInput != value)
                {
                    _noticiaInput = value;
                    OnPropertyChanged(nameof(NoticiaInput));
                }
            }
        }

        private string _respostaIA;
        public string RespostaIA
        {
            get => _respostaIA;
            set
            {
                if (_respostaIA != value)
                {
                    _respostaIA = value;
                    OnPropertyChanged(nameof(RespostaIA));
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private Noticia _noticiaRetornada;

        public ICommand EnviarCommand { get; }
        public ICommand SalvarCommand { get; }
        public ICommand VoltarCommand { get; }

        public ChatBotVM(NoticiaService noticiaService)
        {
            _noticiaService = noticiaService ?? throw new ArgumentNullException(nameof(noticiaService));

            EnviarCommand = new RelayCommand(async () => await EnviarNoticiaAsync());
            VoltarCommand = new RelayCommand(Voltar);
        }

        private bool CanEnviar => !string.IsNullOrWhiteSpace(NoticiaInput);

        private async Task EnviarNoticiaAsync()
        {
            if (!CanEnviar)
            {
                MessageBox.Show("Por favor, insira uma notícia para enviar.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            try
            {
                IsLoading = true;
                RespostaIA = "Aguarde, estamos processando sua solicitação...";
                _noticiaRetornada = await _noticiaService.EnviarNoticiaAsync(new Noticia { Conteudo = NoticiaInput });

                if (_noticiaRetornada != null)
                {
                    RespostaIA = $"Justificativa: {_noticiaRetornada.Justificativa}\n" +
                                $"Fake News? {((bool)_noticiaRetornada.snFalsa ? "Sim" : "Não")}\n";
                }
                else
                {
                    RespostaIA = "Resposta da IA inválida.";
                }
            }
            catch (Exception ex)
            {
                RespostaIA = $"Erro: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }


        private void Voltar()
        {
            var telaDashboard = App.ServiceProvider.GetRequiredService<ViewDashboard>();
            telaDashboard.Show();
            RequestClose.Invoke();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
