using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using RDO.App.Services;
using RDO.App.Views;
using Windows.Graphics;
using Windows.UI;

namespace RDO.App
{
    public sealed partial class MainWindow : Window
    {
        private static readonly SolidColorBrush Transparent  = new(Colors.Transparent);
        private static readonly SolidColorBrush CloseHover   = new(Color.FromArgb(255, 196, 43, 28));
        private static readonly SolidColorBrush ClosePressed = new(Color.FromArgb(255, 166, 35, 24));
        private static readonly SolidColorBrush White        = new(Colors.White);

        public MainWindow()
        {
            InitializeComponent();
            ConfigurarTitleBar();
            CarregarIcone();
            AppTitleBar.Loaded += (_, _) => AtualizarPassthrough();
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

            // Oculta os botoes nativos — substituidos pelos XAML customizados
            var tb = AppWindow.TitleBar;
            var t  = Colors.Transparent;
            tb.ButtonBackgroundColor         = t;
            tb.ButtonInactiveBackgroundColor = t;
            tb.ButtonHoverBackgroundColor    = t;
            tb.ButtonPressedBackgroundColor  = t;
            tb.ButtonForegroundColor         = t;
            tb.ButtonInactiveForegroundColor = t;
            tb.ButtonHoverForegroundColor    = t;
            tb.ButtonPressedForegroundColor  = t;

            AppWindow.Changed += (_, _) =>
            {
                AtualizarPassthrough();
                AtualizarIconeMaximize();
            };
        }

        private ElementTheme Tema =>
            (Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Dark;

        private void AtualizarPassthrough()
        {
            if (Content?.XamlRoot is not { } root) return;

            var scale          = root.RasterizationScale;
            const int btnCount = 3;
            const int btnW     = 46;
            const int btnH     = 44;

            var scaledW = (int)(btnW * btnCount * scale);
            var scaledH = (int)(btnH * scale);
            var rect    = new RectInt32(AppWindow.Size.Width - scaledW, 0, scaledW, scaledH);

            InputNonClientPointerSource
                .GetForWindowId(AppWindow.Id)
                .SetRegionRects(NonClientRegionKind.Passthrough, new[] { rect });
        }

        private void AtualizarIconeMaximize()
        {
            if (AppWindow.Presenter is OverlappedPresenter p)
                MaxRestoreIcon.Glyph = p.State == OverlappedPresenterState.Maximized
                    ? ""   // E923 ChromeRestore
                    : ""; // E922 ChromeMaximize
        }

        // ── Hover min / max — fundo adaptado ao tema ─────────────────────────────
        private SolidColorBrush HoverBrush => Tema == ElementTheme.Dark
            ? new SolidColorBrush(Color.FromArgb(255, 44, 44, 44))
            : new SolidColorBrush(Color.FromArgb(255, 194, 196, 206));

        private SolidColorBrush PressedBrush => Tema == ElementTheme.Dark
            ? new SolidColorBrush(Color.FromArgb(255, 58, 58, 58))
            : new SolidColorBrush(Color.FromArgb(255, 181, 183, 194));

        private void CaptionBtn_PointerEntered(object s, PointerRoutedEventArgs _)
            => ((Button)s).Background = HoverBrush;

        private void CaptionBtn_PointerExited(object s, PointerRoutedEventArgs _)
            => ((Button)s).Background = Transparent;

        private void CaptionBtn_PointerPressed(object s, PointerRoutedEventArgs _)
            => ((Button)s).Background = PressedBrush;

        private void CaptionBtn_PointerReleased(object s, PointerRoutedEventArgs _)
            => ((Button)s).Background = HoverBrush;

        // ── Hover fechar — fundo vermelho + icone branco ─────────────────────────
        private SolidColorBrush CloseFgDefault => Tema == ElementTheme.Dark
            ? new SolidColorBrush(Color.FromArgb(255, 204, 204, 204))
            : new SolidColorBrush(Color.FromArgb(255, 30, 36, 51));

        private void CloseBtn_PointerEntered(object s, PointerRoutedEventArgs _)
        {
            BtnClose.Background  = CloseHover;
            CloseIcon.Foreground = White;
        }

        private void CloseBtn_PointerExited(object s, PointerRoutedEventArgs _)
        {
            BtnClose.Background  = Transparent;
            CloseIcon.Foreground = CloseFgDefault;
        }

        private void CloseBtn_PointerPressed(object s, PointerRoutedEventArgs _)
            => BtnClose.Background = ClosePressed;

        private void CloseBtn_PointerReleased(object s, PointerRoutedEventArgs _)
            => BtnClose.Background = CloseHover;

        // ── Handlers dos botoes ──────────────────────────────────────────────────
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter p) p.Minimize();
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter p)
            {
                if (p.State == OverlappedPresenterState.Maximized) p.Restore();
                else p.Maximize();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
