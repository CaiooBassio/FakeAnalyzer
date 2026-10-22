using InstaFakeAnalyzerClient.ApiServices;
using InstaFakeAnalyzerClient.View;
using InstaFakeAnalyzerClient.ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Data;
using System.Windows;

namespace InstaFakeAnalyzerClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // Carregar appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddHttpClient<NoticiaService>(client =>
            {
                client.BaseAddress = new Uri(config["Urls:ApiUrl"]);
            });
            services.AddHttpClient<AuthService>(client =>
            {
                client.BaseAddress = new Uri(config["Urls:ApiUrl"]);
            });
            services.AddHttpClient<UsuarioService>(client =>
            {
                client.BaseAddress = new Uri(config["Urls:ApiUrl"]);
            });
            // Registrar janelas/telas
            services.AddTransient<ViewDashboard>();
            services.AddTransient<ViewLogin>();
            services.AddTransient<ViewVerificarNoticias>();
            services.AddTransient<ViewCadastroUsuario>();
            services.AddTransient<ViewChatbot>();
            services.AddTransient<ViewInserirNoticia>();
            // Registrar ViewModels
            services.AddTransient<CadastroVM>();
            services.AddTransient<LoginVM>();
            services.AddTransient<DashboardVM>();
            services.AddTransient<VerificarNoticiasVM>();
            services.AddTransient<InserirNoticiaVM>();
            services.AddTransient<ChatBotVM>();

            // Criar o ServiceProvider
            ServiceProvider = services.BuildServiceProvider();

            // Abrir a janela inicial
            var loginWindow = ServiceProvider.GetRequiredService<ViewLogin>();
            loginWindow.Show();
        }
    }

}
