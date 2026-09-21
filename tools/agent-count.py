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

FOUR states, not two. An agent is BUILT (a source file claims it), HOST
(delegated to Claude Code on purpose, D-01), DEFERRED (a decision says it
cannot be built yet and names itself) or LEFT. Collapsing HOST into LEFT
produces a to-do list with four items on it that will never be done, which is
how this repository's own build-state page came to recommend building the
Orchestrator - an agent docs/02 section 7 settles as the host's.

DEFERRED IS THE SAME MISTAKE ONE STEP ALONG, and it was live here until
2026-09-21. `HERON-DOC-REL-005` needs releases to write notes about and there
are none; `HERON-DOC-CHG-008` needs two versions to write a change log between
and 683 files say `Heron-Since: 0.1.0`. D-77 settled both in writing on
2026-09-16, and this tool went on reporting `2 left` - so the balance-of-work
page, which is what a person reads to know what remains, has carried two items
nobody can do since the day they were deliberately not done. A number that
cannot go to zero stops being read.

IT IS DERIVED, NOT LISTED HERE. The registry row says DEFERRED and names the
decision, and that sentence is the only copy of the fact. An agent whose row
says DEFERRED without naming a decision is NOT counted as deferred - it is
left, and loudly, because "we will do it later" with nobody's name on it is
how a to-do list becomes a wish.

`HOST_PROVIDED` is NOT redefined here. It is imported from check-metadata.py,
because two copies of that list is exactly the drift this repository keeps
having to write about. One place per fact.

