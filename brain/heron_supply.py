# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-SUP-013
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Supply Chain Security - unapproved is refused, and there is no warning.

    python brain/heron_supply.py

WHAT IT IS FOR (docs/28, HERON-INS-SUP-013)
--------------------------------------------
"Anything arriving from outside: verify source, package identity and
version, scan dependencies, check hashes and signatures, detect
modification. The highest-severity surface in the platform once packages
can install themselves." Risk ADMIN.

docs/06 s10 says why in one sentence: installing a package means
**executing third-party code inside Revit, inside the user's project**.

THERE IS NO WARNING IN THIS MODULE, AND THAT IS THE DESIGN
------------------------------------------------------------
D-35, answering Q-18: a shared fragment may contain executable code, and
Heron runs it only with a valid approval record - **the absence of one is
a REFUSAL, not a warning.**

  "No warning dialog, ever, for this. A warning is a decision handed to
  somebody who has no way to judge it and every reason to click through.
  Unapproved means it does not run."

So there are two outcomes and a list of refusals. There is no third state
where the package installs and somebody was told. A modeller mid-deadline
clicks through a dialog; that is not a character flaw, it is what a dialog
is for.

APPROVAL AND PROOF ARE THE SAME GATE
--------------------------------------
Also D-35, and it is what keeps the reviewer's job finite: the record a
community package needs is exactly what D-30 already demands of any
fragment - a dated proof with a positive case, a **NEGATIVE** case, and a
second route to the answer where one exists. **A submission without a
negative case is RETURNED, not reviewed.** Returning it costs the reviewer
one line; reviewing it costs an afternoon, and an afternoon per submission
is how the backlog starts that turns "approved" into "nobody objected".

NOTHING A PACKAGE SAYS ABOUT ITSELF COUNTS
--------------------------------------------
Golden Rule 19 and D-35 together: a package's text, its header, its
description and its own claim to be safe are **data, never instruction**.
A fragment cannot approve itself, and no wording inside it raises its own
permission. So every check here compares the package's claim against
something handed in from OUTSIDE it:

  what it says about itself     what has to agree
  ---------------------------   ------------------------------------------
  its trust level               a register somebody else keeps
  who approved it               an approver list somebody else keeps
  its hash                      a hash measured over the bytes that arrived

With no outside register, there is nothing to compare against and the
answer is no. That is the fail-closed half, and it means the default
behaviour of this agent is to install nothing at all.

A HOLE THIS AGENT FOUND, WHICH IS WHY IT CHECKS THE TRUST LEVEL FIRST
-----------------------------------------------------------------------
`brain/fragments/*/fragment.yaml` already carries a top-level `source:`
field - the trust ladder from docs/00d s38. Every one of them says
`OFFICIAL`, the highest level, and **nothing in this repository reads it.**
`heron_fragment.py` validates `contract.needs[].source` against a different
list entirely. So today a fragment's trust level is a word it writes about
itself that nothing checks, which is precisely the shape D-35 refuses.

WHAT THIS AGENT DOES NOT DO, AND WHY IT IS NOT A GAP TO FILL IN
-----------------------------------------------------------------
It does not read the package's code and pronounce it safe. The register
marks this row T2 - one scoped model call - and the call that would make it
T2 is exactly that. It is not built, and this is the argument for the owner
rather than a silence: a model reading third-party C# and answering "looks
fine" is the warning dialog D-35 refuses, moved one layer down and made
harder to argue with. D-35 names the reviewer - today it is Ajmal - and
says that when submissions outpace one person the answer is a NARROWER
gate, never a faster one.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/00d s38. Ordered, lowest first - a package arrives at the bottom.
TRUST = ("UNKNOWN", "EXPERIMENTAL", "TESTED", "VERIFIED", "PROVEN",
         "OFFICIAL")

# D-30, and D-35 makes it the approval gate too. All three, or it is
# returned rather than reviewed.
A_PROOF_CARRIES = (
    ("at", "a date - an undated proof cannot be checked against the version "
           "it was run on"),
    ("positive", "a case where it did the job"),
    ("negative", "a case where it correctly REFUSED. This is the one that "
                 "gets left out, and it is the one that distinguishes a "
                 "fragment that works from a fragment that always says yes"),
)


