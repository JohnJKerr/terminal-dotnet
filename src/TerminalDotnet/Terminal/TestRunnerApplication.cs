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
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
using TerminalDotnet.Filters;
using TerminalDotnet.Search;
using TextMateSharp.Grammars;

namespace TerminalDotnet.Terminal;

internal sealed class TestRunnerApplication(
    TestExplorerSession session,
    FileExplorerSession fileSession,
    FileExplorerSession folderSession,
    ChangesetSession changesetSession,
    CommentSession commentSession,
    FlagSession flagSession,
    IssueSession issueSession,
    string target,
    IFileOpener? editorLauncher = null,
    IWorkspaceWatcher? workspaceWatcher = null)
{
    private const int ContentInset = 1;
    private const int PanelWidth = 20;
    private const int WorkspaceX = ContentInset + PanelWidth + 1;
    private const int SegmentGap = 2;
    private const int FilterRowHeight = 1;
    private const int FilterGap = 1;
    private const int MaxStatusSegments = 4;
    private const int MaxFilterChips = 4;
    private const int StatusRow = ShortcutLines.Rows + 1;
    private const int RowsBelowTheList = StatusRow + 1;
    private const int IssueDetailRows = 4;
    private const int ClearChoice = 0;
    private static readonly TimeSpan SettleDuration = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan EditPollInterval = TimeSpan.FromMilliseconds(250);

    private CancellationTokenSource? runCancellation;
    private CancellationTokenSource? loadCancellation;
    private readonly Stopwatch sinceLoadStarted = new();
    private readonly Stopwatch sincePanelsAppeared = new();
    private IReadOnlyList<VisibleTestNode> testNodes = [];
    private IReadOnlyList<FileRowTone> rowTones = [];
    private object? listedContent;
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

    /// <summary>The file the preview is showing. Stepping to another of the
    /// panel's rows replaces it without closing the preview.</summary>
    private SourceLocation previewing = new("", 1);
    private Label? testStatus;
    private IReadOnlyList<Label> segmentLabels = [];
    private IReadOnlyList<FileStatusSegment> statusSegments = [];
    private IReadOnlyList<Label> filterLabels = [];
    private IReadOnlyList<FilterChip> filterChips = [];
    private Label? emptyState;
    private Label? shortcuts;
    private IReadOnlyList<string> shortcutSegments = [];

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
        listedContent = null;
        using IApplication application = Application.Create();
        application.Init(TerminalDriver());

        using var window = new Window { Title = $"terminal-dotnet - {VersionNumber.Current}" };
        var panels = Panels();
        var search = Search();
        var tests = Tests(search);
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
        filterLabels = FilterLabels(search);
        emptyState = EmptyState(tests);
        shortcuts = Shortcuts();
        shortcuts.ViewportChanged += (_, _) => ShowShortcuts();

        window.Add(panels, search, tests, emptyState, testStatus, shortcuts);
        window.Add([.. segmentLabels]);
        window.Add([.. filterLabels]);
        toast = Toast();
        window.Add(toast);
        search.ValueChanged += async (_, _) =>
        {
            await SearchAsync(search.Text);
            Render(search, tests);
        };
        application.Keyboard.KeyDown += (_, key) =>
            HandleKey(application, key, panels, search, tests);
        Render(search, tests);
        sincePanelsAppeared.Restart();
        panels.SelectedItem = shell.State.ActiveIndex;
        tests.SetFocus();
        FillPanels(application, search, tests);
        RefreshEditedPanels(application, search, tests);
        ReloadWhenTheWorkingTreeSettles(application, search, tests);

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
    private void FillPanels(IApplication application, TextField search, ListView tests)
    {
        loadCancellation = new CancellationTokenSource();
        sinceLoadStarted.Restart();
        TurnActivityMarker(application, search, tests);
        panelWork.Track(FillPanelsAsync(application, search, tests, loadCancellation.Token));
    }

    /// <summary>
    /// Repaints while discovery is out at `dotnet test`, so the marker beside
    /// "Discovering tests..." turns instead of the panel sitting blank. The
    /// draw is asked for explicitly because nothing else wakes the loop while
    /// the reader is waiting.
    /// </summary>
    private void TurnActivityMarker(IApplication application, TextField search, ListView tests) =>
        application.AddTimeout(ActivityMarker.FrameDuration, () =>
        {
            if (session.State.Status != ExplorerStatus.Loading)
            {
                sinceLoadStarted.Stop();
                return false;
            }

            if (shell.State.ActivePanel != PanelKind.Tests)
            {
                return true;
            }

            ShowEmptyState(TestPanelSnapshot.From(session.State, target, sinceLoadStarted.Elapsed).EmptyMessage);
            application.LayoutAndDraw(true);
            return true;
        });

    private Task FillPanelsAsync(
        IApplication application,
        TextField search,
        ListView tests,
        CancellationToken cancellationToken) =>
        new PanelStartup(
            fileSession,
            folderSession,
            changesetSession,
            session,
            target,
            flagSession,
            issueSession).LoadPendingAsync(
            () =>
            {
                application.Invoke(() => Render(search, tests));
                return Task.CompletedTask;
            },
            cancellationToken);

    private ListView Panels()
    {
        var panels = new ListView
        {
            Title = "Panels",
            X = ContentInset,
            Y = ContentInset,
            Width = PanelWidth,
            Height = Dim.Fill(2),
            ShowMarks = false,
            KeystrokeNavigator = null
        };
        panels.Source = new PanelListSource(shell.State.KeyedPanels);
        panels.SelectedItem = shell.State.ActiveIndex;
        return panels;
    }

    private static string? TerminalDriver() => TerminalDriverChoice.FromEnvironment();

    private static Label TestStatus() => new()
    {
        X = WorkspaceX,
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
            X = WorkspaceX,
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

    private IReadOnlyList<Label> FilterLabels(TextField search) => Enumerable
        .Range(0, MaxFilterChips)
        .Select(index => FilterLabel(search, index))
        .ToArray();

    private Label FilterLabel(TextField search, int index)
    {
        var label = new Label
        {
            X = WorkspaceX,
            Y = Pos.Bottom(search),
            Height = 1,
            Visible = false
        };
        label.GettingAttributeForRole += (_, args) =>
        {
            var background = args.Result?.Background ?? Color.Black;
            args.Result = new global::Terminal.Gui.Drawing.Attribute(
                FilterAppearance.ForegroundFor(IsActiveFilter(index)),
                background);
            args.Handled = true;
        };
        return label;
    }

    private bool IsActiveFilter(int index) => index < filterChips.Count && filterChips[index].IsActive;

    private static TextField Search() => new()
    {
        Title = "Search",
        X = WorkspaceX,
        Y = ContentInset,
        Width = Dim.Fill(ContentInset),
        Height = 1,
        TabStop = TabBehavior.NoStop
    };

    private ListView Tests(TextField search)
    {
        var tests = new ListView
        {
            Title = "Tests",
            X = WorkspaceX,
            Y = Pos.Bottom(search) + FilterRowHeight + FilterGap,
            Width = Dim.Fill(ContentInset),
            Height = Dim.Fill(RowsBelowTheList),
            ShowMarks = false,
            KeystrokeNavigator = null
        };
        tests.RowRender += (_, args) => ColorTreeRow(tests, args);
        return tests;
    }

    private static Label EmptyState(ListView list) => new()
    {
        X = WorkspaceX,
        Y = Pos.Top(list),
        Width = Dim.Fill(ContentInset),
        Height = 1,
        Visible = false
    };

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
        Key key,
        ListView panels,
        TextField search,
        ListView tests)
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
            panels.HasFocus,
            ActiveSearchQuery().Length > 0);
        if (shellAction is not null)
        {
            HandleShellAction(application, shellAction, key, panels, search, tests);
            return;
        }

        if (ActiveFileSession() is { } files)
        {
            HandleFileKey(application, key, files, tests, search);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Changes)
        {
            HandleChangesetKey(application, key, tests, search);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Issues)
        {
            HandleIssueKey(application, key, tests, search);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Comments)
        {
            HandleCommentKey(application, key, tests, search);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Flags)
        {
            HandleFlagKey(application, key, tests, search);
            return;
        }

        HandleTestKey(application, key, search, tests);
    }

    private void HandleShellAction(
        IApplication application,
        ShellAction action,
        Key key,
        ListView panels,
        TextField search,
        ListView tests)
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
                panelWork.Track(ClearSearchAsync(application, search, tests));
                tests.SetFocus();
                return;
            case ShellAction.LeaveSearch:
            case ShellAction.FocusRows:
                tests.SetFocus();
                Render(search, tests);
                return;
            case ShellAction.FocusSearch:
                search.SetFocus();
                Render(search, tests);
                return;
            case ShellAction.FocusPanels:
                panels.SetFocus();
                Render(search, tests);
                return;
            case ShellAction.SelectFocusedPanel:
                OpenPanel(application, panels.SelectedItem ?? 0, panels, search, tests);
                return;
            case ShellAction.SelectPanel selected:
                OpenPanel(application, (int)selected.Panel, panels, search, tests);
                return;
            case ShellAction.ShowCommands:
                ShowCommands(application);
                return;
            case ShellAction.Rebuild:
                Rebuild(application, search, tests, askedFor: true);
                return;
            case ShellAction.Dismiss:
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
    private void OpenPanel(
        IApplication application,
        int index,
        ListView panels,
        TextField search,
        ListView tests)
    {
        var from = shell.State.ActivePanel;
        shell.Select(index);
        ShowActivePanel(panels, search, tests);
        if (editsSinceTheBuild.WorthRebuildingOnOpening(from, shell.State.ActivePanel))
        {
            Rebuild(application, search, tests, askedFor: false);
        }
    }

    private void ShowActivePanel(ListView panels, TextField search, ListView tests)
    {
        panels.SelectedItem = shell.State.ActiveIndex;
        Render(search, tests);
        tests.SetFocus();
    }

    private void HandleTestKey(
        IApplication application,
        Key key,
        TextField search,
        ListView tests)
    {
        var action = TestPanelKeyBindings.ActionFor(
            key,
            session.State.SearchQuery,
            tests.HasFocus);
        if (action is null)
        {
            return;
        }

        key.Handled = true;
        HandleTestAction(application, action, search, tests);
    }

    private void HandleTestAction(
        IApplication application,
        TestPanelAction action,
        TextField search,
        ListView tests)
    {
        switch (action)
        {
            case TestPanelAction.OpenSource:
                RequestTestSource(application, preview: false, search, tests);
                return;
            case TestPanelAction.PreviewSource:
                RequestTestSource(application, preview: true, search, tests);
                return;
            case TestPanelAction.ShowOutput:
                ShowTestOutput(application);
                return;
            case TestPanelAction.CancelRun:
                runCancellation?.Cancel();
                return;
            case TestPanelAction.Dispatch dispatch:
                panelWork.Track(DispatchAsync(application, dispatch.Command, search, tests));
                return;
        }
    }

    /// <summary>The Explorer and the Files browse the same kind of tree, so
    /// everything below here treats them as one panel over two sessions.</summary>
    private FileExplorerSession? ActiveFileSession() => shell.State.ActivePanel switch
    {
        PanelKind.Explorer => fileSession,
        PanelKind.Files => folderSession,
        _ => null
    };

    private string ActiveSearchQuery() => ActiveFileSession() is { } files
        ? files.State.SearchQuery
        : shell.State.ActivePanel switch
        {
            PanelKind.Changes => changesetSession.State.SearchQuery,
            PanelKind.Issues => issueSession.State.SearchQuery,
            PanelKind.Comments => commentSession.State.SearchQuery,
            PanelKind.Flags => flagSession.State.SearchQuery,
            _ => session.State.SearchQuery
        };

    private Task SearchAsync(string query) => ActiveFileSession() is { } files
        ? files.DispatchAsync(new FileExplorerCommand.Search(query))
        : shell.State.ActivePanel switch
        {
            PanelKind.Changes => changesetSession.DispatchAsync(new ChangesetCommand.Search(query)),
            PanelKind.Issues => issueSession.DispatchAsync(new IssueCommand.Search(query)),
            PanelKind.Comments => commentSession.DispatchAsync(new CommentCommand.Search(query)),
            PanelKind.Flags => flagSession.DispatchAsync(new FlagCommand.Search(query)),
            _ => session.DispatchAsync(new ExplorerCommand.Search(query))
        };

    private async Task ClearSearchAsync(IApplication application, TextField search, ListView tests)
    {
        if (shell.State.ActivePanel == PanelKind.Tests)
        {
            await DispatchAsync(application, new ExplorerCommand.ClearSearch(), search, tests);
            return;
        }

        await ClearPanelSearchAsync();
        Render(search, tests);
    }

    private Task ClearPanelSearchAsync() => ActiveFileSession() is { } files
        ? files.DispatchAsync(new FileExplorerCommand.ClearSearch())
        : shell.State.ActivePanel switch
        {
            PanelKind.Comments => commentSession.DispatchAsync(new CommentCommand.ClearSearch()),
            PanelKind.Flags => flagSession.DispatchAsync(new FlagCommand.ClearSearch()),
            PanelKind.Issues => issueSession.DispatchAsync(new IssueCommand.ClearSearch()),
            _ => changesetSession.DispatchAsync(new ChangesetCommand.ClearSearch())
        };


    private void HandleFileKey(
        IApplication application,
        Key key,
        FileExplorerSession fileExplorer,
        ListView files,
        TextField search)
    {
        if (!files.HasFocus)
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
                new FileExplorerCommand.ToggleFilter(toggle.Filter),
                search,
                files));
            return;
        }

        if (action is FilePanelAction.OpenFile open)
        {
            key.Handled = true;
            RequestOpen(application, open.Path, line: 1);
            return;
        }

        if (action is FilePanelAction.PreviewFile preview)
        {
            key.Handled = true;
            ShowPreview(application, preview.Path, 1, search, files);
            return;
        }

        var command = FileCommandFor(key, fileExplorer.State.SearchQuery);
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchFileAsync(fileExplorer, command, search, files));
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
        FileExplorerCommand command,
        TextField search,
        ListView files)
    {
        await fileExplorer.DispatchAsync(command);
        Render(search, files);
    }

    private void HandleChangesetKey(
        IApplication application,
        Key key,
        ListView files,
        TextField search)
    {
        if (!files.HasFocus || changesetSession.State.Files.Count == 0)
        {
            return;
        }

        var selected = changesetSession.State.Files[changesetSession.State.SelectedIndex];
        var action = ChangesetPanelKeyBindings.ActionFor(key, selected, search.HasFocus);
        if (action is ChangesetAction.ShowDiff)
        {
            key.Handled = true;
            ShowDiff(application, search, files);
            return;
        }

        if (action is ChangesetAction.OpenFile open)
        {
            key.Handled = true;
            RequestOpen(application, open.Path, line: 1);
            return;
        }

        if (action is ChangesetAction.PreviewFile preview)
        {
            key.Handled = true;
            ShowPreview(application, preview.Path, 1, search, files);
            return;
        }

        if (action is ChangesetAction.RestoreFile)
        {
            key.Handled = true;
            panelWork.Track(RestoreSelectedAsync(application, search, files));
            return;
        }

        var command = ChangesetCommandFor(key, changesetSession.State.SearchQuery);
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchChangesetAsync(command, search, files));
    }

    private void HandleIssueKey(IApplication application, Key key, ListView rows, TextField search)
    {
        if (!rows.HasFocus) return;
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
        if (action is IssuePanelAction.Preview preview)
        {
            key.Handled = true;
            ShowPreview(application, preview.Path, preview.Line, search, rows);
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
        panelWork.Track(DispatchIssueAsync(command, search, rows));
    }

    private async Task DispatchIssueAsync(IssueCommand command, TextField search, ListView rows)
    {
        await issueSession.DispatchAsync(command);
        Render(search, rows);
    }

    private void HandleCommentKey(
        IApplication application,
        Key key,
        ListView files,
        TextField search)
    {
        if (!files.HasFocus)
        {
            return;
        }

        var selected = SelectedComment();
        var action = CommentPanelKeyBindings.ActionFor(key, selected, search.HasFocus);
        if (action is not null && selected is not null)
        {
            key.Handled = true;
            HandleCommentAction(application, action, selected, search, files);
            return;
        }

        var command = CommentCommandFor(key);
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchCommentAsync(command, search, files));
    }

    private FileComment? SelectedComment() =>
        commentSession.State.SelectedIndex < commentSession.State.Comments.Count
            ? commentSession.State.Comments[commentSession.State.SelectedIndex]
            : null;

    private Flag? SelectedFlag() => flagSession.State.SelectedIndex < flagSession.State.Flags.Count
        ? flagSession.State.Flags[flagSession.State.SelectedIndex]
        : null;

    private void HandleFlagKey(IApplication application, Key key, ListView rows, TextField search)
    {
        if (!rows.HasFocus)
        {
            return;
        }

        var action = FlagPanelKeyBindings.ActionFor(key, SelectedFlag(), search.HasFocus);
        if (action is FlagPanelAction.Edit edit)
        {
            key.Handled = true;
            RequestOpen(application, edit.Path, edit.Line);
            return;
        }
        if (action is FlagPanelAction.Preview preview)
        {
            key.Handled = true;
            ShowPreview(application, preview.Path, preview.Line, search, rows);
            return;
        }
        if (action is FlagPanelAction.ToggleFilter filter)
        {
            key.Handled = true;
            panelWork.Track(DispatchFlagAsync(new FlagCommand.ToggleFilter(filter.Category), search, rows));
            return;
        }

        FlagCommand? command = Is(key, KeyCode.CursorUp) || Is(key, KeyCode.K)
            ? new FlagCommand.MoveUp()
            : Is(key, KeyCode.CursorDown) || Is(key, KeyCode.J)
                ? new FlagCommand.MoveDown()
                : null;
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchFlagAsync(command, search, rows));
    }

    private async Task DispatchFlagAsync(FlagCommand command, TextField search, ListView rows)
    {
        await flagSession.DispatchAsync(command);
        Render(search, rows);
    }

    private void HandleCommentAction(
        IApplication application,
        CommentAction action,
        FileComment selected,
        TextField search,
        ListView files)
    {
        if (action is CommentAction.ReadComment)
        {
            ShowComment(application, selected);
            return;
        }

        if (action is CommentAction.RewriteComment)
        {
            RewriteComment(application, selected, search, files);
            return;
        }

        if (action is CommentAction.PreviewFile preview)
        {
            ShowPreview(application, preview.Path, 1, search, files);
            return;
        }

        if (action is CommentAction.SaveComments)
        {
            SaveComments(application, search, files);
            return;
        }

        if (action is CommentAction.ClearComments)
        {
            ClearComments(application, search, files);
            return;
        }

        if (action is CommentAction.CopyComments)
        {
            panelWork.Track(DispatchCommentAsync(new CommentCommand.CopyAll(), search, files));
            return;
        }

        panelWork.Track(DispatchCommentAsync(
            new CommentCommand.DeleteSelected(),
            search,
            files));
    }

    /// <summary>Clearing cannot be undone, so it is asked for twice. Cancel is
    /// offered last because the box opens on its last button, and a reader who
    /// presses Enter without reading should keep their notes.</summary>
    private void ClearComments(IApplication application, TextField search, ListView files)
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

        panelWork.Track(DispatchCommentAsync(new CommentCommand.ClearAll(), search, files));
    }

    private void SaveComments(IApplication application, TextField search, ListView files)
    {
        var path = OverThePanels(
            () => SavePrompt.Ask(application, "Save comments", SuggestedCommentPath()));
        if (path is null || !MayWriteOver(application, path))
        {
            return;
        }

        panelWork.Track(DispatchCommentAsync(new CommentCommand.SaveAll(path), search, files));
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
        FileComment selected,
        TextField search,
        ListView files)
    {
        var written = OverThePanels(
            () => CommentDialog.Ask(application, selected.DisplayPath, selected.Text));
        if (written is null)
        {
            return;
        }

        panelWork.Track(DispatchCommentAsync(
            new CommentCommand.RewriteSelected(written),
            search,
            files));
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
        CommentCommand command,
        TextField search,
        ListView files)
    {
        await commentSession.DispatchAsync(command);
        Render(search, files);
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
        ChangesetCommand command,
        TextField search,
        ListView files)
    {
        await changesetSession.DispatchAsync(command);
        Render(search, files);
    }

    private async Task RestoreSelectedAsync(
        IApplication application,
        TextField search,
        ListView files)
    {
        await changesetSession.DispatchAsync(new ChangesetCommand.RestoreSelected());
        application.Invoke(() => Render(search, files));
    }

    private void ShowDiff(IApplication application, TextField search, ListView files) =>
        panelWork.Track(ShowDiffAsync(application, search, files));

    private async Task ShowDiffAsync(IApplication application, TextField search, ListView files)
    {
        await changesetSession.DispatchAsync(new ChangesetCommand.LoadSelectedDiff());
        application.Invoke(() => ShowDiffDialog(application, search, files));
    }

    /// <summary>The diff is read in its own window rather than the shared cell
    /// dialog, because it answers to keys of its own: the reader steps through
    /// the changeset without closing what they are reading.</summary>
    private void ShowDiffDialog(IApplication application, TextField search, ListView files)
    {
        using var dialog = FullScreenDialog(DiffTitle());
        var diff = new ColoredTextView(wordWrap: false)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        diff.Load(DiffContent());
        SetBlackBackground(dialog);
        SetBlackBackground(diff);
        diff.KeyDown += (_, key) => HandleDiffKey(application, dialog, diff, key);
        dialog.Add(diff);
        OverThePanels(() => application.Run(dialog));
        Render(search, files);
    }

    private void HandleDiffKey(
        IApplication application,
        Window dialog,
        ColoredTextView diff,
        Key key)
    {
        var action = DiffKeyBindings.ActionFor(key);
        if (action is null)
        {
            return;
        }

        key.Handled = true;
        if (action is DiffAction.StepFile step)
        {
            StepDiff(application, dialog, diff, step.Step);
            return;
        }

        if (action is DiffAction.Scroll scroll)
        {
            diff.ScrollVertical(scroll.Rows);
        }
    }

    /// <summary>
    /// Moves the diff to another of the panel's files. The panel's selection
    /// goes with it, so the files it steps through are the ones the search left,
    /// and closing the diff leaves the reader on the file they stopped at.
    ///
    /// The load is waited out here for the same reason the preview waits out
    /// its own: the diff runs a loop that only turns when a key arrives, so a
    /// result posted back to it would sit unread until the reader pressed
    /// something else.
    /// </summary>
    private void StepDiff(
        IApplication application,
        Window dialog,
        ColoredTextView diff,
        int step)
    {
        changesetSession
            .DispatchAsync(new ChangesetCommand.StepDiff(step))
            .GetAwaiter()
            .GetResult();
        dialog.Title = DiffTitle();
        diff.Load(DiffContent());
        dialog.SetNeedsDraw();
        application.LayoutAndDraw(true);
    }

    private string DiffTitle() =>
        $"Diff — {ChangesetPanelSnapshot.From(changesetSession.State).DiffTitle} — " +
        "↑/k up  ↓/j down  n/N file  Esc close";

    private List<List<Cell>> DiffContent() =>
        DiffCells(ChangesetPanelSnapshot.From(changesetSession.State).DiffLines);

    private static List<List<Cell>> DiffCells(IReadOnlyList<DiffLine> lines) => lines
        .Select(line => Cell.ToCellList(
            line.Text,
            new global::Terminal.Gui.Drawing.Attribute(
                DiffAppearance.ForegroundFor(line.Tone),
                Color.Black)))
        .ToList();

    private async Task DispatchAsync(
        IApplication application,
        ExplorerCommand command,
        TextField search,
        ListView tests)
    {
        if (!RunsTests(command))
        {
            await session.DispatchAsync(command);
            Render(search, tests);
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
            Render(search, tests);
            await run;
        }
        finally
        {
            runCancellation = null;
        }

        application.Invoke(() => Render(search, tests));
    }

    private void RequestTestSource(
        IApplication application,
        bool preview,
        TextField search,
        ListView rows)
    {
        panelWork.Track(RequestTestSourceAsync(application, preview, search, rows));
    }

    private async Task RequestTestSourceAsync(
        IApplication application,
        bool preview,
        TextField search,
        ListView rows)
    {
        await session.DispatchAsync(new ExplorerCommand.LoadSelectedSource());
        if (session.State.SourceLocation is not { } source)
        {
            return;
        }

        application.Invoke(() =>
        {
            if (preview)
            {
                ShowPreview(application, source.Path, source.HighlightLine, search, rows);
                return;
            }

            RequestOpen(application, source.Path, source.HighlightLine);
        });
    }

    private void ShowPreview(
        IApplication application,
        string path,
        int line,
        TextField search,
        ListView rows)
    {
        if (ReadForPreview(application, path) is not { } text)
        {
            return;
        }

        previewing = new SourceLocation(path, line);
        using var preview = FullScreenDialog(PreviewTitle(path, line));
        var code = new Code
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(PreviewDetails().Length > 0 ? IssueDetailRows : 0),
            Text = text,
            Language = LanguageFrom(path),
            SyntaxHighlighter = new TextMateSyntaxHighlighter(ThemeName.DarkPlus)
        };
        var diagnostic = PreviewDiagnostic();
        var sourceHighlight = PreviewHighlight();
        code.GettingAttributeForRole += (_, args) =>
        {
            var background = args.Result?.Background ?? Color.Black;
            args.Result = new global::Terminal.Gui.Drawing.Attribute(
                PreviewCodeAppearance.ForegroundFor(args.Role),
                background);
            args.Handled = true;
        };
        code.ViewportChanged += (_, _) => ShowSourceHighlight(code, sourceHighlight);
        code.KeyDown += (_, key) =>
            HandlePreviewKey(application, preview, code, sourceHighlight, diagnostic, key);
        preview.Add(code, sourceHighlight, diagnostic);
        ScrollToHighlightedLine(code, line);
        ShowSourceHighlight(code, sourceHighlight);
        OverThePanels(() => application.Run(preview));
        Render(search, rows);
        LeaveForTheEditor(application);
    }

    private Label PreviewDiagnostic()
    {
        var text = PreviewDetails();
        var diagnostic = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(IssueDetailRows),
            Width = Dim.Fill(),
            Height = IssueDetailRows,
            Text = text,
            Visible = text.Length > 0
        };
        diagnostic.TextFormatter.MultiLine = true;
        diagnostic.TextFormatter.WordWrap = true;
        diagnostic.GettingAttributeForRole += (_, args) =>
        {
            var background = args.Result?.Background ?? Color.Black;
            args.Result = new global::Terminal.Gui.Drawing.Attribute(
                FileRowAppearance.ForegroundFor(PreviewDetailTone(), Color.White),
                background);
            args.Handled = true;
        };
        return diagnostic;
    }

    private string PreviewDetails() => shell.State.ActivePanel switch
    {
        PanelKind.Issues => IssuePanelSnapshot.From(issueSession.State).SelectedDetails,
        PanelKind.Flags => FlagPanelSnapshot.From(flagSession.State).SelectedDetails,
        _ => ""
    };

    private FileRowTone PreviewDetailTone() =>
        shell.State.ActivePanel == PanelKind.Issues &&
        issueSession.State.SelectedIndex < issueSession.State.Issues.Count &&
        issueSession.State.Issues[issueSession.State.SelectedIndex].Severity == IssueSeverity.Warning
            ? FileRowTone.Warning
            : shell.State.ActivePanel == PanelKind.Issues
                ? FileRowTone.Deleted
                : FileRowTone.Neutral;

    private bool PreviewHighlightsSource() =>
        shell.State.ActivePanel is PanelKind.Issues or PanelKind.Flags;

    private static Label PreviewHighlight()
    {
        var highlight = new Label
        {
            X = 0,
            Width = Dim.Fill(),
            Height = 1,
            Visible = false
        };
        highlight.GettingAttributeForRole += (_, args) =>
        {
            var foreground = args.Result?.Foreground ?? Color.White;
            args.Result = new global::Terminal.Gui.Drawing.Attribute(foreground, Color.BrightBlue);
            args.Handled = true;
        };
        return highlight;
    }

    private string? ReadForPreview(IApplication application, string path)
    {
        try
        {
            if (FileText.ReadWithin(path) is { } text)
            {
                return text;
            }

            OverThePanels(() => MessageBox.ErrorQuery(application, "Preview", TooLargeToPreview(path), "Ok"));
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            OverThePanels(() => MessageBox.ErrorQuery(application, "Preview", exception.Message, "Ok"));
            return null;
        }
    }

    private static string TooLargeToPreview(string path) =>
        $"{Path.GetFileName(path)} is larger than {FileText.MaxBytes / (1024 * 1024)} MB, " +
        "too large to preview. Open it in your editor instead.";

    /// <summary>Closing the preview only leaves the nested loop, so the shell
    /// beneath it is asked to stop as well when the reader left for the
    /// editor.</summary>
    private void LeaveForTheEditor(IApplication application)
    {
        if (openSourceRequested)
        {
            application.RequestStop();
        }
    }

    private static string PreviewTitle(string path, int line) =>
        $"Preview — {Path.GetFileName(path)}:{line} — ↑/k up  ↓/j down  n/N file  e edit  c comment  Esc close";

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

    private void HandlePreviewKey(
        IApplication application,
        Window preview,
        Code code,
        Label sourceHighlight,
        Label diagnostic,
        Key key)
    {
        var action = PreviewKeyBindings.ActionFor(key, code.Viewport.Height);
        if (action is null)
        {
            return;
        }

        key.Handled = true;
        if (action is PreviewAction.Edit)
        {
            RequestOpen(application, previewing.Path, previewing.HighlightLine);
            return;
        }

        if (action is PreviewAction.Comment)
        {
            CommentOn(application, previewing.Path);
            return;
        }

        if (action is PreviewAction.StepFile step)
        {
            StepPreview(application, preview, code, sourceHighlight, diagnostic, step.Step);
            return;
        }

        ScrollPreview(code, action);
        ShowSourceHighlight(code, sourceHighlight);
    }

    /// <summary>
    /// Moves the preview to another of the panel's rows. The panel's selection
    /// goes with it, so the rows it steps through are the ones the search and
    /// the filter left, and closing the preview leaves the reader on the file
    /// they stopped at.
    ///
    /// The work is waited out here rather than handed to the background: the
    /// preview runs a loop of its own that only turns when a key arrives, so a
    /// result posted back to it would not be picked up until the reader pressed
    /// something else.
    /// </summary>
    private void StepPreview(
        IApplication application,
        Window preview,
        Code code,
        Label sourceHighlight,
        Label diagnostic,
        int step)
    {
        var target = NextPreviewAsync(step).GetAwaiter().GetResult();
        if (target is null || target == previewing)
        {
            return;
        }

        ShowInPreview(application, preview, code, sourceHighlight, diagnostic, target);
    }

    private void ShowInPreview(
        IApplication application,
        Window preview,
        Code code,
        Label sourceHighlight,
        Label diagnostic,
        SourceLocation target)
    {
        if (ReadForPreview(application, target.Path) is not { } text)
        {
            return;
        }

        previewing = target;
        preview.Title = PreviewTitle(target.Path, target.HighlightLine);
        code.Language = LanguageFrom(target.Path);
        code.Text = text;
        diagnostic.Text = PreviewDetails();
        diagnostic.Visible = diagnostic.Text.Length > 0;
        code.Height = Dim.Fill(diagnostic.Visible ? IssueDetailRows : 0);
        ScrollToHighlightedLine(code, target.HighlightLine);
        ShowSourceHighlight(code, sourceHighlight);
        preview.SetNeedsDraw();
        application.LayoutAndDraw(true);
    }

    private static void ScrollToHighlightedLine(Code code, int line)
    {
        code.ScrollVertical(-code.GetContentSize().Height);
        code.ScrollVertical(Math.Max(0, line - 1));
    }

    private void ShowSourceHighlight(Code code, Label label)
    {
        var highlight = PreviewSourceHighlight.From(
            code.Text,
            previewing.HighlightLine,
            code.Viewport.Y,
            code.Viewport.Height,
            PreviewHighlightsSource());
        label.Text = highlight.Text;
        label.Y = highlight.Row;
        label.Visible = highlight.Visible;
    }

    /// <summary>The rows are tried in turn from where the panel stands, because
    /// a row can have nothing to show: a folder, a suite whose source cannot be
    /// found, or a file that has been deleted.</summary>
    private async Task<SourceLocation?> NextPreviewAsync(int step)
    {
        foreach (var index in RowRing.From(ActiveRowCount(), ActiveSelectedIndex(), step))
        {
            if (await PreviewAtAsync(index) is { } target)
            {
                return target;
            }
        }

        return null;
    }

    private int ActiveRowCount() => ActiveFileSession() is { } files
        ? files.State.VisibleNodes.Count
        : shell.State.ActivePanel switch
        {
            PanelKind.Changes => changesetSession.State.Files.Count,
            PanelKind.Issues => issueSession.State.Issues.Count,
            PanelKind.Comments => commentSession.State.Comments.Count,
            PanelKind.Flags => flagSession.State.Flags.Count,
            _ => session.State.VisibleNodes.Count
        };

    private int ActiveSelectedIndex() => ActiveFileSession() is { } files
        ? files.State.SelectedIndex
        : shell.State.ActivePanel switch
        {
            PanelKind.Changes => changesetSession.State.SelectedIndex,
            PanelKind.Issues => issueSession.State.SelectedIndex,
            PanelKind.Comments => commentSession.State.SelectedIndex,
            PanelKind.Flags => flagSession.State.SelectedIndex,
            _ => session.State.SelectedIndex
        };

    private async Task<SourceLocation?> PreviewAtAsync(int index)
    {
        if (ActiveFileSession() is { } files)
        {
            await files.DispatchAsync(new FileExplorerCommand.SelectIndex(index));
            var node = files.State.VisibleNodes[files.State.SelectedIndex];
            return node.Kind == FileNodeKind.File
                ? new SourceLocation(node.Files[0].Path, 1)
                : null;
        }

        if (shell.State.ActivePanel == PanelKind.Changes)
        {
            await changesetSession.DispatchAsync(new ChangesetCommand.SelectIndex(index));
            var file = changesetSession.State.Files[changesetSession.State.SelectedIndex];
            return file.Kind == ChangeKind.Deleted ? null : new SourceLocation(file.Path, 1);
        }

        if (shell.State.ActivePanel == PanelKind.Issues)
        {
            await issueSession.DispatchAsync(new IssueCommand.SelectIndex(index));
            var issue = issueSession.State.Issues[issueSession.State.SelectedIndex];
            return new SourceLocation(issue.Path, issue.Line);
        }

        if (shell.State.ActivePanel == PanelKind.Comments)
        {
            await commentSession.DispatchAsync(new CommentCommand.SelectIndex(index));
            var comment = commentSession.State.Comments[commentSession.State.SelectedIndex];
            return new SourceLocation(comment.Path, 1);
        }

        if (shell.State.ActivePanel == PanelKind.Flags)
        {
            await flagSession.DispatchAsync(new FlagCommand.SelectIndex(index));
            var flag = flagSession.State.Flags[flagSession.State.SelectedIndex];
            return new SourceLocation(flag.Path, flag.Line);
        }

        await session.DispatchAsync(new ExplorerCommand.SelectIndex(index));
        await session.DispatchAsync(new ExplorerCommand.LoadSelectedSource());
        return session.State.SourceLocation;
    }

    private static void ScrollPreview(Code code, PreviewAction action)
    {
        switch (action)
        {
            case PreviewAction.Scroll scroll:
                code.ScrollVertical(scroll.Rows);
                return;
            case PreviewAction.ScrollToStart:
                code.ScrollVertical(-code.GetContentSize().Height);
                return;
            case PreviewAction.ScrollToEnd:
                code.ScrollVertical(code.GetContentSize().Height);
                return;
        }
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
        flagSession,
        issueSession);

    private PanelReload PanelReload() => new(
        [fileSession, folderSession],
        changesetSession,
        target,
        flagSession,
        issueSession);

    /// <summary>
    /// Asks on every frame rather than reloading as the edits land, because a
    /// single save arrives as several edits and an agent's change arrives as
    /// dozens. The burst answers once the tree has been quiet.
    /// </summary>
    private void ReloadWhenTheWorkingTreeSettles(
        IApplication application,
        TextField search,
        ListView tests)
    {
        if (workspaceWatcher is null)
        {
            return;
        }

        application.AddTimeout(EditPollInterval, () =>
        {
            if (!reloadingWhatIsOnDisk && outsideEdits.SettledAt(sinceWatching.Elapsed))
            {
                ReloadWhatIsOnDisk(application, search, tests);
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
    private void Rebuild(IApplication application, TextField search, ListView rows, bool askedFor)
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
        Render(search, rows);
        sinceLoadStarted.Restart();
        TurnActivityMarker(application, search, rows);
        sinceRebuildStarted.Restart();
        ShowToast(application, RebuildToast.Rebuilding(TimeSpan.Zero));
        TurnRebuildToast(application);
        panelWork.Track(RebuildAsync(application, rebuild, search, rows, loadCancellation!.Token));
    }

    private async Task RebuildAsync(
        IApplication application,
        ProjectRebuild rebuild,
        TextField search,
        ListView rows,
        CancellationToken cancellationToken)
    {
        await rebuild.RunAsync(
            () =>
            {
                application.Invoke(() => Render(search, rows));
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
    private void ReloadWhatIsOnDisk(IApplication application, TextField search, ListView tests)
    {
        reloadingWhatIsOnDisk = true;
        panelWork.Track(ReloadWhatIsOnDiskAsync(application, search, tests, loadCancellation!.Token));
    }

    private async Task ReloadWhatIsOnDiskAsync(
        IApplication application,
        TextField search,
        ListView tests,
        CancellationToken cancellationToken)
    {
        try
        {
            await PanelReload().FromDiskAsync(
                () =>
                {
                    application.Invoke(() => Render(search, tests));
                    return Task.CompletedTask;
                },
                cancellationToken);
        }
        finally
        {
            reloadingWhatIsOnDisk = false;
        }
    }

    private void RefreshEditedPanels(IApplication application, TextField search, ListView tests)
    {
        if (!panelsWereEdited)
        {
            return;
        }

        panelsWereEdited = false;
        outsideEdits.Forget();
        editsSinceTheBuild.Noticed();
        panelWork.Track(RefreshEditedPanelsAsync(application, search, tests, loadCancellation!.Token));
    }

    private Task RefreshEditedPanelsAsync(
        IApplication application,
        TextField search,
        ListView tests,
        CancellationToken cancellationToken) =>
        EditorWorkflow().RefreshAsync(
            () =>
            {
                application.Invoke(() => Render(search, tests));
                return Task.CompletedTask;
            },
            cancellationToken);

    private void Render(TextField search, ListView tests)
    {
        var fileExplorer = ActiveFileSession();
        shortcutSegments = PanelShortcuts.For(
            shell.State.ActivePanel,
            (fileExplorer ?? fileSession).State,
            changesetSession.State,
            session.State,
            commentSession.State,
            search.HasFocus,
            flagSession.State,
            issueSession.State);
        ShowShortcuts();
        if (fileExplorer is not null)
        {
            RenderFiles(search, tests, fileExplorer);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Changes)
        {
            RenderChanges(search, tests);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Issues)
        {
            RenderIssues(search, tests);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Comments)
        {
            RenderComments(search, tests);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Flags)
        {
            RenderFlags(search, tests);
            return;
        }

        var snapshot = TestPanelSnapshot.From(session.State, target);
        tests.Title = $"Tests — {snapshot.Breadcrumb}";
        tests.Height = Dim.Fill(RowsBelowTheList);
        HideSegments();
        ShowFilters(snapshot.Filters);
        ShowEmptyState(snapshot.EmptyMessage);
        testStatus!.Visible = true;
        testStatus.Text = snapshot.StatusLine;
        search.Title = snapshot.SearchQuery.Length == 0
            ? "Search"
            : $"Search — {snapshot.SearchHitCount} hits";
        search.Text = snapshot.SearchQuery;
        testNodes = snapshot.Tests;
        ListRows(tests, snapshot.Tests, () => snapshot.TestRows);
        if (snapshot.Tests.Count > 0)
        {
            tests.SelectedItem = snapshot.SelectedIndex;
        }
    }

    /// <summary>Wraps to the width the label has now, which is why it is also
    /// called as the width changes rather than only as the shortcuts change.</summary>
    private void ShowShortcuts() =>
        shortcuts!.Text = string.Join(
            '\n',
            ShortcutLines.For(shortcutSegments, shortcuts.Viewport.Width));

    private void ListRows(ListView list, object content, Func<IReadOnlyList<string>> rows)
    {
        if (ReferenceEquals(listedContent, content))
        {
            return;
        }

        listedContent = content;
        list.SetSource(new ObservableCollection<string>(rows()));
    }

    private void ListTonedRows(
        ListView list,
        object content,
        Func<IReadOnlyList<(string Text, FileRowTone Tone)>> rows)
    {
        if (ReferenceEquals(listedContent, content))
        {
            return;
        }

        var listed = rows();
        listedContent = content;
        rowTones = listed.Select(row => row.Tone).ToArray();
        list.SetSource(new ObservableCollection<string>(listed.Select(row => row.Text)));
    }

    private void RenderFiles(TextField search, ListView files, FileExplorerSession fileExplorer)
    {
        var snapshot = FilePanelSnapshot.From(fileExplorer.State);
        files.Title = shell.State.Panels[shell.State.ActiveIndex];
        RenderRows(
            search,
            files,
            snapshot.SearchQuery,
            snapshot.SearchHitCount,
            snapshot.Nodes,
            () => [.. snapshot.Rows.Select(row => (row.Text, row.Tone))],
            snapshot.SelectedIndex,
            snapshot.StatusSegments,
            snapshot.Filters,
            snapshot.EmptyMessage);
    }

    private void RenderChanges(TextField search, ListView files)
    {
        var snapshot = ChangesetPanelSnapshot.From(changesetSession.State);
        files.Title = "Changes";
        RenderRows(
            search,
            files,
            snapshot.SearchQuery,
            snapshot.SearchHitCount,
            snapshot.Files,
            () => [.. snapshot.Rows.Select(row => (row.Text, row.Tone))],
            snapshot.SelectedIndex,
            snapshot.StatusSegments,
            [],
            snapshot.EmptyMessage);
    }

    private void RenderIssues(TextField search, ListView rows)
    {
        var snapshot = IssuePanelSnapshot.From(issueSession.State);
        var layout = IssuePanelLayout.From(snapshot, rows.Viewport.Width);
        rows.Title = "Issues";
        RenderRows(search, rows, snapshot.SearchQuery, snapshot.Issues.Count, layout,
            () => [.. layout.Rows.Select(row => (row.Text, row.Tone))], layout.SelectedRowIndex,
            snapshot.StatusSegments, snapshot.Filters, snapshot.EmptyMessage);
    }

    private void RenderComments(TextField search, ListView files)
    {
        var snapshot = CommentPanelSnapshot.From(commentSession.State);
        files.Title = "Comments";
        RenderRows(
            search,
            files,
            snapshot.SearchQuery,
            snapshot.SearchHitCount,
            snapshot.Comments,
            () => [.. snapshot.Rows.Select(row => (row.Text, row.Tone))],
            snapshot.SelectedIndex,
            snapshot.StatusSegments,
            [],
            snapshot.EmptyMessage);
    }

    private void RenderFlags(TextField search, ListView rows)
    {
        var snapshot = FlagPanelSnapshot.From(flagSession.State);
        rows.Title = "Flags";
        RenderRows(
            search,
            rows,
            snapshot.SearchQuery,
            snapshot.Flags.Count,
            snapshot.Flags,
            () => [.. snapshot.Rows.Select(row => (row.Text, row.Tone))],
            snapshot.SelectedRowIndex,
            snapshot.StatusSegments,
            snapshot.Filters,
            snapshot.EmptyMessage);
    }

    private void RenderRows(
        TextField search,
        ListView files,
        string searchQuery,
        int searchHitCount,
        object content,
        Func<IReadOnlyList<(string Text, FileRowTone Tone)>> rows,
        int selectedIndex,
        IReadOnlyList<FileStatusSegment> segments,
        IReadOnlyList<FilterChip> filters,
        string emptyMessage)
    {
        search.Title = searchQuery.Length == 0 ? "Search" : $"Search — {searchHitCount} hits";
        search.Text = searchQuery;
        ListTonedRows(files, content, rows);
        files.Height = Dim.Fill(RowsBelowTheList);
        testStatus!.Visible = false;
        ShowSegments(segments);
        ShowFilters(filters);
        ShowEmptyState(emptyMessage);
        if (rowTones.Count > 0)
        {
            files.SelectedItem = selectedIndex;
        }
    }

    private void ShowEmptyState(string message)
    {
        emptyState!.Text = message;
        emptyState.Visible = message.Length > 0;
    }

    private void ShowSegments(IReadOnlyList<FileStatusSegment> segments)
    {
        statusSegments = segments;
        var placed = StatusSegmentLayout.Place(segments, WorkspaceX, SegmentGap);
        for (var index = 0; index < segmentLabels.Count; index++)
        {
            Show(segmentLabels[index], index < placed.Count ? placed[index] : null);
        }
    }

    private void ShowFilters(IReadOnlyList<FilterChip> chips)
    {
        filterChips = chips;
        var columns = StatusSegmentLayout.ColumnsFor(
            chips.Select(chip => chip.Text).ToArray(),
            WorkspaceX,
            SegmentGap);
        for (var index = 0; index < filterLabels.Count; index++)
        {
            ShowChip(filterLabels[index], chips, columns, index);
        }
    }

    private static void ShowChip(
        Label label,
        IReadOnlyList<FilterChip> chips,
        IReadOnlyList<int> columns,
        int index)
    {
        label.Visible = index < chips.Count;
        if (index >= chips.Count)
        {
            return;
        }

        label.X = columns[index];
        label.Width = chips[index].Text.Length;
        label.Text = chips[index].Text;
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

    private void ColorTreeRow(ListView tree, ListViewRowEventArgs args)
    {
        if (shell.State.ActivePanel == PanelKind.Tests)
        {
            ColorTestRow(tree, args);
            return;
        }

        ColorFileRow(tree, args);
    }

    private void ColorFileRow(ListView files, ListViewRowEventArgs args)
    {
        if (args.Row >= rowTones.Count)
        {
            return;
        }

        args.RowAttribute = FileRowAppearance.For(
            rowTones[args.Row],
            files.IsSelectedOrMarked(args.Row),
            files.GetAttributeForRole(VisualRole.Normal),
            files.GetAttributeForRole(VisualRole.Focus));
    }

    private void ColorTestRow(ListView tests, ListViewRowEventArgs args)
    {
        if (args.Row >= testNodes.Count || tests.IsSelectedOrMarked(args.Row))
        {
            return;
        }

        var node = testNodes[args.Row];
        SetRowForeground(tests, args, TestRowAppearance.ForegroundFor(node.Outcome, node.Update));
    }

    private static void SetRowForeground(
        ListView list,
        ListViewRowEventArgs args,
        Color foreground)
    {
        var background = list.GetAttributeForRole(VisualRole.Normal).Background;
        args.RowAttribute = new global::Terminal.Gui.Drawing.Attribute(foreground, background);
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
