# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
How often Heron answers without thinking. The metric D-58 put in the registry.

    python tools/measure-routes.py
    python tools/measure-routes.py --revit 2024

WHAT IT REPLACED AND WHY
------------------------
docs/28 used to ask the Observability Agent for "model calls per request".
Heron cannot count those - D-01 puts every model call in the host - so D-58
replaced it with the number Heron CAN see and that docs/19 s5 actually cares
about:

    1. Utterance cache hit?    -> execute, 0 model calls
    2. Capability exact match? -> execute proven fragment, 0 model calls
    3. Capability semantic match? -> 1 T2 call to confirm intent, then execute
    4. No capability?          -> full workflow (expensive, tell the user)

    "Steps 1 and 2 must be tried BEFORE any model is invoked, structurally -
     not as an optimisation added later."

Model calls per request was a PROXY for that rule holding. The share of
requests answered by the identity or cache route measures it DIRECTLY, from
Heron's own side of the wire, with no telemetry and nothing to configure.

TWO DIFFERENT NUMBERS, AND THEY MUST NOT BE ADDED TOGETHER
----------------------------------------------------------
  STRUCTURAL   Ask the library its OWN declared utterances and count the
               routes. It answers "can the short circuit reach these at all",
               which is a property of the library rather than of any user.
               It is a CEILING: real requests are phrased worse than the
               phrasings a fragment declares for itself.

  LIVE         What the utterance cache actually holds - rows, and hits per
               row. That is real traffic, and it is the only half that says
               anything about what people type.

Reporting one and calling it the other is the mistake this split exists to
prevent. The structural number will look excellent forever; it is measuring
the library talking to itself.

WHAT THE FIRST RUN FOUND, AND IT IS THE POINT OF THE TOOL
---------------------------------------------------------
`remember()` - the function that writes the utterance cache - is called from
`tests/test_search.py` and from NOWHERE ELSE. Neither `ask()` nor `find()`
calls it, and no MCP tool does. So in production the cache is never written,
and route 2 can never fire for a real user.

docs/19 s6 calls that cache the one that "pays for itself faster than any of
the others", and docs/19 s5 makes it step 1 of the pipeline. It is built, it
is tested, it is indexed, and nothing fills it.

WHERE remember() SHOULD BE CALLED FROM IS NOT THIS TOOL'S DECISION, and it is
not an obvious one. Caching whatever the keyword route ranked first would make
a GUESS permanent: the next identical request returns the cached fragment by
route 2 and never searches at all. A cache of confirmed resolutions is a
different thing from a cache of first guesses, and only one of them is safe.
That question is parked in docs/OPEN-QUESTIONS.md rather than answered here.

IT IS NOT A GATE AND EXITS 0. It reports a share; there is no agreed target to
miss, and inventing one tonight would make today's library the standard.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as FRAG                      # noqa: E402

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")

# The routes heron_search.ask() can return, in pipeline order. Named here so
# an unrecognised one is reported rather than silently dropped into a total.
DETERMINISTIC = ("identity", "cache")
# `hybrid` is what find() calls its fused keyword-and-nearness answer; `keywords`
# is what ask() calls its unfused one. Both are named so a change of entry point
# cannot silently turn a thinking answer into an UNRECOGNISED ROUTE, and both
# mean the same thing here: a ranked guess rather than a lookup.
THINKING = ("hybrid", "keywords")
NEITHER = ("nothing",)


def utterances():
    """((fragment id, declared phrasing) pairs, problems) for the whole library.

    Through heron_fragment.load_all() rather than parsing here. The first
    version did its own `yaml.safe_load` and skipped anything malformed in
    silence, so a broken fragment would simply have been absent from a share
    computed over "the library" - D-48's failure, in a report rather than a
    loader. load_all names each one and main() prints them.
    """
    found, problems = FRAG.load_all(FRAGMENTS)
    out = []
    for frag in found.values():
        for said in (frag.data.get("utterances") or []):
            out.append((frag.id, said))
    return out, problems


