# Heron-Agent:  HERON-WSP-VAL-003, HERON-AHR-MON-011
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Folder Validation + Architecture Monitor - the working prototype of
HERON-WSP-VAL-003 and HERON-AHR-MON-011.

Two questions, both of which used to be answered by hand:

  1. Is every file in the part it belongs to?
  2. Does any part depend on something it must not?

The second is the one that matters. A layering rule that is only written in a
document gets broken quietly; a layering rule in a script gets broken loudly,
once, and then fixed.

    python tools/check-structure.py

Exit 0 = clean.
"""

import io
import os
import re
import sys

# ---------------------------------------------------------------- structure
# The canonical layout. Top-level folders mirror the four product parts from
# the specification, so "where do I fix the Revit thing" has one answer.
PARTS = {
    "revit":    "Part 1 - loads into Revit.exe. C#. Changing it needs a Revit restart.",
    "mcp":      "Part 2 - the bridge to the AI host. Python, runs outside Revit.",
    "brain":    "Part 3 - knowledge, RAG, fragments, skills. Python, outside Revit.",
    "platform": "Part 4 - kernel, install, update. Shared by everything.",
}
SUPPORT = {
    "docs":  "specification, decisions, questions",
    "tests": "acceptance tests and the Revit-free host",
    "tools": "scripts that keep the repository honest",
    ".github": "issue templates and CI",
    ".claude": "project skills that ship with the repository",
}

# ------------------------------------------------------------------ layering
# Who may depend on whom. Everything may depend on platform; platform depends
# on nothing. revit and brain never touch each other.
ALLOWED = {
    "platform": set(),
    "revit":    {"platform"},
    "mcp":      {"platform"},
    "brain":    {"platform"},
    "tests":    {"platform", "revit", "mcp", "brain"},
    "tools":    set(),
}

# The adapter boundary from docs/16 section 4: Autodesk.Revit types may appear
# ONLY inside revit/. Anywhere else means the boundary has been crossed, and
# the core has become untestable without Revit.
REVIT_API = re.compile(r"\bAutodesk\.Revit\b")

# Only the Path Manager may resolve a Windows special folder. Anything else
# building its own path is how two parts of the system quietly disagree about
# where something lives - which happened once, in the add-in's log path, and
# is the reason this rule exists.
SPECIAL_FOLDER = re.compile(r'GetFolderPath|SpecialFolder[.]|environ.\[.(APPDATA|LOCALAPPDATA)')
PATH_OWNERS = ("platform/Heron.Core/HeronPaths.cs",
               "mcp/client/heron_bridge_client.py")

CODE_EXT = (".cs", ".py", ".ps1", ".csproj")
SKIP_DIRS = {"bin", "obj", ".vs", "__pycache__", ".git", "node_modules"}


def walk(root):
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        for fn in filenames:
            if fn.endswith(CODE_EXT):
                yield os.path.join(dirpath, fn).replace(os.sep, "/")


def part_of(path):
    return path.split("/")[0]


def main():
    problems = []
    notes = []

    # --- 1. every declared part exists -------------------------------------
    for name in PARTS:
        if not os.path.isdir(name):
            problems.append("part '%s' is missing from the repository" % name)

    # --- 2. no stray code outside the declared parts ------------------------
    known = set(PARTS) | set(SUPPORT)
    for entry in sorted(os.listdir(".")):
        if not os.path.isdir(entry) or entry.startswith("."):
            continue
        if entry in known or entry in SKIP_DIRS:
            continue
        if any(True for _ in walk(entry)):
            problems.append("'%s/' holds code but is not a declared part "
                            "(add it to PARTS, or move the code)" % entry)

    # --- 3. layering ---------------------------------------------------------
    refs = 0
    for part in list(PARTS) + ["tests", "tools"]:
        if not os.path.isdir(part):
            continue
        for path in walk(part):
            try:
                text = io.open(path, encoding="utf-8", errors="replace").read()
            except OSError:
                continue

            # project references reveal C# dependencies
            for m in re.finditer(r'ProjectReference\s+Include="([^"]+)"', text):
                target = m.group(1).replace("\\", "/")
                resolved = os.path.normpath(
                    os.path.join(os.path.dirname(path), target)).replace(os.sep, "/")
                dep = part_of(resolved)
                refs += 1
                if dep == part:
                    continue
                if dep not in ALLOWED.get(part, set()):
                    problems.append("%s depends on '%s' - not allowed. %s may depend on: %s"
                                    % (path, dep, part,
                                       ", ".join(sorted(ALLOWED.get(part, set()))) or "nothing"))

            # The adapter boundary. tools/ is exempt: scripts talk ABOUT the
            # code rather than being it - and this very checker contains the
            # pattern, which it duly flagged on its first run.
            if (path not in PATH_OWNERS and part != "tools"
                    and SPECIAL_FOLDER.search(text)):
                problems.append("%s builds its own path - only HeronPaths may resolve a "
                                "special folder (docs/06 section 2)" % path)

            if part not in ("revit", "tools") and REVIT_API.search(text):
                problems.append("%s references Autodesk.Revit outside revit/ - "
                                "the adapter boundary is broken (docs/16 section 4)" % path)

    # --- 4. every part explains itself --------------------------------------
    for name in PARTS:
        if os.path.isdir(name) and not os.path.exists(os.path.join(name, "README.md")):
            notes.append("%s/ has no README.md saying what belongs there" % name)

    # --- report --------------------------------------------------------------
    print("Parts:            %s" % ", ".join(sorted(PARTS)))
    print("Project refs:     %d checked" % refs)
    print("Layering rules:   %d" % len(ALLOWED))
    print()

    # --- 4. PowerShell encoding ----------------------------------------------
    #
    # A .ps1 that is BOM-less AND contains a non-ASCII character is a file
    # Windows PowerShell 5.1 reads as ANSI. An em dash's last byte becomes a
    # smart quote, which PowerShell accepts as a STRING DELIMITER - so one dash
    # opens an unterminated string and cascades into dozens of parse errors
    # that look like syntax. The script never runs at all.
    #
    # This is checked here because nothing else can catch it. These scripts are
    # written and edited on machines with NO POWERSHELL - there is nothing to
    # fail on until the file reaches Windows, which is exactly the wrong moment
    # to find out. Safe either way: a BOM, or no non-ASCII byte.
    ps_checked = 0
    for dirpath, dirnames, filenames in os.walk("."):
        dirnames[:] = [d for d in dirnames
                       if d not in ("__pycache__", "bin", "obj", ".vs", ".git")]
        for name in filenames:
            if not name.endswith(".ps1"):
                continue
            path = os.path.join(dirpath, name)
            raw = io.open(path, "rb").read()
            ps_checked += 1
            if raw[:3] == b"\xef\xbb\xbf":
                continue                      # a BOM settles it
            try:
                raw.decode("ascii")
            except UnicodeDecodeError:
                problems.append(
                    "%s has no UTF-8 BOM and contains a non-ASCII byte. Windows PowerShell "
                    "5.1 will read it as ANSI and may fail to parse it at all - give it a BOM "
                    "or keep it pure ASCII." % path)

    print("PowerShell files: %d checked for the ANSI trap" % ps_checked)

    if problems:
        print("STRUCTURE PROBLEMS (%d):" % len(problems))
        for p in problems:
            print("  - %s" % p)
        print()
        return 1

    if notes:
        print("Notes:")
        for n in notes:
            print("  - %s" % n)
        print()

    print("Structure clean. Every part in its place, no layering violations,")
    print("and Autodesk.Revit appears only inside revit/.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
