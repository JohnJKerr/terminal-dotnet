using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Files;
using TerminalDotnet.Filters;
using static TerminalDotnet.Terminal.KeyMatch;

namespace TerminalDotnet.Terminal;

public abstract record FilePanelAction
{
    public sealed record OpenFile(string Path) : FilePanelAction;

    /// <summary>Lists the projects' files, narrowed to a filter when one is
    /// named, or every file beneath the launch folder.</summary>
    public sealed record ShowFiles(bool AllFiles, ExplorerFilter? Filter) : FilePanelAction;
}

public static class FilePanelKeyBindings
{
    public static FilePanelAction? ActionFor(
        Key key,
        VisibleFileNode? selected,
        bool searchActive,
        bool showsAllFiles = false,
        ExplorerFilter? activeFilter = null)
    {
        if (searchActive)
        {
            return null;
        }

        if (FilterKeyBindings.StepFor(key) is { } step)
        {
            return FilesStepped(showsAllFiles, activeFilter, step);
        }

        if (selected is null || selected.Kind != FileNodeKind.File)
        {
            return null;
        }

        return Is(key, KeyCode.Enter) || Is(key, KeyCode.E)
            ? new FilePanelAction.OpenFile(selected.Files[0].Path)
            : null;
    }

    private enum Listing { AllFiles, Updated }

    private static readonly Listing[] Listings = Enum.GetValues<Listing>();

    private static FilePanelAction FilesStepped(bool showsAllFiles, ExplorerFilter? activeFilter, int step)
    {
        Listing? showing = showsAllFiles ? Listing.AllFiles : activeFilter is null ? null : Listing.Updated;
        return FilterRing.Stepped(Listings, showing, step) switch
        {
            Listing.AllFiles => new FilePanelAction.ShowFiles(AllFiles: true, Filter: null),
            Listing.Updated => new FilePanelAction.ShowFiles(AllFiles: false, ExplorerFilter.Updated),
            _ => new FilePanelAction.ShowFiles(AllFiles: false, Filter: null)
        };
    }
}
