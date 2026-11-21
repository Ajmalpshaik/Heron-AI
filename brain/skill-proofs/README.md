<!-- Heron-Agent:  HERON-SKL-VAL-004 -->
<!-- Heron-Step:   17 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  brain -->
<!-- See docs/29-metadata-standard.md -->

# Skill proofs — a job method's composition, signed by a person

A skill is a **composition**: its steps in an order, each handing on to the next. Every step can be a
PROVEN fragment and the skill still be unproved, because a fragment's proof says it does its job alone -
not that the first step hands the second what it needs, nor that the last answers the skill's question.
That claim is proved here, in a file of its own, the way [`agent-proofs/`](../agent-proofs/) is apart from
the cards ([FRAGMENT-ISSUES row 141](../../docs/fragment-issues/section-5-rows-126-150.md)).

**How a file gets here:**

1. On the PC, with Revit open: `python tools/run-skill.py <job.yaml> --dry-run`, then without it. It runs the
   plan twice through `heron_bridge_client.py plan` - a positive leg where the answer is something and a
   negative where it must be nothing (D-30) - every change rolled back, and writes a draft to
   `brain/proof-drafts/skill-<skill>.yaml` (gitignored, with the two run records under `runs/`).
2. A person reads the draft and signs it: `python brain/heron_plan_proof.py accept <skill> --by "Name"`.
   Only a PASS is signed, and only while the skill and every step's fragment are as they were when it ran
   (the fingerprint).

**Nothing here changes a skill's `heron-status`.** A signed proof is the evidence; promoting the skill is
the owner's decision. A fresh checkout holds only this README until somebody signs one.
