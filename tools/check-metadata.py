# Heron-Agent:  HERON-STD-MET-014
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Metadata & Policy Checker - the working prototype of HERON-STD-MET-014.

Enforces docs/29-metadata-standard.md, and does the thing that makes the
standard worth having: it audits the 250-agent registry AGAINST the code,
in both directions.

    python tools/check-metadata.py

Exit 0 = clean.
"""

import io
import os
import re
import sys

REGISTRY = os.path.join("docs", "28-agent-registry.md")
SOURCE_ROOTS = ["revit", "mcp", "brain", "platform", "tests", "tools"]
SOURCE_EXT = (".cs", ".py", ".ps1")
SKIP_NAMES = {"__pycache__", "bin", "obj", ".vs"}

FIELDS = ["Heron-Agent", "Heron-Step", "Heron-Status", "Heron-Since", "Heron-Layer"]
LAYERS = {"bridge", "revit", "brain", "platform", "test", "tool"}
STATUSES = {"DISCOVERED", "DRAFT", "TESTING", "VALIDATED", "SHADOW",
            "PROVEN", "PRODUCTION", "DEPRECATED", "ARCHIVED"}

# Steps considered implemented so far. Raise this as build steps complete.
CURRENT_STEP = 2


def registry_agents():
    """id -> (name, step) for every agent in the registry."""
    agents = {}
    if not os.path.exists(REGISTRY):
        return agents
    for line in io.open(REGISTRY, encoding="utf-8"):
        if not line.startswith("| `HERON-"):
            continue
        cols = [c.strip() for c in line.split("|")]
        if len(cols) < 7:
            continue
        aid = cols[1].strip("`")
        name = re.sub(r"\*\*|↗", "", cols[2]).strip()
        step = cols[6].replace("*", "").strip()
        step = None if step in ("—", "-", "") else step
        agents[aid] = (name, step)
    return agents


def source_files():
    for root in SOURCE_ROOTS:
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = [d for d in dirnames if d not in SKIP_NAMES]
            for fn in filenames:
                if fn.endswith(SOURCE_EXT):
                    yield os.path.join(dirpath, fn).replace(os.sep, "/")


def read_header(path):
    """Parse the Heron-* fields from the top of a file."""
    found = {}
    with io.open(path, encoding="utf-8", errors="replace") as fh:
        for i, line in enumerate(fh):
            if i > 40:
                break
            m = re.match(r"^\s*(?://|#)\s*(Heron-[A-Za-z]+)\s*:\s*(.+?)\s*$", line)
            if m:
                found[m.group(1)] = m.group(2)
    return found


def main():
    agents = registry_agents()
    if not agents:
        print("FAIL  could not read %s" % REGISTRY)
        return 1
    print("Registry: %d agents" % len(agents))

    problems = []
    claimed = set()
    checked = 0

    for path in sorted(source_files()):
        header = read_header(path)
        checked += 1

        missing = [f for f in FIELDS if f not in header]
        if missing:
            problems.append("%s: missing %s" % (path, ", ".join(missing)))
            continue

        status = header["Heron-Status"].upper()
        if status not in STATUSES:
            problems.append("%s: Heron-Status '%s' is not a lifecycle stage (docs/24)"
                            % (path, header["Heron-Status"]))

        layer = header["Heron-Layer"].lower()
        if layer not in LAYERS:
            problems.append("%s: Heron-Layer '%s' is not one of %s"
                            % (path, layer, ", ".join(sorted(LAYERS))))

        value = header["Heron-Agent"].strip()
        if value.lower() != "none":
            for aid in [a.strip() for a in value.split(",") if a.strip()]:
                if aid not in agents:
                    problems.append("%s: claims '%s', which is not in the registry" % (path, aid))
                else:
                    claimed.add(aid)

    print("Source files checked: %d" % checked)
    print("Agents implemented:   %d" % len(claimed))

    # The reverse audit: registry agents due by now with no implementing file.
    unimplemented = sorted(
        aid for aid, (_, step) in agents.items()
        if step and step.isdigit() and int(step) <= CURRENT_STEP and aid not in claimed)

    print()
    if problems:
        print("METADATA PROBLEMS (%d):" % len(problems))
        for p in problems:
            print("  - %s" % p)
        print()

    if unimplemented:
        print("Registry claims these for step <= %d, but no file implements them:" % CURRENT_STEP)
        for aid in unimplemented:
            print("  - %-28s %s" % (aid, agents[aid][0]))
        print()
        print("  Not an error while a step is in progress - it is the honest")
        print("  to-do list for finishing step %d." % CURRENT_STEP)
        print()

    if problems:
        return 1
    print("Metadata clean.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
