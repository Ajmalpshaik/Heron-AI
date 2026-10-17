#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The score of the owner's own questions, and a drop planted to prove it is seen.

    python tests/test_score_routing.py

`tools/score-routing.py` asks the 79 questions in tests/data/owner-questions.yaml
through `heron_brain.lookup` - the function `heron_lookup` calls - and scores
where the owner's confirmed answer came back. A score is only worth having if
a FALL in it is noticed, so the claim this suite exists for is that one:

    PLANT A DROP AND THE SCORER CATCHES IT.

It is planted twice. Once in the arithmetic, where a question that was first
is made to vanish and a tool or gap row is made to land on a change. And once
for real, through the real search: a private knowledge store is built, two of
his questions are asked, and then the capability that answered each is
RETIRED in that private store - DEPRECATED, which the search never offers -
and asked again. The scorer has to see both fall, and say so.

THE FIXTURE KEY IN SECTION 5 IS NOT HIS KEY, AND THAT IS DELIBERATE. Its
answers are whatever the search put first on the first pass, read at test
time - so the drop is certain whatever the library looks like the day this
runs, and nothing here asserts today's ranking, which is a sample (row 116).
HIS answer key is never derived from the search: section 1 checks it only
against the library, and pins the wording of his questions.

WHAT IT DOES NOT DO: it never runs the full 79 - that takes minutes and is the
tool's job. Whether a drop stops a pull request is not this suite's call
either: the owner decided it never does (D-100), and section 4 holds the
tool to that.

Section 7, added 2026-10-09: the store the score is taken over is checked by
check-routing's store_for_this_tree() - a renamed id and an edited row are
rebuilt, and the SHARED store is never rebuilt from a worktree or from the
main folder on a feature branch (rows 5b-229 and 5b-233). It stops each run
at the first step after the guard, so it asks no question.

Section 8, added 2026-10-17: the 120 plain questions frozen out of
FRAGMENT-ISSUES rows 5b-477 and 5b-478 into tests/data/plain-questions.yaml.
They are checked against the rows' own text, pinned, held to the library, and
a drop is planted in their arithmetic - a question first answered by a read
and then by a change has to be named as having fallen.

Section 9, added 2026-10-17: a session's own sentence set (rows 5b-481,
5b-485, 5b-510) - its shape, asked on a tiny fixture checkout built in a
scratch folder (this tree's search code, four cards, a FAKE cross-encoder that
counts shared words, and no trained encoder, so it answers alike on any
machine and fetches nothing), two backends in two processes, a record refused
when its answers ran on another backend, every changed answer tagged against
a second fixture tree with one card gone, and candidate identities re-scored
offline on the shortlists the fake re-ranker read.

Exit 0 passed, 1 failed, 3 could not run for want of PyYAML.
"""

import contextlib
import hashlib
import importlib.util
import io
import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "score-routing.py")
KEY = os.path.join(ROOT, "tests", "data", "owner-questions.yaml")
PLAIN_KEY = os.path.join(ROOT, "tests", "data", "plain-questions.yaml")
PLAIN_ROWS = os.path.join(ROOT, "docs", "fragment-issues",
                          "section-5b-rows-401-425.md")
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

# THE OWNER'S WORDS, PINNED. sha256 of the 79 questions joined by newlines,
# whitespace collapsed. A question is never reworded to suit the search
# (FRAGMENT-ISSUES row 113, D-34); if one must change, it is on his word,
# recorded in the key's `corrections`, and this pin moves in the same change.
QUESTIONS_PIN = "ba1e6f01ddecbc9d78d767cd116f1ae1a39d9826cd1818b6b168fd9493733bf6"

# THE 120 PLAIN QUESTIONS, PINNED THE SAME WAY. They are a session's words,
# frozen out of rows 5b-477 and 5b-478; section 8 also checks them against
# the rows' own text, so this pin moves only with the register.
PLAIN_PIN = "707ee54960688742d7104f035215c5f2f0a021ef0c927329f098a270a5abab49"

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("score_routing", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                                 # noqa: BLE001
        return None


def finish():
    print()
    if FAILURES:
        print("FAILED - %d:" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASSED")
    return 0


def result(tool, n, kind, place=None, got="X", risk="READ", asked=False,
           route="hybrid", error=None):
    """One scored row, shaped the way `score_one` shapes it."""
    return {"n": n, "kind": kind, "answer": "A%d" % n, "question": "q%d" % n,
            "route": route, "got": got, "got_risk": risk, "order": [],
            "place": place, "reached": None,
            "writes": risk in ("MODIFY", "PUBLISH", "ADMIN"),
            "expected_risk": None if kind == "gap" else "READ",
            "asked_change": None if kind == "gap" else asked, "error": error}


# ---------------------------------------------------------------------------
# Section 7: the store it scores over - check-routing's guard (rows 5b-229
# and 5b-233)
# ---------------------------------------------------------------------------

# What the first step after the store guard raises in section 7, so a run the
# guard lets through stops there instead of asking 79 questions.
MEASURED = "went on to score"


class _Measured(Exception):
    """The store guard let the run through."""


def _stop(*_args, **_kwargs):
    raise _Measured()


def _set_env(values):
    """Set each variable in VALUES - None unsets it. Returns what was there."""
    kept = dict((k, os.environ.get(k)) for k in values)
    for key, value in values.items():
        if value is None:
            os.environ.pop(key, None)
        else:
            os.environ[key] = value
    return kept


def _store_rows(home):
    """{id: row} the GLOBAL store in HOME holds, read without changing it."""
    import heron_scope as SCOPE
    kept = _set_env({"HERON_KNOWLEDGE": home})
    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            return dict((row["id"], row) for row in store.fragments())
        finally:
            store.close()
    finally:
        _set_env(kept)


def _edit_store(home, sql, args):
    """One statement against the GLOBAL store in HOME - a stale store, made."""
    import heron_scope as SCOPE
    kept = _set_env({"HERON_KNOWLEDGE": home})
    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            store.db.execute(sql, args)
            store.db.commit()
        finally:
            store.close()
    finally:
        _set_env(kept)


def _built_store():
    """A private scratch folder holding a store rebuilt from this tree."""
    import heron_scope as SCOPE
    home = tempfile.mkdtemp(prefix="heron-score-store-")
    kept = _set_env({"HERON_KNOWLEDGE": home})
    try:
        SCOPE.rebuild()
    finally:
        _set_env(kept)
    return home


def _guarded(tool, env, source=None, on_main=None):
    """(exit code, what it said) from the tool's main([]) in environment ENV.

    The first step after the store guard - reading the skill files - raises
    instead of running, so the code is MEASURED when the guard let the run
    through. SOURCE plants what heron_scope.refreshes_from() answers, ON_MAIN
    what heron_scope.on_main_branch() answers: the main folder's HEAD, which
    a test cannot check a branch out in. A raise of any other kind is
    recorded as the code, never let out (heron-ship s2a).
    """
    import heron_scope as SCOPE
    import heron_search as SEARCH
    was = (SEARCH.index, SCOPE.refreshes_from,
           getattr(SCOPE, "on_main_branch", None), tool.load_skills)
    SEARCH.index = _stop
    tool.load_skills = _stop
    if source is not None:
        SCOPE.refreshes_from = lambda root=None: source
    if on_main is not None:
        SCOPE.on_main_branch = lambda root=None: on_main
    kept = _set_env(env)
    said = io.StringIO()
    try:
        with contextlib.redirect_stdout(said), contextlib.redirect_stderr(said):
            code = tool.main([])
    except _Measured:
        code = MEASURED
    except BaseException as raised:                      # noqa: BLE001
        code = "raised %s: %s" % (type(raised).__name__, raised)
    finally:
        _set_env(kept)
        SEARCH.index, SCOPE.refreshes_from = was[0], was[1]
        tool.load_skills = was[3]
        if on_main is not None:
            if was[2] is None:
                del SCOPE.on_main_branch
            else:
                SCOPE.on_main_branch = was[2]
    return code, said.getvalue()


def store_guard(tool):
    """Section 7. Until 2026-10-09 this tool kept its own copy of the store
    check: the ids only, no row's content, and a rebuild of the ONE store
    every chat on the PC reads from whatever checkout it ran in. It calls
    check-routing's store_for_this_tree() now, and these are the four
    stores that guard must catch."""
    import heron_fragment as FRAG

    home = _built_store()
    try:
        some = sorted(_store_rows(home))[0]
        stranger = "FRG-NOT-ON-DISK-000"
        _edit_store(home, "UPDATE fragments SET id = ? WHERE id = ?",
                    (stranger, some))
        code, said = _guarded(tool, {"HERON_KNOWLEDGE": home})
        now = _store_rows(home)
        check(code == MEASURED and some in now and stranger not in now,
              "a store with one id renamed - as many rows as cards, not "
              "THESE cards - is rebuilt before anything is scored (got %r)"
              % (code,))
        check(stranger in said, "and the run names the id that did not match")
        wrong = "an identity the card no longer says"
        _edit_store(home, "UPDATE fragments SET semantic_identity = ? "
                          "WHERE id = ?", (wrong, some))
        code, said = _guarded(tool, {"HERON_KNOWLEDGE": home})
        now = _store_rows(home).get(some) or {}
        check(code == MEASURED
              and now.get("semantic_identity") not in (None, wrong),
              "a store with one row's identity edited is rebuilt - every "
              "row's content is compared, not only the ids (got %r)" % (code,))
        check("edited since" in said and some in said,
              "and the run names the card")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    home = tempfile.mkdtemp(prefix="heron-score-shared-")
    try:
        code, said = _guarded(
            tool, {"HERON_KNOWLEDGE": home},
            source=(os.path.join("main", "brain", "fragments"), "main"))
        held = len(_store_rows(home))
        check(code == 2 and held == 0,
              "the SHARED store, asked from a checkout that is not main, is "
              "exit 2 with nothing written (got %r, %d rows)" % (code, held))
        check("SHARED" in said and "HERON_KNOWLEDGE" in said,
              "and it says to point HERON_KNOWLEDGE at a scratch folder")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    top = tempfile.mkdtemp(prefix="heron-score-mainfolder-")
    try:
        appdata = os.path.join(top, "appdata")
        shared = os.path.join(appdata, "Heron", "knowledge")
        os.makedirs(shared)
        code, said = _guarded(tool, {"HERON_KNOWLEDGE": None,
                                     "APPDATA": appdata},
                              source=(FRAG.FRAGMENTS_DIR, None), on_main=False)
        held = len(_store_rows(shared))
        check(code == 2 and held == 0,
              "and from the MAIN folder with a feature branch checked out, "
              "the same: exit 2, nothing written (got %r, %d rows)"
              % (code, held))
        check("a branch that is not main" in " ".join(said.split()),
              "and it says the branch is why")
    finally:
        shutil.rmtree(top, ignore_errors=True)


def _frozen_from_rows():
    """{477: [...], 478: [...]} - each row's "The 60, frozen:" list, split the
    way the yaml was made from it, so the file is checked against the
    register's own words rather than against a second copy of them."""
    import re
    out = {}
    with io.open(PLAIN_ROWS, encoding="utf-8") as handle:
        lines = handle.read().splitlines()
    for n in (477, 478):
        line = [x for x in lines if x.startswith("| **%d** |" % n)]
        found = re.search(r"\*\*The 60, frozen:\*\* (.*?)\. \|",
                          line[0]) if line else None
        out[n] = ([q.strip().strip("*").strip()
                   for q in found.group(1).split(" · ")] if found else [])
    return out


