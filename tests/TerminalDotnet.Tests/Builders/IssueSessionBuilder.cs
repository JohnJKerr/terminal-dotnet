using TerminalDotnet.Comments;
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
using TerminalDotnet.Tests.Fakes;

namespace TerminalDotnet.Tests.Builders;

/// <summary>Arranges an issues panel over a build and a tree that report
/// nothing unless a test says otherwise.</summary>
internal sealed class IssueSessionBuilder
{
    private IIssueBackend issues = new FixedIssues();
    private IFlagBackend flags = new FixedFlags();
    private ICommentClipboard clipboard = new RecordingClipboard();

    public IssueSessionBuilder WithIssues(params CompilationIssue[] reported) => WithIssueBackend(new FixedIssues(reported));

    public IssueSessionBuilder WithIssueBackend(IIssueBackend used)
    {
        issues = used;
        return this;
    }

    public IssueSessionBuilder WithFlags(params Flag[] flagged) => WithFlagBackend(new FixedFlags(flagged));

    public IssueSessionBuilder WithFlagBackend(IFlagBackend used)
    {
        flags = used;
        return this;
    }

    public IssueSessionBuilder WithClipboard(ICommentClipboard used)
    {
        clipboard = used;
        return this;
    }

    public IssueSession Build() => new(issues, clipboard, flags);
}
