# Terminal UI design

The original interactive wireframes are preserved in
[`wireframes/DotNet Sidecar TUI Wireframes.dc.html`](wireframes/DotNet%20Sidecar%20TUI%20Wireframes.dc.html).
Keep `support.js` beside the HTML file when opening it in a browser. The hidden
`.thumbnail` file is the WebP preview supplied with the original artifact.

## Current direction

The layout follows the panel-based design, "Terminal Dotnet Layout", from the
project's Claude Design workspace:

- Every panel is on the screen at once. The Explorer, Tests and Changes are
  stacked on the left; the Preview takes the rest of the width, over the Issues
  and the Comments.
- The list on the left that the reader was last in stretches to show more of
  its rows.
- Each panel is reached by the number in its title (`0` Preview, `1` Explorer,
  `2` Tests, `3` Changes, `4` Issues, `5` Comments), or by `Tab` and
  `Shift+Tab` in that on-screen order.
- Filters are toggled with capital letters and named in the panel's title; the
  focused panel spells each one out, the others show only the letters and any
  filter in use. One filter is active per panel at a time.
- The Explorer and the former Files panel are one panel, with `A` for every
  file beneath the launch folder. The Issues and the former Flags panel are
  one list, with `X`, `W` and `F` for errors, warnings and flags.
- The Preview follows the selection of the last list and shows a change's diff
  in place, replacing the full-screen preview and diff dialogs.
- `/` searches the focused panel. The keys for the focused panel and its
  selection are listed along the bottom.
