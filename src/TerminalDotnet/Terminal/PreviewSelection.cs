using System.Globalization;
using Terminal.Gui.Text;

namespace TerminalDotnet.Terminal;

/// <summary>A cell of the previewed text: a line of it, and a column across
/// that line, both counted from zero.</summary>
public readonly record struct PreviewCell(int Line, int Column);

/// <summary>
/// The text a drag across the preview covers, from the cell the button went
/// down on to the cell it has reached, both included. The cells are those of
/// the text rather than of the screen, so the selection stays on its text
/// while the preview scrolls under it.
/// </summary>
/// <param name="TabStop">How far apart the preview draws its tab stops. A
/// preview that draws a tab as a single cell has them one apart.</param>
public sealed record PreviewSelection(PreviewCell From, PreviewCell To, int TabStop = 1)
{
    private PreviewCell First => Reversed ? To : From;

    private PreviewCell Last => Reversed ? From : To;

    private bool Reversed => (To.Line, To.Column).CompareTo((From.Line, From.Column)) < 0;

    public string TextIn(IReadOnlyList<string> lines) => From == To
        ? ""
        : string.Join('\n', SelectedLines(lines).Select(line => string.Concat(SelectedGlyphs(lines, line).Select(glyph => glyph.Text))));

    private IEnumerable<int> SelectedLines(IReadOnlyList<string> lines)
    {
        var first = Math.Max(First.Line, 0);
        var last = Math.Min(Last.Line, lines.Count - 1);
        return Enumerable.Range(first, Math.Max(last - first + 1, 0));
    }

    /// <summary>A glyph is taken when any cell of it is, so a drag that ends
    /// on half of a wide character or part-way through a tab takes all of it.
    /// </summary>
    private IEnumerable<Glyph> SelectedGlyphs(IReadOnlyList<string> lines, int line)
    {
        var from = line == First.Line ? First.Column : 0;
        var to = line == Last.Line ? Last.Column : int.MaxValue;
        return Glyphs(lines[line]).Where(glyph => glyph.Column + glyph.Width > from && glyph.Column <= to);
    }

    private IEnumerable<Glyph> Glyphs(string line)
    {
        var column = 0;
        var graphemes = StringInfo.GetTextElementEnumerator(line);
        while (graphemes.MoveNext())
        {
            var glyph = new Glyph(graphemes.GetTextElement(), column, 0);
            glyph = glyph with { Width = WidthOf(glyph) };
            yield return glyph;
            column += glyph.Width;
        }
    }

    private int WidthOf(Glyph glyph) => glyph.Text == "\t"
        ? TabStop - glyph.Column % TabStop
        : Math.Max(glyph.Text.GetColumns(), 1);

    private sealed record Glyph(string Text, int Column, int Width);
}
