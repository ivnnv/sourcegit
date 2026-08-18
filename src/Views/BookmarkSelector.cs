using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace SourceGit.Views
{
    public class BookmarkSelector : Control
    {
        public static readonly DirectProperty<BookmarkSelector, int> BookmarkProperty =
            AvaloniaProperty.RegisterDirect<BookmarkSelector, int>(
                nameof(Bookmark),
                static o => o.Bookmark,
                static (o, v) => o.Bookmark = v);

        public int Bookmark
        {
            get => _bookmark;
            set => SetAndRaise(BookmarkProperty, ref _bookmark, value);
        }

        // [fork:colored-tabs] ARGB used when Bookmark is the custom sentinel (-1)
        public static readonly DirectProperty<BookmarkSelector, uint> CustomProperty =
            AvaloniaProperty.RegisterDirect<BookmarkSelector, uint>(
                nameof(Custom),
                static o => o.Custom,
                static (o, v) => o.Custom = v);

        public uint Custom
        {
            get => _custom;
            set => SetAndRaise(CustomProperty, ref _custom, value);
        }

        // [fork:colored-tabs] Raised when the trailing custom swatch is clicked, so the host can open a color picker
        public event EventHandler CustomRequested;

        public BookmarkSelector()
        {
            var geo = Application.Current!.FindResource("Icons.Bookmark") as StreamGeometry;
            _icon = geo!.Clone();
            var iconBounds = _icon.Bounds;
            var translation = Matrix.CreateTranslation(-(Vector)iconBounds.Position);
            var scale = Math.Min(14.0 / iconBounds.Width, 14.0 / iconBounds.Height);
            var transform = translation * Matrix.CreateScale(scale, scale);
            if (_icon.Transform == null || _icon.Transform.Value == Matrix.Identity)
                _icon.Transform = new MatrixTransform(transform);
            else
                _icon.Transform = new MatrixTransform(_icon.Transform.Value * transform);

            var x = 2.0;
            for (var i = 0; i < Models.Bookmarks.Brushes.Length + 1; i++)
            {
                var hitBox = new Rect(x - 2.5, 2.5, 18, 20);
                _hitBoxes.Add(hitBox);
                x += 26;
            }
        }

        public override void Render(DrawingContext context)
        {
            // Just enable clicking anywhere in the control.
            context.FillRectangle(Brushes.Transparent, new Rect(0, 0, Bounds.Width, Bounds.Height));

            var defaultBrush = this.FindResource("Brush.FG1") as IBrush;
            var selectedBorder = new Pen(new SolidColorBrush((Color)this.FindResource("SystemAccentColor")!));
            var dashedBorder = new Pen(defaultBrush, 1, new DashStyle([2, 2], 0));

            for (var i = 0; i < _hitBoxes.Count; i++)
            {
                var hitBox = _hitBoxes[i];
                // [fork:colored-tabs] last box is the custom swatch, represented by the -1 sentinel
                var value = i == Models.Bookmarks.Brushes.Length ? Models.Bookmarks.Custom : i;
                if (value == _bookmark)
                    context.DrawRectangle(selectedBorder, hitBox, 3);

                var bursh = Models.Bookmarks.Get(value, _custom) ?? defaultBrush;
                using (context.PushTransform(Matrix.CreateTranslation(hitBox.X + 3, 5)))
                    context.DrawGeometry(bursh, null, _icon);

                if (value == Models.Bookmarks.Custom)
                    context.DrawRectangle(dashedBorder, hitBox, 3);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == BookmarkProperty || change.Property == CustomProperty)
                InvalidateVisual();
            else if (change.Property.Name == nameof(ActualThemeVariant) && change.NewValue != null)
                InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var pos = e.GetPosition(this);
                for (var i = 0; i < _hitBoxes.Count; i++)
                {
                    if (!_hitBoxes[i].Contains(pos))
                        continue;

                    // [fork:colored-tabs] clicking the trailing swatch selects custom and asks the host for a color
                    if (i == Models.Bookmarks.Brushes.Length)
                    {
                        Bookmark = Models.Bookmarks.Custom;
                        CustomRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        Bookmark = i;
                    }

                    break;
                }

                e.Handled = true;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // [fork:colored-tabs] +1 slot for the custom swatch
            return new Size(9 * 14 + 8 * 12 + 4, 24);
        }

        private int _bookmark = 0;
        private uint _custom = 0;
        private Geometry _icon = null;
        private List<Rect> _hitBoxes = [];
    }
}
