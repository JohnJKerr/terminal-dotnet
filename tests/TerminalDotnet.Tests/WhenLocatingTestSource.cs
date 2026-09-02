using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenLocatingTestSource
{
    [Fact]
    public async Task It_finds_a_test_before_the_test_has_run()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            var project = Path.Combine(root, "Shop.Tests.csproj");
            var sourcePath = Path.Combine(root, "CartTests.cs");
            await File.WriteAllTextAsync(project, "<Project />");
            await File.WriteAllTextAsync(sourcePath, """
                namespace Shop.Tests;

                public sealed class CartTests
                {
                    [Fact]
                    public void Adds_item()
                    {
                    }
                }
                """);
            var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", project);

            // Act
            var source = await new FileTestSourceLocator(new FileSourceProvider()).LocateAsync(test);

            // Assert
            Assert.Equal((sourcePath, 6), (source!.Path, source.HighlightLine));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
