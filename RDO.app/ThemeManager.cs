using Microsoft.UI.Xaml;
using Windows.Storage;

namespace RDO.App
{
    public static class ThemeManager
    {
        public static ElementTheme Current { get; private set; } = ElementTheme.Dark;

        public static void Apply(ElementTheme theme)
        {
            Current = theme;
            ApplicationData.Current.LocalSettings.Values["Theme"] = theme.ToString();

            if ((Application.Current as App)?.MainWindow?.Content is FrameworkElement fe)
                fe.RequestedTheme = theme;
        }

        public static void Toggle() =>
            Apply(Current == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark);

        public static void LoadSaved()
        {
            var saved = ApplicationData.Current.LocalSettings.Values["Theme"] as string;
            Apply(saved == "Light" ? ElementTheme.Light : ElementTheme.Dark);
        }
    }
}
