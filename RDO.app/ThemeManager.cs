using Microsoft.UI.Xaml;
using RDO.App.Services;
using System;

namespace RDO.App
{
    public static class ThemeManager
    {
        public static ElementTheme Current { get; private set; } = ElementTheme.Dark;

        public static event Action<ElementTheme>? ThemeChanged;

        public static void Apply(ElementTheme theme)
        {
            Current = theme;
            LocalSettingsService.Set("Theme", theme.ToString());

            if ((Application.Current as App)?.MainWindow?.Content is FrameworkElement fe)
                fe.RequestedTheme = theme;

            ThemeChanged?.Invoke(theme);
        }

        public static void Toggle() =>
            Apply(Current == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark);

        public static void LoadSaved()
        {
            var saved = LocalSettingsService.Get<string>("Theme");
            Apply(saved == "Light" ? ElementTheme.Light : ElementTheme.Dark);
        }
    }
}
