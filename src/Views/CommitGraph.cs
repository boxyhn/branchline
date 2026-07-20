using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace SourceGit.Views
{
    public class CommitGraph : Control, Models.IAvatarHost
    {
        public static readonly DirectProperty<CommitGraph, Models.CommitGraph> GraphProperty =
            AvaloniaProperty.RegisterDirect<CommitGraph, Models.CommitGraph>(
                nameof(Graph),
                static o => o.Graph,
                static (o, v) => o.Graph = v);

        public Models.CommitGraph Graph
        {
            get => _graph;
            set => SetAndRaise(GraphProperty, ref _graph, value);
        }

        public static readonly DirectProperty<CommitGraph, Models.CommitGraphLayout> LayoutProperty =
            AvaloniaProperty.RegisterDirect<CommitGraph, Models.CommitGraphLayout>(
                nameof(Layout),
                static o => o.Layout,
                static (o, v) => o.Layout = v);

        public Models.CommitGraphLayout Layout
        {
            get => _layout;
            set => SetAndRaise(LayoutProperty, ref _layout, value);
        }

        public static readonly StyledProperty<IBrush> DotBrushProperty =
            AvaloniaProperty.Register<CommitGraph, IBrush>(nameof(DotBrush), Brushes.Transparent);

        public IBrush DotBrush
        {
            get => GetValue(DotBrushProperty);
            set => SetValue(DotBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> RowBrushProperty =
            AvaloniaProperty.Register<CommitGraph, IBrush>(nameof(RowBrush), Brushes.Transparent);

        public IBrush RowBrush
        {
            get => GetValue(RowBrushProperty);
            set => SetValue(RowBrushProperty, value);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_graph == null || _layout == null)
                return;

            var startY = _layout.StartY;
            var clipWidth = _layout.ClipWidth;
            var clipHeight = Bounds.Height;
            var rowHeight = _layout.RowHeight;
            var endY = startY + clipHeight + 28;

            using (context.PushClip(new Rect(0, 0, clipWidth, clipHeight)))
            using (context.PushTransform(Matrix.CreateTranslation(0, -startY)))
            {
                DrawRowBackgrounds(context, _graph, startY, endY, rowHeight, clipWidth);
                DrawCurves(context, _graph, startY, endY, rowHeight);
                DrawAnchors(context, _graph, startY, endY, rowHeight);
            }
        }

        public Models.Commit CommitAt(Point point, double hitRadius = 14)
        {
            if (_graph == null || _layout == null)
                return null;

            var rowHeight = _layout.RowHeight;
            foreach (var dot in _graph.Dots)
            {
                var center = new Point(dot.Center.X, dot.Center.Y * rowHeight - _layout.StartY);
                if (Math.Abs(center.Y - point.Y) > hitRadius)
                    continue;

                var delta = center - point;
                if (delta.X * delta.X + delta.Y * delta.Y <= hitRadius * hitRadius)
                    return dot.Commit;
            }

            return null;
        }

        public Models.Commit CommitInRowAt(Point point)
        {
            if (_graph == null || _layout == null || _layout.RowHeight <= 0)
                return null;

            var row = (int)Math.Floor((point.Y + _layout.StartY) / _layout.RowHeight);
            return row >= 0 && row < _graph.Dots.Count ? _graph.Dots[row].Commit : null;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == GraphProperty ||
                change.Property == LayoutProperty ||
                change.Property == DotBrushProperty ||
                change.Property == RowBrushProperty)
                InvalidateVisual();
        }

        public void OnAvatarResourceChanged(string email, Bitmap image)
        {
            InvalidateVisual();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Models.AvatarManager.Instance.Subscribe(this);
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            Models.AvatarManager.Instance.Unsubscribe(this);
        }

        private void DrawRowBackgrounds(DrawingContext context, Models.CommitGraph graph, double top, double bottom, double rowHeight, double clipWidth)
        {
            foreach (var dot in graph.Dots)
            {
                var centerY = dot.Center.Y * rowHeight;
                if (centerY + rowHeight / 2 < top)
                    continue;
                if (centerY - rowHeight / 2 > bottom)
                    break;

                var radius = dot.Type == Models.CommitGraph.DotType.Head ? 13.5 : 12.5;
                var left = dot.Center.X + radius;
                context.FillRectangle(RowBrush, new Rect(left, centerY - rowHeight / 2, Math.Max(0, clipWidth - left), rowHeight));
            }
        }

        private void DrawCurves(DrawingContext context, Models.CommitGraph graph, double top, double bottom, double rowHeight)
        {
            var grayedPen = new Pen(new SolidColorBrush(Colors.Gray, 0.4), Models.CommitGraph.Pens[0].Thickness);

            foreach (var link in graph.Links)
            {
                var startY = link.Start.Y * rowHeight;
                var endY = link.End.Y * rowHeight;

                if (endY < top)
                    continue;
                if (startY > bottom)
                    break;

                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    ctx.BeginFigure(new Point(link.Start.X, startY), false);
                    ctx.QuadraticBezierTo(new Point(link.Control.X, link.Control.Y * rowHeight), new Point(link.End.X, endY));
                }

                var pen = link.IsHighlighted ? Models.CommitGraph.Pens[link.Color] : grayedPen;
                context.DrawGeometry(null, pen, geo);
            }

            foreach (var line in graph.Paths)
            {
                var last = new Point(line.Points[0].X, line.Points[0].Y * rowHeight);
                var size = line.Points.Count;
                var endY = line.Points[size - 1].Y * rowHeight;

                if (endY < top)
                    continue;
                if (last.Y > bottom)
                    break;

                var geo = new StreamGeometry();
                var pen = line.IsHighlighted ? Models.CommitGraph.Pens[line.Color] : grayedPen;

                using (var ctx = geo.Open())
                {
                    var started = false;
                    var ended = false;
                    for (int i = 1; i < size; i++)
                    {
                        var cur = new Point(line.Points[i].X, line.Points[i].Y * rowHeight);
                        if (cur.Y < top)
                        {
                            last = cur;
                            continue;
                        }

                        if (!started)
                        {
                            ctx.BeginFigure(last, false);
                            started = true;
                        }

                        if (cur.Y > bottom)
                        {
                            cur = new Point(cur.X, bottom);
                            ended = true;
                        }

                        if (cur.X > last.X)
                        {
                            ctx.QuadraticBezierTo(new Point(cur.X, last.Y), cur);
                        }
                        else if (cur.X < last.X)
                        {
                            if (i < size - 1)
                            {
                                var midY = (last.Y + cur.Y) / 2;
                                ctx.CubicBezierTo(new Point(last.X, midY + 4), new Point(cur.X, midY - 4), cur);
                            }
                            else
                            {
                                ctx.QuadraticBezierTo(new Point(last.X, cur.Y), cur);
                            }
                        }
                        else
                        {
                            ctx.LineTo(cur);
                        }

                        if (ended)
                            break;
                        last = cur;
                    }
                }

                context.DrawGeometry(null, pen, geo);
            }
        }

        private void DrawAnchors(DrawingContext context, Models.CommitGraph graph, double top, double bottom, double rowHeight)
        {
            var dotFill = DotBrush;
            var grayedPen = new Pen(Brushes.Gray, Models.CommitGraph.Pens[0].Thickness);

            foreach (var dot in graph.Dots)
            {
                var center = new Point(dot.Center.X, dot.Center.Y * rowHeight);

                if (center.Y < top)
                    continue;
                if (center.Y > bottom)
                    break;

                var pen = dot.IsHighlighted ? Models.CommitGraph.Pens[dot.Color] : grayedPen;
                var radius = dot.Type == Models.CommitGraph.DotType.Head ? 13.5 : 12.5;
                var avatar = string.IsNullOrWhiteSpace(dot.Author?.Email)
                    ? null
                    : Models.AvatarManager.Instance.Request(dot.Author.Email, false);

                context.DrawEllipse(dotFill, new Pen(pen.Brush, 2.2), center, radius, radius);

                if (avatar != null)
                {
                    var imageRadius = radius - 2.3;
                    var imageRect = new Rect(
                        center.X - imageRadius,
                        center.Y - imageRadius,
                        imageRadius * 2,
                        imageRadius * 2);

                    using (context.PushClip(new RoundedRect(imageRect, imageRadius)))
                        context.DrawImage(avatar, imageRect);
                }
                else
                {
                    var imageRadius = radius - 2.3;
                    var imageRect = new Rect(
                        center.X - imageRadius,
                        center.Y - imageRadius,
                        imageRadius * 2,
                        imageRadius * 2);

                    using (context.PushClip(new RoundedRect(imageRect, imageRadius)))
                        Avatar.DrawGitHubStyleFallback(context, imageRect, dot.Author ?? Models.User.Invalid);
                }
            }
        }

        private Models.CommitGraph _graph = null;
        private Models.CommitGraphLayout _layout = null;
    }
}
