using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TerminalDotnet.Explorer;

namespace TerminalDotnet.Terminal;

public sealed class TestRunnerApplication(
    TestExplorerSession session,
    string target,
    EditorLauncher? editorLauncher = null)
{
    private const int ContentInset = 1;
    private const int PanelWidth = 20;
    private const int WorkspaceX = ContentInset + PanelWidth + 1;

    private CancellationTokenSource? runCancellation;
    private IReadOnlyList<OutputLine> outputLines = [];
    private IReadOnlyList<OutputLine> resultLines = [];
    private IReadOnlyList<VisibleTestNode> testNodes = [];
    private bool openSourceRequested;
    private bool failureNavigationPending;

    public void Run()
    {
        while (RunTerminal())
        {
            OpenSource();
        }
    }

    private bool RunTerminal()
    {
        openSourceRequested = false;
        using IApplication application = Application.Create();
        application.Init();

        using var window = new Window { Title = "terminal-dotnet" };
        var panels = Panels();
        var search = Search();
        var tests = Tests(search);
        var result = Result(tests);
        var output = Output(result);
        var shortcuts = Shortcuts();

        window.Add(panels, search, tests, result, output, shortcuts);
        search.ValueChanged += async (_, _) =>
        {
            await session.DispatchAsync(new ExplorerCommand.Search(search.Text));
            Render(search, tests, result, output);
        };
        application.Keyboard.KeyDown += (_, key) =>
            HandleKey(application, key, panels, search, tests, result, output);
        Render(search, tests, result, output);
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
        panels.SetSource(new ObservableCollection<string>(["Tests"]));
        panels.SelectedItem = 0;
        return panels;
    }

    private static TextField Search() => new()
    {
        Title = "Search",
        X = WorkspaceX,
        Y = ContentInset,
        Width = Dim.Fill(ContentInset),
        Height = 1
    };

    private ListView Tests(TextField search)
    {
        var tests = new ListView
        {
            Title = "Tests",
            X = WorkspaceX,
            Y = Pos.Bottom(search),
            Width = Dim.Fill(ContentInset),
            Height = Dim.Percent(45),
            ShowMarks = false,
            KeystrokeNavigator = null
        };
        tests.RowRender += (_, args) => ColorTestRow(tests, args);
        return tests;
    }

    private ListView Result(ListView tests)
    {
        var result = new ListView
        {
            Title = "Test Result",
            X = WorkspaceX,
            Y = Pos.Bottom(tests),
            Width = Dim.Fill(ContentInset),
            Height = Dim.Percent(25),
            ShowMarks = false,
            KeystrokeNavigator = null
        };
        result.RowRender += (_, args) => ColorLine(result, resultLines, args);
        return result;
    }

    private ListView Output(ListView result)
    {
        var output = new ListView
        {
            Title = "Execution Output",
            X = WorkspaceX,
            Y = Pos.Bottom(result),
            Width = Dim.Fill(ContentInset),
            Height = Dim.Fill(2),
            ShowMarks = false,
            KeystrokeNavigator = null
        };
        output.RowRender += (_, args) => ColorOutputRow(output, args);
        return output;
    }

    private static Label Shortcuts() => new()
    {
        X = 1,
        Y = Pos.AnchorEnd(1),
        Width = Dim.Fill(1),
        Height = 1,
        Text = "Tab pane  / search  ↑/k up  ↓/j down  Space fold  ]f next failure  Enter/r run  R rerun  F failures  c cancel  o source  q quit"
    };

    private void HandleKey(
        IApplication application,
        Key key,
        ListView panels,
        TextField search,
        ListView tests,
        ListView result,
        ListView output)
    {
        if (search.HasFocus && Is(key, KeyCode.Esc))
        {
            key.Handled = true;
            search.Text = "";
            _ = DispatchAsync(application, new ExplorerCommand.ClearSearch(), search, tests, result, output);
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

        if (tests.HasFocus && Is(key, (KeyCode)'/'))
        {
            key.Handled = true;
            search.SetFocus();
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
                    tests,
                    result,
                    output);
                return;
            }
        }

        if (tests.HasFocus && Is(key, (KeyCode)']'))
        {
            key.Handled = true;
            failureNavigationPending = true;
            return;
        }

        if (Is(key, KeyCode.O))
        {
            key.Handled = true;
            RequestOpenSource(application);
            return;
        }

        if (panels.HasFocus && Is(key, KeyCode.Enter))
        {
            key.Handled = true;
            tests.SetFocus();
            return;
        }

        if (result.HasFocus && ScrollOutput(key, result) || output.HasFocus && ScrollOutput(key, output))
        {
            key.Handled = true;
            return;
        }

        var command = tests.HasFocus ? CommandFor(key) : null;
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        HandleCommand(application, command, search, tests, result, output);
    }

    private void HandleCommand(
        IApplication application,
        TerminalCommand command,
        TextField search,
        ListView tests,
        ListView result,
        ListView output)
    {
        if (command is CancelRun)
        {
            runCancellation?.Cancel();
            return;
        }

        _ = DispatchAsync(application, command.ExplorerCommand!, search, tests, result, output);
    }

    private async Task DispatchAsync(
        IApplication application,
        ExplorerCommand command,
        TextField search,
        ListView tests,
        ListView result,
        ListView output)
    {
        if (!RunsTests(command))
        {
            await session.DispatchAsync(command);
            Render(search, tests, result, output);
            return;
        }

        runCancellation?.Dispose();
        runCancellation = new CancellationTokenSource();
        var run = session.DispatchAsync(command, runCancellation.Token);
        Render(search, tests, result, output);
        await run;
        application.Invoke(() => Render(search, tests, result, output));
    }

    private void RequestOpenSource(IApplication application)
    {
        if (editorLauncher is null || session.State.SourceContext is null)
        {
            return;
        }

        openSourceRequested = true;
        application.RequestStop();
    }

    private void OpenSource()
    {
        var source = session.State.SourceContext!;
        editorLauncher!.OpenAsync(
            source.Path,
            source.HighlightLine).GetAwaiter().GetResult();
    }

    private void Render(TextField search, ListView tests, ListView result, ListView output)
    {
        var snapshot = TestPanelSnapshot.From(session.State, target);
        tests.Title = $"Tests — {snapshot.Target}";
        search.Title = snapshot.SearchQuery.Length == 0
            ? "Search"
            : $"Search — {snapshot.SearchHitCount} hits";
        search.Text = snapshot.SearchQuery;
        testNodes = snapshot.Tests;
        tests.SetSource(new ObservableCollection<string>(snapshot.TestRows));
        resultLines = snapshot.ResultLines;
        result.SetSource(new ObservableCollection<string>(snapshot.ResultLines.Select(line => line.Text)));
        outputLines = snapshot.OutputLines;
        output.SetSource(new ObservableCollection<string>(snapshot.OutputLines.Select(line => line.Text)));
        if (snapshot.Tests.Count > 0)
        {
            tests.SelectedItem = snapshot.SelectedIndex;
        }
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

    private void ColorOutputRow(ListView output, ListViewRowEventArgs args)
    {
        ColorLine(output, outputLines, args);
    }

    private static void ColorLine(
        ListView list,
        IReadOnlyList<OutputLine> lines,
        ListViewRowEventArgs args)
    {
        if (args.Row >= lines.Count || list.IsSelectedOrMarked(args.Row))
        {
            return;
        }

        var foreground = lines[args.Row].Tone switch
        {
            OutputLineTone.Failure => Color.BrightRed,
            OutputLineTone.Success => Color.BrightGreen,
            OutputLineTone.Skipped => Color.BrightYellow,
            OutputLineTone.Status => Color.BrightCyan,
            _ => Color.None
        };
        if (foreground == Color.None)
        {
            return;
        }

        SetRowForeground(list, args, foreground);
    }

    private static void SetRowForeground(
        ListView list,
        ListViewRowEventArgs args,
        Color foreground)
    {
        var background = list.GetAttributeForRole(VisualRole.Normal).Background;
        args.RowAttribute = new global::Terminal.Gui.Drawing.Attribute(foreground, background);
    }

    private static bool ScrollOutput(Key key, ListView output)
    {
        if (Is(key, KeyCode.K))
        {
            output.MoveUp(false);
            return true;
        }

        if (Is(key, KeyCode.J))
        {
            output.MoveDown(false);
            return true;
        }

        return false;
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
