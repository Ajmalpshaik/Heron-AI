# Heron-Agent:  HERON-RAG-LIB-001
# Heron-Step:   8
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 8 - one knowledge store per scope, as one file each.

    python tests/test_scope_store.py

Runs anywhere. No Revit, no Windows - SQLite is in the standard library.

WHAT IT PROVES
  1. A cross-scope query is impossible to WRITE, not merely absent.
  2. Deleting one project's store leaves every other scope untouched.
  3. The stores are DERIVED - delete them all and a rebuild puts them back.
  4. The scope is resolved from facts, and an unidentified project is REFUSED
     rather than guessed at.

WHAT IT DOES NOT PROVE. That any of this is wired to a real Revit document. The
resolver is handed the same facts the bridge would report; nothing here has
asked a live model what is open.
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


# Declared cases are required for a fragment to validate, since 2026-09-07. The
# fixture carries them rather than being exempted: a fixture that skips a rule is
# a fixture that has stopped testing it.
CASES = (
    u"positive:\n"
    u"  - given: two ducts in the set\n"
    u"    expect: both reported, named\n"
    u"negative:\n"
    u"  - given: a set containing none of what it reports\n"
    u"    expect: zero reported, in words, and no error\n"
)


def write_valid_fragment(folder):
    """A fragment that passes validation, written at `folder`.

    Built, not copied - see the note at the call site. Kept minimal on purpose:
    every key here is one heron_fragment.validate() actually requires, so if that
    gate gains a rule this fixture fails and says so, rather than drifting.
    """
    import yaml

    data = {
        "heron-agent": "HERON-RAG-LIB-001",
        "heron-step": 8,
        "heron-status": "DRAFT",
        "heron-since": "0.1.0",
        "heron-layer": "brain",
        "id": "FRG-ELE-001",
        "semantic-identity": "a test fragment",
        "kind": "filter",
        "domain": "test",
        "capability": "DO_A_TEST_THING",
        "version": 1,
        "source": "OFFICIAL",
        "risk": "READ",
        "purpose": "Exists to be indexed.",
        "contract": {
            "needs": [{"name": "doc", "type": "Document"}],
            "provides": [{"name": "elements", "type": "IList<Element>",
                          "role": "result"}],
        },
        "revit": ["2020", "2024"],
        "runtime": ["net472", "net48"],
        "utterances": ["do the test thing"],
    }

    os.makedirs(os.path.join(folder, "impl", "any"))
    os.makedirs(os.path.join(folder, "tests"))
    io.open(os.path.join(folder, "fragment.yaml"), "w", encoding="utf-8").write(
        yaml.safe_dump(data, default_flow_style=False, sort_keys=False))
    io.open(os.path.join(folder, "impl", "any", "fragment.cs"), "w",
            encoding="utf-8").write(u"// code\n")
    io.open(os.path.join(folder, "tests", "cases.yaml"), "w",
            encoding="utf-8").write(CASES)
    return folder


