using System.Reflection;

namespace TerminalDotnet;

/// <summary>The version this build was stamped with, as major.minor.patch.</summary>
public sealed record VersionNumber(int Major, int Minor, int Patch)
{
    public static VersionNumber Current => From(BuiltInVersion());

    public static VersionNumber From(string? informationalVersion) =>
        Version.TryParse(WithoutSourceRevision(informationalVersion), out var version)
            ? new VersionNumber(version.Major, version.Minor, Math.Max(version.Build, 0))
            : new VersionNumber(0, 0, 0);

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    private static string WithoutSourceRevision(string? informationalVersion) =>
        informationalVersion?.Split('+')[0].Trim() ?? string.Empty;

    private static string? BuiltInVersion() =>
        typeof(VersionNumber).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
}
