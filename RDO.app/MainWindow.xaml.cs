using Microsoft.UI.Xaml;
using RDO.App.Views;

namespace RDO.App
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            RootFrame.Navigate(typeof(LoginPage));
        }
    }
}