using System;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    public partial class CommitChanges : UserControl
    {
        public static readonly DirectProperty<CommitChanges, bool> UseExternalDiffHostProperty =
            AvaloniaProperty.RegisterDirect<CommitChanges, bool>(
                nameof(UseExternalDiffHost),
                static o => o.UseExternalDiffHost,
                static (o, v) => o.UseExternalDiffHost = v);

        public static readonly DirectProperty<CommitChanges, GridLength> ChangeListWidthProperty =
            AvaloniaProperty.RegisterDirect<CommitChanges, GridLength>(
                nameof(ChangeListWidth),
                static o => o.ChangeListWidth,
                static (o, v) => o.ChangeListWidth = v);

        public bool UseExternalDiffHost
        {
            get => _useExternalDiffHost;
            set
            {
                if (_useExternalDiffHost == value)
                    return;

                SetAndRaise(UseExternalDiffHostProperty, ref _useExternalDiffHost, value);
                ApplyDiffHostMode();
            }
        }

        public GridLength ChangeListWidth
        {
            get => _changeListWidth;
            set
            {
                if (_changeListWidth == value)
                    return;

                SetAndRaise(ChangeListWidthProperty, ref _changeListWidth, value);
                if (!_isApplyingDiffHostMode && !UseExternalDiffHost)
                    ViewModels.Preferences.Instance.Layout.CommitDetailChangesLeftWidth = value;
            }
        }

        public CommitChanges()
        {
            InitializeComponent();
            ApplyDiffHostMode();
        }

        private void ApplyDiffHostMode()
        {
            if (LayoutGrid == null)
                return;

            _isApplyingDiffHostMode = true;
            if (UseExternalDiffHost)
            {
                ChangeListWidth = new GridLength(1, GridUnitType.Star);
                LayoutGrid.ColumnDefinitions[1].Width = new GridLength(0);
                LayoutGrid.ColumnDefinitions[2].Width = new GridLength(0);
                EmbeddedDiffSplitter.IsVisible = false;
                EmbeddedDiffPanel.IsVisible = false;
                EmbeddedDiffPanel.DataContext = null;
            }
            else
            {
                ChangeListWidth = ViewModels.Preferences.Instance.Layout.CommitDetailChangesLeftWidth;
                LayoutGrid.ColumnDefinitions[1].Width = new GridLength(4);
                LayoutGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                EmbeddedDiffSplitter.IsVisible = true;
                EmbeddedDiffPanel.IsVisible = true;
                EmbeddedDiffPanel.ClearValue(DataContextProperty);
            }
            _isApplyingDiffHostMode = false;
        }

        private void OnCloseExternalDiff(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CommitDetail vm)
                vm.SelectedChanges = [];

            e.Handled = true;
        }

        private void OnChangeContextRequested(object sender, ContextRequestedEventArgs e)
        {
            e.Handled = true;

            if (sender is not ChangeCollectionView { SelectedChanges: { Count: > 0 } changes } view)
                return;

            var detailView = this.FindAncestorOfType<CommitDetail>();
            if (detailView == null)
                return;

            var container = view.FindDescendantOfType<ChangeCollectionContainer>();
            if (container is { SelectedItems.Count: 1, SelectedItem: ViewModels.ChangeTreeNode { IsFolder: true } node })
                detailView.CreateChangeContextMenuByFolder(node, changes)?.Open(view);
            else if (changes.Count > 1)
                detailView.CreateMultipleChangesContextMenu(changes)?.Open(view);
            else
                detailView.CreateChangeContextMenu(changes[0])?.Open(view);
        }

        private async void OnChangeCollectionViewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not ViewModels.CommitDetail vm)
                return;

            if (sender is not ChangeCollectionView { SelectedChanges: { Count: > 0 } selectedChanges } view)
                return;

            var cmdKey = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
            if (e.Key == Key.C && e.KeyModifiers.HasFlag(cmdKey))
            {
                var builder = new StringBuilder();
                var copyAbsPath = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                var container = view.FindDescendantOfType<ChangeCollectionContainer>();
                if (container is { SelectedItems.Count: 1, SelectedItem: ViewModels.ChangeTreeNode { IsFolder: true } node })
                {
                    builder.Append(copyAbsPath ? vm.GetAbsPath(node.FullPath) : node.FullPath);
                }
                else if (selectedChanges.Count == 1)
                {
                    builder.Append(copyAbsPath ? vm.GetAbsPath(selectedChanges[0].Path) : selectedChanges[0].Path);
                }
                else
                {
                    foreach (var c in selectedChanges)
                        builder.AppendLine(copyAbsPath ? vm.GetAbsPath(c.Path) : c.Path);
                }

                await this.CopyTextAsync(builder.ToString());
                e.Handled = true;
            }
            else if (e.Key == Key.F && e.KeyModifiers == cmdKey)
            {
                CommitChangeSearchBox.Focus();
                e.Handled = true;
            }
        }

        private bool _useExternalDiffHost;
        private bool _isApplyingDiffHostMode;
        private GridLength _changeListWidth = ViewModels.Preferences.Instance.Layout.CommitDetailChangesLeftWidth;
    }
}
