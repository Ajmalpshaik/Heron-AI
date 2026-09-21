#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 2, the half a machine with no Revit can answer.

READ THIS BEFORE READING THE RESULT
-----------------------------------
STAGE 2 IS NOT PROVED BY THIS FILE AND CANNOT BE. Its question is whether two
Heron tabs appear in a real Revit ribbon, and whether the Heron tab is built
correctly by the tools alone, the AI Bridge alone, and both together. Only a
Windows machine with Revit on it answers that, with a screenshot of each of
the three combinations.

    docs/work-notes/plans/plugin-extension/02-implementation.md, Stage 2

WHAT THIS DOES ANSWER
---------------------
Everything about the three pieces that is decided in SOURCE, before Revit is
involved - and every one of these is a way the stage could fail on the day
with nobody able to say why:

    the two Heron pieces disagree about the tab name by one character
        -> TWO tabs both called Heron, which looks almost right on screen
    a piece assumes it loaded first
        -> CreateRibbonTab throws, the tab does not appear, no message
    a piece assumes it loaded second
        -> nothing creates the tab, so tools-only draws nothing. R-34
    two products share an AddInId
        -> Revit refuses both. D-88
    a <ManifestSettings> element
        -> CRASHES Revit 2025 and older

THE TAB NAME IS REPEATED IN TWO ASSEMBLIES ON PURPOSE (see
HeronToolsApplication.cs) and this file is what stops that costing anything:
the two literals are read out of the two files and compared.

    python tests/test_ribbon_tab_sharing.py
