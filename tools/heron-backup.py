# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-BAK-010, HERON-WSP-RST-011
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The Backup and Restore Agents - the user's own data, and getting it back.

    python tools/heron-backup.py backup            take one
    python tools/heron-backup.py list              what exists
    python tools/heron-backup.py verify [<name>]   is it still intact
    python tools/heron-backup.py drill             the round trip, into scratch
    python tools/heron-backup.py restore <name> --confirm

WHAT IS ACTUALLY AT RISK, MEASURED BEFORE THIS WAS WRITTEN
-----------------------------------------------------------
`%APPDATA%\\Heron` holds four files. Three of them matter and nothing anywhere
in this repository was protecting any of them:

    audit/*.jsonl     Heron's only record of what it has ever done. docs/12
                      makes it evidence, HeronAudit never prunes it, and it is
                      NOT in git. Lose it and the Capability Gap report has
                      nothing to read and no way to get it back.
    config/           the settings, including write.enabled - the switch D-19
                      puts writing behind, which HeronPermissions reads before
                      it will allow MODIFY or above.

                      This line cited "docs/12 section 9" until 2026-09-19.
                      There is no section 9 in docs/12 - it has six - and that
                      document never mentions write.enabled at all. The wrong
                      citation had already been copied into docs/07 by somebody
                      reading this docstring and trusting it, which is what a
                      stale reference does: it does not stay in one file.
    knowledge/        see below.

A grep for backup across every .py, .cs and .ps1 in the repository returned
nothing. This is that.

WHY AN EXCLUDE-LIST AND NOT AN INCLUDE-LIST
--------------------------------------------
docs/21 section 8 is explicit that the derived index must not be backed up -
"backing up a derived artefact adds size and creates the risk of restoring a
stale index over fresh knowledge". So `knowledge/global.db` is excluded.

But it is excluded BY NAME, from an otherwise complete copy, rather than by
listing the two things worth keeping. An include-list is a decision taken today
about files that will exist tomorrow: the day somebody drops a personal
fragment library into the data folder, an include-list silently does not back
it up and nobody finds out until they need it. An exclude-list backs up the new
thing by default and is wrong only in the direction of keeping too much.

Every exclusion is named in the manifest with its reason, so a restore can say
what it is NOT bringing back.

A DISAGREEMENT WORTH RECORDING RATHER THAN RESOLVING QUIETLY
-------------------------------------------------------------
`heron_scope.knowledge_dir()` calls that folder DATA - "the user's knowledge,
it roams with them, and a cache wipe must never take it". docs/21 section 7
calls the index DERIVED and says rebuilding it must always be safe. Both are
right about different things: the FOLDER is the user's, and the `global.db`
sitting in it today is a SQLite index that `_Open()` rebuilds from the fragment
files whenever it is empty. So the file is skipped and the folder is not, and
if canonical knowledge ever lands there it is backed up without anybody having
to remember to change this.

RESTORE IS TESTED, BECAUSE THE SPEC SAYS A BELIEF IS NOT A RESTORE PATH
------------------------------------------------------------------------
docs/21 section 8: *"Restore must be tested, not merely implemented. An
untested restore path is not a restore path - it is a belief. Recommend a
periodic automated drill: restore into a scratch location, rebuild the index,
run health checks, compare."*

`drill` is that, and it is why the Restore Agent is built in the same commit as
the Backup Agent rather than after it. A backup nobody has restored is a
folder of files with a hopeful name.

WHAT THIS DOES NOT PROTECT AGAINST, SAID PLAINLY
-------------------------------------------------
By default the copy lands under `%APPDATA%\\Heron\\backup`, because docs/06
section 2 lists Backup in the Data class. That survives an uninstall, an
update, and somebody deleting the audit folder. **It does not survive losing
the disk, or the profile.** Pass `--to` a path on another drive for that. The
default is the useful one, not the complete one, and calling it disaster
recovery would be a lie.
"""

import io
import os
import re
import sys
import json
import shutil
import hashlib
import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

MANIFEST = "manifest.json"

# Skipped, with the reason a restore will quote. Matched against the path
# relative to the data root, with forward slashes, case-insensitively.
EXCLUDED = (
    (re.compile(r"^knowledge/.*\.db$", re.I),
     "a derived SQLite index, rebuilt from the fragment files on first use "
     "(docs/21 s7). Restoring a stale one over fresh knowledge is the risk "
     "docs/21 s8 names"),
    (re.compile(r"^backup/", re.I),
     "the backup folder itself - copying backups into backups"),
    (re.compile(r".*\.tmp$", re.I), "a temporary file"),
)


def w(s):
    sys.stdout.write(s.encode("ascii", "replace").decode("ascii"))


def data_root():
    """
    `%APPDATA%\\Heron` - the Data class, and the only thing worth backing up.

    HERON_DATA overrides it, which is what the drill and the tests use. Python
    cannot call HeronPaths, which is C# and the authority; this resolves the
    one directory and nothing else builds a path here.
    """
    override = os.environ.get("HERON_DATA")
    if override:
        return override
    appdata = os.environ.get("APPDATA")
    if not appdata:
        return None
    return os.path.join(appdata, "Heron")


def backup_root():
    """Where copies go. Under Data by default - see the caveat in the header."""
    override = os.environ.get("HERON_BACKUP")
    if override:
        return override
    root = data_root()
    return os.path.join(root, "backup") if root else None


def excluded_reason(relative):
    for pattern, why in EXCLUDED:
        if pattern.match(relative):
            return why
    return None


def digest(path):
    h = hashlib.sha256()
    with io.open(path, "rb") as handle:
        while True:
            chunk = handle.read(65536)
            if not chunk:
                break
            h.update(chunk)
    return h.hexdigest()


def survey(root):
    """(kept, skipped) - every file under the data root, decided."""
    kept, skipped = [], []
    for base, dirs, files in os.walk(root):
        dirs.sort()
        for name in sorted(files):
            full = os.path.join(base, name)
            relative = os.path.relpath(full, root).replace(os.sep, "/")
            why = excluded_reason(relative)
            if why:
                skipped.append({"path": relative, "why": why})
            else:
                kept.append(relative)
    return kept, skipped


def take(destination=None, label=None):
    """Copy the data class. Returns the manifest, or None with a reason printed."""
    root = data_root()
    if not root or not os.path.isdir(root):
        w("There is no %s to back up. Heron has not written anything yet.\n"
          % (root or "data folder"))
        return None

    where = destination or backup_root()
    stamp = label or datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    target = os.path.join(where, stamp)
    if os.path.exists(target):
        w("A backup called %s already exists. Nothing was written.\n" % stamp)
        return None

    kept, skipped = survey(root)
    files = []
    for relative in kept:
        source = os.path.join(root, relative.replace("/", os.sep))
        landing = os.path.join(target, relative.replace("/", os.sep))
        folder = os.path.dirname(landing)
        if not os.path.isdir(folder):
            os.makedirs(folder)
        shutil.copy2(source, landing)
        files.append({"path": relative,
                      "bytes": os.path.getsize(landing),
                      "sha256": digest(landing)})

    manifest = {
        "taken": datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "source": root,
        "files": files,
        "excluded": skipped,
        "note": ("Backs up the DATA class only. The derived index is excluded "
                 "on purpose and is rebuilt on first use - docs/21 s8."),
    }
    io.open(os.path.join(target, MANIFEST), "w", encoding="utf-8").write(
        json.dumps(manifest, indent=2, sort_keys=True))
    manifest["at"] = target
    return manifest


def read_manifest(path):
    try:
        return json.loads(io.open(os.path.join(path, MANIFEST),
                                  encoding="utf-8").read())
    except (IOError, OSError, ValueError):
        return None


def backups():
    """Every backup that has a readable manifest, newest first."""
    where = backup_root()
    if not where or not os.path.isdir(where):
        return []
    found = []
    for name in sorted(os.listdir(where), reverse=True):
        path = os.path.join(where, name)
        if not os.path.isdir(path):
            continue
        manifest = read_manifest(path)
        if manifest:
            found.append((name, path, manifest))
    return found


def verify(path, manifest=None):
    """
    Is every file still exactly what was copied. Returns a list of problems.

    Checked by CONTENT, not by size or timestamp. A backup whose verification
    is "the file is still there" answers a question nobody was worried about.
    """
    manifest = manifest or read_manifest(path)
    if not manifest:
        return ["no readable manifest - this is not a backup this tool wrote"]

    problems = []
    for entry in manifest.get("files", []):
        full = os.path.join(path, entry["path"].replace("/", os.sep))
        if not os.path.exists(full):
            problems.append("%s is missing" % entry["path"])
            continue
        if digest(full) != entry["sha256"]:
            problems.append("%s has changed since it was copied" % entry["path"])
    return problems


def restore(path, into=None, confirm=False):
    """
    Put a backup back. Returns (restored, problems).

    IT TAKES A SAFETY COPY FIRST, ALWAYS. Restoring is the one operation here
    that destroys something, and the thing it destroys is the only copy of what
    was on the machine a second ago. If the backup turns out to be the wrong
    one, the state it replaced has to still exist.
    """
    manifest = read_manifest(path)
    if not manifest:
        return [], ["no readable manifest at %s" % path]

    problems = verify(path, manifest)
    if problems:
        return [], ["refusing to restore a backup that does not verify"] + problems

    target = into or data_root()
    if not target:
        return [], ["there is nowhere to restore to"]

    if not confirm:
        return [], ["restore needs --confirm. It would overwrite %d file(s) in %s"
                    % (len(manifest["files"]), target)]

    safety = None
    if os.path.isdir(target) and any(
            os.path.exists(os.path.join(target, e["path"].replace("/", os.sep)))
            for e in manifest["files"]):
        safety = take(label="before-restore-" +
                      datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))

    restored = []
    for entry in manifest["files"]:
        source = os.path.join(path, entry["path"].replace("/", os.sep))
        landing = os.path.join(target, entry["path"].replace("/", os.sep))
        folder = os.path.dirname(landing)
        if not os.path.isdir(folder):
            os.makedirs(folder)
        shutil.copy2(source, landing)
        restored.append(entry["path"])

    if safety:
        w("The state it replaced was copied to %s first.\n" % safety["at"])
    return restored, []


def drill():
    """
    The round trip, into a scratch location. docs/21 section 8 asks for exactly
    this and calls an untested restore path a belief.

    Take a backup, restore it somewhere that is NOT the live data folder, and
    compare content hashes both ways. Nothing the user has is touched.
    """
    import tempfile
    w("DRILL - back up, restore into scratch, compare\n")
    w("=" * 58 + "\n")

    scratch = tempfile.mkdtemp(prefix="heron-drill-")
    try:
        made = take(destination=os.path.join(scratch, "copies"), label="drill")
        if not made:
            return 1
        w("  backed up %d file(s), skipped %d\n"
          % (len(made["files"]), len(made["excluded"])))

        problems = verify(made["at"])
        w("  verify: %s\n" % ("every file matches its hash" if not problems
                              else "; ".join(problems)))
        if problems:
            return 1

        landing = os.path.join(scratch, "restored")
        restored, failed = restore(made["at"], into=landing, confirm=True)
        if failed:
            for line in failed:
                w("  FAIL %s\n" % line)
            return 1
        w("  restored %d file(s) into a scratch folder\n" % len(restored))

        mismatched = []
        for entry in made["files"]:
            back = os.path.join(landing, entry["path"].replace("/", os.sep))
            if not os.path.exists(back):
                mismatched.append("%s did not come back" % entry["path"])
            elif digest(back) != entry["sha256"]:
                mismatched.append("%s came back different" % entry["path"])
        if mismatched:
            for line in mismatched:
                w("  FAIL %s\n" % line)
            return 1

        w("  compare: all %d file(s) came back byte for byte\n" % len(restored))
        w("\n")
        w("The restore path works. The index was NOT part of it and is rebuilt\n")
        w("on first use, which is what docs/21 s8 asks for.\n")
        w("Nothing under %s was touched.\n" % (data_root() or "the data folder"))
        return 0
    finally:
        shutil.rmtree(scratch, ignore_errors=True)


def main(argv):
    command = argv[0] if argv else "backup"

    if command == "backup":
        destination = None
        if "--to" in argv:
            try:
                destination = argv[argv.index("--to") + 1]
            except IndexError:
                w("--to needs a folder\n")
                return 2
        made = take(destination)
        if not made:
            return 1
        total = sum(f["bytes"] for f in made["files"])
        w("Backed up %d file(s), %d KB, to %s\n"
          % (len(made["files"]), total // 1024, made["at"]))
        for entry in made["excluded"]:
            w("  skipped %s - %s\n" % (entry["path"], entry["why"]))
        if not os.environ.get("HERON_BACKUP") and destination is None:
            w("\nThis sits beside what it protects. It survives an uninstall and\n"
              "a deleted audit folder; it does NOT survive losing the disk.\n"
              "Use --to on another drive for that.\n")
        return 0

    if command == "list":
        found = backups()
        if not found:
            w("No backups yet. Run:  python tools/heron-backup.py backup\n")
            return 0
        for name, path, manifest in found:
            w("%-28s %s  %d file(s)\n"
              % (name, manifest.get("taken", "?"), len(manifest.get("files", []))))
        return 0

    if command == "verify":
        found = backups()
        wanted = argv[1] if len(argv) > 1 else None
        if not found:
            w("No backups to verify.\n")
            return 1
        rows = [f for f in found if wanted is None or f[0] == wanted]
        if not rows:
            w("No backup called %s.\n" % wanted)
            return 1
        bad = 0
        for name, path, manifest in rows:
            problems = verify(path, manifest)
            w("%-28s %s\n" % (name, "intact" if not problems
                              else "%d PROBLEM(S)" % len(problems)))
            for line in problems:
                w("    %s\n" % line)
            bad += len(problems)
        return 1 if bad else 0

    if command == "drill":
        return drill()

    if command == "restore":
        if len(argv) < 2:
            w("restore needs the name of a backup. Run `list` to see them.\n")
            return 2
        wanted = argv[1]
        rows = [f for f in backups() if f[0] == wanted]
        if not rows:
            w("No backup called %s.\n" % wanted)
            return 1
        restored, problems = restore(rows[0][1], confirm="--confirm" in argv)
        for line in problems:
            w("%s\n" % line)
        if problems:
            return 1
        w("Restored %d file(s) from %s.\n" % (len(restored), wanted)),
        w("The derived index was not restored - Heron rebuilds it on first use.\n")
        return 0

    w("Commands: backup [--to <folder>] | list | verify [<name>] | drill | "
      "restore <name> --confirm\n")
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
