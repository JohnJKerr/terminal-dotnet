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
}