def main():
    home = tempfile.mkdtemp(prefix="heron-kn-")
    os.environ["HERON_KNOWLEDGE"] = home

    import heron_scope as S
    import heron_fragment as F

    try:
        print("1. A cross-scope query cannot be written")
        store = S.open_scope(S.GLOBAL)
        try:
            refused = False
            try:
                store.execute("ATTACH DATABASE 'other.db' AS other")
            except S.CrossScopeRefused as exc:
                refused = True
                reason = str(exc)
            check(refused, "ATTACH is refused, by name")
            check("Golden Rule 5" in reason,
                  "and the refusal says which rule it is protecting")

            refused_lower = False
            try:
                store.execute("attach database 'x.db' as x")
            except S.CrossScopeRefused:
                refused_lower = True
            check(refused_lower, "lower case does not slip past it")

            detached = False
            try:
                store.execute("DETACH DATABASE other")
            except S.CrossScopeRefused:
                detached = True
            check(detached, "so does DETACH - the other half of the same door")

            ok_query = store.execute(
                "SELECT COUNT(*) AS n FROM fragments").fetchone()["n"]
            check(ok_query == 0, "an ordinary query still works")
        finally:
            store.close()

        check(not hasattr(S, "open_scopes"),
              "there is no open_scopes() - the API cannot express two at once")

        print()
        print("2. The scope is resolved from facts, never guessed")
        scope, key = S.resolve(wanted=S.GLOBAL)
        check((scope, key) == (S.GLOBAL, None), "a non-project scope needs no key")

        refused = False
        try:
            S.resolve(document={}, wanted=S.PROJECT)
        except ValueError as exc:
            refused = True
            why = str(exc)
        check(refused, "an unidentified project is REFUSED, not defaulted")
        check("does not guess" in why,
              "and the message says why: a wrong guess is a contractual breach")

        scope, key = S.resolve(
            document={"project_key": "abcd-1234", "project_name": "Tower A"},
            wanted=S.PROJECT)
        check((scope, key) == (S.PROJECT, "abcd-1234"),
              "given a key, it resolves to that project")
        check(S.read_labels().get("abcd-1234") == "Tower A",
              "the human label is remembered separately from the key")

        print()
        print("  ..the document NAME is never the key")
        _s, k1 = S.resolve(document={"project_key": "same-key",
                                     "project_name": "Project1"}, wanted=S.PROJECT)
        _s, k2 = S.resolve(document={"project_key": "same-key",
                                     "project_name": "Renamed Later"},
                           wanted=S.PROJECT)
        check(k1 == k2, "renaming the model does not move its knowledge")

        print()
        print("3. One project's store is its own file")
        a = S.open_scope(S.PROJECT, "client-a")
        b = S.open_scope(S.PROJECT, "client-b")
        g = S.open_scope(S.GLOBAL)
        try:
            check(a.path != b.path, "two projects, two files")
            check(os.path.dirname(a.path) == os.path.dirname(b.path),
                  "both under projects/, so a human can find them")
            a.execute("INSERT INTO meta (key, value) VALUES ('secret', 'A only')")
            a.db.commit()
            found = b.execute(
                "SELECT value FROM meta WHERE key = 'secret'").fetchone()
            check(found is None,
                  "what is written to client A is not visible from client B")
        finally:
            a.close(); b.close(); g.close()

        print()
        print("  ..deleting one project takes nothing else with it")
        os.remove(S.scope_path(S.PROJECT, "client-a"))
        check(not os.path.exists(S.scope_path(S.PROJECT, "client-a")),
              "client A's store is gone")
        check(os.path.exists(S.scope_path(S.PROJECT, "client-b")),
              "client B's is untouched")
        check(os.path.exists(S.scope_path(S.GLOBAL)),
              "and so is the global scope")

        print()
        print("4. The stores are DERIVED - deleting them all loses nothing")
        count, problems = S.rebuild()
        check(count >= 2, "a rebuild indexes the fragments on disk (%d)" % count)

        shutil.rmtree(home)
        check(not os.path.exists(home), "every knowledge file deleted")

        again, _ = S.rebuild()
        check(again == count,
              "and a rebuild puts back exactly what was there (%d)" % again)

        store = S.open_scope(S.GLOBAL)
        try:
            rows = store.fragments()
            ids = sorted(r["id"] for r in rows)
            check("FRG-ELE-001" in ids,
                  "the indexed rows are the real fragments: %s" % ", ".join(ids))
            check(all(r["status"] for r in rows),
                  "each row carries its lifecycle status for later filtering")
        finally:
            store.close()

        print()
        print("An invalid fragment is not indexed")
        # THE LIBRARY IS NOT SCRATCH, AND THIS USED TO TREAT IT AS SCRATCH. The
        # broken fragment was written straight into brain/fragments/ as
        # zz-broken-temp/ and deleted afterwards - so a test wrote into library
        # source, and an interrupted run left it sitting there. The earlier fix
        # moved the makedirs inside the try with exist_ok so the NEXT run could
        # clear it, which made the mess survivable rather than stopping it.
        #
        # A temp library holds exactly the same proof and cannot outlive the run.
        # rebuild() calls heron_fragment.load_all() with no root, and load_all
        # resolves `root or FRAGMENTS_DIR` at CALL time, so pointing that one
        # module global at the copy redirects it - restored in the finally.
        #
        # The good fragment is BUILT rather than copied from the library. A copy
        # was tried first and does not work: every PROVEN fragment's proof is
        # fingerprinted against its own bytes and path, so copying one out of
        # brain/fragments/ makes it fail validation and the temp library indexes
        # NOTHING. Copying a DRAFT one instead would work today and rot quietly
        # the day that fragment is promoted - a built one cannot be reached by
        # the library changing under it.
        work = tempfile.mkdtemp(prefix="heron-broken-")
        was_fragments_dir = F.FRAGMENTS_DIR
        try:
            write_valid_fragment(os.path.join(work, "do-a-test-thing"))
            F.FRAGMENTS_DIR = work
            before, _ = S.rebuild()

            broken_dir = os.path.join(work, "zz-broken")
            os.makedirs(os.path.join(broken_dir, "impl", "any"))
            io.open(os.path.join(broken_dir, "fragment.yaml"), "w",
                    encoding="utf-8").write(u"id: FRG-ELE-999\nkind: nonsense\n")
            after, _ = S.rebuild()

            check(after == before,
                  "a malformed fragment is skipped, not indexed as if it were fine")
            # WITHOUT THIS THE CHECK ABOVE PASSES ON 0 == 0. It compared two
            # counts and never asked whether either was a real one, so a library
            # that indexed nothing at all satisfied it. That is not theoretical:
            # the first version of this rewrite copied a PROVEN fragment, indexed
            # 0, and the comparison above still said ok.
            check(before == 1,
                  "and it is compared against a real count rather than 0 (%d)"
                  % before)
        finally:
            F.FRAGMENTS_DIR = was_fragments_dir
            shutil.rmtree(work, ignore_errors=True)

        print()
        print("Unknown scopes are an error, never a guess")
        bad = False
        try:
            S.scope_path("clients")
        except ValueError:
            bad = True
        check(bad, "'clients' is not a scope and is refused")

    finally:
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - one file per scope, a cross-scope query that cannot be")
    print("written, and stores that are derived rather than precious.")
    print("Nothing here has spoken to a real Revit document: the resolver was")
    print("handed the facts the bridge would report, not asked for them.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
