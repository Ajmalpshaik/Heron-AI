<!-- Heron-Agent:  none -->
<!-- Heron-Step:   18 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  docs -->
<!-- See docs/29-metadata-standard.md -->

# The sweep that closed section 5b, and then reopened it

**2026-09-21, a Linux container with no Revit and no Windows.** Eighteen recorded defects
repaired, ten more found in the same sitting, eight gates added, and three things learned
that are worth more than any single row.

---

## The three lessons, first, because the rows are only examples of them

**1. A CHECK THAT CANNOT SAY WHAT IT FOUND IS A CHECK NOBODY HEARS.** This was the whole
day. `tests/test_bridge_roundtrip.py` could not say *"I did not run"*, so `check-gaps.py`
silenced it by name — and a silenced suite prints nothing at all, so *"every test passes"*
quietly meant one fewer suite than exists. `tools/open-defects.py` read a row's state from
`split("|")[-2]`, so a row holding a literal pipe reported a fragment of a sentence as its
state — row 162 said `OPEN` in writing for a day and was counted as neither open nor
settled. And the suite sweep ran every test with its output at `DEVNULL` and printed a
filename, which is the reason a Linux-only failure cost **six red CI runs, three sections
made skippable, one wrong rename and a deletion** before anybody ran the file by hand and
read one line of what it printed.

**2. A STALE CLAIM IS NEVER IN ONE PLACE, AND A FIX IN ONE LANGUAGE IS NOT A FIX.** *"The
write path has never been compiled or run"* was in four files and a test was holding it in
position. `HeronConfig.GetBool` learned four new words in the morning and the Python reader
of the same settings file did not, so `write.enabled = on` would have let the add-in permit
a change while the server reported writing as off. `CheckpointStore.save` had the exact
defect `HeronAtomicWrite` was written to fix a week earlier, in Python, with a comment
claiming the fixed behaviour.

**3. A DOCSTRING IS NOT A CHECK.** `heron_diagnose._tools` has said *"is every declared tool
actually being offered"* since it was written and counted the table instead. The component
could not fail. In a file of probes that reads exactly like a probe.

---

## What was repaired

Section 5b went **18 open → 0**, and then to zero again after ten more were found and fixed:

| Rows | What they were |
|---|---|
| 5b-1, 2, 6, 7, 8 | the kernel: a delete guard comparing paths as text, a refusal naming a removed button, two workflow-id minters, `GetBool` reading every word it did not know as false, and four classes named by no test |
| 5b-9, 11, 12, 13, 14, 15, 16, 18, 21, 22 | the add-in and the bridge: a false banner, a refusal reporting two identical numbers, an audit recording *attempted* ids under a comment promising *moved* ones, a key that changed on `Save As`, a null category crash, a banner clear comparing string references, two escapers, no ceiling on a request line |
| 5b-5, 19, 20 | two Revits losing each other's evidence, and two dialogs handing a modeller a .NET sentence |
| 5b-25 to 5b-34 | found by the sweep itself, in `mcp/` |
| 160, 162, 164, 165, 166, 167 | the machinery: the Linux bridge, the silenced suite, the register's own parser, the sweep that printed only a filename, and `main` being red since #221 |

## What was built

- **`tests/Heron.Kernel.TestHost`** — the first suite here that **runs** the C# rather than
  reading it as text. 63 checks, no Revit, no Windows, no model. Every other C# suite in
  this repository reads the source and checks a claim about it, which is the right tool for
  *"does this file still say what it must"* and the wrong one for *"what does this return
  for NaN"*.
- **`HeronAppend.Line`** — one place that knows how to append to a file another Revit may be
  appending to. `FileShare.ReadWrite`, a named system-wide mutex, a retry, and an honest
  `false` that `HeronAudit` acts on through a `ReportLoss` hook.
- **Eight gates**, each proved by making it fail on purpose: every error code the add-in can
  produce is classified or named as deliberately open; every `@server.tool()` is matched
  against the registry; the two config vocabularies are compared against the C#; the
  register's own rows must have three cells and no blank line between them; the operations
  answered outside the permission gate are a named `READ`-risk allow-list.

## The four hours that were not spent on any of it

`apt-get install -y dotnet-sdk-10.0`, `pip install --user mcp`, one `dotnet build` of the
bridge test host. Every one of the four suites this repository has repeatedly recorded as
*"cannot run on Linux"* runs. `tests/README.md` and the `heron-ship` skill both already said
so; this sitting is the third to prove it and the second to have read the warning first.

## What this sitting could not do, and did not pretend to

**Nothing here ran inside Revit.** Every row that needs a model says which measurement it is
missing, by name, rather than implying that a compile is a proof:

- the move refusal's new wording needs a model changing under a live preview
- the audit's per-element ids need a blocked or pinned element
- `DocumentKey` needs a `Save As` inside the two-minute preview window
- the banner clear needs the last model closing after a job has run
- **D3 is still not struck through.** The write path has moved three ducts and nobody has
  put a tape measure on the result

**Two proofs are STALE and only the owner can clear them.** `tag-elements-in-view` and
`force-tag-leader-lshape` had their code changed under a signed proof by #221. `restamp`
exists for a platform-skewed fingerprint, not for code that genuinely changed — using it
here would forge a proof.

## Two rows this sitting caused itself

Worth reading, because both are the ordinary shape of a careful mistake rather than a
careless one.

**5b-29.** `GetBool` was fixed in the C# at about 01:00 and the Python reader of the same
file was not looked at. The sweep reached that file an hour later and found the drift. A fix
to one half of a mirrored pair is not finished until the other half is looked at.

**5b-30's renumbering.** `main` took row 5b-25 in #221 and this branch took 25 as well —
[row 149]'s hazard, for at least the fifth time. The half of its own rule that nobody
follows is the ANNOUNCE.
