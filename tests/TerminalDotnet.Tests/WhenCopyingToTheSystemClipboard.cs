using System.ComponentModel;
using TerminalDotnet.Comments;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenCopyingToTheSystemClipboard
{
    [Fact]
    public async Task It_hands_the_text_to_the_clipboard_tool()
    {
        // Arrange
        var runner = new RecordingCommandRunner();
        var clipboard = new CommandClipboard(runner, "/repo");

        // Act
        await clipboard.TryCopyAsync("needs a guard");

        // Assert
        Assert.Equal("needs a guard", runner.Requests[0].StandardInput);
    }

    [Fact]
    public async Task It_reaches_for_the_wayland_tool_first()
    {
        // Arrange
        var runner = new RecordingCommandRunner();
        var clipboard = new CommandClipboard(runner, "/repo");

        // Act
        await clipboard.TryCopyAsync("needs a guard");

        // Assert
        Assert.Equal("wl-copy", runner.Requests[0].FileName);
    }

    [Fact]
    public async Task It_says_the_text_was_copied_once_a_tool_takes_it()
    {
        // Arrange
        var clipboard = new CommandClipboard(new RecordingCommandRunner(), "/repo");

        // Act
        var copied = await clipboard.TryCopyAsync("needs a guard");

        // Assert
        Assert.True(copied);
    }

    [Fact]
    public async Task It_tries_the_next_tool_when_the_first_is_not_installed()
    {
        // Arrange
        var runner = new RecordingCommandRunner
        {
            Installed = ["xclip", "xsel", "pbcopy"],
            Takes = "xclip"
        };
        var clipboard = new CommandClipboard(runner, "/repo");

        // Act
        await clipboard.TryCopyAsync("needs a guard");

        // Assert
        Assert.Equal(["wl-copy", "xclip"], runner.Requests.Select(request => request.FileName));
    }

    [Fact]
    public async Task It_tries_the_next_tool_when_one_runs_but_fails()
    {
        // Arrange
        var runner = new RecordingCommandRunner { Takes = "xclip" };
        var clipboard = new CommandClipboard(runner, "/repo");

        // Act
        await clipboard.TryCopyAsync("needs a guard");

        // Assert
        Assert.Equal(["wl-copy", "xclip"], runner.Requests.Select(request => request.FileName));
    }

    [Fact]
    public async Task It_reaches_the_windows_clipboard_when_no_other_tool_is_installed()
    {
        // Arrange
        var runner = new RecordingCommandRunner { Installed = ["clip"], Takes = "clip" };
        var clipboard = new CommandClipboard(runner, "/repo");

        // Act
        var copied = await clipboard.TryCopyAsync("needs a guard");

        // Assert
        Assert.True(copied);
    }

    [Fact]
    public async Task It_stops_at_the_first_tool_that_takes_the_text()
    {
        // Arrange
        var runner = new RecordingCommandRunner();
        var clipboard = new CommandClipboard(runner, "/repo");

        // Act
        await clipboard.TryCopyAsync("needs a guard");

        // Assert
        Assert.Single(runner.Requests);
    }

    [Fact]
    public async Task It_says_nothing_was_copied_when_no_tool_is_installed()
    {
        // Arrange
        var clipboard = new CommandClipboard(
            new RecordingCommandRunner { Installed = [] },
            "/repo");

        // Act
        var copied = await clipboard.TryCopyAsync("needs a guard");

        // Assert
        Assert.False(copied);
    }

    [Fact]
    public async Task It_does_not_wait_on_the_output_of_a_tool_that_stays_running()
    {
        // Arrange
        var runner = new RecordingCommandRunner();
        var clipboard = new CommandClipboard(runner, "/repo");

        // Act
        await clipboard.TryCopyAsync("needs a guard");

        // Assert
        Assert.False(runner.Requests[0].CaptureOutput);
    }

    private sealed class RecordingCommandRunner : ICommandRunner
    {
        public List<CommandRequest> Requests { get; } = [];

        /// <summary>The tools the machine has. One missing throws the way a
        /// missing executable does.</summary>
        public IReadOnlyList<string> Installed { get; init; } =
            ["wl-copy", "xclip", "xsel", "pbcopy"];

        /// <summary>The one tool that takes the text. The rest run and refuse.
        /// </summary>
        public string Takes { get; init; } = "wl-copy";

        public Task<CommandResult> RunAsync(
            CommandRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            if (!Installed.Contains(request.FileName))
            {
                throw new Win32Exception($"{request.FileName} not found");
            }

            return Task.FromResult(
                new CommandResult(request.FileName == Takes ? 0 : 1, "", ""));
        }
    }
}
