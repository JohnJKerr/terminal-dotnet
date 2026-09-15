# Security audit

Scope: the terminal-dotnet application (`src/TerminalDotnet`), its build and
install scripts, its NuGet dependencies, and its GitHub Actions workflows, on
branch `kerrjohn/dev-188-cross-platform-windowsmac` (DEV-188), September 2026.
The first pass recorded the findings; a hardening pass on the same branch then
addressed them. Each finding's status reflects that pass.

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
`TERMINAL_DOTNET_DRIVER`, `PATH`) and configuration folder are trusted.

## Summary

| # | Finding | Severity | Status |
| --- | --- | --- | --- |
| 1 | Opening a repository builds it, which runs the repository's code | High, inherent | **Mitigated**: trust prompt; restricted mode tracked in DEV-192 |
| 2 | Saving comments followed a planted symbolic link and overwrote its target | Medium | **Fixed** |
| 3 | GitHub Actions were referenced by mutable tags, with default token permissions | Medium | **Fixed** |
| 4 | Git runs with repository-local configuration | Low | **Fixed** for the command-running settings; residual accepted |
| 5 | Previews and flag scans read whole files and do not skip special files | Low | **Fixed** |
| 6 | Subprocess output is captured without a size limit | Low | Accepted |
| 7 | Discovered test names are put into the `--filter` expression unescaped | Low | **Fixed** |
| 8 | Terminal escape sequences in untrusted text | Low | **Pass**, pinned by tests |
| 9 | NuGet restores are not locked | Low | **Fixed** |
| 10 | Symbolic link loops while walking source folders | Info | Regression test added |
| 11 | XML parsing (`.slnx`, TRX) | Info | Pass |
| 12 | Command construction and shell injection | Info | Pass |
| 13 | Dependency vulnerabilities | Info | Pass, none known |
| 14 | `install.sh` | Info | Pass |
| 15 | The trust store | Info | Pass |

## Findings

### 1. Opening a repository builds it, which runs the repository's code (mitigated)

At launch the Issues panel runs `dotnet build` and the Tests panel runs
`dotnet test --list-tests`. Both run again when files change, and when a stale
panel is opened. MSBuild runs whatever the repository asks for: custom targets
in `Directory.Build.props` and `.targets`, analyzers, source generators, and
NuGet packages from any feed its `NuGet.config` names.

This is inherent to the product, and the same is true of `dotnet build`, VS
Code's C# extension and Rider. The app must not do it without the user's
consent.

Mitigation: `WorkspaceTrust` and `TrustPrompt`.
- **Order.** `Program.cs` resolves the repository root and checks it against
  the user's trusted folders before any panel, watcher or build is created.
- **Prompt.** An untrusted folder gets a prompt naming the folder and what
  building it runs. Only **Trust folder** continues; **Quit**, `Esc` and a
  stray `Enter` (the default button) exit with code 1 without building.
- **Scope.** Trust is remembered per repository root, or per launch folder
  outside git. Matching is exact (case-insensitive on Windows), so trusting a
  folder never extends to a separate repository nested inside it.
- **Tests:** `WhenTrustingAFolder`. The prompt was driven end to end in a real
  terminal: quitting, `Esc`, trusting, and relaunching a trusted folder.

Residual:
- `git rev-parse` runs before the prompt, to find the repository root. It is
  hardened as described in finding 4.
- Declining exits instead of opening a restricted view. DEV-192 tracks a
  restricted mode that keeps the panels that never run `dotnet`.

### 2. Saving comments followed a planted symbolic link (fixed)

`w` suggests `comments.md` beside the solution. `FileCommentStore` wrote with
`File.WriteAllTextAsync`, which follows symbolic links. A repository could
commit `comments.md` as a link to `~/.bashrc`. The overwrite prompt would ask
about "comments.md", and confirming it replaced the user's shell profile.

Fix: `FileReplacement`, now shared by the comment store and the trust store.
- It writes a new, uniquely named sibling file opened with
  `FileMode.CreateNew`, which never follows a link.
- It then renames that file over the destination. A rename replaces the link
  itself and leaves its target untouched.
- A failed save no longer leaves a half-written file.

Covered by `WhenSavingTheCommentsToDisk`.

### 3. GitHub Actions supply chain (fixed)

`build.yml` used `actions/checkout@v4` and `actions/setup-dotnet@v4`, which are
tags that can be moved. It also ran with the repository's default token
permissions and left the token in the checkout's git config.

Fix:
- **Pinning.** Every action is pinned to a full commit SHA, with the release
  named in a comment.
- **Token.** Workflows default to `permissions: contents: read`, and
  `persist-credentials: false` is set on checkout.
- **Release jobs.** The release workflow splits in two:
  - The job that runs the tests and packs the tool stays read-only.
  - A separate `publish` job, gated by the `nuget` environment, alone gets
    `contents: write` and `id-token: write`.
- **Publishing.** nuget.org trusted publishing exchanges the workflow's GitHub
  token for a single-use key that lasts one hour. No long-lived NuGet API key
  exists in the repository or its secrets.
- **Script inputs.** The workflow passes the tag and the key to scripts through
  environment variables rather than `${{ }}` interpolation, and refuses a tag
  that does not match the stamped version.

Recommended next: enable Dependabot for `github-actions` and `nuget`, so the
pins are kept current.

### 4. Git runs with repository-local configuration (fixed, with a residual)

`git status`, `ls-files`, `diff`, `restore` and `rev-parse` run inside the
repository. A cloned repository cannot ship its own `.git/config`, but a
directory unpacked from an archive, or shared by another account, can.

