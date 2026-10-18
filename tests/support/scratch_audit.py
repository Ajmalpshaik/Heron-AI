#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

r"""
A test suite's audit lines never land in the owner's real trail - FRAGMENT-ISSUES
row 5b-1070.

    sys.path.insert(0, os.path.join(HERE, "support"))
    import scratch_audit                                         # noqa: E402
    scratch_audit.plant()            # HERON_AUDIT, for the whole run
    scratch_audit.plant(data=True)   # and HERON_DATA - see below

Not a suite: check-gaps runs tests/test_*.py, and this is what they import.
In a folder of its own because every .py directly in tests/ is a suite -
brain/heron_naming.py observes that and test_naming.py holds it.

WHY IT EXISTS. Opening a knowledge store writes knowledge.refresh and
knowledge.restore lines through brain/heron_audit.py into
heron_gaps.brain_audit_dir(), and a planted HERON_KNOWLEDGE does not move
them. With nothing planted that folder is the owner's REAL brain audit -
%USERPROFILE%\.heron\audit since D-135, %APPDATA%\Heron\audit before. The
audit is evidence and is never pruned (D-62), and the gaps report counts its
lines as if a chat had asked. Measured 2026-10-18 on the owner's laptop:
tests/test_score_routing.py, run with nothing planted, made that folder with
two test lines - and it was one of the seven suites that already named
HERON_AUDIT, in the one section that planted it.

ONCE, AT MODULE LEVEL, FOR THE WHOLE RUN. A plant inside one section leaves
every other section writing to the real trail, which is how that suite
leaked. One call before anything can open a store covers every section and
every child started with this process's environment.

HERON_DATA, ONLY WITH data=True. The home that knowledge_dir() and every
project's memory fall back to when HERON_KNOWLEDGE is not set. A suite that
takes HERON_KNOWLEDGE away and then opens a store opens the owner's shared
store on Windows, so such a suite plants this too. It stays opt-in because
on Linux data_home() is None with nothing planted, and a suite written to
see "no home" there would see one.

RESTORED AT EXIT. Both variables go back to what they were - unset if they
were unset - and the folder is removed.

tests/test_audit_planted.py holds every suite that names HERON_KNOWLEDGE to
a module-level call of plant().
"""

import atexit
import os
import shutil
import tempfile

# What this process held before plant(), None for unset; and the one folder.
_before = {}
_folder = []


def plant(data=False):
    """HERON_AUDIT, and with `data` HERON_DATA, at one scratch folder until
    this process ends. Returns the folder.

    A second call never makes a second folder: it adds HERON_DATA if asked
    and puts back a value the suite has since changed."""
    if not _folder:
        _folder.append(tempfile.mkdtemp(prefix="heron-test-audit-"))
        atexit.register(restore)
    root = _folder[0]
    _set("HERON_AUDIT", os.path.join(root, "audit"))
    if data or "HERON_DATA" in _before:
        _set("HERON_DATA", os.path.join(root, "home"))
    return root


def restore():
    """Every variable back as it was before plant(), and the folder gone."""
    for name, value in _before.items():
        if value is None:
            os.environ.pop(name, None)
        else:
            os.environ[name] = value
    _before.clear()
    for root in _folder:
        shutil.rmtree(root, ignore_errors=True)
    del _folder[:]


def _set(name, value):
    if name not in _before:
        _before[name] = os.environ.get(name)
    os.environ[name] = value
