# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Scaffold the next agent from its row in the register.

    python tools/new-agent.py HERON-OPS-QUE-002
    python tools/new-agent.py HERON-OPS-QUE-002 --part mcp --step 16

Writes three files and nothing else:

    brain/agents/<ID>.yaml      the contract, ready to fill in
    <part>/heron_<name>.py      the module, header already correct
    tests/test_<name>.py        the test, which fails until it is written

WHY THIS EXISTS
---------------
146 agents can be built on a machine with no Revit
(docs/work-notes/plans/agent-build-order-2026-09-13.md). Each one needs the
same five-field metadata header, a contract in the same shape, and a test. Done
by hand 146 times that is 146 chances to mistype a layer, invent a field, or
copy a header from a file in a different part - and check-metadata.py finds the
mistake after the work, not before it.

IT IMPLEMENTS NO AGENT OF ITS OWN. `Heron-Agent: none` is the honest answer:
this is scaffolding, and docs/29 says an absent field is a gap while `none` is
a decision. The Agent Builder (HERON-AHR-BLD-004) is a T3 agent that writes the
body; this writes the envelope, which is the part that is identical every time.

IT REFUSES MORE THAN IT WRITES, and that is the point:

    an id that is not in docs/28-agent-registry.md .... it does not exist
    an agent something already claims ................. it is already built
    a file that is already there ...................... nothing is overwritten
    a name that is a PREFIX of an existing module ..... it would shadow it

The first is the rule the register depends on: an agent is in the register
before it is in the code, never the other way round.
"""

import importlib.util
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PARTS = ("brain", "mcp", "platform", "tools")
LAYER = {"brain": "brain", "mcp": "bridge", "platform": "platform",
         "tools": "tool"}
DEFAULT_STEP = 15


def register():
    """
    The register and the claims, read from tools/agent-count.py.

    Loaded by path because the filename has a hyphen. This is the same trick
    agent-count.py itself uses to read HOST_PROVIDED out of check-metadata.py,
    and for the same reason: a second copy of a list is the drift this
    repository keeps having to write about.
    """
    path = os.path.join(ROOT, "tools", "agent-count.py")
    spec = importlib.util.spec_from_file_location("heron_agent_count", path)
    module = importlib.util.module_from_spec(spec)
    cwd = os.getcwd()
    os.chdir(ROOT)                       # it reads docs/ and the parts by relative path
    try:
        spec.loader.exec_module(module)
        agents, _headings, _totals = module.registry()
        claims = module.built()
    finally:
        os.chdir(cwd)
    return agents, claims


def prefix_collisions(part, module):
    """
    Existing modules in `part` that this name shadows, either way round.

    THE RULE IS tests/test_references.py's, claim 5: no module name in
    brain/ may be a prefix of another. It is not cosmetic - HERON-DOC-REF-006
    renames and searches over module names, and the rule is what lets it tell
    a whole name from the start of a longer one. A prefix pair makes every
    rename of the shorter name ambiguous, silently, in the direction that
    edits something nobody asked for.

    Checked in whichever part is being written rather than in brain/ alone.
    Measured 2026-09-17: no part has a prefix pair, so this refuses nothing
    that exists and nothing that is legal today.

    THE TEST FILE IS DELIBERATELY NOT CHECKED, though this tool writes one
    too. tests/ already holds four legal prefix pairs - test_chain and
    test_chain_expectation, test_contract and test_contract_reference,
    test_fragment_needs and test_fragment_needs_reader, test_generate and
    test_generate_jobs - because nothing renames suites by name the way
    HERON-DOC-REF-006 renames modules. Checking there would refuse names
    that are correct today, which is a worse failure than the one this
    prevents.
    """
    mine = "heron_%s" % module
    try:
        names = sorted(name[:-3] for name in os.listdir(os.path.join(ROOT, part))
                       if name.endswith(".py") and not name.startswith("__"))
    except OSError:
        return []
    return [other for other in names
            if other != mine
            and (mine.startswith(other) or other.startswith(mine))]


def module_name(agent_name):
    """'Queue Manager' -> 'queue_manager'. Predictable, and never clever."""
    slug = re.sub(r"[^a-z0-9]+", "_", agent_name.lower()).strip("_")
    slug = re.sub(r"_agent$", "", slug)
    return slug or "agent"


CONTRACT = '''\
# {name} - {agent}
#
# Fill in every field. `python brain/heron_contract.py` refuses this file
# until they are real, which is deliberate: a contract of placeholders is
# worse than none, because it looks like a promise.
#
# NOT DECLARED HERE: tier and risk (docs/28-agent-registry.md owns them),
# status (the module's Heron-Status header owns it).

agent: {agent}
version: 0.1.0

input:
  TODO:
    type: string
    required: true
    description: >
      What this agent is given. Written for the person building the agent
      that will call it.

output:
  TODO:
    type: string
    description: >
      What it promises back. A caller plans around this sentence.

allowed-tools: []

timeout-seconds: 30

failures:
  - TODO_NAME_A_REAL_FAILURE

retry:
  attempts: 0
  on-failures: []
'''

MODULE = '''\
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   {step}
# Heron-Status: DISCOVERED
# Heron-Since:  0.1.0
# Heron-Layer:  {layer}
# See docs/29-metadata-standard.md

"""
{name} - TODO: one line saying what it is FOR, not what it is.

    python {part}/heron_{module}.py

