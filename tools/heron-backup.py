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

TWO FOLDERS SINCE D-135, AND EACH GOES BACK WHERE IT CAME FROM
-----------------------------------------------------------------
D-135 moved the Python side's DATA - the knowledge folder, each project's
memory, the brain's audit - out of `%APPDATA%\\Heron` into the home that
`heron_paths.data_home()` names, `%USERPROFILE%\\.heron` on Windows. The
add-in's settings and trail stayed in `%APPDATA%\\Heron`. So with no
HERON_DATA one backup holds both, each under a folder of its own named for
its part (`appdata/`, `home/`), and its manifest records each part's
`source`. A restore puts each part back into that recorded folder, never the
other, and refuses before writing anything when one of them cannot be made on
this PC (row 5b-1069).

With HERON_DATA set it is that one folder, laid out flat, exactly as before:
that is what the drill's tests plant, and a person who points HERON_DATA
somewhere has said which folder they mean.

WHAT THIS DOES NOT PROTECT AGAINST, SAID PLAINLY
-------------------------------------------------
By default the copy lands under `<home>\\backup`, because docs/06 section 2
lists Backup in the Data class. It was `%APPDATA%\\Heron\\backup` until
row 5b-1069, which inside a Microsoft Store Claude is the app's private copy:
a Reset deletes it along with what it backs up. Backups already taken there
are still listed. That survives an uninstall, an update, and somebody
deleting the audit folder. **It does not survive losing the disk, or the
profile.** Pass `--to` a path on another drive for that. The default is the
useful one, not the complete one, and calling it disaster recovery would be
a lie.
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

import heron_paths as PATHS                                   # noqa: E402

MANIFEST = "manifest.json"

# The two folders a backup holds with no HERON_DATA, in this order, each under
# a folder of this name inside the backup.
APPDATA_PART = "appdata"
HOME_PART = "home"

# Skipped, with the reason a restore will quote. Matched against the path
# relative to each folder backed up, with forward slashes, case-insensitively.
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


def _same(one, other):
    return os.path.normcase(os.path.abspath(one)) == \
        os.path.normcase(os.path.abspath(other))


def appdata_root():
    """
    `%APPDATA%\\Heron` - the add-in's DATA: its settings and its audit trail.

    Python cannot call HeronPaths, which is C# and the authority; this resolves
    the one directory and nothing else builds a path here.
    """
    appdata = (os.environ.get("APPDATA") or "").strip()
    return os.path.join(appdata, "Heron") if appdata else None


def sources():
    """
    [(part, folder)] - what one backup copies, in order, there or not.

    HERON_DATA set: that folder alone, part None, laid out flat - what the
    drill and the tests use, and a person's own choice of folder. Otherwise
    `%APPDATA%\\Heron` and the home `heron_paths.data_home()` names, where
    D-135 moved the knowledge folder, each project's memory and the brain's
    audit. `data_home()` is a name only: asking it carries nothing.
    """
    chosen = (os.environ.get("HERON_DATA") or "").strip()
    if chosen:
        return [(None, chosen)]
    found = []
    appdata = appdata_root()
    if appdata:
        found.append((APPDATA_PART, appdata))
    home = PATHS.data_home()
    if home and not (appdata and _same(home, appdata)):
        found.append((HOME_PART, home))
    return found


def backup_root():
    """Where copies go: `<home>\\backup` by default - see the header for why
    not `%APPDATA%\\Heron\\backup` any more - and that only with no home."""
    override = os.environ.get("HERON_BACKUP")
    if override:
        return override
    home = PATHS.data_home()
    if home:
        return os.path.join(home, "backup")
    appdata = appdata_root()
    return os.path.join(appdata, "backup") if appdata else None


def old_backup_root():
    """`%APPDATA%\\Heron\\backup`, where a backup with no --to went until row
    5b-1069 - listed still, so one taken there is not lost from view. None
    when HERON_BACKUP or HERON_DATA names the place itself."""
    if os.environ.get("HERON_BACKUP") or (os.environ.get("HERON_DATA") or "").strip():
        return None
    appdata = appdata_root()
    return os.path.join(appdata, "backup") if appdata else None


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


def take(destination=None, label=None, source=None):
    """Copy the data class. Returns the manifest, or None with a reason printed.

    `source` IS ONE FOLDER TO COPY, laid out flat, and it defaults to
    `sources()` because that is what a backup is. It exists because `restore`
    needs a safety copy of WHAT IT IS ABOUT TO OVERWRITE, which is the data
    folder only when the restore goes there - see restore().
    """
    return _take([(None, source)] if source else sources(), destination, label)


