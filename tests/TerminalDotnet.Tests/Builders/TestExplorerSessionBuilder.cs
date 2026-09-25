using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Fakes;

namespace TerminalDotnet.Tests.Builders;

/// <summary>Arranges a test explorer over tests held in memory, with no
/// source to find and nothing updated unless a test asks for them.</summary>
internal sealed class TestExplorerSessionBuilder
{
    public const string Target = "/repo/Shop.sln";

    private ITestBackend backend = new InMemoryTestBackend([]);
    private ITestSourceLocator sources = new FixedTestSource();
    private IUpdatedSourceProvider updatedSources = new FixedUpdatedSources();

    public TestExplorerSessionBuilder WithTests(params TestCase[] tests) =>
        WithBackend(new InMemoryTestBackend(tests));

    public TestExplorerSessionBuilder WithBackend(ITestBackend used)
    {
        backend = used;
        return this;
    }

    public TestExplorerSessionBuilder WithSourceAt(SourceLocation source) =>
        WithSourceLocator(new FixedTestSource(source));

    public TestExplorerSessionBuilder WithSourceLocator(ITestSourceLocator used)
    {
        sources = used;
        return this;
    }

    public TestExplorerSessionBuilder WithUpdatedSources(params UpdatedSource[] sources) =>
        WithUpdatedSources(new FixedUpdatedSources(sources));

    /// <summary>Sources git reports as modified.</summary>
    public TestExplorerSessionBuilder WithEditedSources(params string[] paths) =>
        WithUpdatedSources([.. paths.Select(path => new UpdatedSource(path, ChangeKind.Modified))]);

    public TestExplorerSessionBuilder WithUpdatedSources(IUpdatedSourceProvider used)
    {
        updatedSources = used;
        return this;
    }

    public TestExplorerSession Build() => new(backend, sources, updatedSources);

    /// <summary>The session once it has discovered its tests.</summary>
    public async Task<TestExplorerSession> LoadedAsync(string target = Target)
    {
        var session = Build();
        await session.LoadAsync(target);
        return session;
    }
}
