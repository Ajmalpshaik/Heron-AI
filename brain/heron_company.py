# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-CMP-002
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The company standard - it cites, and with nothing to cite it says so.

    python brain/heron_company.py "how thick should duct insulation be"

WHAT IT IS FOR (docs/28, HERON-STD-CMP-002)
--------------------------------------------
"The organisation's own approved standard." T2, risk ANALYZE.

IT ANSWERS FROM ONE SCOPE AND NOWHERE ELSE
--------------------------------------------
heron_scope's own comment on `open_scope`: "The ONLY way in. One scope,
never a list - there is no signature here that a cross-scope query could
be written against." This agent opens COMPANY and refuses to run against
any other store it is handed.

That refusal is not paranoia. HERON-STD-PRJ-009 exists because a project
standard OUTRANKS the company default and has to SAY it is doing so
(docs/20 s2). A company-standard agent that quietly answered out of the
project store would make that override invisible at the one moment it
matters - and "using the company standard" would be a false sentence
about where the number came from.

IT CITES, AND IT NEVER WRITES A SENTENCE OF ITS OWN
-----------------------------------------------------
docs/28 gives HERON-STD-ISO-003 the rule "CITES, NEVER INVENTS", and it
is the same rule here: what comes back is clauses, each with its
document and its locator, in the words they were written in. Nothing is
summarised, nothing is converted - reporting 40mm as 4cm would be
correcting somebody's standard on the way past, which heron_conflict
already refuses to do for the same reason.

If the answer is not in a document somebody loaded, there is no answer.
Heron knowing what ISO 19650 generally says is not the company's
standard, and offering it as one is the failure this row exists to
prevent.

FOUR NOTHINGS, CARRIED THROUGH UNALTERED
------------------------------------------
HERON-RAG-RNK-006 went to some lengths to keep these apart, and
flattening them here would undo it one level up:

    empty        no document is indexed in this scope. Nothing has been
                 put in yet - not a search result at all
    unindexed    documents are in, and the searchable text has not been
                 built. Also not a search result
    nothing      indexed documents were searched and none matched
    retired      every document is there and none is offered

Each comes back with its own route and that agent's own note, word for
word. A single "no company standard found" would be one sentence for
four different problems, three of which somebody can fix in a minute.

WHAT IT DOES NOT DO
---------------------
It does not decide which clause answers the question - that is language,
and the clauses go to the host with the question attached (T2).

