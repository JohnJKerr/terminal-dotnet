#!/usr/bin/env bash
#
# Moves to a new major or minor version, taking the patch back to zero.
#
#   ./bump-version.sh minor      0.1.x -> 0.2.0
#   ./bump-version.sh major      0.1.x -> 1.0.0
#   ./bump-version.sh minor --no-commit
#                                edit Directory.Build.props and stop, leaving
#                                the commit to you
#
# The patch counts commits since the offset, so the offset has to be the count
# the bump commit itself carries. This script commits the bump to guarantee
# that, and verifies the count afterwards.
#
set -euo pipefail

readonly PROPS="Directory.Build.props"

usage() {
    sed -n '2,/^set -euo/p' "$0" | sed 's/^# \{0,1\}//; $d'
}

part=""
commit="true"

while [ "$#" -gt 0 ]; do
    case "$1" in
        major|minor) part="$1"; shift ;;
        --no-commit) commit="false"; shift ;;
        -h|--help) usage; exit 0 ;;
        *) echo "Unknown option: $1" >&2; usage >&2; exit 2 ;;
    esac
done

[ -n "$part" ] || { echo "Say which part to bump: major or minor." >&2; usage >&2; exit 2; }

cd -- "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

git rev-parse --git-dir >/dev/null 2>&1 || {
    echo "Not a git repository, so the patch count has nothing to count." >&2
    exit 1
}

[ -z "$(git status --porcelain)" ] || {
    echo "The working tree has changes. Commit or stash them first, so the" >&2
    echo "bump lands as a commit of its own." >&2
    exit 1
}

read_property() {
    sed -n "s|.*<$1>\([0-9]\{1,\}\)</$1>.*|\1|p" "$PROPS"
}

readonly old_major="$(read_property VersionMajor)"
readonly old_minor="$(read_property VersionMinor)"

[ -n "$old_major" ] && [ -n "$old_minor" ] || {
    echo "Could not read the current version out of $PROPS." >&2
    exit 1
}

if [ "$part" = "major" ]; then
    new_major="$((old_major + 1))"
    new_minor="0"
else
    new_major="$old_major"
    new_minor="$((old_minor + 1))"
fi
readonly new_major new_minor

# Counts are inclusive, so the commit made below is the current count plus one.
readonly offset="$(( $(git rev-list --count HEAD) + 1 ))"

sed -i -E \
    -e "s|<VersionMajor>[0-9]+</VersionMajor>|<VersionMajor>$new_major</VersionMajor>|" \
    -e "s|<VersionMinor>[0-9]+</VersionMinor>|<VersionMinor>$new_minor</VersionMinor>|" \
    -e "s|<VersionPatchOffset>[0-9]+</VersionPatchOffset>|<VersionPatchOffset>$offset</VersionPatchOffset>|" \
    "$PROPS"

echo "$old_major.$old_minor -> $new_major.$new_minor.0 (patch offset $offset)"

if [ "$commit" = "false" ]; then
    echo
    echo "Left uncommitted. The offset only holds if the bump is the very next"
    echo "commit; otherwise rerun this script."
    exit 0
fi

git add -- "$PROPS"
git commit --quiet --message "Build: Move to $new_major.$new_minor"

readonly counted="$(git rev-list --count HEAD)"
[ "$counted" = "$offset" ] || {
    echo "Expected the bump commit to be number $offset but it is $counted." >&2
    echo "Fix VersionPatchOffset in $PROPS before building." >&2
    exit 1
}

echo "Committed as $(git rev-parse --short HEAD)."
