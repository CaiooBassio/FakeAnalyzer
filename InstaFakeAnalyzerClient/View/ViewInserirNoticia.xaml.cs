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
    /// Lógica interna para ViewInserirNoticia.xaml
    /// </summary>
    public partial class ViewInserirNoticia : Window
    {
        public ViewInserirNoticia(InserirNoticiaVM vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.RequestClose += () => this.Close();
        }
    }
}
