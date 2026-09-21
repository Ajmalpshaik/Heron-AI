# Heron-Agent:  none
# Heron-Step:   13
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
One sweep over everything built, sorted into what is UNFINISHED and what is
merely WAITING FOR A MACHINE.

    python tools/check-gaps.py

WHY THE SPLIT IS THE WHOLE POINT
--------------------------------
This repository has one recurring failure and it is not bugs: it is an unproven
claim quietly ageing into a believed one. Two very different things look
identical in a list of open items -

    "nobody has finished this"        somebody should, today
    "nobody has a Revit to test it"   nobody can, until the machine

- and a report that mixes them teaches its reader to skim, at which point the
first kind hides inside the second. So they are counted separately, and the
exit code follows ONLY the first: this tool fails while work remains that could
be done here, and passes when the only thing left is a machine.

WHAT IT IS NOT
--------------
Not a test. Everything here is read off the repository as it stands - the build
order, the agent registry, the fragment library, the register - and compared
against itself. It cannot tell you whether anything WORKS; tests do that, and
this counts whether they exist and pass.
"""

import io
import os
import re
import subprocess
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

# A suite exits 3 when it could not run for want of an optional
# dependency. Kept as a name in both files rather than as a bare 3.
SKIPPED = 3

# How long one suite may take before this tool stops waiting for it.
#
# This sweep runs every suite serially, so it costs the sum of all of them -
# around eight minutes on the machine this was measured on, which is slow but
# is NOT the defect. The defect was that a suite which never returns hangs this
# tool for ever, with no output naming it: the reader sees a dead terminal and
# cannot tell a hung gate from a slow one. A gate that cannot say why it
# stopped cannot certify anything.
#
# 300s is roughly four times the slowest suite measured here
# (test_brain_reachable.py, 78s on 2026-09-10), so a healthy suite on a slower
# machine still finishes well inside it. A suite that exceeds it is reported
# UNFINISHED, because a test that hangs is work somebody can do today - which
# is exactly the line this whole tool is drawn along.
SUITE_TIMEOUT = 300

UNFINISHED = []      # could be done here, today
WAITING = []         # genuinely needs a machine this is not


def unfinished(what):
    UNFINISHED.append(what)


def waiting(what, needs):
    WAITING.append((what, needs))


def read(*parts):
    path = os.path.join(ROOT, *parts)
    if not os.path.exists(path):
        return ""
    return io.open(path, encoding="utf-8", errors="replace").read()


# ---------------------------------------------------------------------------

def check_steps():
    """Every step the build order describes, against what is on disk."""
    print("BUILD ORDER - is each step's code and test actually there")
    order = read("docs", "27-build-order.md")

    # Phase 2's steps name their own module and test in brain/README.md; the
    # build order names the step. Cross-read rather than hardcode a list, so a
    # new step is picked up without editing this file.
    steps = re.findall(r"^### Step (\d+) — (.+?)(?:\s*\*|$)", order, re.M)
    steps += re.findall(r"^## Step (\d+) — (.+?)(?:\s*\*|$)", order, re.M)
    numbers = sorted(set(int(n) for n, _t in steps))
    print("  %d steps described (%s)" % (len(numbers),
                                         ", ".join(str(n) for n in numbers)))

    built = {}
    for name in sorted(os.listdir(os.path.join(ROOT, "brain"))):
        if not name.endswith(".py"):
            continue
        header = read("brain", name)
        found = re.search(r"^# Heron-Step:\s*(\d+)", header, re.M)
        if found:
            built.setdefault(int(found.group(1)), []).append("brain/" + name)

    for name in sorted(os.listdir(os.path.join(ROOT, "tests"))):
        if not name.endswith(".py"):
            continue
        found = re.search(r"^# Heron-Step:\s*(\d+)", read("tests", name), re.M)
        if found:
            built.setdefault(int(found.group(1)), []).append("tests/" + name)

    for step in numbers:
        if step <= 6:
            continue                    # Phase 0/1, tracked by NEEDS-CHECKING
        files = built.get(step, [])
        has_code = any(f.startswith("brain/") for f in files)
        has_test = any(f.startswith("tests/") for f in files)
        if not has_code:
            unfinished("Step %d has no module in brain/" % step)
        elif not has_test:
            unfinished("Step %d has code but no test" % step)
        else:
            print("  ok    Step %-2d %s" % (step, ", ".join(files)))


# How much of a failing suite's own output to show. Enough for the FAILED
# block every suite here ends with, and short enough that ten failures do not
# bury the sections after this one.
FAIL_LINES = 25


def check_tests():
    """Every test suite, run."""
    print()
    print("TESTS - every suite, actually run")
    print("  (serial, so this section costs the sum of them - several minutes)",
          flush=True)
    section_started = time.time()
    folder = os.path.join(ROOT, "tests")
    for name in sorted(os.listdir(folder)):
        if not name.startswith("test_") or not name.endswith(".py"):
            continue
        started = time.time()
        try:
            # CAPTURED, NOT DISCARDED, AND THE REASON IS SIX RED CI RUNS.
            # This ran every suite with stdout and stderr at DEVNULL and
            # recorded only WHICH file failed. When
            # tests/test_fragment_tracking.py failed on Linux and passed on
            # Windows, that left nobody a way to see why: three sections were
            # made skippable to bisect it, the file was renamed once, and in
            # the end it was deleted - and the answer, when somebody finally
            # ran the file by hand on Linux, was one line of its output
            # (FRAGMENT-ISSUES rows 160 and 166). Holding the text of a suite
            # that PASSED would be noise; holding the text of one that failed
            # costs nothing and is the whole diagnosis.
            proc = subprocess.run([sys.executable, os.path.join(folder, name)],
                                  stdout=subprocess.PIPE,
                                  stderr=subprocess.STDOUT, cwd=ROOT,
                                  timeout=SUITE_TIMEOUT)
        except subprocess.TimeoutExpired:
            # Named, not silent. The whole point of the bound is that the
            # reader learns WHICH suite stopped returning.
            print("  HUNG  %s - still running after %ds, gave up"
                  % (name, SUITE_TIMEOUT), flush=True)
            unfinished("%s DOES NOT RETURN within %ds"
                       % (name, SUITE_TIMEOUT))
            continue
        took = time.time() - started
        if proc.returncode == 0:
            print("  ok    %s (%.1fs)" % (name, took), flush=True)
        elif proc.returncode == SKIPPED:
            # A suite that could not run for want of an OPTIONAL dependency,
            # not one that failed. It is neither: reporting it `ok` would be a
            # green nobody earned, and reporting it UNFINISHED would put a
            # missing pip package in the same list as unwritten code.
            #
            # This exists because the exit code is the only thing a suite
            # can say WITHOUT being read: "I skipped" has to survive being
            # reduced to a number. It used to be the only thing that crossed
            # at all - stdout went to DEVNULL until rows 160 and 166, and the
            # comment above records what that cost. test_mcp_serves.py was
            # the first to need the third code: the MCP SDK is not installed
            # on a machine with no Revit.
            print("  wait  %s - skipped, see its own output for why" % name,
                  flush=True)
            waiting("%s could not run here" % name,
                    "an optional dependency this machine does not have")
        else:
            print("  FAIL  %s (%.1fs)" % (name, took), flush=True)
            # WHY, not just which. The last lines are where a suite here puts
            # its verdict - every one of them prints its failures at the end,
            # under FAILED, which is the convention tests/README.md states.
            said = (proc.stdout or b"").decode("utf-8", "replace").strip()
            tail = said.splitlines()[-FAIL_LINES:]
            for line in tail:
                print("          %s" % line, flush=True)
            if not tail:
                print("          (it printed nothing at all)", flush=True)
            unfinished("%s FAILS" % name)
    print("  %.0fs for the suites" % (time.time() - section_started),
          flush=True)


def check_tools():
    """The repository's own checkers."""
    print()
    print("CHECKERS - the repository checking itself")
    # THE FOUR THE SHIP CHECKLIST CALLS "THE FOUR THAT MUST PASS", and this
    # ran THREE of them until 2026-09-21. The missing one was check-package,
    # which is the only thing here that reads Heron.addin - and gates.yml
    # says why that matters in its own words: "a manifest fault costs a
    # modeller their whole add-in while every other gate on this page stays
    # green." It needs nothing but Python and costs about a tenth of a
    # second, so there was never a reason for it to be absent; it simply was.
    # FRAGMENT-ISSUES row 5b-58.
    #
    # The list is typed rather than derived on purpose: these four are the
    # ones a non-zero exit from means YOUR CHANGE, which is a judgement the
    # ship checklist makes and this file follows.
    #
    # WHAT THIS SAID UNTIL 2026-09-21, AND IT WAS FALSE: "the other checkers
    # in CI are reports, and a report's finding is a question for a person."
    # gates.yml's job named "The gates that must pass" runs NINE commands
    # under bash -e - these four plus check-signatures, check-licence,
    # check-narrow-errors, check-routing and check-intrusion - so a non-zero
    # from any of them fails the pull request exactly as these four do.
    # Measured: check-routing exited 2 on a head whose four were all green.
    #
    # THE FIVE ARE STILL NOT SWEPT HERE, AND THAT IS A CHOICE RATHER THAN AN
    # OVERSIGHT THIS TIME. check-routing and check-intrusion need a knowledge
    # store and rebuild one when it is stale, which is minutes rather than
    # tenths of a second, and this sweep is already long enough that people
    # skip it. Adding them needs the same four-state care the tests got - a
    # store that is absent is not a gap in the code - and that is its own
    # change. FRAGMENT-ISSUES row 5b-70.
    for name in ("check-docs.py", "check-metadata.py", "check-structure.py",
                 "check-package.py"):
        proc = subprocess.run([sys.executable, os.path.join(ROOT, "tools", name)],
                              stdout=subprocess.DEVNULL,
                              stderr=subprocess.DEVNULL, cwd=ROOT)
        if proc.returncode == 0:
            print("  ok    %s" % name)
        else:
            unfinished("%s reports drift" % name)


