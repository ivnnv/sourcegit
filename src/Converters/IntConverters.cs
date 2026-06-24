using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace SourceGit.Converters
{
    // [fork:colored-tabs] Brush converter for custom bookmark colors
    public class BookmarkBrushConverter : IMultiValueConverter
    {
        public static readonly BookmarkBrushConverter Instance = new();

        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            var bookmark = values.Count > 0 && values[0] is int b ? b : 0;
            var bookmarkCustom = values.Count > 1 && values[1] is uint bc ? bc : 0u;

            if (bookmark == -1 && bookmarkCustom != 0)
                return new SolidColorBrush(Color.FromUInt32(bookmarkCustom));

            return Models.Bookmarks.Get(bookmark) ?? App.Current?.FindResource("Brush.FG1") as IBrush;
        }
    }

    public static class IntConverters
    {
        public static readonly FuncValueConverter<int, bool> IsGreaterThanZero =
            new(v => v > 0);

        public static readonly FuncValueConverter<int, bool> IsGreaterThanFour =
            new(v => v > 4);

        public static readonly FuncValueConverter<int, bool> IsZero =
            new(v => v == 0);

        public static readonly FuncValueConverter<int, bool> IsNotOne =
            new(v => v != 1);

        // [fork:colored-tabs] Used in EditRepositoryNode to mark the "Custom..." dropdown item (sentinel = -1)
        public static readonly FuncValueConverter<int, bool> IsNegativeOne =
            new(v => v == -1);

        public static readonly FuncValueConverter<int, Thickness> ToTreeMargin =
            new(v => new Thickness(v * 16, 0, 0, 0));

        public static readonly FuncValueConverter<int, string> ToUnsolvedDesc =
            new(v => v == 0 ? App.Text("MergeConflictEditor.AllResolved") : App.Text("MergeConflictEditor.ConflictsRemaining", v));
    }
}
