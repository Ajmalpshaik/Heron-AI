# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-REVIT-API-020
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Revit API agent - what an operation may call, and whether it needs a
transaction.

    python brain/heron_revit_api.py

WHAT IT IS FOR (docs/28, HERON-REVIT-API-020)
----------------------------------------------
"Correct API, namespace, method, deprecations, transaction requirements
for a novel operation." T2, risk READ.

T2 IS ONE SCOPED MODEL CALL, AND UNDER D-01 THAT CALL IS THE HOST'S. So
this agent knows no Revit API of its own and never will. It does two
things a host cannot do for itself:

  1. FRAMES THE QUESTION, with the constraints attached - which releases
     the answer has to hold on, what each of them stopped shipping, and
     what shape of transaction the operation needs.

  2. CHECKS THE ANSWER. A proposal naming a member a requested release
     removed is REFUSED, not reported. That is the whole point of the
     row: an answer about the Revit API is confident, fluent and wrong
     in a way nobody doubts, and there is no compiler here to catch it.

THE CASE THIS EXISTS FOR IS IN THIS REPOSITORY'S OWN HISTORY
--------------------------------------------------------------
`ElementId.IntegerValue` was written in with a comment calling it "the
property every version has had". It compiles on 2020 through 2025. On
2026 and 2027 it does not exist - not deprecated, GONE. Somebody had
checked five releases and extrapolated to eight, which is exactly what
D-05 forbids and exactly what a fluent answer about an API does.

`HERON-REVIT-ACI-034` holds the evidence and asks it of FRAGMENTS THAT
EXIST. This asks it of an operation NOBODY HAS WRITTEN YET, which is the
cheaper moment: before the C# rather than after the eighth release fails
to compile.

WHAT IT CANNOT SEE, AND WHY THAT IS SAID OUT LOUD
---------------------------------------------------
The evidence records what each release REMOVED, not the whole surface.
So a member this agent does not warn about is UNWARNED, never PROVEN
PRESENT - and `unjudged` says so on every run. Reading those two the
same way is D-52's plausible zero, and it has already happened once
inside HERON-REVIT-ACI-034 itself.

A changed unit, a changed meaning, a new exception and a changed
signature are all invisible to it. A member still there that returns
millimetres where it returned feet is the shape that costs a model, and
nothing static finds it.

THE TRANSACTION RULE IS DERIVED, NEVER READ OUT OF THE WORDING
----------------------------------------------------------------
`writes` is declared by the caller because guessing it from the verb in
`does` is how a read opens an empty transaction "just in case" - which
the conventions forbid in as many words. And `documents` is asked for
because every `Transaction` and `TransactionGroup` constructor takes
exactly ONE `Document`: an operation touching two models is at best one
undo EACH, which is D-47 and must be said before it starts rather than
after.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_apichanges as ACI  # noqa: E402
import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-FRG-VAL-001's list, bound rather than retyped. D-05 does not
# extrapolate and neither does this.
RELEASES = FRAG.REVIT_VERSIONS

# What an operation has to say about itself before anything here is
# worth asking. Each one changes the answer, which is why none is
# guessed.
AN_OPERATION_CARRIES = (
    ("does", "the sentence a modeller would say. Without it there is no "
             "question to put to the host"),
    ("writes", "whether it CHANGES the model. The transaction rule turns "
               "on this, and reading it out of the verb in `does` is how "
               "a read opens an empty transaction"),
)

# What the host has to hand back for its answer to be checkable.
A_PROPOSAL_CARRIES = (
    ("members", "the Revit API members it would call. An answer naming "
                "none cannot be checked against anything"),
    ("transaction", "the shape it would use - 'none', 'transaction' or "
                    "'group'. An answer that does not say is an answer "
                    "somebody has to read the code to check"),
)

# The three shapes, and nothing else is one.
NONE, ONE, GROUP = "none", "transaction", "group"
SHAPES = (NONE, ONE, GROUP)


def _members(value):
    """Named members as a list, whatever shape they arrived in."""
    if value is None:
        return []
    if isinstance(value, str):
        return [value]
    if isinstance(value, (list, tuple, set, frozenset)):
        return [str(one).strip() for one in value if str(one).strip()]
    return [str(value).strip()]


