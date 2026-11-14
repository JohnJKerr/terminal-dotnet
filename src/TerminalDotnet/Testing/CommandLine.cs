using System.Diagnostics;
using System.Text;

namespace TerminalDotnet.Testing;

public sealed record CommandRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    bool CaptureOutput = true)
{
    public bool InheritStandardInput { get; init; }

    public string StandardInput { get; init; } = "";

    public Action<string>? OnOutput { get; init; }

    public int? OutputLimit { get; init; }

    public IReadOnlyDictionary<string, string> Environment { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken = default);
}

public sealed class ProcessCommandRunner : ICommandRunner
{
    public async Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(ExecutablePath(request.FileName))
        {
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardOutput = request.CaptureOutput,
            RedirectStandardError = request.CaptureOutput,
            RedirectStandardInput = !request.InheritStandardInput,
            UseShellExecute = false
        };
        AddRequestedEnvironment(startInfo, request);

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException($"Could not start {request.FileName}.");
        if (!request.InheritStandardInput)
        {
            await WriteStandardInputAsync(process, request).ConfigureAwait(false);
        }
        var standardOutput = request.CaptureOutput
            ? ReadAllAsync(process.StandardOutput, request)
            : Task.FromResult(string.Empty);
        var standardError = request.CaptureOutput
            ? ReadAllAsync(process.StandardError, request)
            : Task.FromResult(string.Empty);
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await EndedAsync(process, standardOutput, standardError).ConfigureAwait(false);
            throw;
        }

        return new CommandResult(
            process.ExitCode,
            await standardOutput.ConfigureAwait(false),
            await standardError.ConfigureAwait(false));
    }

    private static string ExecutablePath(string fileName)
    {
        if (Path.IsPathRooted(fileName) || fileName.Contains(Path.DirectorySeparatorChar) ||
            fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            return fileName;
        }

        var extensions = OperatingSystem.IsWindows() && !Path.HasExtension(fileName)
            ? new[] { ".exe", ".com" }
            : new[] { "" };
        var folders = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(folder => folder.Trim('"'))
            .Where(Path.IsPathFullyQualified);
        foreach (var folder in folders)
        {
            var executable = extensions.Select(extension => Path.Combine(folder, fileName + extension))
                .FirstOrDefault(File.Exists);
            if (executable is not null)
            {
                return executable;
            }
        }

        throw new System.ComponentModel.Win32Exception(2, $"Executable not found on PATH: {fileName}");
    }

    private static void AddRequestedEnvironment(ProcessStartInfo startInfo, CommandRequest request)
    {
        foreach (var variable in request.Environment)
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }
    }

    private static async Task<string> ReadAllAsync(StreamReader reader, CommandRequest request)
    {
        var builder = new StringBuilder();
        var buffer = new char[4096];
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);
            if (read == 0)
            {
                return KeptEnd(builder, request.OutputLimit).ToString();
            }

            var chunk = new string(buffer, 0, read);
            builder.Append(chunk);
            if (builder.Length > 2 * request.OutputLimit)
            {
                KeptEnd(builder, request.OutputLimit);
            }

            request.OnOutput?.Invoke(chunk);
        }
    }

    private static StringBuilder KeptEnd(StringBuilder builder, int? limit) =>
        limit is { } kept && builder.Length > kept
            ? builder.Remove(0, builder.Length - kept)
            : builder;

    private static async Task WriteStandardInputAsync(Process process, CommandRequest request)
    {
        if (request.StandardInput.Length > 0)
        {
            await process.StandardInput.WriteAsync(request.StandardInput).ConfigureAwait(false);
        }

        process.StandardInput.Close();
    }

    private static async Task EndedAsync(Process process, params Task<string>[] readers)
    {
        KillProcessTree(process);
        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        await Task.WhenAll(readers).ConfigureAwait(false);
    }

    private static void KillProcessTree(Process process) =>
        ProcessTree.Stop(() => process.Kill(entireProcessTree: true));
}
