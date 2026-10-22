using InstaFakeAnalyzerClient.ApiServices;
using InstaFakeAnalyzerClient.Models;
using InstaFakeAnalyzerClient.ViewModel;
using Microsoft.Extensions.DependencyInjection;
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
    /// Lógica interna para ViewDashboard.xaml
    /// </summary>
    public partial class ViewDashboard : Window
    {
        public ViewDashboard(DashboardVM vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.RequestClose += () => this.Close();
        }

    }
}
