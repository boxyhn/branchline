# Branchline

Branchline is a macOS-focused visual Git client for reading dense repository
history. It is derived from [SourceGit](https://github.com/sourcegit-scm/sourcegit)
and keeps SourceGit's production Git workflows while presenting branches,
graph lanes, commit messages, authors, and timestamps as one resizable table.

## Highlights

- Compact branch labels aligned with their commits
- Multi-lane commit graph with merge and branch crossings
- Author avatars inside graph nodes and in the author column
- Resizable branch, graph, message, author, and time columns
- Commit details, changed files, and repository tree in one inspector
- Single-click changed-file diff in the main workspace
- Full repository file viewer with `Esc` to return to the graph
- Copyable commit SHA with full-SHA hover details
- Dark macOS chrome with restrained translucent materials
- Clone, fetch, pull, push, merge, rebase, stash, worktree, diff, blame,
  submodule, Git LFS, and the rest of SourceGit's Git feature set

## Install

Download the archive for your Mac from
[GitHub Releases](https://github.com/boxyhn/branchline/releases/latest):

- `osx-arm64`: Apple Silicon Macs
- `osx-x64`: Intel Macs

Unzip the archive and move `Branchline.app` to `/Applications`.

Release builds are ad-hoc signed. If macOS blocks the first launch, remove the
download quarantine attribute and open the app again:

```bash
xattr -dr com.apple.quarantine /Applications/Branchline.app
open /Applications/Branchline.app
```

Branchline requires macOS 13 or later and Git 2.25.1 or later. Application data
is stored in `~/Library/Application Support/Branchline`.

## Build

Install the .NET 10 SDK, then run:

```bash
dotnet publish src/SourceGit.csproj \
  -c Release \
  -o build/Branchline \
  -r osx-arm64

VERSION=$(cat VERSION) \
RUNTIME=osx-arm64 \
bash build/scripts/package.osx-app.sh
```

Use `osx-x64` for an Intel build. Pushing a `v*` tag builds both architectures
and publishes them as a GitHub Release.

## Upstream and License

Branchline is based on SourceGit and preserves its Git implementation, project
history, and MIT license. Upstream changes remain available through the
`sourcegit-scm/sourcegit` fork relationship.

Copyright and permission terms are provided in [LICENSE](LICENSE).
