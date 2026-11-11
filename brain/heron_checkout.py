# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Which folders under a checkout are ANOTHER checkout - one answer, for any
walk of this repository that starts where a worktree can sit.

    import heron_checkout as CHECKOUT
    for folder, dirs, files in os.walk(root):
        dirs[:] = CHECKOUT.own(folder, dirs)

WHY IT EXISTS (FRAGMENT-ISSUES row 5b-1539)
-------------------------------------------
A session's worktree sits INSIDE the checkout it was added from, at
`.claude/worktrees/<name>/`, and it is a whole second copy of the repository
at whatever commit that session started from. Git never reads it as part of
this checkout. A walk of the disk does, unless something tells it not to -
and on 2026-11-11, with four of them under the main folder:

  tools/check-compile.py   with no git to ask, exited 1 naming every
                           worktree's .csproj as a project nothing builds -
                           before its own no-SDK answer, so a machine with no
                           compiler read as a broken repository
  tools/module-reach.py    counted every worktree's suite as an importer and
                           emptied two of its four buckets
  tools/measure-routes.py  named each caller once per worktree
  tools/check-structure.py checked every worktree's PowerShell files too

CI saw none of it: a worktree is never committed.

THE RULE IS GIT'S OWN, AND IT NEEDS NO LIST
-------------------------------------------
A folder that holds its own `.git` is a checkout in its own right: a linked
worktree keeps `.git` as a FILE naming its folder under the main repository,
a clone keeps it as a FOLDER. That is how git itself decides where one
working tree stops, and it is the fact `heron_scope.main_checkout()` already
reads to tell a worktree from the checkout it came from.

So nothing here names `.claude/worktrees`, or any other place a tool may put
one. A list of folder names is a guess about where the next harness will put
its copies, and this repository already carries that guess once a walker -
each with a private skip list, no two alike. This is not one more of them:
it is asked AS WELL AS a walker's own list of build output, and it answers
the one question none of them could.

`.gitignore` names `.claude/worktrees/` as well, for git's sake: without the
line a worktree shows as untracked work on a machine whose harness did not
write its own exclude.

IT IMPORTS NOTHING BUT `os`
---------------------------
The same reason `heron_relpath` gives: `tools/check-structure.py` runs on a
machine with nothing installed, and an answer some of its callers cannot
import is not shared.

THE FOLDER A WALK STARTS FROM IS NEVER ASKED ABOUT
--------------------------------------------------
Every checkout holds a `.git`, this one included. Only a folder met BELOW the
start of a walk can be somebody else's, which is why `own()` takes the names
a walk is about to go into rather than the folder it is standing in.
"""

import os


def is_another(folder):
    """True when `folder` holds its own `.git` - a FILE in a linked worktree,
    a FOLDER in a clone. `lexists`, so a `.git` that is a dangling link still
    counts: whatever it points at, the folder was made as a checkout."""
    return os.path.lexists(os.path.join(folder, ".git"))


def own(parent, names):
    """Those of `names`, the folders directly under `parent`, that belong to
    the checkout being walked. In the order given, so a caller that sorted
    them keeps its order."""
    return [name for name in names
            if not is_another(os.path.join(parent, name))]
