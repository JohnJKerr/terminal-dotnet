using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenTitlingAPanel
{
    [Fact]
    public void It_leads_with_the_number_that_reaches_the_panel()
    {
        // Act
        var title = PanelTitle.For(PanelKind.Tests, [], "", focused: false);

        // Assert
        Assert.Equal("[2]─Tests", title);
    }

    [Fact]
    public void It_names_every_filter_on_the_focused_panel()
    {
        // Act
        var title = PanelTitle.For(PanelKind.Explorer, Filters(), "", focused: true);

        // Assert
        Assert.Equal("[1]─Explorer A All files U Updated", title);
    }

    [Fact]
    public void It_shows_only_the_keys_of_unused_filters_on_other_panels()
    {
        // Act
        var title = PanelTitle.For(PanelKind.Explorer, Filters(), "", focused: false);

        // Assert
        Assert.Equal("[1]─Explorer A U", title);
    }

    [Fact]
    public void It_names_the_filter_in_use_on_any_panel()
    {
        // Act
        var title = PanelTitle.For(
            PanelKind.Explorer,
            [new FilterChip("A All files", false), new FilterChip("U Updated", true)],
            "",
            focused: false);

        // Assert
        Assert.Equal("[1]─Explorer A ●U Updated", title);
    }

    [Fact]
    public void It_marks_the_filter_in_use_on_the_focused_panel()
    {
        // Act
        var title = PanelTitle.For(
            PanelKind.Explorer,
            [new FilterChip("A All files", true), new FilterChip("U Updated", false)],
            "",
            focused: true);

        // Assert
        Assert.Equal("[1]─Explorer ●A All files U Updated", title);
    }

    [Fact]
    public void It_shows_the_search_after_the_filters()
    {
        // Act
        var title = PanelTitle.For(PanelKind.Explorer, Filters(), "order", focused: false);

        // Assert
        Assert.Equal("[1]─Explorer A U ─ /order", title);
    }

    [Fact]
    public void It_counts_the_selected_row_in_the_footer()
    {
        // Act
        var footer = PanelTitle.Footer(selectedIndex: 2, rowCount: 42);

        // Assert
        Assert.Equal("3 of 42", footer);
    }

    [Fact]
    public void It_counts_nothing_in_an_empty_panel()
    {
        // Act
        var footer = PanelTitle.Footer(selectedIndex: 0, rowCount: 0);

        // Assert
        Assert.Equal("0 of 0", footer);
    }

    [Fact]
    public void It_titles_the_preview_with_the_key_that_reaches_it()
    {
        // Act
        var title = PanelTitle.For(PanelKind.Preview, [], "", focused: false);

        // Assert
        Assert.Equal("[0]─Preview", title);
    }

    private static IReadOnlyList<FilterChip> Filters() =>
        [new FilterChip("A All files", false), new FilterChip("U Updated", false)];
}
