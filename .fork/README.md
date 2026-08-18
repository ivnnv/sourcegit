# This is a fork

`ivnnv/sourcegit`, a personal fork of [sourcegit-scm/sourcegit](https://github.com/sourcegit-scm/sourcegit).
If you are reading this inside a running SourceGit, it is this build.

## Branches

- `master` tracks upstream 1:1. Never commit here.
- One branch per feature. No `feature/` prefix.
- `daily-driver` is the build actually in use. It is disposable and recreated
  each upgrade: `git checkout -B daily-driver upstream/master`, then a
  `--no-ff` merge of every feature branch.
- `pr/<feature>` is a clean version of a feature cut for an upstream PR. That is
  why this file lives on its own branch: nothing here ever reaches upstream.

Because `daily-driver` is rebuilt from scratch every cycle, anything that must
survive has to live on a feature branch that gets merged back in. A file
committed straight onto `daily-driver` is gone at the next upgrade.

## What this fork adds

Regenerate the live list from the composition merges:

```
git log --merges --pretty='%s' daily-driver ^upstream/master \
  | grep -iE 'into daily-driver$' \
  | sed -E "s/.*'([^']+)' into daily-driver/\1/" | sort -u
```

As of upstream v2026.18:

- `branch-diff` — cumulative branch-vs-base diff, the way a PR shows it.
- `colored-tabs` — per-repo tab colors, custom color picker, tab context menu.
- `commit-refs-copy` — inline copy button on each ref in the commit info panel.
- `custom-branch-sort` — drag to reorder branches in the sidebar.
- `explicit-branches` — the sidebar lists only local branches opened here, so
  agent-created branches stop flooding it. Also adds `--branch <name>` to the
  command line.
- `sidebar-section-reorder` — drag to reorder Histories / Working Copy / Stashes.
- `ui-scaling-prop` — configurable UI scale.

Every fork-only line carries a `// [fork:<branch>]` comment. Search for one to
find every place a feature touches, and to know who owns a merge conflict.

## Command line

```
SourceGit <repo-path> [--branch <name>]
```

`--branch` pins that branch into the sidebar list, so an agent can surface a
branch it just created. The app is a hard singleton: a second launch forwards
its arguments to the running instance over a named pipe and exits.

## Build

```
LIBRARY_PATH=/opt/homebrew/opt/openssl@3/lib:/opt/homebrew/opt/brotli/lib \
dotnet publish src/SourceGit.csproj -c Release -r osx-arm64 --self-contained \
  -o build/SourceGit -p:PublishSingleFile=true
```

Then `build/scripts/package.osx-app.sh`, `codesign --force --deep --sign -`, and
`xattr -dr com.apple.quarantine`.

Run `git submodule update --init --recursive` after every sync from upstream.
Skipping it fails the build with an Avalonia version downgrade error that says
nothing about submodules.

## The upgrade runbook

The full per-feature conflict-resolution history is not in this repo. It lives
in the agent config, which is synced across machines:

`~/Developer/tools/agents-config/shared/claude/memory/project_sourcegit_fork.md`
