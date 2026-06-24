using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace SourceGit.Converters
{
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

        // [fork:custom-branch-sort] Generic enum-int comparators used by sort-mode UI bindings
        public static readonly FuncValueConverter<object, bool> IsEnumZero =
            new(v => v is int i ? i == 0 : v is Enum e && Convert.ToInt32(e) == 0);

        public static readonly FuncValueConverter<object, bool> IsEnumOne =
            new(v => v is int i ? i == 1 : v is Enum e && Convert.ToInt32(e) == 1);

        public static readonly FuncValueConverter<object, bool> IsEnumTwo =
            new(v => v is int i ? i == 2 : v is Enum e && Convert.ToInt32(e) == 2);

        public static readonly FuncValueConverter<int, Thickness> ToTreeMargin =
            new(v => new Thickness(v * 16, 0, 0, 0));

        public static readonly FuncValueConverter<int, string> ToUnsolvedDesc =
            new(v => v == 0 ? App.Text("MergeConflictEditor.AllResolved") : App.Text("MergeConflictEditor.ConflictsRemaining", v));
    }
}