def check_agents():
    """Agents claimed by code, and agents a built step should have produced."""
    print()
    print("AGENTS - what the code claims, against the registry")
    registry = read("docs", "28-agent-registry.md")
    known = set(re.findall(r"`(HERON-[A-Z]+-[A-Z]+-\d+)`", registry))

    # THE SAME SIX ROOTS, THE SAME THREE EXTENSIONS AND THE SAME WALK AS
    # tools/agent-count.py, deliberately, because for a long time it was none
    # of those and nothing said so.
    #
    # This listed EIGHT named folders flat, with os.listdir, over `.py` and
    # `.cs` only. `.ps1` is a source extension in this repository -
    # check-metadata.py's own SOURCE_EXT says so - and two agents are declared
    # in PowerShell: HERON-INS-ORC-001 in tools/setup.ps1 and
    # HERON-REVIT-DEP-024 in tools/deploy-addin.ps1. Both were invisible here
    # and visible to every other scanner, so this reported 225 where
    # agent-count.py reported 227, for however long, while failing nothing.
    #
    # The flat listing was the same defect not yet firing: a module added under
    # brain/agents/, a command under revit/Heron.Revit.Addin/Commands/, or any
    # project not on that hardcoded list of eight would have been counted
    # everywhere else and silently not counted here. Fixed together, 2026-09-17,
    # because they are one mistake - a scan whose reach is narrower than the
    # claim it is read as.
    claimed = {}
    for root in ("revit", "mcp", "brain", "platform", "tests", "tools"):
        base = os.path.join(ROOT, root)
        if not os.path.isdir(base):
            continue
        for here, dirs, files in os.walk(base):
            dirs[:] = sorted(d for d in dirs
                             if d not in ("__pycache__", "bin", "obj", ".vs"))
            for name in sorted(files):
                if not name.endswith((".py", ".cs", ".ps1")):
                    continue
                path = os.path.join(here, name)
                rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
                if rel.startswith("brain/fragments/"):
                    # A fragment carries its metadata in its own fragment.yaml,
                    # for the same reason check-metadata.py skips it here: one
                    # place per fact. Nothing under it declares an agent.
                    continue
                if rel == "tools/check-gaps.py":
                    # Skip this file. It contains the pattern it is searching
                    # for, and on its first run it duly reported its own regex
                    # as two undeclared agents - a scanner that scans itself
                    # finds itself.
                    continue
                text = io.open(path, encoding="utf-8", errors="replace").read()
                # Anchored to the start of a comment line, so a mention of the
                # field inside code or prose is not read as a declaration.
                for line in re.findall(r"^\s*(?://|#)\s*Heron-Agent:\s*(.+)$",
                                       text, re.M):
                    for agent in [a.strip() for a in line.split(",")]:
                        if agent and agent != "none":
                            claimed.setdefault(agent, []).append(rel)

    unknown = sorted(a for a in claimed if a not in known)
    for agent in unknown:
        unfinished("%s is claimed by %s but is not in the registry"
                   % (agent, ", ".join(claimed[agent])))
    print("  %d agent id(s) claimed by code, %d known to the registry"
          % (len(claimed), len(known)))
    if not unknown:
        print("  ok    every claimed agent exists in the registry")


