# Repository Guidance

This file is the single source of truth for every coding agent working in this repository. `CLAUDE.md` imports it; change the guidance here, not there.

## Build and test

```bash
dotnet build TerminalDotnet.slnx -m:1
dotnet test TerminalDotnet.slnx -m:1
dotnet test TerminalDotnet.slnx -m:1 --filter "FullyQualifiedName~TestName"
```

The single-worker option avoids MSBuild worker communication failures in restricted environments.

## Architecture

`TestExplorerSession` owns the test tree, navigation, and run state. Terminal code renders its state and dispatches commands. `ITestBackend` expresses test discovery and execution; `DotnetCliTestBackend` adapts those operations to `dotnet test`. `ICommandRunner` is the external command-line seam.

Keep terminal framework types and process details outside the explorer module.

## Coding standards

- Prefer flat transformations and guard clauses. Express hierarchical mapping with named functions and `SelectMany`; keep loop bodies at one level of abstraction.
- Keep methods focused on one operation and name extracted transformations after the domain result they produce.
- Name the operation. If a comment is what says what the code is doing, extract a method whose name says it, and delete the comment.
- Add a seam when production and test adapters both exist or behavior genuinely varies.
- Preserve nullable reference type safety and use immutable records for state and messages.

## Working with `dotnet` and other processes

- Fix the cause, not its symptom. If a change makes something worse, undo or narrow that change rather than adding logic that compensates for it. For example, run `dotnet test --list-tests` with `-m:1` so each project's list follows its own header; don't guess a test's project from its namespace.
- Bound what a process can cost while it is running, not afterwards. Keep only the tail of output you will show (`CommandRequest.OutputLimit`), and give anything that holds a shared gate a time limit.
- Give each path to the same operation the same guards. Startup and rebuild both skip discovery after a failed build; a new entry point must do the same.
- Apply progress updates as they are reported. `Progress<T>` queues each update on the thread pool, so state can show an update late or after the work has finished, and a test can read it before it lands. Use a synchronous `IProgress<T>` in production and `Fakes/RecordedProgress` in tests.
- Parse SDK output defensively. Paths can contain spaces and parentheses, so anchor on the end of a line (for example, the last ` (`) rather than the first match.

## Tests

- Exercise behavior only through the public interface. Do not expose internals or add `InternalsVisibleTo` for tests, and do not make a helper public just so a test can call it.
- Use xUnit.
- Keep one assertion call per test. Split distinct observations into separate behavior tests.
- Delineate non-empty test sections with `// Arrange`, `// Act`, and `// Assert` comments.
- Name tests as observable behavior, preferring names such as `It_opens_the_file`. Group suites by what they cover, in a folder and matching namespace such as `Tests`, `Build`, `Files`, `Keyboard`, `Presentation` or `Workspace`, and name suite classes after their behavioral context, such as `WhenOpeningAFile`, with behaviors such as `Pressing_enter_opens_it`.
- Arrange shared state through the builders in `Builders/` (start from `GivenA`) and the fakes in `Fakes/`. A new test double goes in `Fakes/`, not in a private class inside a suite.
- Replace external processes with an in-memory `ICommandRunner`; the unit suite must not launch live `dotnet test` processes. Only the `Workspace` suites that cover `ProcessCommandRunner` itself may start a real shell.
- Follow vertical TDD slices: one failing behavior, its minimal implementation, then the next behavior. See the test fail before changing production code, and run the full suite before each commit.

## Product scope

The tree interaction has been validated and the project is now moving into the actual implementation. The Tests and Explorer panels share the two-column shell and common interaction conventions. Files, Git status, and editor integration are in scope.

## Commits and pull requests

- Prefix commit subjects with the feature area, for example: `Run Tests: Ensure single test can run`.
- Keep each commit to one behavior, small enough to review at a glance.
- Do not sign commits or pull requests. Leave out `Co-Authored-By`, `Co-authored-by`, `Claude-Session` and any other generated-by or attribution trailer, even when your tool adds one by default: the commit message ends with its own last line.
- A pull request description covers every commit on the branch. When you add commits, update the description.
