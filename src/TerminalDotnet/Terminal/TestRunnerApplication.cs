using System.Collections.ObjectModel;
using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Issues;
using TerminalDotnet.Filters;
using TerminalDotnet.Search;

namespace TerminalDotnet.Terminal;

internal sealed class TestRunnerApplication(
    TestExplorerSession session,
    FileExplorerSession fileSession,
    FileExplorerSession folderSession,
    ChangesetSession changesetSession,
    CommentSession commentSession,
    IssueSession issueSession,
    string target,
    IFileOpener? editorLauncher = null,
    IWorkspaceWatcher? workspaceWatcher = null)
{
    private const int ContentInset = 1;
    private const int SegmentGap = 2;
    private const int MaxStatusSegments = 4;
    private const int MaxFilterChips = 5;
    private const int FilterGap = 2;
    private const int StatusRow = ShortcutLines.Rows + 1;
    private const int SearchRow = StatusRow + 1;
    private const int ClearChoice = 0;
    private static readonly TimeSpan SettleDuration = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan EditPollInterval = TimeSpan.FromMilliseconds(250);

    private CancellationTokenSource? runCancellation;
    private CancellationTokenSource? loadCancellation;
    private readonly Stopwatch sinceLoadStarted = new();
    private readonly Stopwatch sincePanelsAppeared = new();
    private IReadOnlyList<VisibleTestNode> testNodes = [];
    private readonly PanelShell shell = new();
    private readonly BackgroundWork panelWork = new();
    private readonly EditBurst outsideEdits = new();
    private readonly EditsSinceTheBuild editsSinceTheBuild = new();
    private readonly Stopwatch sinceRebuildStarted = new();
    private const int ToastPadding = 4;
    private View? toast;
    private Label? toastText;
    private Toast? shownToast;
    private int toastsShown;
    private readonly Stopwatch sinceWatching = new();
    private bool openSourceRequested;
    private bool panelsWereEdited;
    private bool reloadingWhatIsOnDisk;
    private string? openPath;
    private int openLine = 1;
    private int openDialogs;

    private Label? testStatus;
    private IReadOnlyList<Label> segmentLabels = [];
    private IReadOnlyList<Label> filterLabels = [];
    private IReadOnlyList<FilterChip> filterChips = [];
    private IReadOnlyList<FileStatusSegment> statusSegments = [];
    private Label? shortcuts;
    private IReadOnlyList<string> shortcutSegments = [];

    /// <summary>The views of the terminal that is up. Each terminal the app
    /// raises builds its own, so these stand in until the first one does.</summary>
    private TextField search = new();
    private readonly Dictionary<PanelKind, ListPanel> lists = [];

    private View workspace = new();
    private PreviewPanel preview = new();
    private IApplication? running;

    /// <summary>What the preview panel was last asked to show, so it loads
    /// again only when the selection it follows moves.</summary>
    private PreviewSubject? previewed;

    /// <summary>The file the preview panel is showing, which its keys edit and
    /// comment on.</summary>
    private SourceLocation? previewedSource;

    /// <summary>The list taking the keys, or the list the preview follows
    /// while the reader is in the preview.</summary>
    private ListView ActiveList => lists[shell.State.PreviewedList].View;

    public void Run()
    {
        WatchTheWorkingTree();
        while (RunTerminal())
        {
            OpenRequestedFile();
        }
    }

    /// <summary>
    /// The panels are the only thing in the app that knows what the working
    /// tree looked like, and an agent editing alongside the reader moves it
    /// underneath them. Watching starts before the first terminal and outlives
    /// each one, so the edits made while the editor holds the screen are still
    /// there to be reloaded when it hands the screen back.
    /// </summary>
    private void WatchTheWorkingTree()
    {
        if (workspaceWatcher is null)
        {
            return;
        }

        sinceWatching.Restart();
        workspaceWatcher.Watch(
            Path.GetDirectoryName(Path.GetFullPath(target))!,
            path =>
            {
                outsideEdits.Noticed(path, sinceWatching.Elapsed);
                editsSinceTheBuild.Noticed(path);
            });
    }

    private bool RunTerminal()
    {
        openSourceRequested = false;
        openPath = null;
        openLine = 1;
        using IApplication application = Application.Create();
        application.Init(TerminalDriver());
        running = application;
        previewed = null;

        using var window = new Window { Title = $"terminal-dotnet - {VersionNumber.Current}" };
        search = Search();
        workspace = Workspace();
        testStatus = TestStatus();
        testStatus.GettingAttributeForRole += (_, args) =>
        {
            var background = args.Result?.Background ?? Color.Black;
            args.Result = new global::Terminal.Gui.Drawing.Attribute(
                TestStatusAppearance.ForegroundFor(session.State),
                background);
            args.Handled = true;
        };
        segmentLabels = StatusSegmentLabels();
        filterLabels = FilterLabels();
        shortcuts = Shortcuts();
        shortcuts.ViewportChanged += (_, _) => ShowShortcuts();

        window.Add(workspace, search, testStatus, shortcuts);
        window.Add([.. segmentLabels]);
        window.Add([.. filterLabels]);
        toast = Toast();
        window.Add(toast);
        search.ValueChanged += async (_, _) =>
        {
            await SearchAsync(search.Text);
            Render();
        };
        application.Keyboard.KeyDown += (_, key) =>
        {
            HandleKey(application, key);
            HoldTheFocus(key);
        };
        Render();
        sincePanelsAppeared.Restart();
        ActiveList.SetFocus();
        FillPanels(application);
        RefreshEditedPanels(application);
        ReloadWhenTheWorkingTreeSettles(application);

        application.Run(window);
        ShutDown();
        return openSourceRequested;
    }

    /// <summary>
    /// Cancelling a run only asks its process tree to end, so the panels'
    /// work is waited out before the application is torn down. Returning
    /// first would leave `dotnet test` orphaned behind the exiting terminal.
    /// </summary>
    private void ShutDown()
    {
        runCancellation?.Cancel();
        loadCancellation?.Cancel();
        panelWork.EndedAsync().GetAwaiter().GetResult();
        loadCancellation?.Dispose();
        loadCancellation = null;
    }

    /// <summary>
    /// The panels are painted before anything is loaded, so opening the app
    /// does not wait on test discovery, which builds the solution. Each panel
    /// fills in as its own load lands.
    /// </summary>
    private void FillPanels(IApplication application)
    {
        loadCancellation = new CancellationTokenSource();
        sinceLoadStarted.Restart();
        TurnActivityMarker(application);
        panelWork.Track(FillPanelsAsync(application, loadCancellation.Token));
    }

    /// <summary>
    /// Repaints while discovery is out at `dotnet test`, so the marker beside
    /// "Discovering tests..." turns instead of the panel sitting blank. The
    /// draw is asked for explicitly because nothing else wakes the loop while
    /// the reader is waiting.
    /// </summary>
    private void TurnActivityMarker(IApplication application) =>
        application.AddTimeout(ActivityMarker.FrameDuration, () =>
        {
            if (session.State.Status != ExplorerStatus.Loading)
            {
                sinceLoadStarted.Stop();
                return false;
            }

            RenderTests();
            application.LayoutAndDraw(true);
            return true;
        });

    private Task FillPanelsAsync(
        IApplication application,
        CancellationToken cancellationToken) =>
        new PanelStartup(
            fileSession,
            folderSession,
            changesetSession,
            session,
            target,
            issues: issueSession).LoadPendingAsync(
            () =>
            {
                application.Invoke(() => Render());
                return Task.CompletedTask;
            },
            cancellationToken);

    private static string? TerminalDriver() => TerminalDriverChoice.FromEnvironment();

    private static Label TestStatus() => new()
    {
        X = ContentInset,
        Y = Pos.AnchorEnd(StatusRow),
        Width = Dim.Fill(ContentInset),
        Height = 1
    };

    private IReadOnlyList<Label> StatusSegmentLabels() => Enumerable
        .Range(0, MaxStatusSegments)
        .Select(StatusSegmentLabel)
        .ToArray();

    private Label StatusSegmentLabel(int index)
    {
        var label = new Label
        {
            X = ContentInset,
            Y = Pos.AnchorEnd(StatusRow),
            Height = 1,
            Visible = false
        };
        label.GettingAttributeForRole += (_, args) =>
        {
            var background = args.Result?.Background ?? Color.Black;
            args.Result = new global::Terminal.Gui.Drawing.Attribute(
                FileRowAppearance.ForegroundFor(ToneFor(index), Color.White),
                background);
            args.Handled = true;
        };
        return label;
    }

    private FileRowTone ToneFor(int index) => index < statusSegments.Count
        ? statusSegments[index].Tone
        : FileRowTone.Neutral;

    private static TextField Search() => new()
    {
        Title = "Search",
        X = ContentInset,
        Y = Pos.AnchorEnd(SearchRow),
        Width = Dim.Percent(40),
        Height = 1,
        TabStop = TabBehavior.NoStop
    };

    /// <summary>The focused panel's filters sit beside the search, the one in
    /// use picked out, so what the panel is hiding is always in view.</summary>
    private IReadOnlyList<Label> FilterLabels() => [.. Enumerable
        .Range(0, MaxFilterChips)
        .Select(FilterLabel)];

    private Label FilterLabel(int index)
    {
        var label = new Label
        {
            Y = Pos.AnchorEnd(SearchRow),
            Height = 1,
            Visible = false
        };
        label.GettingAttributeForRole += (_, args) =>
        {
            args.Result = new global::Terminal.Gui.Drawing.Attribute(
                FilterAppearance.ForegroundFor(index < filterChips.Count && filterChips[index].IsActive),
                args.Result?.Background ?? Color.Black);
            args.Handled = true;
        };
        return label;
    }

    private void ShowFilters(IReadOnlyList<FilterChip> chips)
    {
        filterChips = chips;
        var columns = StatusSegmentLayout.ColumnsFor([.. chips.Select(chip => chip.Text)], 0, FilterGap);
        foreach (var (label, index) in filterLabels.Select((label, index) => (label, index)))
        {
            label.Visible = index < chips.Count;
            if (!label.Visible)
            {
                continue;
            }

            label.X = Pos.Right(search) + FilterGap + columns[index];
            label.Width = chips[index].Text.Length;
            label.Text = chips[index].Text;
        }
    }

    /// <summary>Every panel is on the screen at once, laid out again whenever
    /// the room they share changes size.</summary>
    private View Workspace()
    {
        var shown = new View
        {
            X = ContentInset,
            Y = ContentInset,
            Width = Dim.Fill(ContentInset),
            Height = Dim.Fill(SearchRow),
            CanFocus = true
        };
        foreach (var panel in Enum.GetValues<PanelKind>().Where(panel => panel != PanelKind.Preview))
        {
            lists[panel] = new ListPanel();
            shown.Add(lists[panel].View, lists[panel].Footer);
        }

        preview = new PreviewPanel();
        shown.Add(preview.View);
        shown.ViewportChanged += (_, _) => ArrangePanels();
        return shown;
    }

    private void ArrangePanels()
    {
        var layout = PanelLayout.For(
            workspace.Viewport.Width,
            workspace.Viewport.Height,
            shell.State.ExpandedList);
        foreach (var (panel, shown) in lists)
        {
            shown.Place(layout[panel]);
        }

        preview.Place(layout.Preview);
    }

    /// <summary>The shortcuts keep their rows whether or not they fill them, so
    /// a longer line wraps instead of running off the edge and the rows above do
    /// not shift as the selection changes.</summary>
    private static Label Shortcuts()
    {
        var shortcuts = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(ShortcutLines.Rows),
            Width = Dim.Fill(1),
            Height = ShortcutLines.Rows
        };
        shortcuts.TextFormatter.MultiLine = true;
        shortcuts.TextFormatter.WordWrap = false;
        return shortcuts;
    }

    private void HandleKey(
        IApplication application,
        Key key)
    {
        if (!StartupInput.Accepts(sincePanelsAppeared.Elapsed, SettleDuration))
        {
            key.Handled = true;
            return;
        }

        if (openDialogs > 0)
        {
            return;
        }

        var shellAction = ShellKeyBindings.ActionFor(
            key,
            search.HasFocus,
            ActiveSearchQuery().Length > 0);
        if (shellAction is not null)
        {
            HandleShellAction(application, shellAction, key);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Preview)
        {
            HandlePreviewPanelKey(application, key);
            return;
        }

        if (ActiveFileSession() is { } files)
        {
            HandleFileKey(application, key, files);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Changes)
        {
            HandleChangesetKey(application, key);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Issues)
        {
            HandleIssueKey(application, key);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Comments)
        {
            HandleCommentKey(application, key);
            return;
        }

        HandleTestKey(application, key);
    }

    /// <summary>An arrow a panel had no use for, such as ↓ on its
    /// last row, would carry the focus into the panel beside it without the
    /// shell knowing. Panels are left only by number or Tab.</summary>
    private void HoldTheFocus(Key key)
    {
        if (key.Handled || openDialogs > 0 || search.HasFocus)
        {
            return;
        }

        key.Handled = key.NoShift.KeyCode is
            KeyCode.CursorUp or KeyCode.CursorDown or KeyCode.CursorLeft or KeyCode.CursorRight;
    }

    private void HandleShellAction(
        IApplication application,
        ShellAction action,
        Key key)
    {
        if (action is ShellAction.TypeIntoSearch)
        {
            return;
        }

        key.Handled = true;
        switch (action)
        {
            case ShellAction.ClearSearch:
                search.Text = "";
                panelWork.Track(ClearSearchAsync(application));
                ActiveList.SetFocus();
                return;
            case ShellAction.LeaveSearch:
                ActiveList.SetFocus();
                Render();
                return;
            case ShellAction.FocusSearch when shell.State.ActivePanel == PanelKind.Preview:
                return;
            case ShellAction.FocusSearch:
                search.SetFocus();
                Render();
                return;
            case ShellAction.SelectPanel selected:
                OpenPanel(application, selected.Panel);
                return;
            case ShellAction.SelectNextPanel:
                OpenPanel(application, SteppedPanel(1));
                return;
            case ShellAction.SelectPreviousPanel:
                OpenPanel(application, SteppedPanel(-1));
                return;
            case ShellAction.ShowCommands:
                ShowCommands(application);
                return;
            case ShellAction.Refresh:
                ReloadWhatIsOnDisk(application);
                Rebuild(application, askedFor: true);
                return;
            case ShellAction.Dismiss:
            case ShellAction.HoldFocus:
                return;
            case ShellAction.Quit:
                QuitUnlessNotesWouldBeLost(application);
                return;
        }
    }

    private const int KeepChoice = 1;

    /// <summary>Comments live only as long as the app, so quitting on notes
    /// that have not been copied or saved throws them away. Keeping them is
    /// offered last, because the box opens on its last button.</summary>
    private void QuitUnlessNotesWouldBeLost(IApplication application)
    {
        if (!commentSession.State.Unsaved)
        {
            application.RequestStop();
            return;
        }

        var count = commentSession.State.Comments.Count;
        var chosen = OverThePanels(() => MessageBox.Query(
            application,
            "Quit",
            $"{count} comments have not been copied or saved. Quitting loses them.",
            "Quit anyway",
            "Keep them"));
        if (chosen != KeepChoice)
        {
            application.RequestStop();
        }
    }

    /// <summary>The tests and the issues describe the last build, so opening
    /// either after the tree has been edited sets a rebuild off rather than
    /// showing the reader what the project used to be.</summary>
    private void OpenPanel(IApplication application, PanelKind panel)
    {
        var from = shell.State.ActivePanel;
        shell.Select(panel);
        ShowActivePanel();
        if (editsSinceTheBuild.WorthRebuildingOnOpening(from, shell.State.ActivePanel))
        {
            Rebuild(application, askedFor: false);
        }
    }

    private PanelKind SteppedPanel(int step) => shell.State.Stepped(step);

    /// <summary>Moving to a list on the left stretches it, so the panels are
    /// laid out again before the new one takes the keys.</summary>
    private void ShowActivePanel()
    {
        ArrangePanels();
        Render();
        if (shell.State.ActivePanel == PanelKind.Preview)
        {
            preview.View.SetFocus();
            return;
        }

        ActiveList.SetFocus();
    }

    private void HandleTestKey(
        IApplication application,
        Key key)
    {
        var action = TestPanelKeyBindings.ActionFor(
            key,
            session.State.SearchQuery,
            ActiveList.HasFocus);
        if (action is null)
        {
            return;
        }

        key.Handled = true;
        HandleTestAction(application, action);
    }

    private void HandleTestAction(
        IApplication application,
        TestPanelAction action)
    {
        switch (action)
        {
            case TestPanelAction.OpenSource:
                RequestTestSource(application);
                return;
            case TestPanelAction.ShowOutput:
                ShowTestOutput(application);
                return;
            case TestPanelAction.CancelRun:
                runCancellation?.Cancel();
                return;
            case TestPanelAction.Dispatch dispatch:
                panelWork.Track(DispatchAsync(application, dispatch.Command));
                return;
        }
    }

    /// <summary>The Explorer browses the projects' files or every file beneath
    /// the launch folder, so everything below here treats it as one panel over
    /// two sessions.</summary>
    private FileExplorerSession? ActiveFileSession() =>
        shell.State.ActivePanel == PanelKind.Explorer ? ExplorerSession() : null;

    private FileExplorerSession ExplorerSession() => shell.State.ShowsAllFiles ? folderSession : fileSession;

    private string ActiveSearchQuery() => ActiveFileSession() is { } files
        ? files.State.SearchQuery
        : shell.State.ActivePanel switch
        {
            PanelKind.Changes => changesetSession.State.SearchQuery,
            PanelKind.Issues => issueSession.State.SearchQuery,
            PanelKind.Comments => commentSession.State.SearchQuery,
            _ => session.State.SearchQuery
        };

    private Task SearchAsync(string query) => ActiveFileSession() is { } files
        ? files.DispatchAsync(new FileExplorerCommand.Search(query))
        : shell.State.ActivePanel switch
        {
            PanelKind.Changes => changesetSession.DispatchAsync(new ChangesetCommand.Search(query)),
            PanelKind.Issues => issueSession.DispatchAsync(new IssueCommand.Search(query)),
            PanelKind.Comments => commentSession.DispatchAsync(new CommentCommand.Search(query)),
            _ => session.DispatchAsync(new ExplorerCommand.Search(query))
        };

    private async Task ClearSearchAsync(IApplication application)
    {
        if (shell.State.ActivePanel == PanelKind.Tests)
        {
            await DispatchAsync(application, new ExplorerCommand.ClearSearch());
            return;
        }

        await ClearPanelSearchAsync();
        Render();
    }

    private Task ClearPanelSearchAsync() => ActiveFileSession() is { } files
        ? files.DispatchAsync(new FileExplorerCommand.ClearSearch())
        : shell.State.ActivePanel switch
        {
            PanelKind.Comments => commentSession.DispatchAsync(new CommentCommand.ClearSearch()),
            PanelKind.Issues => issueSession.DispatchAsync(new IssueCommand.ClearSearch()),
            _ => changesetSession.DispatchAsync(new ChangesetCommand.ClearSearch())
        };


    private void HandleFileKey(
        IApplication application,
        Key key,
        FileExplorerSession fileExplorer)
    {
        if (!ActiveList.HasFocus)
        {
            return;
        }

        var action = FilePanelKeyBindings.ActionFor(
            key,
            SelectedFile(fileExplorer),
            search.HasFocus);
        if (action is FilePanelAction.ToggleFilter toggle)
        {
            key.Handled = true;
            panelWork.Track(DispatchFileAsync(
                fileExplorer,
                new FileExplorerCommand.ToggleFilter(toggle.Filter)));
            return;
        }

        if (action is FilePanelAction.ToggleAllFiles)
        {
            key.Handled = true;
            shell.ToggleAllFiles();
            Render();
            return;
        }

        if (action is FilePanelAction.OpenFile open)
        {
            key.Handled = true;
            RequestOpen(application, open.Path, line: 1);
            return;
        }

        var command = FileCommandFor(key, fileExplorer.State.SearchQuery);
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchFileAsync(fileExplorer, command));
    }

    private static VisibleFileNode? SelectedFile(FileExplorerSession fileExplorer) =>
        fileExplorer.State.VisibleNodes.Count == 0
            ? null
            : fileExplorer.State.VisibleNodes[fileExplorer.State.SelectedIndex];

    private static FileExplorerCommand? FileCommandFor(Key key, string searchQuery)
    {
        if (Is(key, KeyCode.CursorUp) || Is(key, KeyCode.K))
        {
            return new FileExplorerCommand.MoveUp();
        }

        if (Is(key, KeyCode.CursorDown) || Is(key, KeyCode.J))
        {
            return new FileExplorerCommand.MoveDown();
        }

        if (Is(key, KeyCode.Z))
        {
            return new FileExplorerCommand.ToggleAllExpanded();
        }

        return Is(key, KeyCode.Space) || Is(key, KeyCode.Enter)
            ? new FileExplorerCommand.ToggleExpanded()
            : null;
    }

    private async Task DispatchFileAsync(
        FileExplorerSession fileExplorer,
        FileExplorerCommand command)
    {
        await fileExplorer.DispatchAsync(command);
        Render();
    }

    private void HandleChangesetKey(
        IApplication application,
        Key key)
    {
        if (!ActiveList.HasFocus || changesetSession.State.Files.Count == 0)
        {
            return;
        }

        var selected = changesetSession.State.Files[changesetSession.State.SelectedIndex];
        var action = ChangesetPanelKeyBindings.ActionFor(key, selected, search.HasFocus);
        if (action is ChangesetAction.ShowDiff)
        {
            key.Handled = true;
            OpenPanel(application, PanelKind.Preview);
            return;
        }

        if (action is ChangesetAction.OpenFile open)
        {
            key.Handled = true;
            RequestOpen(application, open.Path, line: 1);
            return;
        }

        if (action is ChangesetAction.PreviewFile)
        {
            key.Handled = true;
            OpenPanel(application, PanelKind.Preview);
            return;
        }

        if (action is ChangesetAction.RestoreFile)
        {
            key.Handled = true;
            panelWork.Track(RestoreSelectedAsync(application));
            return;
        }

        var command = ChangesetCommandFor(key, changesetSession.State.SearchQuery);
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchChangesetAsync(command));
    }

    private void HandleIssueKey(IApplication application, Key key)
    {
        if (!ActiveList.HasFocus) return;
        var selected = issueSession.State.SelectedIndex < issueSession.State.Issues.Count
            ? issueSession.State.Issues[issueSession.State.SelectedIndex]
            : null;
        var action = IssuePanelKeyBindings.ActionFor(key, selected, search.HasFocus);
        if (action is IssuePanelAction.Edit edit)
        {
            key.Handled = true;
            RequestOpen(application, edit.Path, edit.Line);
            return;
        }
        var command = action switch
        {
            IssuePanelAction.Copy => new IssueCommand.CopySelected(),
            IssuePanelAction.Dispatch dispatch => dispatch.Command,
            _ when Is(key, KeyCode.CursorUp) || Is(key, KeyCode.K) => new IssueCommand.MoveUp(),
            _ when Is(key, KeyCode.CursorDown) || Is(key, KeyCode.J) => new IssueCommand.MoveDown(),
            _ => null
        };
        if (command is null) return;
        key.Handled = true;
        panelWork.Track(DispatchIssueAsync(command));
    }

    private async Task DispatchIssueAsync(IssueCommand command)
    {
        await issueSession.DispatchAsync(command);
        Render();
    }

    private void HandleCommentKey(
        IApplication application,
        Key key)
    {
        if (!ActiveList.HasFocus)
        {
            return;
        }

        var selected = SelectedComment();
        var action = CommentPanelKeyBindings.ActionFor(key, selected, search.HasFocus);
        if (action is not null && selected is not null)
        {
            key.Handled = true;
            HandleCommentAction(application, action, selected);
            return;
        }

        var command = CommentCommandFor(key);
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchCommentAsync(command));
    }

    private FileComment? SelectedComment() =>
        commentSession.State.SelectedIndex < commentSession.State.Comments.Count
            ? commentSession.State.Comments[commentSession.State.SelectedIndex]
            : null;

    private void HandleCommentAction(
        IApplication application,
        CommentAction action,
        FileComment selected)
    {
        if (action is CommentAction.ReadComment)
        {
            ShowComment(application, selected);
            return;
        }

        if (action is CommentAction.RewriteComment)
        {
            RewriteComment(application, selected);
            return;
        }

        if (action is CommentAction.SaveComments)
        {
            SaveComments(application);
            return;
        }

        if (action is CommentAction.ClearComments)
        {
            ClearComments(application);
            return;
        }

        if (action is CommentAction.CopyComments)
        {
            panelWork.Track(DispatchCommentAsync(new CommentCommand.CopyAll()));
            return;
        }

        panelWork.Track(DispatchCommentAsync(
            new CommentCommand.DeleteSelected()));
    }

    /// <summary>Clearing cannot be undone, so it is asked for twice. Cancel is
    /// offered last because the box opens on its last button, and a reader who
    /// presses Enter without reading should keep their notes.</summary>
    private void ClearComments(IApplication application)
    {
        var count = commentSession.State.Comments.Count;
        var chosen = OverThePanels(() => MessageBox.Query(
            application,
            "Clear comments",
            $"Clear all {count} comments? This cannot be undone.",
            "Clear",
            "Cancel"));
        if (chosen != ClearChoice)
        {
            return;
        }

        panelWork.Track(DispatchCommentAsync(new CommentCommand.ClearAll()));
    }

    private void SaveComments(IApplication application)
    {
        var path = OverThePanels(
            () => SavePrompt.Ask(application, "Save comments", SuggestedCommentPath()));
        if (path is null || !MayWriteOver(application, path))
        {
            return;
        }

        panelWork.Track(DispatchCommentAsync(new CommentCommand.SaveAll(path)));
    }

    /// <summary>Saving replaces what the file held, so a path that already has
    /// something in it is asked about. Keeping it is offered last, because the
    /// box opens on its last button.</summary>
    private bool MayWriteOver(IApplication application, string path)
    {
        if (!commentSession.HoldsSomethingAtAsync(path).GetAwaiter().GetResult())
        {
            return true;
        }

        var chosen = OverThePanels(() => MessageBox.Query(
            application,
            "Save comments",
            $"{Path.GetFileName(path)} already exists. Saving replaces what is in it.",
            "Replace it",
            "Keep it"));
        return chosen != KeepChoice;
    }

    private string SuggestedCommentPath() => Path.Combine(LaunchFolder(), "comments.md");

    private string LaunchFolder() => Path.GetDirectoryName(Path.GetFullPath(target))!;

    private void ShowComment(IApplication application, FileComment selected) => ShowCellDialog(
        application,
        $"Comment — {selected.DisplayPath} — ↑/↓ scroll  Esc close",
        CommentCells(selected.Text),
        wordWrap: true);

    private static List<List<Cell>> CommentCells(string text) => text
        .Split('\n')
        .Select(line => Cell.ToCellList(
            line.TrimEnd('\r'),
            new global::Terminal.Gui.Drawing.Attribute(Color.White, Color.Black)))
        .ToList();

    private void RewriteComment(
        IApplication application,
        FileComment selected)
    {
        var written = OverThePanels(
            () => CommentDialog.Ask(application, selected.DisplayPath, selected.Text));
        if (written is null)
        {
            return;
        }

        panelWork.Track(DispatchCommentAsync(
            new CommentCommand.RewriteSelected(written)));
    }

    private static CommentCommand? CommentCommandFor(Key key)
    {
        if (Is(key, KeyCode.CursorUp) || Is(key, KeyCode.K))
        {
            return new CommentCommand.MoveUp();
        }

        return Is(key, KeyCode.CursorDown) || Is(key, KeyCode.J)
            ? new CommentCommand.MoveDown()
            : null;
    }

    private async Task DispatchCommentAsync(
        CommentCommand command)
    {
        await commentSession.DispatchAsync(command);
        Render();
    }

    private static ChangesetCommand? ChangesetCommandFor(Key key, string searchQuery)
    {
        if (Is(key, KeyCode.CursorUp) || Is(key, KeyCode.K))
        {
            return new ChangesetCommand.MoveUp();
        }

        return Is(key, KeyCode.CursorDown) || Is(key, KeyCode.J)
            ? new ChangesetCommand.MoveDown()
            : null;
    }

    private async Task DispatchChangesetAsync(
        ChangesetCommand command)
    {
        await changesetSession.DispatchAsync(command);
        Render();
    }

    private async Task RestoreSelectedAsync(
        IApplication application)
    {
        await changesetSession.DispatchAsync(new ChangesetCommand.RestoreSelected());
        application.Invoke(() => Render());
    }

    private async Task DispatchAsync(
        IApplication application,
        ExplorerCommand command)
    {
        if (!RunsTests(command))
        {
            await session.DispatchAsync(command);
            Render();
            return;
        }

        if (runCancellation is not null)
        {
            return;
        }

        using var cancellation = new CancellationTokenSource();
        runCancellation = cancellation;
        try
        {
            var run = session.DispatchAsync(command, cancellation.Token);
            Render();
            await run;
        }
        finally
        {
            runCancellation = null;
        }

        application.Invoke(() => Render());
    }

    private void RequestTestSource(IApplication application) =>
        panelWork.Track(RequestTestSourceAsync(application));

    private async Task RequestTestSourceAsync(IApplication application)
    {
        await session.DispatchAsync(new ExplorerCommand.LoadSelectedSource());
        if (session.State.SourceLocation is not { } source)
        {
            return;
        }

        application.Invoke(() => RequestOpen(application, source.Path, source.HighlightLine));
    }

    private static string TooLargeToPreview(string path) =>
        $"{Path.GetFileName(path)} is larger than {FileText.MaxBytes / (1024 * 1024)} MB, " +
        "too large to preview. Open it in your editor instead.";

    private void CommentOn(IApplication application, string path)
    {
        var displayPath = DisplayPathFor(path);
        var written = OverThePanels(
            () => CommentDialog.Ask(application, displayPath, commentSession.Against(path)));
        if (written is null)
        {
            return;
        }

        panelWork.Track(commentSession.DispatchAsync(
            new CommentCommand.Add(path, displayPath, written)));
    }

    /// <summary>Comments read against the tree the app was launched in, the
    /// same way the panels name their files.</summary>
    private string DisplayPathFor(string path) => Path.GetRelativePath(LaunchFolder(), path);

    private void ShowTestOutput(IApplication application)
    {
        var snapshot = TestPanelSnapshot.From(session.State, target, sinceLoadStarted.Elapsed);
        ShowCellDialog(
            application,
            $"{snapshot.SelectedOutputTitle} — ↑/↓ scroll  Esc close",
            AnsiTestOutput.ToCells(snapshot.SelectedOutput),
            wordWrap: true);
    }

    private void ShowCommands(IApplication application) => ShowCellDialog(
        application,
        "Commands — ↑/↓ scroll  Esc close",
        CommandMenu.Rows().Select(CommandMenuCells).ToList(),
        wordWrap: false);

    private static List<Cell> CommandMenuCells(CommandMenuRow row) => Cell.ToCellList(
        row.Text,
        new global::Terminal.Gui.Drawing.Attribute(
            row.IsHeading ? Color.BrightCyan : Color.White,
            Color.Black)).ToList();

    private void ShowCellDialog(
        IApplication application,
        string title,
        List<List<Cell>> lines,
        bool wordWrap)
    {
        using var dialog = FullScreenDialog(title);
        var text = new ColoredTextView(wordWrap)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        text.Load(lines);
        SetBlackBackground(dialog);
        SetBlackBackground(text);
        dialog.Add(text);
        OverThePanels(() => application.Run(dialog));
    }

    private static Window FullScreenDialog(string title) => new()
    {
        Title = title,
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        ShadowStyle = ShadowStyles.None
    };

    /// <summary>
    /// Runs a dialog over the panels. The shell listens for keys across the
    /// whole application, so the panels are told to stand down for as long as
    /// something is open in front of them; otherwise a q typed into a comment
    /// would quit rather than be written. Dialogs open over one another — a
    /// comment is written over the preview it was prompted by — so what is
    /// open is counted rather than flagged.
    /// </summary>
    private T OverThePanels<T>(Func<T> show)
    {
        openDialogs++;
        try
        {
            return show();
        }
        finally
        {
            openDialogs--;
        }
    }

    private void OverThePanels(Action show) => OverThePanels<object?>(() =>
    {
        show();
        return null;
    });

    private static void SetBlackBackground(View view)
    {
        view.GettingAttributeForRole += (_, args) =>
        {
            var foreground = args.Result?.Foreground ?? Color.White;
            args.Result = new global::Terminal.Gui.Drawing.Attribute(foreground, Color.Black);
            args.Handled = true;
        };
    }

    private static string LanguageFrom(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".cs" => "csharp",
        ".fs" => "fsharp",
        ".vb" => "vb",
        ".json" => "json",
        ".xml" or ".csproj" or ".fsproj" or ".vbproj" => "xml",
        ".md" => "markdown",
        ".yml" or ".yaml" => "yaml",
        ".sh" => "shellscript",
        _ => "plaintext"
    };

    /// <summary>The editor is handed the bare shell and nothing else, because
    /// reloading here would hold the terminal down for as long as the panels
    /// take. What the edit changed is refreshed once the terminal is back.
    /// </summary>
    private void OpenRequestedFile()
    {
        if (editorLauncher is null || openPath is null)
        {
            return;
        }

        EditorWorkflow().OpenAsync(openPath, openLine).GetAwaiter().GetResult();
        panelsWereEdited = true;
    }

    private ExplorerEditorWorkflow EditorWorkflow() => new(
        [fileSession, folderSession],
        changesetSession,
        editorLauncher!,
        target,
        issues: issueSession);

    private PanelReload PanelReload() => new(
        [fileSession, folderSession],
        changesetSession,
        target,
        issues: issueSession);

    /// <summary>
    /// Asks on every frame rather than reloading as the edits land, because a
    /// single save arrives as several edits and an agent's change arrives as
    /// dozens. The burst answers once the tree has been quiet.
    /// </summary>
    private void ReloadWhenTheWorkingTreeSettles(
        IApplication application)
    {
        if (workspaceWatcher is null)
        {
            return;
        }

        application.AddTimeout(EditPollInterval, () =>
        {
            if (!reloadingWhatIsOnDisk && outsideEdits.SettledAt(sinceWatching.Elapsed))
            {
                ReloadWhatIsOnDisk(application);
            }

            return true;
        });
    }

    /// <summary>
    /// The issues and the tests are the panels an outside edit does not
    /// reload, because both have to build before they can answer. The reader
    /// stays on the panel they were on, and the rebuild is told in a toast over
    /// it. A rebuild set off by opening a panel keeps quiet when it cannot
    /// start: only a reader who pressed for one is owed the reason.
    /// </summary>
    private void Rebuild(IApplication application, bool askedFor)
    {
        var rebuild = new ProjectRebuild(issueSession, session, target);
        var started = rebuild.Start();
        if (started == RebuildStart.WaitingOnTheRun && askedFor)
        {
            ShowToast(application, RebuildToast.WaitingOnTheRun());
            return;
        }

        if (started != RebuildStart.Started)
        {
            return;
        }

        editsSinceTheBuild.Built();
        Render();
        sinceLoadStarted.Restart();
        TurnActivityMarker(application);
        sinceRebuildStarted.Restart();
        ShowToast(application, RebuildToast.Rebuilding(TimeSpan.Zero));
        TurnRebuildToast(application);
        panelWork.Track(RebuildAsync(application, rebuild, loadCancellation!.Token));
    }

    private async Task RebuildAsync(
        IApplication application,
        ProjectRebuild rebuild,
        CancellationToken cancellationToken)
    {
        await rebuild.RunAsync(
            () =>
            {
                application.Invoke(() => Render());
                return Task.CompletedTask;
            },
            cancellationToken);
        application.Invoke(() => ShowToast(
            application,
            RebuildToast.Finished(issueSession.State, session.State)));
    }

    /// <summary>Turns the marker in the toast for as long as the rebuild is out,
    /// asking for the draw because nothing else wakes the loop meanwhile.
    /// </summary>
    private void TurnRebuildToast(IApplication application) =>
        application.AddTimeout(ActivityMarker.FrameDuration, () =>
        {
            if (shownToast is not { Tone: ToastTone.Working })
            {
                return false;
            }

            ShowToastText(RebuildToast.Rebuilding(sinceRebuildStarted.Elapsed));
            application.LayoutAndDraw(true);
            return true;
        });

    private View Toast()
    {
        toastText = new Label { X = 1, Y = 0 };
        toastText.GettingAttributeForRole += (_, args) =>
        {
            args.Result = new global::Terminal.Gui.Drawing.Attribute(ToastColor(), Color.Black);
            args.Handled = true;
        };
        var shown = new View
        {
            Y = 1,
            Height = 3,
            BorderStyle = LineStyle.Rounded,
            CanFocus = false,
            TabStop = TabBehavior.NoStop,
            Visible = false
        };
        SetBlackBackground(shown);
        shown.Add(toastText);
        return shown;
    }

    private Color ToastColor() => shownToast?.Tone switch
    {
        ToastTone.Succeeded => Color.BrightGreen,
        ToastTone.Failed => Color.BrightRed,
        ToastTone.Waiting => Color.BrightYellow,
        _ => Color.White
    };

    /// <summary>Each toast replaces the one before it, so a fade scheduled for
    /// an older toast must not take down the one showing now.</summary>
    private void ShowToast(IApplication application, Toast shown)
    {
        var showing = ++toastsShown;
        ShowToastText(shown);
        toast!.Visible = true;
        application.LayoutAndDraw(true);
        if (!shown.FadesAway)
        {
            return;
        }

        application.AddTimeout(RebuildToast.FadeAfter, () =>
        {
            if (showing == toastsShown)
            {
                toast.Visible = false;
                shownToast = null;
                application.LayoutAndDraw(true);
            }

            return false;
        });
    }

    private void ShowToastText(Toast shown)
    {
        shownToast = shown;
        var width = shown.Text.Length + ToastPadding;
        toastText!.Text = shown.Text;
        toastText.Width = shown.Text.Length;
        toast!.Width = width;
        toast.X = Pos.AnchorEnd(width + ContentInset);
    }

    /// <summary>The issues are left out: an agent saves often enough that a
    /// build per burst would never finish one before the next began.</summary>
    private void ReloadWhatIsOnDisk(IApplication application)
    {
        if (reloadingWhatIsOnDisk)
        {
            return;
        }

        reloadingWhatIsOnDisk = true;
        panelWork.Track(ReloadWhatIsOnDiskAsync(application, loadCancellation!.Token));
    }

    private async Task ReloadWhatIsOnDiskAsync(
        IApplication application,
        CancellationToken cancellationToken)
    {
        try
        {
            await PanelReload().FromDiskAsync(
                () =>
                {
                    application.Invoke(() => Render());
                    return Task.CompletedTask;
                },
                cancellationToken);
        }
        finally
        {
            reloadingWhatIsOnDisk = false;
        }
    }

    private void RefreshEditedPanels(IApplication application)
    {
        if (!panelsWereEdited)
        {
            return;
        }

        panelsWereEdited = false;
        outsideEdits.Forget();
        editsSinceTheBuild.Noticed();
        panelWork.Track(RefreshEditedPanelsAsync(application, loadCancellation!.Token));
    }

    private Task RefreshEditedPanelsAsync(
        IApplication application,
        CancellationToken cancellationToken) =>
        EditorWorkflow().RefreshAsync(
            () =>
            {
                application.Invoke(() => Render());
                return Task.CompletedTask;
            },
            cancellationToken);

    private void Render()
    {
        var fileExplorer = ActiveFileSession();
        shortcutSegments = PanelShortcuts.For(
            shell.State.ActivePanel,
            (fileExplorer ?? fileSession).State,
            changesetSession.State,
            session.State,
            commentSession.State,
            search.HasFocus,
            issueSession.State);
        ShowShortcuts();
        RenderExplorer();
        RenderTests();
        RenderChanges();
        RenderIssues();
        RenderComments();
        if (shell.State.ActivePanel == PanelKind.Preview)
        {
            ShowFilters([]);
        }

        FollowTheSelection();
    }

    private void FollowTheSelection()
    {
        var subject = PreviewSubject.For(shell.State.PreviewedList, PanelStatesNow());
        if (subject == previewed)
        {
            return;
        }

        previewed = subject;
        previewedSource = null;
        switch (subject)
        {
            case PreviewSubject.SourceFile file:
                PreviewSource(new SourceLocation(file.Path, file.Line));
                return;
            case PreviewSubject.ChangeDiff:
                panelWork.Track(PreviewDiffAsync(subject));
                return;
            case PreviewSubject.SelectedTest:
                panelWork.Track(PreviewTestAsync(subject));
                return;
            default:
                preview.ShowNothing(PreviewPanelTitle("Preview", ""));
                return;
        }
    }

    /// <summary>The preview reads its own keys: it scrolls what it shows,
    /// hands it to the editor or a comment, and steps the list it follows.
    /// </summary>
    private void HandlePreviewPanelKey(IApplication application, Key key)
    {
        var action = PreviewKeyBindings.ActionFor(key, preview.PageHeight);
        if (action is null || previewedSource is not { } source)
        {
            return;
        }

        key.Handled = true;
        switch (action)
        {
            case PreviewAction.Edit:
                RequestOpen(application, source.Path, source.HighlightLine);
                return;
            case PreviewAction.Comment:
                CommentOn(application, source.Path);
                return;
            case PreviewAction.StepFile step:
                panelWork.Track(StepPreviewedListAsync(step.Step));
                return;
            case PreviewAction.Scroll scroll:
                preview.Scroll(scroll.Rows);
                return;
            case PreviewAction.ScrollToStart:
                preview.ScrollToStart();
                return;
            case PreviewAction.ScrollToEnd:
                preview.ScrollToEnd();
                return;
        }
    }

    private async Task StepPreviewedListAsync(int step)
    {
        var down = step > 0;
        await (shell.State.PreviewedList switch
        {
            PanelKind.Explorer => ExplorerSession().DispatchAsync(
                down ? new FileExplorerCommand.MoveDown() : new FileExplorerCommand.MoveUp()),
            PanelKind.Tests => session.DispatchAsync(
                down ? new ExplorerCommand.MoveDown() : new ExplorerCommand.MoveUp()),
            PanelKind.Changes => changesetSession.DispatchAsync(
                down ? new ChangesetCommand.MoveDown() : new ChangesetCommand.MoveUp()),
            PanelKind.Issues => issueSession.DispatchAsync(
                down ? new IssueCommand.MoveDown() : new IssueCommand.MoveUp()),
            _ => commentSession.DispatchAsync(
                down ? new CommentCommand.MoveDown() : new CommentCommand.MoveUp())
        });
        running?.Invoke(Render);
    }

    private PanelStates PanelStatesNow() => new(
        ExplorerSession().State,
        session.State,
        changesetSession.State,
        issueSession.State,
        commentSession.State);

    /// <summary>A file that cannot be read says so in the preview rather than
    /// in a box over the panels, because the preview follows every move.</summary>
    private void PreviewSource(SourceLocation source)
    {
        var title = PreviewPanelTitle("Preview", $"{DisplayPathFor(source.Path)}:{source.HighlightLine}");
        previewedSource = source;
        try
        {
            preview.ShowSource(
                title,
                FileText.ReadWithin(source.Path) ?? TooLargeToPreview(source.Path),
                LanguageFrom(source.Path),
                source.HighlightLine,
                shell.State.PreviewedList is PanelKind.Issues or PanelKind.Tests,
                PreviewedDetails(),
                PreviewedDetailTone());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            preview.ShowNothing(title);
        }
    }

    private string PreviewedDetails() => shell.State.PreviewedList == PanelKind.Issues
        ? IssuePanelSnapshot.From(issueSession.State).SelectedDetails
        : "";

    private FileRowTone PreviewedDetailTone() =>
        shell.State.PreviewedList == PanelKind.Issues &&
        issueSession.State.SelectedIndex < issueSession.State.Issues.Count
            ? IssuePanelSnapshot.ToneFor(issueSession.State.Issues[issueSession.State.SelectedIndex])
            : FileRowTone.Neutral;

    /// <summary>The diff and the test's source both take a moment to fetch, so
    /// the preview shows them only if the reader is still on the row that
    /// asked.</summary>
    private async Task PreviewDiffAsync(PreviewSubject subject)
    {
        await changesetSession.DispatchAsync(new ChangesetCommand.LoadSelectedDiff());
        running?.Invoke(() =>
        {
            if (previewed != subject)
            {
                return;
            }

            var snapshot = ChangesetPanelSnapshot.From(changesetSession.State);
            previewedSource = new SourceLocation(((PreviewSubject.ChangeDiff)subject).Path, 1);
            preview.ShowDiff(PreviewPanelTitle("Diff", snapshot.DiffTitle), snapshot.DiffLines);
        });
    }

    private async Task PreviewTestAsync(PreviewSubject subject)
    {
        await session.DispatchAsync(new ExplorerCommand.LoadSelectedSource());
        running?.Invoke(() =>
        {
            if (previewed != subject)
            {
                return;
            }

            if (session.State.SourceLocation is { } source)
            {
                PreviewSource(source);
                return;
            }

            preview.ShowNothing(PreviewPanelTitle("Preview", "source not found"));
        });
    }

    private static string PreviewPanelTitle(string name, string subject) =>
        $"[{PanelKeys.For(PanelKind.Preview)}]─{name}" + (subject.Length == 0 ? "" : $" ─ {subject}");

    /// <summary>Wraps to the width the label has now, which is why it is also
    /// called as the width changes rather than only as the shortcuts change.</summary>
    private void ShowShortcuts() =>
        shortcuts!.Text = string.Join(
            '\n',
            ShortcutLines.For(shortcutSegments, shortcuts.Viewport.Width));

    private void RenderExplorer()
    {
        var snapshot = FilePanelSnapshot.From(ExplorerSession().State, shell.State.ShowsAllFiles);
        RenderPanel(
            PanelKind.Explorer,
            snapshot.Filters,
            snapshot.SearchQuery,
            snapshot.SearchHitCount,
            snapshot.Nodes,
            () => [.. snapshot.Rows.Select(row => ListRow.Toned(row.Text, row.Tone))],
            snapshot.SelectedIndex,
            snapshot.EmptyMessage,
            new PanelPosition(snapshot.SelectedIndex, snapshot.Nodes.Count));
        ShowSegmentsWhenActive(PanelKind.Explorer, snapshot.StatusSegments);
    }

    private void RenderTests()
    {
        var snapshot = TestPanelSnapshot.From(session.State, target, sinceLoadStarted.Elapsed);
        testNodes = snapshot.Tests;
        RenderPanel(
            PanelKind.Tests,
            snapshot.Filters,
            snapshot.SearchQuery,
            snapshot.SearchHitCount,
            snapshot.Tests,
            () => [.. snapshot.TestRows.Zip(snapshot.Tests, TestRow)],
            snapshot.SelectedIndex,
            snapshot.EmptyMessage,
            new PanelPosition(snapshot.SelectedIndex, snapshot.Tests.Count));
        if (shell.State.ActivePanel != PanelKind.Tests)
        {
            testStatus!.Visible = false;
            return;
        }

        HideSegments();
        testStatus!.Visible = true;
        testStatus.Text = snapshot.StatusLine;
    }

    private static ListRow TestRow(string text, VisibleTestNode node) =>
        new(text, TestRowAppearance.ForegroundFor(node.Outcome, node.Update));

    private void RenderChanges()
    {
        var snapshot = ChangesetPanelSnapshot.From(changesetSession.State);
        RenderPanel(
            PanelKind.Changes,
            [],
            snapshot.SearchQuery,
            snapshot.SearchHitCount,
            snapshot.Files,
            () => [.. snapshot.Rows.Select(row => ListRow.Toned(row.Text, row.Tone))],
            snapshot.SelectedIndex,
            snapshot.EmptyMessage,
            new PanelPosition(snapshot.SelectedIndex, snapshot.Files.Count));
        ShowSegmentsWhenActive(PanelKind.Changes, snapshot.StatusSegments);
    }

    private void RenderIssues()
    {
        var snapshot = IssuePanelSnapshot.From(issueSession.State);
        var layout = IssuePanelLayout.From(snapshot, lists[PanelKind.Issues].View.Viewport.Width);
        RenderPanel(
            PanelKind.Issues,
            snapshot.Filters,
            snapshot.SearchQuery,
            snapshot.Issues.Count,
            layout,
            () => [.. layout.Rows.Select(row => ListRow.Toned(row.Text, row.Tone))],
            layout.SelectedRowIndex,
            snapshot.EmptyMessage,
            new PanelPosition(snapshot.SelectedIndex, snapshot.Issues.Count));
        ShowSegmentsWhenActive(PanelKind.Issues, snapshot.StatusSegments);
    }

    private void RenderComments()
    {
        var snapshot = CommentPanelSnapshot.From(commentSession.State);
        RenderPanel(
            PanelKind.Comments,
            [],
            snapshot.SearchQuery,
            snapshot.SearchHitCount,
            snapshot.Comments,
            () => [.. snapshot.Rows.Select(row => ListRow.Toned(row.Text, row.Tone))],
            snapshot.SelectedIndex,
            snapshot.EmptyMessage,
            new PanelPosition(snapshot.SelectedIndex, snapshot.Comments.Count));
        ShowSegmentsWhenActive(PanelKind.Comments, snapshot.StatusSegments);
    }

    /// <summary>Every panel lists its rows, but only the one taking the keys
    /// fills the search box beneath them.</summary>
    private void RenderPanel(
        PanelKind panel,
        IReadOnlyList<FilterChip> filters,
        string searchQuery,
        int searchHitCount,
        object content,
        Func<IReadOnlyList<ListRow>> rows,
        int selectedIndex,
        string emptyMessage,
        PanelPosition position)
    {
        var active = shell.State.ActivePanel == panel;
        lists[panel].Show(
            PanelTitle.For(panel, filters, searchQuery, active),
            PanelTitle.Footer(position.Selected, position.Count),
            content,
            rows,
            selectedIndex,
            emptyMessage);
        if (!active)
        {
            return;
        }

        search.Title = searchQuery.Length == 0 ? "Search" : $"Search — {searchHitCount} hits";
        search.Text = searchQuery;
        ShowFilters(filters);
    }

    /// <summary>Where the selection stands among what the panel lists. The
    /// issues wrap across several rows each, so they count issues, not rows.
    /// </summary>
    private sealed record PanelPosition(int Selected, int Count);

    private void ShowSegmentsWhenActive(PanelKind panel, IReadOnlyList<FileStatusSegment> segments)
    {
        if (shell.State.ActivePanel == panel)
        {
            ShowSegments(segments);
        }
    }

    private void ShowSegments(IReadOnlyList<FileStatusSegment> segments)
    {
        statusSegments = segments;
        var placed = StatusSegmentLayout.Place(segments, ContentInset, SegmentGap);
        for (var index = 0; index < segmentLabels.Count; index++)
        {
            Show(segmentLabels[index], index < placed.Count ? placed[index] : null);
        }
    }

    private static void Show(Label label, PlacedStatusSegment? segment)
    {
        label.Visible = segment is not null;
        if (segment is null)
        {
            return;
        }

        label.X = segment.Column;
        label.Width = segment.Text.Length;
        label.Text = segment.Text;
    }

    private void HideSegments()
    {
        foreach (var label in segmentLabels)
        {
            label.Visible = false;
        }
    }

    private void RequestOpen(IApplication application, string path, int line)
    {
        if (editorLauncher is null)
        {
            return;
        }

        openPath = path;
        openLine = line;
        openSourceRequested = true;
        application.RequestStop();
    }

    private static bool RunsTests(ExplorerCommand command) => command is
        ExplorerCommand.RunSelected or
        ExplorerCommand.RerunLast or
        ExplorerCommand.RerunFailed;

    private static bool Is(Key key, KeyCode keyCode) =>
        !key.IsShift && key.NoShift.KeyCode == keyCode;
}
