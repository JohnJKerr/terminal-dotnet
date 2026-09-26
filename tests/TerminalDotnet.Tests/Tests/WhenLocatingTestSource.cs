using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenLocatingTestSource
{
    private const string CartTests = """
        namespace Shop.Tests;

        public sealed class CartTests
        {
            [Fact]
            public void Adds_item()
            {
            }
        }
        """;

    [Fact]
    public async Task It_ignores_a_copy_left_in_a_nested_build_directory()
    {
        // Arrange
        using var workspace = ShopTests().WithFile("sub/obj/CartTests.cs", CartTests);

        // Act
        var source = await LocateAddsItemAsync(workspace);

        // Assert
        Assert.Null(source);
    }

    [Fact(Timeout = 10_000)]
    public async Task It_gives_up_on_a_missing_test_when_a_folder_links_back_to_its_parent()
    {
        // Arrange
        using var workspace = ShopTests().WithFolder("sub");
        Directory.CreateSymbolicLink(workspace.PathTo("sub/loop"), workspace.Root);

        // Act
        var source = await LocateAddsItemAsync(workspace);

        // Assert
        Assert.Null(source);
    }

    [Fact(Timeout = 10_000)]
    public async Task It_passes_over_a_named_pipe_rather_than_waiting_for_a_writer()
    {
        // Arrange
        using var workspace = ShopTests();
        if (!NamedPipe.TryCreate(workspace.PathTo("CartTests.cs")))
        {
            return;
        }

        // Act
        var source = await Task.Run(() => LocateAddsItemAsync(workspace));

        // Assert
        Assert.Null(source);
    }

    [Fact]
    public async Task It_finds_a_test_before_the_test_has_run()
    {
        // Arrange
        using var workspace = ShopTests().WithFile("CartTests.cs", CartTests);

        // Act
        var source = await LocateAddsItemAsync(workspace);

        // Assert
        Assert.Equal((workspace.PathTo("CartTests.cs"), 6), (source?.Path, source?.HighlightLine));
    }

    private static TemporaryWorkspace ShopTests() =>
        TemporaryWorkspace.Create().WithFile("Shop.Tests.csproj", "<Project />");

    private static Task<SourceLocation?> LocateAddsItemAsync(TemporaryWorkspace workspace) =>
        new FileTestSourceLocator().LocateAsync(
            GivenA.TestCase("Shop.Tests.CartTests.Adds_item", workspace.PathTo("Shop.Tests.csproj")));
}
