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

    public static string Declined(string folder) =>
        $"Not trusted: {folder}{Environment.NewLine}" +
        "terminal-dotnet only opens folders whose solution you trust it to build.";

    public static string NotRemembered(string folder, string storePath) =>
        $"Could not remember that {folder} is trusted: {storePath} could not be written. " +
        "You will be asked again next time.";
}
