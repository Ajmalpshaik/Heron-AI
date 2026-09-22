# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-CMP-002, HERON-STD-BIM-001, HERON-STD-MOD-005, HERON-STD-QAQ-006, HERON-STD-LOD-007, HERON-STD-DOC-008
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

SIX ROWS, ONE FILE, AND A NOUN IN FRONT (F31, D-76)
-----------------------------------------------------
Five more Standards rows are this agent with a different search term.
HERON-AHR-WFP-015 - the row whose whole job is to say no before anything
is hired - asks "can an existing agent be extended?", and for these five
the answer is yes:

    subject=None             HERON-STD-CMP-002  the company standard
    subject="bim"            HERON-STD-BIM-001  a stated BIM standard
    subject="modelling"      HERON-STD-MOD-005  connections, elevations
    subject="qa"             HERON-STD-QAQ-006  the QA process
    subject="lod"            HERON-STD-LOD-007  LOD, and it takes a STAGE
    subject="documentation"  HERON-STD-DOC-008  sheets, titleblocks

The subject steers retrieval and is reported back, so an answer always
says which row it was answering as and what was actually searched. An
unknown subject is REFUSED rather than ignored: a subject silently
dropped would answer the company question and look exactly like it had
answered the asked one.

`subject=None` is the behaviour this file had before F31, unchanged.

WHAT THE EXTRA ROWS DO NOT GET, WHICH IS THE HONEST HALF
----------------------------------------------------------
Three of the five are only PARTLY this file, and each says so in its own
answer rather than only in this comment:

    BIM-001    docs/28 says "applies a standard TO A MODEL". This cites;
               it does not open a model. The checkable half is
               HERON-QA-BIM-011's
    MOD-005    connections and elevations are geometry, and nothing on
               this side of the bridge can reach it
    DOC-008    sheets, titleblocks and view names are NAMES, and
               HERON-QA-BIM-011 already routes those to HERON-STD-NAM-004

LOD-007 is the one with a genuine difference of shape rather than of
subject - "expected at this STAGE" is a second input no other standards
row takes - and it is the only one given a parameter of its own.

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

# THE SIX ROWS THIS FILE ANSWERS, AND THE TERMS THAT TELL THEM APART.
# Recorded here rather than in five near-identical files, which is what
# HERON-AHR-WFP-015 exists to prevent - see F31 and D-76. `company` has
# no terms on purpose: subject=None must query exactly what it queried
# before F31, or extending this agent would have changed it.
SUBJECTS = {
    "company": {
        "subject": "company",
        "agent": "HERON-STD-CMP-002",
        "terms": (),
        "takesStage": False,
        "row": "the organisation's own approved standard",
        "short": None,
    },
    "bim": {
        "subject": "bim",
        "agent": "HERON-STD-BIM-001",
        "terms": ("BIM", "standard"),
        "takesStage": False,
        "row": "applies a stated BIM standard to a model",
        "short": "THIS CITED; IT DID NOT CHECK A MODEL. docs/28 gives this "
                 "row a model to apply the standard to, and nothing here "
                 "opened one. The checkable half is HERON-QA-BIM-011's.",
    },
    "modelling": {
        "subject": "modelling",
        "agent": "HERON-STD-MOD-005",
        "terms": ("modelling", "connection", "elevation", "practice"),
        "takesStage": False,
        "row": "how things should be modelled - connections, elevations",
        "short": "THIS CITED; IT DID NOT LOOK AT GEOMETRY. Connections and "
                 "elevations are geometry inside Revit, which nothing on "
                 "this side of the bridge can reach.",
    },
    "qa": {
        "subject": "qa",
        "agent": "HERON-STD-QAQ-006",
        "terms": ("QA", "QC", "quality", "process", "review", "checking"),
        "takesStage": False,
        "row": "the organisation's QA process requirements",
        "short": "THIS ANSWERED WHAT THE PROCESS REQUIRES, NOT WHETHER IT "
                 "WAS FOLLOWED. The row says requirements; compliance "
                 "would be a different job, and the audit trail's.",
    },
    "lod": {
        "subject": "lod",
        "agent": "HERON-STD-LOD-007",
        "terms": ("LOD", "level of development", "level of detail"),
        "takesStage": True,
        "row": "level of development expected at this stage",
        "short": None,
    },
    "documentation": {
        "subject": "documentation",
        "agent": "HERON-STD-DOC-008",
        "terms": ("sheet", "titleblock", "annotation", "documentation"),
        "takesStage": False,
        "row": "sheet, titleblock and annotation requirements",
        "short": "SHEET, TITLEBLOCK AND VIEW NAMES ARE NAMES, and "
                 "HERON-QA-BIM-011 already routes those to "
                 "HERON-STD-NAM-004. What is left here is the clauses.",
    },
}


def resolve(subject, stage=None):
    """(the subject's entry, None) - or (None, a refusal to hand back).

    AN UNKNOWN SUBJECT IS REFUSED, NEVER SHRUGGED OFF. A subject quietly
    dropped answers the COMPANY question and hands back something that
    looks exactly like an answer to the asked one - the same defect as a
    flag swallowed into a list of file names and reported as data.
    """
    key = subject if subject is None else str(subject).strip().lower()
    if key in (None, ""):
        key = "company"
    if key not in SUBJECTS:
        return None, {
            "answered": False,
            "refused": "UNKNOWN_SUBJECT",
            "why": "%r is not a subject this agent answers as. The six are "
                   "%s. Answering the company question under another row's "
                   "name would be a false sentence about which standard "
                   "was read." % (subject, ", ".join(sorted(SUBJECTS))),
        }
    picked = SUBJECTS[key]
    if stage and not picked["takesStage"]:
        return None, {
            "answered": False,
            "refused": "STAGE_NOT_TAKEN",
            "why": "subject %r takes no stage, and %r was handed in. Only "
                   "%s does - docs/28 gives it 'expected at this stage', "
                   "and that second input is the one genuine difference "
                   "among these rows. Accepting a stage here and ignoring "
                   "it would report a filtered answer that was never "
                   "filtered." % (picked["subject"], stage,
                                  SUBJECTS["lod"]["agent"]),
        }
    return picked, None


