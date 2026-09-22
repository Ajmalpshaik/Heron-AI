# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-ISO-003
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
External standards - relevance is not authorship.

    python tests/test_iso.py

WHAT IT PROVES
  1. NO SOURCE, NO CLAIM. With nothing indexed at all, `claimed` is
     false and not one word about the standard comes back.

  2. RELEVANCE IS NOT AUTHORSHIP. A company method statement about the
     common data environment matches a question about ISO 19650's CDE
     and is reported as NOT the standard - `claimed` stays false.

  3. THE TITLE DECIDES, NOT THE TEXT. A document that MENTIONS ISO 19650
     in its body is still not the standard; one whose title carries the
     name is.

  4. WHEN IT DOES ANSWER, IT QUOTES - character for character, and every
     clause is marked not redistributable with a reason.

  5. THE STANDARD IS REQUIRED, NEVER GUESSED.

  6. SCOPES ARE ASKED SEPARATELY, and each reports its own route.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
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


PLAN = ("# Acme Information Management Plan\n\n"
        "## 2 Common data environment\n\n"
        "The project common data environment shall be hosted on Acme's own "
        "server, following ISO 19650 throughout.\n")

STANDARD = ("# ISO 19650-2 Information management\n\n"
            "## 5 Information management process\n\n"
            "The common data environment shall provide a single source of "
            "information for the project.\n")

QUESTION = "what does the standard say about the common data environment"


