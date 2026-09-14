# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-RES-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Resource Manager - what it cannot measure it reports as unmeasured.

    python brain/heron_resources.py

WHAT IT IS FOR (docs/28, HERON-OPS-RES-003)
--------------------------------------------
"CPU, RAM, disk, AI spend, Revit responsiveness. Pauses background work."
T1 - no model call. It reads numbers and answers one question: should
background work pause?

FIVE THINGS IN THAT SENTENCE AND IT CAN SEE THREE
---------------------------------------------------
    CPU                   yes, where the platform offers a load average
    RAM                   yes, where the platform offers one to read
    disk                  yes, everywhere Python runs
    AI spend              NOT HERE. HERON-KRN-TOK-015 is the meter, and
                          two meters for one number is one meter and one
                          argument. Asked of the budget, never counted here
    Revit responsiveness  NOT MEASURABLE FROM `brain` AT ALL. Revit is on
                          the far side of the bridge and `brain` may not
                          depend on `mcp` (D-48). It is reported as
                          unmeasured, every time, and the pause decision
                          says it did not include it

A READING THAT FAILED IS NOT A READING OF ZERO
------------------------------------------------
`/proc/meminfo` exists on Linux and not on Windows. `os.getloadavg` exists
on neither reliably. A resource manager that returned 0% when the file was
missing would report a machine under no pressure at all, on the platform
Heron actually ships to - which is the same shape as the agent registry
reporting an unmeasured health score as zero, and it is why that field says
"unmeasured".

So every reading is `{value, unmeasured, why}`, and a reading that could
not be taken is never compared against a threshold.

AND IT PAUSES RATHER THAN DECIDING
------------------------------------
`should_pause()` answers about BACKGROUND work only. It never says a
person's work should stop - docs/21 s12 has background yielding to
interactive, never the other way round, and a resource manager that could
pause a user is one bad reading away from doing it.

It carries a `state` of NOTHING_MEASURABLE when no reading at all could be
taken, so a caller that wants to branch on "blind" rather than on "busy"
has a name to branch on rather than a boolean that means both.

