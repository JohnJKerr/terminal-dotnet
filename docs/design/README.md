# Terminal UI design

The original interactive wireframes are preserved in
[`wireframes/DotNet Sidecar TUI Wireframes.dc.html`](wireframes/DotNet%20Sidecar%20TUI%20Wireframes.dc.html).
Keep `support.js` beside the HTML file when opening it in a browser. The hidden
`.thumbnail` file is the WebP preview supplied with the original artifact.

## Current direction

The implementation deliberately diverges from the original three-column
variant 1b:

- The outer window title is `terminal-dotnet`.
- The layout has two columns: a full-height panel rail on the left and the
  selected panel workspace on the right.
- The Explorer workspace is a searchable, collapsible project, folder, and
  file tree, mirroring the layout on disk. Folders sort before the files
  beside them. Modified files are blue and new files are green.
- The Tests workspace is split vertically into a scrollable test tree and a
  scrollable execution-output pane.
- A one-cell inset separates content from the outer box, with a one-cell gutter
  between the rail and workspace.
- Keyboard shortcuts remain visible along the bottom.
- `Tab` and `←` / `→` move focus directly between the panel rail and
  workspace. `s` focuses search; `↑` / `↓` and `j` / `k` scroll the focused pane.
- Test outcomes are coloured: failed red, passed green, not run white, and
  running cyan. Execution output uses red for failures, green for passes,
  yellow for skips, cyan for status, and the terminal theme for neutral lines.

The Explorer and Tests panels reuse the same two-column shell and replace the
right-hand workspace rather than introducing a permanent third column.
