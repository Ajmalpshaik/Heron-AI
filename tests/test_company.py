# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-CMP-002, HERON-STD-BIM-001, HERON-STD-MOD-005, HERON-STD-QAQ-006, HERON-STD-LOD-007, HERON-STD-DOC-008
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The company standard - one scope, quoted, and four nothings kept apart.

    python tests/test_company.py

WHAT IT PROVES
  1. IT READS ONE SCOPE. A project store is refused, and the module opens
     nothing but COMPANY - checked against heron_scope's own constant.

  2. IT QUOTES. The clause that comes back is the document's own text,
     character for character, including the EXCEPT that makes it right.

  3. THE FOUR NOTHINGS STAY FOUR. An empty scope, an unindexed one and a
     genuine miss give three different routes and three different notes.

  4. A CITATION RESOLVES TO SOMETHING A PERSON CAN OPEN - document,
     locator and path, from HERON-RAG-RNK-006's own candidate.

  5. IT PICKS NO CLAUSE. Both come back, and the question goes with them.

  6. A PROJECT DOCUMENT SAYING SOMETHING ELSE IS INVISIBLE HERE - proved
     by loading one and showing the answer does not change.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.

  8. SIX ROWS, ONE FILE, AND NONE OF THEM COSMETIC (F31, D-76). Each
     subject searches something different, names its own agent id, and
     an unknown one is REFUSED rather than dropped. subject=None is
     proved character-for-character unchanged, because extending an
     agent must not quietly become editing it - and the header and the
     SUBJECTS table are checked against EACH OTHER, so the claim and
     the code cannot drift apart the way a claim and a contract did on
     2026-09-16.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


COMPANY_TEXT = ("# Acme Engineering BIM Standard 2026\n\n"
                "## 3 Mechanical\n\n"
                "### 3.1 Ductwork insulation\n\n"
                "Ducts shall be insulated to 30mm, EXCEPT where installed "
                "within conditioned spaces.\n\n"
                "### 3.2 Duct naming\n\n"
                "Every duct type shall be named SYSTEM-SIZE-MATERIAL.\n")

PROJECT_TEXT = ("# Tower B Project Specification\n\n"
                "### 3.1 Ductwork insulation\n\n"
                "Ducts shall be insulated to 40mm throughout.\n")

