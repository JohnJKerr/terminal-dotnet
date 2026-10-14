namespace TerminalDotnet.Testing;

/// <summary>
/// What every <c>dotnet build</c> and <c>dotnet test</c> the app starts needs.
/// Discovery reads English sentences out of the output, a reused build node
/// keeps the assemblies it just wrote locked, and one core is left idle so
/// the terminal can still draw while the rest of the machine builds.
/// </summary>
public static class DotnetProcess
{
    public const string LanguageVariable = "DOTNET_CLI_UI_LANGUAGE";

    public static IReadOnlyDictionary<string, string> Environment { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal) { [LanguageVariable] = "en" };

    public static IReadOnlyList<string> Switches() =>
        ["--nologo", "--tl:off", "-nodeReuse:false", CpuSwitch()];

    public static string CpuSwitch() => $"-m:{Math.Max(1, System.Environment.ProcessorCount - 1)}";

    public static CommandRequest Request(IReadOnlyList<string> arguments, string workingDirectory) =>
        new("dotnet", arguments, workingDirectory) { Environment = Environment };
}
