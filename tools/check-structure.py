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

import ast
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
#
# mcp MAY depend on brain, and the direction is not a convenience. docs/02 s7
# draws the stack with the brain UNDERNEATH the MCP server - "Heron MCP Server
# — Python: brain, RAG, fragments, skills, memory" - because the host reaches
# Heron only through MCP tools (D-01). A brain nothing on that side may import
# is a brain no conversation can reach, which is exactly the state Phase 2 was
# found in on 2026-08-29: eight modules, seven fragments and ten skills, built,
# tested and imported by nothing but their own tests.
#
# This table did not permit that import and did not forbid it either - it only
# ever ran against C# ProjectReferences, so the Python side has never been
# checked here at all. Writing the rule down is the point: the omission read as
# a prohibition to anybody who opened this file.
#
# THE PYTHON SIDE IS CHECKED FROM 2026-09-12, and running it for the first time
# settled what "tools": set() had always meant. It raised 34 imports, every one
# of them a gate in tools/ reading brain/ - which is what D-48 asks those gates
# to do. The row was written for ProjectReferences, and tools/ has no project
# file, so it had never applied to anything. It stays as it is for C# and the
# Python pass exempts tools/ explicitly, with the reason at the exemption.
#
# The isolation that matters is unchanged. revit and brain still never touch:
# the brain must stay runnable, and testable, on a machine with no Revit on it.
ALLOWED = {
    "platform": set(),
    "revit":    {"platform"},
    "mcp":      {"platform", "brain"},
    "brain":    {"platform"},
    "tests":    {"platform", "revit", "mcp", "brain"},
    "tools":    set(),
}

# The adapter boundary from docs/16 section 4: Autodesk.Revit types may appear
# ONLY inside revit/. Anywhere else means the boundary has been crossed, and
# the core has become untestable without Revit.
REVIT_API = re.compile(r"\bAutodesk\.Revit\b")

# The Python half of ALLOWED, and until 2026-09-12 it did not exist.
#
# The comment above has said since 2026-08-29 that this table "only ever ran
# against C# ProjectReferences, so the Python side has never been checked here
# at all". That was written as an admission and then left standing, which is
# the worst of both: the rule reads as enforced and is not. Two thirds of this
# repository is Python.
#
# Python here does not import by package path - brain/ and mcp/ put their own
# folder on sys.path and then `import heron_fragment`. So a module's PART
# cannot be read off the import line; it has to be looked up. That map is built
# from disk, and a name owned by two parts is reported rather than guessed,
# because a guess in a layering checker is a layering rule that is sometimes
# not applied.
#
# READ WITH AST RATHER THAN GREPPED, and the difference matters here in a way
# it does not for the Autodesk.Revit rule a few lines below. That one greps on
# purpose - a vendor namespace named in a COMMENT is still a boundary being
# discussed in the wrong file. An import is not like that: a docstring
# containing the words "import heron_brain" is prose, and failing a build over
# it would be a false positive in a gate, which teaches people to skim gates.
def python_imports(text):
    """Module names imported by this file, or None if it does not parse."""
    try:
        tree = ast.parse(text)
    except SyntaxError:
        return None
    names = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                names.add(alias.name.split(".")[0])
        elif isinstance(node, ast.ImportFrom):
            if node.module and not node.level:
                names.add(node.module.split(".")[0])
    return names

# Only the Path Manager may resolve a Windows special folder. Anything else
# building its own path is how two parts of the system quietly disagree about
# where something lives - which happened once, in the add-in's log path, and
# is the reason this rule exists.
SPECIAL_FOLDER = re.compile(r'GetFolderPath|SpecialFolder[.]|environ.\[.(APPDATA|LOCALAPPDATA)')
PATH_OWNERS = ("platform/Heron.Core/HeronPaths.cs",
               "mcp/client/heron_bridge_client.py")

CODE_EXT = (".cs", ".py", ".ps1", ".csproj")
# "build" is generated output owned by tools/check-fragments-compile.py, and it is
# gitignored. It is listed here because leaving it out made THIS checker report a
# problem that did not exist: run it while a fragment compile is mid-build and the
# few seconds of generated .cs read as "stray code outside the declared parts".
# A checker that fails on timing is worse than one that does not run - it teaches
# the reader to skim past checker output, which is the one habit this repository
# cannot afford. Caught 2026-08-30 by running the two concurrently.
SKIP_DIRS = {"bin", "obj", "build", ".vs", "__pycache__", ".git", "node_modules"}


def walk(root):
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        for fn in filenames:
            if fn.endswith(CODE_EXT):
                yield os.path.join(dirpath, fn).replace(os.sep, "/")


def part_of(path):
    return path.split("/")[0]


def python_owners():
    """
    module name -> the part that owns it, built from disk.

    Returns (owners, ambiguous). An ambiguous name is one two parts both
    define; nothing is inferred for those, and they are reported instead.
    """
    seen = {}
    ambiguous = {}
    for part in PARTS:
        if not os.path.isdir(part):
            continue
        for path in walk(part):
            if not path.endswith(".py"):
                continue
            name = path.rsplit("/", 1)[-1][:-3]
            if name in seen and seen[name] != part:
                ambiguous.setdefault(name, {seen[name]}).add(part)
            else:
                seen[name] = part
    return seen, ambiguous


def main():
    problems = []
    notes = []
    py_owner, py_ambiguous = python_owners()
    py_imports = 0
    tool_reach = {}
    for name, parts in sorted(py_ambiguous.items()):
        notes.append("module '%s' is defined in %s - this checker cannot say which "
                     "part an import of it means, so it checks neither"
                     % (name, " and ".join(sorted(parts))))

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

            # Python imports, the same table applied to the other language.
            #
            # A file that does not parse is reported rather than skipped: an
            # unchecked file in a layering gate is a layering rule that is
            # sometimes not applied, and this is the one place that would never
            # otherwise say so.
            #
            # tools/ IS EXEMPT, for the reason already written a few lines
            # below about Autodesk.Revit: a script talks ABOUT the code rather
            # than being it. Every gate in this folder reads the library
            # through brain/heron_fragment.load_all() and D-48 says it should -
            # the three that parsed fragment.yaml themselves each lost a
            # malformed fragment silently. Forbidding that import would make
            # the honest way the illegal way.
            #
            # Its reach is still printed, as a NOTE rather than a problem.
            # "Which tools reach into the brain" is worth being able to see;
            # "a tool reached into the brain" is not a defect.
            if path.endswith(".py") and part in ALLOWED:
                modules = python_imports(text)
                if modules is None:
                    problems.append("%s does not parse, so its imports could not "
                                    "be checked against the layering rules" % path)
                    modules = set()
                for module in sorted(modules):
                    dep = py_owner.get(module)
                    if dep is None or dep == part or module in py_ambiguous:
                        continue
                    py_imports += 1
                    if part == "tools":
                        tool_reach.setdefault(path, set()).add(dep)
                        continue
                    if dep not in ALLOWED.get(part, set()):
                        problems.append(
                            "%s imports '%s', which belongs to '%s' - not allowed. "
                            "%s may depend on: %s"
                            % (path, module, dep, part,
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
    print("Python imports:   %d checked across %d module(s)" % (py_imports, len(py_owner)))
    if tool_reach:
        reached = sorted(set(p for parts in tool_reach.values() for p in parts))
        print("Tools reading:    %d script(s) in tools/ read %s - allowed, D-48"
              % (len(tool_reach), ", ".join(reached)))
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

    print("Structure clean. Every part in its place, no layering violations")
    print("in either language, and Autodesk.Revit appears only inside revit/.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
