# Needs checking — Group RG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group RG - Heron's plugin from its own marketplace: the first release that fills plugin-release, and an install on a clean Windows user (2026-11-28)

**Tracked as GitHub issue [#1531](https://github.com/Ajmalpshaik/Heron-AI/issues/1531) since 2026-11-28** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [D-212](../decisions/D-212.md) publishes Heron's plugin from a `plugin-release` branch of this repository.
[`.claude-plugin/marketplace.json`](../../.claude-plugin/marketplace.json) on `main` sends Claude Code there, and
[`plugin-release.yml`](../../.github/workflows/plugin-release.yml) adds one commit to the branch when a person
publishes a release - section 11 of [the design](../work-notes/plans/claude-code-plugin-2026-11-27.md).

**What is done and what is not.** Run on 2026-11-28 in a Linux cloud container, Claude Code **2.1.332**, with no
sign-in: `claude plugin validate --strict` passed on the marketplace, on a scratch marketplace pointing at the
branch's layout, and on that layout. On a throwaway repository with the marketplace on `main` and `plugin-release`
written by [`tools/publish-plugin.py`](../../tools/publish-plugin.py), its source given as a git address at ref
`plugin-release`, `claude plugin marketplace add` and `claude plugin install heron@heron` installed 0.3.0 (63 MB in
Claude Code's plugin cache), and `claude plugin update heron@heron` moved it to 0.3.1. **Never run against GitHub,
and never on Windows** - no release has filled `plugin-release` yet. The design's other section 8 checks wait for
the parts they belong to: the SessionStart hook, and Setup's tick.

**The machine:** GitHub, for RG1; then a **clean Windows user** - one where Setup has not added Heron to Claude Code,
so that only the plugin can answer - with Claude Code and Python. No Revit is needed.

**Before RG1:** `v0.3.0` was published before the workflow existed, so the release that first fills the branch
raises `manifestVersion` in `platform/heron-products.json` and is tagged `v` and that number.

| ID | Do this | Pass looks like |
|---|---|---|
| **RG1** | Publish that release (not a pre-release), then open the *Plugin release* run it starts | Its *publish* job is green; `plugin-release` holds one commit, *"Heron plugin X.Y.Z - release vX.Y.Z"*, with no parent, and `.claude-plugin/plugin.json` at its root says that version. A FAIL - the push refused by the repository's rules, or a refusal from `publish-plugin.py` - is written here word for word |
| **RG2** | After RG1 - the install line works only after the first release that fills `plugin-release` - on the clean Windows user: `claude plugin marketplace add Ajmalpshaik/Heron-AI`, then `claude plugin install heron@heron`, then a new chat and `/mcp` | Both commands succeed, `claude plugin list` shows `heron@heron` at the release's version, and `/mcp` shows `plugin:heron:heron` connected with Heron's tools |
| **RG3** | Right after RG2, the size of `%USERPROFILE%\.claude\plugins\marketplaces\heron` and of `%USERPROFILE%\.claude\plugins\cache\heron\heron\<version>` | Both numbers written here. The container's install cache was 63 MB for 0.3.0; GitHub's clone of `main` for the marketplace is the number nobody has |
| **RG4** | After the next release fills the branch: `claude plugin marketplace update heron`, then `claude plugin update heron@heron` | It says *updated from* the old version *to* the new one, and a new chat runs the new one. Auto-update stays off until section 9's third question is answered |
| **RG5** | `claude plugin uninstall heron@heron` | `%USERPROFILE%\.heron` and `%APPDATA%\Heron` are untouched - the same files, the same sizes |
