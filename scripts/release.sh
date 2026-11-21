#!/usr/bin/env bash
# Prepare a release: set the version, commit, tag vX.Y.Z and push. Publishes nothing by itself:
# publication is done on demand with the "Release" GitHub workflow.
# Usage: scripts/release.sh <x.y.z>
# Requires: clean working tree on main, a dated CHANGELOG.md entry "## [x.y.z] - YYYY-MM-DD",
# a version greater than the x.y.z part of the current one, and a tag that does not exist yet.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROPS="$ROOT/Directory.Build.props"
CHANGELOG="$ROOT/CHANGELOG.md"
REMOTE="${RELEASE_REMOTE:-origin}"

fail() { echo "release: $*" >&2; exit 1; }

[ $# -eq 1 ] || fail "usage: scripts/release.sh <x.y.z>"
VERSION="$1"
TAG="v$VERSION"
[[ "$VERSION" =~ ^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$ ]] || fail "'$VERSION' is not a release version x.y.z"

cd "$ROOT"

CURRENT="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$PROPS" | head -1)"
[[ "$CURRENT" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)(\.[0-9]+)?$ ]] || fail "could not read the version from Directory.Build.props"
BASE="${BASH_REMATCH[1]}.${BASH_REMATCH[2]}.${BASH_REMATCH[3]}"

greater() { # greater a.b.c x.y.z -> true when the first is greater
  local IFS=.; local -a a=($1) b=($2)
  for i in 0 1 2; do
    (( a[i] > b[i] )) && return 0
    (( a[i] < b[i] )) && return 1
  done
  return 1
}
greater "$VERSION" "$BASE" || fail "the version must be greater than $BASE (current: $CURRENT)"

[ -z "$(git status --porcelain)" ] || fail "the working tree has uncommitted changes"
BRANCH="$(git rev-parse --abbrev-ref HEAD)"
[ "$BRANCH" = "main" ] || fail "releases are made from main (current branch: $BRANCH)"

git rev-parse -q --verify "refs/tags/$TAG" >/dev/null && fail "tag $TAG already exists locally"
if git ls-remote --exit-code --tags "$REMOTE" "refs/tags/$TAG" >/dev/null 2>&1; then
  fail "tag $TAG already exists on $REMOTE"
fi

ESCAPED="${VERSION//./\\.}"
grep -Eq "^## \[$ESCAPED\] - [0-9]{4}-[0-9]{2}-[0-9]{2}[[:space:]]*$" "$CHANGELOG" \
  || fail "CHANGELOG.md has no entry '## [$VERSION] - YYYY-MM-DD'"

# All checks passed: change, commit, tag, push.
sed -i.bak "s:<Version>$CURRENT</Version>:<Version>$VERSION</Version>:" "$PROPS" && rm -f "$PROPS.bak"
grep -q "<Version>$VERSION</Version>" "$PROPS" || fail "could not set the version in Directory.Build.props"

git add "$PROPS"
git commit -q -m "Release $VERSION"
git tag -a "$TAG" -m "Moth-Eidol $VERSION"
git push --atomic "$REMOTE" "$BRANCH" "$TAG"

echo "Released $VERSION ($TAG pushed). Publish it with the Release workflow:"
echo "  gh workflow run release.yml -f version=$VERSION [-f store=true]"
