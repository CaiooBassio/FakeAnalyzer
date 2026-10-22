using InstaFakeAnalyzerClient.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Lógica interna para ViewChatBot.xaml
    /// </summary>
    public partial class ViewChatbot : Window
    {
        public ViewChatbot(ChatBotVM vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.RequestClose += () => this.Close();
        }
    }
}
