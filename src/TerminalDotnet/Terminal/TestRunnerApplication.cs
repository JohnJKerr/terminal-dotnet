using System.Collections.ObjectModel;
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

public sealed class TestRunnerApplication(
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
    private const string ConsoleDriver = "dotnet";

    private CancellationTokenSource? runCancellation;
    private IReadOnlyList<VisibleTestNode> testNodes = [];
    private IReadOnlyList<FileRowTone> rowTones = [];
    private readonly PanelShell shell = new();
    private bool openSourceRequested;
    private string? openPath;
    private int openLine = 1;
    private bool failureNavigationPending;
    private bool previewVisible;
    private Label? testStatus;
    private IReadOnlyList<Label> segmentLabels = [];
    private IReadOnlyList<FileStatusSegment> statusSegments = [];
    private IReadOnlyList<Label> filterLabels = [];
    private IReadOnlyList<FilterChip> filterChips = [];
    private Label? emptyState;
    private Label? shortcuts;

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
        panels.SelectedItem = shell.State.ActiveIndex;
        tests.SetFocus();

        application.Run(window);
        runCancellation?.Cancel();
        runCancellation?.Dispose();
        runCancellation = null;
        return openSourceRequested;
    }

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
        panels.SetSource(new ObservableCollection<string>(shell.State.Panels));
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
        Y = Pos.AnchorEnd(2),
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
            Y = Pos.AnchorEnd(2),
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
            Height = Dim.Fill(3),
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

    private static Label Shortcuts() => new()
    {
        X = 1,
        Y = Pos.AnchorEnd(1),
        Width = Dim.Fill(1),
        Height = 1
    };

    private void HandleKey(
        IApplication application,
        Key key,
        ListView panels,
        TextField search,
        ListView tests)
    {
        if (previewVisible)
        {
            return;
        }

        var shellAction = ShellKeyBindings.ActionFor(key, search.HasFocus, panels.HasFocus);
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
                _ = ClearSearchAsync(application, search, tests);
                tests.SetFocus();
                return;
            case ShellAction.LeaveSearch:
            case ShellAction.FocusRows:
                tests.SetFocus();
                return;
            case ShellAction.FocusSearch:
                search.SetFocus();
                return;
            case ShellAction.FocusPanels:
                panels.SetFocus();
                return;
            case ShellAction.SelectPanel:
                shell.Select(panels.SelectedItem ?? 0);
                Render(search, tests);
                tests.SetFocus();
                return;
            case ShellAction.Quit:
                application.RequestStop();
                return;
        }
    }

    private void HandleTestKey(
        IApplication application,
        Key key,
        TextField search,
        ListView tests)
    {
        var awaitingFailureNavigation = failureNavigationPending;
        if (tests.HasFocus && awaitingFailureNavigation)
        {
            failureNavigationPending = false;
        }

        var action = TestPanelKeyBindings.ActionFor(
            key,
            session.State.SearchQuery,
            tests.HasFocus,
            awaitingFailureNavigation);
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
            case TestPanelAction.AwaitFailureNavigation:
                failureNavigationPending = true;
                return;
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
                _ = DispatchAsync(application, dispatch.Command, search, tests);
                return;
        }
    }

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
            _ = DispatchFileAsync(new FileExplorerCommand.ToggleFilter(toggle.Filter), search, files);
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
        _ = DispatchFileAsync(command, search, files);
    }

    private VisibleFileNode? SelectedFile() => fileSession.State.VisibleNodes.Count == 0
        ? null
        : fileSession.State.VisibleNodes[fileSession.State.SelectedIndex];

    private static FileExplorerCommand? FileCommandFor(Key key, string searchQuery)
    {
        if (searchQuery.Length > 0 && Is(key, KeyCode.N))
        {
            return key.IsShift
                ? new FileExplorerCommand.PreviousSearchMatch()
                : new FileExplorerCommand.NextSearchMatch();
        }

        if (Is(key, KeyCode.CursorUp) || Is(key, KeyCode.K))
        {
            return new FileExplorerCommand.MoveUp();
        }

        if (Is(key, KeyCode.CursorDown) || Is(key, KeyCode.J))
        {
            return new FileExplorerCommand.MoveDown();
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
            _ = RestoreSelectedAsync(application, search, files);
            return;
        }

        var command = ChangesetCommandFor(key, changesetSession.State.SearchQuery);
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        _ = DispatchChangesetAsync(command, search, files);
    }

    private static ChangesetCommand? ChangesetCommandFor(Key key, string searchQuery)
    {
        if (searchQuery.Length > 0 && Is(key, KeyCode.N))
        {
            return key.IsShift ? new ChangesetCommand.MoveUp() : new ChangesetCommand.MoveDown();
        }

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

    private void ShowDiff(IApplication application) => _ = ShowDiffAsync(application);

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

        runCancellation?.Dispose();
        runCancellation = new CancellationTokenSource();
        var run = session.DispatchAsync(command, runCancellation.Token);
        Render(search, tests);
        await run;
        application.Invoke(() => Render(search, tests));
    }

    private void RequestTestSource(IApplication application, bool preview)
    {
        _ = RequestTestSourceAsync(application, preview);
    }

    private async Task RequestTestSourceAsync(IApplication application, bool preview)
    {
        await session.DispatchAsync(new ExplorerCommand.LoadSelectedSource());
        if (session.State.SourceContext is not { } source)
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
        var snapshot = TestPanelSnapshot.From(session.State, target);
        ShowCellDialog(
            application,
            $"{snapshot.SelectedOutputTitle} — ↑/↓ scroll  Esc close",
            AnsiTestOutput.ToCells(snapshot.SelectedOutput),
            wordWrap: true);
    }

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
        shortcuts!.Text = PanelShortcuts.For(
            shell.State.ActivePanel,
            fileSession.State,
            changesetSession.State,
            session.State);
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
        tests.Height = Dim.Fill(3);
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
        tests.SetSource(new ObservableCollection<string>(snapshot.TestRows));
        if (snapshot.Tests.Count > 0)
        {
            tests.SelectedItem = snapshot.SelectedIndex;
        }
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
            snapshot.Rows.Select(row => (row.Text, row.Tone)).ToArray(),
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
            snapshot.Rows.Select(row => (row.Text, row.Tone)).ToArray(),
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
        IReadOnlyList<(string Text, FileRowTone Tone)> rows,
        int selectedIndex,
        IReadOnlyList<FileStatusSegment> segments,
        IReadOnlyList<FilterChip> filters,
        string emptyMessage)
    {
        search.Title = searchQuery.Length == 0 ? "Search" : $"Search — {searchHitCount} hits";
        search.Text = searchQuery;
        rowTones = rows.Select(row => row.Tone).ToArray();
        files.SetSource(new ObservableCollection<string>(rows.Select(row => row.Text)));
        files.Height = Dim.Fill(3);
        testStatus!.Visible = false;
        ShowSegments(segments);
        ShowFilters(filters);
        ShowEmptyState(emptyMessage);
        if (rows.Count > 0)
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

    private static bool Is(Key key, KeyCode keyCode) => key.NoShift.KeyCode == keyCode;
}