def plain_questions(tool, capabilities):
    """Section 8. Every name is asked for before it is called, so a tool that
    has none of it fails here once per claim rather than raising once."""
    load_plain = getattr(tool, "load_plain", None)
    check(callable(load_plain), "the tool reads the plain questions at all")
    if not callable(load_plain):
        return
    doc, rows, problems = load_plain(PLAIN_KEY)
    check(not problems, "the plain set is well formed%s"
          % ("" if not problems else ": " + "; ".join(problems[:3])))
    check(len(rows) == 120 and [r["n"] for r in rows] == list(range(1, 121)),
          "120 questions, numbered 1 to 120 in order")
    blob = "\n".join(" ".join(r["question"].split()) for r in rows)
    check(hashlib.sha256(blob.encode("utf-8")).hexdigest() == PLAIN_PIN,
          "the questions are pinned - a reworded one fails here")
    frozen = _frozen_from_rows()
    check([r["question"] for r in rows if r.get("row") == 477] == frozen[477]
          and len(frozen[477]) == 60,
          "n 1-60 are row 5b-477's sixty, word for word and in its order")
    check([r["question"] for r in rows if r.get("row") == 478] == frozen[478]
          and len(frozen[478]) == 60,
          "n 61-120 are row 5b-478's sixty, word for word and in its order")
    missing = tool.unknown_plain(rows, capabilities)
    check(not missing, "every named read is a capability Heron has%s"
          % ("" if not missing else ": " + "; ".join(
              "#%s %s" % m for m in missing[:3])))
    check(all(not r.get("answer") for r in rows
              if r.get("named") == "no-read"),
          "a question no read answers names none - the rows forbid declaring "
          "it on one that does not")

    work = tempfile.mkdtemp(prefix="heron-plain-key-")
    try:
        bad = os.path.join(work, "plain.yaml")
        with io.open(bad, "w", encoding="utf-8") as handle:
            handle.write("questions:\n"
                         "  - n: 1\n    question: ask\n    named: no-read\n"
                         "    answer: [SOMETHING]\n"
                         "  - n: 2\n    question: ask two\n"
                         "    named: owners-call\n"
                         "  - n: 4\n    question: ask three\n    named: guess\n")
        _d, _r, shape = load_plain(bad)
        check(len(shape) == 4, "a no-read row naming a read, an owner's call "
              "naming none, a number out of order and an unknown `named` are "
              "all four named: %s" % shape)
        quiet = io.StringIO()
        with contextlib.redirect_stderr(quiet), contextlib.redirect_stdout(quiet):
            code = tool.main(["--plain-questions", bad, "--list-plain"])
        check(code == 2, "and a malformed plain set is exit 2, not a score")
    finally:
        shutil.rmtree(work, ignore_errors=True)

    listed = io.StringIO()
    with contextlib.redirect_stdout(listed):
        code = tool.main(["--list-plain"])
    check(code == 0 and len(listed.getvalue().strip().splitlines()) == 120,
          "--list-plain prints the 120 and asks nothing")

    fingerprint = tool.plain_fingerprint(rows)
    moved_read = [dict(r) for r in rows]
    moved_read[4]["answer"] = ["SOMETHING_ELSE"]
    check(tool.plain_fingerprint(moved_read) != fingerprint,
          "a named read changed moves the set's fingerprint")
    check(tool.plain_fingerprint(rows) != tool.key_fingerprint(
        tool.load_key(KEY)[1]), "and it is not his key's fingerprint")

    def one(n, answer, got_risk="READ", place_=None, got="X", route="hybrid",
            row=477):
        return {"n": n, "row": row, "question": "q%d" % n, "answer": answer,
                "error": None, "route": route, "got": got,
                "got_risk": got_risk, "order": [], "place": place_,
                "writes": got_risk in ("MODIFY", "PUBLISH", "ADMIN")}

    results = [one(1, ["A"], place_=1, route="identity"),
               one(2, ["A"], place_=3),
               one(3, ["A"]),
               one(4, ["A"], got_risk="MODIFY", place_=2, row=478),
               one(5, [], got_risk="ANALYZE"),
               one(6, [], got_risk="ADMIN", row=478),
               one(7, [], got=None, got_risk=None)]
    codes = "".join(tool.plain_code(r) for r in results)
    check(codes == "13-w.w0", "one character per plain row: %r" % codes)
    summary = tool.summarise_plain(results)
    check((summary["writes"], summary["exact"], summary["named"],
           summary["named_first"], summary["named_top3"]) == (2, 1, 4, 1, 3),
          "writes, exact phrases, and the named read 1st and in the top 3 are "
          "counted apart - a write with the read 2nd still showed it in the "
          "top three: %s" % summary)
    check(summary["writes_by_row"] == {478: 2},
          "and the writes are split by the row each question came from")

    kinds7 = [tool.NAMED] * 4 + [tool.UNNAMED] * 3
    planted = "w3-w..0"              # #1 answered by a change; #6 safe now
    falls = tool.moved(codes, planted, kinds7)
    check([(m[0], m[3]) for m in falls] == [(0, "down"), (5, "up")],
          "a named read first becoming a change is DOWN, a change becoming a "
          "read is UP: %s" % falls)
    check([m[3] for m in tool.moved(".", "w", [tool.UNNAMED])] == ["down"],
          "an unnamed question reaching a change is DOWN")
    check([m[3] for m in tool.moved("2", "-", [tool.NAMED])] == ["down"],
          "a named read falling out of the shortlist is DOWN")

    stamp = {"date": "2026-01-01", "fragments": 500, "backend": "lexical",
             "revit": None, "key": "feed1234"}
    line = tool.plain_history_row(stamp, summary, codes)
    table = "\n".join([tool.PLAIN_HEADER, "|" + "---|" * len(tool.PLAIN_COLUMNS),
                       line, ""])
    parsed = tool.history_rows(table, tool.PLAIN_HEADER, tool.PLAIN_COLUMNS)
    check(parsed is not None and parsed[0]["Per question"] == codes
          and parsed[0]["Reached a write"] == "2",
          "a plain row reads back as it was written")
    check(tool.history_rows(table) is None,
          "and his table's reader does not mistake it for his")
    with io.open(os.path.join(ROOT, "brain", "retrieval-history.md"),
                 encoding="utf-8") as handle:
        held = handle.read()
    check(tool.history_rows(held, tool.PLAIN_HEADER, tool.PLAIN_COLUMNS)
          is not None, "brain/retrieval-history.md carries the plain table")

    work = tempfile.mkdtemp(prefix="heron-plain-hist-")
    try:
        history = os.path.join(work, "history.md")
        with io.open(history, "w", encoding="utf-8") as handle:
            handle.write(table)
        spoken = io.StringIO()
        worse = dict(summary, writes=3)
        with contextlib.redirect_stdout(spoken):
            said = tool.compare_plain(history, stamp, planted, worse, kinds7,
                                      [{"n": r["n"], "question": "plain %d"
                                        % r["n"]} for r in results])
        check(said and "THE PLAIN QUESTIONS DROPPED" in spoken.getvalue()
              and "plain 1" in spoken.getvalue(),
              "compare_plain() says the plain questions dropped, and names "
              "the one that fell")
        check(tool.exit_code(said) == 0, "and it still exits 0 (D-100)")
    finally:
        shutil.rmtree(work, ignore_errors=True)

    backends = [{"route": "hybrid", "error": None,
                 "backends": {"nearness": "lexical"}},
                {"route": "hybrid", "error": None,
                 "backends": {"nearness": "not used"}}]
    check(tool.backend_of(backends) == "lexical",
          "an answer the meaning route found nothing for is no vote - the "
          "run is not called mixed for it")
    check(tool.backend_of(backends[1:]) == "not used",
          "and a run where no answer carried a rank says so")


