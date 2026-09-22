# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
A path as a person reads it - the one relpath that never raises.

    import heron_relpath as RELPATH
    RELPATH.relpath(path, ROOT)

WHY IT EXISTS
-------------
`os.path.relpath` RAISES on Windows when its two paths are on different
drives - `ValueError: path is on mount 'C:', start on mount 'D:'`. The
owner's PC keeps the repository on D: and the temp folder on C:, so a
redirected output, a scratch folder or a folder somebody typed meets a ROOT
it has no relative form against.

Almost every call is DISPLAY: a refusal naming the file it refused, a
closing line saying where something was written. So the failure is absurd -
a tool that has finished its work, or is cleanly refusing to, crashes while
formatting the sentence that says so. A DISPLAY HELPER MUST NEVER BE ABLE TO
RAISE. Where there is no relative form, the absolute path is the answer: a
worse message, never a worse outcome.

CI cannot see any of it. Linux has one mount and relpath always answers
there, so every instance reached main looking green and was found on the one
machine it matters on - FRAGMENT-ISSUES rows 163, 5b-17 and 5b-152.

WHY A MODULE OF ITS OWN, IMPORTING NOTHING BUT `os`
----------------------------------------------------
The rule already had a home, `heron_fragment.repo_relative()`, and copies
kept appearing anyway: `tools/check-licence.py` carried one, and
`tools/api-changes.py` grew another on 2026-09-22. check-licence.py wrote
down why, and it is the whole cause - importing heron_fragment costs
PyYAML, and some callers must run without it:

  tools/build-release-assets.py   .github/workflows/release.yml installs no
                                  PyYAML before it runs this
  tools/check-licence.py          run before you trust a download enough to
                                  install anything for it
  tools/api-changes.py            given its releases, answers NOT RUN
                                  without importing heron_fragment

An answer half its callers cannot import is not shared. Each of them writes
its own, and a copy is where a rule goes stale. So the rule lives here, and
heron_fragment.repo_relative() asks it like everything else does.
tests/test_relpath.py imports this module, and runs the first two tools
above, with PyYAML blocked - and fails the day that stops being true.

`start` IS THE CALLER'S
-----------------------
Every caller passes its own ROOT, read at the moment it calls. Suites point
a tool's ROOT at a scratch folder, and a helper holding a ROOT of its own
would quietly answer against the real checkout instead.

WHICH CALLS NEED IT
-------------------
A raw `os.path.relpath` is at risk only when its `path` can come from
outside the checkout: a command-line folder, a redirected output, a temp
directory. A path joined onto ROOT, or walked from under it, is on ROOT's
drive by construction - the call is lexical and never looks at the disk, so
not even a junction inside the checkout can make it raise. Audited
2026-09-22 across tools/, brain/ and mcp/: the unguarded calls reachable
from another drive were `build-release-assets.py --out`, which raised after
checksums.txt was written, and `prove-skill.py --jobs`, which raised after
the first job file. Row 5b-152 has the audit.
"""

import os


def relpath(path, start):
    """`path` written against `start`, or whole when it cannot be.

    Whole means `os.path.abspath(path)`, not `path` as it was given: a caller
    that joins the answer back onto `start` must still reach the same file,
    and `os.path.join` discards everything before an absolute component.
    """
    try:
        return os.path.relpath(path, start)
    except ValueError:
        # Windows, and the two are on different drives: there is no relative
        # form to give.
        return os.path.abspath(path)
