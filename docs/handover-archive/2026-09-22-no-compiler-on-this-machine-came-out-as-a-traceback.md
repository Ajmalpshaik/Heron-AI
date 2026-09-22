# Session note — NO COMPILER ON THIS MACHINE CAME OUT AS A TRACEBACK

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — NO COMPILER ON THIS MACHINE CAME OUT AS A TRACEBACK

**[Row 5b-133](../FRAGMENT-ISSUES.md), FIXED. [Row 5b-134](../FRAGMENT-ISSUES.md) raised, and it needs
a machine with the SDK.** `tools/check-fragments-compile.py` read end to end — 400 lines, never
opened before. The **sixth** of the seven unread CI gates.

It compiles every fragment's C# against every Revit release it claims, with no Revit and no
Windows — and until it existed, **nothing compiled the fragments at all**. The library's whole
point was the one part no compiler had ever read.

**It shells out to `dotnet build`.** Measured on this container, which has no .NET SDK:

```
FileNotFoundError: [Errno 2] No such file or directory: 'dotnet'
exit 1
```

A traceback is none of AGENTS.md's four states, and **exit 1 is the code for a fragment that does
not compile** — so a reader, or a script, cannot tell a broken library from a machine with no
compiler on it.

**The workflow already knew, and worked around it rather than fixing it.** The step sits in a
different CI job, and the comment beside it says: *"it shells out to `dotnet build` with no guard:
the gates runner has no SDK and it would fail there for the wrong reason."* That is this defect,
written into `gates.yml` instead of into the tool — while the file does the right thing one
function below for the smaller dependency: *"This needs PyYAML: pip install --user pyyaml"*.

A `no_compiler()` prints **NOT RUN** in those words, names what is missing, and returns **3** —
the code [`tests/README.md`](../../tests/README.md) gives a suite that could not run, and which it
states plainly is *not a pass*. **It is asked last, on purpose**: an empty library and a release
nobody claims are argument questions, answerable without an SDK, and the first version of the
guard ran first and turned the undeclared-release case from 2 into 3 — which the suite caught.

**Nothing that reads this tool is affected**, measured: `gates.yml` runs it with a plain `run:` in
a job that installs .NET 10, and `brain/heron_matrix.py` reads `build/compile-results.json`, whose
absence it already handles.

### The suite never compiles anything

A compile needs the SDK and minutes, and CI already does it. **3 red** against the module as
found, with everything else green before and after: what it reads out of a `fragment.yaml`, the
harness parameters generated from the contract, the contract assignment that turns a broken
promise into a compiler error, `GC.KeepAlive` so it cannot be optimised away, the **absolute**
`#line` path, the record it leaves for HERON-FRG-MTX-009, and both refusals.

**Teeth proved three ways**: the guard removed (**3 red**), the typed contract assignment replaced
with `var` (**1 red**), and the `#line` path made repo-relative again (**1 red**).

**And a gate caught this session in the act.** The suite's first draft asserted
`"using Autodesk.Revit.DB;" in text`, and `check-structure.py` failed the build: *"references
Autodesk.Revit outside revit/ — the adapter boundary is broken"*. It **greps on purpose**, and
says why in a comment — *"a vendor namespace named in a COMMENT is still a boundary being
discussed in the wrong file"*. The check now reads `USINGS` from the tool that owns it and asserts
the harness declares **every** name on it and no others, which is tighter than naming one and
types no vendor namespace at all. The gate was right; the test was wrong.

### The parts worth knowing

A fragment is a **snippet**, so it is wrapped in a method whose **parameters are its declared
`needs`**, generated from the contract rather than guessed — and after the snippet, each name the
contract **provides** is assigned to a local of the declared type. **That contract half is worth
more than the syntax half**: a snippet that compiles while breaking its promise is exactly the one
that composes into something broken later, far from here.

The `#line` directive is **absolute and has to be**: a repo-relative one built fine on net472 and
net48 and failed on every net8 and net10 release with CS1504 — found by running all eight releases
instead of one.

`USINGS` is **a contract with unbuilt work**. It declares what a fragment may assume is in scope,
so D-28's in-process Roslyn executor must supply the same set; a namespace added here and not
there compiles green and fails at the PC. `check-structure.py` forbids `Autodesk.Revit` inside
`brain/` exactly so a fragment can only use what that list says it has.

### What is left, and it needs the SDK

**[Row 5b-134](../FRAGMENT-ISSUES.md).** The generated `CSPROJ` carries a **verbatim second copy** of
`Directory.Build.props`'s five `DefineConstants` lines, with a note admitting it will go stale:
*"If Directory.Build.props gains a symbol, add it here too."* Measured: **the two are identical
today**, so nothing is wrong — and the copy looks **provably redundant**, because the project is
generated *inside* the repository so the props file applies, and the same CSPROJ uses
`$(HeronTfm)`, which only that props file defines.

**But removing it blind would be a change to the harness that decides whether 396 fragments
compile across eight releases**, and a fix nobody can see fail is not a fix this repository
accepts. One line of work at a machine with the SDK: delete the five lines, run the tool, confirm
every fragment still compiles. Either answer closes the row.

**One CI gate remains unopened**: `check-intrusion` (213).
