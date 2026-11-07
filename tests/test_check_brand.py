#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Does the brand check see the colours it says it sees, and only those?

    python tests/test_check_brand.py

`tools/check-brand.py` names every colour Heron shows that the palette in
docs/brand/README.md does not have (D-149). EVERY CASE BUILDS ITS OWN
REPOSITORY in a temp folder and points the tool at it, so the suite asserts
what the check does, not how many colours today's tree still owes.

Exits 3, NOT RUN, when git is not on this machine: the tool cannot run without
it, so nothing here could prove anything either way.
"""

import contextlib
import importlib.util
import io
import os
import shutil
import stat
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-brand.py")

FAILURES = []
MADE = []

PALETTE = """# Brand

## The colours

| Name | Hex | From | Where |
|---|---|---|---|
| **Ink** | `#0F0F10` | sheet | text |
| **Accent** | `#FD641A` | sheet | highlight |

## The font
"""


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    try:
        spec = importlib.util.spec_from_file_location("check_brand", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except Exception:                                          # noqa: BLE001 - reported as FAILED
        return None


def have_git():
    try:
        subprocess.run(["git", "--version"], capture_output=True, check=True)
        return True
    except (OSError, subprocess.CalledProcessError):
        return False


def _writable(func, path, _info):
    os.chmod(path, stat.S_IWRITE)
    func(path)


def repo(files, palette=PALETTE):
    root = tempfile.mkdtemp(prefix="heron-brand-")
    MADE.append(root)
    every = dict(files)
    if palette is not None:
        every["docs/brand/README.md"] = palette
    for name, text in every.items():
        path = os.path.join(root, *name.split("/"))
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as handle:
            handle.write(text)
    subprocess.run(["git", "init", "-q"], cwd=root, check=True)
    subprocess.run(["git", "add", "-A"], cwd=root, check=True)
    return root


def scan(tool, root, *argv):
    tool.ROOT = root
    old = sys.argv
    sys.argv = ["check-brand.py"] + list(argv)
    buffer = io.StringIO()
    try:
        with contextlib.redirect_stdout(buffer):
            code = tool.main()
    finally:
        sys.argv = old
    return code, buffer.getvalue()


def main():
    if not have_git():
        print("NOT RUN  git is not on this machine, and the check cannot run without it")
        return 3
    tool = load()
    if tool is None:
        print("FAILED  tools/check-brand.py could not be loaded")
        return 1

    print("1. Colours in the palette, at any opacity and in any notation, pass")
    root = repo({
        "mcp/page.css": ".a{color:#0f0f10} .b{background:#FD641A80} .c{color:rgba(253, 100, 26, .5)}\n",
        "revit/Win.cs": "var a = Color.FromRgb(0x0F, 0x0F, 0x10);\nvar b = Color.FromArgb(128, 253, 100, 26);\n",
    })
    code, out = scan(tool, root)
    check(code == 0 and "0 colour(s) outside" in out, "exit 0, nothing outside the palette")

    print()
    print("2. A colour outside the palette is named with its file and line")
    root = repo({
        "revit/Win.cs": "// ok\nvar blue = Color.FromRgb(0x2F, 0x9B, 0xE3);\n",
        "mcp/page.css": ".x{color:#abc}\n",
        "brain/report.py": "STYLE = 'color: rgb(1, 2, 3)'\n",
    })
    code, out = scan(tool, root)
    check("revit/Win.cs" in out and "line 2: #2F9BE3" in out, "C# FromRgb in hex, at line 2")
    check("#AABBCC" in out, "a three-digit CSS colour is expanded and named")
    check("#010203" in out, "a CSS rgb() is named")
    check(code == 0, "a report: exit 0 even with findings")
    code, _ = scan(tool, root, "--strict")
    check(code == 1, "--strict: exit 1 on a finding")

    print()
    print("3. What it leaves out, it leaves out")
    root = repo({
        "brain/fragments/duct-colours/fragment.cs": "var c = Color.FromRgb(255, 0, 0);\n",
        "tests/test_x.py": "C = '#123456'\n",
        "brain/notes.py": "# see issue #123 and row #456\n",
        "revit/readme.txt": "#2F9BE3\n",
    })
    code, out = scan(tool, root)
    check("0 colour(s) outside" in out,
          "a fragment's model colour, a test, a three-digit # in Python and a .txt are not findings")

    print()
    print("4. It refuses to run rather than guess")
    root = repo({"mcp/page.css": ".x{color:#2F9BE3}\n"}, palette=None)
    code, out = scan(tool, root)
    check(code == 2 and "NOT RUN" in out, "no palette page: exit 2, not a pass")
    root = repo({"mcp/page.css": ".x{color:#2F9BE3}\n"}, palette="# Brand\n\nno table here\n")
    code, out = scan(tool, root)
    check(code == 2, "a palette page with no colour table: exit 2")

    print()
    print("5. The palette is this repository's own table, not a copy")
    tool.ROOT = ROOT
    palette = tool.read_palette()
    check(palette.get("FD641A") == "Accent" and palette.get("0F0F10") == "Ink"
          and palette.get("FF5A4E") == "Error",
          "docs/brand/README.md is read: Accent, Ink and Error found by their hex")

    for root in MADE:
        shutil.rmtree(root, onerror=_writable)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the brand check reads the palette and names what is outside it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
