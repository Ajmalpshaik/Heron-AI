#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

r"""
Every suite that plants a knowledge store plants HERON_AUDIT too, once, for
the whole run - FRAGMENT-ISSUES row 5b-1070.

    python tests/test_audit_planted.py

Opening a store writes knowledge.refresh and knowledge.restore lines through
brain/heron_audit.py into heron_gaps.brain_audit_dir(), and a planted
HERON_KNOWLEDGE does not move them. With nothing planted that folder is the
owner's REAL brain audit - %USERPROFILE%\.heron\audit since D-135 - which is
evidence, never pruned (D-62), and read by the gaps report as requests.
Measured 2026-10-18 on the owner's laptop: tests/test_score_routing.py, run
with nothing planted, made that folder with two test lines.

THE RULE, read from each suite's source: a suite that names HERON_KNOWLEDGE
in its code - or uses the helper at all - calls tests/support/scratch_audit.py's
plant() as a statement of its own module, before any module-level statement
that names HERON_KNOWLEDGE. Planting HERON_AUDIT inside one section is NOT
enough, and that is the case the row was measured on: test_score_routing
planted it in section 5 only.

WHAT THE RULE CANNOT SEE is a suite that opens a store and never names
HERON_KNOWLEDGE. Two did, found 2026-10-18 by running every suite with
HERON_DATA standing in for the real home: test_diagnose (the shared store
itself, and its audit lines) and test_companion (audit lines). Both call
plant() now, and a suite that uses the helper is held to it like the rest.

  1  the reader of that rule, on made-up suites, so it is seen to refuse
  2  plant() does what the suites rely on - in a child, so exit is real
  3  every suite under tests/ keeps the rule

A mention inside a docstring or a message is not a name in code: the reader
looks for the string "HERON_KNOWLEDGE" itself, or a keyword of that name.
"""

import ast
import json
import os
import subprocess
import sys
import tempfile
import textwrap

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TESTS = os.path.join(ROOT, "tests")
SUPPORT = os.path.join(TESTS, "support")
BRAIN = os.path.join(ROOT, "brain")
sys.path.insert(0, SUPPORT)

import scratch_audit                                            # noqa: E402

scratch_audit.plant()

KNOWLEDGE = "HERON_KNOWLEDGE"
HELPER = "scratch_audit"

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# ---------------------------------------------------------------------------
# The rule
# ---------------------------------------------------------------------------

def _names_knowledge(node):
    """True when HERON_KNOWLEDGE is named in code anywhere under `node`."""
    for inner in ast.walk(node):
        if isinstance(inner, ast.Constant) and inner.value == KNOWLEDGE:
            return True
        if isinstance(inner, ast.keyword) and inner.arg == KNOWLEDGE:
            return True
    return False


def _is_plant(statement):
    """True for a statement that is a call of scratch_audit.plant(...)."""
    if not isinstance(statement, ast.Expr):
        return False
    call = statement.value
    return (isinstance(call, ast.Call)
            and isinstance(call.func, ast.Attribute)
            and call.func.attr == "plant"
            and isinstance(call.func.value, ast.Name)
            and call.func.value.id == HELPER)


def _uses_helper(node):
    """True when scratch_audit is imported or named anywhere under `node`."""
    for inner in ast.walk(node):
        if isinstance(inner, ast.Import) and any(
                alias.name == HELPER for alias in inner.names):
            return True
        if isinstance(inner, ast.Name) and inner.id == HELPER:
            return True
    return False


def concerned(tree):
    """True for a suite the rule applies to."""
    return _names_knowledge(tree) or _uses_helper(tree)


