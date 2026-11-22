# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-FRG-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.3.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Revit tool catalogue a modeller reads - tools/generate-tool-catalogue.py.

    python tests/test_tool_catalogue.py

WHY A GENERATOR GETS A TEST HERE
--------------------------------
test_catalog.py says it: a generator that only draws needs none, one that
CONCLUDES does. This one concludes, for every tool, whether a modeller can use
it in a chat today - and a wrong answer there sends somebody to a tool that is
refused, or hides one that works. And its pages are COMMITTED, so they can go
stale; this test is the gate that says so.

WHAT IT PROVES
  1. The seven risk words are the runtime's seven, in the runtime's order.
  2. A PROVEN card whose proof stands is usable and gets steps; one that
     changes the model asks for Changes; ADMIN asks for Admin as well.
  3. A DRAFT that changes the model is refused by name (D-117) and gets NO
     steps - a list of steps would be a way to try it.
  4. A DRAFT that only reads runs, and says it is not proved.
  5. A PROVEN card whose proof went stale is refused like a DRAFT.
  6. A retired card is counted, never listed.
  7. A need filled from the selection says so, and asks the modeller to select.
  8. No proof's model name reaches a page - it can be a client's job.
  9. The ribbon is read out of the add-in's code: every button it builds is
     on the page, under the panel the code names.
 10. The pages are the same twice running, with "\\n" line ends.
 11. --check finds a page edited by hand and a page no job wants.
 12. THE COMMITTED PAGES ARE CURRENT - the check that fails a change that
     moved a card and did not run the tool.

WHAT IT DOES NOT PROVE. That the pages read well. A person judges that.
"""

import importlib.util
import io
import os
import re
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


def load_tool():
    path = os.path.join(ROOT, "tools", "generate-tool-catalogue.py")
    spec = importlib.util.spec_from_file_location("tool_catalogue", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class Card(object):
    """The parts of heron_fragment.Fragment the generator reads, and no more."""

    def __init__(self, capability, status, risk, domain="revit.mep", stale=False,
                 needs=None, model="SECRET CLIENT JOB 9999 - Revit 2024"):
        self.status = status
        self.data = {"capability": capability, "risk": risk, "domain": domain,
                     "purpose": "First paragraph.\n\nSecond paragraph."}
        self._stale = stale
        self.semantic_identity = "does the %s job" % capability.lower()
        self.supported = ["2020", "2024"]
        self.proof = None if status == "DRAFT" else {
            "date": "2026-11-01", "by": "Ajmal PS", "model": model,
            "positive_case": "x" * 20, "negative_case": "y" * 20,
            "second_route": "none", "fingerprint": "abc"}
        self._needs = needs or [{"name": "doc", "type": "Document"}]

    def proof_is_stale(self):
        return self._stale

    def fingerprint(self):
        return "abc"

    def needs(self):
        return list(self._needs)

    def provides(self):
        return [{"name": "itemCount", "role": "result", "type": "int"},
                {"name": "refused", "role": "accounting", "type": "string"}]

    def utterances(self):
        return ["do the %s job" % self.data["capability"].lower()]


def fixture_library():
    selection = [{"name": "doc", "type": "Document"},
                 {"name": "elements", "type": "IList<Element>"},
                 {"name": "offset", "type": "double", "source": "request",
                  "optional": True, "absent": "0"}]
    cards = [
        Card("PROVEN_READ", "PROVEN", "READ"),
        Card("PROVEN_CHANGE", "PROVEN", "MODIFY", needs=selection),
        Card("PROVEN_ADMIN", "PROVEN", "ADMIN"),
        Card("DRAFT_CHANGE", "DRAFT", "MODIFY"),
        Card("DRAFT_READ", "DRAFT", "READ"),
        Card("STALE_CHANGE", "PROVEN", "MODIFY", stale=True),
        Card("OLD_ONE", "DEPRECATED", "READ"),
    ]
    return dict((c.data["capability"], c) for c in cards)


def section(page, capability):
    """One tool's entry on a page, from its anchor to the next one."""
    start = page.index('<a id="%s"></a>' % capability.lower().replace("_", "-"))
    rest = page.find("<a id=", start + 1)
    return page[start:rest if rest > 0 else len(page)]


