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
using TerminalDotnet.Filters;
using TerminalDotnet.Issues;
using TerminalDotnet.Search;
using static TerminalDotnet.Terminal.KeyMatch;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace TerminalDotnet.Terminal;

internal sealed class TestRunnerApplication(
    PanelSessions panels,
    string target,
    IFileOpener editorLauncher,
    IWorkspaceWatcher workspaceWatcher)
{
    private const int ContentInset = 1;
    private const int StatusRow = ShortcutLines.Rows + 1;
    private const int SearchRow = StatusRow + 1;

    /// <summary>The choices a message box offers, in the order it offers them.
    /// It opens on its last button, so the safe choice goes last.</summary>
    private const int ClearChoice = 0;
    private const int KeepChoice = 1;

    private static readonly TimeSpan SettleDuration = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan EditPollInterval = TimeSpan.FromMilliseconds(250);

    private readonly TestExplorerSession session = panels.Tests;
    private readonly FileExplorerSession fileSession = panels.ProjectFiles;
    private readonly FileExplorerSession folderSession = panels.FolderFiles;
    private readonly ChangesetSession changesetSession = panels.Changes;
    private readonly CommentSession commentSession = panels.Comments;
    private readonly IssueSession issueSession = panels.Issues;

    private readonly PanelStartup startup = new(
        panels.ProjectFiles,
        panels.FolderFiles,
        panels.Changes,
        panels.Tests,
        target,
        panels.Issues);

    private readonly PanelReload reload = new(
        [panels.ProjectFiles, panels.FolderFiles],
        panels.Changes,
        target,
        panels.Issues);

    private readonly ExplorerEditorWorkflow editorWorkflow = new(
        [panels.ProjectFiles, panels.FolderFiles],
        panels.Changes,
        editorLauncher,
        target,
        panels.Issues);

    private readonly ProjectRebuild rebuild = new(panels.Issues, panels.Tests, target);

    private IReadOnlyDictionary<PanelKind, IListNavigation>? navigation;

    private CancellationTokenSource? runCancellation;
    private CancellationTokenSource? loadCancellation;
    private readonly Stopwatch sinceLoadStarted = new();
    private readonly Stopwatch sincePanelsAppeared = new();
    private readonly PanelShell shell = new();
    private readonly BackgroundWork panelWork = new();
    private readonly EditBurst outsideEdits = new();
    private readonly EditsSinceTheBuild editsSinceTheBuild = new();
    private readonly Stopwatch sinceRebuildStarted = new();
    private ToastPanel toast = new(ContentInset);
    private readonly Stopwatch sinceWatching = new();
    private bool openSourceRequested;
    private bool panelsWereEdited;
    private bool reloadingWhatIsOnDisk;
    private string? openPath;
    private int openLine = 1;
    private readonly DialogHost dialogs = new();

    private Label? testStatus;
    private Label? shortcuts;
    private IReadOnlyList<string> shortcutSegments = [];

    /// <summary>The views of the terminal that is up. Each terminal the app
    /// raises builds its own, so these stand in until the first one does.</summary>
    private TextField search = new();
    private StatusSegmentLine segments = new(ContentInset, StatusRow);
    private FilterChipLine filters = new(new View(), SearchRow);
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
        var outsideTheLoop = SynchronizationContext.Current;
        var loop = new TerminalLoopContext(work => application.Invoke(work));
        SynchronizationContext.SetSynchronizationContext(loop);
        try
        {
            return RunPanels(application, loop);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(outsideTheLoop);
        }
    }

    private bool RunPanels(IApplication application, TerminalLoopContext loop)
    {
        using var window = new Window { Title = $"terminal-dotnet - {VersionNumber.Current}" };
        search = Search();
        workspace = Workspace();
        testStatus = TestStatus();
        ViewColours.ColourText(testStatus, () => TestStatusAppearance.ForegroundFor(session.State));
        segments = new StatusSegmentLine(ContentInset, StatusRow);
        filters = new FilterChipLine(search, SearchRow);
        shortcuts = Shortcuts();
        shortcuts.ViewportChanged += (_, _) => ShowShortcuts();

        window.Add(workspace, search, testStatus, shortcuts);
        window.Add([.. segments.Labels]);
        window.Add([.. filters.Labels]);
        toast = new ToastPanel(ContentInset);
        window.Add(toast.View);
        search.ValueChanged += async (_, _) =>
        {
            await SearchAsync(search.Text);
            RenderOnTheLoop();
        };
        application.Keyboard.KeyDown += (_, key) =>
        {
            HandleKey(application, key);
            HoldTheFocus(key);
        };
        Render();
        SettleOnceTheFirstFrameIsDrawn(application);
        ActiveList.SetFocus();
        FillPanels(application);
        RefreshEditedPanels(application);
        ReloadWhenTheWorkingTreeSettles(application);

        application.Run(window);
        ShutDown(loop);
        return openSourceRequested;
    }

    /// <summary>
    /// The terminal's replies to the driver's start-up queries arrive once
    /// the first frame is on screen, and the tiled panels take long enough to
    /// draw that a clock started before it would have run out by then. The
    /// panels count as having appeared when that frame is drawn.
    /// </summary>
    private void SettleOnceTheFirstFrameIsDrawn(IApplication application)
    {
        sincePanelsAppeared.Restart();
        var drawn = false;
        application.LayoutAndDrawComplete += (_, _) =>
        {
            if (drawn)
            {
                return;
            }

            drawn = true;
            sincePanelsAppeared.Restart();
        };
    }

    /// <summary>
    /// Cancelling a run only asks its process tree to end, so the panels'
    /// work is waited out before the application is torn down. Returning
    /// first would leave `dotnet test` orphaned behind the exiting terminal.
    /// The loop no longer takes that work, so it is released to finish
    /// without it.
    /// </summary>
    private void ShutDown(TerminalLoopContext loop)
    {
        runCancellation?.Cancel();
        loadCancellation?.Cancel();
        loop.Release();
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
        startup.LoadPendingAsync(PanelLanded, cancellationToken);

    private static string? TerminalDriver() => TerminalDriverChoice.FromEnvironment();

    private static Label TestStatus() => new()
    {
        X = ContentInset,
        Y = Pos.AnchorEnd(StatusRow),
        Width = Dim.Fill(ContentInset),
        Height = 1
    };

    private static TextField Search() => new()
    {
        Title = "Search",
        X = ContentInset,
        Y = Pos.AnchorEnd(SearchRow),
        Width = Dim.Percent(40),
        Height = 1,
        TabStop = TabBehavior.NoStop
    };

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
            shown.Add(lists[panel].View);
            shown.Add([.. lists[panel].Overlays]);
            var chosen = panel;
            lists[panel].RowChosen += row => ChooseRow(chosen, row);
            lists[panel].View.HasFocusChanged += (_, _) => FollowTheFocus(chosen, lists[chosen].View.HasFocus);
        }

        preview = new PreviewPanel();
        shown.Add(preview.View);
        shown.Add([.. preview.Overlays]);
        preview.View.HasFocusChanged += (_, _) => FollowTheFocus(PanelKind.Preview, preview.View.HasFocus);
        shown.ViewportChanged += (_, _) => ArrangePanels();
        return shown;
    }

    /// <summary>A click gives a panel the focus without a key reaching the
    /// shell, so the shell follows the focus to wherever it landed.</summary>
    private void FollowTheFocus(PanelKind panel, bool focused)
    {
        if (!focused || dialogs.AnyOpen || shell.State.ActivePanel == panel || running is not { } application)
        {
            return;
        }

        OpenPanel(application, panel);
    }

    /// <summary>A row picked with the mouse moves the panel's own selection,
    /// so the preview and the panel's keys follow it.</summary>
    private void ChooseRow(PanelKind panel, int row) =>
        panelWork.Track(ChooseRowAsync(panel, row));

    private async Task ChooseRowAsync(PanelKind panel, int row)
    {
        await Navigation[panel].ChooseRowAsync(row);
        RenderOnTheLoop();
    }

    /// <summary>How the shell moves through each list panel. The preview is
    /// not a list, so it has none.</summary>
    private IReadOnlyDictionary<PanelKind, IListNavigation> Navigation => navigation ??=
        new Dictionary<PanelKind, IListNavigation>
        {
            [PanelKind.Explorer] = new FileListNavigation(ExplorerSession),
            [PanelKind.Tests] = new TestListNavigation(session),
            [PanelKind.Changes] = new ChangesetListNavigation(changesetSession),
            [PanelKind.Issues] = new IssueListNavigation(issueSession, IssueAtRow),
            [PanelKind.Comments] = new CommentListNavigation(commentSession)
        };

    private IListNavigation? ActiveNavigation() => Navigation.GetValueOrDefault(shell.State.ActivePanel);

    private int IssueAtRow(int row) => IssuePanelLayout
        .From(IssuePanelSnapshot.From(issueSession.State), lists[PanelKind.Issues].View.Viewport.Width)
        .IssueAt(row);

    private void ArrangePanels()
    {
        var layout = PanelLayout.For(
            workspace.Viewport.Width,
            workspace.Viewport.Height,
            shell.State.ExpandedList,
            shell.State.FullScreenPanel);
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

        if (dialogs.AnyOpen)
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
        if (key.Handled || dialogs.AnyOpen || search.HasFocus)
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
                panelWork.Track(ClearSearchAsync());
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
                OpenPanel(application, panels => panels.SelectNext());
                return;
            case ShellAction.SelectPreviousPanel:
                OpenPanel(application, panels => panels.SelectPrevious());
                return;
            case ShellAction.ShowCommands:
                ShowCommands(application);
                return;
            case ShellAction.Refresh:
                ReloadWhatIsOnDisk(application);
                Rebuild(application, askedFor: true);
                return;
            case ShellAction.ToggleFullScreen:
                shell.ToggleFullScreen();
                ShowActivePanel();
                return;
            case ShellAction.Dismiss when shell.LeaveFullScreen():
                ShowActivePanel();
                return;
            case ShellAction.Dismiss:
            case ShellAction.HoldFocus:
                return;
            case ShellAction.Quit:
                QuitUnlessNotesWouldBeLost(application);
                return;
        }
    }

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
        var chosen = dialogs.Over(() => MessageBox.Query(
            application,
            "Quit",
            CommentPrompt.QuitLoses(count),
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
    private void OpenPanel(IApplication application, PanelKind panel) =>
        OpenPanel(application, panels => panels.Select(panel));

    private void OpenPanel(IApplication application, Action<PanelShell> move)
    {
        var from = shell.State.ActivePanel;
        move(shell);
        ShowActivePanel();
        if (editsSinceTheBuild.WorthRebuildingOnOpening(from, shell.State.ActivePanel))
        {
            Rebuild(application, askedFor: false);
        }
    }

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

    private string ActiveSearchQuery() => ActiveNavigation()?.SearchQuery ?? "";

    private Task SearchAsync(string query) => ActiveNavigation()?.SearchAsync(query) ?? Task.CompletedTask;

    private async Task ClearSearchAsync()
    {
        if (ActiveNavigation() is not { } list)
        {
            return;
        }

        await list.ClearSearchAsync();
        RenderOnTheLoop();
    }

    private void HandleFileKey(
        IApplication application,
        Key key,
        FileExplorerSession fileExplorer)
    {
        if (!ActiveList.HasFocus)
        {
            return;
        }

        if (FilePanelKeyBindings.ActionFor(key, SelectedFile(fileExplorer), search.HasFocus) is { } action)
        {
            key.Handled = true;
            HandleFileAction(application, action, fileExplorer);
            return;
        }

        var command = FileCommandFor(key);
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchFileAsync(fileExplorer, command));
    }

    private void HandleFileAction(
        IApplication application,
        FilePanelAction action,
        FileExplorerSession fileExplorer)
    {
        switch (action)
        {
            case FilePanelAction.ToggleFilter toggle:
                panelWork.Track(DispatchFileAsync(fileExplorer, new FileExplorerCommand.ToggleFilter(toggle.Filter)));
                return;
            case FilePanelAction.ToggleAllFiles:
                shell.ToggleAllFiles();
                Render();
                return;
            case FilePanelAction.OpenFile open:
                RequestOpen(application, open.Path, line: 1);
                return;
        }
    }

    private static VisibleFileNode? SelectedFile(FileExplorerSession fileExplorer) =>
        fileExplorer.State.VisibleNodes.Count == 0
            ? null
            : fileExplorer.State.VisibleNodes[fileExplorer.State.SelectedIndex];

    private static FileExplorerCommand? FileCommandFor(Key key)
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
        RenderOnTheLoop();
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
        if (ChangesetPanelKeyBindings.ActionFor(key, selected, search.HasFocus) is { } action)
        {
            key.Handled = true;
            HandleChangesetAction(application, action);
            return;
        }

        var command = ChangesetCommandFor(key);
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchChangesetAsync(command));
    }

    private void HandleChangesetAction(IApplication application, ChangesetAction action)
    {
        switch (action)
        {
            case ChangesetAction.ShowDiff:
                shell.PreviewChangeDiff();
                Render();
                return;
            case ChangesetAction.OpenFile open:
                RequestOpen(application, open.Path, line: 1);
                return;
            case ChangesetAction.PreviewFile:
                shell.PreviewChangedFile();
                Render();
                return;
            case ChangesetAction.RestoreFile:
                panelWork.Track(RestoreSelectedAsync());
                return;
        }
    }

    private void HandleIssueKey(IApplication application, Key key)
    {
        if (!ActiveList.HasFocus)
        {
            return;
        }

        var action = IssuePanelKeyBindings.ActionFor(key, SelectedIssue(), search.HasFocus);
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
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchIssueAsync(command));
    }

    private CompilationIssue? SelectedIssue() =>
        issueSession.State.SelectedIndex < issueSession.State.Issues.Count
            ? issueSession.State.Issues[issueSession.State.SelectedIndex]
            : null;

    private async Task DispatchIssueAsync(IssueCommand command)
    {
        await issueSession.DispatchAsync(command);
        RenderOnTheLoop();
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
        switch (action)
        {
            case CommentAction.ReadComment:
                ShowComment(application, selected);
                return;
            case CommentAction.RewriteComment:
                RewriteComment(application, selected);
                return;
            case CommentAction.SaveComments:
                SaveComments(application);
                return;
            case CommentAction.ClearComments:
                ClearComments(application);
                return;
            case CommentAction.CopyComments:
                panelWork.Track(DispatchCommentAsync(new CommentCommand.CopyAll()));
                return;
            case CommentAction.DeleteComment:
                panelWork.Track(DispatchCommentAsync(new CommentCommand.DeleteSelected()));
                return;
        }
    }

    /// <summary>Clearing cannot be undone, so it is asked for twice. Cancel is
    /// offered last because the box opens on its last button, and a reader who
    /// presses Enter without reading should keep their notes.</summary>
    private void ClearComments(IApplication application)
    {
        var count = commentSession.State.Comments.Count;
        var chosen = dialogs.Over(() => MessageBox.Query(
            application,
            "Clear comments",
            CommentPrompt.ClearAll(count),
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
        var path = dialogs.Over(
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

        var chosen = dialogs.Over(() => MessageBox.Query(
            application,
            "Save comments",
            $"{Path.GetFileName(path)} already exists. Saving replaces what is in it.",
            "Replace it",
            "Keep it"));
        return chosen != KeepChoice;
    }

    private string SuggestedCommentPath() => Path.Combine(LaunchFolder(), "comments.md");

    private string LaunchFolder() => Path.GetDirectoryName(Path.GetFullPath(target))!;

    private void ShowComment(IApplication application, FileComment selected) => dialogs.ShowText(
        application,
        $"Comment — {selected.DisplayPath} — ↑/↓ scroll  Esc close",
        CommentCells(selected.Text),
        wordWrap: true);

    private static List<List<Cell>> CommentCells(string text) => text
        .Split('\n')
        .Select(line => Cell.ToCellList(
            line.TrimEnd('\r'),
            new Attribute(Color.White, Color.Black)))
        .ToList();

    private void RewriteComment(
        IApplication application,
        FileComment selected)
    {
        var written = dialogs.Over(
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
        RenderOnTheLoop();
    }

    private static ChangesetCommand? ChangesetCommandFor(Key key)
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
        RenderOnTheLoop();
    }

    private async Task RestoreSelectedAsync()
    {
        await changesetSession.DispatchAsync(new ChangesetCommand.RestoreSelected());
        RenderOnTheLoop();
    }

    private async Task DispatchAsync(
        IApplication application,
        ExplorerCommand command)
    {
        if (!RunsTests(command))
        {
            await session.DispatchAsync(command);
            RenderOnTheLoop();
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

        RenderOnTheLoop();
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
        var written = dialogs.Over(
            () => CommentDialog.Ask(application, displayPath, commentSession.Against(path)));
        if (written is null)
        {
            return;
        }

        panelWork.Track(DispatchCommentAsync(new CommentCommand.Add(path, displayPath, written)));
    }

    /// <summary>Comments read against the tree the app was launched in, the
    /// same way the panels name their files.</summary>
    private string DisplayPathFor(string path) => Path.GetRelativePath(LaunchFolder(), path);

    private void ShowTestOutput(IApplication application)
    {
        var snapshot = TestPanelSnapshot.From(session.State, target, sinceLoadStarted.Elapsed);
        dialogs.ShowText(
            application,
            $"{snapshot.SelectedOutputTitle} — ↑/↓ scroll  Esc close",
            AnsiTestOutput.ToCells(snapshot.SelectedOutput),
            wordWrap: true);
    }

    private void ShowCommands(IApplication application) => dialogs.ShowText(
        application,
        "Commands — ↑/↓ scroll  Esc close",
        CommandMenu.Rows().Select(CommandMenuCells).ToList(),
        wordWrap: false);

    private static List<Cell> CommandMenuCells(CommandMenuRow row) => Cell.ToCellList(
        row.Text,
        new Attribute(
            row.IsHeading ? Color.BrightCyan : Color.White,
            Color.Black)).ToList();

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
        if (openPath is null)
        {
            return;
        }

        editorWorkflow.OpenAsync(openPath, openLine).GetAwaiter().GetResult();
        panelsWereEdited = true;
    }

    /// <summary>
    /// Asks on every frame rather than reloading as the edits land, because a
    /// single save arrives as several edits and an agent's change arrives as
    /// dozens. The burst answers once the tree has been quiet.
    /// </summary>
    private void ReloadWhenTheWorkingTreeSettles(
        IApplication application)
    {
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
        var started = rebuild.Start();
        if (started == RebuildStart.WaitingOnTheRun && askedFor)
        {
            toast.Show(application, RebuildToast.WaitingOnTheRun());
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
        toast.Show(application, RebuildToast.Rebuilding(TimeSpan.Zero));
        toast.KeepTurning(application, () => RebuildToast.Rebuilding(sinceRebuildStarted.Elapsed));
        panelWork.Track(RebuildAsync(application, loadCancellation!.Token));
    }

    private async Task RebuildAsync(
        IApplication application,
        CancellationToken cancellationToken)
    {
        await rebuild.RunAsync(PanelLanded, cancellationToken);
        application.Invoke(() => toast.Show(application, RebuildToast.Finished(issueSession.State, session.State)));
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
            await reload.FromDiskAsync(PanelLanded, cancellationToken);
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
        editorWorkflow.RefreshAsync(PanelLanded, cancellationToken);

    private Task PanelLanded()
    {
        RenderOnTheLoop();
        return Task.CompletedTask;
    }

    /// <summary>Work that has awaited something draws through the loop, which
    /// passes it over once the terminal it would draw on has gone.</summary>
    private void RenderOnTheLoop() => running?.Invoke(Render);

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
            issueSession.State,
            shell.State.FullScreen);
        ShowShortcuts();
        RenderExplorer();
        RenderTests();
        RenderChanges();
        RenderIssues();
        RenderComments();
        if (shell.State.ActivePanel == PanelKind.Preview)
        {
            filters.Show([]);
        }

        FollowTheSelection();
    }

    private void FollowTheSelection()
    {
        var subject = PreviewSubject.For(
            shell.State.PreviewedList,
            PanelStatesNow(),
            shell.State.PreviewsChangedFile);
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
        var action = PreviewKeyBindings.ActionFor(key, preview.PageHeight, showsAFile: previewedSource is not null);
        if (action is null)
        {
            return;
        }

        key.Handled = true;
        switch (action)
        {
            case PreviewAction.Edit when previewedSource is { } source:
                RequestOpen(application, source.Path, source.HighlightLine);
                return;
            case PreviewAction.Comment when previewedSource is { } source:
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

    /// <summary>Goes straight to the next row with something to preview,
    /// and stays put when there is none.</summary>
    private async Task StepPreviewedListAsync(int step)
    {
        var list = shell.State.PreviewedList;
        if (PreviewSubject.NextShown(list, PanelStatesNow(), step, shell.State.PreviewsChangedFile) is not { } row)
        {
            return;
        }

        await Navigation[list].SelectAsync(row);
        RenderOnTheLoop();
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

    private RowTone PreviewedDetailTone() =>
        shell.State.PreviewedList == PanelKind.Issues && SelectedIssue() is { } issue
            ? IssuePanelSnapshot.ToneFor(issue)
            : RowTone.Neutral;

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
            new PanelSearch(snapshot.Filters, snapshot.SearchQuery, snapshot.SearchHitCount),
            new PanelListing(
                snapshot.Nodes,
                () => TonedRows(snapshot.Rows),
                snapshot.SelectedIndex,
                snapshot.EmptyMessage),
            new PanelPosition(snapshot.SelectedIndex, snapshot.Nodes.Count));
        ShowSegmentsWhenActive(PanelKind.Explorer, snapshot.StatusSegments);
    }

    private void RenderTests()
    {
        var snapshot = TestPanelSnapshot.From(session.State, target, sinceLoadStarted.Elapsed);
        RenderPanel(
            PanelKind.Tests,
            new PanelSearch(snapshot.Filters, snapshot.SearchQuery, snapshot.SearchHitCount),
            new PanelListing(
                snapshot.Tests,
                () => [.. snapshot.TestRows.Zip(snapshot.Tests, TestRow)],
                snapshot.SelectedIndex,
                snapshot.EmptyMessage),
            new PanelPosition(snapshot.SelectedIndex, snapshot.Tests.Count));
        if (shell.State.ActivePanel != PanelKind.Tests)
        {
            testStatus!.Visible = false;
            return;
        }

        segments.Hide();
        testStatus!.Visible = true;
        testStatus.Text = snapshot.StatusLine;
    }

    private static IReadOnlyList<ListRow> TonedRows(IReadOnlyList<PanelRow> rows) =>
        [.. rows.Select(row => ListRow.Toned(row.Text, row.Tone))];

    private static ListRow TestRow(string text, VisibleTestNode node) =>
        new(text, TestRowAppearance.ForegroundFor(node.Outcome, node.Update));

    private void RenderChanges()
    {
        var snapshot = ChangesetPanelSnapshot.From(changesetSession.State);
        RenderPanel(
            PanelKind.Changes,
            new PanelSearch([], snapshot.SearchQuery, snapshot.SearchHitCount),
            new PanelListing(
                snapshot.Files,
                () => TonedRows(snapshot.Rows),
                snapshot.SelectedIndex,
                snapshot.EmptyMessage),
            new PanelPosition(snapshot.SelectedIndex, snapshot.Files.Count));
        ShowSegmentsWhenActive(PanelKind.Changes, snapshot.StatusSegments);
    }

    private void RenderIssues()
    {
        var snapshot = IssuePanelSnapshot.From(issueSession.State);
        var layout = IssuePanelLayout.From(snapshot, lists[PanelKind.Issues].View.Viewport.Width);
        RenderPanel(
            PanelKind.Issues,
            new PanelSearch(snapshot.Filters, snapshot.SearchQuery, snapshot.Issues.Count),
            new PanelListing(
                layout.Rows,
                () => TonedRows(layout.Rows),
                layout.SelectedRowIndex,
                snapshot.EmptyMessage),
            new PanelPosition(snapshot.SelectedIndex, snapshot.Issues.Count));
        ShowSegmentsWhenActive(PanelKind.Issues, snapshot.StatusSegments);
    }

    private void RenderComments()
    {
        var snapshot = CommentPanelSnapshot.From(commentSession.State);
        RenderPanel(
            PanelKind.Comments,
            new PanelSearch([], snapshot.SearchQuery, snapshot.SearchHitCount),
            new PanelListing(
                snapshot.Comments,
                () => TonedRows(snapshot.Rows),
                snapshot.SelectedIndex,
                snapshot.EmptyMessage),
            new PanelPosition(snapshot.SelectedIndex, snapshot.Comments.Count));
        ShowSegmentsWhenActive(PanelKind.Comments, snapshot.StatusSegments);
    }

    /// <summary>Every panel lists its rows, but only the one taking the keys
    /// fills the search box beneath them.</summary>
    private void RenderPanel(PanelKind panel, PanelSearch searched, PanelListing listing, PanelPosition position)
    {
        var active = shell.State.ActivePanel == panel;
        lists[panel].Show(
            PanelTitle.Segments(panel, searched.Filters, searched.Query, active),
            PanelTitle.Footer(position.Selected, position.Count),
            listing);
        if (!active)
        {
            return;
        }

        search.Title = SearchBox.Title(searched.Query, searched.HitCount);
        search.Text = searched.Query;
        filters.Show(searched.Filters);
    }

    /// <summary>What a panel is searched for and filtered to, which the
    /// search box beneath the panels shows while the panel takes the keys.
    /// </summary>
    private sealed record PanelSearch(IReadOnlyList<FilterChip> Filters, string Query, int HitCount);

    /// <summary>Where the selection stands among what the panel lists. The
    /// issues wrap across several rows each, so they count issues, not rows.
    /// </summary>
    private sealed record PanelPosition(int Selected, int Count);

    private void ShowSegmentsWhenActive(PanelKind panel, IReadOnlyList<StatusSegment> reported)
    {
        if (shell.State.ActivePanel == panel)
        {
            segments.Show(reported);
        }
    }

    private void RequestOpen(IApplication application, string path, int line)
    {
        openPath = path;
        openLine = line;
        openSourceRequested = true;
        application.RequestStop();
    }

    private static bool RunsTests(ExplorerCommand command) => command is
        ExplorerCommand.RunSelected or
        ExplorerCommand.RerunLast or
        ExplorerCommand.RerunFailed;
}