Exit 0 = the register reconciles.
Exit 1 = the registry disagrees with itself or with the code.
"""

import io
import os
import re
import subprocess
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
        risk = cols[5].replace("*", "").strip()
        agents[aid] = dict(
            name=re.sub(r"\*\*|↗", "", cols[2]).strip(),
            # The "Does" column - the agent's responsibility in the register's
            # own words. Nothing here reads it; it is carried because this is
            # the ONE parser of that file, and the alternative to adding a
            # field was a second parser somewhere else, which is the drift
            # this repository keeps writing about.
            does=re.sub(r"\*\*|↗", "", cols[3]).strip(),
            tier=cols[4].replace("*", "").strip(),
            # The "Risk" column, read for the same reason as "Does" above and
            # carried the same way. An em-dash means NOBODY HAS ASSIGNED ONE,
            # which is a different fact from READ and must not be flattened
            # into it - the Trainer refuses to train an agent whose
            # permissions nobody has decided.
            risk=None if risk in ("—", "-", "") else risk,
            step=None if step in ("—", "-", "") else step,
            dept=dept or "(no department)",
            # DEFERRED, AND ONLY WITH A DECISION'S NAME ON IT. The "Does"
            # cell is the register's own prose and the only copy of this
            # fact; `deferred` holds the decision id, which is what gets
            # printed, so a reader can go and disagree with it.
            deferred=_deferred(cols[3]),
        )
    return agents, headings, stated_totals


# "**DEFERRED 2026-09-16 by [D-77](DECISIONS.md)**" - the word, then a
# decision id within the same sentence. Without the id this does not match,
# which is the point: see the docstring.
DEFERRED = re.compile(r"\bDEFERRED\b[^.]{0,60}?\[(D-\d+)\]")


def _deferred(cell):
    """The decision deferring this agent, or None. Never a bare promise."""
    found = DEFERRED.search(cell or "")
    return found.group(1) if found else None


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


HEADER = re.compile(r"^\s*(?://|#)\s*Heron-Agent\s*:\s*(.+?)\s*$")


def _ids(line):
    """The agent ids one header line claims. A line may carry several."""
    m = HEADER.match(line)
    if not m or m.group(1).strip().lower() == "none":
        return []
    return [a.strip() for a in m.group(1).split(",") if a.strip()]


def _scanned(path):
    """Would built() have read this file? Same rules, one place."""
    path = path.replace("/", os.sep)
    if not path.endswith(SOURCE_EXT) or os.path.basename(path) == SELF:
        return False
    if path.startswith(SKIP_PREFIXES):
        return False
    if path.split(os.sep)[0] not in SOURCE_ROOTS:
        return False
    return not (set(path.split(os.sep)) & SKIP_NAMES)


def claimed_at_head():
    """Agent ids a header claimed in the last commit, or None if unknown.

    Not every copy of this repository is a git checkout - an installed
    Heron is not - so a missing git is not a finding. It is the absence
    of a second opinion, and it is reported as that.
    """
    try:
        done = subprocess.Popen(
            ["git", "grep", "-n", "-I", "-E",
             r"^[[:space:]]*(//|#)[[:space:]]*Heron-Agent[[:space:]]*:",
             "HEAD", "--", "*.py", "*.cs", "*.ps1"],
            cwd=os.getcwd(), stdout=subprocess.PIPE,
            stderr=subprocess.PIPE)
        out, _err = done.communicate()
    except (OSError, ValueError):
        return None
    if done.returncode not in (0, 1):
        return None

    claims = collections.defaultdict(list)
    for line in out.decode("utf-8", "replace").splitlines():
        # HEAD:path:lineno:text - the path may not contain a colon, and the
        # text may, so split only the three fields in front of it.
        parts = line.split(":", 3)
        if len(parts) != 4:
            continue
        _head, path, lineno, text = parts
        if not lineno.isdigit() or int(lineno) > 41 or not _scanned(path):
            continue
        for aid in _ids(text):
            claims[aid].append(path)
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
        elif a["deferred"]:
            # NOT BUILT AND NOT OUTSTANDING. Counted apart so LEFT stays a
            # list somebody can finish, and printed by name below so the
            # deferral stays arguable rather than disappearing.
            d["defer"] += 1
            if a["tier"] == "T1":
                d["t1defer"] += 1

    rows = sorted(
        depts.items(),
        key=lambda kv: (-(kv[1]["built"] / float(kv[1]["total"])), -kv[1]["total"]))

    widest = max(len(d) for d in depts)
    header = "%-*s %6s %6s %5s %6s %6s %8s" % (widest, "DEPARTMENT", "TOTAL",
                                               "BUILT", "HOST", "DEFER",
                                               "LEFT", "T1 LEFT")
    w("\nHERON AGENT REGISTER   %s\n" % REGISTRY)
    w("%s\n" % ("=" * len(header)))
    w("%s\n" % header)
    w("%s\n" % ("-" * len(header)))

    grand = collections.Counter()
    for name, c in rows:
        left = c["total"] - c["built"] - c["host"] - c["defer"]
        t1left = c["T1"] - c["t1built"] - c["t1defer"]
        grand.update(dict(total=c["total"], built=c["built"], host=c["host"],
                          defer=c["defer"], left=left, t1left=t1left,
                          T1=c["T1"], T2=c["T2"], T3=c["T3"]))
        w("%-*s %6d %6d %5s %6s %6d %8d\n" % (
            widest, name, c["total"], c["built"],
            c["host"] or "-", c["defer"] or "-", left, t1left))

    w("%s\n" % ("-" * len(header)))
    w("%-*s %6d %6d %5d %6d %6d %8d\n" % (
        widest, "TOTAL", grand["total"], grand["built"],
        grand["host"], grand["defer"], grand["left"], grand["t1left"]))

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
    p_defer = [a for a in assigned
               if a not in claims and a not in hosts and agents[a]["deferred"]]
    p_left = [a for a in assigned if a not in claims and a not in hosts
              and not agents[a]["deferred"]]

    w("\nPHASE 0/1  (the %d agents carrying a build step)\n" % len(assigned))
    w("  %d built - %d provided by the host - %d deferred - %d outstanding\n"
      % (len(p_built), len(p_host), len(p_defer), len(p_left)))
    if p_left:
        for aid in sorted(p_left, key=lambda a: (int(agents[a]["step"]), a)):
            w("    step %-2s %-3s %-26s %s\n" % (agents[aid]["step"], agents[aid]["tier"],
                                                 aid, agents[aid]["name"]))
    if p_host:
        w("  Delegated on purpose (docs/02 section 7, D-01) - never built here:\n")
        for aid in sorted(p_host):
            w("    %-26s %s\n" % (aid, hosts[aid]))

    # --- deferred ---------------------------------------------------------
    # PRINTED BY NAME, WITH THE DECISION, because a deferral that stops being
    # visible stops being arguable - and these are the rows most likely to
    # have gone stale, since what they are waiting for is a release and a
    # second version, both of which will arrive without anyone re-reading
    # this file.
    deferred = sorted(aid for aid, a in agents.items()
                      if a["deferred"] and aid not in claims and aid not in hosts)
    if deferred:
        w("\nDEFERRED BY A DECISION  (not built, and not outstanding)\n")
        for aid in deferred:
            w("  %-26s %-4s %s\n"
              % (aid, agents[aid]["deferred"], agents[aid]["name"]))
        w("  Each names the decision deferring it. When what it is waiting "
          "for exists,\n  the decision is what has to change first.\n")

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

    # 4. AN AGENT THAT WAS BUILT IS NO LONGER BUILT. Written 2026-09-15,
    #    the day a new agent was written straight over brain/heron_architect.py
    #    - 272 lines of HERON-AHR-ARC-003 and its 201-line suite - and the
    #    only thing that showed it was this tool's total not moving. That is
    #    too quiet for what it is: an overwritten file is unrecoverable
    #    outside git, and the register reconciled perfectly either way
    #    because the id that vanished and the id that arrived cancelled out.
    was = claimed_at_head()
    if was is None:
        w("\n(No git here, so nothing was compared against the last commit. "
          "An installed Heron is not a checkout - this is the absence of a "
          "second opinion, not a finding.)\n")
    else:
        for aid in sorted(set(was) - set(claims)):
            problems.append(
                "'%s' was claimed by %s in the last commit and is claimed by "
                "nothing now. Either it was deleted on purpose, or a file was "
                "written over" % (aid, ", ".join(sorted(set(was[aid])))))

    w("\n")
    if problems:
        w("REGISTER DOES NOT RECONCILE (%d):\n" % len(problems))
        for p in problems:
            w("  - %s\n" % p)
        w("\n")
        return 1

    # THE SHAPE OF THIS LINE IS READ BY tools/balance-of-work.py, which takes
    # `%d agents` and `%d left` off it. `deferred` sits between them and is
    # named, so the page a person reads to know what remains says both.
    w("Register reconciles: %d agents, %d built, %d host-provided, "
      "%d deferred, %d left.\n"
      % (grand["total"], grand["built"], grand["host"], grand["defer"],
         grand["left"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
