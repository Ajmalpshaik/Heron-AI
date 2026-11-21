#!/usr/bin/env python3
# Heron-Agent:  HERON-SKL-VAL-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A skill job file names only cards that are live - row 5b-1956.

    python tests/test_skill_jobs_current.py

WHAT HAPPENED
-------------
The ten job files in tools/jobs/skills/ were written by tools/prove-skill.py on
2026-09-19 and never again. On 2026-11-01 select-by-category-name was merged
into filter-elements-by-category and retired (DEPRECATED), and every one of
the ten still arranged its setup with it - a person following one on the PC
would send a retired card, or be refused, with nothing here saying so.

WHAT THIS HOLDS
---------------
  1. Every fragment a skill job file names - in `defaults.setup`, as a job's
     `fragment`, or in a job's own `setup` - is a card in the library, and
     none is DEPRECATED or ARCHIVED.
  2. Every `*-model-half.yaml` names a skill Heron has, and each job's
     fragment provides a capability that skill declares.
  3. Each file carries the generator's current sentence about running the
     composition - tools/run-skill.py - and not the one from before it.

WHAT IT DOES NOT HOLD
---------------------
That a file is byte for byte what prove-skill.py writes today. The plans move
with every card that changes, and a gate on that would turn unrelated pull
requests red; regenerating is `python tools/prove-skill.py --plan-only --jobs
<folder>`, and this suite is what catches a file left naming a retired card.
"""

import glob
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

import yaml                                                    # noqa: E402

import heron_fragment as HF                                    # noqa: E402
import heron_skill as SKILL                                    # noqa: E402

FAILURES = []
RETIRED = ("DEPRECATED", "ARCHIVED")
JOBS = os.path.join(ROOT, "tools", "jobs", "skills")


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def named(job):
    """Every fragment slug the job file names, with where."""
    out = []
    defaults = job.get("defaults") or {}
    for slug in defaults.get("setup") or []:
        out.append((slug, "defaults.setup"))
    for number, row in enumerate(job.get("jobs") or [], 1):
        if not isinstance(row, dict):
            continue
        if row.get("fragment"):
            out.append((row["fragment"], "job %d" % number))
        for slug in row.get("setup") or []:
            out.append((slug, "job %d setup" % number))
    return out


def main():
    found, _ = HF.load_all()
    by_slug = dict((f.slug, f) for f in found.values())
    skills = SKILL.load_all()[0]
    files = sorted(glob.glob(os.path.join(JOBS, "*-model-half.yaml")))
    check(files, "there are skill job files to read (%d)" % len(files))

    print("1. every card a job file names is live")
    for path in files:
        job = yaml.safe_load(io.open(path, encoding="utf-8").read()) or {}
        bad = []
        for slug, where in named(job):
            frag = by_slug.get(slug)
            if frag is None:
                bad.append("%s (%s): no such card" % (slug, where))
            elif frag.status in RETIRED:
                bad.append("%s (%s): %s" % (slug, where, frag.status))
        check(not bad, "%s - %s" % (os.path.basename(path), "; ".join(bad) or "every card live"))

    print("2. each job serves the skill the file is for")
    for path in files:
        skill_id = os.path.basename(path)[:-len("-model-half.yaml")]
        skill = skills.get(skill_id)
        if skill is None:
            check(False, "%s names no skill Heron has" % os.path.basename(path))
            continue
        job = yaml.safe_load(io.open(path, encoding="utf-8").read()) or {}
        needs = set(skill.needs())
        stray = [row.get("fragment") for row in job.get("jobs") or []
                 if isinstance(row, dict) and row.get("fragment") in by_slug
                 and by_slug[row["fragment"]].data.get("capability") not in needs]
        check(not stray, "%s - every job is a capability %s declares%s"
              % (os.path.basename(path), skill_id, (": not %s" % stray) if stray else ""))

    print("3. each says where the composition is run")
    for path in files:
        text = io.open(path, encoding="utf-8").read()
        check("tools/run-skill.py" in text and "NOTHING HERE RUNS" not in text,
              "%s points at tools/run-skill.py" % os.path.basename(path))

    print("")
    if FAILURES:
        print("%d failure(s)." % len(FAILURES))
        return 1
    print("Every skill job file names only live cards.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
