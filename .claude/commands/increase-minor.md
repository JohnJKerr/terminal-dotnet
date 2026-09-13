---
description: Move to the next minor version, taking the patch back to zero
allowed-tools: Bash(./bump-version.sh:*), Bash(git show:*), Bash(git status:*)
---

Run `./bump-version.sh minor` from the repository root.

Then report the new version and the commit it made. If the script refuses
because the working tree is dirty, say so and stop — do not commit or stash
anything on the user's behalf.