It does not compare with the project. Two scopes disagreeing is
HERON-RAG-CNF-015's to surface and HERON-STD-PRJ-009's to rule on, and
this agent opening a second store to check would be the cross-scope
query heron_scope has no signature for.
"""

from __future__ import annotations

import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_retrieve as RETRIEVE  # noqa: E402
import heron_scope as SCOPE  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The one scope this agent may read, by identity.
SCOPE_NAME = SCOPE.COMPANY

# HERON-RAG-RNK-006's document lookup, called rather than reimplemented.
find_documents = RETRIEVE.find_documents

# The route that actually carries clauses. Everything else is one of the
# four nothings, and each keeps its own name.
FOUND = "documents"


def ask(question, store=None):
    """
    {answered, clauses, ask} - or a refusal. Nothing is summarised and
    no clause is converted.
    """
    question = str(question or "").strip()
    if not question:
        return {"answered": False, "refused": "NOTHING_ASKED",
                "why": "no question was handed in. A standard answers a "
                       "question; it is not a thing to be listed."}

    opened = False
    if store is None:
        try:
            store = SCOPE.open_scope(SCOPE_NAME)
            opened = True
        except (ValueError, OSError) as error:
            return {"answered": False, "refused": "NO_STORE",
                    "why": "the company scope could not be opened: %s"
                           % error}

    if getattr(store, "scope", None) != SCOPE_NAME:
        return {"answered": False, "refused": "WRONG_SCOPE",
                "why": "this is the %r store, not %r. A company-standard "
                       "agent answering out of another scope would make an "
                       "override invisible at the one moment it matters - "
                       "HERON-STD-PRJ-009 exists for that, and docs/20 s2 "
                       "says an override must never be silent."
                       % (getattr(store, "scope", None), SCOPE_NAME)}

    try:
        answer = find_documents(store, question)
        # THE CLAUSE TEXT IS FETCHED WHILE THE STORE IS STILL OPEN, and it
        # is not in the candidate. HERON-RAG-RNK-006 returns the title, the
        # locator and the path - what a person needs to GO AND OPEN it -
        # and leaves the words where they live. A citing agent has to read
        # them, and heron_conflict reads them the same way for the same
        # reason.
        text = {}
        if answer.route == FOUND:
            for hit in answer.candidates:
                try:
                    row = store.execute(
                        "SELECT text FROM chunks WHERE id = ?",
                        (hit["id"],)).fetchone()
                except Exception:
                    row = None
                text[hit["id"]] = row["text"] if row else None
    finally:
        if opened:
            try:
                store.close()
            except Exception:
                pass

    if answer.route != FOUND:
        # NOT A REFUSAL. Four different nothings, each keeping the note
        # HERON-RAG-RNK-006 wrote for it - three of the four are something
        # somebody can fix in a minute, and one sentence for all four
        # would hide which.
        return {
            "answered": False,
            "route": answer.route,
            "clauses": [],
            "asks": [],
            "why": "the company standard has no answer to %r: %s"
                   % (question, answer.note),
            "unjudged": [
                "NOTHING WAS INVENTED. %s. What Heron generally knows about "
                "a standard is not this company's standard, and offering it "
                "as one is the failure docs/28 gives this row."
                % answer.note,
                "THE ROUTE IS %r AND NOT JUST `nothing`. HERON-RAG-RNK-006 "
                "keeps four of these apart - no document indexed, documents "
                "unindexed, nothing matched, everything retired - and three "
                "of them are fixable in a minute. Flattening them here "
                "would undo that one level up." % answer.route,
                "NO OTHER SCOPE WAS READ. The project store may well have "
                "an answer, and finding it is HERON-STD-PRJ-009's, not "
                "this agent's - heron_scope has no signature a cross-scope "
                "query could be written against.",
            ],
        }

    clauses, unreadable = [], []
    for candidate in answer.candidates:
        words = text.get(candidate["id"])
        card = {
            "document": candidate.get("document"),
            "documentId": candidate.get("document_id"),
            "locator": candidate.get("locator"),
            "heading": candidate.get("heading_path"),
            "path": candidate.get("path"),
            "status": candidate.get("status"),
            "untrusted": candidate.get("untrusted"),
            "text": words,
            "scope": SCOPE_NAME,
        }
        # A CITATION WITH NO WORDS IS NOT A CITATION. A chunk the shortlist
        # named and the store could not read is reported as that, never as
        # an empty clause somebody might quote.
        (clauses if words else unreadable).append(card)

    return {
        "answered": True,
        "route": answer.route,
        "of": len(clauses),
        "clauses": clauses,
        "unreadable": unreadable,
        "asks": [{
            "question": question,
            "which": "which of these %d clause%s answers it, and does any "
                     "of them not? Quote, never paraphrase - the number in "
                     "the clause is the company's number."
                     % (len(clauses), "" if len(clauses) == 1 else "s"),
            "clauses": clauses,
        }],
        "sources": sorted(set(card["document"] for card in clauses
                              if card["document"])),
        "why": "%d clause%s from the company standard for %r, across %d "
               "document%s. %s"
               % (len(clauses), "" if len(clauses) == 1 else "s", question,
                  len(set(card["document"] for card in clauses)),
                  "" if len(set(card["document"]
                                for card in clauses)) == 1 else "s",
                  answer.note),
        "unjudged": [
            "WHICH CLAUSE ANSWERS THE QUESTION. That is language, and "
            "docs/28 makes this row T2 for it. The clauses go to the host "
            "with the question attached; nothing here picked one.",
            "NOTHING WAS SUMMARISED AND NO NUMBER WAS CONVERTED. The "
            "clauses are in the words they were written in. Reporting 40mm "
            "as 4cm would be correcting somebody's standard on the way "
            "past, which heron_conflict refuses for the same reason.",
            "NO OTHER SCOPE WAS READ, so nothing here knows whether the "
            "PROJECT says something different. HERON-RAG-CNF-015 surfaces "
            "that and HERON-STD-PRJ-009 rules on it - and docs/20 s2 says "
            "the override must be said out loud when it happens.",
            ("%d CHUNK%s WAS ON THE SHORTLIST AND ITS WORDS COULD NOT BE "
             "READ, so %s named rather than offered as an empty clause "
             "somebody might quote."
             % (len(unreadable), "" if len(unreadable) == 1 else "S",
                "it is" if len(unreadable) == 1 else "they are")
             if unreadable else
             "every clause on the shortlist came back with its words."),
            "WHETHER THE COMPANY STANDARD IS RIGHT, CURRENT, OR COMPLETE. "
            "It is what somebody loaded. A superseded revision still in the "
            "store answers exactly as confidently as the current one.",
        ],
    }


def main(argv):
    print("COMPANY STANDARD   it cites, and with nothing to cite it says so")
    print("=" * 72)
    print("\nscope: %s, and no signature here can read a second one"
          % SCOPE_NAME)
    print("lookup: %s.%s" % (find_documents.__module__,
                             find_documents.__name__))

    question = " ".join(argv) or "how thick should duct insulation be"

    made = None
    if not os.environ.get("HERON_KNOWLEDGE"):
        # A COMPANY STANDARD TO CITE, so the demo shows the found path
        # rather than only the honest empty one. Thrown away afterwards.
        import shutil
        import tempfile
        made = tempfile.mkdtemp(prefix="heron-company-")
        os.environ["HERON_KNOWLEDGE"] = made
        import heron_ingest as INGEST
        import heron_search as SEARCH
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE_NAME)
        paper = os.path.join(made, "Acme BIM Standard 2026.md")
        with io.open(paper, "w", encoding="utf-8") as handle:
            handle.write("# Acme Engineering BIM Standard 2026\n\n"
                         "## 3 Mechanical\n\n"
                         "### 3.1 Ductwork insulation\n\n"
                         "Ducts shall be insulated to 30mm, EXCEPT where "
                         "installed within conditioned spaces.\n\n"
                         "### 3.2 Duct naming\n\n"
                         "Every duct type shall be named "
                         "SYSTEM-SIZE-MATERIAL.\n")
        INGEST.ingest(store, paper, added_by="demo",
                      source_trust="company")
        SEARCH.index_chunks(store)
        store.close()

    try:
        answer = ask(question)
        print("\n%s" % answer.get("why", answer.get("refused")))

        for clause in answer.get("clauses", []):
            print("\n  %s %s" % (clause["document"], clause["locator"] or ""))
            print("    %s" % str(clause["text"] or "")[:70])
        for one in answer.get("asks", []):
            print("\n  ASKS  %s" % one["which"][:64])

        print("\nrefused")
        for these in ("", "   ", None):
            bad = ask(these)
            print("  %-18s %s" % (bad["refused"], bad["why"][:44]))

        class NotCompany(object):
            scope = SCOPE.PROJECT

        bad = ask(question, store=NotCompany())
        print("  %-18s %s" % (bad["refused"], bad["why"][:44]))

        print("\nwhat this agent does not judge")
        for line in answer.get("unjudged", []):
            print("  - %s" % line)
    finally:
        if made:
            import shutil
            os.environ.pop("HERON_KNOWLEDGE", None)
            shutil.rmtree(made, ignore_errors=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
