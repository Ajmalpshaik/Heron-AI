#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The five-field header, the two-way audit, and the numbers that must agree.

    python tests/test_check_metadata.py

`tools/check-metadata.py` is the working prototype of HERON-STD-MET-014 and
one of the four gates AGENTS.md tells you to run before claiming anything.
It is the third of the seven CI gates that had never been opened, and this
is the first suite it has ever had.

A NEGATIVE RESULT. Read end to end 2026-09-22, 310 lines, and nothing was
found wrong. This suite exists so the reading does not have to be done a
third time, and so the eight things it does can be seen to fail.

FOUR THINGS WERE CHECKED BY MEASURING RATHER THAN BY READING, and all four
hold. `CURRENT_STEP = 6` is not stale: every step number in the registry is
6 or less, and steps one to six are each fully implemented or delegated, so
there is nothing to raise it to. No registry row is silently skipped for
having too few columns. No `.cs`, `.py` or `.ps1` file outside SOURCE_ROOTS
carries a Heron header. And the third place a version is stated -
`platform/heron-products.json` - is not unchecked: `check-products.py`
compares it against `Directory.Build.props` itself, which is the house rule
that one gate owns one question.

EVERY CASE BUILDS ITS OWN REPOSITORY, in a temp folder, and chdirs into it -
the tool reads `docs/28-agent-registry.md` and the source roots by RELATIVE
path. A suite that ran in place would be asserting today's 250 agents.

WHAT IT CANNOT DO: it does not say whether the registry is RIGHT. That is
the owner's, and row 5b-83 is open on a neighbouring question.

    python tests/test_check_metadata.py
"""

import contextlib
import importlib.util
import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-metadata.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_metadata", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


HEADER = """# Heron-Agent:  %s
# Heron-Step:   1
# Heron-Status: %s
# Heron-Since:  0.1.0
# Heron-Layer:  %s
# See docs/29-metadata-standard.md
"""

REGISTRY = """# 28. Agent registry

