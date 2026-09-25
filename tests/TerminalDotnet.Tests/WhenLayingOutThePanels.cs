using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenLayingOutThePanels
{
    [Fact]
    public void The_lists_on_the_left_take_about_a_third_of_the_width()
    {
        // Act
        var layout = PanelLayout.For(width: 200, height: 50, expanded: PanelKind.Explorer);

        // Assert
        Assert.Equal(72, layout[PanelKind.Explorer].Width);
    }

    [Fact]
    public void The_lists_on_the_left_keep_room_for_their_rows_on_a_narrow_screen()
    {
        // Act
        var layout = PanelLayout.For(width: 60, height: 50, expanded: PanelKind.Explorer);

        // Assert
        Assert.Equal(PanelLayout.MinimumListWidth, layout[PanelKind.Tests].Width);
    }

    [Fact]
    public void The_lists_on_the_left_fill_the_height_between_them()
    {
        // Act
        var layout = PanelLayout.For(width: 200, height: 50, expanded: PanelKind.Explorer);

        // Assert
        Assert.Equal(
            50,
            layout[PanelKind.Explorer].Height + layout[PanelKind.Tests].Height + layout[PanelKind.Changes].Height);
    }

    [Fact]
    public void The_lists_on_the_left_are_stacked_in_order()
    {
        // Act
        var layout = PanelLayout.For(width: 200, height: 50, expanded: PanelKind.Explorer);

        // Assert
        Assert.Equal(
            layout[PanelKind.Tests].Y + layout[PanelKind.Tests].Height,
            layout[PanelKind.Changes].Y);
    }

    [Fact]
    public void The_expanded_list_is_taller_than_the_lists_beside_it()
    {
        // Act
        var layout = PanelLayout.For(width: 200, height: 50, expanded: PanelKind.Tests);

        // Assert
        Assert.True(layout[PanelKind.Tests].Height > layout[PanelKind.Explorer].Height * 2);
    }

    [Fact]
    public void The_preview_takes_the_rest_of_the_width()
    {
        // Act
        var layout = PanelLayout.For(width: 200, height: 50, expanded: PanelKind.Explorer);

        // Assert
        Assert.Equal(200 - 72, layout.Preview.Width);
    }

    [Fact]
    public void The_issues_and_comments_share_the_bottom_of_the_right_column()
    {
        // Act
        var layout = PanelLayout.For(width: 200, height: 50, expanded: PanelKind.Explorer);

        // Assert
        Assert.Equal(
            layout.Preview.Width,
            layout[PanelKind.Issues].Width + layout[PanelKind.Comments].Width);
    }

    [Fact]
    public void The_issues_and_comments_take_under_a_third_of_the_height()
    {
        // Act
        var layout = PanelLayout.For(width: 200, height: 50, expanded: PanelKind.Explorer);

        // Assert
        Assert.Equal(15, layout[PanelKind.Issues].Height);
    }

    [Fact]
    public void The_preview_sits_above_the_issues()
    {
        // Act
        var layout = PanelLayout.For(width: 200, height: 50, expanded: PanelKind.Explorer);

        // Assert
        Assert.Equal(layout.Preview.Y + layout.Preview.Height, layout[PanelKind.Issues].Y);
    }

    [Theory]
    [InlineData(PanelKind.Tests)]
    [InlineData(PanelKind.Preview)]
    public void A_full_screen_panel_fills_the_screen(PanelKind panel)
    {
        // Act
        var layout = PanelLayout.For(width: 100, height: 40, expanded: PanelKind.Explorer, fullScreen: panel);

        // Assert
        Assert.Equal(new PanelArea(0, 0, 100, 40), layout[panel]);
    }

    [Fact]
    public void The_panels_behind_a_full_screen_panel_are_given_no_room()
    {
        // Act
        var layout = PanelLayout.For(width: 100, height: 40, expanded: PanelKind.Explorer, fullScreen: PanelKind.Issues);

        // Assert
        Assert.All(
            Enum.GetValues<PanelKind>().Where(panel => panel != PanelKind.Issues),
            panel => Assert.Equal(PanelLayout.Hidden, layout[panel]));
    }
}