QUESTION = "how thick should duct insulation be"


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_company.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    home = tempfile.mkdtemp(prefix="heron-company-test-")
    was = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        import heron_company as CMP
        import heron_scope as SCOPE
        import heron_ingest as INGEST
        import heron_search as SEARCH
        import heron_retrieve as RETRIEVE
        import heron_contract as CON

        print("\n1. it reads one scope")
        check(CMP.SCOPE_NAME is SCOPE.COMPANY,
              "CMP.SCOPE_NAME IS SCOPE.COMPANY - the same object")
        check(CMP.find_documents is RETRIEVE.find_documents,
              "and the lookup IS HERON-RAG-RNK-006's")
        opens = [line for line in logic.split("\n")
                 if "open_scope(" in line]
        check(len(opens) == 1 and "SCOPE_NAME" in opens[0],
              "the module opens exactly one scope, by that constant: %s"
              % (opens[0].strip() if opens else "none"))

        class NotCompany(object):
            scope = SCOPE.PROJECT

        said = CMP.ask(QUESTION, store=NotCompany())
        reached.add(said.get("refused"))
        check(said.get("refused") == "WRONG_SCOPE",
              "a project store is refused")
        check("PRJ-009" in said["why"],
              "and the reason names the agent that exists for overrides")

        print("\n3. the four nothings stay four")
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.COMPANY)
        blank = CMP.ask(QUESTION, store=store)
        check(blank["answered"] is False and blank["route"] == "empty",
              "nothing ingested -> route %r" % blank["route"])

        paper = os.path.join(home, "Acme BIM Standard 2026.md")
        with io.open(paper, "w", encoding="utf-8") as handle:
            handle.write(COMPANY_TEXT)
        INGEST.ingest(store, paper, added_by="test", source_trust="company")
        unindexed = CMP.ask(QUESTION, store=store)
        check(unindexed["route"] == "unindexed",
              "ingested and not indexed -> route %r" % unindexed["route"])
        check(unindexed["why"] != blank["why"],
              "and it is a different sentence from the empty one")

        SEARCH.index_chunks(store)
        miss = CMP.ask("what colour is the site hoarding", store=store)
        check(miss["route"] == "nothing",
              "a genuine miss -> route %r" % miss["route"])
        routes = set([blank["route"], unindexed["route"], miss["route"]])
        check(len(routes) == 3, "three different routes: %s"
                                % ", ".join(sorted(routes)))
        notes = set([blank["why"], unindexed["why"], miss["why"]])
        check(len(notes) == 3, "and three different sentences")

        print("\n2. it quotes")
        answer = CMP.ask(QUESTION, store=store)
        check(answer["answered"] is True, "the standard answers")
        quoted = " ".join(" ".join(c["text"].split())
                          for c in answer["clauses"])
        check("insulated to 30mm" in quoted,
              "the number is the company's own: %r"
              % quoted[:46])
        check("EXCEPT where installed within conditioned spaces" in quoted,
              "AND THE EXCEPTION CAME WITH IT - a rule quoted without its "
              "exception is a different rule")
        for clause in answer["clauses"]:
            check(" ".join(clause["text"].split()) in
                  " ".join(COMPANY_TEXT.split()),
                  "every clause is the document's own text, unaltered")

        print("\n4. a citation resolves to something a person can open")
        first = answer["clauses"][0]
        # THE TITLE IS THE INGESTER'S, NOT A GUESS ABOUT ITS RULE. This
        # check first asserted the markdown H1 and the ingester had used
        # the file name; asking that agent what it calls the file is the
        # claim that actually matters - the citation carries ITS title.
        theirs = INGEST.document_title(
            COMPANY_TEXT, os.path.splitext(os.path.basename(paper))[0])
        check(first["document"] == theirs,
              "the document is named exactly as the ingester named it: %r"
              % first["document"])
        check(first["path"] and os.path.isfile(first["path"]),
              "and the path is a file that exists: %s" % first["path"])
        check(first["documentId"], "with a document id for matching")
        check(first["scope"] == SCOPE.COMPANY,
              "and the scope it came from")
        check(not answer["unreadable"],
              "no chunk came back without its words")

        print("\n5. it picks no clause")
        check(answer["of"] == 2, "both clauses come back (%d)" % answer["of"])
        check(len(answer["asks"]) == 1, "with one question for the host")
        check(len(answer["asks"][0]["clauses"]) == answer["of"],
              "and every clause attached to it")
        check("paraphrase" in answer["asks"][0]["which"],
              "the question says quote, never paraphrase")

        print("\n6. a project document saying something else is invisible")
        other = SCOPE.open_scope(SCOPE.PROJECT, "TOWER-B")
        spec = os.path.join(home, "Tower B Project Specification.md")
        with io.open(spec, "w", encoding="utf-8") as handle:
            handle.write(PROJECT_TEXT)
        INGEST.ingest(other, spec, added_by="test", source_trust="project")
        SEARCH.index_chunks(other)
        other.close()
        again = CMP.ask(QUESTION, store=store)
        quoted_again = " ".join(" ".join(c["text"].split())
                                for c in again["clauses"])
        check("40mm" not in quoted_again,
              "the project's 40mm does not appear")
        check(quoted_again == quoted,
              "and the answer is character for character what it was")
        check(any("PRJ-009" in line for line in again["unjudged"]),
              "while the answer SAYS another scope may disagree")
        store.close()

        print("\n8. six rows, one file, and none of them cosmetic")

        # subject=None IS THE OLD AGENT, CHARACTER FOR CHARACTER. This is
        # the whole safety of extending rather than rewriting: if this
        # ever fails, F31 stopped being an extension and became an edit.
        plain = CMP.ask(QUESTION)
        check(plain["queried"] == QUESTION,
              "no subject searches the question and nothing else")
        check(plain["agent"] == "HERON-STD-CMP-002",
              "and answers as the row this file already was")
        check(plain["short"] is None,
              "with nothing withheld from it")

        # THE HEADER AND THE TABLE ARE CHECKED AGAINST EACH OTHER, not
        # both against a list typed here. A claim that agrees only with
        # itself is what let a withdrawn agent keep its contract.
        head = io.open(os.path.join(ROOT, "brain", "heron_company.py"),
                       encoding="utf-8").read().split("\n\n", 1)[0]
        claimed = set()
        for line in head.split("\n"):
            if "Heron-Agent:" in line:
                claimed |= set(
                    a.strip() for a in
                    line.split("Heron-Agent:", 1)[1].split(",") if a.strip())
        tabled = set(one["agent"] for one in CMP.SUBJECTS.values())
        check(claimed == tabled,
              "the header claims exactly the %d rows SUBJECTS answers"
              % len(tabled))
        check(len(tabled) == 6, "which is six: %s" % ", ".join(sorted(tabled)))
        known = CON.registry_ids()
        check(all(one in known for one in tabled),
              "and every one is a real row in docs/28")

        # EACH SUBJECT SEARCHES SOMETHING DIFFERENT. If two produced the
        # same query the extension would be decoration, and WFP-015's
        # question - can an existing agent be extended - would have been
        # answered yes on a technicality.
        queries = {}
        for key in sorted(CMP.SUBJECTS):
            one = CMP.ask(QUESTION, subject=key)
            queries[key] = one["queried"]
            check(one["agent"] == CMP.SUBJECTS[key]["agent"],
                  "subject %r answers as %s" % (key, one["agent"]))
        check(len(set(queries.values())) == len(queries),
              "all %d subjects search something different" % len(queries))
        check(all(QUESTION in text for text in queries.values()),
              "and every one of them still contains the question asked")

        # LOD IS THE ONE WITH A SECOND INPUT, and the stage has to REACH
        # the query - a parameter accepted and dropped is worse than one
        # refused, because the answer looks filtered.
        staged = CMP.ask(QUESTION, subject="lod", stage="RIBA 4")
        check("RIBA 4" in staged["queried"],
              "the stage reaches what is actually searched")
        check(staged["stage"] == "RIBA 4", "and is reported back")
        check(CMP.ask(QUESTION, subject="lod")["stage"] is None,
              "while LOD without a stage says so rather than inventing one")

        # THE THREE PARTIAL ROWS SAY WHAT THEY DID NOT DO, IN THE ANSWER.
        for key in ("bim", "modelling", "documentation"):
            check(bool(CMP.ask(QUESTION, subject=key)["short"]),
                  "subject %r says in its answer what it did not do" % key)
        for key in ("company", "lod"):
            check(CMP.ask(QUESTION, subject=key)["short"] is None,
                  "subject %r withholds nothing" % key)

        # AND THE TWO NEW REFUSALS ARE REACHED, not merely declared.
        unknown = CMP.ask(QUESTION, subject="acoustics")
        reached.add(unknown.get("refused"))
        check(unknown.get("refused") == "UNKNOWN_SUBJECT",
              "a subject this agent does not answer as is refused")
        check("acoustics" in unknown["why"],
              "and the refusal names it rather than listing the six only")

        misplaced = CMP.ask(QUESTION, subject="qa", stage="RIBA 4")
        reached.add(misplaced.get("refused"))
        check(misplaced.get("refused") == "STAGE_NOT_TAKEN",
              "a stage on a row that takes none is refused, never ignored")

        print("\n7. every failure is named and reached")
        for these in ("", "   ", None):
            bad = CMP.ask(these)
            reached.add(bad.get("refused"))
            check(bad.get("refused") == "NOTHING_ASKED",
                  "%r is refused" % these)
        check("NO_STORE" in logic,
              "NO_STORE is named for the scope that cannot be opened")
        reached.add("NO_STORE")

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-STD-CMP-002.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 5, "the contract declares 5 failures")
        for failure in named:
            check(failure in logic, "the code names %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(answer["unjudged"]) == 5, "five things are left unjudged")
        print("\n9. A FLAG WITH NO VALUE IS REFUSED, NOT SEARCHED FOR")
        # ROW 5b-104, AND THIS ONE DID NOT CRASH - IT ANSWERED. The loop that
        # takes the flags is guarded with `and i + 1 < len(argv)`, so a flag
        # last on the line stopped matching and fell through to
        # `words.append` - which is word for word the failure the comment
        # above it says it prevents: "a --subject swallowed into the question
        # would search for the word --subject". Measured by running it:
        #
        #   heron_company.py "how thick is duct insulation" --subject
        #     -> searched for 'how thick is duct insulation --subject', exit 0
        #
        # An honest empty answer about a question nobody asked is worse than
        # a refusal, because it reads as a measurement.
        import io as _io
        import contextlib as _ctx

        for flag in ("--subject", "--stage"):
            said = _io.StringIO()
            try:
                with _ctx.redirect_stdout(said):
                    code = CMP.main(["how thick is duct insulation", flag])
            except BaseException as raised:      # noqa: BLE001 - that IS the check
                code = None
                check(False, "%s with no value is refused rather than "
                             "raising %s" % (flag, type(raised).__name__))
            if code is not None:
                check(code == 2,
                      "%s with no value exits 2 - nothing was searched, so it "
                      "must not come back as an answer" % flag)
                check(flag not in said.getvalue().split("searched for")[-1]
                      or "needs a value" in said.getvalue(),
                      "and the flag is NAMED in a refusal rather than joined "
                      "to the question and looked up")

        print("\n10. AND A FLAG THIS TOOL DOES NOT HAVE IS REFUSED TOO")
        # ROW 5b-112. The comment above that same loop states the rule in the
        # present tense - "THE FLAGS STOP THE QUESTION, AND AN UNKNOWN ONE IS
        # REFUSED" - and only the two flags it HAS were ever stopped.
        # Measured by running it, before the fix:
        #
        #   heron_company.py "how thick is duct insulation" --scope project
        #     -> the company standard has no answer to
        #        'how thick is duct insulation --scope project', exit 0
        #
        # Row 5b-104 closed this door for --subject and --stage and left it
        # open for every other flag, which is the half-a-finding shape this
        # register keeps recording. heron_retrieve has refused unknown flags
        # by name since 2026-08-30 and says why.
        said = _io.StringIO()
        try:
            with _ctx.redirect_stdout(said):
                code = CMP.main(["how thick is duct insulation",
                                 "--scope", "project"])
        except BaseException as raised:      # noqa: BLE001 - that IS the check
            code = None
            check(False, "an unknown flag is refused rather than raising %s"
                         % type(raised).__name__)
        if code is not None:
            spoke = said.getvalue()
            check(code == 2,
                  "an unknown flag exits 2 rather than answering, because an "
                  "honest empty answer to a question nobody asked reads as a "
                  "measurement")
            check("duct insulation --scope" not in spoke,
                  "and it never becomes part of the question")
            check("not a flag this tool has" in spoke and "--scope" in spoke,
                  "and it is refused in the house words - heron_retrieve, "
                  "heron_conflict and heron_research all say `not a flag "
                  "this tool has` and exit 2")

    finally:
        if was is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    one scope, quoted, and four nothings kept apart")
    return 0


if __name__ == "__main__":
    sys.exit(main())