With every reading unmeasured it answers PAUSE, for the same reason
HERON-OPS-SCH-001 treats silence as "somebody is working": the scheduler
asks this to decide whether to spend somebody's machine, and an answer of
"I could not tell" is not permission.
"""

import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Where background work should yield. Not tuned against anything - these
# are defaults, and `should_pause` reports which one it crossed so a person
# can disagree with the number rather than with the verdict.
BUSY_LOAD_PER_CPU = 0.8
LOW_MEMORY_FRACTION = 0.15
LOW_DISK_FRACTION = 0.10

# What this agent will never claim to have measured.
CANNOT_SEE = {
    "revit_responsiveness":
        "Revit is on the far side of the bridge and `brain` may not depend "
        "on `mcp` (D-48). Nothing in this layer can see whether Revit is "
        "responding, and reporting it as fine would be inventing the one "
        "number a modeller would most want to trust",
    "ai_spend":
        "HERON-KRN-TOK-015 is the meter for spend. Counting it here as well "
        "would be a second meter for one number, which is one meter and one "
        "argument - ask the budget",
}


def _unmeasured(why):
    return {"value": None, "unmeasured": True, "why": why}


def cpu():
    """
    Load average per CPU, or unmeasured.

    Not "CPU percent": a load average is what the platform offers without a
    sampling interval, and inventing a percentage from one instant would be
    a number that looks more precise than the thing behind it.
    """
    if not hasattr(os, "getloadavg"):
        return _unmeasured("os.getloadavg is not available on this platform "
                           "- Windows has no load average, and this is the "
                           "platform Heron ships to")
    try:
        one_minute = os.getloadavg()[0]
    except OSError as exc:
        return _unmeasured("the load average could not be read: %s" % exc)
    count = os.cpu_count() or 1
    return {"value": one_minute / float(count), "unmeasured": False,
            "why": "%.2f load across %d cpu(s), one-minute average"
                   % (one_minute, count)}


def memory():
    """Free memory as a fraction of total, or unmeasured."""
    path = "/proc/meminfo"
    if not os.path.exists(path):
        return _unmeasured("%s is not there. It is a Linux file, and on "
                           "Windows this reading has to come from somewhere "
                           "else that nobody has written yet" % path)
    try:
        values = {}
        with open(path, encoding="utf-8") as handle:
            for line in handle:
                name, _, rest = line.partition(":")
                parts = rest.split()
                if parts and parts[0].isdigit():
                    values[name.strip()] = int(parts[0])
    except (IOError, OSError, ValueError) as exc:
        return _unmeasured("%s could not be read: %s" % (path, exc))
    total = values.get("MemTotal")
    available = values.get("MemAvailable", values.get("MemFree"))
    if not total or available is None:
        return _unmeasured("%s gave no MemTotal or MemAvailable. A partial "
                           "file is not a low-memory machine and is not a "
                           "healthy one either" % path)
    return {"value": available / float(total), "unmeasured": False,
            "why": "%.1f GB free of %.1f GB"
                   % (available / 1048576.0, total / 1048576.0)}


def disk(path=None):
    """Free disk as a fraction of total, or unmeasured."""
    path = path or ROOT
    try:
        usage = shutil.disk_usage(path)
    except (OSError, ValueError) as exc:
        return _unmeasured("the disk holding %s could not be read: %s"
                           % (path, exc))
    if not usage.total:
        return _unmeasured("the disk holding %s reports a total of zero, "
                           "which is not a reading" % path)
    return {"value": usage.free / float(usage.total), "unmeasured": False,
            "why": "%.1f GB free of %.1f GB on the disk holding %s"
                   % (usage.free / 1073741824.0, usage.total / 1073741824.0,
                      path)}


def reading(path=None):
    """
    Everything this agent can and cannot see, in one mapping.

    The two it can never see are in here too, marked unmeasured with the
    reason - an absent key reads as an oversight, and a present one that
    says "no" reads as a decision.
    """
    found = {"cpu_per_core": cpu(), "memory_free": memory(),
             "disk_free": disk(path)}
    for name, why in sorted(CANNOT_SEE.items()):
        found[name] = _unmeasured(why)
    return found


def should_pause(readings=None, path=None):
    """
    {pause, because, unmeasured, why} for BACKGROUND work only.

    Never about a person's work. docs/21 s12 has background yielding to
    interactive and never the other way round, and a resource manager that
    could pause a user is one bad reading away from doing it.
    """
    readings = reading(path) if readings is None else readings

    because, unmeasured = [], []
    for name, found in sorted(readings.items()):
        if found.get("unmeasured"):
            unmeasured.append("%s: %s" % (name, found.get("why")))
            continue
        value = found.get("value")
        if name == "cpu_per_core" and value is not None \
                and value >= BUSY_LOAD_PER_CPU:
            because.append("cpu_per_core is %.2f, at or over %.2f - %s"
                           % (value, BUSY_LOAD_PER_CPU, found.get("why")))
        elif name == "memory_free" and value is not None \
                and value <= LOW_MEMORY_FRACTION:
            because.append("memory_free is %.0f%%, at or under %.0f%% - %s"
                           % (value * 100, LOW_MEMORY_FRACTION * 100,
                              found.get("why")))
        elif name == "disk_free" and value is not None \
                and value <= LOW_DISK_FRACTION:
            because.append("disk_free is %.0f%%, at or under %.0f%% - %s"
                           % (value * 100, LOW_DISK_FRACTION * 100,
                              found.get("why")))

    measured = [n for n, f in readings.items() if not f.get("unmeasured")]
    if not measured:
        # NOTHING COULD BE READ. The scheduler asks this to decide whether
        # to spend somebody's machine, and "I could not tell" is not
        # permission - the same reading HERON-OPS-SCH-001 gives silence.
        return {"pause": True, "state": "NOTHING_MEASURABLE",
                "because": [], "unmeasured": unmeasured,
                "why": "NOTHING could be measured, so background work "
                       "pauses. An answer of 'I could not tell' is not "
                       "permission to use somebody's machine."}

    return {"pause": bool(because), "state": None, "because": because,
            "unmeasured": unmeasured,
            "why": ("%d reading(s) say background work should pause"
                    % len(because)) if because else
                   ("%d reading(s) taken and none is under pressure; %d "
                    "could not be read and are not counted either way"
                    % (len(measured), len(unmeasured)))}


def main(argv):
    print("RESOURCE MANAGER   three of the five, and it says which three")
    print("=" * 72)
    found = reading()
    for name in sorted(found):
        entry = found[name]
        if entry["unmeasured"]:
            print("  %-22s UNMEASURED" % name)
            print("      %s" % entry["why"][:92])
        else:
            print("  %-22s %-8.3f %s" % (name, entry["value"],
                                         entry["why"][:52]))

    print()
    answer = should_pause(found)
    print("  should background work pause?   %s"
          % ("YES" if answer["pause"] else "no"))
    print("      %s" % answer["why"])
    for line in answer["because"]:
        print("      because: %s" % line[:88])

    print()
    print("  With nothing readable at all:")
    blind = dict((name, _unmeasured("this machine could not say"))
                 for name in found)
    answer = should_pause(blind)
    print("    pause: %s - %s" % ("YES" if answer["pause"] else "no",
                                  answer["why"][:84]))

    print()
    print("  It never answers about a PERSON'S work. docs/21 s12 has")
    print("  background yielding to interactive and never the other way,")
    print("  and a resource manager that could pause a user is one bad")
    print("  reading away from doing it.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