def write(where, name, text):
    path = os.path.join(where, name)
    with io.open(path, "w", encoding="utf-8") as handle:
        handle.write(text)
    return path


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_iso.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    home = tempfile.mkdtemp(prefix="heron-iso-test-")
    was = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        import heron_iso as ISO
        import heron_scope as SCOPE
        import heron_ingest as INGEST
        import heron_search as SEARCH
        import heron_retrieve as RETRIEVE
        import heron_contract as CON

        SCOPE.rebuild()
        only = [SCOPE.COMPANY]

        print("\n1. no source, no claim")
        check(ISO.librarian is RETRIEVE.librarian,
              "ISO.librarian IS RETRIEVE.librarian - one lookup")
        blank = ISO.cite(QUESTION, standard="ISO 19650", scopes=only)
        check(blank["claimed"] is False, "with nothing indexed, claimed is "
                                         "false")
        check(blank["clauses"] == [], "and no clause came back")
        check("HAS NO COPY" in blank["why"],
              "the answer says Heron has no copy: %r" % blank["why"][:44])
        check("19650" not in " ".join(
            str(one["which"]) for one in blank["asks"]).replace(
                "IS ISO 19650", "").replace("ISO 19650,", ""),
              "and nothing in the question to the host states what the "
              "standard says")

        print("\n2. relevance is not authorship")
        store = SCOPE.open_scope(SCOPE.COMPANY)
        INGEST.ingest(store, write(home, "Acme Information Management Plan.md",
                                   PLAN),
                      added_by="test", source_trust="company")
        SEARCH.index_chunks(store)
        store.close()
        near = ISO.cite(QUESTION, standard="ISO 19650", scopes=only)
        check(near["claimed"] is False,
              "a matching company plan does NOT make a claim")
        check(len(near["elsewhere"]) >= 1,
              "it comes back in `elsewhere` (%d)" % len(near["elsewhere"]))
        check(near["clauses"] == [], "and never in `clauses`")
        check("not the standard speaking" in near["elsewhere"][0]["why"],
              "with a reason a reader can act on")

        print("\n3. the title decides, not the text")
        body = " ".join(near["elsewhere"][0]["text"].split())
        check("ISO 19650" in body,
              "the plan's own text MENTIONS ISO 19650: %r" % body[-40:])
        check(near["elsewhere"][0]["isTheStandard"] is False,
              "and it is still not the standard")
        check(ISO.names_the_standard("ISO 19650-2 Information management",
                                     "ISO 19650") is True,
              "a title carrying the name IS the standard")
        check(ISO.names_the_standard("ISO19650-2", "ISO 19650") is True,
              "and the spaceless spelling matches too")
        check(ISO.names_the_standard("Acme Information Management Plan",
                                     "ISO 19650") is False,
              "a title without it is not")

        print("\n4. when it does answer, it quotes")
        store = SCOPE.open_scope(SCOPE.COMPANY)
        INGEST.ingest(store,
                      write(home, "ISO 19650-2 Information management.md",
                            STANDARD),
                      added_by="test", source_trust="company")
        SEARCH.index_chunks(store)
        store.close()
        answer = ISO.cite(QUESTION, standard="ISO 19650", scopes=only)
        check(answer["claimed"] is True, "now the standard is indexed")
        check(answer["of"] >= 1, "%d clause(s) of it came back" % answer["of"])
        quoted = " ".join(" ".join(c["text"].split())
                          for c in answer["clauses"])
        check("single source of information" in quoted,
              "in the standard's own words: %r" % quoted[:48])
        check(" ".join(quoted.split()) in " ".join(STANDARD.split()),
              "character for character out of the document")
        for clause in answer["clauses"]:
            check(clause["redistributable"] is False,
                  "every clause is marked not redistributable")
            check("Apache 2.0" in clause["why"],
                  "and says why - Heron ships under a different licence")
        check(answer["elsewhere"],
              "while the company plan is still reported, separately")

        print("\n5. the standard is required, never guessed")
        for std in (None, "", "   "):
            said = ISO.cite(QUESTION, standard=std, scopes=only)
            reached.add(said.get("refused"))
            check(said.get("refused") == "NO_STANDARD_NAMED",
                  "standard=%r is refused" % std)
        check("D-33" in said["why"], "and the reason names D-33")

        print("\n6. scopes are asked separately")
        both = ISO.cite(QUESTION, standard="ISO 19650",
                        scopes=[SCOPE.COMPANY, SCOPE.GLOBAL, SCOPE.PROJECT])
        labels = [card["scope"] for card in both["scopes"]]
        check(labels == [SCOPE.COMPANY, SCOPE.GLOBAL, SCOPE.PROJECT],
              "all three are reported, in order: %s" % ", ".join(labels))
        routes = dict((card["scope"], card["route"] or card["skipped"])
                      for card in both["scopes"])
        check(routes[SCOPE.COMPANY] == "documents",
              "company found documents")
        check(routes[SCOPE.GLOBAL] == "empty",
              "global is empty, and says so rather than being folded in")
        check("no project is identified" in str(routes[SCOPE.PROJECT]),
              "and the project scope is SKIPPED, not guessed at")

        print("\n7. every failure is named and reached")
        for q in (None, "", "   "):
            bad = ISO.cite(q, standard="ISO 19650", scopes=only)
            reached.add(bad.get("refused"))
            check(bad.get("refused") == "NOTHING_ASKED", "%r is refused" % q)

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-STD-ISO-003.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 2, "the contract declares 2 failures")
        for failure in named:
            check(failure in logic, "the code names %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(answer["unjudged"]) == 6, "six things are left unjudged")

        print("\n8. a read that failed is not a clause that is not there")
        # ROW 5b-114, and it is one of the three brain/mcp/tools sites row
        # 5b-110 asked to be READ before that gate is widened. The clause
        # text is fetched in a second pass, and that read was wrapped in a
        # bare `except Exception: row = None` followed by
        # `if row is None or not row["text"]: continue` - so a clause the
        # store could not read was SILENTLY DROPPED. Measured on a store
        # holding a real ISO 19650 document: healthy, 3 clauses; with only
        # that one query raising `database is locked`, 0 clauses and
        #
        #   HERON HAS NO COPY OF ISO 19650 indexed in company.
        #
        # A confident, specific, FALSE sentence - D-52's plausible zero - in
        # the module whose headline rule is "NO SOURCE, NO CLAIM ... there is
        # no path through this agent that writes a sentence about a standard".
        import sqlite3 as _sqlite3

        TEXT_QUERY = "SELECT text FROM chunks WHERE id = ?"
        real_open = ISO.SCOPE.open_scope

        class Locked(object):
            """The same store, with ONLY the clause-text read failing.

            The shortlist is therefore identical - this separates "could not
            read the words" from "found nothing", which is the whole point.
            """

            def __init__(self, inner):
                self._inner = inner

            def execute(self, sql, *args, **kwargs):
                if str(sql).strip() == TEXT_QUERY:
                    raise _sqlite3.OperationalError("database is locked")
                return self._inner.execute(sql, *args, **kwargs)

            def __getattr__(self, name):
                return getattr(self._inner, name)

        healthy = ISO.cite(QUESTION, standard="ISO 19650", scopes=only)
        check(healthy["claimed"] is True and healthy["of"] > 0,
              "the same store answers with %d clause(s) while it can be read"
              % healthy["of"])

        ISO.SCOPE.open_scope = (lambda scope, project=None:
                                Locked(real_open(scope, project)))
        locked = None
        try:
            locked = ISO.cite(QUESTION, standard="ISO 19650", scopes=only)
        except BaseException as raised:  # noqa: BLE001 - that IS the check
            check(False, "a failed clause read is reported rather than "
                         "raising %s" % type(raised).__name__)
        finally:
            ISO.SCOPE.open_scope = real_open

        if locked is not None:
            check("HAS NO COPY" not in locked["why"],
                  "it does not say Heron has no copy of a standard it HAS: "
                  "%r" % locked["why"][:56])
            check(bool(locked.get("unreadable")),
                  "the clauses it could not read are carried, not dropped")
            check("could not be read" in locked["why"].lower(),
                  "and the sentence a reader sees says a read failed")

        print("\n9. the command line: a typo is never the answer")
        # ROW 5b-115, and it is rows 5b-104 and 5b-112 in a FIFTH site.
        # Measured by running it:
        #
        #   heron_iso.py --standard          -> IndexError, exit 1
        #   heron_iso.py "..." --standard "ISO 19650" --top 5
        #                                    -> --top 5 went into the
        #                                       question, exit 0
        #
        # 5b-104's scan missed this one because it reads `argv[at + 1]` and
        # the grep was for `argv[i + 1]` - row 5b-95's lesson, that a scan
        # over source text is not a measurement.
        import contextlib as _ctx

        for argv, what in ((["--standard"], "a flag with no value"),
                           (["what does it call the CDE", "--standard",
                             "ISO 19650", "--top", "5"], "an unknown flag")):
            said = io.StringIO()
            code = None
            try:
                with _ctx.redirect_stdout(said):
                    code = ISO.main(list(argv))
            except BaseException as raised:  # noqa: BLE001 - that IS the check
                check(False, "%s is refused rather than raising %s"
                             % (what, type(raised).__name__))
            if code is not None:
                spoke = said.getvalue()
                check(code == 2, "%s exits 2" % what)
                check("--standard" in spoke or "--top" in spoke,
                      "and the flag is NAMED in the refusal")
                # NOT "HAS NO COPY", which was green for the wrong reason:
                # this suite's store HAS a standard in it by now, so the
                # unknown-flag run answered with clauses instead. The line
                # only the ANSWER path prints is what separates the two.
                check("scopes asked, separately" not in spoke,
                      "nothing was searched at all - no answer was printed "
                      "for %s" % what)
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
    print("PASS    relevance is not authorship")
    return 0


if __name__ == "__main__":
    sys.exit(main())
