# Proposals — Part E

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Part E — Three findings taken from working Revit tooling, written as Heron's own

**Added 2026-08-27, rewritten 2026-08-28.** These came from studying Revit tooling that already works in
daily use, rather than from reasoning. They are recorded here as **Heron's own conclusions, with the
reasoning intact**, deliberately not as pointers at another codebase — because a note that says *"go and
read that other repository"* is worth nothing the day that repository stops being used, and the reasoning
is the part worth keeping anyway.

Nothing here was copied. Each is a mechanism that was understood and then written for Heron, in Heron's
shape.

### E1 — Runtime C# compilation inside Revit is viable, and Heron will want it

C# can be compiled **at run time, inside `Revit.exe`**, with Roslyn (`Microsoft.CodeAnalysis.CSharp.Scripting`),
across the same 2020→2027 range Heron targets. A sentence becomes C#, is checked, is compiled, and runs on
Revit's thread inside a `TransactionGroup` — with no rebuild and no restart. This is demonstrated, not
theoretical.

Heron today can only run **operations compiled into the add-in ahead of time**, so adding a capability
means a rebuild, a redeploy and a Revit restart. That is the correct trade for Phase 0 and Phase 1 — a
fixed, reviewable set of operations is exactly what makes the first write defensible — but it is not the
endpoint, and [D-04](../DECISIONS.md) already anticipates a hybrid.

**The proposal:** when Phase 2 fragments arrive, the execution half does not need designing from first
principles. The shape is known to work on this hardware and this Revit range.

**What it does NOT solve, and this needs saying plainly:** Roslyn compiles *scripts* once the add-in is
already loaded. Every `.cs` file in `revit/` and `platform/` still needs an ahead-of-time build with the
.NET SDK. Runtime compilation is not a way around Step 6 having never met a compiler.

### E2 — Generated code needs a gate before it reaches the compiler

The mechanism worth having: scan generated code **before** compiling it, and split the result three ways.

| | |
|---|---|
| **Blocked** | Process launch, registry, network, reflection, unmanaged calls, `unsafe`, file delete/move, `#r`/`#load` directives, `using static`, type aliases |
| **Warning** | Legitimate but destructive — element delete, purge, writing a file |
| **Safe** | Runs |

Two things matter more than the list itself, and both are the kind of thing only real use teaches:

**A blocklist earns its entries by finding its own holes.** `#r "..."` is a one-line bypass of every other
check, because it pulls in an arbitrary assembly before anything else runs. Reflection generalises past
most name-based checks. `using static` renames a blocked call to a bare method name. Each of those is
invisible until someone looks for it.

**And it must say plainly what it is not.** Text matching is a speed bump against careless generated code,
**not a security boundary** — a determined bypass gets through, and only AST/semantic analysis or real
process isolation would change that. A guard that is believed to be stronger than it is, is worse than no
guard.

This is [Golden Rule 18](../14-golden-rules.md) — *generated code never touches a live model on its first
run* — with a known-workable implementation shape behind it. **Not built in Step 6**, deliberately: Heron
generates no code yet, and a gate for a door that does not exist is machinery to maintain, not safety.

### E3 — A lesson taken into the code the same day

`RevitWrite.SafeRollBack` and `RevitWrite.TryRefresh` exist because of this study, and they are the one
part of it that changed Heron immediately.

The bug: an unguarded `group.RollBack()` in a catch block. When the rollback **also** throws — a group
left un-rollback-able by a `Commit()` that has just failed — the second exception escapes and buries the
first. The caller gets no result at all.

Heron had the same shape, written blind the day before, guarded only by `GetStatus() == Started`. That
check is not enough: it cannot see a group left broken by an `Assimilate()` that failed part way, and
`GetStatus()` can throw on its own. Both guards are now kept — the status check to avoid provoking an
exception in the ordinary case, the catch to handle everything it cannot see.

The same lesson from the other direction: a view refresh **after** a successful commit, if it throws,
reports an already-committed change as a failure and then tries to roll back a group it can no longer
roll back. The user is told nothing happened while their elements have in fact moved.

**The general rule, which is the part worth keeping:** a rollback is always the *second* thing going
wrong, and cleanup and cosmetics must never be able to become the first thing reported. Neither of them
is the work.

---
