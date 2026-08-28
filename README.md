# TerminalDotnet prototype

> Throwaway vertical slice: validate whether navigating a discovered test tree and running the selected subtree is useful.

Run it from a directory containing one `.sln`, `.slnx`, or `.csproj` file:

```bash
dotnet run --project /path/to/terminal-dotnet/src/TerminalDotnet
```

Keys:

- `↑` / `k`: move up
- `↓` / `j`: move down
- `r` / `Enter`: run every test beneath the highlighted project, class, or test
- `R`: rerun the previous test set
- `F`: rerun failed tests
- `c`: cancel the active run
- `o`: open the current failure location in `$EDITOR`
- `q` / `Esc`: quit

Run the prototype's tests with:

```bash
dotnet test /path/to/terminal-dotnet/TerminalDotnet.slnx -m:1
```

The .NET command line is behind `ICommandRunner`. Tests use an in-memory implementation and do not launch live test runs.

## Prototype limits

- Parses the human-readable VSTest `--list-tests` output.
- Infers test project paths from VSTest assembly output.
- Uses exact VSTest `FullyQualifiedName` filters.
- Reads structured results from TRX files.
- Holds all state in memory.
- Uses the built-in console as a replaceable terminal adapter.