# ---------------------------------------------------------------------------
# Section 9: a session's own sentences - rows 5b-481, 5b-485 and 5b-510
# ---------------------------------------------------------------------------

# THE FIXTURE'S CROSS-ENCODER: the words a question and a passage share. It
# replaces heron_rerank._construct, the one place weights are ever loaded, so
# everything else the re-ranker does is the real module's.
FAKE_SCORER = '''
def _construct(offline):
    """THE TEST'S CROSS-ENCODER - words shared by question and passage."""
    class _Fake(object):
        def predict(self, pairs):
            return [_overlap(q, p) for q, p in pairs]
    return _Fake()


def _overlap(question, passage):
    import re
    return float(len(set(re.findall(r"[a-z0-9]+", question.lower()))
                     & set(re.findall(r"[a-z0-9]+", passage.lower()))))
'''

# AND NO TRAINED ENCODER, whatever the machine running the test has - so the
# fixture fetches nothing and `model` is refused on every machine alike.
NO_MODEL = '''
def _load_model():
    """THE TEST'S MACHINE HAS NO TRAINED ENCODER."""
    return None
'''

FIXTURE_CARDS = ("rename-elements", "find-views", "rename-family",
                 "count-elements")

# Words no card says, so the fake scorer's winner is certain whatever the
# cards' own words become.
WANTED = "rename plugh xyzzy"
QUESTION = "how many frotz"

SECTION_9_NAMES = ("load_sentences", "parse_expect", "backend_problems",
                   "ask_tree", "compare_runs", "tag", "load_record", "rescore",
                   "load_identities", "sentence_fingerprint", "BACKENDS")


def _overlap(question, passage):
    """The fake's own count, in this process. WORDS, NOT SPACE-SPLIT PIECES:
    since D-134 a passage reads "identity. sentences. capability domain", and
    "frotz." is not "frotz"."""
    return float(len(set(re.findall(r"[a-z0-9]+", question.lower()))
                     & set(re.findall(r"[a-z0-9]+", passage.lower()))))


def _fixture_tree(top, cards=FIXTURE_CARDS):
    """A tiny checkout in TOP: this tree's search code, a few cards, the fake
    cross-encoder and no trained encoder."""
    for part in (("brain",), ("mcp", "server")):
        source, there = os.path.join(ROOT, *part), os.path.join(top, *part)
        os.makedirs(there)
        for name in os.listdir(source):
            if name.endswith(".py"):
                shutil.copy2(os.path.join(source, name), there)
    os.makedirs(os.path.join(top, "brain", "skills"))
    for card in cards:
        shutil.copytree(os.path.join(ROOT, "brain", "fragments", card),
                        os.path.join(top, "brain", "fragments", card))
    for name, extra in (("heron_rerank.py", FAKE_SCORER),
                        ("heron_embed.py", NO_MODEL)):
        with io.open(os.path.join(top, "brain", name), "a",
                     encoding="utf-8") as handle:
            handle.write("\n\n" + extra)
    return top


def _declared(card):
    """The first utterance CARD declares - answered by identity, certainly."""
    import yaml
    with io.open(os.path.join(ROOT, "brain", "fragments", card,
                              "fragment.yaml"), encoding="utf-8") as handle:
        doc = yaml.safe_load(handle) or {}
    said = doc.get("utterances") or []
    return " ".join(str(said[0]).split()) if said else None


