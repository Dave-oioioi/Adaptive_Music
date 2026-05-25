namespace AdaptiveMusic.UI;

internal sealed record AppTheme(
    bool IsDark,
    Color PageBackground,
    Color GroupBackground,
    Color PrimaryBlue,
    Color PrimaryText,
    Color SecondaryText,
    Color MusicRow,
    Color TriggerRow);

internal static class AppThemes
{
    public static AppTheme Resolve() => Light;

    public static readonly AppTheme Dark = new(
        true,
        Color.FromArgb(28, 28, 30),
        Color.FromArgb(44, 44, 46),
        Color.FromArgb(10, 132, 255),
        Color.FromArgb(242, 242, 247),
        Color.FromArgb(174, 174, 178),
        Color.FromArgb(36, 61, 45),
        Color.FromArgb(75, 54, 30));

    public static readonly AppTheme Light = new(
        false,
        Color.FromArgb(242, 242, 247),
        Color.White,
        Color.FromArgb(0, 122, 255),
        Color.FromArgb(28, 28, 30),
        Color.FromArgb(99, 99, 102),
        Color.FromArgb(235, 247, 240),
        Color.FromArgb(255, 244, 232));

}
