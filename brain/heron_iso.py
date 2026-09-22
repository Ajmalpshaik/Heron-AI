# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-ISO-003
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
External standards - no source, no claim.

    python brain/heron_iso.py "what does ISO 19650 call the CDE" \\
        --standard "ISO 19650"

WHAT IT IS FOR (docs/28, HERON-STD-ISO-003)
--------------------------------------------
"ISO 19650 and related. CITES, NEVER INVENTS." T2, risk ANALYZE.

THE RULE IS WRITTEN OUT IN docs/05 s150 AND IT IS NOT A PREFERENCE
--------------------------------------------------------------------
    "Invented standards. s47 already forbids this. Enforce it
     STRUCTURALLY: any claim about ISO 19650, QCS, Ashghal or a company
     standard must carry a citation to an indexed source document. NO
     SOURCE, NO CLAIM. A standards answer with no citation is a BUG, not
     a low-confidence answer."

Structurally is the word that matters. This agent cannot produce a
sentence about a standard, because the only text it can return is text
it copied out of an indexed chunk. There is no path through it that
writes prose about ISO 19650, and that is the design rather than a
promise.

HERON DOES NOT SHIP ANY OF THESE STANDARDS
--------------------------------------------
ISO 19650 is bought. So is QCS. Heron ships under Apache 2.0 (D-08) and
carries no copy of either, so with nothing indexed the honest answer is
that Heron HAS NO COPY of the standard - not a summary of what it says.
Knowing roughly what a standard says is exactly the thing this row
forbids being offered as an answer.

WHICH STANDARD IS ASKED FOR, NEVER GUESSED
--------------------------------------------
"What does the standard say about X" names no standard, and picking one
would be choosing whose rules a person is about to follow. So the
standard is a required input - D-33, asked once - and a clause only
counts as coming FROM that standard when the document's own title says
so. Nothing is inferred from a clause's contents.

A DOCUMENT THAT MATCHED IS NOT THE SAME AS THE STANDARD
---------------------------------------------------------
The retrieval will happily return the company's own method statement
for a question about ISO 19650, because it talks about the same things.
Those clauses come back LABELLED as not being the standard, and the
answer's `claimed` stays false. "Something relevant was found" and "the
standard says this" are different sentences and only one of them is
what was asked for.

ONE SCOPE AT A TIME, THROUGH THE LIBRARIAN
--------------------------------------------
HERON-RAG-RNK-006's `librarian` asks each scope separately and returns
one labelled answer per scope, never a union - Golden Rule 5, and its
own docstring says there is no argument that could take a merged query.
This agent calls it and keeps the answers apart.

