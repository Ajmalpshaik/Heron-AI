# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Heron's command lines print safely on a cp1252 Windows console - rows 5b-2041
and 5b-2044. Runs without Revit.

    python tests/test_console_streams.py

A Windows console is cp1252, which has no Arabic letters, no 'Δ' and no '→'.
Before this change, each script below stopped with UnicodeEncodeError the
moment it printed one of them. Each test stands a cp1252 stream in for that
console.

WHAT IT PROVES
  1. heron_console.safe_streams() turns a cp1252 stream into one that prints
     'Δ', '→' and an Arabic word, and leaves a stream it cannot reconfigure
     (a StringIO) as it was.
  2. Row 5b-2041: heron_sheet_list.main reads a .csv whose sheet names are
     'Plan Δ level' and an Arabic word, plain and with --json, to the end.
  3. Row 5b-2042: heron_retrieve.py, run as a command with
     PYTHONIOENCODING=cp1252, shows a document that has headings - its heading
     path holds '→' - and an Arabic question, and ends with exit 0.
  4. Row 5b-2044: every main the row names makes its streams safe before
     anything else it does - read from the code, since most of them need a
     store, a model or a network to run.

WHAT IT CANNOT DO: run on the owner's real Windows console. The cp1252 stream
is that console as Python sees it; the console itself is not here.
"""

from __future__ import print_function

import ast
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BRAIN = os.path.join(ROOT, "brain")
sys.path.insert(0, BRAIN)
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))

import heron_console  # noqa: E402
import scratch_audit  # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

ARABIC = u"مخطط"          # an Arabic word
DELTA = u"Δ"

FAILURES = []

# Row 5b-2044's list, with 5b-2041 and 5b-2042's own two. Row 5b-2043's
# tools/resign-machine-proofs.py was removed by #1417, and the row withdrawn.
BRAIN_MAINS = (
    "heron_sheet_list heron_retrieve heron_gaps heron_ingest heron_ground "
    "heron_research heron_designbasis heron_commands heron_checklists "
    "heron_usage heron_audit heron_scope heron_paths heron_company heron_fire "
    "heron_hvac heron_contract heron_conflict heron_search heron_context "
    "heron_embed heron_earlier heron_agents heron_capability heron_graph "
    "heron_fragment heron_dotnet heron_instructions heron_matrix").split()
# The two engines import the standard library and their own siblings only
# (tests/test_hvac.py, tests/test_fire.py), so they reconfigure in place.
STANDALONE = ("heron_fire", "heron_hvac")
TOOL_MAINS = ("heron-ready check-revit-gate new-agent "
              "build-release-assets build-setup").split()


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def cp1252_console():
    return io.TextIOWrapper(io.BytesIO(), encoding="cp1252")


def run_on_console(call):
    """Run call() with stdout and stderr on cp1252 streams.
    Returns (exit code or 'crashed: ...', bytes printed)."""
    out, err = cp1252_console(), cp1252_console()
    saved = sys.stdout, sys.stderr
    sys.stdout, sys.stderr = out, err
    try:
        code = call()
        out.flush()
        err.flush()
        shown = out.buffer.getvalue() + err.buffer.getvalue()
    except UnicodeEncodeError as error:
        code, shown = "crashed: %s" % error, b""
    finally:
        sys.stdout, sys.stderr = saved
    return code, shown


def test_the_helper():
    print("safe_streams makes a cp1252 stream print what cp1252 has not got")
    def call():
        heron_console.safe_streams()
        print(u"%s offset %s → %s" % (DELTA, ARABIC, "the end"))
        return 0
    code, shown = run_on_console(call)
    check(code == 0, "printing ran to the end (got %r)" % (code,))
    check(u"→".encode("utf-8") in shown and ARABIC.encode("utf-8") in shown,
          "the arrow and the Arabic word were printed, not dropped")
    kept = io.StringIO()
    saved = sys.stdout
    sys.stdout = kept
    try:
        heron_console.safe_streams()
        print(DELTA)
    finally:
        sys.stdout = saved
    check(kept.getvalue() == DELTA + "\n", "a StringIO is left as it is")


def test_sheet_list():
    print("row 5b-2041: the sheet list prints Arabic and Δ sheet names")
    import heron_sheet_list as SHEETS
    folder = tempfile.mkdtemp()
    try:
        path = os.path.join(folder, "register.csv")
        with io.open(path, "w", encoding="utf-8", newline="") as fh:
            fh.write(u"Sheet Number,Sheet Name\nM-101,Plan %s level\nM-102,%s\n"
                     % (DELTA, ARABIC))
        for form in ([path], [path, "--json"]):
            code, shown = run_on_console(lambda: SHEETS.main(form))
            check(code in (0, None), "%s ran to the end (got %r)"
                  % (" ".join(["main"] + form[1:]), code))
            check(ARABIC.encode("utf-8") in shown,
                  "%s printed the Arabic sheet name" % " ".join(["main"] + form[1:]))
    finally:
        shutil.rmtree(folder, ignore_errors=True)


def test_retrieve():
    print("row 5b-2042: retrieval shows a document with headings on cp1252")
    folder = tempfile.mkdtemp()
    try:
        knowledge = os.path.join(folder, "knowledge")
        os.makedirs(knowledge)
        doc = os.path.join(folder, "insulation.md")
        with io.open(doc, "w", encoding="utf-8") as fh:
            fh.write(u"# Ductwork\n\n## Insulation\n\n### Thickness\n\n"
                     u"Duct insulation thickness is 25 mm for supply air.\n")
        env = dict(os.environ)
        env["HERON_KNOWLEDGE"] = knowledge
        env["PYTHONIOENCODING"] = "cp1252"
        env.pop("PYTHONUTF8", None)
        # An empty store is refused before anything is shown, so the cards
        # go in first, as `heron_retrieve.py` itself says to.
        rebuilt = subprocess.run(
            [sys.executable, os.path.join(BRAIN, "heron_scope.py"), "--rebuild"],
            env=env, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=600)
        check(rebuilt.returncode == 0, "the store was rebuilt (exit %d)"
              % rebuilt.returncode)
        ingest = subprocess.run(
            [sys.executable, os.path.join(BRAIN, "heron_ingest.py"), doc],
            env=env, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=600)
        check(ingest.returncode == 0, "the document was ingested (exit %d)%s"
              % (ingest.returncode, "" if ingest.returncode == 0 else
                 ": " + ingest.stderr.decode("utf-8", "replace")[-300:]))
        found = subprocess.run(
            [sys.executable, os.path.join(BRAIN, "heron_retrieve.py"),
             u"duct insulation thickness %s" % ARABIC],
            env=env, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=600)
        text = (found.stdout + found.stderr).decode("utf-8", "replace")
        check(found.returncode == 0 and "UnicodeEncodeError" not in text,
              "retrieval ran to the end (exit %d)%s" % (
                  found.returncode, "" if found.returncode == 0 else ": " + text[-300:]))
        check(u"→" in text, "the heading path's arrow was printed")
    finally:
        shutil.rmtree(folder, ignore_errors=True)


def first_statements(path):
    """The source of main's first statement after any docstring."""
    tree = ast.parse(io.open(path, encoding="utf-8").read())
    for node in tree.body:
        if isinstance(node, ast.FunctionDef) and node.name == "main":
            body = node.body
            if (isinstance(body[0], ast.Expr) and isinstance(body[0].value, ast.Constant)
                    and isinstance(body[0].value.value, str)):
                body = body[1:]
            return [ast.dump(statement) for statement in body[:2]]
    return None


def test_every_main_named():
    print("row 5b-2044: every main named makes its streams safe first")
    for module in BRAIN_MAINS:
        found = first_statements(os.path.join(BRAIN, module + ".py"))
        if module in STANDALONE:
            check(found is not None and "reconfigure" in found[0],
                  "brain/%s.py reconfigures its streams first" % module)
            continue
        check(found is not None and "heron_console" in found[0]
              and "safe_streams" in found[1],
              "brain/%s.py calls heron_console.safe_streams() first" % module)
    for script in TOOL_MAINS:
        found = first_statements(os.path.join(ROOT, "tools", script + ".py"))
        check(found is not None and "reconfigure" in found[0],
              "tools/%s.py reconfigures its streams first" % script)


def main():
    heron_console.safe_streams()      # this suite prints a 'Δ' of its own
    print(__doc__.strip().splitlines()[0])
    for test in (test_the_helper, test_sheet_list, test_retrieve,
                 test_every_main_named):
        test()
    print("")
    if FAILURES:
        print("%d FAILED" % len(FAILURES))
        return 1
    print("All passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
