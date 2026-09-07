<!-- Heron-Agent:  HERON-FRG-VAL-001 -->
<!-- Heron-Step:   17 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  brain -->
<!-- See docs/29-metadata-standard.md -->

# Proof drafts — evidence waiting for a person

Everything in this folder was written by the **Fragment Validation Agent**
([`brain/heron_validate.py`](../heron_validate.py)). None of it is a proof yet.

A draft is what came back when a fragment was run: what it returned, what it returned when it should
have returned nothing, and whether a second route agreed. It is arranged into the shape
[D-30](../../docs/DECISIONS.md) asks for, and then it stops — because the last step is a judgement no
machine can make. **Is that answer right for this building?**

## Why they live here and not in the fragment

The agent has **no write path into `brain/fragments/`**. `write_draft` refuses a path under the library,
so "it never sets `heron-status`" is a property of the folder layout rather than of the code being
careful. Clash detection lists the clashes; the engineer decides which are real and signs the drawing.
The machine never signs.

The second reason is smaller and still real: the search, the graph and the routing checks all read
`brain/fragments/`. Unreviewed prose sitting in there would be indexed and retrieved as though it were
settled.

The cost is that one fact has two homes until somebody accepts it, which is exactly what
[docs/29](../../docs/29-metadata-standard.md) warns about — so `accept` **deletes the draft** as it
records the proof, and the split is temporary by construction.

## Reading one

```
python brain/heron_validate.py review              every draft
python brain/heron_validate.py review list-levels  one of them
```

Read `gaps:` first. It is the list of things the run could **not** establish, and a draft with gaps is a
job half done, not a proof with footnotes. `by:` is empty in every draft — deliberately. The field is
required by the validator, so a draft pasted into a fragment without a person's name on it fails loudly
instead of quietly counting as proven.

## Accepting one

```
python brain/heron_validate.py accept list-levels --by "Ajmal PS"
```

Your name **is** the signature — D-30: *"whoever ran it records it, under their name and the date, not a
tick."* This writes the `proof:` block into the fragment and **leaves `heron-status` exactly as it was**.
Promoting the fragment is a separate, deliberate act, and it is yours.

## What to do with a draft you do not believe

Delete the file. Nothing depends on it, and rejecting a draft costs nothing. That is the whole design:
**if the agent produces three hundred drafts and most get rejected, it has made the backlog worse** —
somebody now has to read three hundred wrong things instead of proving three hundred right ones. It is
built to do ten well.