def _run(tool, argv):
    """(exit code, stdout and stderr) of tool.main(ARGV), never raising."""
    said = io.StringIO()
    try:
        with contextlib.redirect_stdout(said), contextlib.redirect_stderr(said):
            code = tool.main(argv)
    except BaseException as raised:                      # noqa: BLE001
        code = "raised %s: %s" % (type(raised).__name__, raised)
    return code, said.getvalue()


def _answer(n, route="hybrid", capability="A", risk="READ", nearness="lexical",
            rerank="not used", candidates=3, say=None):
    return {"n": n, "say": say or "s%d" % n, "route": route,
            "capability": capability, "risk": risk, "order": [capability],
            "candidates": candidates, "error": None,
            "backends": {"nearness": nearness, "rerank": rerank}}


def sentence_shape(tool, work):
    """9a. A sentence set's shape, and the expectations it may carry."""
    good = os.path.join(work, "good.yaml")
    with io.open(good, "w", encoding="utf-8") as handle:
        handle.write("groups:\n"
                     "  - group: renames\n"
                     "    expect: want RENAME_ELEMENTS\n"
                     "    sentences:\n"
                     "      - rename   the plan\n"
                     "      - say: which plans are there\n"
                     "        expect: not-a-write\n"
                     "  - group: watched\n"
                     "    sentences:\n"
                     "      - say: rename the plan\n"
                     "        expect: want RENAME_ELEMENTS\n"
                     "      - say: rename the families\n"
                     "        expect: neutral\n")
    _doc, items, problems = tool.load_sentences(good)
    check(not problems and len(items) == 3,
          "a set with a group default, a sentence's own expectation and one "
          "sentence in two groups is well formed: %s" % (problems,))
    first = items[0] if items else {}
    check(first.get("say") == "rename the plan"
          and first.get("groups") == ["renames", "watched"],
          "a sentence in two groups is asked once and belongs to both")
    check([i.get("expect") for i in items] == ["want RENAME_ELEMENTS",
                                               "not-a-write", "neutral"],
          "and each keeps one expectation: %s" % [i.get("expect")
                                                  for i in items])
    check(tool.parse_expect("want COUNT_ELEMENTS") == ("want", "COUNT_ELEMENTS")
          and tool.parse_expect("NOT-A-WRITE") == ("not-a-write", None)
          and tool.parse_expect("want count_elements") is None
          and tool.parse_expect("want") is None
          and tool.parse_expect("probably") is None,
          "want <CAPABILITY>, not-a-write and neutral, and nothing else")

    bad = os.path.join(work, "bad.yaml")
    with io.open(bad, "w", encoding="utf-8") as handle:
        handle.write("groups:\n"
                     "  - sentences: [no name]\n"
                     "  - group: one\n"
                     "    expect: hopefully\n"
                     "    sentences: [a]\n"
                     "  - group: two\n"
                     "    sentences: []\n"
                     "  - group: three\n"
                     "    expect: neutral\n"
                     "    sentences:\n"
                     "      - which view: this one\n"
                     "      - '   '\n"
                     "      - say: b\n"
                     "        expect: want B\n"
                     "  - group: four\n"
                     "    sentences:\n"
                     "      - say: b\n"
                     "        expect: neutral\n")
    _doc, _items, problems = tool.load_sentences(bad)
    check(len(problems) == 7,
          "a group with no name, an expectation that is none, a group with "
          "no sentences, an unquoted colon, an empty sentence, a sentence "
          "with no expectation and one with two are all seven named: %s"
          % (problems,))
    code, said = _run(tool, ["--sentences", bad])
    check(code == 2 and "not well formed" in said,
          "and a malformed set is exit 2, not a score")
    code, said = _run(tool, ["--sentences", good, "--list"])
    check(code == 0 and len(said.strip().splitlines()) == 3,
          "--list with --sentences prints the set and asks nothing")
    check(tool.sentence_fingerprint(items) != tool.sentence_fingerprint(
        [dict(items[0], expect="neutral")] + items[1:]),
        "an expectation changed moves the set's fingerprint")
    return good


def backend_evidence(tool):
    """9b. A run is refused when its answers ran on another backend."""
    import heron_embed as EMBED
    import heron_rerank as RERANK
    import heron_retrieve as RETRIEVE

    check((tool.LEXICAL, tool.MODEL) == (EMBED.LEXICAL, EMBED.MODEL)
          and "not used" in tool.NOT_RAN
          and RETRIEVE.ran([])["rerank"] in tool.NOT_RAN
          and RERANK.CROSS_ENCODER not in tool.NOT_RAN,
          "the backend names are the ones heron_retrieve.ran() writes")
    check(set(tool.BACKENDS) == {"lexical", "model", "lexical+rerank",
                                 "model+rerank"},
          "four backends: lexical, model, and each with the cross-encoder")
    lexical = [_answer(1), _answer(2), _answer(3, route="identity",
                                                nearness="not used")]
    check(tool.backend_problems("lexical", lexical) == [],
          "lexical answers are a lexical run, an exact phrase casting no vote")
    fell = [_answer(1), _answer(2)]
    check(tool.backend_problems("model", fell) != [],
          "the model asked for and lexical answering - a silent fallback - "
          "is named")
    check(tool.backend_problems("lexical", [_answer(1), _answer(
        2, rerank="cross-encoder")]) != [],
        "a cross-encoder re-reading a shortlist on a run asked for without "
        "it is named - row 481's 'both backends' were both re-ranked")
    check(tool.backend_problems("model+rerank", [
        _answer(1, nearness="model", rerank="cross-encoder"),
        _answer(2, nearness="model", rerank="not used", candidates=2)]) != [],
        "and a shortlist of two the re-ranker did not re-read, on a run that "
        "asked for it, is named")
    check(tool.backend_problems("model+rerank", [
        _answer(1, nearness="model", rerank="cross-encoder"),
        _answer(2, nearness="model", rerank="not used", candidates=1)]) == [],
        "but a shortlist of one, which the re-ranker never reads, is not")
    check(tool.backend_problems("lexical", [_answer(
        1, nearness="not used")]) != [],
        "a run where no ranked answer named the backend proved nothing")
    check(tool.backend_problems("installed", [_answer(1), _answer(
        2, nearness="model")]) != [],
        "and an unnamed run whose answers disagree is called mixed")


