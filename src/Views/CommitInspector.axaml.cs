using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace SourceGit.Views
{
    public partial class CommitInspector : UserControl
    {
        public static readonly DirectProperty<CommitInspector, bool> ShowAllFilesProperty =
            AvaloniaProperty.RegisterDirect<CommitInspector, bool>(
                nameof(ShowAllFiles),
                static o => o.ShowAllFiles,
                static (o, v) => o.ShowAllFiles = v);

        public bool ShowAllFiles
        {
            get => _showAllFiles;
            set => SetAndRaise(ShowAllFilesProperty, ref _showAllFiles, value);
        }

        public CommitInspector()
        {
            InitializeComponent();
            SyncChangeViewMode();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is ViewModels.CommitDetail vm)
                vm.ActiveTabIndex = 0;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && DataContext is ViewModels.CommitDetail { IsEditingMessage: true } vm)
            {
                vm.CancelMessageEdit();
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        private void OnViewWorkingDirectory(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CommitDetail vm)
                vm.OpenWorkingDirectory();

            e.Handled = true;
        }

        private async void OnCopyCommitSHA(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CommitDetail { Commit: { } commit })
                await this.CopyTextAsync(commit.SHA);

            e.Handled = true;
        }

        private async void OnMessagePointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
                DataContext is not ViewModels.CommitDetail vm)
                return;

            if (await vm.BeginMessageEditAsync())
            {
                Dispatcher.UIThread.Post(() =>
                {
                    MessageSubjectEditor.Focus();
                    MessageSubjectEditor.CaretIndex = 0;
                });
            }

            e.Handled = true;
        }

        private async void OnRecomposeMessageWithAI(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CommitDetail vm)
                await vm.RecomposeMessageWithAIAsync();

            e.Handled = true;
        }

        private async void OnApplyMessageEdit(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CommitDetail vm)
                await vm.ApplyMessageEditAsync();

            e.Handled = true;
        }

        private void OnCancelMessageEdit(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CommitDetail vm)
                vm.CancelMessageEdit();

            e.Handled = true;
        }

        private void OnParentPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
                DataContext is ViewModels.CommitDetail { ParentSHA.Length: > 0 } vm)
                vm.NavigateTo(vm.ParentSHA);

            e.Handled = true;
        }

        private void OnPathModeSelected(object sender, RoutedEventArgs e)
        {
            ViewModels.Preferences.Instance.CommitChangeViewMode = Models.ChangeViewMode.List;
            SyncChangeViewMode();
            e.Handled = true;
        }

        private void OnTreeModeSelected(object sender, RoutedEventArgs e)
        {
            ViewModels.Preferences.Instance.CommitChangeViewMode = Models.ChangeViewMode.Tree;
            SyncChangeViewMode();
            e.Handled = true;
        }

        private void OnToggleAllFiles(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CommitDetail vm)
            {
                vm.ActiveTabIndex = ShowAllFiles ? 2 : 0;
                if (!ShowAllFiles)
                    _ = vm.ViewRevisionFileAsync(null);
            }

            e.Handled = true;
        }

        private void OnChangeListPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton != MouseButton.Left || DataContext is not ViewModels.CommitDetail vm)
                return;

            Dispatcher.UIThread.Post(vm.OpenSelectedChange);
        }

        private void SyncChangeViewMode()
        {
            if (PathModeButton == null || TreeModeButton == null)
                return;

            var tree = ViewModels.Preferences.Instance.CommitChangeViewMode == Models.ChangeViewMode.Tree;
            PathModeButton.IsChecked = !tree;
            TreeModeButton.IsChecked = tree;
        }

        private bool _showAllFiles;
    }
}
