using System.Diagnostics;

namespace TerminalDotnet.Testing;

public sealed record CommandRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    bool CaptureOutput = true)
{
    public string StandardInput { get; init; } = "";

    /// <summary>Variables added to the process's own environment. Empty leaves
    /// the process with the environment it inherited.</summary>
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
        var startInfo = new ProcessStartInfo(request.FileName)
        {
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardOutput = request.CaptureOutput,
            RedirectStandardError = request.CaptureOutput,
            RedirectStandardInput = true,
            UseShellExecute = false
        };
        foreach (var variable in request.Environment)
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException($"Could not start {request.FileName}.");
        await WriteStandardInputAsync(process, request).ConfigureAwait(false);
        var standardOutput = request.CaptureOutput
            ? process.StandardOutput.ReadToEndAsync(CancellationToken.None)
            : Task.FromResult(string.Empty);
        var standardError = request.CaptureOutput
            ? process.StandardError.ReadToEndAsync(CancellationToken.None)
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