def transaction_for(operation):
    """
    What this operation needs, from what it DOES rather than how it is
    worded.

    The conventions, applied rather than restated:
      - a read-only operation takes NO transaction, and an empty one
        "just in case" is forbidden in as many words;
      - a write is exactly ONE undo step, so a single-stage write is one
        `Transaction` and a multi-stage write is one named
        `TransactionGroup`;
      - one undo cannot cross two documents, because every constructor
        takes exactly one `Document`. That is Revit's shape and not a
        convention somebody could relax.
    """
    writes = bool(operation.get("writes"))
    stages = operation.get("stages")
    try:
        stages = int(stages)
    except (TypeError, ValueError):
        stages = None

    if not writes:
        return {"needs": False, "shape": NONE,
                "why": "it changes nothing. Counting, checking, listing, "
                       "exporting and reporting open no transaction, and "
                       "an empty one opened just in case is forbidden in "
                       "as many words."}
    if stages is not None and stages > 1:
        return {"needs": True, "shape": GROUP, "stages": stages,
                "why": "it writes in %d stages, and the user presses "
                       "Ctrl+Z ONCE. A named TransactionGroup is what "
                       "makes several stages one undo step; named for "
                       "what the user did, so Revit's undo history reads "
                       "properly." % stages}
    return {"needs": True, "shape": ONE,
            "why": "it writes, and every write is exactly one undo step. "
                   "One Transaction, named for what the user did, rolled "
                   "back on failure so the model is untouched."}


