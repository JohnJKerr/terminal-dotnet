using System.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Mouse;

public sealed class WhenScrollingThePreviewWithTheMouse
{
    [Fact]
    public void Wheeling_down_over_source_text_moves_the_preview_forward()
    {
        // Arrange
        var (preview, source) = GivenA.PreviewWithLongSource();

        // Act
        WheelOver(source, preview, MouseFlags.WheeledDown);

        // Assert
        Assert.Equal(1, source.Viewport.Y);
    }

    [Fact]
    public void Wheeling_up_over_source_text_moves_the_preview_back()
    {
        // Arrange
        var (preview, source) = GivenA.PreviewWithLongSource();
        source.ScrollVertical(1);

        // Act
        WheelOver(source, preview, MouseFlags.WheeledUp);

        // Assert
        Assert.Equal(0, source.Viewport.Y);
    }

    private static void WheelOver(View target, View preview, MouseFlags direction)
    {
        var mouse = new global::Terminal.Gui.Input.Mouse { Flags = direction, Position = new Point(2, 2), View = target };
        if (target.NewMouseEvent(mouse) != true)
        {
            preview.NewMouseEvent(mouse);
        }
    }
}