Fix:
- Every git call is built by `GitRequest` with `-c core.fsmonitor=false`, so a
  configured file-system monitor never runs.
- Diffs also pass `--no-ext-diff --no-textconv`, so an external diff program or
  a textconv driver is never started.

Covered by `WhenDiscoveringChangedFiles` and `WhenDiscoveringFolderFiles`.

Residual, accepted: clean and smudge filters named in a repository-local
config, together with the repository's `.gitattributes`, still run during
`status`, `diff` and `restore`. Filter names are arbitrary, so they cannot be
switched off one by one. Git's `safe.directory` check already refuses
repositories owned by another user, and a clone cannot carry the config.

### 5. Unbounded and special-file reads (fixed)

The preview read whole files with `File.ReadAllText`. The Flags panel read
every listed file, and test source lookup read every `.cs` file under a
project. Outside git, the listing can include a named pipe; opening one blocks
until something writes to it. A stack trace can name a device such as
`/dev/zero`, which never runs out.

Fix: `FileText` is now the one way the app reads a file it shows or scans.
- **Special files.** A file reporting zero length is returned as empty text
  without being opened. Named pipes and character devices report zero, so they
  are never opened.
- **Size.** A file over 10 MB is declined, and the limit is enforced again while
  reading, in case the file grows in between.
- **Preview.** A declined file shows a message suggesting the editor instead.
- **Flags and source lookup.** They pass over declined files.

Covered by `WhenReadingAFileForDisplay`, `WhenBrowsingTheFlags` and
`WhenLocatingTestSource`.

Residual, accepted: a file swapped for a named pipe between being measured and
being opened would still block. That needs a process racing the app on the
same machine.

### 6. Subprocess output is captured without a limit (accepted)

`ProcessCommandRunner` reads standard output and error with `ReadToEndAsync`
and applies no deadline. On cancellation it does kill the whole process tree,
and quitting waits for the panels' work, so nothing is orphaned. The producers
are the user's own `dotnet` and `git`, and the output grows only as large as the
build log. Accepted for a local tool; revisit if the app ever runs commands
unattended.

### 7. Test names in the `--filter` expression (fixed)

Runs pass `FullyQualifiedName=<name>` joined by `|`, with names taken from the
repository's own test discovery. A name containing filter syntax could widen
or break the filter.

Fix: each name is escaped as the VSTest filter syntax documents:
- `\`, `(`, `)`, `&`, `|`, `=`, `!` and `~` are prefixed with `\`.
- A generic type's `,` becomes `%2C`.

Covered by `WhenUsingTheDotnetCliTestBackend.It_escapes_filter_syntax_in_a_test_name`.

### 8. Terminal escape sequences in untrusted text (pass)

File contents, diffs, compiler messages, test output and file names all reach
the screen. Every character the app draws goes through Terminal.Gui's output
buffer. It translates C0 and C1 control characters into visible control
pictures before anything is written to the terminal, so ESC becomes `␛`.
Test output additionally goes through `AnsiTestOutput`, which removes OSC and
cursor-moving CSI sequences and interprets only colour (SGR) codes.

`WhenDrawingUntrustedText` pins the toolkit's behaviour. An OSC 52 clipboard
write, a BEL and a C1 CSI drawn into the buffer leave no control character
behind, so a Terminal.Gui upgrade that changed this would fail the suite.

### 9. NuGet restores are locked (fixed)

Fix:
- `RestorePackagesWithLockFile` is on for every project, and the
  `packages.lock.json` files, which record content hashes, are committed.
- CI and the release workflow restore with `--locked-mode`, so a package that
  changed on the feed fails the build.
- `install.sh --self-contained` restores for a single runtime, which would
  rewrite the lock file. It records that restore's lock file in a temporary
  folder instead.

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

Every process starts with `UseShellExecute = false`, and arguments are passed
through `ProcessStartInfo.ArgumentList`, so nothing goes through a shell.
- **Git.** Paths come after `--` and use `:(literal)` pathspecs, so a file named
  `*.cs` touches only itself. Git settings come first, as described in
  finding 4.
- **Editor.** It receives an absolute path, which cannot be read as an option.
- **Clipboard.** Text goes over standard input, not on the command line.

### 13. Dependencies (pass)

`dotnet list package --vulnerable --include-transitive` reports no known
vulnerabilities (15 September 2026). The CI build now runs this check on every
platform. Newer versions are available: Terminal.Gui 2.5.0,
Microsoft.NET.Test.Sdk 18.10.1 and xunit.runner.visualstudio 4.0.0. Update
them in a separate change, since Terminal.Gui minor releases have changed
behaviour before.

### 14. `install.sh` (pass)

It runs under `set -euo pipefail`. It publishes to a staging directory, swaps
that into place, and deletes only `<prefix>/libexec/terminal-dotnet` and
`<prefix>/bin/terminal-dotnet`. It rejects an empty `--prefix`.

### 15. The trust store (pass)

Trusted folders live in `trusted-folders` under the user's configuration
folder (`ApplicationData`), one absolute path per line. The folder belongs to
the user and is outside any repository, and the store is treated as the
authority it is:
- **Fails closed.** A store over 1 MB, or one that cannot be read or parsed,
  trusts nothing, so the user is asked again.
- **Writes.** They go through `FileReplacement` (finding 2).
- **Save failures.** A save that fails still trusts the folder for the current
  launch, which the user did consent to. After the app closes, a message says
  the answer was not remembered.

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
