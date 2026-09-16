# Heron-Agent:  HERON-STD-MET-014, HERON-NAM-MET-006
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

# Fragment implementations are CONTENT, not Heron's own source, and their
# metadata standard is their own fragment.yaml - id, status, contract,
# compatibility and proof, all in one place the validator reads.
#
# Giving them a Heron- header too would put `Heron-Status: DRAFT` in the .cs
# beside `status: DRAFT` in the .yaml, and the moment a fragment is promoted
# one of the two would be updated and the other would not. That is this
# repository's most-repeated failure with a new face on it, so the rule is
# ONE place per fact: brain/heron_fragment.py validates these files, and this
# checker does not read them.
SKIP_PREFIXES = (os.path.join("brain", "fragments") + os.sep,)

FIELDS = ["Heron-Agent", "Heron-Step", "Heron-Status", "Heron-Since", "Heron-Layer"]
LAYERS = {"bridge", "revit", "brain", "platform", "test", "tool"}
STATUSES = {"DISCOVERED", "DRAFT", "TESTING", "VALIDATED", "SHADOW",
            "PROVEN", "PRODUCTION", "DEPRECATED", "ARCHIVED"}

# Steps considered implemented so far. Raise this as build steps complete.
#
# This was left at 2 while steps 3, 4 and 5 were finished and proven, so the
# reverse audit below silently skipped three whole steps while still printing
# "Metadata clean". A check that is quietly narrower than it looks is worse
# than no check, because it is trusted. If you complete a step, raise this in
# the same commit.
CURRENT_STEP = 6

# Agents the HOST performs, so no file here implements them and none ever will.
#
# Not an omission - a recorded decision. docs/02 section 7 settles where the
# orchestration runs: "Claude Code - host: conversation, agents, persona,
# orchestration". These four are that layer. Heron declares them in the
# registry because they are real parts of the system that must be reasoned
# about; it does not build them because building them again would replace a
# working host with a worse copy.
#
# They are listed rather than deleted so the audit stays honest in both
# directions: an agent with no file is either delegated ON PURPOSE and named
# here, or it is work still to do. Silence would make those two look alike.
# DELEGATED TO THE HOST UNDER D-01, "Claude Code is the conversation layer
# AND THE AGENT HOST". These are not unbuilt; they are language work the host
# does, and counting them as missing would report a gap that is filled.
#
# The last five were added on 2026-09-17 by D-80, settling F39. The owner
# raised it himself: the host IS the model, so a row whose whole job is
# language does not need one of Heron's own.
HOST_PROVIDED = {
    "HERON-ORC-MAIN-001": "the host plans and sequences the work",
    "HERON-ORC-INT-002":  "the host classifies what is being asked",
    "HERON-ORC-PER-003":  "the host chooses the wording and the level",
    "HERON-ORC-SUM-006":  "the host writes the reply the user reads",
    "HERON-DEV-REQ-001":  "the host turns a request into a specification",
    "HERON-DEV-PLN-002":  "the host sequences the work - the same sentence "
                          "ORC-MAIN-001 is already delegated for",
    "HERON-DEV-ARC-003":  "the host decides structure; heron_belongs.py "
                          "(IMP-ARC-011) does the mechanical half",
    "HERON-DEV-GEN-004":  "the host writes the code, as it wrote every line "
                          "in this repository",
    "HERON-DEV-RAP-007":  "the host holds the Revit API knowledge; "
                          "heron_dotnet.py and heron_csharp.py hold the "
                          "checkable half",
}


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
                if fn.endswith(SOURCE_EXT) and not os.path.join(
                        dirpath, fn).startswith(SKIP_PREFIXES):
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


def build_version():
    """The one version in Directory.Build.props - the C# side's own answer."""
    try:
        text = io.open("Directory.Build.props", encoding="utf-8").read()
    except (IOError, OSError):
        return None
    m = re.search(r"<Version>([^<]+)</Version>", text)
    return m.group(1).strip() if m else None