def look(operation, releases=None, proposal=None, found=None, path=None):
    """
    {asked, evidence, gone, transaction, why} - or a refusal.

    Nothing is called, nothing is compiled and nothing is changed. With
    no `proposal` the answer is the QUESTION and what to answer it with;
    with one, the answer is whether it may be used.
    """
    if not operation:
        return {"looked": False, "refused": "NOT_AN_OPERATION",
                "why": "no operation was handed in. 'Which API do I use' "
                       "with nothing to do is a question about the call."}

    operation = getattr(operation, "data", operation)
    if not isinstance(operation, dict):
        return {"looked": False, "refused": "NOT_AN_OPERATION",
                "why": "%r is not an operation. One carries %s."
                       % (operation, ", ".join(field for field, _
                                               in AN_OPERATION_CARRIES))}

    # `writes` is a BOOLEAN and False is a real answer, so its absence is
    # tested by key rather than by truth. `not operation.get("writes")`
    # would read a declared read-only operation as an undeclared one.
    absent = []
    if not str(operation.get("does") or "").strip():
        absent.append("does")
    if operation.get("writes") is None:
        absent.append("writes")
    if absent:
        return {"looked": False, "refused": "NOT_AN_OPERATION",
                "missing": absent,
                "why": "the operation is missing %s. %s"
                       % (", ".join(absent),
                          " ".join(why for field, why in AN_OPERATION_CARRIES
                                   if field in absent))}

    wanted = [str(one).strip() for one in (releases or [])
              if str(one).strip()]
    if not wanted:
        return {"looked": False, "refused": "NO_RELEASE",
                "why": "no release was named, so there is nothing to be "
                       "correct ON. What a release removed is a property "
                       "of that release, and an answer that holds on 2024 "
                       "and not on 2026 is two answers."}
    strangers = sorted(set(one for one in wanted if one not in RELEASES))
    if strangers:
        return {"looked": False, "refused": "UNKNOWN_RELEASE",
                "why": "%s is not one of the releases this project "
                       "supports: %s. D-05 does not extrapolate."
                       % (", ".join("'%s'" % one for one in strangers),
                          ", ".join(RELEASES))}

    # ONE UNDO CANNOT CROSS TWO DOCUMENTS, and this is checked before any
    # question of which member to call - an operation nobody can make
    # atomic is a design question, not an API one.
    documents = operation.get("documents")
    try:
        documents = int(documents) if documents is not None else 1
    except (TypeError, ValueError):
        documents = 1
    if bool(operation.get("writes")) and documents > 1:
        return {"looked": False, "refused": "WOULD_CROSS_TWO_DOCUMENTS",
                "documents": documents,
                "why": "it writes to %d documents. Every Transaction and "
                       "TransactionGroup constructor takes exactly ONE "
                       "Document, so this is at best one undo EACH - "
                       "that is Revit's shape rather than a convention, "
                       "and D-47 requires the operation to SAY SO BEFORE "
                       "IT STARTS rather than after." % documents}

    evidence = found if found is not None else ACI.evidence(path)
    if not evidence:
        return {"looked": False, "refused": "NO_EVIDENCE",
                "why": "nobody has produced the API evidence, so nothing "
                       "here knows what any release stopped shipping. "
                       "Run `python tools/api-changes.py` on a machine "
                       "with the Revit assemblies. An answer given "
                       "without it would be the confident kind this "
                       "agent exists to refuse."}

    # A RELEASE NOBODY LOOKED AT AND A RELEASE THAT REMOVED NOTHING ARE
    # NOT THE SAME ANSWER. HERON-REVIT-ACI-034's own distinction, bound
    # rather than restated - reading them the same way is what reported
    # every fragment clear.
    per_release, unlooked, baseline = {}, [], []
    for release in wanted:
        gone = ACI.removed_in(evidence, release)
        if gone is None:
            # THE EARLIEST RELEASE HAS NO TRANSITION INTO IT, and that is
            # a third answer rather than the unknown one. ACI's own
            # distinction, bound: nothing was removed AT the baseline
            # because there is nothing before it, which is not the same
            # as nobody having looked.
            (baseline if release == RELEASES[0] else unlooked).append(release)
            continue
        per_release[release] = gone
    if baseline:
        return {"looked": False, "refused": "NO_EARLIER_RELEASE",
                "releases": sorted(baseline),
                "why": "%s is the earliest release this project supports, "
                       "so there is no transition into it and nothing can "
                       "have been removed AT it. That is not the same as "
                       "nothing being removed, and it is not something to "
                       "read as a clear answer - ask about the releases "
                       "after it, which is where a member goes missing."
                       % ", ".join("'%s'" % one for one in sorted(baseline))}
    if unlooked:
        return {"looked": False, "refused": "NO_EVIDENCE_FOR_RELEASE",
                "releases": sorted(unlooked),
                "why": "the evidence holds no transition INTO %s, so "
                       "what %s removed is unknown rather than nothing. "
                       "A targeted run of api-changes.py holds one "
                       "transition, and reading a missing one as an "
                       "empty one is the plausible zero D-52 is about."
                       % (", ".join("'%s'" % one for one in sorted(unlooked)),
                          "it" if len(unlooked) == 1 else "they")}

    needs = transaction_for(operation)
    named = _members(operation.get("members"))
    checking = proposal is not None

    if checking:
        proposal = getattr(proposal, "data", proposal)
        if not isinstance(proposal, dict):
            return {"looked": False, "refused": "NOT_A_PROPOSAL",
                    "why": "%r is not a proposal. One carries %s."
                           % (proposal, ", ".join(
                               field for field, _ in A_PROPOSAL_CARRIES))}
        thin = [field for field, _why in A_PROPOSAL_CARRIES
                if not proposal.get(field)]
        if thin:
            return {"looked": False, "refused": "NOT_A_PROPOSAL",
                    "missing": thin,
                    "why": "the proposal is missing %s. %s"
                           % (", ".join(thin),
                              " ".join(why for field, why
                                       in A_PROPOSAL_CARRIES
                                       if field in thin))}
        named = named + _members(proposal.get("members"))

    # THE CHECK ITSELF. A member is matched against what each requested
    # release dropped, by its own name and by its last segment - real C#
    # has a `using` at the top and writes `id.IntegerValue`, so a
    # fully-qualified comparison finds nothing and clears everything.
    #
    # BOTH KINDS OF MATCH ARE REFUSED AND THEY ARE NOT THE SAME EVIDENCE,
    # which is a finding from the first real use of this agent. Asked about
    # an MEP operation, it flagged `MEPSystem.Name` because a release
    # dropped `RibbonItemData.Name` - a different class that happens to end
    # in the same word. `Name`, `IsConnected` and `Id` are common enough
    # that a tail match on them is usually a collision.
    #
    # It still REFUSES on a tail match, because the case this agent exists
    # for is exactly that shape: somebody writes `id.IntegerValue` and
    # the removed member is the same property under its full vendor
    # namespace, where `id` is a VARIABLE and no comparison of class
    # names could ever connect the two. Failing safe is the whole point.
    # (The namespace is not spelled here - the adapter boundary keeps
    # that prefix inside revit/, and check-structure greps file text.)
    #
    # What changes is that the answer SAYS WHICH IT IS, so a reader can
    # tell a real removal from a common word, instead of being handed a
    # list that is right and unreadable.
    gone = []
    for member in sorted(set(named)):
        tail = member.rsplit(".", 1)[-1]
        # `.ElementId.IntegerValue` - the member as written, anchored, so a
        # removed name ending in those exact segments is the same member
        # under a longer namespace rather than a word that matches.
        suffix = "." + member
        for release in sorted(per_release):
            for dropped in per_release[release]:
                exact = dropped == member or dropped.endswith(suffix)
                if not exact and dropped.rsplit(".", 1)[-1] != tail:
                    continue
                gone.append({
                    "member": member, "release": release,
                    "removed": dropped,
                    "match": "exact" if exact else "same-name",
                    "why": "the removed member IS this one, under its full "
                           "namespace" if exact else
                           "only the last segment matches - '%s' was "
                           "removed from a DIFFERENT class. Refused anyway, "
                           "because a member written against a variable "
                           "('id.IntegerValue') can never be matched by "
                           "class and that is the case this agent exists "
                           "for. Check this one by eye." % dropped})
                break

    summary = dict(
        (release, {"removed": len(per_release[release]),
                   "of_ours": sorted(set(one["member"] for one in gone
                                         if one["release"] == release))})
        for release in per_release)

    answer = {
        "looked": True, "fixed": False,
        "releases": wanted, "evidence": summary,
        "gone": gone, "transaction": needs,
        "unjudged": _unjudged(wanted, per_release, named, gone, checking),
    }

    if gone:
        return dict(answer, refused="MEMBER_IS_GONE",
                    accepted=False if checking else None,
                    why="%s. Refused rather than reported: there is no "
                        "compiler here to catch it and a wrong answer "
                        "about the API reads exactly as confident as a "
                        "correct one.%s"
                        % ("; ".join("'%s' is not in %s (%s)"
                                     % (one["member"], one["release"],
                                        one["match"])
                                     for one in gone),
                           "" if all(one["match"] == "exact" for one in gone)
                           else " NOTE: a `same-name` match means only the "
                                "LAST SEGMENT matched something removed "
                                "from another class - common words like "
                                "`Name` and `Id` collide that way. Those "
                                "are worth checking by eye before "
                                "believing."))

    if checking:
        shape = str(proposal.get("transaction") or "").strip().lower()
        if shape not in SHAPES:
            return dict(answer, refused="TRANSACTION_IS_WRONG",
                        accepted=False, proposed=shape,
                        why="'%s' is not one of %s. An unknown word is "
                            "refused rather than read as the safest one."
                            % (shape, ", ".join(SHAPES)))
        if shape != needs["shape"]:
            return dict(answer, refused="TRANSACTION_IS_WRONG",
                        accepted=False, proposed=shape,
                        why="it proposes '%s' and this operation needs "
                            "'%s'. %s"
                            % (shape, needs["shape"], needs["why"]))
        return dict(answer, accepted=True,
                    why="%d member(s) checked against what %s removed, "
                        "and none of them is gone. The transaction shape "
                        "matches: %s"
                        % (len(set(named)), ", ".join(wanted),
                           needs["why"]))

    return dict(answer, asked=_question(operation, wanted, needs),
                why="%d release(s) asked about, %d member(s) already "
                    "named and none of them gone. The question is framed "
                    "and NOT answered - the one scoped call is the "
                    "host's under D-01."
                    % (len(wanted), len(set(named))))


