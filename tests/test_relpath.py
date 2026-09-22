#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A path shown to a person never raises, on any drive, on any machine.

    python tests/test_relpath.py

`os.path.relpath` RAISES on Windows when its two paths are on different
drives, and the owner's PC keeps the repository on D: and the temp folder on
C:. CI runs on one Linux mount, where the call cannot raise, so every
instance of this reached main looking green. `brain/heron_relpath.py` holds
the one answer now, and this pins it - with the two places it was reachable
unguarded, found 2026-09-22 (FRAGMENT-ISSUES row 5b-152):

  tools/build-release-assets.py --out   raised AFTER checksums.txt, the
                                        completion mark, was written - a
                                        finished release exited 1
  tools/prove-skill.py --jobs           raised after the FIRST job file,
                                        and wrote none of the rest

THE CONDITION IS FORCED, NOT FOUND. A suite that waited for a second drive
would prove nothing on the machines that run it most. So for the length of
one check, `os.path.relpath` is replaced by one that raises what Windows
raises whenever its two paths sit on different sides of a scratch folder
standing in for the other drive - and only then, so a check can fail only
on the call it is about. Where THIS machine really has two drives, the real
call is exercised as well, and the output says which ran.

AND WITH PyYAML BLOCKED, because that is why the copies kept coming back:
the rule's old home, heron_fragment, cannot be imported without it, and
.github/workflows/release.yml never installs it.
"""

import ast
import contextlib
import importlib.util
import io
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BRAIN = os.path.join(ROOT, "brain")
TOOLS = os.path.join(ROOT, "tools")
sys.path.insert(0, BRAIN)

import heron_relpath as RELPATH                                 # noqa: E402

REAL = os.path.relpath

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def inside(path, folder):
    path, folder = os.path.abspath(path), os.path.abspath(folder)
    return path == folder or path.startswith(folder.rstrip(os.sep) + os.sep)


@contextlib.contextmanager
def another_drive(far):
    """While inside, everything under `far` is on a drive of its own."""
    def relpath(path, start=os.curdir):
        if inside(path, far) != inside(start, far):
            raise ValueError("path is on mount %r, start on mount %r"
                             % ("far" if inside(path, far) else "near",
                                "far" if inside(start, far) else "near"))
        return REAL(path, start)
    os.path.relpath = relpath
    try:
        yield
    finally:
        os.path.relpath = REAL


def attempt(fn, *args):
    """(answer, None) or (None, what it raised)."""
    try:
        return fn(*args), None
    except Exception as error:                          # noqa: BLE001
        return None, "%s: %s" % (type(error).__name__, error)


def load(name, filename):
    """A tool as a module, or None - its file name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location(
            name, os.path.join(TOOLS, filename))
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException as error:                      # noqa: BLE001
        print("  could not load tools/%s - %s: %s"
              % (filename, type(error).__name__, error))
        return None


def spoken(fn, argv):
    """(exit code, what it printed, what it raised).

    A RAISE IS CAUGHT AND NAMED RATHER THAN LET OUT. A raise is the whole
    subject here, so a suite that died of one would report the defect as its
    own crash.
    """
    was, out = sys.argv, io.StringIO()
    sys.argv = list(argv)
    try:
        with contextlib.redirect_stdout(out):
            code = fn()
        return code, out.getvalue(), None
    except SystemExit as stop:
        return stop.code, out.getvalue(), None
    except Exception as error:                          # noqa: BLE001
        return None, out.getvalue(), "%s: %s" % (type(error).__name__, error)
    finally:
        sys.argv = was


def fresh(*lines):
    """(exit code, stdout, stderr) from a new interpreter with no PyYAML.

    None in sys.modules is Python's own way of saying a module is not there,
    so `import yaml` fails exactly as it does on a machine that never
    installed it.
    """
    source = "\n".join(("import sys", "sys.modules['yaml'] = None") + lines)
    env = dict(os.environ, PYTHONIOENCODING="utf-8")
    try:
        run = subprocess.run([sys.executable, "-c", source], cwd=ROOT, env=env,
                             stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                             stderr=subprocess.PIPE, timeout=600)
    except subprocess.TimeoutExpired:
        return None, "", "did not finish in 600 seconds"
    return (run.returncode, run.stdout.decode("utf-8", "replace"),
            run.stderr.decode("utf-8", "replace"))


def run_tool(filename, *argv):
    tool = os.path.join(TOOLS, filename)
    return fresh("import runpy",
                 "sys.argv = %r" % ([tool] + list(argv),),
                 "runpy.run_path(%r, run_name='__main__')" % tool)


def last(text):
    """The last line of a traceback, which is the one that names it."""
    lines = [one for one in text.strip().splitlines() if one.strip()]
    return lines[-1] if lines else "(nothing on stderr)"