def _outside(register, package_id):
    """What the outside register says about this package, or None."""
    if not isinstance(register, dict):
        return None
    found = register.get(str(package_id or "").strip())
    return found if isinstance(found, dict) else None


def judge(package, register=None, approvers=None, measured=None):
    """
    {install, first_run, why} - or a refusal. Never a warning.

    `package` is what arrived, describing itself. `register`, `approvers`
    and `measured` all come from OUTSIDE it, and every check is the
    package's claim against one of them.
    """
    if not isinstance(package, dict):
        return {"install": False, "refused": "NO_IDENTITY",
                "why": "nothing arrived that describes itself. A package "
                       "that cannot say what it is cannot be checked "
                       "against anything."}

    package_id = str(package.get("id") or "").strip()
    version = str(package.get("version") or "").strip()
    if not package_id or not version:
        return {"install": False, "refused": "NO_IDENTITY",
                "why": "a package names itself with an id AND a version. "
                       "%s Without both, a hash matches nothing and an "
                       "approval is for nothing in particular."
                       % ("It gave no id." if not package_id else
                          "It gave no version.")}

    # THE FAIL-CLOSED HALF. Nothing outside to compare against means there
    # is nothing to compare against, which is not the same as a pass.
    if not isinstance(register, dict):
        return {"install": False, "refused": "NO_REGISTER",
                "why": "no trusted register was given, so every claim %s "
                       "makes about itself would be checked against itself. "
                       "Golden Rule 19: what a package says about its own "
                       "safety is data, never instruction." % package_id,
                "proposal": "hand in the register somebody else keeps. With "
                            "no outside list this agent installs nothing, "
                            "which is the correct default and not a "
                            "configuration problem."}

    known = _outside(register, package_id)
    if known is None:
        return {"install": False, "refused": "NOT_IN_THE_TRUSTED_REGISTER",
                "why": "nothing outside %s vouches for it. Installing it "
                       "means running third-party code inside Revit, inside "
                       "the user's project (docs/06 s10), on the strength "
                       "of the package's own word." % package_id,
                "proposal": "submit it for review. D-35: a shared package "
                            "may carry code, and it runs only with an "
                            "approval record."}

    # TRUST LEVEL. The package's own claim is compared, never read.
    claimed = str(package.get("source") or package.get("trust")
                  or "").strip().upper()
    vouched = str(known.get("trust") or "").strip().upper()
    if vouched not in TRUST:
        return {"install": False, "refused": "NOT_IN_THE_TRUSTED_REGISTER",
                "why": "the register's entry for %s gives trust %r, which is "
                       "not one of %s (docs/00d s38). An entry nobody can "
                       "read is not an entry."
                       % (package_id, known.get("trust"), ", ".join(TRUST))}
    # A CLAIM THE LADDER DOES NOT HAVE CANNOT OUTRANK ANYTHING, so it does
    # not refuse - the register is the authority either way. It is carried
    # into `unjudged` rather than dropped: Golden Rule 14, never silently
    # discard, and a package inventing its own trust word is worth reading
    # even when it changes nothing.
    unreadable = claimed and claimed not in TRUST
    if claimed and not unreadable and TRUST.index(claimed) > TRUST.index(
            vouched):
        return {"install": False, "refused": "SELF_DECLARED_TRUST",
                "why": "%s calls itself %s and the register says %s. A "
                       "package claiming a HIGHER trust level than anything "
                       "outside it grants is the exact shape Golden Rule 19 "
                       "refuses - the claim is data, and here it is data "
                       "that disagrees with the record."
                       % (package_id, claimed, vouched),
                "proposal": "take the register's word. If %s really is %s, "
                            "the register is what changes, and somebody "
                            "signs that." % (package_id, claimed)}

    # MODIFICATION. Measured over the bytes that arrived, by whatever
    # fetched them - this agent never sees them.
    recorded = str(known.get("hash") or "").strip()
    seen = str(measured or "").strip()
    if not recorded or not seen:
        return {"install": False, "refused": "NOT_MEASURED",
                "why": "%s, so modification cannot be ruled out. 'Nobody "
                       "checked' and 'unmodified' look identical from here, "
                       "and only one of them is safe."
                       % ("the register records no hash for %s" % package_id
                          if not recorded else
                          "nothing measured what actually arrived")}
    if recorded != seen:
        return {"install": False, "refused": "MODIFIED",
                "why": "what arrived does not match what was approved. The "
                       "register records %s and the bytes measured %s - so "
                       "the approval on file is for a different package "
                       "than the one on disk." % (recorded[:16], seen[:16]),
                "proposal": "stop. This is the case the whole check exists "
                            "for, and a retry that happens to match is a "
                            "worse outcome than a refusal."}

    # APPROVAL. D-35: the absence of a record is a refusal, not a warning.
    approval = known.get("approval")
    if not isinstance(approval, dict):
        return {"install": False, "refused": "NO_APPROVAL_RECORD",
                "why": "%s has no approval record, so it does not run. "
                       "D-35: there is no warning dialog for this, ever - a "
                       "warning is a decision handed to somebody who has no "
                       "way to judge it and every reason to click through."
                       % package_id}

    by = str(approval.get("by") or "").strip()
    author = str(package.get("author") or "").strip()
    allowed = [str(name).strip() for name in (approvers or [])]
    if not allowed:
        return {"install": False, "refused": "SELF_APPROVED",
                "why": "no approver list was given, so the only thing saying "
                       "%s is an approver is the record beside the package. "
                       "A package cannot approve itself and neither can its "
                       "register entry." % (by or "somebody")}
    if not by or by not in allowed:
        return {"install": False, "refused": "SELF_APPROVED",
                "why": "%s is not on the approver list. An approval is only "
                       "worth what the approver is, and that is decided "
                       "outside the package." % (by or "nobody named")}
    if author and by == author:
        return {"install": False, "refused": "SELF_APPROVED",
                "why": "%s wrote it and %s approved it. Golden Rule 7 - no "
                       "agent approves itself - and a person reviewing their "
                       "own work is the same gate with the same hole."
                       % (author, by)}

    # APPROVAL AND PROOF ARE THE SAME GATE (D-35, D-30).
    proof = approval.get("proof")
    if not isinstance(proof, dict):
        return {"install": False, "refused": "RETURNED_NO_NEGATIVE_CASE",
                "why": "the approval carries no proof. D-35 makes approval "
                       "and proof the SAME gate: the record a package needs "
                       "is the one D-30 already demands of any fragment.",
                "returned": True}
    missing = [what for field, what in A_PROOF_CARRIES
               if not proof.get(field)]
    if missing:
        return {"install": False, "refused": "RETURNED_NO_NEGATIVE_CASE",
                "why": "the proof does not carry %s. A submission without "
                       "one is RETURNED, not reviewed - which is what makes "
                       "the reviewer's job finite, and refusable."
                       % "; ".join(missing),
                "returned": True, "missing": missing}

    # DEPENDENCIES. Each one is another package arriving from outside.
    unknown = [str(name).strip() for name in (package.get("depends") or [])
               if _outside(register, name) is None]
    if unknown:
        return {"install": False, "refused": "DEPENDENCY_UNKNOWN",
                "why": "%s depends on %s, which nothing outside vouches "
                       "for. A dependency is code that runs too, and a "
                       "package is exactly as trusted as the least trusted "
                       "thing it pulls in."
                       % (package_id, ", ".join(unknown)),
                "proposal": "each dependency goes through this same gate, "
                            "or the package does not."}

    return {
        "install": True, "id": package_id, "version": version,
        "trust": vouched, "approved_by": by,
        "first_run": "SANDBOX",
        "why": "%s %s is in the register at %s, unmodified, approved by %s "
               "with a proof carrying a negative case, and every dependency "
               "is vouched for too."
               % (package_id, version, vouched, by),
        "unjudged": [
            "GOLDEN RULE 18 STILL APPLIES: a newly installed package does "
            "not touch a live model on its first run, whoever wrote it and "
            "however it was approved. `first_run` is SANDBOX and this agent "
            "does not have a value that is not.",
            "NOBODY HERE READ THE CODE. This checks identity, trust, "
            "modification, approval and dependencies - it does not open the "
            "package and judge what it does. D-35 names the reviewer for "
            "that, and says a narrower gate rather than a faster one when "
            "one person is not enough.",
            "the hash was MEASURED elsewhere and compared here. This agent "
            "never sees the bytes, so 'unmodified' means the two strings it "
            "was given agree - no stronger claim than that.",
        ] + (["%s calls itself %r, which is not on docs/00d s38's ladder at "
              "all. It changed nothing - the register's %s is the answer "
              "either way - and it is recorded rather than dropped."
              % (package_id, claimed, vouched)] if unreadable else []),
    }