def _question(operation, wanted, needs):
    """The wording of the call, with what it has to satisfy attached."""
    return (
        "Which Revit API members would do this, on EVERY one of %s?\n"
        "  What it does: %s\n"
        "  It %s the model%s.\n"
        "  Answer with the fully-qualified members and the transaction "
        "shape ('%s' here, because %s)\n"
        "  Name nothing you have not seen in the API of the OLDEST "
        "release above - a member added later is a member the others do "
        "not have, and D-05 does not extrapolate."
        % (", ".join(wanted), str(operation.get("does")).strip(),
           "CHANGES" if operation.get("writes") else "does NOT change",
           "" if not operation.get("writes") else
           ", in %s" % ("one stage" if needs["shape"] == ONE
                        else "%s stages" % needs.get("stages", "several")),
           needs["shape"], needs["why"].rstrip(".")))


def _unjudged(wanted, per_release, named, gone, checking):
    total = sum(len(one) for one in per_release.values())
    return [
        "NO REVIT WAS CALLED AND NOTHING WAS COMPILED. This reads a "
        "digest of what each release stopped shipping and compares "
        "names. The one scoped call the register gives this row is the "
        "HOST's under D-01, and this agent knows no Revit API of its "
        "own - that is the point rather than a limitation.",
        "A MEMBER THIS DID NOT WARN ABOUT IS UNWARNED, NOT PROVEN "
        "PRESENT. The evidence records what each release REMOVED, not "
        "the whole surface, so %d name(s) came back clear against %d "
        "removal(s) across %s - and clear here is the absence of a "
        "warning. Reading it as proof is D-52's plausible zero, which "
        "has already happened once inside the agent that holds this "
        "evidence."
        % (len(set(named)) - len(set(one["member"] for one in gone)),
           total, ", ".join(wanted)),
        "A CHANGED UNIT, A CHANGED MEANING, A NEW EXCEPTION AND A "
        "CHANGED SIGNATURE ARE ALL INVISIBLE. A member still there that "
        "returns millimetres where it returned feet is the shape that "
        "costs a model, and no comparison of names finds it.",
        "%s" % ("EVERY MATCH ABOVE WAS EXACT - the removed member is this "
                "one under its full namespace." if not gone or
                all(one["match"] == "exact" for one in gone) else
                "%d MATCH(ES) ARE `same-name` AND NEED AN EYE: only the "
                "last segment matched, against a removal from a DIFFERENT "
                "class. This agent refuses them anyway, because a member "
                "written against a variable can never be matched by class "
                "and that is the case it exists for - but `Name`, `Id` and "
                "`IsConnected` collide that way often enough that the "
                "distinction is reported rather than hidden."
                % len([one for one in gone
                       if one["match"] != "exact"])),
        "WHETHER THE TRANSACTION SHAPE IS RIGHT WAS DERIVED FROM WHAT "
        "THE CALLER DECLARED, not from the wording. `writes` is asked "
        "for rather than read out of the verb in `does`, because a read "
        "that opens an empty transaction just in case is the thing the "
        "conventions forbid in as many words.",
        "%s" % ("WHETHER THIS IS THE RIGHT OPERATION AT ALL. Correct API "
                "for the wrong job is still the wrong job, and that is a "
                "person's answer." if not checking else
                "WHETHER THE HOST UNDERSTOOD THE QUESTION. What came back "
                "was checked against the evidence and against the "
                "transaction rule; that it does what the operation "
                "described is not checkable here and needs a compiler "
                "and a model."),
    ]


