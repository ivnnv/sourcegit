namespace SourceGit.Models
{
    public static class Bookmarks
    {
        public static readonly Avalonia.Media.IBrush[] Brushes = [
            null,
            Avalonia.Media.Brushes.Red,
            Avalonia.Media.Brushes.Orange,
            Avalonia.Media.Brushes.Gold,
            Avalonia.Media.Brushes.ForestGreen,
            Avalonia.Media.Brushes.DarkCyan,
            Avalonia.Media.Brushes.DeepSkyBlue,
            Avalonia.Media.Brushes.Purple,
        ];

        public static Avalonia.Media.IBrush Get(int i)
        {
            return (i >= 0 && i < Brushes.Length) ? Brushes[i] : null;
        }

        // [fork:colored-tabs] -1 is the custom sentinel; custom is an ARGB value
        public static Avalonia.Media.IBrush Get(int i, uint custom)
        {
            if (i == Custom)
                return custom == 0 ? null : new Avalonia.Media.SolidColorBrush(custom);

            return Get(i);
        }

        // [fork:colored-tabs] Sentinel index meaning "use the custom ARGB instead of a preset"
        public const int Custom = -1;
    }
}