THE CLAUSES ARE NOT REDISTRIBUTABLE, AND THE ANSWER SAYS SO
-------------------------------------------------------------
A quoted ISO clause is somebody else's copyrighted text sitting inside
Heron's answer. Reading it on this machine is what the licence was
bought for; putting it in a pull request is not. Every clause from a
named standard is marked `redistributable: False` so that
HERON-RPT-RED-003 and HERON-GIT-COM-010 have something to refuse on -
D-66 and Q-53's whole argument, one step earlier.
"""

from __future__ import annotations

import os
import re
import sqlite3
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_retrieve as RETRIEVE  # noqa: E402
import heron_scope as SCOPE  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-RAG-RNK-006's one-scope-at-a-time lookup, bound rather than
# reimplemented. Its own docstring: "there is no argument here that could
# take a merged query".
librarian = RETRIEVE.librarian

# docs/05 s150's own words, kept as the rule this agent enforces.
NO_SOURCE_NO_CLAIM = (
    "any claim about ISO 19650, QCS, Ashghal or a company standard must "
    "carry a citation to an indexed source document. No source, no claim. "
    "A standards answer with no citation is a bug, not a low-confidence "
    "answer (docs/05 s150)")

# Why a quoted clause may be read here and may not be published.
NOT_REDISTRIBUTABLE = (
    "this is somebody else's copyrighted standard. Heron ships under "
    "Apache 2.0 (D-08) and carries no copy of it - the licence that was "
    "bought covers reading it on this machine, not putting it in a pull "
    "request. HERON-RPT-RED-003 and HERON-GIT-COM-010 are what refuse")

_SPACE = re.compile(r"\s+")


def _folded(text):
    """Lower case, one space - and a spaceless twin, for ISO19650."""
    flat = _SPACE.sub(" ", str(text or "")).strip().lower()
    return flat, flat.replace(" ", "")


def names_the_standard(title, standard):
    """
    Whether a document's TITLE says it is that standard.

    The title and nothing else. A clause that mentions ISO 19650 is a
    clause about it, not a clause FROM it, and treating the two as one
    is how a company method statement starts answering as the standard.
    """
    if not title or not standard:
        return False

    # IT HAS TO BEGIN WITH THE DESIGNATION, NOT MERELY CONTAIN IT. A
    # substring test passed "Acme guide to ISO 19650 compliance" and
    # "Deviations from ISO 19650" - which are the two commonest titles a
    # COMPANY document about the standard actually has, and both were
    # then presented as the standard speaking. That is the docstring
    # above being contradicted one level up: a title that mentions
    # ISO 19650 is a title about it, not a title FROM it.
    #
    # A leading adoption prefix is allowed and is recognised
    # mechanically rather than from a list: "BS EN ISO 19650-2:2018" is
    # the British adoption and is the standard, and everything before
    # the designation there is upper case. "Acme guide to" is not, and
    # neither is "Deviations from". No vocabulary is invented, and a
    # prefix nobody anticipated still works as long as it is written the
    # way standards bodies write theirs.
    #
    # Found by a review on 2026-09-15.
    raw = _SPACE.sub(" ", str(title)).strip()
    kept = []
    for word in raw.split(" "):
        if word and word == word.upper() and not any(
                letter.islower() for letter in word):
            kept.append(word)
            continue
        break
    for start in range(len(kept) + 1):
        flat, tight = _folded(" ".join(raw.split(" ")[start:]))
        wanted, wanted_tight = _folded(standard)
        if flat.startswith(wanted) or tight.startswith(wanted_tight):
            return True
    return False


def _nothing_claimed(standard, per_scope, elsewhere, unreadable):
    """Why nothing was claimed - and a FAULT is never reported as an absence.

    "Heron has no copy of ISO 19650 indexed" is the most useful sentence this
    agent has when it is true, and the worst one it could write when it is
    not: it is confident, it is specific, and a reader acts on it by going to
    find the document they already loaded. A store that answered the search
    and then failed on the words has to say so instead.
    """
    where = ", ".join(card["label"] for card in per_scope) or "any scope"
    if unreadable:
        return ("%d CLAUSE(S) MATCHED AND THEIR WORDS COULD NOT BE READ, so "
                "this is NOT 'no copy indexed' - the store answered the "
                "search and then failed on the text: %s. Nothing is claimed, "
                "and the reason is a fault rather than an absence."
                % (len(unreadable),
                   "; ".join(sorted(set(one["why"] for one in unreadable)))))
    return ("HERON HAS NO COPY OF %s indexed in %s. %d other document(s) "
            "matched the question and none of them IS the standard, so "
            "there is no claim to make." % (standard, where, len(elsewhere)))


def cite(question, standard=None, scopes=None, project=None,
         project_name=None, limit=5):
    """
    {claimed, clauses, elsewhere, scopes} - or a refusal. Every word of
    every clause is copied out of an indexed document.
    """
    question = str(question or "").strip()
    if not question:
        return {"claimed": False, "refused": "NOTHING_ASKED",
                "why": "no question was handed in."}

    standard = str(standard or "").strip()
    if not standard:
        return {"claimed": False, "refused": "NO_STANDARD_NAMED",
                "why": "which standard? `the standard` names none, and "
                       "picking one would be choosing whose rules somebody "
                       "is about to follow. D-33 - asked once, never "
                       "assumed."}

    asked = librarian(question, scopes=scopes, project=project,
                      project_name=project_name, limit=limit)

    from_it, elsewhere, per_scope, unreadable = [], [], [], []
    for one in asked:
        card = {"scope": one.scope, "label": one.label,
                "skipped": one.skipped,
                "route": one.answer.route if one.answer else None,
                "note": one.answer.note if one.answer else None}
        per_scope.append(card)
        if one.skipped or not one.answer or one.answer.route != "documents":
            continue

        # THE WORDS ARE NOT IN THE CANDIDATE and librarian closed the store,
        # so it is reopened to read them - the same seam heron_conflict
        # reopens for, and for the same reason. The question is not asked
        # again; only the text is read.
        try:
            store = SCOPE.open_scope(one.scope, project)
        except Exception as why:
            card["skipped"] = "could not be reopened for its text: %s" % why
            continue
        try:
            for hit in one.answer.candidates:
                # A READ THAT FAILED IS NOT A CLAUSE THAT IS NOT THERE.
                # This was `except Exception: row = None` and then the
                # `continue` below, so a locked or malformed store dropped
                # the clause without a word and the answer went out saying
                # HERON HAS NO COPY OF <standard> indexed - a confident,
                # specific, FALSE sentence about a store that has it, in the
                # module whose headline rule is NO SOURCE, NO CLAIM. D-52's
                # plausible zero. Row 5b-114.
                try:
                    row = store.execute(
                        "SELECT text FROM chunks WHERE id = ?",
                        (hit["id"],)).fetchone()
                except sqlite3.DatabaseError as why:
                    unreadable.append({
                        "scope": one.scope, "label": one.label,
                        "document": hit.get("document"),
                        "locator": hit.get("locator"),
                        "why": "%s: %s" % (type(why).__name__, why)})
                    continue
                # A chunk that is genuinely absent, or genuinely empty, is
                # still skipped: that is a fact about the store's contents
                # rather than a fault reading it, and the two must not be
                # reported as one thing.
                if row is None or not row["text"]:
                    continue
                clause = {
                    "scope": one.scope, "label": one.label,
                    "document": hit.get("document"),
                    "documentId": hit.get("document_id"),
                    "locator": hit.get("locator"),
                    "heading": hit.get("heading_path"),
                    "path": hit.get("path"),
                    "text": row["text"],
                    "isTheStandard": names_the_standard(hit.get("document"),
                                                        standard),
                }
                if clause["isTheStandard"]:
                    clause["redistributable"] = False
                    clause["why"] = NOT_REDISTRIBUTABLE
                    from_it.append(clause)
                else:
                    clause["why"] = ("this matched the question and its "
                                     "title does not say it is %s, so it "
                                     "is not the standard speaking"
                                     % standard)
                    elsewhere.append(clause)
        finally:
            try:
                store.close()
            except Exception:
                pass

    return {
        "claimed": bool(from_it),
        "standard": standard,
        "of": len(from_it),
        "clauses": from_it,
        "elsewhere": elsewhere,
        "unreadable": unreadable,
        "scopes": per_scope,
        "sources": sorted(set(card["document"] for card in from_it
                              if card["document"])),
        "asks": ([{"question": question, "standard": standard,
                   "which": "which of these %d clause%s of %s answers it? "
                            "Quote it - the words are the standard's."
                            % (len(from_it), "" if len(from_it) == 1 else "s",
                               standard),
                   "clauses": from_it}]
                 if from_it else
                 [{"question": question, "standard": standard,
                   "which": "nothing indexed IS %s, so there is no answer to "
                            "give. %s" % (standard, NO_SOURCE_NO_CLAIM),
                   "clauses": []}]),
        "why": ("%d clause%s of %s across %d scope%s asked. %s"
                % (len(from_it), "" if len(from_it) == 1 else "s", standard,
                   len(per_scope), "" if len(per_scope) == 1 else "s",
                   "%d other document(s) matched the question and are not "
                   "the standard." % len(elsewhere) if elsewhere
                   else "Nothing else matched.")
                if from_it else _nothing_claimed(standard, per_scope,
                                                 elsewhere, unreadable)),
        "unjudged": [
            "NO SOURCE, NO CLAIM. %s Every word of every clause above was "
            "copied out of an indexed chunk - there is no path through this "
            "agent that writes a sentence about a standard." % NO_SOURCE_NO_CLAIM,
            ("%d DOCUMENT%s MATCHED THE QUESTION AND %s NOT %s. Relevance is "
             "not authorship: the company's own method statement talks about "
             "the same things, and `the standard says this` is a different "
             "sentence from `something relevant was found`."
             % (len(elsewhere), "" if len(elsewhere) == 1 else "S",
                "IS" if len(elsewhere) == 1 else "ARE", standard)
             if elsewhere else
             "nothing came back that was not %s, so there was nothing to "
             "tell apart." % standard),
            ("%d CLAUSE%s MARKED NOT REDISTRIBUTABLE. %s"
             % (len(from_it), "" if len(from_it) == 1 else "S",
                NOT_REDISTRIBUTABLE)
             if from_it else
             "nothing was quoted, so nothing had to be marked "
             "unpublishable."),
            "WHICH CLAUSE ANSWERS THE QUESTION. That is language, and "
            "docs/28 makes this row T2 for it. The clauses go to the host "
            "with the question attached.",
            "EACH SCOPE WAS ASKED ON ITS OWN AND NOTHING WAS MERGED - "
            "HERON-RAG-RNK-006's librarian, whose own docstring says there "
            "is no argument that could take a merged query (Golden Rule 5).",
            "WHETHER THE INDEXED COPY IS THE CURRENT EDITION. A superseded "
            "revision of a standard answers exactly as confidently as the "
            "one in force, and nothing here knows which is which.",
        ],
    }


def main(argv):
    import io
    import shutil
    import tempfile

    print("EXTERNAL STANDARDS   no source, no claim")
    print("=" * 72)
    print("\nthe rule: %s" % NO_SOURCE_NO_CLAIM)

    question = "what does the standard call the common data environment"
    standard = "ISO 19650"
    if "--standard" in argv:
        at = argv.index("--standard")
        # A FLAG WITH NO VALUE IS A TYPO, NOT AN ABSENT FLAG. `--standard`
        # last on the line answered with `IndexError: list index out of
        # range` at exit 1 - a Python traceback as the answer to a typo,
        # which is exactly what row 5b-104 fixed in four other command
        # lines. THIS SITE WAS MISSED BY THAT SCAN because it reads
        # `argv[at + 1]` and the grep was for `argv[i + 1]`: row 5b-95's
        # lesson, that a scan over source text is not a measurement.
        # Row 5b-115.
        if at + 1 >= len(argv) or argv[at + 1].startswith("-"):
            print("\n  --standard needs a value, e.g. --standard "
                  "\"ISO 19650\". Which standard is never guessed - D-33, "
                  "and picking one would be choosing whose rules somebody "
                  "is about to follow.")
            return 2
        standard = argv[at + 1]
        argv = argv[:at] + argv[at + 2:]

    # AND A FLAG THIS TOOL DOES NOT HAVE IS REFUSED, NEVER SEARCHED FOR.
    # Everything left over becomes the question, so `--top 5` was asked as
    # part of it and the answer came back at exit 0 about a question nobody
    # wrote. Row 5b-112's shape, one module along, and heron_retrieve has
    # refused unknown flags in these words since 2026-08-30.
    unknown = [word for word in argv if word.startswith("-")]
    if unknown:
        print("\n  not a flag this tool has: %s" % " ".join(unknown))
        print('  python brain/heron_iso.py "what does it call the CDE" '
              '--standard "ISO 19650"')
        print("  the only flag is --standard, and it takes a value")
        return 2

    if argv:
        question = " ".join(argv)

    made = None
    if not os.environ.get("HERON_KNOWLEDGE"):
        made = tempfile.mkdtemp(prefix="heron-iso-")
        os.environ["HERON_KNOWLEDGE"] = made
        import heron_ingest as INGEST
        import heron_search as SEARCH
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.COMPANY)
        # DELIBERATELY NOT THE STANDARD. A company method statement that
        # talks about the same thing is exactly what must not answer as
        # the standard.
        paper = os.path.join(made, "Acme Information Management Plan.md")
        with io.open(paper, "w", encoding="utf-8") as handle:
            handle.write("# Acme Information Management Plan\n\n"
                         "## 2 Common data environment\n\n"
                         "The project common data environment shall be "
                         "hosted on Acme's own server.\n")
        INGEST.ingest(store, paper, added_by="demo", source_trust="company")
        SEARCH.index_chunks(store)
        store.close()

    try:
        answer = cite(question, standard=standard,
                      scopes=[SCOPE.COMPANY, SCOPE.GLOBAL])
        print("\n%s" % answer["why"])
        print("\nscopes asked, separately")
        for card in answer["scopes"]:
            print("  %-12s %s" % (card["label"],
                                  card["skipped"] or card["route"]))
        for clause in answer["clauses"]:
            print("\n  FROM %s  %s" % (standard, clause["document"]))
            print("    %s" % " ".join(clause["text"].split())[:64])
        for clause in answer["elsewhere"]:
            print("\n  NOT THE STANDARD  %s" % clause["document"])
            print("    %s" % " ".join(clause["text"].split())[:64])
        for one in answer["asks"]:
            print("\n  ASKS  %s" % one["which"][:64])

        print("\nrefused")
        for q, std in ((None, standard), ("   ", standard),
                       (question, None), (question, "  ")):
            bad = cite(q, standard=std)
            print("  %-22s %s" % (bad["refused"], bad["why"][:42]))

        print("\nwhat this agent does not judge")
        for line in answer["unjudged"]:
            print("  - %s" % line)
    finally:
        if made:
            os.environ.pop("HERON_KNOWLEDGE", None)
            shutil.rmtree(made, ignore_errors=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
