# Repository Guidance

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
- Add a seam when production and test adapters both exist or behavior genuinely varies.
- Preserve nullable reference type safety and use immutable records for state and messages.

## Tests

- Exercise behavior only through the public interface. Do not expose internals or add `InternalsVisibleTo` for tests.
- Use xUnit.
- Delineate non-empty test sections with `// Arrange`, `// Act`, and `// Assert` comments.
- Name tests as observable behavior. Use `Given{Context}` fixtures and `When{Action}` classes when shared setup makes that structure useful.
- Replace external processes with an in-memory `ICommandRunner`; the unit suite must not launch live `dotnet test` processes.
- Follow vertical TDD slices: one failing behavior, its minimal implementation, then the next behavior.

## Prototype scope

This remains a throwaway vertical slice for validating tree navigation and subtree execution. Keep files, Git views, persistence, and editor integration out until the interaction is validated.
