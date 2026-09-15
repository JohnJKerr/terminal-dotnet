# Security audit

Scope: the terminal-dotnet application (`src/TerminalDotnet`), its build and
install scripts, its NuGet dependencies, and its GitHub Actions workflows, at
branch `kerrjohn/dev-188-cross-platform-windowsmac` (DEV-188), September 2026.

Method: every point where outside data or processes cross into the app was
traced from where it is produced to where it ends up. Each crossing was checked
for trust, size, deadline, parsing and rendering behaviour. Dependencies were
checked with `dotnet list package --vulnerable --include-transitive`.

## Threat model

terminal-dotnet is a local, single-user developer tool. It makes no network
calls of its own, collects no telemetry, stores no credentials and keeps
comments in memory until the user saves them. The realistic attacker is
**the author of a repository the user opens**: a cloned open-source project, a
pull request branch or a downloaded sample. Everything read from the working
tree is treated as untrusted. The user's own environment (`VISUAL`, `EDITOR`,
`TERMINAL_DOTNET_DRIVER`, `PATH`) is trusted.

## Summary

| # | Finding | Severity | Status |
| --- | --- | --- | --- |
| 1 | Opening a repository builds it, which runs the repository's code | High, inherent | **Documented**; trust prompt proposed |
| 2 | Saving comments followed a planted symbolic link and overwrote its target | Medium | **Fixed** |
| 3 | GitHub Actions were referenced by mutable tags, with default token permissions | Medium | **Fixed** |
| 4 | Git runs with repository-local configuration | Low | Follow-up |
| 5 | Previews and flag scans read whole files and do not skip special files | Low | Follow-up |
| 6 | Subprocess output is captured without a size limit | Low | Accepted |
| 7 | Discovered test names are put into the `--filter` expression unescaped | Low | Follow-up |
| 8 | Terminal escape sequences in untrusted text | Low | Mitigated by Terminal.Gui; adversarial test pending |
| 9 | NuGet restores are not locked | Low | Follow-up |
| 10 | Symbolic link loops while walking source folders | Info | Regression test added |
| 11 | XML parsing (`.slnx`, TRX) | Info | Pass |
| 12 | Command construction and shell injection | Info | Pass |
| 13 | Dependency vulnerabilities | Info | Pass, none known |
| 14 | `install.sh` | Info | Pass |

## Findings

### 1. Opening a repository builds it, which runs the repository's code

At launch the Issues panel runs `dotnet build` and the Tests panel runs
`dotnet test --list-tests`. Both run again when files change and when a stale
panel is opened. MSBuild runs whatever the repository asks for: custom targets
in `Directory.Build.props` and `.targets`, analyzers, source generators, and
NuGet packages from any feed its `NuGet.config` names. **Starting
terminal-dotnet in a repository is equivalent to running `dotnet build` in it.**

This is inherent to the product, and the same is true of `dotnet build`, VS
Code's C# extension and Rider. Users need to know it, though, and nothing in
the app said so.

- Done: the README's *Security* section states it plainly.
- Proposed follow-up: ask before the first build of a folder that is not yet
  trusted, as VS Code's Workspace Trust does, and remember the answer per
  repository root. Offer a `--no-build` launch that keeps the file, Changes,
  Flags and Comments panels and loads Tests and Issues on demand.

### 2. Saving comments followed a planted symbolic link (fixed)

`w` suggests `comments.md` beside the solution. `FileCommentStore` wrote with
`File.WriteAllTextAsync`, which follows symbolic links. A repository could
commit `comments.md` as a link to `~/.bashrc`. The overwrite prompt would ask
about "comments.md", and confirming it replaced the user's shell profile.

Fix: the text is written to a new, uniquely named sibling file opened with
`FileMode.CreateNew`, which never follows a link, and then renamed over the
destination. A rename replaces the link itself and leaves its target untouched.
It also means a failed save can no longer leave a half-written file.
Covered by `WhenSavingTheCommentsToDisk.It_leaves_alone_the_file_a_symbolic_link_points_at`.

### 3. GitHub Actions supply chain (fixed)

`build.yml` used `actions/checkout@v4` and `actions/setup-dotnet@v4`, which are
tags that can be moved. It also ran with the repository's default token
permissions and left the token in the checkout's git config.

Fix:
- Every action is pinned to a full commit SHA, with the release named in a
  comment.
- Workflows default to `permissions: contents: read`.
- `persist-credentials: false` is set on checkout.
- The new release workflow splits in two:
  - The job that runs the tests and packs the tool stays read-only.
  - A separate `publish` job, gated by the `nuget` environment, alone gets
    `contents: write` and `id-token: write`.
- Publishing uses nuget.org trusted publishing: the workflow exchanges its
  GitHub token for a single-use key that lasts one hour, so no long-lived NuGet
  API key exists in the repository or its secrets.
- The workflow passes the tag and the key to scripts through environment
  variables rather than `${{ }}` interpolation, and refuses a tag that does not
  match the stamped version.

Recommended next: enable Dependabot for `github-actions` and `nuget`, so the
pins are kept current.

### 4. Git runs with repository-local configuration

