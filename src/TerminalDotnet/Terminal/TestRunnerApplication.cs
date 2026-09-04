using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TextMateSharp.Grammars;

namespace TerminalDotnet.Terminal;

public sealed class TestRunnerApplication(
    TestExplorerSession session,
    FileExplorerSession fileSession,
    string target,
    IFileOpener? editorLauncher = null)
{
    private const int ContentInset = 1;
    private const int PanelWidth = 20;
    private const int WorkspaceX = ContentInset + PanelWidth + 1;
    private const int SegmentGap = 2;
    private const int SearchGap = 1;

    private CancellationTokenSource? runCancellation;
    private IReadOnlyList<VisibleTestNode> testNodes = [];
    private IReadOnlyList<FilePanelRow> fileRows = [];
    private readonly PanelShell shell = new();
    private bool openSourceRequested;
    private string? openPath;
    private int openLine = 1;
    private bool failureNavigationPending;
    private bool previewVisible;
    private Label? testStatus;
    private IReadOnlyList<Label> fileStatus = [];
    private IReadOnlyList<FileStatusSegment> fileStatusSegments = [];
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
        application.Init();

        using var window = new Window { Title = "terminal-dotnet" };
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
        fileStatus = FileStatus();
        shortcuts = Shortcuts();

        window.Add(panels, search, tests, testStatus, shortcuts);
        window.Add([.. fileStatus]);
        search.ValueChanged += async (_, _) =>
        {
            if (shell.State.ActivePanel == PanelKind.Explorer)
            {
                await fileSession.DispatchAsync(new FileExplorerCommand.Search(search.Text));
            }
            else
            {
                await session.DispatchAsync(new ExplorerCommand.Search(search.Text));
            }
            Render(search, tests);
        };
        application.Keyboard.KeyDown += (_, key) =>
            HandleKey(application, key, panels, search, tests);
        Render(search, tests);
        panels.SelectedItem = shell.State.ActivePanel == PanelKind.Explorer ? 0 : 1;
        tests.SetFocus();

        application.Run(window);
        runCancellation?.Cancel();
        runCancellation?.Dispose();
        runCancellation = null;
        return openSourceRequested;
    }

    private static ListView Panels()
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
        panels.SetSource(new ObservableCollection<string>(["Explorer", "Tests"]));
        panels.SelectedItem = 0;
        return panels;
    }

    private static Label TestStatus() => new()
    {
        X = WorkspaceX,
        Y = Pos.AnchorEnd(2),
        Width = Dim.Fill(ContentInset),
        Height = 1
    };

    private IReadOnlyList<Label> FileStatus() => FilePanelSnapshot.From(fileSession.State)
        .StatusSegments
        .Select((_, index) => FileStatusSegment(index))
        .ToArray();

    private Label FileStatusSegment(int index)
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

    private FileRowTone ToneFor(int index) => index < fileStatusSegments.Count
        ? fileStatusSegments[index].Tone
        : FileRowTone.Neutral;

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
            Y = Pos.Bottom(search) + SearchGap,
            Width = Dim.Fill(ContentInset),
            Height = Dim.Fill(3),
            ShowMarks = false,
            KeystrokeNavigator = null
        };
        tests.RowRender += (_, args) => ColorTreeRow(tests, args);
        return tests;
    }

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

        if (search.HasFocus && Is(key, KeyCode.Esc))
        {
            key.Handled = true;
            search.Text = "";
            if (shell.State.ActivePanel == PanelKind.Explorer)
            {
                _ = DispatchFileAsync(new FileExplorerCommand.ClearSearch(), search, tests);
            }
            else
            {
                _ = DispatchAsync(application, new ExplorerCommand.ClearSearch(), search, tests);
            }
            tests.SetFocus();
            return;
        }

        if (search.HasFocus && Is(key, KeyCode.Enter))
        {
            key.Handled = true;
            tests.SetFocus();
            return;
        }

        if (search.HasFocus)
        {
            return;
        }

        if (Is(key, KeyCode.Q) || Is(key, KeyCode.Esc))
        {
            key.Handled = true;
            application.RequestStop();
            return;
        }

        if (Is(key, KeyCode.S))
        {
            key.Handled = true;
            search.SetFocus();
            return;
        }

        if (panels.HasFocus && Is(key, KeyCode.CursorRight))
        {
            key.Handled = true;
            tests.SetFocus();
            return;
        }

        if (!panels.HasFocus && Is(key, KeyCode.CursorLeft))
        {
            key.Handled = true;
            panels.SetFocus();
            return;
        }

        if (panels.HasFocus && Is(key, KeyCode.Enter))
        {
            key.Handled = true;
            shell.Select(panels.SelectedItem ?? 0);
            Render(search, tests);
            tests.SetFocus();
            return;
        }

        if (shell.State.ActivePanel == PanelKind.Explorer)
        {
            HandleFileKey(application, key, tests, search);
            return;
        }

        if (tests.HasFocus && session.State.SearchQuery.Length > 0 && Is(key, KeyCode.N))
        {
            key.Handled = true;
            var searchCommand = key.IsShift
                ? (ExplorerCommand)new ExplorerCommand.PreviousSearchMatch()
                : new ExplorerCommand.NextSearchMatch();
            HandleCommand(application, new TerminalCommand(searchCommand), search, tests);
            return;
        }

        if (tests.HasFocus && failureNavigationPending)
        {
            failureNavigationPending = false;
            if (Is(key, KeyCode.F))
            {
                key.Handled = true;
                HandleCommand(
                    application,
                    new TerminalCommand(new ExplorerCommand.NextFailure()),
                    search,
                    tests);
                return;
            }
        }

        if (tests.HasFocus && Is(key, (KeyCode)']'))
        {
            key.Handled = true;
            failureNavigationPending = true;
            return;
        }

        if (Is(key, KeyCode.E))
        {
            key.Handled = true;
            RequestTestSource(application, preview: false);
            return;
        }

        if (Is(key, KeyCode.P))
        {
            key.Handled = true;
            RequestTestSource(application, preview: true);
            return;
        }

        if (Is(key, KeyCode.O))
        {
            key.Handled = true;
            ShowTestOutput(application);
            return;
        }

        var command = tests.HasFocus ? CommandFor(key) : null;
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        HandleCommand(application, command, search, tests);
    }

    private void HandleCommand(
        IApplication application,
        TerminalCommand command,
        TextField search,
        ListView tests)
    {
        if (command is CancelRun)
        {
            runCancellation?.Cancel();
            return;
        }

        _ = DispatchAsync(application, command.ExplorerCommand!, search, tests);
    }

    private void HandleFileKey(
        IApplication application,
        Key key,
        ListView files,
        TextField search)
    {
        if (!files.HasFocus || fileSession.State.VisibleNodes.Count == 0)
        {
            return;
        }

        var selected = fileSession.State.VisibleNodes[fileSession.State.SelectedIndex];
        var action = FilePanelKeyBindings.ActionFor(key, selected, search.HasFocus);
        if (action is FilePanelAction.OpenFile open)
        {
            key.Handled = true;
            openPath = open.Path;
            openLine = 1;
            openSourceRequested = true;
            application.RequestStop();
            return;
        }

        if (action is FilePanelAction.PreviewFile preview)
        {
            key.Handled = true;
            ShowPreview(application, preview.Path, 1);
            return;
        }

        FileExplorerCommand? command = null;
        if (fileSession.State.SearchQuery.Length > 0 && Is(key, KeyCode.N))
        {
            command = key.IsShift
                ? new FileExplorerCommand.PreviousSearchMatch()
                : new FileExplorerCommand.NextSearchMatch();
        }
        else if (Is(key, KeyCode.CursorUp) || Is(key, KeyCode.K))
        {
            command = new FileExplorerCommand.MoveUp();
        }
        else if (Is(key, KeyCode.CursorDown) || Is(key, KeyCode.J))
        {
            command = new FileExplorerCommand.MoveDown();
        }
        else if (Is(key, KeyCode.Space) || Is(key, KeyCode.Enter))
        {
            command = new FileExplorerCommand.ToggleExpanded();
        }

        if (command is null)
        {
            return;
        }

        key.Handled = true;
        _ = DispatchFileAsync(command, search, files);
    }

    private async Task DispatchFileAsync(
        FileExplorerCommand command,
        TextField search,
        ListView files)
    {
        await fileSession.DispatchAsync(command);
        Render(search, files);
    }

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
        if (!preview && editorLauncher is null)
        {
            return;
        }

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

            openPath = source.Path;
            openLine = source.HighlightLine;
            openSourceRequested = true;
            application.RequestStop();
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
        using var dialog = new Window
        {
            Title = $"{snapshot.SelectedOutputTitle} — ↑/↓ scroll  Esc close",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ShadowStyle = ShadowStyles.None
        };
        var text = new TestOutputView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        text.Load(AnsiTestOutput.ToCells(snapshot.SelectedOutput));
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
        new ExplorerEditorWorkflow(fileSession, editorLauncher!, target).OpenAsync(
            openPath!,
            openLine).GetAwaiter().GetResult();
    }

    private void Render(TextField search, ListView tests)
    {
        shortcuts!.Text = PanelShortcuts.For(shell.State.ActivePanel, fileSession.State, session.State);
        if (shell.State.ActivePanel == PanelKind.Explorer)
        {
            RenderFiles(search, tests);
            return;
        }

        var snapshot = TestPanelSnapshot.From(session.State, target);
        tests.Title = $"Tests — {snapshot.Breadcrumb}";
        tests.Height = Dim.Fill(3);
        HideFileStatus();
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
        search.Title = snapshot.SearchQuery.Length == 0
            ? "Search"
            : $"Search — {snapshot.SearchHitCount} hits";
        search.Text = snapshot.SearchQuery;
        fileRows = snapshot.Rows;
        files.SetSource(new ObservableCollection<string>(snapshot.Rows.Select(row => row.Text)));
        files.Height = Dim.Fill(3);
        testStatus!.Visible = false;
        ShowFileStatus(snapshot.StatusSegments);
        if (snapshot.Nodes.Count > 0)
        {
            files.SelectedItem = snapshot.SelectedIndex;
        }
    }

    private void ShowFileStatus(IReadOnlyList<FileStatusSegment> segments)
    {
        fileStatusSegments = segments;
        var column = WorkspaceX;
        foreach (var (label, segment) in fileStatus.Zip(segments))
        {
            label.X = column;
            label.Width = segment.Text.Length;
            label.Text = segment.Text;
            label.Visible = true;
            column += segment.Text.Length + SegmentGap;
        }
    }

    private void HideFileStatus()
    {
        foreach (var label in fileStatus)
        {
            label.Visible = false;
        }
    }

    private void ColorTreeRow(ListView tree, ListViewRowEventArgs args)
    {
        if (shell.State.ActivePanel == PanelKind.Explorer)
        {
            ColorFileRow(tree, args);
            return;
        }

        ColorTestRow(tree, args);
    }

    private void ColorFileRow(ListView files, ListViewRowEventArgs args)
    {
        if (args.Row >= fileRows.Count)
        {
            return;
        }

        args.RowAttribute = FileRowAppearance.For(
            fileRows[args.Row].Tone,
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

        var foreground = testNodes[args.Row].Outcome switch
        {
            TestNodeOutcome.Failed => Color.BrightRed,
            TestNodeOutcome.Passed => Color.BrightGreen,
            TestNodeOutcome.Skipped => Color.BrightYellow,
            TestNodeOutcome.Running => Color.BrightCyan,
            _ => Color.White
        };
        SetRowForeground(tests, args, foreground);
    }

    private static void SetRowForeground(
        ListView list,
        ListViewRowEventArgs args,
        Color foreground)
    {
        var background = list.GetAttributeForRole(VisualRole.Normal).Background;
        args.RowAttribute = new global::Terminal.Gui.Drawing.Attribute(foreground, background);
    }

    private static TerminalCommand? CommandFor(Key key)
    {
        if (Is(key, KeyCode.CursorUp) || Is(key, KeyCode.K))
        {
            return new TerminalCommand(new ExplorerCommand.MoveUp());
        }

        if (Is(key, KeyCode.CursorDown) || Is(key, KeyCode.J))
        {
            return new TerminalCommand(new ExplorerCommand.MoveDown());
        }

        if (Is(key, KeyCode.Space))
        {
            return new TerminalCommand(new ExplorerCommand.ToggleExpanded());
        }

        if (Is(key, KeyCode.Enter) || Is(key, KeyCode.R) && !key.IsShift)
        {
            return new TerminalCommand(new ExplorerCommand.RunSelected());
        }

        if (Is(key, KeyCode.R) && key.IsShift)
        {
            return new TerminalCommand(new ExplorerCommand.RerunLast());
        }

        if (Is(key, KeyCode.F) && key.IsShift)
        {
            return new TerminalCommand(new ExplorerCommand.RerunFailed());
        }

        if (Is(key, KeyCode.C))
        {
            return new CancelRun();
        }

        return null;
    }

    private static bool RunsTests(ExplorerCommand command) => command is
        ExplorerCommand.RunSelected or
        ExplorerCommand.RerunLast or
        ExplorerCommand.RerunFailed;

    private static bool Is(Key key, KeyCode keyCode) => key.NoShift.KeyCode == keyCode;

    private record TerminalCommand(ExplorerCommand? ExplorerCommand = null);

    private sealed record CancelRun : TerminalCommand;
}
