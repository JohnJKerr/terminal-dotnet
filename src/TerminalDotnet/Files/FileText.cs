using System.Text;

namespace TerminalDotnet.Files;

/// <summary>
/// The text of a file the app shows or scans. Those files come from the
/// repository being read, which can hold anything: a generated file of
/// hundreds of megabytes, or a named pipe or device that an unversioned folder
/// or a test's stack trace points at. A file is only opened when it reports
/// content, because opening a named pipe waits for a writer and a device never
/// runs dry, and no more than the limit is ever read.
/// </summary>
public static class FileText
{
    public const long MaxBytes = 10 * 1024 * 1024;
    private const int ChunkBytes = 81_920;

    /// <returns>The file's text, or null when it holds more than
    /// <paramref name="maxBytes"/>.</returns>
    public static string? ReadWithin(string path, long maxBytes = MaxBytes)
    {
        var length = new FileInfo(path).Length;
        if (length == 0)
        {
            return "";
        }

        if (length > maxBytes)
        {
            return null;
        }

        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        return BytesWithin(stream, maxBytes) is { } bytes ? Decoded(bytes) : null;
    }

    /// <returns>The file's lines, split where <see cref="File.ReadLines(string)"/>
    /// splits them, or null when it holds more than <paramref name="maxBytes"/>.
    /// </returns>
    public static IReadOnlyList<string>? ReadLinesWithin(string path, long maxBytes = MaxBytes) =>
        ReadWithin(path, maxBytes) is { } text ? LinesOf(text) : null;

    private static IReadOnlyList<string> LinesOf(string text)
    {
        using var reader = new StringReader(text);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }

    /// <summary>The file can grow between being measured and being read, so
    /// the limit holds while reading too.</summary>
    private static MemoryStream? BytesWithin(Stream stream, long maxBytes)
    {
        var bytes = new MemoryStream();
        var chunk = new byte[ChunkBytes];
        int read;
        while ((read = stream.Read(chunk)) > 0)
        {
            if (bytes.Length + read > maxBytes)
            {
                return null;
            }

            bytes.Write(chunk, 0, read);
        }

        bytes.Position = 0;
        return bytes;
    }

    private static string Decoded(MemoryStream bytes)
    {
        using var reader = new StreamReader(bytes, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
