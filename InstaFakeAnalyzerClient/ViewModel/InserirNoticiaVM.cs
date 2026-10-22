using GalaSoft.MvvmLight.Command;
using InstaFakeAnalyzerClient;
using InstaFakeAnalyzerClient.ApiServices;
using InstaFakeAnalyzerClient.Models;
using InstaFakeAnalyzerClient.View;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

public class InserirNoticiaVM : INotifyPropertyChanged
{
    public event Action? RequestClose;
    private readonly NoticiaService _noticiaService;
    public RelayCommand SalvarCommand { get; }
    public RelayCommand VoltarCommand { get; }


    public InserirNoticiaVM(NoticiaService noticiaService)
    {
        _noticiaService = noticiaService;
        SalvarCommand = new RelayCommand(async () => await SalvarNoticiaAsync());
        VoltarCommand = new RelayCommand(Voltar);
    }

    private string _conteudo;
    public string Conteudo
    {
        get => _conteudo;
        set { _conteudo = value; OnPropertyChanged(); }
    }

    private string _justificativa;
    public string Justificativa
    {
        get => _justificativa;
        set { _justificativa = value; OnPropertyChanged(); }
    }

    private bool _isFake;
    public bool IsFake
    {
        get => _isFake;
        set { _isFake = value; OnPropertyChanged(); }
    }

    private void Voltar()
    {
        var telaDashboard = App.ServiceProvider.GetRequiredService<ViewDashboard>();
        telaDashboard.Show();
        RequestClose.Invoke();
    }

    private async Task SalvarNoticiaAsync()
    {
        var novaNoticia = new Noticia
        {
            Conteudo = Conteudo,
            Justificativa = Justificativa,
            snFalsa = IsFake,
            DataCadastro = DateTime.UtcNow,
            snAnalizada = false
        };

        bool sucesso = await _noticiaService.EnviarNoticiaDiretamenteAsync(novaNoticia);

        if (sucesso)
        {
            // Limpar campos
            Conteudo = "";
            Justificativa = "";
            IsFake = false;
            MessageBox.Show("Notícia salva com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Erro ao salvar a notícia. Tente novamente.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