def _queried(question, picked, stage):
    """What is actually searched: the question, steered by the subject.

    With no subject this is the question and nothing else, character for
    character, which is what keeps HERON-STD-CMP-002 the agent it was.
    """
    words = [question] + list(picked["terms"])
    if picked["takesStage"] and stage:
        words.append(str(stage))
    return " ".join(w for w in words if w)


def ask(question, store=None, subject=None, stage=None):
    """
    {answered, clauses, ask} - or a refusal. Nothing is summarised and
    no clause is converted.

    `subject` picks which of the six rows this call answers as and steers
    retrieval with that row's terms. None is HERON-STD-CMP-002 and
    behaves exactly as this file did before F31. Every answer carries the
    row it answered as and the text actually searched, so a caller is
    never left guessing which standard it just read.
    """
    picked, refusal = resolve(subject, stage)
    if refusal is not None:
        return refusal

    question = str(question or "").strip()
    badge = {
        "subject": picked["subject"],
        "agent": picked["agent"],
        "asAsked": picked["row"],
        "stage": stage if picked["takesStage"] else None,
        "queried": _queried(question, picked, stage),
        "short": picked["short"],
    }

    if not question:
        return dict(badge, answered=False, refused="NOTHING_ASKED",
                    why="no question was handed in. A standard answers a "
                        "question; it is not a thing to be listed.")

    answer = _cite(question, badge["queried"], store)
    # THE BADGE IS APPLIED LAST AND OVERWRITES NOTHING IT SHOULD NOT.
    # _cite knows only about citing; which row asked is this layer's, and
    # keeping the two apart is why subject=None is provably unchanged.
    answer.update(badge)
    return answer


def _cite(question, queried, store):
    """The citing itself, with the subject already resolved.

    `question` is what the caller asked and what every message quotes;
    `queried` is what was actually searched, which the subject may have
    steered. Reporting the first and searching the second is why both
    are carried rather than one overwritten by the other.
    """
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
        answer = find_documents(store, queried)
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

    # THE FLAGS STOP THE QUESTION, AND AN UNKNOWN ONE IS REFUSED. A
    # `--subject` swallowed into the question would search for the word
    # "--subject" and report the miss as an honest empty answer.
    subject = stage = None
    words, i = [], 0
    while i < len(argv):
        if argv[i] in ("--subject", "--stage"):
            # A FLAG WITH NO VALUE FELL THROUGH INTO THE QUESTION, which is
            # the exact failure the comment above says this loop prevents.
            # The guard was `and i + 1 < len(argv)`, so `--subject` last on
            # the line stopped matching and reached `words.append` - measured,
            # `heron_company.py "how thick is duct insulation" --subject`
            # searched for `'how thick is duct insulation --subject'` and
            # exited 0. A guard that cannot take the value has to REFUSE it,
            # not hand it to the search. Row 5b-104.
            if i + 1 >= len(argv) or argv[i + 1].startswith("--"):
                print("\n  %s needs a value and was given none. Nothing was "
                      "searched - a flag with no value is a typo, and "
                      "searching for the flag itself would report the miss "
                      "as an honest empty answer." % argv[i])
                return 2
            if argv[i] == "--subject":
                subject, i = argv[i + 1], i + 2
            else:
                stage, i = argv[i + 1], i + 2
            continue
        words.append(argv[i])
        i += 1

    # AND A FLAG THIS TOOL DOES NOT HAVE IS REFUSED BY NAME, which is what the
    # comment above this loop has claimed all along. Only --subject and
    # --stage were ever stopped, so `--scope project` went straight into the
    # question and the answer read "the company standard has no answer to
    # 'how thick is duct insulation --scope project'" at exit 0 - the miss
    # reported as a measurement, which is the one thing that comment exists to
    # prevent. Row 5b-104 closed this door for the two flags it has and left
    # it open for every other. heron_retrieve has refused unknown flags by
    # name since 2026-08-30, in these words. Row 5b-112.
    unknown = [word for word in words if word.startswith("-")]
    if unknown:
        print("\n  not a flag this tool has: %s" % " ".join(unknown))
        print('  python brain/heron_company.py "how thick is duct insulation"')
        print("  the flags are --subject and --stage, and each takes a value")
        return 2

    picked, refusal = resolve(subject, stage)
    if refusal is not None:
        print("\n%s  %s" % (refusal["refused"], refusal["why"]))
        return 2
    print("row:    %s, %s" % (picked["agent"], picked["row"]))

    question = " ".join(words) or "how thick should duct insulation be"

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
        answer = ask(question, subject=subject, stage=stage)
        if answer.get("queried") != question:
            print("\nsearched: %s" % answer["queried"])
        if answer.get("short"):
            print("\nnot this agent's: %s" % answer["short"])
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
        for kwargs in ({"subject": "acoustics"},
                       {"subject": "qa", "stage": "RIBA 4"}):
            bad = ask(question, **kwargs)
            print("  %-18s %s" % (bad["refused"], bad["why"][:44]))

        print("\nsix rows, one file")
        for key in sorted(SUBJECTS):
            one = SUBJECTS[key]
            print("  %-14s %s  %s" % (key, one["agent"], one["row"][:38]))

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