`git status`, `ls-files`, `diff` and `restore` run inside the repository. A
cloned repository cannot ship its own `.git/config`. A directory unpacked from
an archive or shared by another user can, though, and settings such as
`core.fsmonitor` run commands. Git's `safe.directory` check blocks
repositories owned by another user, but not archives the user unpacked.

Follow-up: pass `-c core.fsmonitor=false` to every git call, and add
`--no-ext-diff --no-textconv` to `git diff`. This is cheap hardening that
changes nothing a user sees.

### 5. Unbounded and special-file reads

The preview reads the whole file with `File.ReadAllText`. The Flags panel reads
every listed file line by line, and test source lookup reads every `.cs` file
under a project. Outside a git repository the file listing comes from the disk
and can include a named pipe (FIFO). Reading one blocks forever, which stalls
that panel's load and then the wait at quit. A multi-gigabyte file makes the
preview allocate that much memory.

Follow-up: skip anything that is not a regular file, and cap the preview (for
example at 10 MB) with a message offering the editor instead.

### 6. Subprocess output is captured without a limit

`ProcessCommandRunner` reads standard output and error with `ReadToEndAsync`
and applies no deadline. On cancellation it does kill the whole process tree,
and quitting waits for the panels' work, so nothing is orphaned. The producers
are the user's own `dotnet` and `git`, and output only grows as large as the
build log. Accepted for a local tool; revisit if the app ever runs commands
unattended.

### 7. Test names in the `--filter` expression

Runs pass `FullyQualifiedName=<name>` joined by `|`. VSTest filter syntax gives
meaning to `|`, `&`, `!`, `(`, `)`, `~` and `=`, and names come from the
repository's own test discovery. A crafted name can widen or break the filter,
so the wrong tests run. It cannot escape `dotnet test`, since arguments are
passed without a shell. Follow-up: escape those characters with `\`, as
VSTest's filter syntax documents.

### 8. Terminal escape sequences in untrusted text

File contents, diffs, compiler messages, test output and file names all reach
the screen. Terminal.Gui draws into a cell buffer, and its
`MakePrintable` API translates C0 and C1 control characters into visible
control pictures before output. Test output goes through `AnsiTestOutput` as
well, which removes OSC and cursor-moving CSI sequences and interprets only
colour (SGR) codes. That should stop a file from sending title changes,
hyperlinks or clipboard writes (OSC 52) to the user's terminal.

Unverified: no test yet feeds a file containing raw `ESC`, `BEL` and OSC 52
bytes through the preview and inspects what reaches the driver. Until that
exists this counts as mitigated, not proven.

### 9. NuGet restores are not locked

No `packages.lock.json` exists, so a restore resolves whatever the feed serves
for the pinned versions. Follow-up: set `RestorePackagesWithLockFile`, commit
the lock files, and restore with `--locked-mode` in CI.

### 10. Symbolic link loops (info)

A folder linking back to its parent could in principle trap the recursive walk
used for test source lookup and the non-git file listing. On Linux, .NET's
directory enumeration does not descend through the loop, and a regression test
now pins that down:
`WhenLocatingTestSource.It_gives_up_on_a_missing_test_when_a_folder_links_back_to_its_parent`.
The Windows and macOS CI legs will show whether junctions behave the same way.

### 11. XML parsing (pass)

`.slnx` files are loaded with `XDocument.Load` and TRX results with
`XDocument.Parse`. On .NET these use `XmlReader` defaults: DTD processing is
prohibited and there is no URL resolver. External entities and entity expansion
bombs are rejected.

### 12. Command construction (pass)

Every process starts with `UseShellExecute = false` and arguments passed
through `ProcessStartInfo.ArgumentList`, so nothing goes through a shell.
- Git paths come after `--` and use `:(literal)` pathspecs, so a file named
  `*.cs` touches only itself.
- The editor receives an absolute path, which cannot be read as an option.
- Clipboard text goes over standard input rather than on the command line.

### 13. Dependencies (pass)

`dotnet list package --vulnerable --include-transitive` reports no known
vulnerabilities (15 September 2026). The CI build now runs this check on every
platform. Newer versions are available: Terminal.Gui 2.5.0,
Microsoft.NET.Test.Sdk 18.10.1 and xunit.runner.visualstudio 4.0.0. Update
them in a separate change, since Terminal.Gui minor releases have changed
behaviour before.

### 14. `install.sh` (pass)

It runs under `set -euo pipefail`. It publishes to a staging directory and
swaps it into place, and deletes only `<prefix>/libexec/terminal-dotnet` and
`<prefix>/bin/terminal-dotnet`. It rejects an empty `--prefix`.

## Temporary files

TRX results are written to the system temp directory as
`terminal-dotnet-<random GUID>.trx`, read once and deleted. The name cannot be
guessed, so another local user cannot plant a file or link there in advance.

## Out of scope

- The security of the .NET SDK, MSBuild, git, and the user's editor and
  terminal emulator.
- Code signing and notarisation. The app ships only as a framework-dependent
  .NET tool, whose launcher the SDK creates on the user's machine, so there is
  no downloaded executable to sign. nuget.org signs the package itself.