**Phase 0 and Phase 1 need %d of these** - the ones carrying a step number.

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-AAA-BBB-001` | First | does a thing | T1 | READ | 1 |
| `HERON-CCC-DDD-002` | Second | does another | T1 | READ | 1 |
| `HERON-EEE-FFF-003` | Third | carries no step | T1 | READ | - |
"""


def repo(source="", registry=None, props="0.1.0", client="0.1.0",
         claim=2, extra=None):
    """One little repository, shaped the way the tool reads one."""
    files = {
        "docs/28-agent-registry.md": (registry if registry is not None
                                      else REGISTRY % claim),
        "Directory.Build.props":
            "<Project>\n  <PropertyGroup>\n    <Version>%s</Version>\n"
            "  </PropertyGroup>\n</Project>\n" % props,
        "mcp/client/heron_bridge_client.py":
            HEADER % ("none", "DRAFT", "bridge")
            + 'HERON_VERSION = "%s"\n' % client,
    }
    if source:
        files["brain/heron_thing.py"] = source
    files.update(extra or {})
    return files


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/check-metadata.py loads")
    runner = getattr(tool, "main", None) if tool else None
    check(callable(runner), "and it still has main()")
    if not callable(runner):
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    home = tempfile.mkdtemp(prefix="heron-metadata-test-")
    was_dir = os.getcwd()
    was_host, was_step = dict(tool.HOST_PROVIDED), tool.CURRENT_STEP
    try:
        def run(files, host=None, step=None):
            """(exit code, what it printed) inside a tree built here.

            HOST_PROVIDED IS EMPTIED BY DEFAULT, and that is not a
            convenience. It names nine agents of the real registry, and every
            one of them would be reported against a registry written here -
            correctly, since a delegation covering an agent that is not there
            is exactly what section 4 checks. Leaving it in would make every
            case below fail for that reason and prove nothing about the one
            it is asking.
            """
            place = tempfile.mkdtemp(dir=home)
            for rel, body in files.items():
                path = os.path.join(place, *rel.split("/"))
                if not os.path.isdir(os.path.dirname(path)):
                    os.makedirs(os.path.dirname(path))
                io.open(path, "w", encoding="utf-8", newline="").write(body)
            tool.HOST_PROVIDED = {} if host is None else host
            if step is not None:
                tool.CURRENT_STEP = step
            said = io.StringIO()
            os.chdir(place)
            try:
                with contextlib.redirect_stdout(said):
                    code = runner()
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            finally:
                os.chdir(was_dir)
                tool.HOST_PROVIDED = dict(was_host)
                tool.CURRENT_STEP = was_step
            return code, said.getvalue()

        print("1. A clean tree is clean, and says the numbers it derived")
        code, spoke = run(repo(HEADER % ("HERON-AAA-BBB-001", "DRAFT", "brain")))
        check(code == 0 and "Metadata clean." in spoke,
              "it exits 0 and says so, and it exits %r" % code)
        check("Registry: 3 agents" in spoke, "it counted the registry rows")
        check("Assigned to a step:   2" in spoke,
              "and the two rows carrying a step number")
        check("Agents implemented:   1" in spoke,
              "and the one an actual file claims")

        print()
        print("2. The five fields, each one of them")
        # NAMED HERE, NOT READ FROM THE TOOL. Iterating `tool.FIELDS` would
        # test the tool against its own opinion: drop a field from that list
        # and the suite stops asking about it, which is how this section
        # stayed green while `Heron-Since` was removed - caught 2026-09-22
        # while proving the suite has teeth. The five are docs/29's standard.
        FIVE = ["Heron-Agent", "Heron-Step", "Heron-Status", "Heron-Since",
                "Heron-Layer"]
        check(sorted(tool.FIELDS) == sorted(FIVE),
              "the tool asks for exactly the five docs/29 names, and it asks "
              "for %r" % (sorted(tool.FIELDS),))
        for field in FIVE:
            body = "\n".join(l for l in (HEADER % ("none", "DRAFT", "brain")).split("\n")
                             if field not in l)
            code, spoke = run(repo(body))
            check(code == 1 and field in spoke and "missing" in spoke,
                  "a file with no %s is a problem" % field)

        print()
        print("3. A status and a layer have to be real ones")
        code, spoke = run(repo(HEADER % ("none", "NEARLY", "brain")))
        check(code == 1 and "not a lifecycle stage" in spoke,
              "a status that is not a lifecycle stage is a problem")
        code, spoke = run(repo(HEADER % ("none", "DRAFT", "middleware")))
        check(code == 1 and "Heron-Layer" in spoke,
              "and a layer that is not one of the six")

        print()
        print("4. THE AUDIT RUNS IN BOTH DIRECTIONS")
        code, spoke = run(repo(HEADER % ("HERON-ZZZ-YYY-999", "DRAFT", "brain")))
        check(code == 1 and "not in the registry" in spoke,
              "a file claiming an agent the registry does not have is a "
              "problem")
        code, spoke = run(repo(HEADER % ("none", "DRAFT", "brain")),
                          host={"HERON-ZZZ-YYY-999": "nobody"})
        check(code == 1 and "HOST_PROVIDED lists" in spoke,
              "and so is a delegation covering an agent that is not there - "
              "an exemption for nothing is the shape a rename leaves behind")
        code, spoke = run(repo(HEADER % ("none", "DRAFT", "brain")))
        check("no file implements them" in spoke,
              "a registry agent due by now with no file is NAMED, and not as "
              "an error - it is the to-do list for finishing the step")
        code, spoke = run(repo(HEADER % ("none", "DRAFT", "brain")),
                          host={"HERON-AAA-BBB-001": "the host does this"})
        check(code == 0 and "Provided by the host" in spoke,
              "while one the host provides on purpose is named separately "
              "and is not a gap - silence would make those two look alike")
        todo = []
        for line in spoke.split("no file implements them")[-1].split("\n"):
            if line.startswith("  - HERON-"):
                todo.append(line.split()[1])
            elif line.startswith("  Not an error"):
                break
        check(todo == ["HERON-CCC-DDD-002"],
              "and it is not ALSO on the to-do list, which reads %r" % todo)

        print()
        print("5. Two places, one version number")
        code, spoke = run(repo(HEADER % ("none", "DRAFT", "brain"),
                               props="0.2.0", client="0.1.0"))
        check(code == 1 and "version disagreement" in spoke,
              "the build and the bridge client disagreeing is a problem - "
              "Heron would report one number and stamp another")

        print()
        print("6. A CLAIM ABOUT HOW MANY AGENTS PHASE 0 AND 1 NEED")
        # Three different figures for one set were in circulation at once,
        # each typed and none derived. So the sentence is checked against
        # the rows, and its ABSENCE is checked too.
        code, spoke = run(repo(HEADER % ("none", "DRAFT", "brain"), claim=45))
        check(code == 1 and "but 2 rows carry a step number" in spoke,
              "a claim that disagrees with the rows is a problem")
        bare = REGISTRY % 2
        bare = bare.replace("**Phase 0 and Phase 1 need 2 of these** - the "
                            "ones carrying a step number.\n", "")
        code, spoke = run(repo(HEADER % ("none", "DRAFT", "brain"),
                               registry=bare))
        check(code == 1 and "guarding nothing" in spoke,
              "and so is the claim going missing, because a check nothing "
              "triggers is a check that passes for the wrong reason")

        print()
        print("7. ONE PLACE PER FACT - a fragment is not Heron's own source")
        # A Heron- header in a fragment's .cs would sit beside `status:` in
        # its .yaml, and the day one is promoted the other goes stale.
        code, spoke = run(repo(HEADER % ("none", "DRAFT", "brain"),
                               extra={"brain/fragments/x/Impl.cs":
                                      "// no header at all\n"}))
        check(code == 0,
              "a fragment implementation with no header is not this "
              "checker's business")

        print()
        print("8. A registry it cannot read is a FAILURE, not an empty audit")
        code, spoke = run({"Directory.Build.props":
                           "<Project><Version>0.1.0</Version></Project>\n"})
        check(code == 1 and "could not read" in spoke,
              "no registry exits 1 and says so, rather than auditing nothing "
              "and reporting it clean")
    finally:
        os.chdir(was_dir)
        tool.HOST_PROVIDED = dict(was_host)
        tool.CURRENT_STEP = was_step
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - every field, both directions of the audit, and every")
    print("number it publishes derived from the rows underneath it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
