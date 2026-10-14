using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>Keeps each run update in the order it was reported, as it is
/// reported, so a test can read them as soon as the run returns.</summary>
internal sealed class RecordedProgress : IProgress<TestRunUpdate>
{
    private readonly List<TestRunUpdate> updates = [];

    public IReadOnlyList<TestRunUpdate> Updates
    {
        get
        {
            lock (updates)
            {
                return [.. updates];
            }
        }
    }

    public void Report(TestRunUpdate value)
    {
        lock (updates)
        {
            updates.Add(value);
        }
    }
}
