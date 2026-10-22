using GalaSoft.MvvmLight.CommandWpf;
using InstaFakeAnalyzerClient.ApiServices;
using InstaFakeAnalyzerClient.Models;
using InstaFakeAnalyzerClient.View;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace InstaFakeAnalyzerClient.ViewModel
{
    public class VerificarNoticiasVM : INotifyPropertyChanged
    {
        private readonly NoticiaService _noticiaService;
        private List<Noticia> _filaNoticias;
        private Noticia _noticiaSelecionada;

        public Noticia NoticiaSelecionada
        {
            get => _noticiaSelecionada;
            set { _noticiaSelecionada = value; OnPropertyChanged(); }
        }

        public ICommand SalvarCommand { get; }
        public ICommand VoltarCommand { get; }

        public VerificarNoticiasVM(NoticiaService noticiaService)
        {
            _noticiaService = noticiaService;
            SalvarCommand = new RelayCommand(async () => await SalvarAsync());
            VoltarCommand = new RelayCommand(Voltar);

            _ = CarregarFilaAsync();
        }


        private void Voltar()
        {
            var telaDashboard = App.ServiceProvider.GetService<ViewDashboard>();
            telaDashboard.Show();
            RequestClose.Invoke();
        }
        private async Task CarregarFilaAsync()
        {
            _filaNoticias = await _noticiaService.ObterNoticiasParaVerificarAsync();
            CarregarProximaNoticia();
        }

        private void CarregarProximaNoticia()
        {
            if (_filaNoticias.Count > 0)
            {
                var rand = new Random();
                var index = rand.Next(_filaNoticias.Count);
                NoticiaSelecionada = _filaNoticias[index];
                _filaNoticias.RemoveAt(index);
            }
            else
            {
                MessageBox.Show("Todas as notícias já foram analisadas!", "Fim", MessageBoxButton.OK, MessageBoxImage.Information);
                Voltar();
            }
        }

        private async Task SalvarAsync()
        {
            NoticiaSelecionada.snAnalizada = true;
            await _noticiaService.AtualizarNoticiaAsync(NoticiaSelecionada);
            CarregarProximaNoticia();
        }

        public event Action? RequestClose;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