def check_fragments():
    """The library: well-formed, and how much of it is proven."""
    print()
    print("FRAGMENTS - the library itself")
    try:
        import heron_fragment as FRAG
    except ImportError as exc:
        unfinished("brain/heron_fragment.py will not import: %s" % exc)
        return

    found, problems = FRAG.load_all()
    for line in problems:
        unfinished("fragment library: %s" % line)

    broken = 0
    unproven = []
    for frag in found.values():
        bad = FRAG.validate(frag)
        if bad:
            broken += 1
            unfinished("fragment %s: %s" % (frag.slug, bad[0]))
        if frag.status not in ("PROVEN", "PRODUCTION"):
            unproven.append(frag.id)

    print("  %d fragment(s), %d well-formed" % (len(found), len(found) - broken))
    if unproven:
        waiting("%d fragment(s) below PROVEN: %s"
                % (len(unproven), ", ".join(sorted(unproven))),
                "a real Revit - D-30 needs a proof with a negative case")


def check_capabilities_and_graph():
    """The registry and the graph, over the real library."""
    print()
    print("CAPABILITIES AND GRAPH")
    import tempfile, shutil
    home = tempfile.mkdtemp(prefix="heron-gaps-")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        import heron_scope as SCOPE
        import heron_capability as CAP
        import heron_graph as GRAPH

        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            CAP.rebuild(store)
            for line in CAP.problems(store):
                unfinished("capability registry: %s" % line)
            for name, why in CAP.gaps(store):
                unfinished("capability %s has no provider (%s)" % (name, why))

            found = GRAPH.orphans(store)
            for fragment_id, why in found:
                unfinished("orphan %s: %s" % (fragment_id, why))

            caps = sorted(set(r["capability"] for r in store.fragments()))
            print("  %d capability(ies), %d orphan(s)" % (len(caps), len(found)))
            if not found:
                print("  ok    every filter has a consumer, every action a feed")
        finally:
            store.close()
    finally:
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)