"""

import io
import os
import re
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

BRIDGE = os.path.join("revit", "Heron.Revit.Addin", "HeronApplication.cs")
TOOLS = os.path.join("revit", "Heron.Tools", "HeronToolsApplication.cs")
DOC = os.path.join("revit", "Heron.Doc", "HeronDocApplication.cs")

MANIFESTS = [
    os.path.join("revit", "Heron.Revit.Addin", "Heron.addin"),
    os.path.join("revit", "Heron.Tools", "Heron.Tools.addin"),
    os.path.join("revit", "Heron.Doc", "Heron.Doc.addin"),
]

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(rel):
    try:
        return io.open(os.path.join(ROOT, rel), encoding="utf-8-sig").read()
    except (IOError, OSError):
        return None


def constant(text, name):
    """The value of `private const string <name> = "...";`."""
    if text is None:
        return None
    m = re.search(r'const\s+string\s+%s\s*=\s*"([^"]*)"' % re.escape(name), text)
    return m.group(1) if m else None


def creates_tab_safely(text):
    """
    try { ...CreateRibbonTab(...); } catch ( <vendor>.Exceptions.ArgumentException )

    THE EXCEPTION MUST BE THE QUALIFIED ONE, and that is the whole check.
    The vendor's own ArgumentException lives under an `Exceptions` namespace;
    `System.ArgumentException` is a DIFFERENT TYPE with no such segment, and
    catching that one would catch nothing Revit ever throws - so the tab would
    still fail to appear, with the code looking as if it had been handled.

    THE VENDOR NAMESPACE IS MATCHED, NEVER SPELLED. tools/check-structure.py
    greps file text for it and fails any file outside revit/ that names it,
    comments included - the adapter boundary, docs/16 section 4. An earlier
    version of this file spelled it out three times and broke that gate. The
    pattern below is not a way around the rule: requiring a dotted
    `.Exceptions.ArgumentException` is exactly the claim being made, and it
    still rejects the bare System one.
    """
    if text is None:
        return False
    return re.search(
        r"try\s*\{[^}]*CreateRibbonTab[^}]*\}\s*"
        r"catch\s*\(\s*[A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)*"
        r"\.Exceptions\.ArgumentException\s*\)",
        text, re.S) is not None


def element(root, tag):
    node = root.find(".//" + tag)
    return None if node is None or node.text is None else node.text.strip()


def main():
    bridge, tools, doc = read(BRIDGE), read(TOOLS), read(DOC)

    print("The three ribbon builders exist")
    check(bridge is not None, "%s" % BRIDGE)
    check(tools is not None, "%s" % TOOLS)
    check(doc is not None, "%s" % DOC)
    if bridge is None or tools is None or doc is None:
        print("\nFAILED - a source file is missing, so nothing below means anything.")
        return 1

    print("\nONE tab, built by two pieces - S3 / D-89 / R-35")
    bridge_tab = constant(bridge, "TabName")
    tools_tab = constant(tools, "TabName")
    doc_tab = constant(doc, "TabName")

    check(bridge_tab == "Heron", "the AI Bridge builds the tab %r" % bridge_tab)
    check(tools_tab == "Heron", "the tools build the tab %r" % tools_tab)
    check(bridge_tab is not None and bridge_tab == tools_tab,
          "and the two strings are IDENTICAL - %r vs %r. One character apart "
          "is two tabs with the same name" % (bridge_tab, tools_tab))

    print("\nNeither piece may assume it is first - Revit chooses the order")
    check(creates_tab_safely(bridge),
          "the AI Bridge creates the tab inside try/catch, on the vendor's "
          "qualified Exceptions.ArgumentException and not the bare System one")
    check(creates_tab_safely(tools),
          "the tools create the tab inside try/catch - this is the one that "
          "makes TOOLS-ONLY work, and R-34 promises it")
    check("CreateRibbonPanel" in tools,
          "and the tools add their panel after it, so the panel lands on the "
          "tab whether they made it or joined it")

    print("\nTwo panels have to fit on that one tab")
    bridge_panel = constant(bridge, "PanelName")
    tools_panel = constant(tools, "PanelName")
    check(bridge_panel == "AI Bridge", "the AI Bridge panel is %r" % bridge_panel)
    check(tools_panel is not None and tools_panel != bridge_panel,
          "the tools panel is %r, which is a DIFFERENT name" % tools_panel)

    print("\nHeron Doc is a SECOND tab, not a second piece of the first")
    check(doc_tab == "Heron Doc", "its tab is %r" % doc_tab)
    check(doc_tab != bridge_tab,
          "and it is not the Heron tab - this project proves the other half "
          "of the stage")
    check(creates_tab_safely(doc),
          "it still creates its tab inside try/catch, so the day a second "
          "Heron Doc piece arrives the assumption is not already made")

    print("\nThree manifests, three GUIDs - D-88")
    guids, assemblies, classes = {}, {}, {}
    for rel in MANIFESTS:
        text = read(rel)
        if text is None:
            check(False, "%s is missing" % rel)
            continue
        try:
            root = ET.fromstring(text)
        except ET.ParseError:
            check(False, "%s does not parse as XML" % rel)
            continue

        name = os.path.basename(rel)
        guid = element(root, "AddInId")
        assembly = element(root, "Assembly")
        full = element(root, "FullClassName")

        check(guid is not None and guid not in guids,
              "%s: AddInId %s is not claimed by %s"
              % (name, guid, guids.get(guid, "anything else")))
        check(assembly is not None and assembly not in assemblies,
              "%s: Assembly %s is its own" % (name, assembly))
        check(full is not None and full not in classes,
              "%s: FullClassName %s is its own" % (name, full))

        # Revit 2026 added a settings element that CRASHES 2025 and older,
        # and Heron ships one manifest to all eight releases.
        #
        # CHECKED ON THE PARSED TREE, not on the text. An earlier version of
        # this searched the raw file and failed both new manifests for the
        # COMMENT in them explaining why the element is absent - the test's
        # claim is about an element, so it has to look at elements.
        # tools/check-package.py greps the text instead, and stays that way
        # deliberately: it is the stricter of the two, and the manifests are
        # worded so both pass.
        settings = [e.tag for e in root.iter() if "ManifestSettings" in e.tag]
        check(not settings,
              "%s: carries no settings element Revit 2025 would crash on"
              % name)

        if guid:
            guids[guid] = name
        if assembly:
            assemblies[assembly] = name
        if full:
            classes[full] = name

    check(len(guids) == 3, "three distinct AddInIds across three manifests")

    print("\nThe entry class each manifest names actually exists")
    for full, manifest in sorted(classes.items()):
        cls = full.rsplit(".", 1)[-1]
        found = any(re.search(r"class\s+%s\b" % re.escape(cls), t or "")
                    for t in (bridge, tools, doc))
        check(found, "%s -> class %s is in revit/" % (manifest, cls))

    print("\nNo unsupported Autodesk internals anywhere - R-43b / D-95")
    # Hiding a whole tab needs AdWindows.dll, which Autodesk does not support.
    # Heron promises 2020 to 2027 and beyond, so it takes no dependency on an
    # internal API that can change in any release. The UI namespace named
    # below is the unsupported one and is NOT the add-in API, so
    # check-structure.py does not object to it appearing here.
    hits = []
    for dirpath, dirnames, filenames in os.walk(os.path.join(ROOT, "revit")):
        dirnames[:] = [d for d in dirnames if d not in ("bin", "obj")]
        for fn in filenames:
            if not fn.endswith((".cs", ".csproj")):
                continue
            body = read(os.path.join(dirpath, fn)) or ""
            if re.search(r"Autodesk\.Windows|AdWindows", body):
                hits.append(os.path.relpath(os.path.join(dirpath, fn), ROOT))
    check(not hits, "nothing under revit/ touches Autodesk.Windows or "
                    "AdWindows (found: %s)" % (", ".join(hits) or "none"))

    print("")
    if FAILURES:
        print("FAILED (%d)" % len(FAILURES))
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("Everything Stage 2 decides IN SOURCE holds. The two Heron pieces")
    print("name the same tab, neither assumes it loaded first, the panels")
    print("differ, Heron Doc is its own tab, and the three manifests carry")
    print("three different GUIDs.")
    print("")
    print("STAGE 2 IS STILL UNPROVEN. This is a text check. Nothing here has")
    print("seen a ribbon. The stage is done when a real Revit has shown all")
    print("THREE combinations - AI Bridge only, tools only, both - with a")
    print("screenshot recorded for each, and the second tab appearing and")
    print("disappearing with its two files.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
