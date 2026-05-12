using Microsoft.UI;
using Microsoft.UI.Windowing;
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
            AppTitleBar.Loaded += (_, _) =>
            {
                AtualizarCoresBotoes();
                AppTitleBar.ActualThemeChanged += (_, _) => AtualizarCoresBotoes();
            };
            RootFrame.Navigated += RootFrame_Navigated;
            RootFrame.Navigate(typeof(LoginPage));
        }

        private void CarregarIcone()
        {
            try { TitleBarIcon.Source = new BitmapImage(AssetHelper.GetUri("Assets/icone.png")); }
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
        }

        private ElementTheme Tema =>
            (Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Dark;

        private void AtualizarCoresBotoes()
        {
            var tb = AppWindow.TitleBar;
            var isDark = Tema == ElementTheme.Dark;

            tb.ButtonBackgroundColor         = Colors.Transparent;
            tb.ButtonInactiveBackgroundColor = Colors.Transparent;

            if (isDark)
            {
                tb.ButtonForegroundColor         = Color.FromArgb(255, 204, 204, 204);
                tb.ButtonInactiveForegroundColor = Color.FromArgb(255,  90,  90,  90);
                tb.ButtonHoverBackgroundColor    = Color.FromArgb( 40, 255, 255, 255);
                tb.ButtonHoverForegroundColor    = Color.FromArgb(255, 255, 255, 255);
                tb.ButtonPressedBackgroundColor  = Color.FromArgb( 20, 255, 255, 255);
                tb.ButtonPressedForegroundColor  = Color.FromArgb(255, 180, 180, 180);
            }
            else
            {
                tb.ButtonForegroundColor         = Color.FromArgb(255,  30,  36,  51);
                tb.ButtonInactiveForegroundColor = Color.FromArgb(255, 140, 140, 150);
                tb.ButtonHoverBackgroundColor    = Color.FromArgb( 40,   0,   0,   0);
                tb.ButtonHoverForegroundColor    = Color.FromArgb(255,  10,  16,  35);
                tb.ButtonPressedBackgroundColor  = Color.FromArgb( 20,   0,   0,   0);
                tb.ButtonPressedForegroundColor  = Color.FromArgb(255,  50,  60,  80);
            }
        }
    }
}
