# Needs checking — Group C

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group C — the gate, before anything can move

**Do not skip to D.** C3 is what proves the write path cannot fire by accident; testing the move before
it is pointless, because a passing move tells you nothing about whether the gate works.

The gate now sits at **one** place — every operation passes through it before routing, and its risk
comes from `HeronOperationRegistry` rather than from a literal inside the write path. C5 and C6 are what
prove it blocks the right level rather than simply blocking everything, which would pass C3 while being
useless.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**C1**~~ | Press Emergency Stop — [full row](../needs-checking-archive/group-c.md#row-c1) | **CANNOT BE RUN from 2026-09-06.** |
| ~~**C2**~~ | Press it again — [full row](../needs-checking-archive/group-c.md#row-c2) | **CANNOT BE RUN.** |
| **C3** | With `write.enabled` still **false** (the default — do not change it yet), ask to move ducts | **Refuses**, and names `write.enabled` and the config file path. Nothing goes to Revit |
| ~~**C4**~~ | ~~Press Emergency Stop on, then ask to move ducts~~ | **CANNOT BE RUN.** The two refusals are still written and still distinct in the code ([RevitOperations.cs](../../revit/Heron.Revit.Addin/RevitOperations.cs)), but with no way to set the stop, only the permission refusal can be reached. **Untested from here on** |
| **C5** | With `write.enabled` still false, ask to **select** ducts | **Works.** The gate blocks MODIFY, not READ or EXECUTE — if selecting is refused, the levels are wrong |
| ~~**C6**~~ | Press Emergency Stop on, then ask to count elements — [full row](../needs-checking-archive/group-c.md#row-c6) | **CANNOT BE RUN.** |
| **C7** | Now set `write.enabled = true` in `%APPDATA%\Heron\config\heron.config`. **No restart** — `HeronPermissions.Allows` reads that file fresh on every check, so the change lands on the next request. The instruction to restart was here, and in the refusal message, until 2026-09-06; both said it, neither needed it | — |
| **C8** | Ask to move ducts again | **It is now permitted** (a preview appears). If it still refuses, `write.enabled` is not being read — that exact bug existed until 2026-08-28: the key was read but never *declared*, so `Load()` dropped it silently and the refusal told you to set the thing you had just set |
| **C9** | `revit_health` | First line is a four-state rollup — `Heron: HEALTHY / WARNING / DEGRADED / FAILED`. With writing on it must show **WARNING** on the write gate and say the path is unproven |
| **C10** | Set `revit.operationTimeoutSeconds = 120`, restart Revit, then make Revit busy long enough to time out | The message is *"Revit started the request but has not finished"* — **not** *"no answer"*. Proves the client's deadline follows the add-in's setting instead of the old hardcoded 90 s |
