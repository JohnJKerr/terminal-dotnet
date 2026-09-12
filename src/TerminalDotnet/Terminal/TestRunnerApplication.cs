using System.Collections.ObjectModel;
using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Filters;
using TextMateSharp.Grammars;

namespace TerminalDotnet.Terminal;

internal sealed class TestRunnerApplication(
    TestExplorerSession session,
    FileExplorerSession fileSession,
    ChangesetSession changesetSession,
    string target,
    IFileOpener? editorLauncher = null)
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
    private const string ConsoleDriver = "dotnet";
    private static readonly TimeSpan SettleDuration = TimeSpan.FromMilliseconds(500);

    private CancellationTokenSource? runCancellation;
    private CancellationTokenSource? loadCancellation;
    private readonly Stopwatch sinceLoadStarted = new();
    private readonly Stopwatch sincePanelsAppeared = new();
    private IReadOnlyList<VisibleTestNode> testNodes = [];
    private IReadOnlyList<FileRowTone> rowTones = [];
    private object? listedContent;
    private readonly PanelShell shell = new();
    private readonly BackgroundWork panelWork = new();
    private bool openSourceRequested;
    private string? openPath;
    private int openLine = 1;
    private bool previewVisible;
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
        while (RunTerminal())
        {
            OpenRequestedFile();
        }
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
        TurnActivityMarker(application);
        panelWork.Track(FillPanelsAsync(application, search, tests, loadCancellation.Token));
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

            if (shell.State.ActivePanel != PanelKind.Tests)
            {
                return true;
            }

            ShowEmptyState(ActivityMarker.Marking(session.State.Message, sinceLoadStarted.Elapsed));
            application.LayoutAndDraw(true);
            return true;
        });

    private Task FillPanelsAsync(
        IApplication application,
        TextField search,
        ListView tests,
        CancellationToken cancellationToken) =>
        new PanelStartup(fileSession, changesetSession, session, target).LoadPendingAsync(
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

    private static string TerminalDriver() =>
        Environment.GetEnvironmentVariable("TERMINAL_DOTNET_DRIVER") is { Length: > 0 } driver
            ? driver
            : ConsoleDriver;

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

        if (previewVisible)
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

        if (shell.State.ActivePanel == PanelKind.Explorer)
        {
            HandleFileKey(application, key, tests, search);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Changes)
        {
            HandleChangesetKey(application, key, tests, search);
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
                shell.Select(panels.SelectedItem ?? 0);
                ShowActivePanel(panels, search, tests);
                return;
            case ShellAction.SelectPanel selected:
                shell.Select((int)selected.Panel);
                ShowActivePanel(panels, search, tests);
                return;
            case ShellAction.ShowCommands:
                ShowCommands(application);
                return;
            case ShellAction.Dismiss:
                return;
            case ShellAction.Quit:
                application.RequestStop();
                return;
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
                RequestTestSource(application, preview: false);
                return;
            case TestPanelAction.PreviewSource:
                RequestTestSource(application, preview: true);
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

    private string ActiveSearchQuery() => shell.State.ActivePanel switch
    {
        PanelKind.Explorer => fileSession.State.SearchQuery,
        PanelKind.Changes => changesetSession.State.SearchQuery,
        _ => session.State.SearchQuery
    };

    private Task SearchAsync(string query) => shell.State.ActivePanel switch
    {
        PanelKind.Explorer => fileSession.DispatchAsync(new FileExplorerCommand.Search(query)),
        PanelKind.Changes => changesetSession.DispatchAsync(new ChangesetCommand.Search(query)),
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

    private Task ClearPanelSearchAsync() => shell.State.ActivePanel == PanelKind.Changes
        ? changesetSession.DispatchAsync(new ChangesetCommand.ClearSearch())
        : fileSession.DispatchAsync(new FileExplorerCommand.ClearSearch());


    private void HandleFileKey(
        IApplication application,
        Key key,
        ListView files,
        TextField search)
    {
        if (!files.HasFocus)
        {
            return;
        }

        var action = FilePanelKeyBindings.ActionFor(key, SelectedFile(), search.HasFocus);
        if (action is FilePanelAction.ToggleFilter toggle)
        {
            key.Handled = true;
            panelWork.Track(
                DispatchFileAsync(new FileExplorerCommand.ToggleFilter(toggle.Filter), search, files));
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
            ShowPreview(application, preview.Path, 1);
            return;
        }

        var command = FileCommandFor(key, fileSession.State.SearchQuery);
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        panelWork.Track(DispatchFileAsync(command, search, files));
    }

    private VisibleFileNode? SelectedFile() => fileSession.State.VisibleNodes.Count == 0
        ? null
        : fileSession.State.VisibleNodes[fileSession.State.SelectedIndex];

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
        FileExplorerCommand command,
        TextField search,
        ListView files)
    {
        await fileSession.DispatchAsync(command);
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
            ShowDiff(application);
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
            ShowPreview(application, preview.Path, 1);
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

    private void ShowDiff(IApplication application) => panelWork.Track(ShowDiffAsync(application));

    private async Task ShowDiffAsync(IApplication application)
    {
        await changesetSession.DispatchAsync(new ChangesetCommand.LoadSelectedDiff());
        var snapshot = ChangesetPanelSnapshot.From(changesetSession.State);
        application.Invoke(() => ShowCellDialog(
            application,
            $"Diff — {snapshot.DiffTitle} — ↑/k up  ↓/j down  Esc close",
            DiffCells(snapshot.DiffLines),
            wordWrap: false));
    }

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

    private void RequestTestSource(IApplication application, bool preview)
    {
        panelWork.Track(RequestTestSourceAsync(application, preview));
    }

    private async Task RequestTestSourceAsync(IApplication application, bool preview)
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
                ShowPreview(application, source.Path, source.HighlightLine);
                return;
            }

            RequestOpen(application, source.Path, source.HighlightLine);
        });
    }

    private void ShowPreview(IApplication application, string path, int line)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.ErrorQuery(application, "Preview", exception.Message, "Ok");
            return;
        }

        using var preview = new Window
        {
            Title = $"Preview — {Path.GetFileName(path)}:{line} — ↑/k up  ↓/j down  Esc close",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ShadowStyle = ShadowStyles.None
        };
        var code = new Code
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = text,
            Language = LanguageFrom(path),
            SyntaxHighlighter = new TextMateSyntaxHighlighter(ThemeName.DarkPlus)
        };
        code.GettingAttributeForRole += (_, args) =>
        {
            var background = args.Result?.Background ?? Color.Black;
            args.Result = new global::Terminal.Gui.Drawing.Attribute(
                PreviewCodeAppearance.ForegroundFor(args.Role),
                background);
            args.Handled = true;
        };
        code.KeyDown += (_, key) => ScrollPreview(code, key);
        preview.Add(code);
        previewVisible = true;
        try
        {
            application.Run(preview);
        }
        finally
        {
            previewVisible = false;
        }
    }

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
        using var dialog = new Window
        {
            Title = title,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ShadowStyle = ShadowStyles.None
        };
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
        previewVisible = true;
        try
        {
            application.Run(dialog);
        }
        finally
        {
            previewVisible = false;
        }
    }

    private static void SetBlackBackground(View view)
    {
        view.GettingAttributeForRole += (_, args) =>
        {
            var foreground = args.Result?.Foreground ?? Color.White;
            args.Result = new global::Terminal.Gui.Drawing.Attribute(foreground, Color.Black);
            args.Handled = true;
        };
    }

    private static void ScrollPreview(Code code, Key key)
    {
        var rows = key.NoShift.KeyCode switch
        {
            KeyCode.CursorUp or KeyCode.K => -1,
            KeyCode.CursorDown or KeyCode.J => 1,
            KeyCode.PageUp => -Math.Max(1, code.Viewport.Height - 1),
            KeyCode.PageDown => Math.Max(1, code.Viewport.Height - 1),
            _ => 0
        };
        if (rows == 0)
        {
            return;
        }

        code.ScrollVertical(rows);
        key.Handled = true;
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

    private void OpenRequestedFile()
    {
        if (editorLauncher is null || openPath is null)
        {
            return;
        }

        new ExplorerEditorWorkflow(fileSession, changesetSession, editorLauncher, target)
            .OpenAsync(openPath, openLine)
            .GetAwaiter()
            .GetResult();
    }

    private void Render(TextField search, ListView tests)
    {
        shortcutSegments = PanelShortcuts.For(
            shell.State.ActivePanel,
            fileSession.State,
            changesetSession.State,
            session.State,
            search.HasFocus);
        ShowShortcuts();
        if (shell.State.ActivePanel == PanelKind.Explorer)
        {
            RenderFiles(search, tests);
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Changes)
        {
            RenderChanges(search, tests);
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

    private void RenderFiles(TextField search, ListView files)
    {
        var snapshot = FilePanelSnapshot.From(fileSession.State);
        files.Title = "Explorer";
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
