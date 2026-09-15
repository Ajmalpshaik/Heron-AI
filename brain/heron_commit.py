# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-CMT-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Commits - what may be staged, and no opinion at all about the wording.

    python brain/heron_commit.py

WHAT IT IS FOR (docs/28, HERON-GIT-CMT-004)
--------------------------------------------
"Stages and writes commits." T2, risk MODIFY. It stages nothing and
writes nothing: a verdict comes back.

NO MESSAGE SHAPE IS ENFORCED, BECAUSE NOBODY HAS ADOPTED ONE
---------------------------------------------------------------
This project has no commit convention - PROPOSALS F23 records that, and
records why it matters: `Fixed` and `Improved` cannot be told apart from
a diff, so a change log will eventually need one. Until somebody adopts
it deliberately, a shape enforced here would BECOME it. The same wall
HERON-NAM-GEN-001 and HERON-GIT-BRN-003 both stopped at.

Writing the message is prose, which is the host's under D-01 - and that
is the one scoped call that makes this row T2. This agent makes none.

WHAT IT DOES HAVE AN OPINION ABOUT IS WHAT GOES IN
----------------------------------------------------
A commit is forever in a way nothing else here is. A report can be
withdrawn, a pull request closed, a release yanked - a file that reaches
history stays in every clone until somebody rewrites it for everybody.
So two things are refused outright:

  A REVIT MODEL, by its name alone. D-26 - the line is the FILE. The
  extension list is heron_release's.

  A GENERATED PAGE. Each is rebuilt from the repository in a second and
  none of them is meant to be committed. The list is read out of
  .gitignore's own "Generated pages" block rather than typed here, so a
  fifth page added there is refused with no edit - and if that block
  cannot be found, staging is REFUSED rather than checked against a list
  this file invented.

