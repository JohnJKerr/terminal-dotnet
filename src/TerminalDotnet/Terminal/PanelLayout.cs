namespace TerminalDotnet.Terminal;

public sealed record PanelArea(int X, int Y, int Width, int Height);

/// <summary>
/// Where each panel sits on the screen. The lists the reader moves through
/// most are stacked on the left, and the one they were last in stretches to
/// show more of its rows; the preview takes the rest of the width, over the
/// issues and the comments. A panel shown full screen takes the whole
/// screen, which is what a narrow tile leaves room for.
/// </summary>
public sealed class PanelLayout
{
    public const int MinimumListWidth = 28;
    private const double ListShare = 0.36;
    private const double ExpandedWeight = 2.6;
    private const double BottomShare = 0.3;
    private const int MinimumBottomHeight = 8;

    public static readonly PanelArea Hidden = new(0, 0, 0, 0);

    private static readonly PanelKind[] Stacked = [PanelKind.Explorer, PanelKind.Tests, PanelKind.Changes];

    private readonly IReadOnlyDictionary<PanelKind, PanelArea> areas;

    private PanelLayout(IReadOnlyDictionary<PanelKind, PanelArea> areas)
    {
        this.areas = areas;
    }

    public PanelArea Preview => areas[PanelKind.Preview];

    public PanelArea this[PanelKind panel] => areas[panel];

    public static PanelLayout For(int width, int height, PanelKind expanded, PanelKind? fullScreen = null) =>
        fullScreen is { } shown
            ? FullScreen(width, height, shown)
            : Tiled(width, height, expanded);

    private static PanelLayout FullScreen(int width, int height, PanelKind shown) => new(
        Enum.GetValues<PanelKind>().ToDictionary(
            panel => panel,
            panel => panel == shown ? new PanelArea(0, 0, width, height) : Hidden));

    private static PanelLayout Tiled(int width, int height, PanelKind expanded)
    {
        var listWidth = Math.Min(width, Math.Max(MinimumListWidth, (int)(width * ListShare)));
        var rightWidth = width - listWidth;
        var bottomHeight = Math.Min(height, Math.Max(MinimumBottomHeight, (int)(height * BottomShare)));
        var previewHeight = height - bottomHeight;
        var issuesWidth = rightWidth / 2;
        return new PanelLayout(
            new Dictionary<PanelKind, PanelArea>(StackedAreas(listWidth, height, expanded))
            {
                [PanelKind.Issues] = new(listWidth, previewHeight, issuesWidth, bottomHeight),
                [PanelKind.Comments] = new(listWidth + issuesWidth, previewHeight, rightWidth - issuesWidth, bottomHeight),
                [PanelKind.Preview] = new(listWidth, 0, rightWidth, previewHeight)
            });
    }

    /// <summary>The lists beside the expanded one share a weight each, and the
    /// expanded list takes whatever rows the rounding leaves.</summary>
    private static IEnumerable<KeyValuePair<PanelKind, PanelArea>> StackedAreas(
        int width,
        int height,
        PanelKind expanded)
    {
        var collapsedHeight = (int)(height / (ExpandedWeight + Stacked.Length - 1));
        var heights = Stacked
            .Select(panel => panel == expanded ? height - collapsedHeight * (Stacked.Length - 1) : collapsedHeight)
            .ToArray();
        var tops = heights.Select((_, index) => heights.Take(index).Sum()).ToArray();
        return Stacked.Select((panel, index) =>
            KeyValuePair.Create(panel, new PanelArea(0, tops[index], width, heights[index])));
    }
}
