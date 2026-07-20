using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SourceGit.Views
{
    public class CommitLaneIndicator : Control
    {
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (DataContext is not Models.Commit commit || Models.CommitGraph.Pens.Count == 0)
                return;

            var color = Math.Clamp(commit.Color, 0, Models.CommitGraph.Pens.Count - 1);
            var brush = commit.IsHighlightedInGraph
                ? Models.CommitGraph.Pens[color].Brush
                : Brushes.Gray;
            context.FillRectangle(brush, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            InvalidateVisual();
        }
    }
}
