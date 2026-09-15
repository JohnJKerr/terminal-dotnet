using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenLocatingTestSource
{
    [Fact]
    public async Task It_ignores_a_copy_left_in_a_nested_build_directory()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "sub", "obj"));
            var project = Path.Combine(root, "Shop.Tests.csproj");
            await File.WriteAllTextAsync(project, "<Project />");
            await File.WriteAllTextAsync(Path.Combine(root, "sub", "obj", "CartTests.cs"), """
                public sealed class CartTests
                {
                    public void Adds_item()
                    {
                    }
                }
                """);
            var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", project);

            // Act
            var source = await new FileTestSourceLocator().LocateAsync(test);

            // Assert
            Assert.Null(source);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact(Timeout = 10_000)]
    public async Task It_gives_up_on_a_missing_test_when_a_folder_links_back_to_its_parent()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "sub"));
            Directory.CreateSymbolicLink(Path.Combine(root, "sub", "loop"), root);
            var project = Path.Combine(root, "Shop.Tests.csproj");
            await File.WriteAllTextAsync(project, "<Project />");
            var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", project);

            // Act
            var source = await new FileTestSourceLocator().LocateAsync(test);

            // Assert
            Assert.Null(source);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact(Timeout = 10_000)]
    public async Task It_passes_over_a_named_pipe_rather_than_waiting_for_a_writer()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            if (!NamedPipe.TryCreate(Path.Combine(root, "CartTests.cs")))
            {
                return;
            }

            var project = Path.Combine(root, "Shop.Tests.csproj");
            await File.WriteAllTextAsync(project, "<Project />");
            var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", project);

            // Act
            var source = await Task.Run(() => new FileTestSourceLocator().LocateAsync(test));

            // Assert
            Assert.Null(source);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

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
            var source = await new FileTestSourceLocator().LocateAsync(test);

            // Assert
            Assert.Equal((sourcePath, 6), (source!.Path, source.HighlightLine));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
