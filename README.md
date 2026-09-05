# TerminalDotnet prototype

> Throwaway vertical slice: validate whether navigating a discovered test tree and running the selected subtree is useful.

## Install

```bash
./install.sh
```

That publishes the app to `~/.local/libexec/terminal-dotnet` and puts a `terminal-dotnet`
command in `~/.local/bin`. Run it from any directory containing one `.sln`, `.slnx`, or
`.csproj` file:

```bash
cd /path/to/some/repository
terminal-dotnet
```

The install is framework-dependent, so it uses the .NET SDK already on your PATH — the same
one it shells out to for `dotnet test`. Pass `--self-contained` to bundle the runtime instead,
and `--prefix DIR` to install somewhere other than `~/.local`.

### Upgrading

Re-run the installer. It publishes to a staging directory and swaps it into place, so there is
no need to uninstall first:

```bash
git pull
./install.sh
```

### Uninstalling

```bash
./install.sh --uninstall
```

That removes the `terminal-dotnet` command and the published application directory, and prints
both paths as it goes. It leaves `~/.local/bin` and `~/.local/libexec` in place, because other
programs live there.

Pass the same `--prefix DIR` you installed with, otherwise the uninstall looks in `~/.local`
and finds nothing to remove:

```bash
./install.sh --prefix /opt/tools --uninstall
```

If you no longer have the repository, delete the two paths by hand:

```bash
rm -f ~/.local/bin/terminal-dotnet
rm -rf ~/.local/libexec/terminal-dotnet
```

### Terminal driver

Terminal.Gui offers `ansi`, `dotnet`, and `windows` drivers, and picks `ansi` on Linux by
default. That driver negotiates terminal capabilities over escape sequences, and the
negotiation does not complete under every terminal — inside the [herdr](https://herdr.dev)
multiplexer it leaves a blank screen and never draws a frame. This app therefore asks for the
`dotnet` driver, which renders through `System.Console` and needs no negotiation.

Override it when you want a different driver:

```bash
TERMINAL_DOTNET_DRIVER=ansi terminal-dotnet
```

If a run ever does leave the terminal blank, `Ctrl-C` can drop you back to a shell where
`Enter` types a literal `u`: the abandoned driver left the kitty keyboard protocol enabled.
Run `reset` to restore the terminal.

Without installing, run it straight from the source tree:

```bash
dotnet run --project /path/to/terminal-dotnet/src/TerminalDotnet
```

Panels: `Explorer` lists the solution's source files, `Tests` lists the discovered tests, and
`Changes` lists the files git reports as added, modified, or deleted beneath the directory you
started in. A panel with nothing to list says so in place of its rows.

Keys:

- `↑` / `k`: move up
- `↓` / `j`: move down
- `s`: search the active panel; `Enter` returns to the tree; `n` / `N` select matches; `Esc` clears the search
- `←` / `→`: move directly between the panel rail and workspace
- `Space`: collapse or expand the highlighted project or class
- `Enter` / `d` (Changes): show the highlighted file's diff
- `r` (Changes): restore the highlighted deleted file
- `r` / `Enter`: run every test beneath the highlighted project, class, or test
- `R`: rerun the previous test set
- `F`: rerun failed tests
- `]f`: select the next failed test
- `c`: cancel the active run
- `p`: preview the current test or failure location
- `q` / `Esc`: quit

Run the prototype's tests with:

```bash
dotnet test /path/to/terminal-dotnet/TerminalDotnet.slnx -m:1
```

The .NET command line is behind `ICommandRunner`. Tests use an in-memory implementation and do not launch live test runs.

## Failure-to-source demo

The demo project is deliberately excluded from the solution because its test always fails. From the repository root, restore the demo project once:

```bash
dotnet restore samples/TerminalDotnet.DemoTests/TerminalDotnet.DemoTests.csproj
```

Then launch the prototype from the demo directory with an editor configured:

```bash
cd samples/TerminalDotnet.DemoTests
EDITOR=nvim dotnet run --project ../../src/TerminalDotnet/TerminalDotnet.csproj
```

Run `Opening a failure in the configured editor`, wait for the `Source:` excerpt, then press `p`. The preview should open `FailureDemoTests.cs` at the failing assertion.

To verify the fixture without the terminal UI, run this from the repository root. A failed test at `FailureDemoTests.cs:line 11` is the expected result:

```bash
dotnet test samples/TerminalDotnet.DemoTests/TerminalDotnet.DemoTests.csproj -m:1
```

## Prototype limits

- Parses the human-readable VSTest `--list-tests` output.
- Infers test project paths from VSTest assembly output.
- Uses exact VSTest `FullyQualifiedName` filters.
- Reads structured results from TRX files.
- Holds all state in memory.
- Uses the built-in console as a replaceable terminal adapter.
