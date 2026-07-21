using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class CommitDetailSharedData
    {
        public int ActiveTabIndex
        {
            get;
            set;
        }

        public CommitDetailSharedData()
        {
            ActiveTabIndex = Preferences.Instance.ShowChangesInCommitDetailByDefault ? 1 : 0;
        }
    }

    public partial class CommitDetail : ObservableObject
    {
        public Repository Repository
        {
            get => _repo;
        }

        public int ActiveTabIndex
        {
            get => _sharedData.ActiveTabIndex;
            set
            {
                if (value != _sharedData.ActiveTabIndex)
                {
                    _sharedData.ActiveTabIndex = value;
                    OnPropertyChanged(nameof(ActiveTabIndex));
                    OnPropertyChanged(nameof(ActiveDiffContext));
                    OnPropertyChanged(nameof(ActiveRevisionFileContent));
                    OnPropertyChanged(nameof(ActiveRevisionFilePath));

                    if (value == 1 && DiffContext == null && _selectedChanges is { Count: 1 })
                        DiffContext = new DiffContext(_repo.FullPath, new Models.DiffOption(_commit, _selectedChanges[0]));
                }
            }
        }

        public Models.Commit Commit
        {
            get => _commit;
            set
            {
                if (_commit != null && value != null && _commit.SHA.Equals(value.SHA, StringComparison.Ordinal))
                    return;

                if (SetProperty(ref _commit, value))
                {
                    ActiveTabIndex = 0;
                    ResetMessageEdit(IsUpdatingMessage);
                    OnPropertyChanged(nameof(CanRewordMessage));
                    OnPropertyChanged(nameof(ParentSHA));
                    Refresh();
                }
            }
        }

        public Models.CommitFullMessage FullMessage
        {
            get => _fullMessage;
            private set
            {
                if (SetProperty(ref _fullMessage, value))
                    UpdateMessageParts(value?.Message);
            }
        }

        public string MessageSubject
        {
            get => _messageSubject;
            private set => SetProperty(ref _messageSubject, value);
        }

        public string MessageBody
        {
            get => _messageBody;
            private set => SetProperty(ref _messageBody, value);
        }

        public bool IsEditingMessage
        {
            get => _isEditingMessage;
            private set => SetProperty(ref _isEditingMessage, value);
        }

        public bool IsPreparingMessageEdit
        {
            get => _isPreparingMessageEdit;
            private set => SetProperty(ref _isPreparingMessageEdit, value);
        }

        public bool IsUpdatingMessage
        {
            get => _isUpdatingMessage;
            private set => SetProperty(ref _isUpdatingMessage, value);
        }

        public bool IsGeneratingMessage
        {
            get => _isGeneratingMessage;
            private set => SetProperty(ref _isGeneratingMessage, value);
        }

        public string EditingSubject
        {
            get => _editingSubject;
            set
            {
                if (SetProperty(ref _editingSubject, value))
                    OnPropertyChanged(nameof(EditingSubjectLength));
            }
        }

        public int EditingSubjectLength => _editingSubject?.Length ?? 0;

        public string EditingBody
        {
            get => _editingBody;
            set => SetProperty(ref _editingBody, value);
        }

        public int RewordAffectedCommitCount
        {
            get => _rewordAffectedCommitCount;
            private set => SetProperty(ref _rewordAffectedCommitCount, value);
        }

        public string RewordWarning => RewordAffectedCommitCount == 1
            ? "Rewording this commit message will rebase 1 commit."
            : $"Rewording this commit message will rebase {RewordAffectedCommitCount} commits.";

        public bool CanRewordMessage => _commit is { IsMerged: true, Parents.Count: > 0 };

        public string ParentSHA => _commit?.Parents is { Count: > 0 } parents ? parents[0] : string.Empty;

        public Models.CommitSignInfo SignInfo
        {
            get => _signInfo;
            private set => SetProperty(ref _signInfo, value);
        }

        public List<Models.CommitLink> WebLinks
        {
            get;
            private set;
        }

        public List<string> Children
        {
            get => _children;
            private set => SetProperty(ref _children, value);
        }

        public List<Models.Change> Changes
        {
            get => _changes;
            set
            {
                if (SetProperty(ref _changes, value))
                    OnPropertyChanged(nameof(ChangeSummary));
            }
        }

        public string ChangeSummary => BuildChangeSummary(_changes);

        public List<Models.Change> VisibleChanges
        {
            get => _visibleChanges;
            set => SetProperty(ref _visibleChanges, value);
        }

        public List<Models.Change> SelectedChanges
        {
            get => _selectedChanges;
            set
            {
                if (SetProperty(ref _selectedChanges, value))
                {
                    if (ActiveTabIndex != 1 || value is not { Count: 1 })
                        DiffContext = null;
                    else
                        DiffContext = new DiffContext(_repo.FullPath, new Models.DiffOption(_commit, value[0]), _diffContext);
                }
            }
        }

        public DiffContext DiffContext
        {
            get => _diffContext;
            private set
            {
                if (SetProperty(ref _diffContext, value))
                    OnPropertyChanged(nameof(ActiveDiffContext));
            }
        }

        public DiffContext ActiveDiffContext => ActiveTabIndex == 1 ? _diffContext : null;

        public string SearchChangeFilter
        {
            get => _searchChangeFilter;
            set
            {
                if (SetProperty(ref _searchChangeFilter, value))
                    RefreshVisibleChanges();
            }
        }

        public string ViewRevisionFilePath
        {
            get => _viewRevisionFilePath;
            private set
            {
                if (SetProperty(ref _viewRevisionFilePath, value))
                    OnPropertyChanged(nameof(ActiveRevisionFilePath));
            }
        }

        public object ViewRevisionFileContent
        {
            get => _viewRevisionFileContent;
            private set
            {
                if (SetProperty(ref _viewRevisionFileContent, value))
                    OnPropertyChanged(nameof(ActiveRevisionFileContent));
            }
        }

        public string ActiveRevisionFilePath => ActiveTabIndex == 2 ? _viewRevisionFilePath : null;

        public object ActiveRevisionFileContent => ActiveTabIndex == 2 ? _viewRevisionFileContent : null;

        public string RevisionFileSearchFilter
        {
            get => _revisionFileSearchFilter;
            set
            {
                if (SetProperty(ref _revisionFileSearchFilter, value))
                    RefreshRevisionSearchSuggestion();
            }
        }

        public List<string> RevisionFileSearchSuggestion
        {
            get => _revisionFileSearchSuggestion;
            private set => SetProperty(ref _revisionFileSearchSuggestion, value);
        }

        public bool CanOpenRevisionFileWithDefaultEditor
        {
            get => _canOpenRevisionFileWithDefaultEditor;
            private set => SetProperty(ref _canOpenRevisionFileWithDefaultEditor, value);
        }

        public Vector ScrollOffset
        {
            get => _scrollOffset;
            set => SetProperty(ref _scrollOffset, value);
        }

        public CommitDetail(Repository repo, CommitDetailSharedData sharedData)
        {
            _repo = repo;
            _sharedData = sharedData ?? new CommitDetailSharedData();
            WebLinks = Models.CommitLink.Get(repo.Remotes);
        }

        public CommitDetail Clone()
        {
            var cloned = new CommitDetail(_repo, null);
            cloned.ActiveTabIndex = ActiveTabIndex;
            cloned.Commit = _commit;
            return cloned;
        }

        public void NavigateTo(string commitSHA)
        {
            _repo?.NavigateToCommit(commitSHA);
        }

        public void OpenWorkingDirectory()
        {
            _repo.SelectedViewIndex = 1;
        }

        public void OpenSelectedChange()
        {
            if (_selectedChanges is not { Count: 1 })
                return;

            ActiveTabIndex = 1;
        }

        public async Task<bool> BeginMessageEditAsync()
        {
            if (IsUpdatingMessage)
                return false;

            if (IsEditingMessage)
                return true;

            if (!CanRewordMessage)
            {
                _repo.SendNotification("This commit cannot be reworded from the current branch.", true);
                return false;
            }

            EditingSubject = MessageSubject;
            EditingBody = MessageBody;
            _messageEditCommitSHA = _commit.SHA;
            IsEditingMessage = true;

            var prepared = await PrepareMessageRebaseAsync().ConfigureAwait(true);
            if (!prepared)
                CancelMessageEdit();
            return prepared;
        }

        public void CancelMessageEdit()
        {
            if (IsUpdatingMessage)
                return;

            ResetMessageEdit(false);
        }

        private void ResetMessageEdit(bool preserveUpdateState)
        {
            _messageRebase = null;
            _messageEditCommitSHA = null;
            IsPreparingMessageEdit = false;
            if (!preserveUpdateState)
                IsUpdatingMessage = false;
            IsGeneratingMessage = false;
            IsEditingMessage = false;
            RewordAffectedCommitCount = 0;
            OnPropertyChanged(nameof(RewordWarning));
        }

        public async Task<bool> ApplyMessageEditAsync()
        {
            if (!IsEditingMessage || IsPreparingMessageEdit || IsUpdatingMessage)
                return false;

            if (!_commit.SHA.Equals(_messageEditCommitSHA, StringComparison.Ordinal))
                return false;

            var message = ComposeEditingMessage();
            if (string.IsNullOrWhiteSpace(message))
                return false;

            if (_messageRebase == null && !await PrepareMessageRebaseAsync().ConfigureAwait(true))
                return false;

            var targetSHA = _messageEditCommitSHA;
            if (!_messageRebase.ConfigureReword(targetSHA, message))
            {
                _repo.SendNotification("The selected commit is no longer available for rewording.", true);
                return false;
            }

            IsUpdatingMessage = true;
            var succ = await _messageRebase.Start().ConfigureAwait(true);
            IsUpdatingMessage = false;

            if (!succ)
            {
                _repo.SendNotification("Failed to update the commit message. See the command log for details.", true);
                return false;
            }

            if (_commit.SHA.Equals(targetSHA, StringComparison.Ordinal))
                FullMessage = new Models.CommitFullMessage { Message = message };
            CancelMessageEdit();
            _repo.RefreshAll();
            return true;
        }

        public async Task RecomposeMessageWithAIAsync()
        {
            if (IsGeneratingMessage)
                return;

            var services = _repo.GetPreferredOpenAIServices();
            if (services.Count == 0)
            {
                _repo.SendNotification("No AI service is configured.", true);
                return;
            }

            if (!IsEditingMessage && !await BeginMessageEditAsync().ConfigureAwait(true))
                return;

            IsGeneratingMessage = true;
            var targetSHA = _messageEditCommitSHA;
            var assistant = new AIAssistant(_repo, services[0], _changes);
            await assistant.GenAsync().ConfigureAwait(true);
            IsGeneratingMessage = false;

            if (!IsEditingMessage || !_commit.SHA.Equals(targetSHA, StringComparison.Ordinal))
                return;

            if (!string.IsNullOrWhiteSpace(assistant.Response))
                SetEditingMessage(assistant.Response);
            else
                _repo.SendNotification("AI did not return a commit message.", true);
        }

        public async Task<List<Models.Decorator>> GetRefsContainsThisCommitAsync()
        {
            return await new Commands.QueryRefsContainsCommit(_repo.FullPath, _commit.SHA)
                .GetResultAsync()
                .ConfigureAwait(false);
        }

        public void ClearSearchChangeFilter()
        {
            SearchChangeFilter = string.Empty;
        }

        public void ClearRevisionFileSearchFilter()
        {
            RevisionFileSearchFilter = string.Empty;
        }

        public void CancelRevisionFileSuggestions()
        {
            RevisionFileSearchSuggestion = null;
        }

        public async Task<Models.Commit> GetCommitAsync(string sha)
        {
            return await new Commands.QuerySingleCommit(_repo.FullPath, sha)
                .GetResultAsync()
                .ConfigureAwait(false);
        }

        public string GetAbsPath(string path)
        {
            return Native.OS.GetAbsPath(_repo.FullPath, path);
        }

        public void OpenChangeInMergeTool(Models.Change c)
        {
            new Commands.DiffTool(_repo.FullPath, new Models.DiffOption(_commit, c)).Open();
        }

        public async Task SaveChangesAsPatchAsync(List<Models.Change> changes, string saveTo)
        {
            if (_commit == null)
                return;

            var succ = await Commands.SaveChangesAsPatch.ProcessRevisionCompareChangesAsync(
                _repo.FullPath,
                changes,
                _commit.FirstParentToCompare,
                _commit.SHA,
                saveTo);

            if (succ)
                _repo.SendNotification(App.Text("SaveAsPatchSuccess"));
        }

        public async Task ResetToThisRevisionAsync(string path)
        {
            var c = _changes?.Find(x => x.Path.Equals(path, StringComparison.Ordinal));
            if (c != null)
            {
                await ResetToThisRevisionAsync(c);
                return;
            }

            var log = _repo.CreateLog($"Reset File to '{_commit.SHA}'");
            await new Commands.Checkout(_repo.FullPath).Use(log).FileWithRevisionAsync(path, _commit.SHA);
            log.Complete();
        }

        public async Task ResetToThisRevisionAsync(Models.Change change)
        {
            var log = _repo.CreateLog($"Reset File to '{_commit.SHA}'");

            if (change.Index == Models.ChangeState.Deleted)
            {
                var fullpath = Native.OS.GetAbsPath(_repo.FullPath, change.Path);
                if (File.Exists(fullpath))
                    await new Commands.Remove(_repo.FullPath, [change.Path])
                        .Use(log)
                        .ExecAsync();
            }
            else if (change.Index == Models.ChangeState.Renamed)
            {
                var old = Native.OS.GetAbsPath(_repo.FullPath, change.OriginalPath);
                if (File.Exists(old))
                    await new Commands.Remove(_repo.FullPath, [change.OriginalPath])
                        .Use(log)
                        .ExecAsync();

                await new Commands.Checkout(_repo.FullPath)
                    .Use(log)
                    .FileWithRevisionAsync(change.Path, _commit.SHA);
            }
            else
            {
                await new Commands.Checkout(_repo.FullPath)
                    .Use(log)
                    .FileWithRevisionAsync(change.Path, _commit.SHA);
            }

            log.Complete();
        }

        public async Task ResetToParentRevisionAsync(Models.Change change)
        {
            var log = _repo.CreateLog($"Reset File to '{_commit.SHA}~1'");

            if (change.Index == Models.ChangeState.Added)
            {
                var fullpath = Native.OS.GetAbsPath(_repo.FullPath, change.Path);
                if (File.Exists(fullpath))
                    await new Commands.Remove(_repo.FullPath, [change.Path])
                        .Use(log)
                        .ExecAsync();
            }
            else if (change.Index == Models.ChangeState.Renamed)
            {
                var renamed = Native.OS.GetAbsPath(_repo.FullPath, change.Path);
                if (File.Exists(renamed))
                    await new Commands.Remove(_repo.FullPath, [change.Path])
                        .Use(log)
                        .ExecAsync();

                await new Commands.Checkout(_repo.FullPath)
                    .Use(log)
                    .FileWithRevisionAsync(change.OriginalPath, $"{_commit.SHA}~1");
            }
            else
            {
                await new Commands.Checkout(_repo.FullPath)
                    .Use(log)
                    .FileWithRevisionAsync(change.Path, $"{_commit.SHA}~1");
            }

            log.Complete();
        }

        public async Task ResetMultipleToThisRevisionAsync(List<Models.Change> changes)
        {
            var checkouts = new List<string>();
            var removes = new List<string>();

            foreach (var c in changes)
            {
                if (c.Index == Models.ChangeState.Deleted)
                {
                    var fullpath = Native.OS.GetAbsPath(_repo.FullPath, c.Path);
                    if (File.Exists(fullpath))
                        removes.Add(c.Path);
                }
                else if (c.Index == Models.ChangeState.Renamed)
                {
                    var old = Native.OS.GetAbsPath(_repo.FullPath, c.OriginalPath);
                    if (File.Exists(old))
                        removes.Add(c.OriginalPath);

                    checkouts.Add(c.Path);
                }
                else
                {
                    checkouts.Add(c.Path);
                }
            }

            var log = _repo.CreateLog($"Reset Files to '{_commit.SHA}'");

            if (removes.Count > 0)
                await new Commands.Remove(_repo.FullPath, removes)
                    .Use(log)
                    .ExecAsync();

            if (checkouts.Count > 0)
                await new Commands.Checkout(_repo.FullPath)
                    .Use(log)
                    .MultipleFilesWithRevisionAsync(checkouts, _commit.SHA);

            log.Complete();
        }

        public async Task ResetMultipleToParentRevisionAsync(List<Models.Change> changes)
        {
            var checkouts = new List<string>();
            var removes = new List<string>();

            foreach (var c in changes)
            {
                if (c.Index == Models.ChangeState.Added)
                {
                    var fullpath = Native.OS.GetAbsPath(_repo.FullPath, c.Path);
                    if (File.Exists(fullpath))
                        removes.Add(c.Path);
                }
                else if (c.Index == Models.ChangeState.Renamed)
                {
                    var renamed = Native.OS.GetAbsPath(_repo.FullPath, c.Path);
                    if (File.Exists(renamed))
                        removes.Add(c.Path);

                    checkouts.Add(c.OriginalPath);
                }
                else
                {
                    checkouts.Add(c.Path);
                }
            }

            var log = _repo.CreateLog($"Reset Files to '{_commit.SHA}~1'");

            if (removes.Count > 0)
                await new Commands.Remove(_repo.FullPath, removes)
                    .Use(log)
                    .ExecAsync();

            if (checkouts.Count > 0)
                await new Commands.Checkout(_repo.FullPath)
                    .Use(log)
                    .MultipleFilesWithRevisionAsync(checkouts, $"{_commit.SHA}~1");

            log.Complete();
        }

        public async Task<List<Models.Object>> GetRevisionFilesUnderFolderAsync(string parentFolder)
        {
            return await new Commands.QueryRevisionObjects(_repo.FullPath, _commit.SHA, parentFolder)
                .GetResultAsync()
                .ConfigureAwait(false);
        }

        public async Task ViewRevisionFileAsync(Models.Object file)
        {
            var obj = file ?? new Models.Object() { Path = string.Empty, Type = Models.ObjectType.None };
            ViewRevisionFilePath = obj.Path;

            switch (obj.Type)
            {
                case Models.ObjectType.Blob:
                    CanOpenRevisionFileWithDefaultEditor = true;
                    await SetViewingBlobAsync(obj);
                    break;
                case Models.ObjectType.Commit:
                    CanOpenRevisionFileWithDefaultEditor = false;
                    await SetViewingCommitAsync(obj);
                    break;
                default:
                    CanOpenRevisionFileWithDefaultEditor = false;
                    ViewRevisionFileContent = null;
                    break;
            }
        }

        public async Task OpenRevisionFileAsync(string file, Models.ExternalTool tool)
        {
            var fullPath = Native.OS.GetAbsPath(_repo.FullPath, file);
            var fileName = Path.GetFileNameWithoutExtension(fullPath) ?? "";
            var fileExt = Path.GetExtension(fullPath) ?? "";
            var tmpFile = Path.Combine(Path.GetTempPath(), $"{fileName}~{_commit.SHA.AsSpan(0, 10)}{fileExt}");

            await Commands.SaveRevisionFile
                .RunAsync(_repo.FullPath, _commit.SHA, file, tmpFile)
                .ConfigureAwait(false);

            if (tool == null)
                Native.OS.OpenWithDefaultEditor(tmpFile);
            else
                tool.Launch(tmpFile.Quoted());
        }

        public async Task SaveRevisionFileAsync(Models.Object file, string saveTo)
        {
            await Commands.SaveRevisionFile
                .RunAsync(_repo.FullPath, _commit.SHA, file.Path, saveTo)
                .ConfigureAwait(false);
        }

        private void Refresh()
        {
            _requestingRevisionFiles = false;
            _revisionFiles = null;

            SignInfo = null;
            ViewRevisionFileContent = null;
            ViewRevisionFilePath = string.Empty;
            CanOpenRevisionFileWithDefaultEditor = false;
            Children = null;
            RevisionFileSearchFilter = string.Empty;
            RevisionFileSearchSuggestion = null;
            ScrollOffset = Vector.Zero;
            FullMessage = null;
            MessageSubject = _commit?.Subject ?? string.Empty;
            MessageBody = string.Empty;

            if (_commit == null)
            {
                Changes = [];
                VisibleChanges = [];
                SelectedChanges = null;
                return;
            }

            if (_cancellationSource is { IsCancellationRequested: false })
                _cancellationSource.Cancel();

            _cancellationSource = new CancellationTokenSource();
            var token = _cancellationSource.Token;

            Task.Run(async () =>
            {
                var message = await new Commands.QueryCommitFullMessage(_repo.FullPath, _commit.SHA)
                    .GetResultAsync()
                    .ConfigureAwait(false);
                var inlines = await ParseInlinesInMessageAsync(message).ConfigureAwait(false);

                if (!token.IsCancellationRequested)
                    Dispatcher.UIThread.Post(() =>
                    {
                        FullMessage = new Models.CommitFullMessage
                        {
                            Message = message,
                            Inlines = inlines
                        };
                    });
            }, token);

            Task.Run(async () =>
            {
                var signInfo = await new Commands.QueryCommitSignInfo(_repo.FullPath, _commit.SHA, !_repo.HasAllowedSignersFile)
                    .GetResultAsync()
                    .ConfigureAwait(false);

                if (!token.IsCancellationRequested)
                    Dispatcher.UIThread.Post(() => SignInfo = signInfo);
            }, token);

            if (Preferences.Instance.ShowChildren)
            {
                Task.Run(async () =>
                {
                    var max = Preferences.Instance.MaxHistoryCommits;
                    var cmd = new Commands.QueryCommitChildren(_repo.FullPath, _commit.SHA, max) { CancellationToken = token };
                    var children = await cmd.GetResultAsync().ConfigureAwait(false);
                    if (!token.IsCancellationRequested)
                        Dispatcher.UIThread.Post(() => Children = children);
                }, token);
            }

            Task.Run(async () =>
            {
                var cmd = new Commands.CompareRevisions(_repo.FullPath, _commit.FirstParentToCompare, _commit.SHA) { CancellationToken = token };
                var changes = await cmd.ReadAsync().ConfigureAwait(false);
                var visible = changes;
                if (!string.IsNullOrWhiteSpace(_searchChangeFilter))
                {
                    visible = new List<Models.Change>();
                    foreach (var c in changes)
                    {
                        if (c.Path.Contains(_searchChangeFilter, StringComparison.OrdinalIgnoreCase))
                            visible.Add(c);
                    }
                }

                if (!token.IsCancellationRequested)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Changes = changes;
                        VisibleChanges = visible;

                        SelectedChanges = null;
                    });
                }
            }, token);
        }

        private async Task<Models.InlineElementCollector> ParseInlinesInMessageAsync(string message)
        {
            var inlines = new Models.InlineElementCollector();
            if (_repo.IssueTrackers is { Count: > 0 } rules)
            {
                foreach (var rule in rules)
                    rule.Matches(inlines, message);
            }

            var urlMatches = REG_URL_FORMAT().Matches(message);
            foreach (Match match in urlMatches)
            {
                var start = match.Index;
                var len = match.Length;
                if (inlines.Intersect(start, len) != null)
                    continue;

                var url = message.Substring(start, len);
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    inlines.Add(new Models.InlineElement(Models.InlineElementType.Link, start, len, url));
            }

            var shaMatches = REG_SHA_FORMAT().Matches(message);
            foreach (Match match in shaMatches)
            {
                var start = match.Index;
                var len = match.Length;
                if (inlines.Intersect(start, len) != null)
                    continue;

                var sha = match.Groups[1].Value;
                var isCommitSHA = await new Commands.IsCommitSHA(_repo.FullPath, sha).GetResultAsync().ConfigureAwait(false);
                if (isCommitSHA)
                    inlines.Add(new Models.InlineElement(Models.InlineElementType.CommitSHA, start, len, sha));
            }

            var inlineCodeMatches = REG_INLINECODE_FORMAT().Matches(message);
            foreach (Match match in inlineCodeMatches)
            {
                var start = match.Index;
                var len = match.Length;
                if (inlines.Intersect(start, len) != null)
                    continue;

                inlines.Add(new Models.InlineElement(Models.InlineElementType.Code, start + 1, len - 2, string.Empty));
            }

            inlines.Sort();
            return inlines;
        }

        private void RefreshVisibleChanges()
        {
            if (string.IsNullOrEmpty(_searchChangeFilter))
            {
                VisibleChanges = _changes;
            }
            else
            {
                var visible = new List<Models.Change>();
                foreach (var c in _changes)
                {
                    if (c.Path.Contains(_searchChangeFilter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(c);
                }

                VisibleChanges = visible;
            }
        }

        private void RefreshRevisionSearchSuggestion()
        {
            if (!string.IsNullOrEmpty(_revisionFileSearchFilter))
            {
                if (_revisionFiles == null)
                {
                    if (_requestingRevisionFiles)
                        return;

                    var sha = Commit.SHA;
                    _requestingRevisionFiles = true;

                    Task.Run(async () =>
                    {
                        var files = await new Commands.QueryRevisionFileNames(_repo.FullPath, sha)
                            .GetResultAsync()
                            .ConfigureAwait(false);

                        Dispatcher.UIThread.Post(() =>
                        {
                            if (sha == Commit.SHA && _requestingRevisionFiles)
                            {
                                _revisionFiles = files;
                                _requestingRevisionFiles = false;

                                if (!string.IsNullOrEmpty(_revisionFileSearchFilter))
                                    CalcRevisionFileSearchSuggestion();
                            }
                        });
                    });
                }
                else
                {
                    CalcRevisionFileSearchSuggestion();
                }
            }
            else
            {
                RevisionFileSearchSuggestion = null;
                GC.Collect();
            }
        }

        private void CalcRevisionFileSearchSuggestion()
        {
            var suggestion = new List<string>();
            foreach (var file in _revisionFiles)
            {
                if (file.Contains(_revisionFileSearchFilter, StringComparison.OrdinalIgnoreCase) &&
                    file.Length != _revisionFileSearchFilter.Length)
                    suggestion.Add(file);

                if (suggestion.Count >= 100)
                    break;
            }

            RevisionFileSearchSuggestion = suggestion;
        }

        private async Task SetViewingBlobAsync(Models.Object file)
        {
            var isBinary = await new Commands.IsBinary(_repo.FullPath, _commit.SHA, file.Path).GetResultAsync();
            if (isBinary)
            {
                var imgDecoder = ImageSource.GetDecoder(file.Path);
                if (imgDecoder != Models.ImageDecoder.None)
                {
                    var source = await ImageSource.FromRevisionAsync(_repo.FullPath, _commit.SHA, file.Path, imgDecoder);
                    ViewRevisionFileContent = new Models.RevisionImageFile(file.Path, source.Bitmap, source.Size);
                }
                else
                {
                    var size = await new Commands.QueryFileSize(_repo.FullPath, file.Path, _commit.SHA).GetResultAsync();
                    ViewRevisionFileContent = new Models.RevisionBinaryFile() { Size = size };
                }

                return;
            }

            var contentStream = await Commands.QueryFileContent.RunAsync(_repo.FullPath, _commit.SHA, file.Path);
            var content = await new StreamReader(contentStream).ReadToEndAsync();
            var lfs = Models.LFSObject.Parse(content);
            if (lfs != null)
            {
                var imgDecoder = ImageSource.GetDecoder(file.Path);
                if (imgDecoder != Models.ImageDecoder.None)
                    ViewRevisionFileContent = new RevisionLFSImage(_repo.FullPath, file.Path, lfs, imgDecoder);
                else
                    ViewRevisionFileContent = new Models.RevisionLFSObject() { Object = lfs };
            }
            else
            {
                ViewRevisionFileContent = new Models.RevisionTextFile() { FileName = file.Path, Content = content };
            }
        }

        private async Task SetViewingCommitAsync(Models.Object file)
        {
            var submoduleRoot = Path.Combine(_repo.FullPath, file.Path).Replace('\\', '/').Trim('/');
            var commit = await new Commands.QuerySingleCommit(submoduleRoot, file.SHA).GetResultAsync();
            if (commit == null)
            {
                ViewRevisionFileContent = new Models.RevisionSubmodule()
                {
                    Commit = new Models.Commit() { SHA = file.SHA },
                    FullMessage = new Models.CommitFullMessage()
                };
            }
            else
            {
                var message = await new Commands.QueryCommitFullMessage(submoduleRoot, file.SHA).GetResultAsync();
                ViewRevisionFileContent = new Models.RevisionSubmodule()
                {
                    Commit = commit,
                    FullMessage = new Models.CommitFullMessage { Message = message }
                };
            }
        }

        private async Task<bool> PrepareMessageRebaseAsync()
        {
            if (_messageRebase != null)
                return true;

            var targetSHA = _messageEditCommitSHA;
            if (!IsEditingMessage || string.IsNullOrEmpty(targetSHA) || !_commit.SHA.Equals(targetSHA, StringComparison.Ordinal))
                return false;

            IsPreparingMessageEdit = true;
            var start = await new Commands.QuerySingleCommit(_repo.FullPath, $"{targetSHA}~")
                .GetResultAsync()
                .ConfigureAwait(true);
            if (!IsEditingMessage || !_commit.SHA.Equals(targetSHA, StringComparison.Ordinal))
                return false;

            if (start == null)
            {
                IsPreparingMessageEdit = false;
                _repo.SendNotification("The parent commit required for rewording could not be found.", true);
                return false;
            }

            var prefill = new InteractiveRebasePrefill(targetSHA, Models.InteractiveRebaseAction.Reword);
            var rebase = new InteractiveRebase(_repo, start, prefill);
            await rebase.LoadingTask.ConfigureAwait(true);

            if (!IsEditingMessage || !_commit.SHA.Equals(targetSHA, StringComparison.Ordinal))
                return false;

            _messageRebase = rebase;
            RewordAffectedCommitCount = rebase.Items.Count;
            OnPropertyChanged(nameof(RewordWarning));
            IsPreparingMessageEdit = false;

            if (RewordAffectedCommitCount == 0)
                _repo.SendNotification("The commit range required for rewording could not be loaded.", true);

            return RewordAffectedCommitCount > 0;
        }

        private void UpdateMessageParts(string message)
        {
            var normalized = string.IsNullOrEmpty(message)
                ? _commit?.Subject ?? string.Empty
                : message.ReplaceLineEndings("\n").TrimEnd();
            var firstLineEnd = normalized.IndexOf('\n');
            if (firstLineEnd < 0)
            {
                MessageSubject = normalized;
                MessageBody = string.Empty;
                return;
            }

            MessageSubject = normalized.Substring(0, firstLineEnd);
            MessageBody = normalized.Substring(firstLineEnd + 1).TrimStart('\n');
        }

        private void SetEditingMessage(string message)
        {
            var normalized = message.ReplaceLineEndings("\n").Trim();
            var firstLineEnd = normalized.IndexOf('\n');
            if (firstLineEnd < 0)
            {
                EditingSubject = normalized;
                EditingBody = string.Empty;
            }
            else
            {
                EditingSubject = normalized.Substring(0, firstLineEnd);
                EditingBody = normalized.Substring(firstLineEnd + 1).TrimStart('\n');
            }
        }

        private string ComposeEditingMessage()
        {
            var subject = _editingSubject?.Trim() ?? string.Empty;
            var body = _editingBody?.Trim() ?? string.Empty;
            return string.IsNullOrEmpty(body) ? subject : $"{subject}\n\n{body}";
        }

        private static string BuildChangeSummary(List<Models.Change> changes)
        {
            if (changes == null || changes.Count == 0)
                return "No files changed";

            var counts = new int[9];
            foreach (var change in changes)
            {
                var state = change.Index != Models.ChangeState.None ? change.Index : change.WorkTree;
                counts[(int)state]++;
            }

            var builder = new StringBuilder();
            AppendChangeCount(builder, counts, Models.ChangeState.Modified, "modified");
            AppendChangeCount(builder, counts, Models.ChangeState.Added, "added");
            AppendChangeCount(builder, counts, Models.ChangeState.Deleted, "deleted");
            AppendChangeCount(builder, counts, Models.ChangeState.Renamed, "renamed");
            AppendChangeCount(builder, counts, Models.ChangeState.Copied, "copied");
            AppendChangeCount(builder, counts, Models.ChangeState.TypeChanged, "type changed");
            return builder.Length == 0 ? $"{changes.Count} changed" : builder.ToString();
        }

        private static void AppendChangeCount(StringBuilder builder, int[] counts, Models.ChangeState state, string label)
        {
            var count = counts[(int)state];
            if (count == 0)
                return;

            if (builder.Length > 0)
                builder.Append(", ");
            builder.Append(count).Append(' ').Append(label);
        }

        [GeneratedRegex(@"\b(https?://|ftp://)[\w\d\._/\-~%@()+:?&=#!]*[\w\d/]")]
        private static partial Regex REG_URL_FORMAT();

        [GeneratedRegex(@"\b([0-9a-fA-F]{6,64})\b")]
        private static partial Regex REG_SHA_FORMAT();

        [GeneratedRegex(@"`.*?`")]
        private static partial Regex REG_INLINECODE_FORMAT();

        private Repository _repo = null;
        private CommitDetailSharedData _sharedData = null;
        private Models.Commit _commit = null;
        private Models.CommitFullMessage _fullMessage = null;
        private string _messageSubject = string.Empty;
        private string _messageBody = string.Empty;
        private bool _isEditingMessage = false;
        private bool _isPreparingMessageEdit = false;
        private bool _isUpdatingMessage = false;
        private bool _isGeneratingMessage = false;
        private string _editingSubject = string.Empty;
        private string _editingBody = string.Empty;
        private int _rewordAffectedCommitCount = 0;
        private InteractiveRebase _messageRebase = null;
        private string _messageEditCommitSHA = null;
        private Models.CommitSignInfo _signInfo = null;
        private List<string> _children = null;
        private List<Models.Change> _changes = [];
        private List<Models.Change> _visibleChanges = [];
        private List<Models.Change> _selectedChanges = null;
        private string _searchChangeFilter = string.Empty;
        private DiffContext _diffContext = null;
        private string _viewRevisionFilePath = string.Empty;
        private object _viewRevisionFileContent = null;
        private CancellationTokenSource _cancellationSource = null;
        private bool _requestingRevisionFiles = false;
        private List<string> _revisionFiles = null;
        private string _revisionFileSearchFilter = string.Empty;
        private List<string> _revisionFileSearchSuggestion = null;
        private bool _canOpenRevisionFileWithDefaultEditor = false;
        private Vector _scrollOffset = Vector.Zero;
    }
}
