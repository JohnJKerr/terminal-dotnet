using Terminal.Gui.Drawing;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace TerminalDotnet.Terminal;

/// <summary>
/// A read-only text view that keeps the colours captured from the test run.
/// <see cref="TextView"/> discards per-cell attributes while read-only unless the
/// foreground matches the background, and drops them from the leading cells of a
/// wrapped row, which renders the run in a single flat colour.
/// </summary>
#pragma warning disable CS0618
public sealed class TestOutputView : TextView
{
    public TestOutputView()
    {
        ReadOnly = true;
        WordWrap = true;
    }

    protected override void OnDrawReadOnlyColor(List<Cell> line, int idxCol, int idxRow) =>
        SetAttribute(NearestAttribute(line, idxCol) ?? GetAttributeForRole(VisualRole.ReadOnly));

    private static Attribute? NearestAttribute(List<Cell> line, int index)
    {
        for (var distance = 0; distance < line.Count; distance++)
        {
            if (index - distance >= 0 && line[index - distance].Attribute is { } preceding)
            {
                return preceding;
            }

            if (index + distance < line.Count && line[index + distance].Attribute is { } following)
            {
                return following;
            }
        }

        return null;
    }
}
#pragma warning restore CS0618
