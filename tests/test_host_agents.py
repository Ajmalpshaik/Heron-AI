# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The agents exist twice, once per host, and nothing regenerates one.

    python tests/test_host_agents.py

`.claude/agents/<name>.md` and `.codex/agents/<name>.toml` are the SAME
agents for two hosts - PROJECT-MAP says so: "the same agents for a second
host, pointing at the same skills". Neither is generated from the other, and
until this suite existed nothing compared them. There were four when it was
written; count the folder rather than this sentence.

WHY THAT IS WORTH A SUITE RATHER THAN A HABIT
-----------------------------------------------
This repository's own rule, written at the top of AGENTS.md: "two copies of
a rule is how one of them goes stale." It has been paid for repeatedly -
`HeronConfig.GetBool` learned four new words in the C# and the Python reader
of the same file did not (row 5b-29); a client-id rule was applied to three
doors of four (row 5b-51); a decision's rename shipped in neither of the two
places it named (row 5b-52).

The two copies agree today, byte for byte. This is what keeps them agreeing,
and it is the same answer `tests/test_heron_guard.py` gives for the layering
rule the hook restates and `tests/test_fragment_imports.py` gives for the
executor's import list: where a thing has to exist twice, a test holds the
copies together.

WHAT IT PROVES
  1. THE PAIRS ARE FOUND, NOT LISTED. Every `.claude/agents/*.md` must have
     a `.codex/agents/*.toml` beside it and the other way round, so adding
     one host's copy alone fails here rather than going unnoticed.
  2. NAME, DESCRIPTION AND BODY ARE IDENTICAL. The body is what the agent is
     told; the description is what makes it trigger. A drift in either is a
     drift in behaviour on one host only, which is the hardest kind to see.
  3. THE DIFFERENCE IS REPORTED, not just the fact of one. A suite that says
     "they differ" and stops sends the reader to diff two formats by hand.

WHAT IT DOES NOT PROVE
  Nothing here says the instructions are GOOD. It says the two hosts are
  given the same ones.
"""

from __future__ import annotations

import difflib
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CLAUDE = os.path.join(ROOT, ".claude", "agents")
CODEX = os.path.join(ROOT, ".codex", "agents")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read()


def from_markdown(text):
    """(name, description, body) out of a `.claude` agent file."""
    parts = text.split("---", 2)
    body = parts[2].strip() if len(parts) > 2 else text.strip()
    name = re.search(r"^name:\s*(.+)$", text, re.M)
    description = re.search(r"^description:\s*(.+)$", text, re.M)
    return (name.group(1).strip() if name else None,
            description.group(1).strip() if description else None,
            body)


def from_toml(text):
    """(name, description, body) out of a `.codex` agent file.

    Hand-read rather than parsed. `tomllib` is Python 3.11 and this
    repository states no floor above "Python 3", so a suite that needs 3.11
    would report an old interpreter as a drift between two files that agree.
    The three fields are read by shape and the reader REFUSES rather than
    returning a short answer - the same rule `fragment_needs` follows in
    heron_bridge_client.py.
    """
    name = re.search(r'^name\s*=\s*"(.*)"\s*$', text, re.M)
    description = re.search(r'^description\s*=\s*"(.*)"\s*$', text, re.M)
    opened = 'developer_instructions = """'
    if opened not in text or not text.rstrip().endswith('"""'):
        return None, None, None
    body = text.split(opened, 1)[1].rsplit('"""', 1)[0].strip()
    return (name.group(1).strip() if name else None,
            description.group(1).strip() if description else None,
            body)


def main():
    print("\nThe same agents, for two hosts")

    for folder, what in ((CLAUDE, ".claude/agents"), (CODEX, ".codex/agents")):
        if not os.path.isdir(folder):
            print("  FAIL  %s does not exist" % what)
            FAILURES.append("%s does not exist" % what)
            return 1

    mine = set(n[:-3] for n in os.listdir(CLAUDE) if n.endswith(".md"))
    theirs = set(n[:-5] for n in os.listdir(CODEX) if n.endswith(".toml"))

    # AN EMPTY COMPARISON PASSES PERFECTLY, and this repository has been
    # caught by that before. Two empty folders agree.
    check(len(mine) >= 4,
          "found %d agent(s) under .claude/agents - enough to be comparing "
          "something" % len(mine))
    check(mine == theirs,
          "every agent exists for both hosts%s"
          % ("" if mine == theirs else
             "  <- only .claude: %s ; only .codex: %s"
             % (sorted(mine - theirs) or "none", sorted(theirs - mine) or "none")))

    print("\nName, description and body agree, file by file")
    for agent in sorted(mine & theirs):
        md = from_markdown(read(os.path.join(CLAUDE, agent + ".md")))
        toml = from_toml(read(os.path.join(CODEX, agent + ".toml")))
        if toml == (None, None, None):
            check(False, "%s: the .codex copy could not be read - its "
                         "developer_instructions block is not closed" % agent)
            continue
        for field, left, right in (("name", md[0], toml[0]),
                                   ("description", md[1], toml[1])):
            check(left is not None and left == right,
                  "%s: %s matches%s"
                  % (agent, field,
                     "" if left == right else "  <- .claude %r ; .codex %r"
                     % (left, right)))
        same = md[2] == toml[2]
        check(same, "%s: the instructions are identical" % agent)
        if not same:
            # THE DIFFERENCE, NOT JUST THE FACT OF ONE. Two formats, so a
            # reader cannot diff them with a shell command.
            for line in list(difflib.unified_diff(
                    md[2].split("\n"), toml[2].split("\n"),
                    ".claude", ".codex", lineterm="", n=1))[:20]:
                print("        %s" % line)

    print()
    if FAILURES:
        print("FAILED")
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - both hosts are given the same instructions, and this is "
          "what keeps them that way.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