def _take(parts, destination=None, label=None):
    """Copy each (part, folder) there is. Part None is one folder, flat; a
    named part goes under a folder of its name, its `source` recorded."""
    present = [(part, root) for part, root in parts
               if root and os.path.isdir(root)]
    if not present:
        w("There is no %s to back up. Heron has not written anything yet.\n"
          % (" or ".join(root for _part, root in parts if root) or "data folder"))
        return None

    where = destination or backup_root()
    stamp = label or datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    target = os.path.join(where, stamp)
    if os.path.exists(target):
        w("A backup called %s already exists. Nothing was written.\n" % stamp)
        return None

    # EVERY FOLDER IS SURVEYED BEFORE ANYTHING IS COPIED: the copy lands in
    # <home>\backup, so a home surveyed after the first part was copied lists
    # this backup's own half-written files as skipped.
    surveyed = [(part, root, survey(root)) for part, root in present]
    files, skipped = [], []
    for part, root, (kept, left) in surveyed:
        prefix = part + "/" if part else ""
        for relative in kept:
            origin = os.path.join(root, relative.replace("/", os.sep))
            landing = os.path.join(target, (prefix + relative).replace("/", os.sep))
            folder = os.path.dirname(landing)
            if not os.path.isdir(folder):
                os.makedirs(folder)
            shutil.copy2(origin, landing)
            files.append({"path": prefix + relative,
                          "bytes": os.path.getsize(landing),
                          "sha256": digest(landing)})
        skipped.extend({"path": prefix + e["path"], "why": e["why"]} for e in left)

    manifest = {
        "taken": datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "files": files,
        "excluded": skipped,
        "note": ("Backs up the DATA class only. The derived index is excluded "
                 "on purpose and is rebuilt on first use - docs/21 s8. "
                 "A RESTORE FROM THIS COPIES THESE FILES BACK AND REMOVES "
                 "NOTHING: anything in the folder that this backup does not "
                 "hold is left exactly where it is. That is the safe "
                 "direction for an append-only audit log, and it means the "
                 "folder after a restore is this backup MERGED over what was "
                 "there, not a copy of it."),
    }
    if present[0][0] is None:
        manifest["source"] = present[0][1]
    else:
        # EACH PART RECORDS ITS OWN FOLDER, and a restore puts it back there.
        manifest["parts"] = [{"name": part, "source": root} for part, root in present]
        manifest["absent"] = [{"name": part, "source": root} for part, root in parts
                              if root and (part, root) not in present]
        manifest["note"] += (" Each part - a folder of this backup - goes back "
                             "into the folder its `source` names, and only "
                             "that one.")
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
    """Every backup that has a readable manifest, newest first - in the backup
    folder, and in the old one a backup went to before row 5b-1069. A name
    in both is the backup folder's."""
    places = [backup_root()]
    old = old_backup_root()
    if old and not (places[0] and _same(old, places[0])):
        places.append(old)
    found, seen = [], set()
    for where in places:
        if not where or not os.path.isdir(where):
            continue
        for name in sorted(os.listdir(where), reverse=True):
            path = os.path.join(where, name)
            if name in seen or not os.path.isdir(path):
                continue
            manifest = read_manifest(path)
            if manifest:
                seen.add(name)
                found.append((name, path, manifest))
    found.sort(key=lambda row: row[0], reverse=True)
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

    # WHERE EACH PART GOES: the folder it came from, as its manifest records
    # it - or, given `into`, that folder (flat) or a folder of the part's
    # name inside it, laid out as the backup is. The drill passes `into`.
    order = [p.get("name") for p in manifest.get("parts", [])] or [None]
    if manifest.get("parts"):
        folders = dict((p.get("name"), os.path.join(into, p.get("name")) if into
                        else p.get("source")) for p in manifest["parts"])
    else:
        folders = {None: into or manifest.get("source")}
    if not all(folders.values()):
        return [], ["there is nowhere to restore to"]

    plan = []
    for entry in manifest["files"]:
        part, rest = None, entry["path"]
        if manifest.get("parts"):
            part, _slash, rest = entry["path"].partition("/")
        if part not in folders:
            return [], ["%s is in no part this backup records. Nothing was "
                        "changed." % entry["path"]]
        plan.append((entry, part,
                     os.path.join(folders[part], rest.replace("/", os.sep))))

    # A FOLDER IT CANNOT PUT BACK STOPS IT HERE, BEFORE ANYTHING IS WRITTEN:
    # a backup from a profile this PC has not got would otherwise make that
    # profile's folder, or fail with one part restored and the other not.
    if not into:
        for part in order:
            parent = os.path.dirname(os.path.abspath(folders[part]))
            if not os.path.isdir(parent):
                return [], ["refusing to restore: %s came from %s, and %s is "
                            "not on this PC. Nothing was changed."
                            % (part or "this backup", folders[part], parent)]

    if not confirm:
        return [], ["restore needs --confirm. It would overwrite %d file(s) in %s"
                    % (len(manifest["files"]),
                       " and ".join(folders[part] for part in order))]

    safety = None
    at_risk = [(part, folders[part]) for part in order
               if os.path.isdir(folders[part]) and any(
                   os.path.exists(landing) for _e, one, landing in plan
                   if one == part)]
    if at_risk:
        # OF THE FOLDERS AT RISK, NOT OF THE DATA ROOT - each one a part of
        # its own when the backup has parts, so the safety copy restores the
        # same way. Measured 2026-09-22 restoring into a folder that is not
        # the data root: it copied the data root -
        # a folder that was never at risk - destroyed the other one, and
        # printed "The state it replaced was copied to ... first", which was
        # false. Not reachable from the command line, where only `drill`
        # passes `into` and its landing is always fresh; a reassurance that
        # can be false is worth fixing before it is reachable.
        #
        # MICROSECONDS IN THE LABEL. Two restores inside one second gave the
        # second one "A backup called X already exists. Nothing was written."
        # and it carried on regardless.
        try:
            safety = _take(at_risk,
                           label="before-restore-" +
                           datetime.datetime.now().strftime("%Y%m%d-%H%M%S-%f"))
        except (IOError, OSError) as why:
            safety = None
            w("The safety copy could not be written: %s\n" % why)

        # AND A SAFETY COPY IT COULD NOT TAKE STOPS THE RESTORE. This
        # function's own docstring says the thing it destroys is the only
        # copy of what was on the machine a second ago; carrying on without
        # the copy destroys exactly that. Refusing loses nothing - the backup
        # is still there and the restore can be run again.
        if safety is None:
            return [], ["refusing to restore: the safety copy of %s could "
                        "not be taken, and restoring would overwrite it with "
                        "no way back. Nothing was changed."
                        % " and ".join(folder for _part, folder in at_risk)]

    restored = []
    for entry, _part, landing in plan:
        source = os.path.join(path, entry["path"].replace("/", os.sep))
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
    compare content hashes both ways. Nothing the user has is touched. With
    two folders, each part is restored into a scratch folder of its own name,
    so the compare also proves no file landed in the other part's folder.
    """
    import tempfile
    w("DRILL - back up, restore into scratch, compare\n")
    w("=" * 58 + "\n")

    # NOTHING TO DRILL IS NOT A FAILED DRILL. docs/21 s8 asks for a PERIODIC
    # AUTOMATED drill, and on a machine where Heron has written nothing yet
    # this exited 1 - the code a broken restore path uses. That is the third
    # state, and tests/README.md gives it 3.
    folders = [root for _part, root in sources() if root]
    present = [root for root in folders if os.path.isdir(root)]
    if not present:
        w("NOT RUN - there is no %s, so there is nothing to back up and "
          "nothing\n" % (" or ".join(folders) or "data folder"))
        w("to restore. Heron has not written anything yet. The restore path "
          "is not\nclaimed to work and is not claimed to be broken.\n")
        return 3

    scratch = tempfile.mkdtemp(prefix="heron-drill-")
    try:
        made = take(destination=os.path.join(scratch, "copies"), label="drill")
        if not made:
            return 1
        w("  backed up %d file(s), skipped %d\n"
          % (len(made["files"]), len(made["excluded"])))
        for part in made.get("parts", []):
            w("    %d from %s\n" % (sum(1 for f in made["files"]
                                      if f["path"].startswith(part["name"] + "/")),
                                  part["source"]))
        for part in made.get("absent", []):
            w("    none from %s - it is not there yet\n" % part["source"])

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
        w("  restored %d file(s) into a scratch folder%s\n"
          % (len(restored), ", each folder's into its own"
             if made.get("parts") else ""))

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
        w("Nothing under %s was touched.\n" % " or ".join(present))
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
        for part in made.get("parts", []):
            w("  %d from %s\n" % (sum(1 for f in made["files"]
                                    if f["path"].startswith(part["name"] + "/")),
                                part["source"]))
        for part in made.get("absent", []):
            w("  none from %s - it is not there yet\n" % part["source"])
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
        for part in rows[0][2].get("parts", []):
            w("  %d into %s\n" % (sum(1 for p in restored
                                    if p.startswith(part["name"] + "/")),
                                part["source"]))
        w("Nothing was REMOVED: a file in the folder that this backup does "
          "not hold\nis still there. The folder is this backup merged over "
          "what was there.\n")
        w("The derived index was not restored - Heron rebuilds it on first use.\n")
        return 0

    w("Commands: backup [--to <folder>] | list | verify [<name>] | drill | "
      "restore <name> --confirm\n")
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
