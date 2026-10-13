using TerminalDotnet.Terminal;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenCopyingTestsToTheClipboard
{
    [Fact]
    public async Task It_copies_the_selected_tests_to_the_same_clipboard_used_by_notes()
    {
        // Arrange
        var test = GivenA.TestCase("Shop.Tests.CartTests.Adds_item", "/repo/Shop.Tests.csproj");
        var session = await GivenA.TestExplorer().WithTests(test).LoadedAsync();
        var clipboard = new RecordingClipboard();

        // Act
        await new TestSelectionCopier(clipboard).CopyAsync(session.State);

        // Assert
        Assert.Equal("/repo/Shop.Tests.csproj\n  Shop.Tests.CartTests.Adds_item", clipboard.Copied);
    }
}
