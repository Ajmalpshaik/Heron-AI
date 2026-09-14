# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-AGT-002
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The agent register - how much of the 250 exists, by department.

    python tools/agent-count.py

`check-metadata.py` audits the registry against the code FILE BY FILE: does
this header parse, does it claim an agent that exists, is anything due by now
unimplemented. This tool asks the other question - the one nobody could answer
without adding up columns by hand: **what proportion of each department is
built, and what is left.**

Three states, not two. An agent is BUILT (a source file claims it), HOST
(delegated to Claude Code on purpose, D-01) or LEFT. Collapsing HOST into LEFT
produces a to-do list with four items on it that will never be done, which is
how this repository's own build-state page came to recommend building the
Orchestrator - an agent docs/02 section 7 settles as the host's.

`HOST_PROVIDED` is NOT redefined here. It is imported from check-metadata.py,
because two copies of that list is exactly the drift this repository keeps
having to write about. One place per fact.

Exit 0 = the register reconciles.
Exit 1 = the registry disagrees with itself or with the code.
"""

import io
import os
import re
import sys
import importlib.util
import collections

REGISTRY = os.path.join("docs", "28-agent-registry.md")
SOURCE_ROOTS = ["revit", "mcp", "brain", "platform", "tests", "tools"]
SOURCE_EXT = (".cs", ".py", ".ps1")
SKIP_NAMES = {"__pycache__", "bin", "obj", ".vs"}

# Fragment implementations carry their metadata in their own fragment.yaml.
# Same reason check-metadata.py skips them: one place per fact.
SKIP_PREFIXES = (os.path.join("brain", "fragments") + os.sep,)

# This file contains the string it scans for, so it would find itself.
# check-gaps.py learned this the hard way on its first run.
SELF = os.path.basename(__file__)


def w(s):
    """stdout that survives a console that is not UTF-8."""
    sys.stdout.write(s.encode("ascii", "replace").decode("ascii"))


def host_provided():
    """
    The delegated agents, read from check-metadata.py rather than repeated.

    The filename has a hyphen, so it is not importable by name; loading it by
    path is the price of not owning a second copy of the list.
    """
    path = os.path.join("tools", "check-metadata.py")
    spec = importlib.util.spec_from_file_location("heron_check_metadata", path)
    if spec is None or spec.loader is None:
        return None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return dict(module.HOST_PROVIDED)


def registry():
    """Every agent row, in document order, with its department."""
    agents = collections.OrderedDict()
    headings = []          # (department, count stated in the heading)
    stated_totals = None   # (agents, T1, T2, T3) from the Totals line
    dept = None

    for line in io.open(REGISTRY, encoding="utf-8"):
        head = re.match(r"^## [0-9a-z]+\. (.+?) — (\d+)", line)
        if head:
            dept = re.sub(r"\s*\*.*", "", head.group(1)).strip()
            headings.append((dept, int(head.group(2))))
            continue

        tot = re.match(r"^\*\*Totals: (\d+) agents · (\d+) T1 · (\d+) T2 · (\d+) T3\.\*\*", line)
        if tot:
            stated_totals = tuple(int(g) for g in tot.groups())
            continue

        if not line.startswith("| `HERON-"):
            continue
        cols = [c.strip() for c in line.split("|")]
        if len(cols) < 7:
            continue
        aid = cols[1].strip("`")
        step = cols[6].replace("*", "").strip()
        agents[aid] = dict(
            name=re.sub(r"\*\*|↗", "", cols[2]).strip(),
            # The "Does" column - the agent's responsibility in the register's
            # own words. Nothing here reads it; it is carried because this is
            # the ONE parser of that file, and the alternative to adding a
            # field was a second parser somewhere else, which is the drift
            # this repository keeps writing about.
            does=re.sub(r"\*\*|↗", "", cols[3]).strip(),
            tier=cols[4].replace("*", "").strip(),
            step=None if step in ("—", "-", "") else step,
            dept=dept or "(no department)",
        )
    return agents, headings, stated_totals


def built():
    """Agent ids claimed by a Heron-Agent: header, and the files claiming them."""
    claims = collections.defaultdict(list)
    for root in SOURCE_ROOTS:
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = [d for d in dirnames if d not in SKIP_NAMES]
            for fn in filenames:
                if not fn.endswith(SOURCE_EXT) or fn == SELF:
                    continue
                path = os.path.join(dirpath, fn)
                if path.startswith(SKIP_PREFIXES):
                    continue
                with io.open(path, encoding="utf-8", errors="replace") as fh:
                    for i, line in enumerate(fh):
                        if i > 40:
                            break
                        # Anchored to a comment line, so a regex in a source
                        # file is not mistaken for a declaration.
                        m = re.match(r"^\s*(?://|#)\s*Heron-Agent\s*:\s*(.+?)\s*$", line)
                        if not m or m.group(1).strip().lower() == "none":
                            continue
                        for aid in [a.strip() for a in m.group(1).split(",") if a.strip()]:
                            claims[aid].append(path.replace(os.sep, "/"))
    return claims


def main():
    if not os.path.exists(REGISTRY):
        w("FAIL  could not read %s - run from the repository root\n" % REGISTRY)
        return 1

    agents, headings, stated = registry()
    if not agents:
        w("FAIL  %s parsed to zero agent rows\n" % REGISTRY)
        return 1

    hosts = host_provided()
    if hosts is None:
        w("FAIL  could not load HOST_PROVIDED from tools/check-metadata.py\n")
        return 1

    claims = built()
    problems = []

    # --- the register -----------------------------------------------------
    depts = collections.OrderedDict()
    for aid, a in agents.items():
        d = depts.setdefault(a["dept"], collections.Counter())
        d["total"] += 1
        d[a["tier"]] += 1
        if aid in hosts:
            d["host"] += 1
        elif aid in claims:
            d["built"] += 1
            if a["tier"] == "T1":
                d["t1built"] += 1

    rows = sorted(
        depts.items(),
        key=lambda kv: (-(kv[1]["built"] / float(kv[1]["total"])), -kv[1]["total"]))

    widest = max(len(d) for d in depts)
    header = "%-*s %6s %6s %5s %6s %8s" % (widest, "DEPARTMENT", "TOTAL",
                                           "BUILT", "HOST", "LEFT", "T1 LEFT")
    w("\nHERON AGENT REGISTER   %s\n" % REGISTRY)
    w("%s\n" % ("=" * len(header)))
    w("%s\n" % header)
    w("%s\n" % ("-" * len(header)))

    grand = collections.Counter()
    for name, c in rows:
        left = c["total"] - c["built"] - c["host"]
        t1left = c["T1"] - c["t1built"]
        grand.update(dict(total=c["total"], built=c["built"], host=c["host"],
                          left=left, t1left=t1left,
                          T1=c["T1"], T2=c["T2"], T3=c["T3"]))
        w("%-*s %6d %6d %5s %6d %8d\n" % (
            widest, name, c["total"], c["built"],
            c["host"] or "-", left, t1left))

    w("%s\n" % ("-" * len(header)))
    w("%-*s %6d %6d %5d %6d %8d\n" % (
        widest, "TOTAL", grand["total"], grand["built"],
        grand["host"], grand["left"], grand["t1left"]))

    # --- tiers ------------------------------------------------------------
    tier_built = collections.Counter()
    for aid in claims:
        if aid in agents and aid not in hosts:
            tier_built[agents[aid]["tier"]] += 1

    w("\nBY TIER                       BUILT  TOTAL\n")
    for tier, gloss in (("T1", "no model call"),
                        ("T2", "one scoped call"),
                        ("T3", "agentic loop")):
        w("  %s  %-22s %5d %6d\n" % (tier, gloss, tier_built[tier], grand[tier]))

    # --- phase 0/1 --------------------------------------------------------
    assigned = [aid for aid, a in agents.items() if a["step"] and a["step"].isdigit()]
    p_built = [a for a in assigned if a in claims and a not in hosts]
    p_host = [a for a in assigned if a in hosts]
    p_left = [a for a in assigned if a not in claims and a not in hosts]

    w("\nPHASE 0/1  (the %d agents carrying a build step)\n" % len(assigned))
    w("  %d built - %d provided by the host - %d outstanding\n"
      % (len(p_built), len(p_host), len(p_left)))
    if p_left:
        for aid in sorted(p_left, key=lambda a: (int(agents[a]["step"]), a)):
            w("    step %-2s %-3s %-26s %s\n" % (agents[aid]["step"], agents[aid]["tier"],
                                                 aid, agents[aid]["name"]))
    if p_host:
        w("  Delegated on purpose (docs/02 section 7, D-01) - never built here:\n")
        for aid in sorted(p_host):
            w("    %-26s %s\n" % (aid, hosts[aid]))

    # --- the gates --------------------------------------------------------
    # 1. Every department heading must match the rows beneath it.
    for name, claimed in headings:
        actual = depts.get(name, collections.Counter())["total"]
        if claimed != actual:
            problems.append(
                "heading '%s' says %d, its rows count %d - run "
                "tools/recount-agent-registry.py" % (name, claimed, actual))

    # 2. The Totals line must match the rows.
    if stated is None:
        problems.append("no '**Totals: N agents ...**' line found in %s - the check "
                        "is guarding nothing, restore it or delete this gate" % REGISTRY)
    else:
        real = (grand["total"], grand["T1"], grand["T2"], grand["T3"])
        if stated != real:
            problems.append(
                "Totals line says %d/%dT1/%dT2/%dT3, the rows count %d/%dT1/%dT2/%dT3 - "
                "run tools/recount-agent-registry.py" % (stated + real))

    # 3. A delegated agent must still be an agent.
    for aid in sorted(hosts):
        if aid not in agents:
            problems.append("HOST_PROVIDED lists '%s', which is not in the registry - "
                            "the exemption is covering nothing" % aid)

    # 4. Delegated AND implemented is a contradiction, and nothing else asks.
    #    Either the decision was reversed and the exemption is stale, or a file
    #    claims work the host does. Both are wrong quietly.
    for aid in sorted(set(hosts) & set(claims)):
        problems.append(
            "'%s' is listed as host-provided but %s claims it - reverse the "
            "exemption or the file" % (aid, ", ".join(claims[aid])))

    # 5. Code claiming an agent that does not exist would silently miscount the
    #    register. check-metadata.py reports these per file; this is the gate.
    for aid in sorted(set(claims) - set(agents)):
        problems.append("%s claims '%s', which is not in the registry"
                        % (", ".join(claims[aid]), aid))

    w("\n")
    if problems:
        w("REGISTER DOES NOT RECONCILE (%d):\n" % len(problems))
        for p in problems:
            w("  - %s\n" % p)
        w("\n")
        return 1

    w("Register reconciles: %d agents, %d built, %d host-provided, %d left.\n"
      % (grand["total"], grand["built"], grand["host"], grand["left"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
