using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace SourceGit.Views
{
    public class ChangeStatusIcon : Control
    {
        private static readonly string[] STATUS_BRUSHES =
        [
            "Brush.FG2",
            "Brush.Status.Modified",
            "Brush.Status.TypeChanged",
            "Brush.Status.Added",
            "Brush.Status.Deleted",
            "Brush.Status.Renamed",
            "Brush.Status.Copied",
            "Brush.Status.Untracked",
            "Brush.Status.Conflict",
        ];

        public static readonly DirectProperty<ChangeStatusIcon, bool> IsUnstagedChangeProperty =
            AvaloniaProperty.RegisterDirect<ChangeStatusIcon, bool>(
                nameof(IsUnstagedChange),
                static o => o.IsUnstagedChange,
                static (o, v) => o.IsUnstagedChange = v);

        public bool IsUnstagedChange
        {
            get => _isUnstagedChange;
            set => SetAndRaise(IsUnstagedChangeProperty, ref _isUnstagedChange, value);
        }

        public static readonly DirectProperty<ChangeStatusIcon, Models.Change> ChangeProperty =
            AvaloniaProperty.RegisterDirect<ChangeStatusIcon, Models.Change>(
                nameof(Change),
                static o => o.Change,
                static (o, v) => o.Change = v);

        public Models.Change Change
        {
            get => _change;
            set => SetAndRaise(ChangeProperty, ref _change, value);
        }

        public override void Render(DrawingContext context)
        {
            if (_change == null || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            var idx = (int)(_isUnstagedChange ? _change.WorkTree : _change.Index);
            var unit = Math.Min(Bounds.Width, Bounds.Height) / 16.0;
            var left = (Bounds.Width - unit * 16) * 0.5;
            var top = (Bounds.Height - unit * 16) * 0.5;
            var foreground = FindBrush("Brush.FG2", Brushes.Gray);
            var content = FindBrush("Brush.Contents", Brushes.Black);
            var status = FindBrush(STATUS_BRUSHES[idx], foreground);
            var documentPen = new Pen(foreground, Math.Max(1, unit * 1.15));

            context.DrawRectangle(
                null,
                documentPen,
                new Rect(left + unit * 2, top + unit * 1.5, unit * 10, unit * 13),
                unit * 1.4,
                unit * 1.4);

            var center = new Point(left + unit * 11.5, top + unit * 11.5);
            context.DrawEllipse(content, null, center, unit * 4.2, unit * 4.2);
            context.DrawEllipse(status, null, center, unit * 3.5, unit * 3.5);
            DrawStatusMark(context, idx, center, unit);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsUnstagedChangeProperty || change.Property == ChangeProperty)
            {
                if (_change != null)
                    ToolTip.SetTip(this, _isUnstagedChange ? _change.WorkTreeDesc : _change.IndexDesc);
                InvalidateVisual();
            }
            else if (change.Property.Name == nameof(ActualThemeVariant) && change.NewValue != null)
                InvalidateVisual();
        }

        private IBrush FindBrush(string key, IBrush fallback)
        {
            return this.TryFindResource(key, ActualThemeVariant, out var resource) && resource is IBrush brush
                ? brush
                : fallback;
        }

        private static void DrawStatusMark(DrawingContext context, int idx, Point center, double unit)
        {
            var pen = new Pen(Brushes.White, Math.Max(1, unit * 1.2));
            var x = center.X;
            var y = center.Y;

            switch ((Models.ChangeState)idx)
            {
                case Models.ChangeState.Modified:
                    context.DrawLine(pen, new Point(x - unit * 1.5, y + unit * 1.5), new Point(x + unit * 1.5, y - unit * 1.5));
                    context.DrawLine(pen, new Point(x - unit * 1.5, y + unit * 1.5), new Point(x - unit * 0.5, y + unit * 1.3));
                    break;
                case Models.ChangeState.TypeChanged:
                    context.DrawLine(pen, new Point(x - unit * 1.5, y - unit), new Point(x + unit * 1.5, y - unit));
                    context.DrawLine(pen, new Point(x, y - unit), new Point(x, y + unit * 1.6));
                    break;
                case Models.ChangeState.Added:
                    context.DrawLine(pen, new Point(x - unit * 1.6, y), new Point(x + unit * 1.6, y));
                    context.DrawLine(pen, new Point(x, y - unit * 1.6), new Point(x, y + unit * 1.6));
                    break;
                case Models.ChangeState.Deleted:
                    context.DrawLine(pen, new Point(x - unit * 1.7, y), new Point(x + unit * 1.7, y));
                    break;
                case Models.ChangeState.Renamed:
                    context.DrawLine(pen, new Point(x - unit * 1.8, y), new Point(x + unit * 1.4, y));
                    context.DrawLine(pen, new Point(x + unit * 1.4, y), new Point(x + unit * 0.2, y - unit * 1.2));
                    context.DrawLine(pen, new Point(x + unit * 1.4, y), new Point(x + unit * 0.2, y + unit * 1.2));
                    break;
                case Models.ChangeState.Copied:
                    context.DrawRectangle(null, pen, new Rect(x - unit * 1.7, y - unit * 0.8, unit * 2.4, unit * 2.4));
                    context.DrawRectangle(null, pen, new Rect(x - unit * 0.7, y - unit * 1.8, unit * 2.4, unit * 2.4));
                    break;
                case Models.ChangeState.Untracked:
                    context.DrawLine(pen, new Point(x - unit * 0.8, y - unit), new Point(x, y - unit * 1.6));
                    context.DrawLine(pen, new Point(x, y - unit * 1.6), new Point(x + unit, y - unit));
                    context.DrawLine(pen, new Point(x + unit, y - unit), new Point(x, y + unit * 0.5));
                    context.DrawEllipse(Brushes.White, null, new Point(x, y + unit * 1.8), unit * 0.55, unit * 0.55);
                    break;
                case Models.ChangeState.Conflicted:
                    context.DrawLine(pen, new Point(x, y - unit * 1.8), new Point(x, y + unit * 0.6));
                    context.DrawEllipse(Brushes.White, null, new Point(x, y + unit * 1.8), unit * 0.55, unit * 0.55);
                    break;
            }
        }

        private bool _isUnstagedChange = false;
        private Models.Change _change = null;
    }
}
