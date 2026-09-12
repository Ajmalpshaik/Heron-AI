# The nineteenth session, 2026-09-02 — the test that had been blocked for four sessions

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** no new fragments. It closed the one thing this container had been carrying as
permanently blocked, and it turned out not to be blocked at all.

`tests/test_bridge_roundtrip.py` had reported *not found* for four sessions running, and was written up
each time as "the test host targets `net8.0` and only the .NET 10 runtime is installed here". True, and
the wrong conclusion: **the framework was hardcoded in the test.**

```python
_POSIX_DIR = os.path.join(HOST_PROJECT, "bin", "x64", "Debug-net8.0")
```

That was true of the machine the file was written on and false of the next one. The project **built**
and the host **refused to start** — *"You must install or update .NET"* — which reads like a missing
build rather than a missing runtime, so four sessions in a row recorded it as an environment limit.

It now asks `dotnet --list-runtimes` and builds for the newest `Microsoft.NETCore.App` the machine can
actually run, falling back to the old value when the question cannot be answered. **All 32 checks pass**
— framing, the JSON parser, the token, newest-connection-wins, the whole lease, and the toggle cycle.

> **A pinned framework in a test meant to run on whatever machine is in front of it is a
> machine-specific assumption written down as a constant.** The register row `A4` had even recorded the
> workaround — *"`-p:HeronTfm=net10.0` runs it"* — so the knowledge existed and only a human applying it
> by hand could use it. Knowing a workaround and not automating it is how something stays broken while
> being fully documented.

**The register said Windows and meant it for less than it looked.** `A4` is now down to the one thing a
Linux run genuinely cannot touch: the Windows named pipe itself — its naming, its security descriptor
and the `CreateNewInstance` flag. Everything else on that row is proven here.

**17 of 18 suites now pass**; the one that does not is `test_mcp_serves.py` reporting honestly that it
was skipped for a missing optional dependency, which `check-gaps` reads as WAITING rather than as a
pass.

---