def check_brain_is_reachable():
    """
    Is anything the brain built actually CALLABLE from a conversation?

    Every other section here asks whether a thing exists. This one asks whether
    it is wired to anything, and it exists because the answers differed: on
    2026-08-29 this tool reported everything clean while all eight Phase 2
    modules, seven fragments and ten skills sat unreachable - built, tested,
    and imported by nothing but their own tests.

    The host (Claude Code) is the Orchestrator - D-01, and docs/02 s7 - so it
    reaches Heron ONLY through the MCP tool surface. If no tool consults the
    capability registry, then no request can resolve through a capability, and
    Phase 2's third definition-of-done clause is open however complete the
    modules underneath are.

    A checker built from a build order can only ask whether the build order was
    followed. That is what the previous sections do, and it is why they all
    passed.
    """
    print()
    print("THE BRAIN - built is not the same as reachable")

    brain_modules = sorted(
        n[:-3] for n in os.listdir(os.path.join(ROOT, "brain"))
        if n.startswith("heron_") and n.endswith(".py"))

    callers = []
    for folder in ("mcp/client", "mcp/server", "platform/Heron.Core",
                   "revit/Heron.Bridge", "revit/Heron.Revit.Addin"):
        full = os.path.join(ROOT, *folder.split("/"))
        if not os.path.isdir(full):
            continue
        for name in sorted(os.listdir(full)):
            if not name.endswith(".py"):
                continue
            text = read(*(folder.split("/") + [name]))
            # An import, not a mention: a comment naming a module is not a
            # call site, and this file itself is the reason to be strict.
            for module in brain_modules:
                if re.search(r"^\s*(?:import|from)\s+%s\b" % re.escape(module),
                             text, re.M):
                    callers.append("%s/%s -> %s" % (folder, name, module))

    print("  %d brain module(s), %d import(s) of them outside brain/ and tests/"
          % (len(brain_modules), len(callers)))
    if callers:
        for line in callers:
            print("  ok    %s" % line)
    else:
        unfinished(
            "the brain is unreachable from the host: %d module(s) in brain/ "
            "and not one is imported by mcp/ - so no MCP tool can resolve a "
            "request through a capability, which is Phase 2's third "
            "definition-of-done clause. Needs no Revit" % len(brain_modules))