def main():
    far = tempfile.mkdtemp(prefix="heron-other-drive-")
    store = tempfile.mkdtemp(prefix="heron-relpath-knowledge-")
    # THE BRAIN ASKS FOR A KNOWLEDGE STORE when prove-skill.py imports it.
    # Nothing here reads one, and a suite must never touch the owner's.
    kept = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = store
    try:
        return run(far)
    finally:
        if kept is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = kept
        shutil.rmtree(far, ignore_errors=True)
        shutil.rmtree(store, ignore_errors=True)


def run(far):
    print("A path shown to a person never raises.")

    print()
    print("1. IT NEEDS NOTHING INSTALLED")
    print("   The rule's old home cannot be imported without PyYAML, so every")
    print("   caller that must run without it wrote a copy instead.")
    code, said, err = fresh(
        "sys.path.insert(0, %r)" % BRAIN,
        "import heron_relpath",
        "print(heron_relpath.relpath(%r, %r))"
        % (os.path.join(BRAIN, "x.py"), ROOT))
    check(code == 0,
          "brain/heron_relpath.py imports and answers with PyYAML blocked%s"
          % ("" if code == 0 else " - " + last(err)))
    tree = ast.parse(io.open(os.path.join(BRAIN, "heron_relpath.py"),
                             encoding="utf-8").read())
    imported = sorted(set(
        [a.name for n in ast.walk(tree) if isinstance(n, ast.Import)
         for a in n.names] +
        [n.module for n in ast.walk(tree)
         if isinstance(n, ast.ImportFrom) and n.module]))
    check(imported == ["os"],
          "and it imports nothing but os, as its docstring says - it "
          "imports %s" % (imported,))

    print()
    print("2. ON ONE DRIVE IT IS os.path.relpath")
    mine = os.path.join(BRAIN, "heron_relpath.py")
    got = RELPATH.relpath(mine, ROOT)
    check(got == REAL(mine, ROOT) == os.path.join("brain", "heron_relpath.py"),
          "a file in the checkout comes back relative - %r" % got)
    check(RELPATH.relpath(os.path.dirname(ROOT), ROOT) == os.pardir,
          "and a folder beside it climbs out, rather than being called "
          "absolute, because it has a relative form")

    print()
    print("3. ON TWO DRIVES IT ANSWERS WHOLE - FORCED, SO IT RUNS EVERYWHERE")
    target = os.path.join(far, "out", "changes.json")
    with another_drive(far):
        _raw, raw = attempt(os.path.relpath, target, ROOT)
        got, raised = attempt(RELPATH.relpath, target, ROOT)
        back, back_raised = attempt(RELPATH.relpath, ROOT, far)
    check(raw is not None,
          "the condition is real: inside it the raw call raises, as Windows "
          "does - %s" % raw)
    check(raised is None,
          "relpath() does not%s" % ("" if raised is None
                                    else " - it raised " + raised))
    check(got == os.path.abspath(target),
          "it answers the whole path instead - %r" % got)
    check(got is not None
          and os.path.abspath(os.path.join(ROOT, got))
          == os.path.abspath(target),
          "and joined back onto ROOT it still names the same file")
    check(back_raised is None and back == os.path.abspath(ROOT),
          "and the same with the start on the other drive")

    print()
    print("4. AND FOR REAL, WHERE THIS MACHINE HAS TWO DRIVES")
    here = os.path.splitdrive(ROOT)[0].upper()
    there = os.path.splitdrive(os.path.abspath(far))[0].upper()
    if here and there and here != there:
        target = os.path.join(far, "real.txt")
        _raw, raw = attempt(REAL, target, ROOT)
        check(raw is not None,
              "the checkout is on %s and the temp folder on %s, and the raw "
              "call really raises - %s" % (here, there, raw))
        check(attempt(RELPATH.relpath, target, ROOT)
              == (os.path.abspath(target), None),
              "and relpath() answers the whole path")
    else:
        print("  --    this half did not run: the checkout and the temp "
              "folder share one drive here (%s), so only the forced half "
              "above could" % (here or "no drive letters"))

    print()
    print("5. ONE RULE, ASKED - NOT COPIED")
    print("   Each of these carried its own try/except. Now each must ask.")
    import heron_fragment as FRAG
    api = load("api_changes", "api-changes.py")
    lic = load("check_licence", "check-licence.py")
    rule = RELPATH.relpath
    RELPATH.relpath = lambda path, start: ("asked", path, start)
    try:
        check(FRAG.repo_relative("p") == ("asked", "p", FRAG.ROOT),
              "brain/heron_fragment.py repo_relative() asks it, with its "
              "own ROOT")
        check(api is not None and api.short("p") == ("asked", "p", api.ROOT),
              "tools/api-changes.py short() asks it - the copy it grew on "
              "2026-09-22 is gone")
        check(lic is not None
              and lic.repo_relative("p") == ("asked", "p", lic.ROOT),
              "tools/check-licence.py repo_relative() asks it - so is the "
              "copy it kept to avoid PyYAML")
        moved = FRAG.ROOT
        FRAG.ROOT = far
        try:
            check(FRAG.repo_relative("p") == ("asked", "p", far),
                  "and ROOT is read when it is called, so a suite that "
                  "moves ROOT is obeyed")
        finally:
            FRAG.ROOT = moved
    finally:
        RELPATH.relpath = rule

    print()
    print("6. tools/prove-skill.py --jobs ON ANOTHER DRIVE")
    print("   It raised after writing the first job file, and wrote none of")
    print("   the rest: a half-written folder and a traceback.")
    ps = load("prove_skill", "prove-skill.py")
    check(ps is not None, "tools/prove-skill.py loads")
    if ps is not None:
        jobs = os.path.join(far, "jobs")
        with another_drive(far):
            code, said, raised = spoken(
                ps.main, ["prove-skill.py", "--plan-only", "--jobs", jobs])
        wrote = sorted(os.listdir(jobs)) if os.path.isdir(jobs) else []
        skills = len(ps.SKILL.load_all()[0])
        named = [line.split("MODEL HALF:", 1)[1].strip()
                 for line in said.splitlines() if "MODEL HALF:" in line]
        check(raised is None,
              "it does not raise%s" % ("" if raised is None
                                       else " - it raised " + raised))
        check(code == 0, "it exits 0, and it exited %r" % (code,))
        check(skills > 1 and len(wrote) == skills,
              "and it writes a job for every skill, not just the first - "
              "%d of %d" % (len(wrote), skills))
        check(len(named) == len(wrote) and all(inside(one, jobs)
                                               for one in named),
              "each named whole, since none of them has a relative form")

    print()
    print("7. tools/build-release-assets.py --out ON ANOTHER DRIVE")
    print("   It raised after checksums.txt, the completion mark, was")
    print("   written - a finished release, reported as a failed one.")
    bra = load("build_release_assets", "build-release-assets.py")
    check(bra is not None, "tools/build-release-assets.py loads")
    if bra is not None:
        out = os.path.join(far, "dist")

        def stand_in(path, what):
            with io.open(path, "wb") as handle:
                handle.write(what)
            return path

        # THE BUILDS ARE STOOD IN FOR, AND NOTHING AFTER THEM. What is under
        # test is the tail of main() - the checksums and the line naming the
        # folder - and a real build of every product costs most of an hour.
        bra.build = lambda product, release: None
        bra.pack = lambda product, release, out_dir: stand_in(
            os.path.join(out_dir, bra.asset_name(product, release)),
            b"a built add-in")
        bra.workspace = lambda out_dir: stand_in(
            os.path.join(out_dir, bra.WORKSPACE_NAME), b"the workspace")
        which = shutil.which
        shutil.which = lambda name, *a, **k: (
            name if name == "dotnet" else which(name, *a, **k))
        try:
            with another_drive(far):
                code, said, raised = spoken(
                    bra.main, ["build-release-assets.py", "--out", out,
                               "--releases", bra.NET.RELEASES[-1]])
        finally:
            shutil.which = which
        check(raised is None,
              "it does not raise%s" % ("" if raised is None
                                       else " - it raised " + raised))
        check(code == 0, "it exits 0, and it exited %r" % (code,))
        check(os.path.isfile(os.path.join(out, "checksums.txt")),
              "checksums.txt is there, so the folder is complete")
        check(os.path.abspath(out) in said,
              "and the closing line names the folder whole, since it has no "
              "relative form")

    print()
    print("8. THE TWO TOOLS THAT MUST RUN WITHOUT PyYAML STILL DO")
    code, said, err = run_tool("build-release-assets.py", "--list")
    check(code == 0,
          "tools/build-release-assets.py --list runs with PyYAML blocked - "
          ".github/workflows/release.yml installs none, so the day this "
          "imports heron_fragment every release breaks and no pull request "
          "gate sees it%s" % ("" if code == 0 else " - " + last(err)))
    code, said, err = run_tool("check-licence.py")
    check("unit(s) checked" in said and "Traceback" not in err,
          "tools/check-licence.py runs with PyYAML blocked - the promise its "
          "own docstring makes, kept now that it asks heron_relpath%s"
          % ("" if "Traceback" not in err else " - " + last(err)))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a path shown to a person never raises, on any drive, and")
    print("the rule has one home that needs nothing installed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