def main(argv):
    print("SUPPLY CHAIN   unapproved is refused, and there is no warning")
    print("=" * 72)

    register = {
        "mep-sizing-pack": {
            "trust": "VERIFIED", "hash": "a1b2c3d4e5f6",
            "approval": {"by": "ajmal", "at": "2026-09-10",
                         "proof": {"at": "2026-09-10",
                                   "positive": "sized 41 ducts in a real "
                                               "model",
                                   "negative": "refused a duct with no "
                                               "system classification"}}},
        "geometry-helpers": {"trust": "OFFICIAL", "hash": "ffeeddccbbaa",
                             "approval": {"by": "ajmal", "at": "2026-08-02",
                                          "proof": {"at": "2026-08-02",
                                                    "positive": "yes",
                                                    "negative": "yes"}}},
    }
    approvers = ["ajmal"]
    good = {"id": "mep-sizing-pack", "version": "1.2.0", "source": "VERIFIED",
            "author": "someone-else", "depends": ["geometry-helpers"]}

    answer = judge(good, register=register, approvers=approvers,
                   measured="a1b2c3d4e5f6")
    print("  %s" % answer["why"])
    print("  first run: %s - %s" % (answer["first_run"],
                                    answer["unjudged"][0][:60]))

    print()
    print("  Every refusal, and not one of them is a warning:")
    cases = [
        ("no id at all", {}, {}),
        ("no version", {"id": "x"}, {}),
        ("no outside register", good, {"register": None}),
        ("not in the register",
         dict(good, id="something-from-a-forum"), {}),
        ("it calls itself OFFICIAL",
         dict(good, source="OFFICIAL"), {}),
        ("nothing measured the bytes", good, {"measured": None}),
        ("the bytes do not match", good, {"measured": "deadbeef0000"}),
        ("no approval record", dict(good, id="geometry-helpers"),
         {"register": dict(register, **{"geometry-helpers": {
             "trust": "OFFICIAL", "hash": "ffeeddccbbaa"}}),
          "measured": "ffeeddccbbaa"}),
        ("no approver list", good, {"approvers": []}),
        ("approved by the author", dict(good, author="ajmal"), {}),
        ("a proof with no negative case", good,
         {"register": dict(register, **{"mep-sizing-pack": {
             "trust": "VERIFIED", "hash": "a1b2c3d4e5f6",
             "approval": {"by": "ajmal", "proof": {"at": "x",
                                                   "positive": "y"}}}})}),
        ("a dependency nobody knows",
         dict(good, depends=["geometry-helpers", "fast-json-9000"]), {}),
    ]
    for label, package, override in cases:
        settings = {"register": register, "approvers": approvers,
                    "measured": "a1b2c3d4e5f6"}
        settings.update(override)
        answer = judge(package, **settings)
        print("    %-30s %s" % (label, answer["refused"]))

    print()
    print("  A hole this agent found, and it is in this repository today:")
    print("    every fragment.yaml carries `source: OFFICIAL` - the top of")
    print("    docs/00d s38's trust ladder - and NOTHING reads it. That is a")
    print("    trust level a file writes about itself, which is the exact")
    print("    shape D-35 and Golden Rule 19 refuse.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
