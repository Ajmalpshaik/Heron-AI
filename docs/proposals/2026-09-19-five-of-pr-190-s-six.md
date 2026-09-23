# Proposals — 2026-09-19

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 2026-09-19 — five of PR #190's six proposals are built, and the sixth is still a refusal

PR #190 measured the fragments that cannot be arranged as a job and **proposed** a rule for six
shapes, changing no code. Five of those rules are now written. The sixth was a proposal to write
nothing, and it still is.

**Every claim in #190 was checked against the code before anything was built on it.** Its sibling
PR #194 had to retract a finding that turned out to be a misuse of a tool, so none of the six was
taken on trust. All six survived, and two of them survived with a correction to how they had been
described — see *what #190 got half right*, below.

### The count, measured rather than carried

`python tools/generate-jobs.py` reports against DRAFT fragments **with no run record on this
machine**, and that population moves with what is on disk. Both numbers below are for the same one:

| the population | before | after |
|---|---|---|
| **83** DRAFT with no run record, at `7ec2b04` | 39 emitted, **44** unarrangeable | 46 emitted, **37** unarrangeable |
| **70** DRAFT with no run record, at `de34462` | 26 emitted, **44** unarrangeable | 33 emitted, **37** unarrangeable |

**Two populations, measured a half-hour apart, and the row that matters is the same in both.**
Thirteen fragments gained a run record in between; every one of them was already being emitted, so
the *emitted* figure drops by thirteen and the *unarrangeable* figure does not move at all. That is
the number this work is about, and it is 44 before and 37 after on either reading.

**Seven fragments moved**, and they are named by the diff of the two NOT EMITTED lists rather than
by counting up the rules:

`check-room-mep-completeness` · `connect-air-terminals` · `create-electrical-circuit` ·
`create-roof` · `place-line-based-family` · `propose-mep-openings` · `rotate-elements-about-axis`

**NONE OF THEM IS PROVEN AND NONE OF THEM IS CLAIMED TO BE.** What changed is that Heron now has a
way to hand each of them the input that was stopping it, so a person can *arrange* a proof. The
proof itself needs a model and a session, and that is somebody else's half.

### The five rules

| the shape | the rule | what it frees |
|---|---|---|
| **`RoofType`** | named, exactly like a wall type — one dispatch row | `create-roof` |
| **`IList<Element>`** | the SECOND set, named by CATEGORY; `selected` refused | `check-room-mep-completeness`, `connect-air-terminals`, `propose-mep-openings` |
| **`Line`** | two points, a semicolon between — the inside of one point pair | `rotate-elements-about-axis` |
| **`IList<Curve>`** | the point-pair parser unchanged, each pair a bound line | `place-line-based-family` |
| **`FamilyInstance`** | an electrical panel, by its own Panel Name | `create-electrical-circuit` |

**The roof was the cheapest and it was the MEP case again.** `RoofType` derives from
`HostObjAttributes`, which `FromRequest` has accepted since 2026-09-09 — so the lookup always
worked and only the string comparison in the dispatch stood in the way, exactly as it stood in
front of `DuctType` and `PipeType` in 2026-09-13. **The contract was NOT widened to say
`HostObjAttributes`**, and #190's review was right to warn against it: the declared type becomes the
generated variable's static type, `NewFootPrintRoof` takes a `RoofType`, and that edit would have
traded a refusal for a build failure on all eight releases.

**A set of elements is refused the word `selected`, where a set of IDS accepts it.** That is
narrower on purpose, the same way `View3D` is narrower than `View`, and the reason is a fact about
all three customers rather than a preference: every one of them takes its FIRST set down the setup
chain, which ends by putting those elements in the Revit selection. The word would hand the
identical elements to both roles — the arrangement `generate-jobs.py` already marks as unarrangeable
for `unjoin-geometry` and `switch-join-order`, and the one that *runs*, and *answers*, and means
nothing. A test asserts the claim the rule rests on, so the day a fragment declares a request-sourced
set of elements with no chain-fed set beside it, "always the second set" stops being true out loud
instead of quietly.

### The sixth: an `Arc` is still refused, and the reason is now a test

#190 proposed writing nothing, on the grounds that an Arc's only customer in the library also needs
a face. **Checked, and true**: `create-angular-dimension` is the one fragment declaring an `Arc`, and
it also declares `references (IList<Reference>)`. D-72 settled that a face is not a rule waiting to
be written — it is what a mouse lands on, and no name, number or coordinate says which one. A
three-point Arc parser would therefore have shipped reachable by nobody.

That argument is true **of the library as it stands**, so it is now asserted rather than remembered:
`test_an_arc_was_left_unwritten_and_the_reason_still_holds` fails the day a second fragment declares
an `Arc`, and the decision gets made again with the new fact in hand.

**And the Line rule must not be sold as bringing the dimensions back.** `create-linear-dimension`
declares a `Line` *and* an `IList<Reference>`; it is still blocked, on the face, and a test asserts
that too — #190 said it in a sentence, and a sentence is not a check.

### What #190 got half right, and the correction is the useful part

**"RoofType may work today if the contract simply says the word."** It does not, and the word in
question is the confusing part. If a contract said `HostObjAttributes` the lookup would resolve —
but the contract says `RoofType`, the dispatch compares the declared name as text, and so it fell to
the catch-all. The missing half was always the resolver row, never the contract.

**"`IList<Element>` — named by category, the way the first set is found."** Right, and the precedent
it cited is not quite the one to copy. `propose-mep-openings` says in its own contract that its
second set is "the same shape FIND_CLASHES uses" — and `find-clashes` declares
`ICollection<ElementId>`, which is answered with the word `selected`. Copying *that* would have
reproduced the one-set-twice trap. The category is the right spelling and
`select-by-category-name` — step one of the setup chain — is the precedent that actually applies.

### How it was proved, and what that does not cover

- **Compiled on all eight releases**, 2020 through 2027, every project, via `tools/check-compile.py`.
  That is what establishes that `RoofType`, `Curve`, `Line.CreateBound`,
  `Application.ShortCurveTolerance` and `BuiltInParameter.RBS_ELEC_PANEL_NAME` exist with these
  signatures across the whole supported range.
- **`tests/test_generate_jobs.py` exits 1 on the code as it stood and 0 after.** Both were run. Seven
  new tests, one per rule plus the Arc refusal, the hint coverage and the `selected` refusal — and
  every fragment count in them is derived from the real library, the real setup chain and the real
  registry, so a number that stops being true fails rather than ages.
- **The transcription is still checked, not remembered.** `test_receivable_agrees_with_the_add_in`
  reads the branches out of `RevitFragment.cs` and compares them to `generate-jobs.py`'s copy, in
  both directions. It passes.

**NONE OF THIS IS A BEHAVIOUR PROOF.** A compiler agreeing about a signature says nothing about
whether `ManyByCategory` collects the right elements, whether a panel resolves in a model that has
one, or whether a line comes out where somebody meant it. Seven fragments are now *arrangeable*;
seven proofs are what would make them proven, and each needs a model in front of a person.

---
