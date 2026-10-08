using System.Globalization;
using Terminal.Gui.Text;

namespace TerminalDotnet.Terminal;

/// <summary>A cell of the previewed text: a line of it, and a column across
/// that line, both counted from zero.</summary>
public readonly record struct PreviewCell(int Line, int Column);

/// <summary>The selected part of one line: the column it is drawn from, and
/// the text as it is drawn there.</summary>
public sealed record SelectedStretch(int Line, int Column, string Text);

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

    public string TextIn(IReadOnlyList<string> lines) =>
        string.Join('\n', SelectedLines(lines, 0, lines.Count).Select(line => TextOf(SelectedGlyphs(lines[line], line))));

    /// <summary>The stretches to mark over a preview showing
    /// <paramref name="height"/> lines from <paramref name="top"/>.</summary>
    public IReadOnlyList<SelectedStretch> MarkedIn(IReadOnlyList<string> lines, int top, int height) =>
    [
        .. SelectedLines(lines, top, height)
            .Select(line => (Line: line, Glyphs: SelectedGlyphs(lines[line], line).ToArray()))
            .Where(selected => selected.Glyphs.Length > 0)
            .Select(selected => new SelectedStretch(selected.Line, selected.Glyphs[0].Column, DrawnTextOf(selected.Glyphs)))
    ];

    /// <summary>The selected lines among the <paramref name="count"/> from
    /// <paramref name="start"/>. A button let go where it went down has
    /// selected nothing.</summary>
    private IEnumerable<int> SelectedLines(IReadOnlyList<string> lines, int start, int count)
    {
        var first = Math.Max(First.Line, Math.Max(start, 0));
        var last = Math.Min(Last.Line, Math.Min(start + count, lines.Count) - 1);
        return From == To ? [] : Enumerable.Range(first, Math.Max(last - first + 1, 0));
    }

    private static string TextOf(IEnumerable<Glyph> glyphs) => string.Concat(glyphs.Select(glyph => glyph.Text));

    private static string DrawnTextOf(IEnumerable<Glyph> glyphs) => string.Concat(
        glyphs.Select(glyph => glyph.Text == "\t" ? new string(' ', glyph.Width) : glyph.Text));

    /// <summary>A glyph is taken when any cell of it is, so a drag that ends
    /// on half of a wide character or part-way through a tab takes all of it.
    /// </summary>
    private IEnumerable<Glyph> SelectedGlyphs(string text, int line)
    {
        var from = line == First.Line ? First.Column : 0;
        var to = line == Last.Line ? Last.Column : int.MaxValue;
        return Glyphs(text).Where(glyph => glyph.Column + glyph.Width > from && glyph.Column <= to);
    }

    private IEnumerable<Glyph> Glyphs(string line)
    {
        var column = 0;
        var graphemes = StringInfo.GetTextElementEnumerator(line);
        while (graphemes.MoveNext())
        {
            var glyph = GlyphAt(column, graphemes.GetTextElement());
            yield return glyph;
            column += glyph.Width;
        }
    }

    private Glyph GlyphAt(int column, string text) => new(
        text,
        column,
        text == "\t" ? TabStop - column % TabStop : Math.Max(text.GetColumns(), 1));

    private sealed record Glyph(string Text, int Column, int Width);
}
