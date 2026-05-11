using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using RDO.App.Services;
using RDO.App.Views;
using Windows.UI;

namespace RDO.App
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ConfigurarTitleBar();
            CarregarIcone();
            RootFrame.Navigated += RootFrame_Navigated;
            RootFrame.Navigate(typeof(LoginPage));
        }

        private void CarregarIcone()
        {
            try
            {
                TitleBarIcon.Source = new BitmapImage(AssetHelper.GetUri("Assets/icone.png"));
            }
            catch { }
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var isLogin = e.SourcePageType == typeof(LoginPage);
            var vis = isLogin ? Visibility.Collapsed : Visibility.Visible;
            TitleBarBg.Visibility      = vis;
            TitleBarDivider.Visibility = vis;
            TitleBarContent.Visibility = vis;
        }

        private void ConfigurarTitleBar()
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            var tb = AppWindow.TitleBar;
            tb.ButtonBackgroundColor         = Colors.Transparent;
            tb.ButtonInactiveBackgroundColor = Colors.Transparent;
            tb.ButtonHoverBackgroundColor    = Color.FromArgb(30, 255, 255, 255);
            tb.ButtonPressedBackgroundColor  = Color.FromArgb(60, 255, 255, 255);
            tb.ButtonForegroundColor         = Color.FromArgb(255, 180, 195, 215);
            tb.ButtonInactiveForegroundColor = Color.FromArgb(120, 180, 195, 215);
        }
    }
}
