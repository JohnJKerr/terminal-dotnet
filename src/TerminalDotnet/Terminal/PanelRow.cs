using Terminal.Gui.Drawing;

namespace TerminalDotnet.Terminal;

public enum RowTone
{
    Neutral,
    Modified,
    New,
    Deleted,
    Warning
}

/// <summary>A row of a list panel, in the tone of what it lists.</summary>
public sealed record PanelRow(string Text, RowTone Tone);

public static class RowAppearance
{
    public static global::Terminal.Gui.Drawing.Attribute For(
        RowTone tone,
        bool isSelected,
        global::Terminal.Gui.Drawing.Attribute normal,
        global::Terminal.Gui.Drawing.Attribute selected)
    {
        var baseAppearance = isSelected ? selected : normal;
        return new global::Terminal.Gui.Drawing.Attribute(
            ForegroundFor(tone, baseAppearance.Foreground),
            baseAppearance.Background);
    }

    public static Color ForegroundFor(RowTone tone, Color unchanged) => tone switch
    {
        RowTone.Modified => Color.BrightBlue,
        RowTone.New => Color.BrightGreen,
        RowTone.Deleted => Color.BrightRed,
        RowTone.Warning => Color.BrightYellow,
        _ => unchanged
    };
}
