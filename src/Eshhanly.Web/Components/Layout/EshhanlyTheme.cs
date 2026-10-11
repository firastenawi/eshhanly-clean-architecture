using MudBlazor;

namespace Eshhanly.Web.Components.Layout;

internal static class EshhanlyTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0E9F6E",   // EV green
            Secondary = "#1C64F2",
            AppbarBackground = "#0E9F6E",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#31C48D",
            Secondary = "#76A9FA",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["Cairo", "Segoe UI", "Tahoma", "sans-serif"] },
        },
    };
}