def verdict(source):
    """None when `source` keeps the rule or the rule does not apply to it;
    otherwise what is wrong, in one line."""
    tree = ast.parse(source)
    if not concerned(tree):
        return None
    planted = next((s.lineno for s in tree.body if _is_plant(s)), None)
    if planted is None:
        return ("%s and never calls %s.plant() at module level"
                % ("names %s" % KNOWLEDGE if _names_knowledge(tree)
                   else "imports %s" % HELPER, HELPER))
    defs = (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)
    early = next((s.lineno for s in tree.body
                  if not isinstance(s, defs) and _names_knowledge(s)), None)
    if early is not None and early < planted:
        return ("names %s at line %d, before %s.plant() at line %d"
                % (KNOWLEDGE, early, HELPER, planted))
    return None


def section_1():
    print()
    print("1. THE READER OF THE RULE REFUSES WHAT IT SHOULD")
    plant = "import scratch_audit\nscratch_audit.plant()\n"
    cases = [
        ("a mention in a docstring only is not a name in code",
         '"""Runs with HERON_KNOWLEDGE at a scratch folder."""\n', False),
        ("a store planted with no plant() is refused",
         'import os\nos.environ["HERON_KNOWLEDGE"] = "x"\n', True),
        ("a keyword HERON_KNOWLEDGE= counts as naming it",
         "import os\nenv = dict(os.environ, HERON_KNOWLEDGE='x')\n", True),
        ("plant() inside a function is refused - it covers that call only",
         "import os, scratch_audit\n"
         "def main():\n    scratch_audit.plant()\n"
         "    os.environ['HERON_KNOWLEDGE'] = 'x'\n", True),
        ("HERON_AUDIT planted by hand in one section is refused - "
         "test_score_routing's leak",
         "import os\n"
         "def section_5():\n    os.environ['HERON_AUDIT'] = 'a'\n"
         "    os.environ['HERON_KNOWLEDGE'] = 'x'\n", True),
        ("plant() after a module-level HERON_KNOWLEDGE is refused",
         "import os, scratch_audit\nos.environ['HERON_KNOWLEDGE'] = 'x'\n"
         "scratch_audit.plant()\n", True),
        ("a suite that imports the helper and plants in a function only "
         "is refused, HERON_KNOWLEDGE or not",
         "import scratch_audit\ndef main():\n    scratch_audit.plant()\n",
         True),
        ("plant() at module level, first, is kept",
         plant + "import os\nos.environ['HERON_KNOWLEDGE'] = 'x'\n", False),
        ("plant(data=True) at module level is kept",
         "import scratch_audit\nscratch_audit.plant(data=True)\n"
         "def main():\n    return {'HERON_KNOWLEDGE': 'x'}\n", False),
    ]
    for what, source, refused in cases:
        said = verdict(source)
        check((said is not None) == refused,
              "%s%s" % (what, "" if said is None else " (%s)" % said))


# ---------------------------------------------------------------------------
# The helper
# ---------------------------------------------------------------------------

CHILD = textwrap.dedent(r"""
    import json, os, sys
    sys.path[:0] = [sys.argv[1], sys.argv[2]]
    import scratch_audit, heron_gaps, heron_audit, heron_paths, heron_scope
    seen = {"data_before": heron_paths.data_home()}
    folder = scratch_audit.plant()
    seen["folder"] = folder
    seen["audit"] = heron_gaps.brain_audit_dir()
    seen["trail"] = heron_gaps.audit_dir()
    seen["wrote"] = heron_audit.record("knowledge.refresh", True,
                                       {"why": "test_audit_planted"})
    seen["file"] = heron_audit.current_file()
    seen["again"] = scratch_audit.plant()
    seen["data_unasked"] = heron_paths.data_home()
    scratch_audit.plant(data=True)
    seen["data_after"] = heron_paths.data_home()
    kept = os.environ.pop("HERON_KNOWLEDGE")
    seen["store_default"] = heron_scope.knowledge_dir()
    os.environ["HERON_KNOWLEDGE"] = kept
    if sys.argv[3] == "restore":
        scratch_audit.restore()
        seen["audit_back"] = os.environ.get("HERON_AUDIT")
        seen["data_back"] = os.environ.get("HERON_DATA")
        seen["folder_left"] = os.path.exists(folder)
    print(json.dumps(seen))
""")


