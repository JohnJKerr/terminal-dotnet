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
`--prefix DIR` to install somewhere else, and `--uninstall` to remove it. Re-run `./install.sh`
to upgrade an existing install.

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