def tagged(tool):
    """9c. Every changed answer, tagged want, bad or neutral."""
    _name, threshold, ladder = tool._sibling(
        "generate-jobs.py").write_threshold()

    def rung(risk):
        return ladder.get((risk or "").upper(), -1)

    def writes(answer):
        return rung(answer.get("risk")) >= threshold

    want = {"kind": "want", "want": "A"}
    question = {"kind": "not-a-write", "want": None}
    plain = {"kind": "neutral", "want": None}
    read_a, read_b = {"capability": "A", "risk": "READ"}, {"capability": "B",
                                                           "risk": "READ"}
    write_c = {"capability": "C", "risk": "MODIFY"}
    check([tool.tag(want, read_b, read_a, writes),
           tool.tag(want, read_a, read_b, writes),
           tool.tag(want, read_b, write_c, writes)] == ["want", "bad",
                                                         "neutral"],
          "want A: A arriving is a want, A leaving is bad, one wrong answer "
          "for another is neutral")
    check([tool.tag(question, read_b, write_c, writes),
           tool.tag(question, write_c, read_b, writes),
           tool.tag(question, read_a, read_b, writes),
           tool.tag(plain, read_a, write_c, writes)] == ["bad", "want",
                                                          "neutral", "neutral"],
          "a question reaching a write is bad, leaving one is a want, read "
          "to read is neutral, and a neutral sentence is neutral")

    items = [dict(want, n=1, say="one", expect="want A", groups=["g"]),
             dict(question, n=2, say="two", expect="not-a-write", groups=["g"]),
             dict(question, n=3, say="three", expect="not-a-write",
                  groups=["g"]),
             dict(plain, n=4, say="four", expect="neutral", groups=["g"])]
    before = {"answers": [dict(_answer(1, capability="B"), say="one"),
                          dict(_answer(2, capability="B"), say="two"),
                          dict(_answer(3, capability="C", risk="MODIFY"),
                               say="three"),
                          dict(_answer(4, capability="B"), say="four")]}
    after = {"answers": [dict(_answer(1, capability="A"), say="one"),
                         dict(_answer(2, capability="C", risk="MODIFY"),
                              say="two"),
                         dict(_answer(3, capability="C", risk="MODIFY"),
                              say="three"),
                         dict(_answer(4, capability="A"), say="four")]}
    spoken = io.StringIO()
    with contextlib.redirect_stdout(spoken):
        counts = tool.compare_runs("lexical", before, after, items, rung,
                                   threshold)
    text = spoken.getvalue()
    check((counts["changed"], counts["want"], counts["bad"],
           counts["neutral"]) == (3, 1, 1, 1),
          "three answers changed - one want, one bad, one neutral - and the "
          "one that did not change is not listed: %s" % counts)
    check((counts["write_before"], counts["write_after"]) == (1, 2)
          and "questions reaching a write  1 -> 2 of 2" in text,
          "and the questions reaching a write are counted before and after")
    check('"three"' not in text and '"two"' in text and "bad" in text,
          "each changed sentence is named with its tag")


def asked_for_real(tool, work):
    """9d-9g. A fixture checkout asked on two backends, recorded, compared and
    re-scored - nothing reaches brain/retrieval-history.md."""
    import heron_rerank as RERANK

    history = os.path.join(ROOT, "brain", "retrieval-history.md")
    with io.open(history, "rb") as handle:
        history_md5 = hashlib.md5(handle.read()).hexdigest()

    tree_a = _fixture_tree(os.path.join(work, "tree-a"))
    tree_b = _fixture_tree(os.path.join(work, "tree-b"),
                           [c for c in FIXTURE_CARDS if c != "rename-elements"])
    declared = _declared("rename-elements")
    check(bool(declared), "rename-elements declares an utterance to ask")
    sentences = os.path.join(work, "set.yaml")
    with io.open(sentences, "w", encoding="utf-8") as handle:
        handle.write("groups:\n"
                     "  - group: the card's own\n"
                     "    expect: want RENAME_ELEMENTS\n"
                     "    sentences:\n"
                     "      - %s\n"
                     "      - %s\n"
                     "  - group: questions\n"
                     "    expect: not-a-write\n"
                     "    sentences:\n"
                     "      - %s\n"
                     "      - which views are on level 2\n"
                     % (json.dumps(declared), WANTED, QUESTION))
    record_a = os.path.join(work, "a.json")
    code, said = _run(tool, ["--sentences", sentences, "--tree", tree_a,
                             "--backend", "lexical", "--backend",
                             "lexical+rerank", "--backend", "model",
                             "--shortlists", "--out", record_a])
    check(code == 0 and os.path.exists(record_a),
          "the fixture is asked and recorded, exit 0 (D-100): %r %s"
          % (code, said[-300:] if code != 0 else ""))
    check("NOT ASKED ON MODEL" in said and "Not on model" in said,
          "the model, which the fixture has not got, is named as not asked - "
          "never measured as lexical under its name")
    if not os.path.exists(record_a):
        return
    with io.open(record_a, encoding="utf-8") as handle:
        record = json.load(handle)
    runs = record.get("runs") or {}
    check(sorted(runs) == ["lexical", "lexical+rerank"],
          "one run per backend asked and had: %s" % sorted(runs))
    lexical, reread = runs.get("lexical") or {}, runs.get("lexical+rerank") or {}
    answers = lexical.get("answers") or []
    check(len(answers) == 4 and all(a.get("backends") for a in answers),
          "every answer carries the backends that produced it")
    by_say = dict((a["say"], a) for a in answers)
    check((by_say.get(declared) or {}).get("route") == "identity"
          and (by_say.get(declared) or {}).get("shortlist") is None,
          "a declared sentence is answered by identity and reaches no "
          "shortlist")
    ranked = [a for a in reread.get("answers") or []
              if a.get("route") == "hybrid"]
    check(ranked and all(a["backends"]["rerank"] == "cross-encoder"
                         for a in ranked if a["candidates"] >= 2)
          and all(a["backends"]["rerank"] == "not used"
                  for a in answers if a.get("route") == "hybrid"),
          "the re-ranked run was re-read by the cross-encoder and the "
          "lexical one was not - each in its own process")
    lists = [a.get("shortlist") or [] for a in ranked]
    check(lists and all(0 < len(s) <= 20 for s in lists),
          "--shortlists keeps each re-read shortlist, never more than 20")
    check(all(e.get("rerank") == _overlap(a["say"], e.get("passage") or "")
              for a in ranked for e in a.get("shortlist") or []),
          "and each entry's score is the fake cross-encoder's on the passage "
          "recorded - the shortlist kept is the one the re-ranker read")
    head = record.get("tree") or {}
    check(head.get("path") == os.path.realpath(tree_a)
          and record.get("sentences", {}).get("fingerprint"),
          "the record names the tree it measured and the set it asked")
    check(tool.tree_head(os.path.join(ROOT, "brain")) == (None, None),
          "a folder inside a checkout is not one - no enclosing commit is "
          "claimed for it")

    # 9e - THE RECORD IS REFUSED ON THE WRONG BACKEND, written or read.
    planted = json.loads(json.dumps(record))
    for answer in planted["runs"]["lexical"]["answers"]:
        if answer.get("route") == "hybrid":
            answer["backends"]["nearness"] = "model"
    wrong = os.path.join(work, "wrong.json")
    with io.open(wrong, "w", encoding="utf-8") as handle:
        json.dump(planted, handle)
    _record, refused = tool.load_record(wrong)
    check("lexical" in refused and "lexical+rerank" not in refused,
          "a run filed as lexical whose answers say model is refused when it "
          "is read back, and the other run is not: %s" % refused)
    real_ask = tool.ask_tree
    # Shaped as a worker returns it - capabilities a list of pairs.
    tool.ask_tree = lambda *a, **k: dict(
        planted["runs"]["lexical"], capabilities=[
            [c, r] for c, r in planted["runs"]["lexical"]["capabilities"]
            .items()])
    try:
        out = os.path.join(work, "never.json")
        code, said = _run(tool, ["--sentences", sentences, "--tree", tree_a,
                                 "--backend", "lexical", "--out", out])
    finally:
        tool.ask_tree = real_ask
    check(code == 2 and not os.path.exists(out) and "NOT RECORDED" in said,
          "and a run whose answers ran on another backend is never written: "
          "exit 2, no record (got %r)" % (code,))

    # 9f - EVERY CHANGED ANSWER, TAGGED, against a tree with one card gone.
    record_b = os.path.join(work, "b.json")
    code, said = _run(tool, ["--sentences", sentences, "--tree", tree_b,
                             "--backend", "lexical", "--backend",
                             "lexical+rerank", "--compare", record_a,
                             "--out", record_b])
    check(code == 0 and said.count("LEXICAL - 4 sentences in both") == 1
          and "LEXICAL+RERANK - 4 sentences in both" in said,
          "--compare lines the two up on each backend (got %r)" % (code,))
    # The declared sentence is #1 of the set.
    lost = [line for line in said.splitlines()
            if line.split()[:2] == ["bad", "#1"]]
    check(len(lost) == 2,
          "the declared sentence whose card is gone is listed as bad on both "
          "backends: %s" % lost)
    code, said = _run(tool, ["--sentences", sentences, "--tree", tree_a,
                             "--backend", "lexical", "--compare", record_b])
    check(code == 0 and any(line.split()[:2] == ["want", "#1"]
                            for line in said.splitlines()),
          "and the other way round it is a want")
    code, said = _run(tool, ["--sentences", sentences, "--tree", tree_a,
                             "--backend", "lexical", "--compare", wrong])
    check("NOT COMPARED ON LEXICAL" in said,
          "an earlier run on the wrong backend is not compared with")

    # 9g - THE FAST LOOP: identities re-scored on the recorded shortlists.
    _name, threshold, ladder = tool._sibling(
        "generate-jobs.py").write_threshold()

    def rung(risk):
        return ladder.get((risk or "").upper(), -1)

    items = tool.items_from_record(record)
    win, take = "rename plugh xyzzy", "how many frotz"
    holding = [a for a in ranked if a["say"] in (WANTED, QUESTION)
               and any(e.get("capability") == "RENAME_ELEMENTS"
                       for e in a.get("shortlist") or [])]
    check(len(holding) == 2, "the fixture's precondition: both made-up "
          "sentences' shortlists hold the card (%d do)" % len(holding))
    found = tool.rescore(items, reread, "RENAME_ELEMENTS", [win, take],
                         lambda q, ps: [_overlap(q, p) for p in ps], rung,
                         threshold)
    lines = dict((identity, t) for identity, t in found["lines"])
    check(found["skipped"]["identity"] == 1,
          "the declared sentence is skipped - it never reaches the re-ranker")
    check(found["agree"] == found["read"] and found["read"] >= 2,
          "the recorded identity, re-scored, gives every winner the run gave "
          "(%d of %d)" % (found["agree"], found["read"]))
    check((lines[win]["won"], lines[win]["wanted"]) == (1, 1),
          "an identity holding the wanted sentence's words wins it: %s"
          % lines[win])
    check((lines[take]["taken"], lines[take]["questions"]) == (1, 1),
          "and one holding the question's words takes that question, which a "
          "write must not: %s" % lines[take])
    turned = [dict(item, n=k) for k, item in enumerate(reversed(items), 1)]
    again = tool.rescore(turned, reread, "RENAME_ELEMENTS", [win, take],
                         lambda q, ps: [_overlap(q, p) for p in ps], rung,
                         threshold)
    check(again["lines"] == found["lines"],
          "a set in another order is matched to the record by its words, not "
          "by its numbers")
    gone = json.loads(json.dumps(reread))
    for answer in gone["answers"]:
        if answer["say"] == WANTED:
            answer["shortlist"] = [e for e in answer["shortlist"]
                                   if e["capability"] != "RENAME_ELEMENTS"]
    out_of_reach = tool.rescore(items, gone, "RENAME_ELEMENTS", [win],
                                lambda q, ps: [_overlap(q, p) for p in ps],
                                rung, threshold)
    check(out_of_reach["out_of_reach"]["wanted"] == 1,
          "a shortlist that did not hold the card is out of reach, never "
          "counted as lost")

    candidates = os.path.join(work, "ids.txt")
    with io.open(candidates, "w", encoding="utf-8") as handle:
        handle.write("# candidates\n%s\n\n%s\n%s\n" % (win, take, win))
    check(tool.load_identities(candidates) == [win, take],
          "the candidates file: one per line, comments and repeats skipped")
    kept = list(RERANK._CACHE)
    try:
        RERANK._CACHE[:] = [lambda pairs: [_overlap(q, p) for q, p in pairs]]
        code, said = _run(tool, ["--identities", candidates, "--card",
                                 "RENAME_ELEMENTS", "--from", record_a])
        check(code == 0 and said.count("RE-SCORED OFFLINE") == 2
              and '"%s"' % take in said,
              "--identities --from re-scores every run's shortlists, one line "
              "per candidate (got %r)" % (code,))
        RERANK._CACHE[:] = [None]
        code, said = _run(tool, ["--identities", candidates, "--card",
                                 "RENAME_ELEMENTS", "--from", record_a])
        check(code == 2 and "needs a cross-encoder" in said,
              "with no cross-encoder on this machine it is exit 2, not a score")
    finally:
        RERANK._CACHE[:] = kept

    for argv, why in (
            (["--tree", tree_a], "--tree with no sentence set"),
            (["--sentences", sentences, "--record"],
             "--record with a sentence set - the history holds his and the "
             "plain sets only"),
            (["--sentences", sentences, "--identities", candidates, "--card",
              "RENAME_ELEMENTS"], "--identities asking without --shortlists"),
            (["--identities", candidates, "--from", record_a],
             "--identities with no --card"),
            (["--from", record_a, "--out", os.path.join(work, "x.json")],
             "--from with --out - it asks nothing"),
            (["--sentences", sentences, "--backend", "fastest"],
             "a backend that is none of the four")):
        code, _said = _run(tool, argv)
        check(code == 2, "exit 2 for %s (got %r)" % (why, code))
    with io.open(history, "rb") as handle:
        check(hashlib.md5(handle.read()).hexdigest() == history_md5,
              "and brain/retrieval-history.md is byte for byte as it was")


