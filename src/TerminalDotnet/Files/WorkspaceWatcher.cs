namespace TerminalDotnet.Files;

/// <summary>
/// Where the app hears about the working tree being written to by something
/// other than itself: an agent, another terminal, or a git command.
/// </summary>
public interface IWorkspaceWatcher : IDisposable
{
    /// <summary>Reports each edited path, relative to the watched folder.
    /// Replaces whatever was being watched before.</summary>
    void Watch(string folder, Action<string> onEdit);
}

/// <summary>
/// The operating system's own view of the folder. A build fills the watcher's
/// buffer faster than it can be drained, so the buffer is enlarged and an
/// overflow is reported as an edit of its own rather than dropped: the panels
/// cannot know what they missed, so they reload.
/// </summary>
public sealed class FileSystemWorkspaceWatcher : IWorkspaceWatcher
{
    private const int BufferSize = 64 * 1024;
    private const string Overflowed = "";

    private FileSystemWatcher? watcher;

    public void Watch(string folder, Action<string> onEdit)
    {
        watcher?.Dispose();
        watcher = new FileSystemWatcher(folder)
        {
            IncludeSubdirectories = true,
            InternalBufferSize = BufferSize,
            NotifyFilter = NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size
        };
        watcher.Changed += (_, edit) => Report(folder, edit.FullPath, onEdit);
        watcher.Created += (_, edit) => Report(folder, edit.FullPath, onEdit);
        watcher.Deleted += (_, edit) => Report(folder, edit.FullPath, onEdit);
        watcher.Renamed += (_, edit) => Report(folder, edit.FullPath, onEdit);
        watcher.Error += (_, _) => onEdit(Overflowed);
        watcher.EnableRaisingEvents = true;
    }

    private static void Report(string folder, string path, Action<string> onEdit)
    {
        if (WorkspacePath.RelativeTo(folder, path) is { } relative)
        {
            onEdit(relative);
        }
    }

    public void Dispose()
    {
        watcher?.Dispose();
        watcher = null;
    }
}