Its contract is brain/agents/{agent}.yaml, and the contract is the promise -
this file is only how the promise is kept today.

CLAIM THE AGENT WHEN THE WORK IS DONE, NOT NOW
----------------------------------------------
The header says `Heron-Agent: none` and `Heron-Status: DISCOVERED` on purpose.
tools/agent-count.py reports an agent as BUILT when a source file claims it, so
a stub that claimed {agent} today would raise the built count for work nobody
has done. DISCOVERED is docs/24's own word for it - identity assigned, nothing
proven.

When this module does what the contract promises, put {agent} in the header's
Heron-Agent field in place of `none`, and raise Heron-Status to DRAFT.

Written as a sentence rather than as the two header lines themselves, because
agent-count.py scans any commented Heron-Agent line in the first 40 lines of a
file - including one quoted inside a docstring. An example of the line WOULD
have been read as a claim, which is the defect this whole section exists to
prevent. check-gaps.py learned the same lesson about the string it scans for.

WHY IT EXISTS
-------------
TODO. Name the thing that goes wrong without it. A module whose docstring
explains what the code already says is a module nobody reads twice.
"""

import sys


def main(argv):
    print("{name} is scaffolded and does nothing yet.")
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
'''

TEST = '''\
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   {step}
# Heron-Status: DISCOVERED
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
{name} - what it must do, asserted.

    python tests/test_{module}.py

WHAT IT PROVES
  1. TODO. One numbered claim per thing the contract promises, and one for
     each failure state the contract declares - a failure nothing exercises
     is a failure nobody has seen happen.

It fails until it is written. That is the honest starting state: a test file
that passes while asserting nothing is how a green gate comes to mean less
than nothing (D-30). Claim {agent} in the header when it asserts something.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "{part}"))


def main():
    print("FAIL  tests/test_{module}.py asserts nothing yet")
    return 1


if __name__ == "__main__":
    sys.exit(main())
'''


def write(path, text, written):
    if os.path.exists(path):
        print("  refused   %s already exists - nothing overwritten"
              % os.path.relpath(path, ROOT).replace(os.sep, "/"))
        return False
    directory = os.path.dirname(path)
    if not os.path.isdir(directory):
        os.makedirs(directory)
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(text)
    written.append(os.path.relpath(path, ROOT).replace(os.sep, "/"))
    return True


def main(argv):
    if len(argv) < 2 or argv[1].startswith("-"):
        print(__doc__.strip().splitlines()[0])
        print("usage: python tools/new-agent.py HERON-XXX-YYY-NNN "
              "[--part %s] [--step N] [--module NAME]" % "|".join(PARTS))
        return 2

    agent = argv[1].strip().upper()
    part = "brain"
    step = DEFAULT_STEP
    override = None
    rest = argv[2:]
    if len(rest) % 2:
        # zip() silently dropped a trailing flag, so `--part` with no value
        # scaffolded three files into the DEFAULT part and said nothing. A
        # malformed command writes nothing and says so.
        print("'%s' has no value. Every option takes one: --part %s, "
              "--step N, --module NAME" % (rest[-1], "|".join(PARTS)))
        return 2
    for flag, value in zip(rest[::2], rest[1::2]):
        if flag == "--part":
            part = value
        elif flag == "--step":
            step = value
        elif flag == "--module":
            override = value
        else:
            print("unknown option '%s'" % flag)
            return 2

    if part not in PARTS:
        print("--part must be one of: %s" % ", ".join(PARTS))
        return 2

    # THE THIRD OF THE THREE THINGS THIS TOOL PUTS IN A HEADER, and the only
    # one nothing checked. `--part` is checked against a list above and
    # `--module` against a pattern below; `--step` went straight into the
    # template. MEASURED 2026-09-22: `--step banana` wrote
    # `# Heron-Step:   banana` into the module AND the test, printed "written"
    # three times, and exited 0 - in the tool whose own docstring says the
    # reason it exists is that "check-metadata.py finds the mistake after the
    # work, not before it".
    #
    # AND check-metadata WOULD NOT NAME IT. It asks that the five fields are
    # PRESENT, and its step arithmetic is guarded by `.isdigit()`, so a file
    # with a step that is not a number is quietly left out of the counts
    # rather than reported.
    if not str(step).isdigit():
        print("--step must be a number: got '%s'" % step)
        print("It goes straight into the Heron-Step header of two files, and")
        print("check-metadata.py only asks that the field is THERE - a step")
        print("that is not a number is left out of its counts in silence.")
        return 2

    agents, claims = register()

    if agent not in agents:
        print("'%s' is not in docs/28-agent-registry.md." % agent)
        print("An agent is in the register before it is in the code - add the")
        print("row first, with its tier and risk, and run this again.")
        return 1

    if agent in claims:
        print("'%s' is already built:" % agent)
        for path in claims[agent]:
            print("    %s" % path)
        print("To change it, change that file. To replace it, the Agent")
        print("Retirement Agent archives rather than deletes (HERON-AHR-RET-010).")
        return 1

    row = agents[agent]
    name = row["name"]
    # TWO AGENTS CAN DERIVE ONE MODULE NAME, and the rule that strips
    # "_agent" is what makes it likely: "Performance Agent" in
    # Development and "Skill Performance Agent" in Skill Lifecycle both
    # want heron_performance.py. Measured 2026-09-16: 3 of the 31
    # unbuilt agents collide with a module that already exists.
    #
    # Before --module the refusal was a dead end - correct, and with no
    # way forward but writing the three files by hand, which is exactly
    # the drift this tool exists to prevent.
    module = override or module_name(name)
    if override is not None and not re.match(r"^[a-z][a-z0-9_]*$", override):
        print("--module must be lower case letters, digits and "
              "underscores, starting with a letter: got '%s'" % override)
        print("Every module in brain/ is heron_<name>.py, and that is a")
        print("convention this repository observes rather than states.")
        return 2
    # THE FOURTH REFUSAL, and it was learned the expensive way. This tool
    # derived heron_regression_test.py for "Regression Test Agent" on
    # 2026-09-17; heron_regression.py had been there for weeks, the file did
    # not exist so the check above was happy, and the collision only surfaced
    # in a full 198-suite sweep ten minutes later. PROPOSALS F41.
    shadows = prefix_collisions(part, module)
    if shadows:
        print("  refused   heron_%s.py shadows %s"
              % (module, ", ".join("%s.py" % one for one in shadows)))
        print()
        print("  No module name in %s/ may be a prefix of another, and this" % part)
        print("  one is. tests/test_references.py asserts it, and the rule is")
        print("  what lets HERON-DOC-REF-006 tell a whole module name from the")
        print("  start of a longer one - without it, renaming the shorter name")
        print("  quietly edits the longer one too.")
        print()
        print("  Nothing was written. Name the file for what it DOES and pass")
        print("  it with --module, the way heron_buildmatrix.py was named.")
        return 1

    fields = dict(agent=agent, name=name, module=module, part=part,
                  step=step, layer=LAYER[part])

    print("%s  %s" % (agent, name))
    print("  department %s | tier %s | part %s"
          % (row["dept"], row["tier"], part))
    print()

    # ALL THREE, OR NONE. Writing them one at a time meant a single existing
    # path left a contract with no module, or a module with no test, and still
    # reported success because something had been written.
    destinations = [
        (os.path.join(ROOT, "brain", "agents", "%s.yaml" % agent),
         CONTRACT.format(**fields)),
        (os.path.join(ROOT, part, "heron_%s.py" % module),
         MODULE.format(**fields)),
        (os.path.join(ROOT, "tests", "test_%s.py" % module),
         TEST.format(**fields)),
    ]
    taken = [path for path, _text in destinations if os.path.exists(path)]
    if taken:
        for path in taken:
            print("  refused   %s already exists"
                  % os.path.relpath(path, ROOT).replace(os.sep, "/"))
        print("  Nothing was written. A half-scaffolded agent - a contract "
              "with no module, or a module with no test - is worse than none.")
        return 1

    written = []
    for path, text in destinations:
        write(path, text, written)

    for path in written:
        print("  written   %s" % path)
    if not written:
        return 1

    print()
    print("  Next, in this order:")
    print("    1. fill in brain/agents/%s.yaml, then" % agent)
    print("       python brain/heron_contract.py")
    print("    2. write tests/test_%s.py - the claims first" % module)
    print("    3. write %s/heron_%s.py until they pass" % (part, module))
    print("    4. put '%s' in both headers, in place of 'none',"
          % agent)
    print("       and raise Heron-Status from DISCOVERED to DRAFT")
    print("    5. python tools/check-metadata.py && "
          "python tools/check-structure.py")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
