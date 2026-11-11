#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A worktree inside the checkout is another checkout, and no walk reads it as
part of this one.

    python tests/test_nested_worktree.py

WHY THIS FILE EXISTS (FRAGMENT-ISSUES row 5b-1539)
--------------------------------------------------
A session's worktree is a whole copy of the repository at
`.claude/worktrees/<name>/`, INSIDE the checkout it was added from. Git never
lists it. A walk of the disk did, and four tools answered about two
repositories at once - one of them, `tools/check-compile.py` with no git to
ask, by exiting 1 before it could say the machine has no compiler, which is
what failed `tests/test_check_api_surface.py` on every machine holding a
worktree while CI stayed green.

EVERY CASE BUILDS ITS OWN TREE in a temp folder: a small checkout with a FAKE
worktree under `.claude/worktrees/`, which is a folder holding a `.git` FILE,
as `git worktree add` writes one. Nothing here runs git and nothing is
written into the repository.

Each walker gets two kinds of check, because a walk that reads nothing would
pass the first kind alone:

  the nested copy is not read      red on the code as it stood
  what is this checkout's still is the control - a hook under `.claude`, a
                                   folder merely NAMED `worktrees`

 1. brain/heron_checkout.py - the one rule the four walkers ask
 2. brain/heron_dotnet.py   - unlisted(), when git cannot say what is tracked
 3. tools/module-reach.py   - importers_of()
 4. tools/measure-routes.py - remember_callers()
 5. tools/check-structure.py - the PowerShell files it checks
