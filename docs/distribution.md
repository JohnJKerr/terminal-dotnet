# Distribution

terminal-dotnet is distributed in one way: as the `terminal-dotnet` .NET tool on
[nuget.org](https://www.nuget.org/packages/terminal-dotnet).

## Why NuGet only

The app runs `dotnet build`, `dotnet test` and `git` against the solution in
front of it, so **everyone who can use it already has a .NET SDK installed**.
That makes the .NET tool the one channel that reaches every user:

- **One package for every platform.** The tool is framework-dependent and
  carries the syntax highlighter's native library for Linux, macOS and Windows
  (x64 and arm64). The whole package is about 5 MB.
- **Installed with something users already have.** `dotnet tool install -g`,
  a per-repository local tool pinned in `.config/dotnet-tools.json`, or
  `dnx terminal-dotnet` with no install at all.
- **No platform signing.** The SDK creates the tool's launcher on the user's
  machine, so there is no downloaded executable for macOS Gatekeeper or
  Windows SmartScreen to quarantine. No Apple Developer ID and no macOS build
  machine are needed.
- **One channel to secure and maintain.** Publishing uses nuget.org trusted
  publishing, so no long-lived API key exists to leak.

### What this gives up

- **A .NET 10 runtime is required.** Terminal.Gui 2.4 ships for .NET 10 only.
  `RollForward=Major` lets .NET 11 and later run the tool, but a user whose only
  SDK is 8 or 9 has to install the .NET 10 runtime alongside it.
- **Updates are not part of system upgrades.** Users run
  `dotnet tool update -g terminal-dotnet` rather than getting it through
  `brew upgrade` or `yay -Syu`.
- **Less discoverable outside the .NET ecosystem.** It will not appear in
  Omarchy's *Install* menus, Homebrew search or winget. For a tool only .NET
  developers need, that costs little.

### When to revisit

Add another channel only when users ask for it.
- **AUR:** the route onto Arch and Omarchy. Omarchy's own package repository is
  curated by its maintainers and takes no outside submissions.
- **Homebrew tap:** for macOS. It could build from source against Homebrew's
  `dotnet` formula.
- **winget:** for Windows. It would need self-contained release binaries.

Snap and Flatpak are poor fits in any case. Their sandboxes block the app from
running the host's `dotnet` and `git` against arbitrary folders.

## Releasing

### One-time setup

1. On nuget.org, open your profile menu, choose **Trusted Publishing**, and add
   a policy:
   - Repository owner: `JohnJKerr`
   - Repository: `terminal-dotnet`
   - Workflow file: `release.yml`
   - Environment: `nuget`
   - Scope: allow publishing new packages, so the first version can be pushed,
     limited to `terminal-dotnet`
2. In the GitHub repository settings:
   - Create the `nuget` environment. Optionally, require a reviewer so every
     publish waits for approval.
   - Add a repository variable `NUGET_USER` holding your nuget.org profile
     name. This is not your email address.
3. Commit the change that introduces `release.yml` before the first tag. For a
   private repository the policy stays provisional until a publish succeeds
   within seven days. Re-activate it on nuget.org if that window passes.

### Each release

1. Check the version `main` stamps:

   ```bash
   dotnet msbuild src/TerminalDotnet/TerminalDotnet.csproj -t:StampVersionFromCommitCount -getProperty:Version
   ```

2. Tag that commit and push the tag:

   ```bash
   git tag v0.2.10
   git push origin v0.2.10
   ```

3. `.github/workflows/release.yml` checks that the tag matches the stamped
   version, runs the tests and packs the tool. It then exchanges its GitHub
   token for a one-hour nuget.org key, pushes the package, and creates a GitHub
   release with generated notes and the `.nupkg` attached.

To try a package before tagging:

```bash
dotnet pack src/TerminalDotnet/TerminalDotnet.csproj -m:1 -c Release -o ./nupkg
dotnet tool install --global terminal-dotnet --add-source ./nupkg
```

Uninstall it again before installing the published version.

## Sources

- [Microsoft Learn: Create a .NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create)
- [Microsoft Learn: nuget.org trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
- [NuGet/login action](https://github.com/NuGet/login)
- [Omarchy manual: Other packages](https://learn.omacom.io/2/the-omarchy-manual/66/other-packages)
