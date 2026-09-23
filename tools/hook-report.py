# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
How often each of Heron's development hooks decided something, and what it said.

    python tools/hook-report.py                 # the log this machine keeps
    python tools/hook-report.py --days 7        # only the last seven days
    python tools/hook-report.py --log FILE      # another log file

A REPORT, NOT A GATE. It exits 0 whatever it finds - a hook that speaks in
every decision is a question for a person, not a failure.

WHY IT EXISTS
-------------
A hook that only nags is switched off, and a hook that never fires is not
there at all - and neither is visible from inside one session. Every hook in
.claude/settings.json appends one line per decision to a log outside this
repository (.claude/skills/heron-session/bin/hook_log.py says where, and why
it is never a typed path). This counts those lines, so "is the guard running in
every session?" and "does anybody ever act on the main-moved advice?" are
answered from what happened rather than from memory.

WHAT IT PRINTS, PER HOOK
------------------------
  * how many decisions, in how many sessions, first and last day;
  * each kind of decision and how often (allow, deny, advised, ...);
  * how often it SPOKE - said anything at all - and the things it said most,
    so a hook that says the same sentence two hundred times shows up as that.

A line in the log that will not parse is counted and shown, never fatal: the
file is appended to by several processes, and a torn line must not hide the
rest.
"""

import argparse
import os
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, ".claude", "skills", "heron-session", "bin"))

import hook_log as LOG                               # noqa: E402

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

# How many distinct things said are listed per hook.
TOP = 3
# How much of each is shown. The log keeps more; a report line does not need it.
SHOWN = 90


def summarise(records):
    """{hook: {decisions, sessions, first, last, kinds, spoke, said}}."""
    hooks = {}
    for one in records:
        name = str(one.get("hook") or "?")
        entry = hooks.setdefault(name, {"decisions": 0, "sessions": set(),
                                        "first": None, "last": None,
                                        "kinds": {}, "spoke": 0, "said": {}})
        entry["decisions"] += 1
        if one.get("session"):
            entry["sessions"].add(one["session"])
        when = str(one.get("when") or "")[:10]
        if when:
            entry["first"] = min(entry["first"] or when, when)
            entry["last"] = max(entry["last"] or when, when)
        kind = str(one.get("decision") or "?")
        entry["kinds"][kind] = entry["kinds"].get(kind, 0) + 1
        said = str(one.get("said") or "").strip()
        if said:
            entry["spoke"] += 1
            key = said[:SHOWN]
            entry["said"][key] = entry["said"].get(key, 0) + 1
    return hooks


def within(records, days):
    """Only the records from the last `days` days."""
    if days is None:
        return records
    cutoff = time.strftime("%Y-%m-%dT%H:%M:%SZ",
                           time.gmtime(time.time() - days * 86400))
    return [r for r in records if str(r.get("when") or "") >= cutoff]


def report(records, bad, where, how):
    print("Hook log: %s" % where)
    if how:
        print("found through %s" % how)
    hooks = summarise(records)
    sessions = set()
    for entry in hooks.values():
        sessions |= entry["sessions"]
    print("%d decision(s) from %d hook(s) in %d session(s); %d unreadable line(s)"
          % (len(records), len(hooks), len(sessions), bad))
    if not records:
        print("")
        print("Nothing to count. A hook that has never decided anything is either")
        print("not wired or not reached - .claude/settings.json says which hooks")
        print("should be running.")
        return
    for name in sorted(hooks):
        entry = hooks[name]
        print("")
        print("%s - %d decision(s) in %d session(s), %s to %s"
              % (name, entry["decisions"], len(entry["sessions"]),
                 entry["first"] or "?", entry["last"] or "?"))
        kinds = sorted(entry["kinds"].items(), key=lambda kv: (-kv[1], kv[0]))
        print("  " + ", ".join("%s %d" % kv for kv in kinds))
        share = 100.0 * entry["spoke"] / entry["decisions"]
        print("  spoke in %d of them (%.0f%%)" % (entry["spoke"], share))
        said = sorted(entry["said"].items(), key=lambda kv: (-kv[1], kv[0]))
        for text, count in said[:TOP]:
            print("    %4dx  %s" % (count, text))


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="How often each Heron hook decided something, and what it said.")
    parser.add_argument("--log", help="read this log file instead of this machine's")
    parser.add_argument("--days", type=float,
                        help="only the decisions from the last N days")
    args = parser.parse_args(argv)

    if args.log:
        where, how = args.log, None
    else:
        where, how = LOG.path()
        if where is None:
            print("No hook log on this machine: %s." % how)
            print("The hooks still run; they have nowhere to write their diary.")
            return 0
    if not os.path.isfile(where) and not os.path.isfile(
            os.path.join(os.path.dirname(where), LOG.OLDER)):
        print("Hook log: %s" % where)
        print("It does not exist yet - no hook has decided anything on this machine.")
        return 0
    records, bad = LOG.read(where)
    report(within(records, args.days), bad, where, how)
    return 0


if __name__ == "__main__":
    sys.exit(main())
