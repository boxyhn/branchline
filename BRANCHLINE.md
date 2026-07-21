# Branchline

Branchline is a macOS-focused visual Git client derived from SourceGit. It keeps
SourceGit's production Git workflows while presenting repository history with a
GitKraken-inspired dark layout: branch and tag labels on the left, a wide lane
graph in the center, commit metadata by row, and functional details on the
right.

The commit inspector mirrors GitKraken's read and edit flow: the message card
opens into an inline editor, reports the affected rebase range, and applies a
real interactive-rebase reword. Author identity, parent navigation, change
summary, path/tree controls, and one-click diff opening remain in the same
context.

## Build for macOS

```bash
dotnet publish src/SourceGit.csproj -c Release -o build/Branchline -r osx-arm64
VERSION=$(cat VERSION) RUNTIME=osx-arm64 bash build/scripts/package.osx-app.sh
```

The generated app and archive are `build/Branchline.app` and
`build/branchline_<version>.osx-arm64.zip`. Replace `osx-arm64` with `osx-x64`
for Intel Macs.

Pushing a `v*` tag builds both architectures and publishes the archives to
[GitHub Releases](https://github.com/boxyhn/branchline/releases).

Branchline releases follow their own semantic version sequence beginning at
`1.0.0`; the SourceGit version is treated only as upstream provenance.

## Attribution

Branchline is based on [SourceGit](https://github.com/sourcegit-scm/sourcegit)
and remains available under the repository's MIT license. SourceGit copyright
and license terms are preserved in `LICENSE`.
