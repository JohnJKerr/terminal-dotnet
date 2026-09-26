using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Editor;

public sealed class WhenOpeningSource
{
    [Fact]
    public async Task It_issues_the_configured_editor_command_at_the_failure_line()
    {
        // Arrange
        var runner = new RecordingCommandRunner();
        var launcher = new EditorLauncher("code", runner);

        // Act
        await launcher.OpenAsync("/repo/CartTests.cs", 42);

        // Assert
        Assert.Equal(
            $"code|+42|/repo/CartTests.cs|{TestPaths.Repo}|capture:False",
            runner.LastRequest is null
                ? null
                : string.Join('|',
                    [
                        runner.LastRequest.FileName,
                        .. runner.LastRequest.Arguments,
                        runner.LastRequest.WorkingDirectory,
                        $"capture:{runner.LastRequest.CaptureOutput}"
                    ]));
    }

    [Fact]
    public async Task It_preserves_arguments_from_the_configured_editor_command()
    {
        // Arrange
        var runner = new RecordingCommandRunner();
        var launcher = new EditorLauncher("omarchy-launch-editor --inline", runner);

        // Act
        await launcher.OpenAsync("/repo/CartTests.cs", 42);

        // Assert
        Assert.Equal(
            "omarchy-launch-editor|--inline|+42|/repo/CartTests.cs",
            runner.LastRequest is null
                ? null
                : string.Join('|',
                    [runner.LastRequest.FileName, .. runner.LastRequest.Arguments]));
    }

    [Fact]
    public void It_passes_over_a_visual_editor_that_is_set_but_blank()
    {
        // Act
        var editor = EditorLauncher.Configured(visual: "  ", editor: "vim");

        // Assert
        Assert.Equal("vim", editor);
    }

    [Fact]
    public void It_falls_back_to_the_default_editor_when_neither_is_set()
    {
        // Act
        var editor = EditorLauncher.Configured(visual: null, editor: "");

        // Assert
        Assert.Equal(EditorLauncher.DefaultEditor, editor);
    }
}
