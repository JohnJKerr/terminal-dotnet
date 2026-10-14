namespace TerminalDotnet.Testing;

public static class DotnetProcess
{
    public const string LanguageVariable = "DOTNET_CLI_UI_LANGUAGE";

    public static IReadOnlyDictionary<string, string> Environment { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal) { [LanguageVariable] = "en" };

    public static IReadOnlyList<string> Switches() =>
        [NoLogo, TerminalLoggerOff, DoNotReuseBuildNodes, CpuSwitch()];

    public static IReadOnlyList<string> OneProjectAtATimeSwitches() =>
        [NoLogo, TerminalLoggerOff, DoNotReuseBuildNodes, OneNode];

    public static string CpuSwitch() => $"-m:{Math.Max(1, System.Environment.ProcessorCount - 1)}";

    public static CommandRequest Request(IReadOnlyList<string> arguments, string workingDirectory) =>
        new("dotnet", arguments, workingDirectory) { Environment = Environment };

    private const string NoLogo = "--nologo";
    private const string TerminalLoggerOff = "--tl:off";
    private const string DoNotReuseBuildNodes = "-nodeReuse:false";
    private const string OneNode = "-m:1";
}