def sentence_sets(tool):
    """Section 9. Every name is asked for before it is called, so a tool
    without it fails once per name rather than raising once."""
    missing = [n for n in SECTION_9_NAMES if not hasattr(tool, n)]
    for name in SECTION_9_NAMES:
        check(name not in missing, "the tool has %s" % name)
    if missing:
        return
    work = tempfile.mkdtemp(prefix="heron-score-sentences-test-")
    try:
        sentence_shape(tool, work)
        backend_evidence(tool)
        tagged(tool)
        asked_for_real(tool, work)
    finally:
        shutil.rmtree(work, ignore_errors=True)


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    try:
        import yaml
    except ImportError:
        print("COULD NOT RUN: this needs PyYAML - pip install --user pyyaml")
        return 3

    tool = load()
    check(tool is not None, "tools/score-routing.py loads")
    if tool is None:
        return finish()

    # ------------------------------------------------------------------
    print()
    print("1. HIS answer key - checked against the library, never against the search")
    doc, rows, problems = tool.load_key(KEY)
    check(not problems, "the key is well formed%s"
          % ("" if not problems else ": " + "; ".join(problems[:3])))
    check(len(rows) == 79 and [r["n"] for r in rows] == list(range(1, 80)),
          "79 questions, numbered 1 to 79 in order")
    blob = "\n".join(" ".join(r["question"].split()) for r in rows)
    check(hashlib.sha256(blob.encode("utf-8")).hexdigest() == QUESTIONS_PIN,
          "the questions are his words, unchanged - a reworded question fails "
          "here, and moves only on his word")
    confirmed = doc.get("confirmed") or {}
    check(confirmed.get("by") and confirmed.get("date") and confirmed.get("said"),
          "it records who confirmed it, when, and what he said")

    capabilities = set()
    for name in os.listdir(os.path.join(ROOT, "brain", "fragments")):
        path = os.path.join(ROOT, "brain", "fragments", name, "fragment.yaml")
        if os.path.exists(path):
            with io.open(path, encoding="utf-8") as handle:
                capabilities.add((yaml.safe_load(handle) or {}).get("capability"))
    skills = tool.load_skills()
    import heron_tools
    unknown = tool.unknown_answers(rows, capabilities, skills, heron_tools.TOOLS)
    check(not unknown, "every answer names a capability, skill or tool Heron "
          "has%s" % ("" if not unknown else ": " + "; ".join(
              "#%s %s" % u for u in unknown[:3])))
    kinds = [r["kind"] for r in rows]
    check(all(r.get("answer") is None for r in rows if r["kind"] == "gap"),
          "a gap names no answer - what exists is only a note")
    check(set(kinds) <= set(tool.KINDS), "every kind is one of %s"
          % ", ".join(tool.KINDS))

    # ------------------------------------------------------------------
    print()
    print("2. The key's fingerprint moves with any answer, so a correction is "
          "never read as a movement")
    same = tool.key_fingerprint(rows)
    check(same == tool.key_fingerprint([dict(r) for r in rows]),
          "the same key gives the same fingerprint")
    changed = [dict(r) for r in rows]
    changed[40]["answer"] = "SOMETHING_ELSE"
    check(tool.key_fingerprint(changed) != same,
          "one answer changed moves it")
    reworded = [dict(r) for r in rows]
    reworded[0]["question"] = reworded[0]["question"] + " please"
    check(tool.key_fingerprint(reworded) != same, "one question changed moves it")

    # ------------------------------------------------------------------
    print()
    print("3. Where a row lands, and the one class that is not a judgement call")
    check(tool.shortlist({"capability": "A", "candidates": [
        {"capability": "A"}, {"capability": "B"}, {"capability": "A"},
        {"capability": "C"}]}) == ["A", "B", "C"],
        "the shortlist is the winner, then each other capability once - the "
        "way heron_lookup prints it")
    check(tool.shortlist({"route": "identity", "capability": "A",
                          "candidates": []}) == ["A"],
          "an exact phrase shows one capability and no others")
    check(tool.place(["A", "B", "C"], {"C", "Z"}) == (3, "C"),
          "a place is the first wanted name in that order")
    check(tool.place(["A", "B"], {"Z"}) == (None, None), "absent is None")

    handed = result(tool, 1, "capability", place=None, risk="MODIFY",
                    asked=False)
    check(tool.handed_a_change(handed),
          "a row whose answer changes nothing, answered by a MODIFY, is D-86's "
          "harm")
    check(not tool.handed_a_change(result(tool, 2, "capability", place=1,
                                          risk="MODIFY", asked=True)),
          "a change asked for and given is not")
    check(tool.wrong_change(result(tool, 3, "capability", place=None,
                                   risk="MODIFY", asked=True)),
          "a change asked for and a DIFFERENT one given is listed on its own")
    gap_write = result(tool, 4, "gap", risk="MODIFY")
    check(tool.gap_onto_a_change(gap_write) and not tool.handed_a_change(gap_write),
          "a gap row on a change is its own list - a gap has no answer to "
          "say whether a change was asked for")

    results = [result(tool, 1, "capability", place=1, route="identity"),
               result(tool, 2, "capability", place=2),
               result(tool, 3, "capability", place=3),
               result(tool, 4, "capability", place=4),
               result(tool, 5, "capability", place=5),
               result(tool, 6, "capability", place=None, risk="MODIFY"),
               result(tool, 7, "skill", place=1),
               result(tool, 8, "tool", risk="MODIFY"),
               result(tool, 9, "gap", risk="READ")]
    summary = tool.summarise(results)
    check((summary["first"], summary["second_third"], summary["low"],
           summary["not_found"]) == (1, 2, 2, 1),
          "1st, 2nd-3rd, 4th-5th and not found are counted apart")
    check(summary["capability_rows"] == 6 and summary["skill_rows"] == 1,
          "the four count capability rows only - a skill row is kept out")
    check(summary["exact"] == 1 and summary["exact_right"] == 1,
          "an exact declared phrase is counted separately")
    check(summary["handed"] == 2,
          "a READ row answered by a MODIFY and a READ tool answered by a "
          "MODIFY are both handed a change")
    codes = "".join(tool.code_of(r) for r in results)
    check(codes == "12345-1w.", "one character per row: %r" % codes)

    # ------------------------------------------------------------------
    print()
    print("4. A DROP PLANTED IN THE ARITHMETIC IS CAUGHT")
    stamp = {"date": "2026-01-01", "fragments": 400, "backend": "lexical",
             "revit": None, "key": "abcd1234"}
    kinds9 = [r["kind"] for r in results]
    line = tool.history_row(stamp, summary, codes)
    table = "\n".join(["# a history file", "", tool.TABLE_HEADER,
                       "|" + "---|" * len(tool.COLUMNS), line, "",
                       "text after the table"])
    parsed = tool.history_rows(table)
    check(parsed is not None and len(parsed) == 1
          and parsed[0]["Per question"] == codes
          and parsed[0]["Answer key"] == "abcd1234",
          "a written row reads back as it was written")
    check(tool.history_rows("# no table here") is None,
          "no table is None, which is not the same as an empty one")

    planted = "-2345-1ww"            # #1 fell from first; #9 now a change
    moves = tool.moved(codes, planted, kinds9)
    check([(m[0], m[3]) for m in moves] == [(0, "down"), (8, "down")],
          "both planted falls are named, and nothing else: %s" % moves)
    check(tool.dropped(moves, parsed[0], summary),
          "and it is called a drop")
    better = "1234211w."
    check([m[3] for m in tool.moved(codes, better, kinds9)] == ["up", "up"],
          "a question climbing is an UP, never a drop")
    check(not tool.dropped(tool.moved(codes, codes, kinds9), parsed[0], summary),
          "the same codes are no drop")
    more_handed = dict(summary, handed=summary["handed"] + 1)
    check(tool.dropped([], parsed[0], more_handed),
          "one more question handed a change is a drop even if no place moved")
    check([m[3] for m in tool.moved("1", "?", ["capability"])] == ["unreadable"],
          "a row that could not be scored is never read as a movement")
    # A HAND-EDITED ROW MUST NOT TAKE THE COMPARISON DOWN. The per-question
    # string comes back out of a markdown file a person can edit, so a
    # character this tool never writes is unreadable - never a crash, and
    # never read as "no change" on a tool or gap row.
    try:
        odd = [m[3] for m in tool.moved("1.w", "x?z", ["capability", "gap",
                                                       "tool"])]
    except (ValueError, KeyError, TypeError) as why:
        odd = "raised %s" % type(why).__name__
    check(odd == ["unreadable"] * 3,
          "a character this tool never writes is unreadable, not a crash: %s"
          % (odd,))

    # D-100. The owner decided on 2026-09-23 that a drop is REPORTED and
    # never stops a pull request. Asked by name first, so the check fails
    # cleanly on a tool that has no word for it (heron-ship section 2a).
    decided = getattr(tool, "exit_code", None)
    check(callable(decided), "the exit code is made in one place, exit_code()")
    if callable(decided):
        check(decided(True) == 0 and decided(False) == 0,
              "a drop exits 0 - reported, never a failure (D-100)")

    other = dict(stamp, backend="model")
    check(tool.last_comparable(parsed, other) is None,
          "a run on another backend is not comparable")
    check(tool.last_comparable(parsed, dict(stamp, key="ffff0000")) is None,
          "nor a run of another answer key")
    check(tool.last_comparable(parsed, dict(stamp, fragments=401)) is not None,
          "but a bigger library IS comparable - that is the drop it is for")

    work = tempfile.mkdtemp(prefix="heron-score-test-")
    try:
        history = os.path.join(work, "history.md")
        with io.open(history, "w", encoding="utf-8") as handle:
            handle.write(table)
        spoken = io.StringIO()
        with contextlib.redirect_stdout(spoken):
            said_drop = tool.compare(history, dict(stamp, fragments=401),
                                     planted, tool.summarise(results),
                                     kinds9, [{"n": r["n"], "question":
                                               "question %d" % r["n"]}
                                              for r in results])
        text = spoken.getvalue()
        check(said_drop and "THE SCORE DROPPED" in text,
              "compare() says THE SCORE DROPPED")
        check("#1 " in text and "#9 " in text and "question 1" in text,
              "and names the questions that fell")
        check("400 -> 401" in text,
              "and says the library changed size between the two runs")

        check(tool.append_row(history, "| new row |"),
              "a row is appended to the table")
        with io.open(history, encoding="utf-8") as handle:
            after = handle.read().split("\n")
        check(after.index("| new row |") == after.index(line) + 1
              and after[-1] == "text after the table",
              "right after the last row, and nothing after the table moves")
        # A WINDOWS CHECKOUT HOLDS CRLF. The table must still be found, and
        # the file must keep its own line endings when a row goes in.
        windows = os.path.join(work, "windows.md")
        with io.open(windows, "w", encoding="utf-8", newline="") as handle:
            handle.write(table.replace("\n", "\r\n"))
        check(tool.append_row(windows, "| crlf row |"),
              "a CRLF file's table is found, and the row goes in")
        with io.open(windows, "rb") as handle:
            raw = handle.read()
        check(b"| crlf row |\r\n" in raw and b"\n" not in raw.replace(
            b"\r\n", b""), "and every line still ends CRLF")
        bare = os.path.join(work, "bare.md")
        with io.open(bare, "w", encoding="utf-8") as handle:
            handle.write("# nothing\n")
        check(not tool.append_row(bare, "| x |"),
              "and a file with no table is refused rather than written to")
    finally:
        shutil.rmtree(work, ignore_errors=True)

    # ------------------------------------------------------------------
    print()
    print("5. A DROP PLANTED IN A REAL STORE, THROUGH THE REAL SEARCH, IS CAUGHT")
    saved = dict((k, os.environ.get(k)) for k in ("HERON_KNOWLEDGE",
                                                   "HERON_AUDIT"))
    private = tempfile.mkdtemp(prefix="heron-score-kb-")
    trail = tempfile.mkdtemp(prefix="heron-score-trail-")
    try:
        os.environ["HERON_KNOWLEDGE"] = private
        os.environ["HERON_AUDIT"] = trail
        import heron_scope as SCOPE
        import heron_brain as brain

        built, _problems = SCOPE.rebuild()
        check(built > 0, "a private store is built (%d fragments)" % built)

        audit_folder = []
        with tool._AuditElsewhere() as folder:
            audit_folder.append(folder)
            check(os.environ.get("HERON_AUDIT") == folder != trail,
                  "during a run the trail goes to a throwaway folder")
        check(os.environ.get("HERON_AUDIT") == trail
              and not os.path.exists(audit_folder[0]),
              "and afterwards the owner's own setting is back and the "
              "throwaway folder is gone")

        # Two of HIS questions, asked for real. The answers in THIS key are
        # read off the first pass, so each is first by construction.
        asked = [dict(r) for r in rows if r["n"] in (10, 59)]
        _name, threshold, ladder = tool._sibling(
            "generate-jobs.py").write_threshold()

        def rung(risk):
            return ladder.get((risk or "").upper(), -1)

        store = SCOPE.open_scope(SCOPE.GLOBAL)
        risk_of = {}
        for r in store.fragments():
            risk_of[r["capability"]] = r["risk"]
        store.close()

        with tool._AuditElsewhere():
            first_pass = [brain.lookup(r["question"]) for r in asked]
        for row, found in zip(asked, first_pass):
            row["kind"], row["answer"] = "capability", found.get("capability")
        check(all(r["answer"] for r in asked),
              "the search named a capability for both questions")

        with tool._AuditElsewhere():
            before = tool.measure(asked, brain.lookup, risk_of, {}, {}, rung,
                                  threshold)
        before_codes = "".join(tool.code_of(r) for r in before)
        check(before_codes == "11", "both are first before the drop: %r"
              % before_codes)

        # THE PLANT: every fragment providing either answer is RETIRED in the
        # private store. The ids stay, so nothing detects drift and rebuilds;
        # the search simply may not offer a DEPRECATED fragment.
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        for r in asked:
            store.execute("UPDATE fragments SET status = 'DEPRECATED' "
                          "WHERE capability = ?", (r["answer"],))
        store.db.commit()
        store.close()

        with tool._AuditElsewhere():
            after = tool.measure(asked, brain.lookup, risk_of, {}, {}, rung,
                                 threshold)
        after_codes = "".join(tool.code_of(r) for r in after)
        check("1" not in after_codes,
              "after the plant neither is first: %r" % after_codes)
        falls = tool.moved(before_codes, after_codes, ["capability"] * 2)
        check(len(falls) == 2 and all(m[3] == "down" for m in falls),
              "the scorer names both falls as DOWN")
        row = {"Handed a change": "0"}
        check(tool.dropped(falls, row, tool.summarise(after)),
              "and calls it a drop")
        check(not os.listdir(trail),
              "and not one of those lookups reached the audit trail")
    finally:
        for key, value in saved.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
        shutil.rmtree(private, ignore_errors=True)
        shutil.rmtree(trail, ignore_errors=True)

    # ------------------------------------------------------------------
    print()
    print("6. It refuses rather than scoring nothing")
    quiet = io.StringIO()
    held = dict((k, os.environ.pop(k, None)) for k in ("HERON_KNOWLEDGE",
                                                        "APPDATA"))
    try:
        with contextlib.redirect_stderr(quiet), contextlib.redirect_stdout(quiet):
            code = tool.main([])
        check(code == 2, "no knowledge store anywhere is exit 2, not a score")
    finally:
        for key, value in held.items():
            if value is not None:
                os.environ[key] = value
    broken = tempfile.mkdtemp(prefix="heron-score-key-")
    try:
        bad = os.path.join(broken, "key.yaml")
        with io.open(bad, "w", encoding="utf-8") as handle:
            handle.write("questions:\n  - n: 1\n    question: ask\n"
                         "    kind: capability\n  - n: 3\n    question: ask\n"
                         "    kind: gap\n    answer: SOMETHING\n")
        with contextlib.redirect_stderr(quiet), contextlib.redirect_stdout(quiet):
            code = tool.main(["--questions", bad, "--list"])
        check(code == 2, "a malformed key is exit 2 - no answer, a number out "
              "of order, a repeated question, a gap with an answer")
        _d, _r, shape = tool.load_key(bad)
        check(len(shape) == 4, "and all four faults are named: %s" % shape)
    finally:
        shutil.rmtree(broken, ignore_errors=True)
    listed = io.StringIO()
    with contextlib.redirect_stdout(listed):
        code = tool.main(["--list"])
    check(code == 0 and len(listed.getvalue().strip().splitlines()) == 79,
          "--list prints his 79 questions and asks nothing")

    # ------------------------------------------------------------------
    print()
    print("7. The store it scores over is check-routing's guard - rows 5b-229 "
          "and 5b-233")
    store_guard(tool)

    # ------------------------------------------------------------------
    print()
    print("8. The plain questions - rows 5b-477 and 5b-478, frozen and scored "
          "beside his")
    plain_questions(tool, capabilities)

    # ------------------------------------------------------------------
    print()
    print("9. A session's own sentences - rows 5b-481, 5b-485 and 5b-510: "
          "asked on a fixture tree, refused on the wrong backend, compared, "
          "re-scored offline")
    sentence_sets(tool)

    return finish()


if __name__ == "__main__":
    sys.exit(main())