def live_cache(store):
    """(rows, total hits) actually in the utterance cache.

    Returns (None, None) if the table has never been created, which is a
    different thing from a table with no rows and is reported as such.
    """
    import sqlite3
    try:
        row = store.execute(
            "SELECT COUNT(*) AS rows, COALESCE(SUM(hits), 0) AS hits "
            "FROM utterances").fetchone()
    except sqlite3.OperationalError as why:
        # ONLY "no such table" is an answer. Catching every exception here
        # would report a corrupt store, a locked file or a schema change as
        # "the table does not exist yet" - a sentence about a fresh machine,
        # printed on a broken one. The same failure the heron_context MCP tool
        # had this morning, where every exception was called a refusal.
        if "no such table" in str(why).lower():
            return None, None
        raise
    return row["rows"], row["hits"]


def remember_callers():
    """Every place in the tree that actually CALLS remember().

    Parsed with `ast`, not grepped, and the difference is not fussiness: the
    first version of this function grepped for the text "remember(" and matched
    THIS FILE'S OWN DOCSTRING, which describes the problem. It then printed the
    opposite conclusion - "the cache fills with use" - in the one place the tool
    exists to be right about. A text search cannot tell a call from a sentence,
    and the sentence was mine.

    So: a real Call node whose function is named `remember`, whether plain or
    through a module (`SEARCH.remember(...)`). Prose cannot produce one.
    """
    import ast

    found = []
    for where, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d not in (".git", "__pycache__")]
        for name in files:
            if not name.endswith(".py"):
                continue
            path = os.path.join(where, name)
            try:
                with open(path, encoding="utf-8") as fh:
                    tree = ast.parse(fh.read(), filename=path)
            except (OSError, UnicodeDecodeError, SyntaxError):
                # A file this cannot parse is reported rather than skipped
                # silently - "no production caller" must not be an artefact of
                # a file nobody could read.
                found.append("UNREADABLE:" + os.path.relpath(path, ROOT))
                continue
            for node in ast.walk(tree):
                if not isinstance(node, ast.Call):
                    continue
                fn = node.func
                called = (fn.attr if isinstance(fn, ast.Attribute)
                          else fn.id if isinstance(fn, ast.Name) else None)
                if called == "remember":
                    found.append(os.path.relpath(path, ROOT))
                    break
    return sorted(set(found))


def is_test(path):
    return path.startswith("tests" + os.sep) or os.path.basename(path).startswith("test_")


