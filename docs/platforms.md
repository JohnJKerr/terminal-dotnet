# Platform support

terminal-dotnet is a .NET 10 console application built on Terminal.Gui v2, and
it ships as a single .NET tool package for every platform (see
[distribution.md](distribution.md)). The toolkit supports Windows, macOS and
Linux. The app also publishes for `win-x64`, `win-arm64`, `osx-arm64`,
`osx-x64`, `linux-x64` and `linux-arm64` without source changes, and its one
native dependency (`libonigwrap`, for syntax highlighting) ships for all six.
Compiling for a platform is not the same as working on it, though, so this page
records what was checked and what is still open.

Status as of DEV-188 (September 2026):

| Platform | Builds | Unit suite | Used interactively |
| --- | --- | --- | --- |
| Linux (x64, arm64) | Yes | Passing in CI | Yes, daily on Arch/Omarchy |
| macOS (arm64, x64) | Yes | Added to the CI matrix; first results pending | No |
| Windows (x64, arm64) | Yes | Added to the CI matrix; first results pending | No |

## Runtime requirements on every platform

- **A .NET SDK on `PATH`.** The Tests and Issues panels run `dotnet test` and
  `dotnet build`, so a self-contained binary still needs the SDK that the
  solution being inspected builds with.
- **`git` on `PATH`.** The Changes panel, the Updated filters and the file
  listings shell out to git. Outside a repository the file panels fall back to
  the disk and the Changes panel is empty.
- **An editor in `VISUAL` or `EDITOR`.** See the editor notes below.
- **A terminal with 256 colours and Unicode box drawing.** Windows Terminal,
  iTerm2, Terminal.app, Ghostty, Alacritty, kitty, foot and WezTerm all qualify.
  The legacy Windows console host (`conhost.exe`) works but draws poorly.

## What varies by platform

### Terminal driver

Terminal.Gui has `ansi`, `dotnet` and `windows` drivers. On Linux and macOS the
app asks for `dotnet`, because the `ansi` driver's capability negotiation never
completes inside some multiplexers and leaves a blank screen. On Windows it
leaves the choice to Terminal.Gui, whose default there is the native `windows`
driver. `TERMINAL_DOTNET_DRIVER` overrides both.

### Clipboard

Copying runs the first clipboard tool that accepts the text, in this order:
`wl-copy`, `xclip`, `xsel` (Linux), `pbcopy` (macOS), `clip` (Windows). A
missing tool is skipped, and if none accepts the text the app says so rather
than claiming a copy.

Known limitation: `clip.exe` reads standard input in the console code page, so
non-ASCII characters in a comment can be mangled on Windows.

### Editor

The editor command is `VISUAL`, then `EDITOR`, then `omarchy-launch-editor`. It
is split on spaces and called as `<editor> [args] +<line> <path>`.

- **Linux and macOS:** vi, Vim, Neovim, nano, micro, Emacs and Kakoune accept
  `+<line>`. Helix and VS Code do not; wrap them in a script that translates
  the argument.
- **Windows:** this is the weakest area.
  - `omarchy-launch-editor` does not exist, and there is no platform default.
  - Opening a file with no editor configured currently ends the app instead of
    reporting the problem.
  - Editors installed as `.cmd` or `.bat` shims, such as `code` or `subl`,
    cannot be started without a shell. Point `EDITOR` at the `.exe` instead.
  - An editor path containing spaces, such as `C:\Program Files\...`, is split
    apart. Use the short path or put the editor's folder on `PATH`.
  - Notepad does not understand `+<line>`.

### Paths and git

- git reports repository paths with forward slashes, and on Windows
  `git rev-parse --show-toplevel` prints `C:/...`. Every path goes through
  `Path.GetFullPath` before it is compared, which normalises separators.
- Comparisons are ordinal. On Windows a drive letter whose case differs from
  the launch directory (`c:\` against `C:\`) stops a file from matching its git
  status, which loses its colour.
- On macOS, `/var` and `/tmp` are symlinks into `/private`, and git reports the
  resolved path. The same mismatch happens on any platform when the app is
  launched through a symlinked directory.
- `git diff --no-index -- /dev/null <file>` is used to show new files. Git for
  Windows accepts `/dev/null` there, but this needs confirming on a real
  machine.

### Parsing dotnet output

Test discovery and build issues are read from the SDK's human-readable output.
Windows line endings are trimmed, and Windows paths match the issue and stack
trace patterns. The patterns do expect English SDK output, such as `:line 11`.
Set `DOTNET_CLI_UI_LANGUAGE=en` on a localised machine.

## macOS

macOS uses the same NuGet package as every other platform. The SDK builds the
tool's launcher on the user's Mac during `dotnet tool install`, so there is no
downloaded binary for Gatekeeper to quarantine, and no Developer ID signing or
notarisation is needed. `pbcopy` is always present and `EDITOR` is almost
always set, so nothing else is macOS-specific. The macOS CI leg covers the unit
suite; the interactive check is follow-up 5 below.

## Open follow-ups

1. Confirm the macOS and Windows CI legs pass. Many unit tests use POSIX-style
   literals such as `/repo/Shop.sln`, and `Path.GetFullPath` turns those into
   `C:\repo\Shop.sln` on Windows, so expect failures there to fix first.
2. Report a missing editor in the UI instead of letting the exception end the
   app, and choose a platform default editor.
3. Give the editor launcher per-editor line arguments: `+N` for vi-family and
   nano, `-g path:N` for VS Code, `path:N` for Helix, nothing for Notepad.
   Resolve `.cmd` shims on Windows at the same time.
4. Compare paths case-insensitively on Windows and resolve symlinks before
   comparing git output with the launch directory.
5. Try the app by hand in Windows Terminal and in macOS Terminal.app or
   iTerm2: drawing, key handling (`Shift`+letter panel keys, `Ctrl+K`,
   `Ctrl+R`), the editor handoff and return, and the clipboard.
