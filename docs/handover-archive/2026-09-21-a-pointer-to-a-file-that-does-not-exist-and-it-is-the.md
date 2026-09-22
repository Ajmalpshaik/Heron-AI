# Session note — A POINTER TO A FILE THAT DOES NOT EXIST, AND IT IS THE DOCUMENTED DEAD END

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 — A POINTER TO A FILE THAT DOES NOT EXIST, AND IT IS THE DOCUMENTED DEAD END

**[Row 5b-99](../FRAGMENT-ISSUES.md). FIXED.** Found reading
[#242](https://github.com/Ajmalpshaik/Heron-AI/pull/242) — *one build folder per Revit release* — which
landed while this branch was open and turned three ledger marks STALE. **Reading it was the point; the
stale marks are what made me do it.**

`tools/deploy-addin.ps1` said *"that is the whole point of **Directory.Build.targets**"*. **There is no
`Directory.Build.targets` in this repository.** Only `Directory.Build.props` — which the same script
names correctly **five other times, one of them five lines earlier**.

**And the file it should have named says in capitals why the other is wrong**: *"IT MUST BE SET HERE,
IN .props, AND NOT IN A .targets FILE"*, with the measurement — setting `OutputPath` from a `.targets`
file left `OutDir` **and** `ProjectDepsFilePath` pointing at the flat folder, so the build wrote its
assemblies to one place and its `deps.json` to another and **Revit 2027 would not load**. That
paragraph ends *"Both dead ends are written into the comment so the next person does not walk them
again."* **The one wrong pointer, added in the same commit, sends that next person down one of them**
— and finding nothing there, they cannot tell a typo from a file somebody forgot to commit.

**The check is derived rather than pinned to a name.** `tests/test_deploy_script.py` collects every
`Directory.Build.<something>` the script mentions and asserts each one **exists**. **Shown to fail: 1
check**, and it names the file.

**The rest of #242 was read and is sound.** The split is by **Revit release, not target framework** —
2021 to 2024 all build `net48` but `DefineConstants` makes them different builds, and a per-framework
split would let a 2021 build install into 2024 in silence, which is `A12`'s class of defect. The
flat-layout fallback is safe because the runtime guard reads the assembly. **The comment recording
that four installs failed in a row before this was fixed is the best part of that commit** and is
untouched.

> **A stale mark is a reading queue, and it is a good one.** Three files went stale because somebody
> else changed them; reading why produced a row. When `--stale` is not empty, that is the work, not an
> annoyance to clear.
