using TerminalDotnet.Files;
using Xunit;

namespace TerminalDotnet.Tests;

public sealed class FileSystemExplorerBackendTests
{
    [Fact]
    public async Task DiscoverFindsProjectFilesAndDeclaredNamespaces()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "TerminalDotnet.slnx"),
            "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), "<Project />");
        await File.WriteAllTextAsync(
            Path.Combine(root, "src", "App", "Order.cs"),
            "namespace App.Domain; public sealed class Order;");

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend().DiscoverAsync(
                Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.Equal(
                [("App.csproj", "App.Domain", "Order.cs")],
                files.Select(file =>
                    (Path.GetFileName(file.ProjectPath), file.Namespace, Path.GetFileName(file.Path))));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
