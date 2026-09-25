using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Issues;

public sealed class WhenCreatingAnIssuePanelSnapshot
{
    [Fact]
    public void It_counts_one_error_and_one_warning_in_the_singular()
    {
        // Arrange
        var state = new IssueState([Error(), Warning()]);

        // Act
        var snapshot = IssuePanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["1 Error", "1 Warning", "0 Flags"],
            snapshot.StatusSegments.Select(segment => segment.Text));
    }

    [Fact]
    public void It_counts_several_errors_and_warnings_in_the_plural()
    {
        // Arrange
        var state = new IssueState([Error(), Error(), Warning(), Warning()]);

        // Act
        var snapshot = IssuePanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["2 Errors", "2 Warnings", "0 Flags"],
            snapshot.StatusSegments.Select(segment => segment.Text));
    }

    [Fact]
    public void It_counts_nothing_found_in_the_plural()
    {
        // Arrange
        var state = new IssueState([]);

        // Act
        var snapshot = IssuePanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["0 Errors", "0 Warnings", "0 Flags"],
            snapshot.StatusSegments.Select(segment => segment.Text));
    }

    [Fact]
    public void It_counts_the_flags()
    {
        // Arrange
        var state = new IssueState([Error(), Flag()]);

        // Act
        var snapshot = IssuePanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["1 Error", "0 Warnings", "1 Flag"],
            snapshot.StatusSegments.Select(segment => segment.Text));
    }

    [Fact]
    public void It_offers_a_filter_for_the_flags()
    {
        // Arrange
        var state = new IssueState([Flag()]) { ActiveFilter = IssueFilter.Flags };

        // Act
        var snapshot = IssuePanelSnapshot.From(state);

        // Assert
        Assert.Contains(new FilterChip("F Flags", true), snapshot.Filters);
    }

    [Fact]
    public void It_names_each_filter_by_its_capital_letter()
    {
        // Arrange
        var state = new IssueState([]);

        // Act
        var snapshot = IssuePanelSnapshot.From(state);

        // Assert
        Assert.Equal(["X Errors", "W Warnings", "F Flags"], snapshot.Filters.Select(chip => chip.Text));
    }

    [Fact]
    public void It_leaves_a_flag_in_the_plain_colour()
    {
        // Arrange
        var state = new IssueState([Flag()]);

        // Act
        var snapshot = IssuePanelSnapshot.From(state);

        // Assert
        Assert.Equal([RowTone.Neutral], snapshot.Rows.Select(row => row.Tone));
    }

    private static CompilationIssue Flag() => new(
        "/repo/src/Cart.cs",
        "src/Cart.cs",
        3,
        0,
        "TODO",
        "handle discounts",
        IssueSeverity.Flag);

    private static CompilationIssue Error() => new(
        "/repo/src/Order.cs",
        "src/Order.cs",
        12,
        5,
        "CS0103",
        "The name 'total' does not exist in the current context",
        IssueSeverity.Error);

    private static CompilationIssue Warning() => new(
        "/repo/src/Cart.cs",
        "src/Cart.cs",
        8,
        13,
        "CS0168",
        "The variable 'count' is declared but never used",
        IssueSeverity.Warning);
}