def run_child(sentinel, how):
    env = dict(os.environ, HERON_DATA=sentinel, PYTHONUTF8="1",
               HERON_KNOWLEDGE=os.path.join(sentinel, "private"))
    env.pop("HERON_AUDIT", None)
    done = subprocess.run([sys.executable, "-c", CHILD, SUPPORT, BRAIN, how],
                          env=env, capture_output=True, encoding="utf-8",
                          errors="replace", timeout=120)
    lines = (done.stdout or "").strip().splitlines()
    try:
        return json.loads(lines[-1]), done
    except (IndexError, ValueError):
        return None, done


def inside(path, folder):
    path, folder = os.path.abspath(path or ""), os.path.abspath(folder)
    return path.startswith(folder.rstrip(os.sep) + os.sep)


def section_2():
    print()
    print("2. plant() PUTS THE TRAIL - AND, ASKED, THE HOME - IN A SCRATCH FOLDER")
    sentinel = tempfile.mkdtemp(prefix="heron-real-home-")
    seen, done = run_child(sentinel, "restore")
    check(seen is not None, "the child ran (exit %s)" % done.returncode)
    if seen is None:
        print(done.stdout, done.stderr)
        return
    folder = seen["folder"]
    check(seen["data_before"] == sentinel,
          "before plant() the default home is the stand-in for the real one")
    check(inside(seen["audit"], folder) and inside(seen["trail"], folder),
          "the brain's audit and the add-in's trail are both in the scratch folder")
    check(seen["wrote"] and inside(seen["file"], folder),
          "a store's audit line is written there")
    check(not os.path.exists(os.path.join(sentinel, "audit")),
          "and nothing reaches the stand-in for the real home's audit")
    check(seen["again"] == folder, "a second plant() makes no second folder")
    check(seen["data_unasked"] == sentinel,
          "HERON_DATA is left alone until data=True asks for it")
    check(inside(seen["data_after"], folder)
          and inside(seen["store_default"], folder),
          "with data=True the default home - and the store it opens - is scratch")
    check(seen["audit_back"] is None and seen["data_back"] == sentinel,
          "restore() puts both back: HERON_AUDIT unset, HERON_DATA as it was")
    check(seen["folder_left"] is False, "and removes the scratch folder")

    seen, done = run_child(sentinel, "exit")
    check(seen is not None and not os.path.exists(seen["folder"]),
          "a run that only plants leaves no folder behind once it exits")
    check(os.listdir(sentinel) == [],
          "the stand-in for the real home is still empty after both runs")
    os.rmdir(sentinel)


# ---------------------------------------------------------------------------
# The suites
# ---------------------------------------------------------------------------

def section_3():
    print()
    print("3. EVERY SUITE THAT NAMES HERON_KNOWLEDGE PLANTS HERON_AUDIT FIRST")
    held, broken = 0, []
    for name in sorted(os.listdir(TESTS)):
        if not (name.startswith("test_") and name.endswith(".py")):
            continue
        with open(os.path.join(TESTS, name), encoding="utf-8") as handle:
            source = handle.read()
        try:
            tree = ast.parse(source)
        except SyntaxError as error:
            broken.append((name, "does not parse: %s" % error))
            continue
        if not concerned(tree):
            continue
        held += 1
        said = verdict(source)
        if said:
            broken.append((name, said))
    check(held > 0, "%d suites name %s or use the helper" % (held, KNOWLEDGE))
    for name, said in broken:
        print("        %s - %s" % (name, said))
    check(not broken, "%d of them break the rule" % len(broken))


def main():
    section_1()
    section_2()
    section_3()
    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for what in FAILURES:
            print("  - " + what)
        return 1
    print("PASSED - every suite that plants a store plants its audit too.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