def main():
    tool = load_tool()

    print("\n1. The risk words are the runtime's")
    with io.open(os.path.join(ROOT, "mcp", "server", "heron_runtime.py"), encoding="utf-8") as fh:
        order = re.search(r"^RISK_ORDER = \[([^\]]*)\]", fh.read(), re.M)
    runtime = re.findall(r'"([A-Z]+)"', order.group(1)) if order else []
    check(runtime and list(tool.RISK_WORDS) == runtime,
          "RISK_WORDS is RISK_ORDER, same order: %s" % runtime)

    print("\n2-8. What each card is called")
    groups, retired = tool.collect(fixture_library())
    entries = groups["mep"][1]
    page = tool.render_group("mep", groups["mep"][0], entries)
    by = dict((e["capability"], e) for e in entries)

    s = section(page, "PROVEN_READ")
    check("Usable in a chat" in s and "How to use it" in s, "PROVEN read: usable, with steps")
    check("Turn **" not in s, "PROVEN read: asks for no switch")
    s = section(page, "PROVEN_CHANGE")
    check("Turn **Changes** on" in s and "Undo list" in s, "PROVEN change: asks for Changes")
    check("Select what it works on in Revit (elements)" in s and "| **Works on** |" in s,
          "a need filled from the selection asks the modeller to select")
    check("**offset** - a number" in s and "left out means *0*" in s,
          "a typed need shows its type and what leaving it out means")
    s = section(page, "PROVEN_ADMIN")
    check("Turn **Changes and Admin** on" in s, "ADMIN asks for Admin as well")
    s = section(page, "DRAFT_CHANGE")
    check("refused until proved (D-117)" in s and "How to use it" not in s,
          "DRAFT change: refused by name, no steps")
    check("Once it is proved, you will ask" in s, "DRAFT change: its words are still shown")
    s = section(page, "DRAFT_READ")
    check(by["DRAFT_READ"]["usable"] and "runs in a chat" in s and "How to use it" in s,
          "DRAFT read: runs, and says it is not proved")
    s = section(page, "STALE_CHANGE")
    check("PROOF OUT OF DATE" in s and "How to use it" not in s,
          "PROVEN with a stale proof: refused like a DRAFT")
    check(retired == 1 and "OLD_ONE" not in page, "a retired card is counted, not listed")
    check("SECRET CLIENT JOB" not in page, "no proof's model name reaches the page")
    check("Proved on a real model on 2026-11-01 (Revit 2024)" in section(page, "PROVEN_READ"),
          "the proof's date and release are given")

    print("\n9. The ribbon, from the add-in's code")
    rows = tool.ribbons(ROOT)
    main_row = [r for r in rows if r["source"].endswith("HeronApplication.cs")]
    check(len(main_row) == 1, "the AI Bridge add-in's ribbon was read")
    if main_row:
        r = main_row[0]
        labels = [i["label"] for i in r["items"]] + \
            [m["label"] for i in r["items"] for m in i["menu"]]
        with io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronApplication.cs"),
                     encoding="utf-8-sig") as fh:
            built = len(re.findall(r"new PushButtonData\(", fh.read()))
        menu_rows = sum(len(i["menu"]) for i in r["items"])
        plain = sum(1 for i in r["items"] if not i["menu"])
        check(menu_rows + plain == built,
              "every button the code builds is placed: %d of %d" % (menu_rows + plain, built))
        check(r["tab"] == "Heron" and r["panel"] and "Settings" in labels and "Heron" in labels,
              "tab %r, panel %r, buttons %s" % (r["tab"], r["panel"], labels))
        check(r["state"] == "SHIPPED", "the AI Bridge is the shipped product")
        front = tool.render_front(groups, retired, rows)
        check(all(("**%s**" % l in front) or ("› %s" % l in front) for l in labels),
              "every button is on the front page")

    print("\n10. The same twice, with \\n line ends")
    pages_a = tool.build()
    pages_b = tool.build()
    check(pages_a == pages_b, "two runs give the same pages")
    check(not any("\r" in t for t in pages_a.values()), "no \\r in any page")

    print("\n11. --check finds what is wrong")
    scratch = tempfile.mkdtemp(prefix="heron-catalogue-")
    try:
        tool.write(pages_a, root=scratch)
        check(tool.stale_pages(pages_a, root=scratch) == [], "freshly written pages are current")
        first = sorted(p for p in pages_a if "/tool-catalogue/" in p)[0]
        with io.open(os.path.join(scratch, first), "a", encoding="utf-8") as fh:
            fh.write("typed by hand\n")
        orphan = os.path.join(scratch, tool.PAGES, "nobody.md")
        with io.open(orphan, "w", encoding="utf-8") as fh:
            fh.write("x\n")
        wrong = tool.stale_pages(pages_a, root=scratch)
        check(first in wrong and any("nobody.md" in w for w in wrong),
              "an edited page and an orphan are both named: %s" % wrong)
    finally:
        shutil.rmtree(scratch, ignore_errors=True)

    print("\n12. The committed pages are current")
    wrong = tool.stale_pages(pages_a)
    check(wrong == [], "nothing out of date%s" % (
        "" if not wrong else " - run python tools/generate-tool-catalogue.py: %s" % wrong))

    print("")
    if FAILURES:
        print("%d FAILED" % len(FAILURES))
        return 1
    print("All passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
