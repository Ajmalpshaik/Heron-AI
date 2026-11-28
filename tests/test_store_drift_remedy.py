# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The store-drift warning names what can actually be DONE, from where it is read.

    python tests/test_store_drift_remedy.py

FRAGMENT-ISSUES ROW 5b-1711, issue #853. A modeller on Revit 2024 read
"The knowledge store does not match this checkout: 3 card(s) ..." on every
lookup for a whole session. Away from a store this checkout may build, the
warning ended "rebuild it only from there - or set HERON_KNOWLEDGE to an empty
folder": no folder named as "there", no command. And from a worktree the only
command it could have named, `heron_scope.py --rebuild`, rebuilds the SHARED
store from MAIN's cards (row 5b-404) - so it cannot add a card only that
worktree has.

This holds `heron_brain._store_drift` to one remedy per situation, each asked
of a stand-in for `heron_scope` - no git, no store, no Revit:

  1. A PRIVATE store: `heron_scope.py --rebuild`, with the folder named.
  2. The SHARED store from the main Heron folder on main: `heron-ready.py`,
     the command a modeller is given, which rebuilds it.
  3. The main Heron folder on a BRANCH: back to main, or a store of its own -
     and no rebuild command, which would be refused there.
  4. A WORKTREE: the main Heron folder BY NAME, `git pull` there for cards it
     is missing, `git merge origin/main` here for rows with no card - and no
     rebuild command, which could not add this checkout's cards.
  5. A worktree whose main folder cannot be found: a store of its own.
  6. A question about the store that FAILS: never the rebuild command.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
for folder in (os.path.join(ROOT, "mcp", "server"), os.path.join(ROOT, "brain")):
    if folder not in sys.path:
        sys.path.insert(0, folder)

sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real trail
# (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class Scope(object):
    """A stand-in for heron_scope, answering as one kind of checkout would."""

    def __init__(self, private=False, refusal=None, source=("cards", None),
                 on_main=True, missing=("new-card",), extra=(), broken=False):
        self.private, self.refusal, self.source = private, refusal, source
        self.on_main, self.broken = on_main, broken
        self.missing, self.extra = list(missing), list(extra)

    def drift(self, store, folder=None, walked=None):
        return self.missing, self.extra

    def rebuild_refusal(self, root=None):
        if self.broken:
            raise OSError("the store's folder could not be read")
        return self.refusal

    def knowledge_dir(self):
        if self.broken:
            raise OSError("the store's folder could not be read")
        return "/scratch/own" if self.private else "/home/pc/.heron/knowledge"

    def shared_dir(self):
        return "/home/pc/.heron/knowledge"

    def _same_folder(self, a, b):
        return a == b

    def refreshes_from(self, root=None):
        return self.source

    def on_main_branch(self, root=None):
        return self.on_main


def said(BRAIN, scope):
    """The warning `_store_drift` gives with `scope` standing in for heron_scope."""
    real = BRAIN._brain
    modules = real()
    BRAIN._brain = lambda: (scope,) + tuple(modules[1:])
    try:
        return BRAIN._store_drift(None) or ""
    except Exception as exc:                                    # noqa: BLE001 - a crash is a FAIL
        return "(raised %s: %s)" % (type(exc).__name__, exc)
    finally:
        BRAIN._brain = real


def main():
    try:
        import heron_brain as BRAIN
    except Exception as exc:                                    # noqa: BLE001
        print("COULD NOT RUN: heron_brain does not import here - %s: %s"
              % (type(exc).__name__, exc))
        return 3

    print("1. A private store: the rebuild command, with its folder")
    text = said(BRAIN, Scope(private=True))
    check(text.startswith("The knowledge store does not match this checkout"),
          "the warning is still the one a lookup gives: %r" % text[:70])
    check("python brain/heron_scope.py --rebuild" in text and "/scratch/own" in text,
          "it names the rebuild and the private store's folder")

    print()
    print("2. The shared store, from the main Heron folder on main")
    text = said(BRAIN, Scope())
    check("python tools/heron-ready.py" in text,
          "it names heron-ready.py, the command a modeller is given: %r" % text[-160:])
    check("rebuild it only from there" not in text,
          "and never the old sentence that named no folder and no command")

    print()
    print("2b. The same, from a Heron with no tools/ - the Claude Code plugin (row 5b-2084)")
    was = getattr(BRAIN, "shipped_tool", None)
    check(was is not None, "heron_brain has one rule for naming a tool")
    # Planted whether or not the rule exists, so the two checks below run - and
    # FAIL - on the code before it (heron-ship 2a).
    BRAIN.shipped_tool = lambda name: None
    try:
        text = said(BRAIN, Scope())
    finally:
        if was is None:
            del BRAIN.shipped_tool
        else:
            BRAIN.shipped_tool = was
    whole = os.path.join(BRAIN.ROOT, "brain", "heron_scope.py")
    check("heron-ready" not in text and "tools/" not in text,
          "it names no tool the plugin does not carry: %r" % text[-160:])
    check(('"%s" --rebuild' % whole) in text,
          "and gives the rebuild that IS there, by its whole path")

    print()
    print("3. The main Heron folder on a branch")
    text = said(BRAIN, Scope(refusal="on a branch", on_main=False))
    check("git checkout main" in text and "HERON_KNOWLEDGE" in text,
          "it says to switch back to main, or to give this folder a store of its own")
    check("--rebuild" not in text and "heron-ready" not in text,
          "and names no rebuild, which is refused from a branch")

    print()
    print("4. A worktree, whose main Heron folder is named")
    main_folder = "/home/pc/Heron AI"
    text = said(BRAIN, Scope(refusal="not the main checkout",
                             source=("main-cards", main_folder)))
    check(main_folder in text and "git pull" in text and "HERON_KNOWLEDGE" in text,
          "cards missing: it names the main Heron folder, a pull there, and a store "
          "of its own: %r" % text[-220:])
    check("--rebuild" not in text and "heron-ready" not in text,
          "and names no rebuild, which would rebuild from main's cards, not these")
    text = said(BRAIN, Scope(refusal="not the main checkout",
                             source=("main-cards", main_folder),
                             missing=(), extra=("gone-card",)))
    check("git merge origin/main" in text and main_folder in text,
          "rows with no card here: it says this checkout is behind, and how to catch up")

    print()
    print("5. A worktree whose main Heron folder cannot be found")
    text = said(BRAIN, Scope(refusal="cannot be found", source=None))
    check("cannot be found" in text and "HERON_KNOWLEDGE" in text
          and "--rebuild" not in text,
          "it says the main folder cannot be found, and offers a store of its own")

    print()
    print("6. A question about the store that fails")
    text = said(BRAIN, Scope(broken=True))
    check(text.startswith("The knowledge store does not match this checkout")
          and "could not tell whose store" in text
          and "--rebuild" not in text and "heron-ready" not in text,
          "the warning is still given, and offers no rebuild on a guess: %r" % text[-160:])

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASSED - each place the warning is read from is told what reaches it:")
    print("a rebuild where one may run, the main Heron folder by name where the")
    print("shared store follows it, and a store of its own everywhere else.")
    print("It says nothing about a real PC's store.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