def main(argv):
    print("THE REVIT API AGENT   it frames the question and checks the answer")
    print("=" * 72)

    found = ACI.evidence()
    if not found:
        print("\n  no evidence - run tools/api-changes.py on a machine with")
        print("  the Revit assemblies. Nothing below can run without it.")
        return 0

    move = {"does": "move the selected ducts up 200 mm",
            "writes": True, "documents": 1}

    print("\n1. no proposal - the question, and what to answer it with")
    answer = look(move, releases=["2024", "2026"], found=found)
    print("  %s" % answer["why"])
    for line in answer["asked"].split("\n"):
        print("    %s" % line)

    print("\n2. a proposal naming a member 2026 does not have")
    answer = look(move, releases=["2024", "2026"], found=found,
                  proposal={"members": ["ElementId.IntegerValue"],
                            "transaction": "transaction"})
    print("  %s  %s" % (answer["refused"], answer["why"][:96]))

    print("\n3. the same operation, on releases that still have it")
    answer = look(move, releases=["2024"], found=found,
                  proposal={"members": ["ElementId.IntegerValue"],
                            "transaction": "transaction"})
    print("  accepted=%s  %s" % (answer.get("accepted"), answer["why"][:88]))

    print("\n4. a READ that proposes a transaction anyway")
    answer = look({"does": "count the ducts", "writes": False},
                  releases=["2024"], found=found,
                  proposal={"members": ["FilteredElementCollector"],
                            "transaction": "transaction"})
    print("  %s  %s" % (answer["refused"], answer["why"][:96]))

    print("\n5. a write across two documents")
    answer = look({"does": "copy levels into the linked model",
                   "writes": True, "documents": 2},
                  releases=["2024"], found=found)
    print("  %s  %s" % (answer["refused"], answer["why"][:96]))

    print("\nwhat it does NOT judge")
    for line in answer.get("unjudged") or look(
            move, releases=["2024"], found=found)["unjudged"]:
        print("  - %s" % line[:110])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
