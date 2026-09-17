using TerminalDotnet.Files;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenReadingAFileForDisplay : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("terminal-dotnet-");

    [Fact]
    public async Task It_reads_the_text_of_a_file_within_the_limit()
    {
        // Arrange
        var path = Path.Combine(directory.FullName, "Order.cs");
        await File.WriteAllTextAsync(path, "class Order {}");

        // Act
        var text = FileText.ReadWithin(path, maxBytes: 64);

        // Assert
        Assert.Equal("class Order {}", text);
    }

    [Fact]
    public async Task It_declines_a_file_larger_than_the_limit()
    {
        // Arrange
        var path = Path.Combine(directory.FullName, "Generated.cs");
        await File.WriteAllTextAsync(path, "class Generated {}");

        // Act
        var text = FileText.ReadWithin(path, maxBytes: 8);

        // Assert
        Assert.Null(text);
    }

    [Fact(Timeout = 10_000)]
    public async Task It_reads_nothing_from_a_named_pipe_rather_than_waiting_for_a_writer()
    {
        // Arrange
        var path = Path.Combine(directory.FullName, "Pipe.cs");
        if (!NamedPipe.TryCreate(path))
        {
            return;
        }

        // Act
        var text = await Task.Run(() => FileText.ReadWithin(path));

        // Assert
        Assert.Equal("", text);
    }

    public void Dispose() => directory.Delete(recursive: true);
}
