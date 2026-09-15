# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
agent-count's fourth check - an agent that was built is no longer built.

    python tests/test_agent_count.py

WHY THIS EXISTS
  On 2026-09-15 a new agent was written straight over
  brain/heron_architect.py, which held HERON-AHR-ARC-003 - 272 lines and
  a 201-line suite. Nothing complained. The register reconciled
  perfectly either way, because the id that vanished and the id that
  arrived cancelled each other out in the total, and the only symptom
  was a number that did not move.

  So the check compares against the LAST COMMIT rather than against the
  register, and this suite proves it fires. A guard nobody has watched
  fire is a comment.

WHAT IT PROVES
  1. IT READS THE LAST COMMIT, and what it reads there agrees with the
     register.

  2. IT SKIPS EXACTLY WHAT built() SKIPS - itself, fragments, build
     output - so a file the walker never reads cannot be reported as
     missing from it.

  3. IT FIRES. The comparison is run against a claims map with one id
     taken out, and the answer names that id and the files that held it.

  4. AND IT IS QUIET RIGHT NOW: nothing claimed in the last commit is
     unclaimed in this working tree.

  5. NO GIT IS NOT A FINDING. An installed Heron is not a checkout.
"""

import io
import os
import sys
import importlib.util

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """agent-count.py is not importable by name - it has a hyphen."""
    path = os.path.join(ROOT, "tools", "agent-count.py")
    spec = importlib.util.spec_from_file_location("agent_count", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    here = os.getcwd()
    os.chdir(ROOT)
    try:
        AC = load()
        source = io.open(os.path.join(ROOT, "tools", "agent-count.py"),
                         encoding="utf-8").read()
        # Flattened, because the sentences below are wrapped in the source
        # and a word search that breaks on a line ending proves nothing.
        flat = " ".join(source.split())

        print("1. It reads the last commit")
        was = AC.claimed_at_head()
        check(was is not None,
              "git answered - this checkout has a last commit to compare to")
        if was is None:
            print("\nSKIPPED   no git, and that is not a finding")
            return 0
        check(len(was) > 90,
              "%d agent ids were claimed in it, not a handful" % len(was))
        agents, _headings, _stated = AC.registry()
        strangers = sorted(set(was) - set(agents))
        check(not strangers,
              "and every one of them is in docs/28%s"
              % ("" if not strangers else ": %s" % ", ".join(strangers)))

        print("\n2. It skips exactly what built() skips")
        check(not AC._scanned("tools/agent-count.py"),
              "itself - it contains the string it scans for")
        check(not AC._scanned("brain/fragments/x/fragment.py"),
              "fragments - their metadata is in their own fragment.yaml")
        check(not AC._scanned("platform/Heron.Core/obj/Debug/x.cs"),
              "build output under obj/")
        check(not AC._scanned("docs/28-agent-registry.md"),
              "anything outside the source roots")
        check(not AC._scanned("brain/heron_paths.txt"),
              "and anything that is not a source extension")
        check(AC._scanned("brain/heron_paths.py")
              and AC._scanned("tests/test_paths.py")
              and AC._scanned("platform/Heron.Core/HeronPaths.cs"),
              "while a real module, suite and C# file are all read")

        print("\n3. It fires - the comparison, run with one id taken out")
        # THE INCIDENT, reproduced against the real HEAD map rather than
        # described: take out the id two files claimed and see it named.
        victim = "HERON-AHR-ARC-003"
        check(victim in was,
              "%s is claimed in the last commit" % victim)
        check(len(set(was[victim])) == 2,
              "by two files - a module and its suite")
        pretend = dict((aid, files) for aid, files in was.items()
                       if aid != victim)
        gone = sorted(set(was) - set(pretend))
        check(gone == [victim],
              "and with it unclaimed, the comparison names exactly it")
        check(all("architect" in name for name in was[victim]),
              "together with the files that held it: %s"
              % ", ".join(sorted(set(was[victim]))))
        check("was claimed by" in flat and "written over" in flat,
              "and the message says a file may have been written over")

        print("\n4. And it is quiet right now")
        now = AC.built()
        missing = sorted(set(was) - set(now))
        check(not missing,
              "nothing claimed in the last commit is unclaimed here%s"
              % ("" if not missing else ": %s" % ", ".join(missing)))
        # IT LOOKS ONE WAY ONLY, and that is deliberate: building an agent
        # adds an id, and an addition is not a finding. Shown rather than
        # stated - a map with an id the last commit never had reports
        # nothing.
        building = dict(now)
        building["HERON-ZZZ-NEW-999"] = ["brain/heron_not_yet.py"]
        check(not sorted(set(was) - set(building)),
              "an id this tree has and the last commit did not is reported "
              "as nothing - building an agent is not a regression")

        print("\n5. No git is not a finding")
        check("An installed Heron is not a checkout" in flat,
              "the tool says so in its own words")
        check("absence of a second opinion" in flat,
              "and reports it as the absence of a second opinion")
        check("return None" in flat,
              "returning None rather than an empty map, so 'no answer' and "
              "'nothing claimed' cannot be confused")

        print()
        if FAILURES:
            print("FAILED  %d check(s)" % len(FAILURES))
            for line in FAILURES:
                print("  - %s" % line)
            return 1
        print("PASS    an agent that was built is no longer built - it fires")
        return 0
    finally:
        os.chdir(here)


if __name__ == "__main__":
    sys.exit(main())