def check_register():
    """NEEDS-CHECKING, split the same way this tool splits everything."""
    print()
    print("THE REGISTER - what it says is left")
    text = read("docs/NEEDS-CHECKING.md")
    rows = re.findall(r"^\| (~~)?\*\*([A-Z]\d+[a-z]?)\*\*(~~)?\s*\|(.*)$",
                      text, re.M)
    done = [r for r in rows if r[0]]
    left = [r for r in rows if not r[0]]
    print("  %d row(s), %d done, %d left" % (len(rows), len(done), len(left)))

    for _s, item, _e, body in left:
        group = item[0]
        if group == "R":
            waiting("%s - a conversation at the PC" % item, "the owner")
        elif item in ("A4", "A6", "A8"):
            # A8 is the MCP tool surface being SERVED, not just declared. It
            # needs Windows and Claude Code and no Revit at all - filing it
            # under "needs a real Revit" would hide a row that a PC session
            # could clear in two minutes.
            waiting("%s - %s" % (item, body.split("|")[0].strip()[:60]), "Windows")
        elif item == "A7":
            waiting("%s - the trained embedding backend" % item,
                    "a network that can reach a model host")
        else:
            waiting("%s - %s" % (item, body.split("|")[0].strip()[:60]),
                    "a real Revit")


def main():
    print("Heron - what is unfinished, and what is only waiting")
    print("=" * 62)
    check_steps()
    check_tests()
    check_tools()
    check_agents()
    check_fragments()
    check_capabilities_and_graph()
    check_brain_is_reachable()
    check_register()

    print()
    print("=" * 62)
    if UNFINISHED:
        print("UNFINISHED - %d thing(s) that COULD be done here, now:" % len(UNFINISHED))
        for line in UNFINISHED:
            print("  - %s" % line)
    else:
        print("UNFINISHED - nothing. Everything buildable here is built,")
        print("             every test passes, every checker is clean.")

    print()
    by_need = {}
    for what, needs in WAITING:
        by_need.setdefault(needs, []).append(what)
    print("WAITING - %d item(s), and none of them are work anybody can do here:"
          % len(WAITING))
    for needs in sorted(by_need):
        print("  needs %s:" % needs)
        for what in by_need[needs]:
            print("     - %s" % what)

    print()
    print("The exit code follows the UNFINISHED list only. Waiting is not")
    print("failing - but a waiting item is still UNPROVEN, and no number of")
    print("days spent waiting makes D3 any more true.")
    return 1 if UNFINISHED else 0


if __name__ == "__main__":
    sys.exit(main())