def main(argv):
    revit = None
    if "--revit" in argv:
        revit = argv[argv.index("--revit") + 1]

    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_retrieve as RETRIEVE

    said, problems = utterances()
    if problems:
        print("FRAGMENTS THAT COULD NOT BE READ  (%d) - D-48: named, never "
              "skipped in silence" % len(problems))
        for line in problems:
            print("  %s" % line)
        print("")
    if not said:
        print("No fragment declares an utterance, so there is nothing to ask.")
        return 0

    disk_ids = set(fid for fid, _ in said)
    store = SCOPE.open_scope(SCOPE.GLOBAL)
    store_ids = set(row["id"] for row in store.fragments())
    if store_ids != disk_ids:
        store.close()
        built, problems = SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        store_ids = set(row["id"] for row in store.fragments())
        print("  (store did not match this working tree; rebuilt %d%s)"
              % (built, "; %d problem(s)" % len(problems) if problems else ""))
        if store_ids != disk_ids:
            print("  the store STILL does not match this working tree. These")
            print("  shares would be over a library that is not on disk, which")
            print("  is not a measurement. Run `python brain/heron_fragment.py`.")
            store.close()
            return 2

    try:
        SEARCH.index(store)
        rows, hits = live_cache(store)

        counted = {}
        wrong = {}
        for fragment_id, phrase in said:
            # find(), not ask(), because find() is what production runs:
            # mcp/server/heron_brain.py `lookup()` calls it, and it is the only
            # one that applies the Revit version wall to the SHORT CIRCUIT too.
            # Measuring ask() would report a share no user can ever get.
            answer = RETRIEVE.find(store, phrase, revit=revit)
            counted[answer.route] = counted.get(answer.route, 0) + 1
            # A short circuit that lands on ANOTHER fragment is worse than a
            # miss: it is route 1 answering confidently and wrongly, and the
            # share above would count it as a success.
            if answer.route in DETERMINISTIC and answer.fragment_id != fragment_id:
                wrong[answer.route] = wrong.get(answer.route, 0) + 1
    finally:
        store.close()

    total = len(said)
    deterministic = sum(counted.get(r, 0) for r in DETERMINISTIC)
    thinking = sum(counted.get(r, 0) for r in THINKING)
    unmatched = sum(counted.get(r, 0) for r in NEITHER)
    unknown = dict((r, n) for r, n in counted.items()
                   if r not in DETERMINISTIC + THINKING + NEITHER)

    print("ANSWERED WITHOUT THINKING")
    print("=" * 70)
    print("docs/19 s5: steps 1 and 2 must be tried before any model is invoked.")
    print("This is how often they succeed. D-58.")
    print("")
    print("STRUCTURAL - the library asked its own declared phrasings")
    print("-" * 70)
    print("  %-34s %6d" % ("phrasings asked", total))
    for route in DETERMINISTIC + THINKING + NEITHER:
        got = counted.get(route, 0)
        print("  %-34s %6d  %5.1f%%"
              % ("route: " + route, got, 100.0 * got / total))
    for route, got in sorted(unknown.items()):
        print("  %-34s %6d  %5.1f%%   UNRECOGNISED ROUTE"
              % ("route: " + route, got, 100.0 * got / total))
    print("  " + "-" * 60)
    print("  %-34s %6d  %5.1f%%"
          % ("answered with NO model needed", deterministic,
             100.0 * deterministic / total))
    print("")
    if wrong:
        print("  BUT %d of those short circuits answered with a DIFFERENT"
              % sum(wrong.values()))
        print("  fragment than the one that declared the phrasing. A confident")
        print("  wrong answer costs more than a search, so it is named here")
        print("  rather than counted as a saving. tools/check-routing.py is")
        print("  the tool that says WHICH, and it is the one to run next.")
        print("")
    print("  This is a CEILING and not a forecast. Real requests are phrased")
    print("  worse than the phrasings a fragment writes for itself.")
    print("")

    print("LIVE - what the utterance cache actually holds")
    print("-" * 70)
    if rows is None:
        print("  the cache table does not exist in this scope yet")
    else:
        print("  %-34s %6d" % ("cached wordings", rows))
        print("  %-34s %6d" % ("times they were reused", hits))
    print("")

    callers = remember_callers()
    production = [c for c in callers if not is_test(c)]
    if not production:
        print("  ROUTE 2 CANNOT FIRE IN PRODUCTION, AND THAT IS THE FINDING.")
        print("  remember() - the only function that writes this cache - is")
        print("  called from %s"
              % (", ".join(callers) if callers else "nowhere at all"))
        print("  and from no production code. ask() does not call it, find()")
        print("  does not call it, and no MCP tool does.")
        print("")
        print("  docs/19 s6 calls this cache the one that \"pays for itself")
        print("  faster than any of the others\" and docs/19 s5 makes it step 1")
        print("  of the pipeline. It is built, tested, indexed - and empty.")
        print("")
        print("  Where remember() should be called from is a real decision, not")
        print("  an oversight to patch: caching whatever the keyword route")
        print("  ranked first makes a GUESS permanent. See docs/OPEN-QUESTIONS.md.")
    else:
        print("  remember() is called from production code (%s), so the cache"
              % ", ".join(production))
        print("  fills with use and the LIVE numbers above are real traffic.")
    print("")
    print("Not a gate; exits 0. There is no agreed target to miss, and setting")
    print("one from this run would make today's library the standard.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
