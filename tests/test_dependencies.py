# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The dependency manifest describes the code, and keeps describing it.

    python tests/test_dependencies.py

WHAT IT PROVES
  1. EVERY THIRD-PARTY IMPORT IN THE REPOSITORY IS IN A MANIFEST. Derived by
     walking the imports, not by reading a list - which is the only version of
     this check worth having. Add an import and forget the manifest and this
     fails, which is the exact way W-5 happened: brain/README.md said `pyyaml`
     while the code imported six things, and nothing anywhere noticed.

  2. NOTHING IS IN A MANIFEST THAT THE CODE DOES NOT IMPORT. The opposite
     staleness, and the one that makes people install things for no reason.

  3. EVERY ENTRY SAYS WHAT IT IS FOR AND WHAT IS LOST WITHOUT IT. A name with
     no purpose beside it is a thing nobody dares remove (R-76), and the
     checker's whole report is built out of those two fields.

  4. REQUIRED AND OPTIONAL DO NOT OVERLAP. A package in both lists makes
     "optional" meaningless and the checker's exit code arbitrary.

  5. `mcp/client/` AND `platform/` IMPORT NOTHING THIRD-PARTY. The bridge
     client has to run on a locked-down machine with nothing installed on it -
     brain/README.md states that rule, and until now nothing enforced it. This
     is the one check here that guards a promise rather than a document.

WHAT IT DELIBERATELY DOES NOT PROVE
  Nothing here installs a package or asserts one is present. The suite runs on
  a machine with five of the six absent, and that is the state R-42 requires to
  be fine. Whether an installed version is the RIGHT one is R-78, not built.
"""

from __future__ import annotations

import ast
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "tools"))

FAILURES = []


def check(condition, message):
    if not condition:
        FAILURES.append(message)


def _load_checker():
    """Import tools/check-dependencies.py, whose name is not an identifier."""
    import importlib.util
    path = os.path.join(ROOT, "tools", "check-dependencies.py")
    spec = importlib.util.spec_from_file_location("check_dependencies", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _third_party(area):
    """Every top-level third-party module imported under `area`.

    Standard library and this repository's own modules are excluded by name,
    the same way check-dependencies has to think about it.
    """
    stdlib = set(sys.stdlib_module_names)
    local = set()
    for base, _dirs, files in os.walk(ROOT):
        if ".git" in base:
            continue
        for name in files:
            if name.endswith(".py"):
                local.add(name[:-3])

    found = {}
    start = os.path.join(ROOT, area)
    if not os.path.isdir(start):
        return found
    for base, dirs, files in os.walk(start):
        dirs[:] = [d for d in dirs if d not in ("__pycache__", ".git")]
        for name in sorted(files):
            if not name.endswith(".py"):
                continue
            path = os.path.join(base, name)
            with open(path, "r", encoding="utf-8", errors="replace") as handle:
                try:
                    tree = ast.parse(handle.read())
                except SyntaxError:
                    continue
            for node in ast.walk(tree):
                if isinstance(node, ast.Import):
                    names = [a.name.split(".")[0] for a in node.names]
                elif isinstance(node, ast.ImportFrom) and not node.level:
                    names = [(node.module or "").split(".")[0]]
                else:
                    continue
                for module in names:
                    if module and module not in stdlib and module not in local:
                        found.setdefault(module, os.path.relpath(path, ROOT))
    return found


def main():
    checker = _load_checker()

    required, problems = checker.read_manifest(checker.REQUIRED)
    optional, more = checker.read_manifest(checker.OPTIONAL)
    problems += more

    # 3. Both manifests parse, and every entry carries all three fields.
    check(not problems,
          "the manifests do not parse cleanly: %s" % "; ".join(problems))
    check(required, "requirements.txt lists no package at all")
    check(optional, "requirements-optional.txt lists no package at all")

    for package in required + optional:
        check(package.purpose.strip(),
              "%s has no `for` text - nobody will dare remove it" % package.pip_name)
        check(package.without.strip(),
              "%s does not say what is lost without it" % package.pip_name)

    listed = {p.module: p for p in required + optional}

    # 4. Required and optional are disjoint.
    overlap = {p.module for p in required} & {p.module for p in optional}
    check(not overlap,
          "in BOTH manifests, so `optional` means nothing for it: %s"
          % ", ".join(sorted(overlap)))

    # 1 and 2. The manifest and the imports describe each other.
    imported = {}
    for area in ("brain", "mcp", "tools", "tests", "platform", "revit"):
        imported.update(_third_party(area))

    for module, where in sorted(imported.items()):
        check(module in listed,
              "`%s` is imported by %s and is in NEITHER manifest - the exact "
              "shape of W-5" % (module, where))

    for module, package in sorted(listed.items()):
        check(module in imported,
              "`%s` is in a manifest and nothing imports it - people would "
              "install it for no reason" % package.pip_name)

    # 5. The layers that promise to need nothing, need nothing.
    for area in ("mcp/client", "platform"):
        bare = _third_party(area)
        check(not bare,
              "%s imports %s, and it has to run on a locked-down machine with "
              "nothing installed" % (area, ", ".join(sorted(bare))))

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - every third-party import in this repository is in a")
    print("manifest, every manifest entry is imported by something, and each")
    print("one says what it is for and what is lost without it.")
    print()
    print("Counts are derived rather than written here: run")
    print("`python tools/check-dependencies.py` for the list and what is")
    print("installed on THIS machine.")
    print()
    print("IT PROVES NOTHING ABOUT WHETHER A PACKAGE IS INSTALLED. Five of the")
    print("six are absent here and that is the state R-42 requires to be fine.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