"""

import importlib.util
import io
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

NESTED = ".claude/worktrees/agent-fake"

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(root, rel, body=u"", raw=None):
    path = os.path.join(root, *rel.split("/"))
    if not os.path.isdir(os.path.dirname(path)):
        os.makedirs(os.path.dirname(path))
    if raw is not None:
        io.open(path, "wb").write(raw)
    else:
        io.open(path, "w", encoding="utf-8", newline="").write(body)
    return path


def worktree(root, rel=NESTED):
    """A linked worktree as git leaves one: a folder whose `.git` is a FILE."""
    write(root, rel + "/.git",
          u"gitdir: /nowhere/.git/worktrees/%s\n" % rel.rsplit("/", 1)[-1])
    return rel


def tool(name):
    """A tool in tools/ as a module, or None - their names carry a hyphen."""
    path = os.path.join(ROOT, "tools", name)
    try:
        spec = importlib.util.spec_from_file_location(
            name[:-3].replace("-", "_") + "_nested", path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                           # noqa: BLE001
        return None


def spelled(paths):
    return sorted(p.replace(os.sep, "/") for p in paths)


def inside(paths, rel=NESTED):
    return [p for p in spelled(paths) if p.startswith(rel + "/")]


# ------------------------------------------------------------------ 1

def the_rule(home):
    print("1. heron_checkout: a folder holding its own .git is another checkout")
    try:
        import heron_checkout as CHECKOUT
    except ImportError:
        CHECKOUT = None
    check(CHECKOUT is not None, "brain/heron_checkout.py is there to ask")
    is_another = getattr(CHECKOUT, "is_another", None)
    own = getattr(CHECKOUT, "own", None)
    check(callable(is_another) and callable(own),
          "and it answers is_another(folder) and own(parent, names)")
    if not (callable(is_another) and callable(own)):
        # So the checks below still RUN and FAIL, one line each.
        is_another = lambda folder: None             # noqa: E731
        own = lambda parent, names: None             # noqa: E731

    root = tempfile.mkdtemp(dir=home)
    write(root, ".git/HEAD", u"ref: refs/heads/main\n")
    worktree(root, "linked")
    write(root, "clone/.git/HEAD", u"ref: refs/heads/main\n")
    write(root, "worktrees/plain/file.txt", u"x\n")
    write(root, "plain/file.txt", u"x\n")

    check(is_another(os.path.join(root, "linked")) is True,
          "a linked worktree - .git is a FILE - is another checkout")
    check(is_another(os.path.join(root, "clone")) is True,
          "a clone - .git is a FOLDER - is another checkout")
    check(is_another(os.path.join(root, "plain")) is False,
          "a folder with no .git is this checkout's")
    check(is_another(os.path.join(root, "worktrees")) is False,
          "and so is one merely NAMED worktrees: the rule is the .git, "
          "never the name")
    names = ["plain", "linked", "worktrees", "clone", ".git"]
    check(own(root, names) == ["plain", "worktrees", ".git"],
          "own() keeps what is this checkout's, in the order given "
          "(got %r)" % (own(root, names),))
    check(own(root, []) == [], "and an empty folder is an empty answer")


# ------------------------------------------------------------------ 2

def the_projects(home):
    print("\n2. heron_dotnet.unlisted(): with no git to ask, the walk stays "
          "in this checkout")
    try:
        import heron_dotnet as NET
    except BaseException:                           # noqa: BLE001
        NET = None
    unlisted = getattr(NET, "unlisted", None)
    check(callable(unlisted), "heron_dotnet has unlisted() to ask")
    if not callable(unlisted):
        return

    root = tempfile.mkdtemp(dir=home)
    write(root, "revit/Heron.New/Heron.New.csproj", u"<Project />\n")
    write(root, "worktrees/Heron.Named/Heron.Named.csproj", u"<Project />\n")
    worktree(root)
    write(root, NESTED + "/revit/Heron.Bridge/Heron.Bridge.csproj",
          u"<Project />\n")
    write(root, NESTED + "/platform/Heron.Core/Heron.Core.csproj",
          u"<Project />\n")
    write(root, "vendor/copy/.git/HEAD", u"ref: refs/heads/main\n")
    write(root, "vendor/copy/Copy.csproj", u"<Project />\n")

    # "git cannot say" is the case under test: a machine with no git on PATH,
    # which is what tests/test_check_api_surface.py's empty PATH makes. The
    # temp folder is no repository either, but a TMPDIR inside one would let
    # git answer "none tracked" and every check below pass for nothing.
    had = getattr(NET, "_tracked_projects", None)
    check(callable(had), "it asks git first, through _tracked_projects()")
    NET._tracked_projects = lambda where: None
    try:
        got = unlisted(root=root)
    except BaseException as raised:                 # noqa: BLE001
        got = ["raised %s" % type(raised).__name__]
    finally:
        if had is not None:
            NET._tracked_projects = had

    check("revit/Heron.New/Heron.New.csproj" in got,
          "a project of this checkout that nothing builds is still named")
    check("worktrees/Heron.Named/Heron.Named.csproj" in got,
          "and so is one under a folder merely named worktrees")
    check(not inside(got),
          "no project inside %s is named (got %r)" % (NESTED, inside(got)))
    check(not inside(got, "vendor/copy"),
          "nor one inside a nested clone (got %r)"
          % (inside(got, "vendor/copy"),))
    check(len(got) == 2, "so two projects are unlisted, not five (got %d)"
          % len(got))


# ------------------------------------------------------------------ 3

def the_importers(home):
    print("\n3. module-reach: a worktree's files are not this checkout's "
          "importers")
    reach = tool("module-reach.py")
    importers_of = getattr(reach, "importers_of", None)
    check(callable(importers_of), "tools/module-reach.py has importers_of()")
    if not callable(importers_of):
        return

    root = tempfile.mkdtemp(dir=home)
    write(root, "brain/lonely.py", u"x = 1\n")
    write(root, "brain/hooked.py", u"x = 1\n")
    write(root, "tests/test_lonely.py", u"import lonely\n")
    write(root, ".claude/hooks/session.py", u"import hooked\n")
    worktree(root)
    write(root, NESTED + "/brain/lonely.py", u"x = 1\n")
    write(root, NESTED + "/tests/test_lonely.py", u"import lonely\n")
    write(root, NESTED + "/mcp/server.py", u"import lonely\nimport hooked\n")

    was = (reach.ROOT, reach.BRAIN_DIR)
    reach.ROOT, reach.BRAIN_DIR = root, os.path.join(root, "brain")
    try:
        found = importers_of({"lonely", "hooked"})
    except BaseException as raised:                 # noqa: BLE001
        found = {"lonely": {"raised %s" % type(raised).__name__}}
    finally:
        reach.ROOT, reach.BRAIN_DIR = was

    lonely = spelled(found.get("lonely", ()))
    hooked = spelled(found.get("hooked", ()))
    check(lonely == ["tests/test_lonely.py"],
          "a module only its own suite imports has that one importer "
          "(got %r)" % (lonely,))
    check(hooked == [".claude/hooks/session.py"],
          "a hook under .claude is still a caller, and the only one "
          "(got %r)" % (hooked,))
    check(not inside(lonely + hooked),
          "nothing inside %s is counted" % NESTED)


# ------------------------------------------------------------------ 4

def the_callers(home):
    print("\n4. measure-routes: a caller is named once, from this checkout")
    routes = tool("measure-routes.py")
    callers = getattr(routes, "remember_callers", None)
    check(callable(callers), "tools/measure-routes.py has remember_callers()")
    if not callable(callers):
        return

    root = tempfile.mkdtemp(dir=home)
    call = u"def go(S, a, b, c):\n    S.remember(a, b, c)\n"
    write(root, "tests/test_search.py", call)
    worktree(root)
    write(root, NESTED + "/tests/test_search.py", call)
    write(root, NESTED + "/brain/old_caller.py", call)

    was = routes.ROOT
    routes.ROOT = root
    try:
        got = spelled(callers())
    except BaseException as raised:                 # noqa: BLE001
        got = ["raised %s" % type(raised).__name__]
    finally:
        routes.ROOT = was

    check("tests/test_search.py" in got, "this checkout's caller is found")
    check(not inside(got),
          "and no caller inside %s - a production caller an older copy "
          "still has is not one this tree has (got %r)" % (NESTED, inside(got)))
    check(got == ["tests/test_search.py"], "so it is named once (got %r)" % (got,))


# ------------------------------------------------------------------ 5

def the_powershell(home):
    print("\n5. check-structure: a worktree's PowerShell is not checked as "
          "this checkout's")
    root = tempfile.mkdtemp(dir=home)
    write(root, "tools/fine.ps1", u"Write-Host 'plain ASCII'\n")
    worktree(root)
    # No BOM and a non-ASCII byte: the ANSI trap, in a copy at an older commit.
    write(root, NESTED + "/tools/old.ps1",
          raw=u"Write-Host 'a dash — here'\n".encode("utf-8"))

    try:
        ran = subprocess.run(
            [sys.executable, os.path.join(ROOT, "tools", "check-structure.py")],
            cwd=root, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        said = ran.stdout.decode("utf-8", "replace")
    except OSError as raised:
        said = "could not run: %s" % raised
    counted = re.search(r"PowerShell files:\s+(\d+) checked", said)
    check(counted is not None,
          "it ran, and says how many PowerShell files it checked")
    check(counted is not None and counted.group(1) == "1",
          "one - this checkout's - and not the worktree's as well (got %s)"
          % (counted.group(1) if counted else "no count"))
    check("old.ps1" not in said,
          "and a trap in the worktree's copy is not a problem of this tree")
    check("fine.ps1" not in said,
          "the control: a clean script of this tree raises nothing either")


def main():
    print(__doc__.strip().splitlines()[0])
    print(__doc__.strip().splitlines()[1])
    print()
    home = tempfile.mkdtemp(prefix="heron-nested-worktree-")
    try:
        the_rule(home)
        the_projects(home)
        the_importers(home)
        the_callers(home)
        the_powershell(home)
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("All checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
