namespace TerminalDotnet.Filters;

public enum ExplorerFilter
{
    Updated,
    Failing,
    Passing,
    LastRun,
    NotRun
}

public static class ExplorerFilterText
{
    public static string DisplayName(this ExplorerFilter filter) => filter switch
    {
        ExplorerFilter.LastRun => "Last run",
        ExplorerFilter.NotRun => "Not run",
        _ => filter.ToString()
    };

    /// <summary>The capital letter that toggles the filter.</summary>
    public static string Key(this ExplorerFilter filter) => filter switch
    {
        ExplorerFilter.Updated => "U",
        ExplorerFilter.Failing => "F",
        ExplorerFilter.Passing => "P",
        ExplorerFilter.LastRun => "L",
        _ => "N"
    };
}
