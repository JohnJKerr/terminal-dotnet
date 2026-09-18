namespace TerminalDotnet.Trust;

/// <summary>The folder a launch is asking to build, and whether the reader has
/// already said they trust it.</summary>
public sealed record TrustDecision(string Folder, bool Trusted);

/// <summary>What the reader is asked before a folder is built for the first
/// time. The last choice is the one a stray Enter picks, so it is the safe
/// one.</summary>
public sealed record TrustQuestion(string Title, string Message, string TrustChoice, string QuitChoice)
{
    public static TrustQuestion For(string folder) => new(
        "Trust this folder?",
        $"""
        terminal-dotnet builds the solution in

        {folder}

        Building runs code the solution defines: MSBuild targets, analyzers, source generators and packages from its NuGet feeds. Only continue if you trust the authors of this folder.

        Trusting it is remembered for next time.
        """,
        "Trust folder",
        "Quit");

    /// <returns>The question written out for a console with no room to draw
    /// it. Enter alone answers with the safe choice, as the box's default
    /// button does.</returns>
    public string AsText() =>
        $"{Title}{Environment.NewLine}{Environment.NewLine}" +
        $"{Message}{Environment.NewLine}{Environment.NewLine}" +
        $"{TrustChoice}? [y/N] ({QuitChoice} on Enter): ";

    /// <returns>Whether a written answer trusts the folder. Anything that is
    /// not a yes quits, so a stray Enter is as safe as it is in the box.</returns>
    public static bool Trusts(string? answer) =>
        answer?.Trim().ToLowerInvariant() is "y" or "yes";

    public static string Declined(string folder) =>
        $"Not trusted: {folder}{Environment.NewLine}" +
        "terminal-dotnet only opens folders whose solution you trust it to build.";

    public static string NotRemembered(string folder, string storePath) =>
        $"Could not remember that {folder} is trusted: {storePath} could not be written. " +
        "You will be asked again next time.";
}