def python_version():
    """What the Python side believes Heron's version is."""
    try:
        text = io.open("mcp/client/heron_bridge_client.py", encoding="utf-8").read()
    except (IOError, OSError):
        return None
    m = re.search(r'^HERON_VERSION\s*=\s*"([^"]+)"', text, re.M)
    return m.group(1).strip() if m else None


def check_version_agreement(problems):
    """
    Heron reports its version to the user from Python and stamps its
    assemblies from MSBuild. Two places, one number - so it is verified rather
    than trusted, the same reason every count in the docs is derived.
    """
    csharp, python = build_version(), python_version()
    if csharp is None:
        problems.append("could not read <Version> from Directory.Build.props")
        return
    if python is None:
        problems.append("could not read HERON_VERSION from the bridge client")
        return
    if csharp != python:
        problems.append(
            "version disagreement: Directory.Build.props says %s, the bridge client says %s. "
            "Heron would report one number and stamp another." % (csharp, python))
    else:
        print("Version:  %s, and both sides agree" % csharp)


def check_phase_counts(agents, problems):
    """
    Every sentence claiming how many agents Phase 0 and Phase 1 need must
    match the rows that actually carry a step number.

    This check exists because three different figures for one set were in
    circulation at once - "about 20" in the registry, "45" and "175" in the
    catalogue - and the real answer was 46 and 204. None of them was derived
    from the rows; each was typed once and then went stale as agents were
    added. That is the same failure the 250-agent total already suffered and
    was fixed by counting, so it is fixed the same way here.
    """
    assigned = [aid for aid, (_, step) in agents.items() if step and step.isdigit()]
    total = len(assigned)

    pattern = re.compile(
        r"Phase 0 and Phase 1 needs? (?:about )?\*{0,2}(\d+)", re.IGNORECASE)

    checked_any = False
    for name in sorted(os.listdir("docs")):
        if not name.endswith(".md"):
            continue
        path = os.path.join("docs", name)
        for number, line in enumerate(io.open(path, encoding="utf-8"), 1):
            found = pattern.search(line)
            if not found:
                continue
            checked_any = True
            claimed = int(found.group(1))
            if claimed != total:
                problems.append(
                    "%s line %d says Phase 0 and Phase 1 need %d agents, but %d rows "
                    "carry a step number" % (path, number, claimed, total))

    if not checked_any:
        problems.append(
            "No document states how many agents Phase 0 and Phase 1 need. The claim was "
            "removed or reworded, so this check is now guarding nothing - restore it or "
            "delete the check.")

    return total


def main():
    agents = registry_agents()
    if not agents:
        print("FAIL  could not read %s" % REGISTRY)
        return 1
    print("Registry: %d agents" % len(agents))

    problems = []
    check_version_agreement(problems)
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

    assigned = check_phase_counts(agents, problems)

    print("Source files checked: %d" % checked)
    print("Assigned to a step:   %d" % assigned)
    print("Agents implemented:   %d" % len(claimed))

    # A delegated agent must still BE an agent. If an id here is not in the
    # registry, the exemption is silently covering nothing - the usual cause
    # being a renamed or renumbered agent, which is exactly when an audit
    # should speak up rather than pass.
    for aid in sorted(HOST_PROVIDED):
        if aid not in agents:
            problems.append("HOST_PROVIDED lists '%s', which is not in the registry" % aid)

    # The reverse audit: registry agents due by now with no implementing file,
    # excluding the ones the host provides on purpose.
    unimplemented = sorted(
        aid for aid, (_, step) in agents.items()
        if step and step.isdigit() and int(step) <= CURRENT_STEP
        and aid not in claimed and aid not in HOST_PROVIDED)

    delegated = sorted(aid for aid in HOST_PROVIDED if aid in agents)

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

    if delegated:
        print("Provided by the host, so no file here implements them (docs/02 section 7):")
        for aid in delegated:
            print("  - %-28s %s" % (aid, HOST_PROVIDED[aid]))
        print()

    if problems:
        return 1
    print("Metadata clean.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