THE MESSAGE IS READ FOR ONE THING
-----------------------------------
A credential. Refused whole, never stripped, and the value never
appears in the answer - the same rule HERON-GIT-PR-005 applies to a
pull request body, for the same reason and with a longer memory.
"""

from __future__ import annotations

import io
import os
import posixpath
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_release as RELEASE  # noqa: E402
import heron_secrets as SECRETS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

NEVER_LEAVES = RELEASE.NEVER_LEAVES

IGNORE = os.path.join(ROOT, ".gitignore")
BLOCK = "# Generated pages."


def generated(ignore=None):
    """
    The generated pages, read out of .gitignore's own block.

    Read rather than typed so a fifth page added there is refused with
    no edit here. Returns [] when the block cannot be found, and the
    caller REFUSES on that rather than checking against a guess.
    """
    path = ignore or IGNORE
    try:
        text = io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return []
    if BLOCK not in text:
        return []
    out = []
    for line in text.split(BLOCK, 1)[1].split("\n")[1:]:
        line = line.strip()
        if not line:
            break
        if line.startswith("#"):
            continue
        out.append(line)
    return out


def _tidy(path):
    return posixpath.normpath(str(path).strip().replace("\\", "/"))


def stage(paths, message=None, secrets=None, ignore=None):
    """
    {staged, may_commit, why} - a verdict. Nothing is staged or written.
    """
    if not paths:
        return {"prepared": False, "refused": "NOTHING_TO_STAGE",
                "why": "no paths were handed in. A commit with nothing in "
                       "it tells a reader something happened when nothing "
                       "did."}

    seen = []
    for path in paths:
        if not isinstance(path, str) or not path.strip():
            return {"prepared": False, "refused": "NOT_A_PATH",
                    "why": "%r is not a path." % (path,)}
        tidy = _tidy(path)
        if os.path.isabs(tidy) or tidy.startswith(".."):
            return {"prepared": False, "refused": "OUTSIDE_THE_REPOSITORY",
                    "why": "'%s' points outside the repository, and a "
                           "commit can only carry what is inside it." % tidy}
        seen.append(tidy)

    binaries = RELEASE._binaries(seen)
    if binaries:
        return {"prepared": False, "refused": "CARRIES_A_MODEL",
                "paths": [name for name, _ in binaries],
                "why": "%s. D-26 draws the line at the FILE, not at the "
                       "information in it - and a commit is forever in a "
                       "way nothing else here is. A report can be "
                       "withdrawn and a release yanked; a file that "
                       "reaches history stays in every clone until "
                       "somebody rewrites it for everybody."
                       % "; ".join("'%s' is a %s" % (name, extension)
                                   for name, extension in binaries)}

    pages = generated(ignore)
    if not pages:
        return {"prepared": False, "refused": "NO_IGNORE_LIST",
                "why": "'%s' could not be found in %s, so nothing here can "
                       "say which files are generated. A list invented here "
                       "would be a commit checked against this file's "
                       "opinion rather than against what the repository "
                       "says." % (BLOCK, os.path.basename(ignore or IGNORE))}

    caught = [path for path in seen if path in pages
              or posixpath.basename(path) in pages]
    if caught:
        return {"prepared": False, "refused": "CARRIES_A_GENERATED_PAGE",
                "paths": caught, "generated": pages,
                "why": "%s %s generated - rebuilt from the repository in a "
                       "second, and read out of .gitignore's own block "
                       "rather than listed here. Committing one puts a "
                       "snapshot in history that is wrong the moment "
                       "anything changes, and right-looking forever."
                       % (", ".join("'%s'" % one for one in caught),
                          "is" if len(caught) == 1 else "are")}

    said = str(message or "").strip()
    if not said:
        return {"prepared": False, "refused": "NO_MESSAGE",
                "asked": "What does this change do, and why?",
                "why": "the commit has no message. Nothing here enforces a "
                       "SHAPE - this project has adopted no convention "
                       "(PROPOSALS F23) and one enforced here would become "
                       "it - but a message that is not there is not a "
                       "shape question."}

    keeper = secrets if secrets is not None else SECRETS.Secrets()
    _, found = keeper.redact(said)
    if found:
        return {"prepared": False, "refused": "CARRIES_A_SECRET",
                "found": sorted(set(found)),
                "why": "the message carries %s. Refused WHOLE rather than "
                       "stripped, and the value is not in this answer - "
                       "only that it was there. A commit message is the "
                       "one place a credential is copied into every clone "
                       "and every mirror at once."
                       % ", ".join(sorted(set(found)))}

    return {
        "prepared": True, "may_commit": True, "paths": seen,
        "message": said, "of": len(seen), "generated": pages,
        "why": "%d path(s), none generated and none a Revit model, with a "
               "message carrying no credential. Nothing was staged."
               % len(seen),
        "unjudged": [
            "THE WORDING. This project has adopted no commit convention "
            "(PROPOSALS F23) and a shape enforced here would become it - "
            "the same wall HERON-NAM-GEN-001 and HERON-GIT-BRN-003 both "
            "stopped at. Writing the message is prose, which is the host's "
            "under D-01, and that is the one scoped call making this row "
            "T2. This agent makes none.",
            "WHETHER THESE PATHS BELONG TOGETHER. A commit doing four "
            "unrelated things passes every check here, and nothing "
            "measured whether it should have been four commits.",
            "THE %d GENERATED PAGE(S) WERE READ OUT OF .gitignore (%s), "
            "not listed here - so a fifth one added there is refused with "
            "no edit to this file." % (len(pages), ", ".join(pages)),
            "WHAT IS IN THE FILES. Their NAMES were checked; nothing here "
            "opened one. A credential inside a staged file is "
            "HERON-KRN-SEC-012's, and it is not this agent's claim to have "
            "looked.",
        ],
    }


def main(argv):
    print("COMMITS   what may be staged, and no opinion about the wording")
    print("=" * 72)

    print("\nthe generated pages, read out of .gitignore")
    for one in generated():
        print("  %s" % one)

    answer = stage(["brain/heron_commit.py", "tests/test_commit.py"],
                   "GitHub: the Commit Agent")
    print("\n%s" % answer["why"])

    print("\nrefused")
    for paths, message, kwargs in (
            ([], "x", {}),
            ([None], "x", {}),
            (["/etc/passwd"], "x", {}),
            (["Tower A.rvt"], "x", {}),
            (["skill-catalog.html"], "x", {}),
            (["brain/x.py"], "x", {"ignore": "/nowhere/.gitignore"}),
            (["brain/x.py"], "", {}),
            (["brain/x.py"], "token ghp_" + "A" * 36, {})):
        bad = stage(paths, message, **kwargs)
        print("  %-28s %s" % (bad["refused"], bad["why"][:38]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
