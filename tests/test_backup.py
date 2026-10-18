# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-BAK-010, HERON-WSP-RST-011
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Backup and Restore Agents.

    python tests/test_backup.py

docs/21 section 8: *"Restore must be tested, not merely implemented. An
untested restore path is not a restore path - it is a belief."* This file is
the reason the Restore Agent shipped in the same commit as the Backup Agent.

WHAT IT PROVES
  1. The round trip is byte for byte. Content hashes both ways, not sizes.
  2. The derived index is EXCLUDED, and the exclusion is named with a reason -
     restoring a stale index over fresh knowledge is the risk docs/21 s8 names.
  3. A FILE NOBODY ANTICIPATED IS BACKED UP. This is the whole argument for an
     exclude-list: an include-list silently drops the personal fragment library
     somebody puts in the data folder next year.
  4. verify() catches a corrupted backup, by content. A verification that only
     checks a file still exists answers a question nobody was worried about.
  5. restore refuses without --confirm, and refuses a backup that fails verify.
     Restoring from a damaged copy is worse than not restoring.
  6. Restore takes a SAFETY COPY of what it is about to overwrite, because the
     thing it destroys is the only record of the machine a second ago.
  7. Nothing touches the real %APPDATA% or the real home - every case runs in a
     temp folder, and cases 12 on plant the home.
  8. A RESTORE IS A MERGE AND SAYS SO. A file in the data folder that the
     backup never held is LEFT WHERE IT IS - which is the safe direction for
     an append-only audit log, and the opposite of what "Restored 2 file(s)"
     leads a person to believe when they are restoring BECAUSE the folder is
     wrong. Measured 2026-09-22; the behaviour is right and the sentence was
     missing.
  9. THE SAFETY COPY IS TAKEN FROM WHAT IS ABOUT TO BE OVERWRITTEN. Measured
     2026-09-22 restoring into a folder that is not the data root: it copied
     the data root instead - a folder that was never at risk - destroyed the
     other one, and printed "The state it replaced was copied to ... first",
     which was false. Not reachable from the command line, where only `drill`
     passes `into` and its landing is always fresh.
 10. A DRILL WITH NOTHING TO DRILL IS NOT A FAILED DRILL. docs/21 s8 asks for
     a PERIODIC AUTOMATED drill, and on a machine where Heron has written
     nothing yet it exited 1 - the code a broken restore path uses.
 12. WITH NO HERON_DATA, ONE BACKUP HOLDS BOTH FOLDERS (row 5b-1069, D-135):
     %APPDATA%\\Heron, the add-in's settings and trail, and the home that
     `heron_paths.data_home()` names, where the knowledge folder, each
     project's memory and the brain's audit went. Before this it held the
     first alone.
 13. A RESTORE PUTS EACH FILE BACK INTO THE FOLDER IT CAME FROM - the
     `source` its part of the manifest records - and nothing crosses over.
 14. THE DRILL PROVES THE ROUND TRIP FOR BOTH, and changes neither.
 15. THE DEFAULT BACKUP FOLDER IS IN THE HOME, not %APPDATA%\\Heron\\backup,
     which inside a Microsoft Store Claude is the app's private copy that a
     Reset deletes along with what it backs up. A backup already taken in the
     old place is still listed.
 16. A folder that does not exist yet is left out and named, not an error.
 17. A RESTORE REFUSES A FOLDER IT CANNOT PUT BACK - a recorded source whose
     parent this PC has not got - before it writes anything.
 With HERON_DATA set, cases 1 to 11 are the behaviour they always were: that
 folder alone, laid out flat. Case 12 checks %APPDATA% stays out of it.

WHAT IT DOES NOT PROVE. That a real disaster is survivable. The default backup
sits beside what it protects; it survives a deleted audit folder and an
uninstall, and not a lost disk. The tool says so and so does this.
"""

import io
import os
import sys
import json
import shutil
import tempfile
import importlib.util

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def quietly(call, *args, **kwargs):
    """(what `call` returned, what it printed)."""
    quiet, keep_out = io.StringIO(), sys.stdout
    sys.stdout = quiet
    try:
        return call(*args, **kwargs), quiet.getvalue()
    finally:
        sys.stdout = keep_out


def fingerprint(folder):
    """Every file under `folder`, by its content."""
    found = {}
    for base, _d, files in os.walk(folder):
        for name in files:
            full = os.path.join(base, name)
            found[os.path.relpath(full, folder)] = io.open(full, "rb").read()
    return found


def load():
    path = os.path.join(ROOT, "tools", "heron-backup.py")
    spec = importlib.util.spec_from_file_location("heron_backup_tool", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def put(root, relative, text):
    full = os.path.join(root, relative.replace("/", os.sep))
    folder = os.path.dirname(full)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    io.open(full, "w", encoding="utf-8").write(text)
    return full


def main():
    work = tempfile.mkdtemp(prefix="heron-backup-test-")
    kept = {k: os.environ.get(k) for k in ("HERON_DATA", "HERON_BACKUP", "APPDATA")}
    # The home is asked of heron_paths, which answers None off Windows with no
    # HERON_DATA and the owner's real profile on Windows - so cases 12 on plant
    # it, the way tests/test_data_home.py does, and put it back below.
    sys.path.insert(0, os.path.join(ROOT, "brain"))
    import heron_paths as PATHS
    real_home = PATHS.data_home
    try:
        data = os.path.join(work, "data")
        store = os.path.join(work, "copies")
        os.environ["HERON_DATA"] = data
        os.environ["HERON_BACKUP"] = store
        tool = load()

        put(data, "audit/audit-202609.jsonl", u'{"op":"count_elements","ok":true}\n')
        put(data, "config/heron.config", u"write.enabled = false\n")
        put(data, "knowledge/global.db", u"PRETEND SQLITE - derived, rebuildable\n")

        print("1 and 2. The round trip, and what is deliberately left out")
        made = tool.take()
        check(made is not None, "a backup is taken")
        names = sorted(f["path"] for f in made["files"])
        check(names == ["audit/audit-202609.jsonl", "config/heron.config"],
              "the audit trail and the config are copied")
        skipped = [e["path"] for e in made["excluded"]]
        check(skipped == ["knowledge/global.db"],
              "the derived index is NOT copied")
        check(made["excluded"] and "docs/21" in made["excluded"][0]["why"],
              "and the exclusion carries the reason, citing the decision")
        check(not tool.verify(made["at"]), "the copy verifies straight away")

        print()
        print("3. A file nobody anticipated is backed up - the exclude-list argument")
        put(data, "knowledge/my-own-fragments.yaml", u"id: MINE-001\n")
        put(data, "somewhere-new/notes.txt", u"a thing invented next year\n")
        second = tool.take(label="second")
        names = sorted(f["path"] for f in second["files"])
        check("knowledge/my-own-fragments.yaml" in names,
              "a new file in the knowledge folder IS kept - only the .db is skipped")
        check("somewhere-new/notes.txt" in names,
              "and so is a folder that did not exist when this was written")

        print()
        print("4. Verification is by content, not by presence")
        target = os.path.join(second["at"], "config", "heron.config")
        io.open(target, "w", encoding="utf-8").write(u"write.enabled = true\n")
        problems = tool.verify(second["at"])
        check(len(problems) == 1 and "has changed" in problems[0],
              "a file edited inside the backup is caught (%s)"
              % (problems[0] if problems else "NOT caught"))

        os.remove(target)
        problems = tool.verify(second["at"])
        check(any("is missing" in p for p in problems),
              "and so is one deleted from it")

        print()
        print("5. Restore refuses when it should")
        restored, problems = tool.restore(second["at"], confirm=True)
        check(not restored and any("does not verify" in p for p in problems),
              "a damaged backup is refused even with --confirm")

        restored, problems = tool.restore(made["at"], confirm=False)
        check(not restored and any("--confirm" in p for p in problems),
              "and a good one still needs --confirm")

        print()
        print("6. Restore keeps what it is about to destroy")
        put(data, "audit/audit-202609.jsonl", u'{"op":"LATER WORK","ok":true}\n')
        before = set(os.listdir(store))
        restored, problems = tool.restore(made["at"], confirm=True)
        check(not problems and len(restored) == 2,
              "the good backup restores")
        text = io.open(os.path.join(data, "audit", "audit-202609.jsonl"),
                       encoding="utf-8").read()
        check("count_elements" in text, "the old content is back")
        after = set(os.listdir(store)) - before
        safety = [n for n in after if n.startswith("before-restore-")]
        check(len(safety) == 1,
              "and a safety copy of what it overwrote was taken first")
        if safety:
            rescued = io.open(os.path.join(
                store, safety[0], "audit", "audit-202609.jsonl"),
                encoding="utf-8").read()
            check("LATER WORK" in rescued,
                  "holding the work the restore replaced, so it is not lost")

        print()
        print("7. The drill runs, and touches nothing real")
        quiet, keep_out = io.StringIO(), sys.stdout
        sys.stdout = quiet
        try:
            code = tool.drill()
        finally:
            sys.stdout = keep_out
        check(code == 0, "the drill passes")
        check("came back byte for byte" in quiet.getvalue(),
              "and says the comparison it actually made")

        print()
        print("8. A RESTORE IS A MERGE, AND THE TOOL SAYS SO")
        print("   A file the backup never held is left where it is. That is")
        print("   the safe direction for an append-only audit log - and a")
        print("   person restoring BECAUSE the folder is wrong needs telling.")
        stray = put(data, "audit/never-backed-up.jsonl", u"not in any backup\n")
        fresh = tool.take(label="merge-case")
        put(data, "audit/appeared-after.jsonl", u"arrived later still\n")
        quiet, keep_out = io.StringIO(), sys.stdout
        sys.stdout = quiet
        try:
            restored, problems = tool.restore(fresh["at"], confirm=True)
        finally:
            sys.stdout = keep_out
        check(not problems, "the restore runs")
        check(os.path.exists(os.path.join(data, "audit", "appeared-after.jsonl")),
              "a file the backup never held is STILL THERE afterwards")
        check(os.path.exists(stray), "and one it did hold is back")
        note = (tool.read_manifest(fresh["at"]) or {}).get("note", "")
        said = quiet.getvalue()
        check("left" in note.lower() or "not remove" in note.lower()
              or "merge" in note.lower(),
              "the manifest says a restore does not remove what it does not "
              "hold, and it says %r" % note[:110])

        print()
        print("9. THE SAFETY COPY IS OF WHAT IS ABOUT TO BE OVERWRITTEN")
        print("   Not reachable from the command line today - only `drill`")
        print("   passes `into`, and its landing is always fresh. It prints a")
        print("   reassurance either way, so it must not be a false one.")
        elsewhere = os.path.join(work, "elsewhere")
        put(elsewhere, "audit/never-backed-up.jsonl", u"THE FOLDER AT RISK\n")
        # CLEAR THE SAFETY COPY CASE 8 TOOK. The label is a timestamp to the
        # SECOND, so a second restore inside the same second collides with it
        # - which is case 11's subject and must not confuse this one.
        for name in list(os.listdir(store)):
            if name.startswith("before-restore-"):
                shutil.rmtree(os.path.join(store, name), ignore_errors=True)
        before = set(os.listdir(store))
        quiet, keep_out = io.StringIO(), sys.stdout
        sys.stdout = quiet
        try:
            tool.restore(fresh["at"], into=elsewhere, confirm=True)
        finally:
            sys.stdout = keep_out
        added = sorted(set(os.listdir(store)) - before)
        rescued = ""
        for name in added:
            for base, _d, files in os.walk(os.path.join(store, name)):
                for one in files:
                    if one != tool.MANIFEST:
                        rescued += io.open(os.path.join(base, one),
                                           encoding="utf-8").read()
        check(added, "a safety copy is taken, and it took %r" % (added,))
        check("THE FOLDER AT RISK" in rescued,
              "and it holds what was about to be destroyed, not the data root "
              "- it holds %r" % rescued[:80])

        print()
        print("11. A SAFETY COPY IT COULD NOT TAKE STOPS THE RESTORE")
        print("    restore()'s own docstring: 'the thing it destroys is the")
        print("    only copy of what was on the machine a second ago'. If the")
        print("    copy fails, carrying on destroys exactly that.")
        blocked = os.path.join(work, "blocked")
        put(blocked, "audit/never-backed-up.jsonl", u"MUST SURVIVE\n")
        # A backup folder that is a FILE: nothing can be written under it, so
        # the safety copy cannot be taken for any reason the tool can fix.
        os.environ["HERON_BACKUP"] = os.path.join(work, "a-file-not-a-folder")
        io.open(os.environ["HERON_BACKUP"], "w", encoding="utf-8").write(u"x")
        quiet, keep_out = io.StringIO(), sys.stdout
        sys.stdout = quiet
        try:
            try:
                restored, problems = tool.restore(fresh["at"], into=blocked,
                                                  confirm=True)
            except BaseException as why:            # noqa: BLE001
                restored, problems = ["RAISED %s" % type(why).__name__], []
        finally:
            sys.stdout = keep_out
            os.environ["HERON_BACKUP"] = store
        survived = io.open(os.path.join(blocked, "audit",
                                        "never-backed-up.jsonl"),
                           encoding="utf-8").read()
        check(problems and not restored,
              "the restore refuses rather than proceeding, and it answered "
              "%r / %r" % (restored, problems))
        check("MUST SURVIVE" in survived,
              "and what it would have destroyed is untouched - it holds %r"
              % survived.strip())

        print()
        print("10. A DRILL WITH NOTHING TO DRILL IS NOT A FAILED DRILL")
        print("    docs/21 s8 asks for a PERIODIC AUTOMATED drill. On a")
        print("    machine where Heron has written nothing, exit 1 reads as a")
        print("    broken restore path.")
        os.environ["HERON_DATA"] = os.path.join(work, "nothing-here")
        quiet, keep_out = io.StringIO(), sys.stdout
        sys.stdout = quiet
        try:
            code = tool.drill()
        finally:
            sys.stdout = keep_out
            os.environ["HERON_DATA"] = data
        said = quiet.getvalue()
        check(code == 3,
              "nothing to drill answers 3 - could not run - and it answered "
              "%r" % code)
        check("NOT RUN" in said,
              "saying NOT RUN in those words, and it said %r" % said[-90:])

        print()
        print("12. WITH NO HERON_DATA, ONE BACKUP HOLDS BOTH FOLDERS (row 5b-1069)")
        print("    %APPDATA%\\Heron keeps the add-in's settings and trail; D-135")
        print("    moved the knowledge folder, each project's memory and the")
        print("    brain's audit into the home heron_paths.data_home() names.")
        roaming = os.path.join(work, "roaming")
        appdata = os.path.join(roaming, "Heron")
        home = os.path.join(work, "profile", ".heron")
        both = os.path.join(work, "copies-both")
        os.environ["APPDATA"] = roaming
        put(appdata, "config/heron.config", u"write.enabled = false\n")
        put(appdata, "audit/audit-202610.jsonl", u'{"op":"ADD-IN TRAIL"}\n')
        put(appdata, "knowledge/global.db", u"PRETEND SQLITE - the old store\n")
        put(home, "knowledge/global.db", u"PRETEND SQLITE - the store\n")
        put(home, "knowledge/projects/p1.hvac.json", u'{"ashrae": "62.1-2019"}\n')
        put(home, "audit/audit-brain-202610.jsonl", u'{"op":"BRAIN LINE"}\n')

        # WITH HERON_DATA SET IT IS THAT FOLDER ALONE, AS IT ALWAYS WAS - and
        # a planted %APPDATA% stays out of it.
        os.environ["HERON_BACKUP"] = both
        alone = tool.take(label="heron-data-alone")
        names = sorted(f["path"] for f in (alone or {}).get("files", []))
        check(alone is not None and "config/heron.config" in names
              and not any(n.startswith("appdata/") or n == "audit/audit-202610.jsonl"
                          for n in names)
              and "parts" not in alone,
              "with HERON_DATA set, the backup is that folder alone, flat, and "
              "%APPDATA% is not in it")

        os.environ.pop("HERON_DATA", None)
        PATHS.data_home = lambda environ=None, system=None: home
        made2 = tool.take(label="both")
        check(made2 is not None, "a backup is taken with no HERON_DATA")
        made2 = made2 or {}
        parts = [(p.get("name"), p.get("source")) for p in made2.get("parts", [])]
        check(parts == [("appdata", appdata), ("home", home)],
              "the manifest names both folders, each with its source - it "
              "names %r" % (parts,))
        names = sorted(f["path"] for f in made2.get("files", []))
        check("appdata/config/heron.config" in names
              and "appdata/audit/audit-202610.jsonl" in names,
              "the add-in's settings and trail are in it")
        check("home/knowledge/projects/p1.hvac.json" in names
              and "home/audit/audit-brain-202610.jsonl" in names,
              "and so are the project's memory and the brain's audit, from "
              "the home - it holds %r" % (names,))
        skipped = sorted(e["path"] for e in made2.get("excluded", []))
        check(skipped == ["appdata/knowledge/global.db", "home/knowledge/global.db"],
              "each folder's derived index is left out, with its reason - "
              "left out %r" % (skipped,))
        check(made2.get("at") and not tool.verify(made2["at"]),
              "and the copy verifies")

        print()
        print("13. A RESTORE PUTS EACH FILE BACK INTO THE FOLDER IT CAME FROM")
        put(appdata, "config/heron.config", u"write.enabled = true\n")
        put(home, "audit/audit-brain-202610.jsonl", u'{"op":"LATER BRAIN WORK"}\n')
        before = set(os.listdir(both))
        (restored, problems), said = quietly(tool.restore, made2.get("at", work),
                                             confirm=True)
        check(not problems and len(restored) == 4,
              "the backup restores, all 4 files - %r / %r" % (restored, problems))
        config = io.open(os.path.join(appdata, "config", "heron.config"),
                         encoding="utf-8").read()
        brain = io.open(os.path.join(home, "audit", "audit-brain-202610.jsonl"),
                        encoding="utf-8").read()
        check("false" in config, "the add-in's setting is back in %APPDATA%\\Heron")
        check("BRAIN LINE" in brain, "and the brain's audit is back in the home")
        check(not os.path.exists(os.path.join(appdata, "audit", "audit-brain-202610.jsonl"))
              and not os.path.exists(os.path.join(home, "config"))
              and not os.path.exists(os.path.join(appdata, "home"))
              and not os.path.exists(os.path.join(home, "appdata")),
              "and nothing crossed over - no folder got the other's files, "
              "nor a folder named for a part")
        safety = [n for n in set(os.listdir(both)) - before
                  if n.startswith("before-restore-")]
        check(len(safety) == 1, "one safety copy was taken first - %r" % (safety,))
        if safety:
            rescued = fingerprint(os.path.join(both, safety[0]))
            held = b"".join(rescued.values())
            check(b"write.enabled = true" in held and b"LATER BRAIN WORK" in held,
                  "holding what the restore replaced in BOTH folders")
            sources = [p.get("source") for p in
                       (tool.read_manifest(os.path.join(both, safety[0])) or {})
                       .get("parts", [])]
            check(sorted(sources) == sorted([appdata, home]),
                  "and its manifest records each folder, so it restores the "
                  "same way - it records %r" % (sources,))

        print()
        print("14. THE DRILL PROVES THE ROUND TRIP FOR BOTH, AND CHANGES NEITHER")
        was = (fingerprint(appdata), fingerprint(home))
        code, said = quietly(tool.drill)
        check(code == 0, "the drill passes - it answered %r" % code)
        check(appdata in said and home in said,
              "it names both folders it backed up")
        check("came back byte for byte" in said,
              "and says the comparison it made - %r" % said[-160:])
        check((fingerprint(appdata), fingerprint(home)) == was,
              "and neither real folder changed")

        print()
        print("15. THE DEFAULT BACKUP FOLDER IS IN THE HOME, NOT %APPDATA%\\Heron")
        print("    Inside a Store Claude %APPDATA%\\Heron\\backup is the app's")
        print("    private copy, and a Reset deletes it with what it backs up.")
        os.environ.pop("HERON_BACKUP", None)
        check(tool.backup_root() == os.path.join(home, "backup"),
              "backup_root() is <home>\\backup - it is %r" % tool.backup_root())
        older, _said = quietly(tool.take, destination=os.path.join(appdata, "backup"),
                               label="20260901-000000")
        made3, _said = quietly(tool.take, label="20261018-000000")
        check(made3 is not None and os.path.dirname((made3 or {}).get("at", ""))
              == os.path.join(home, "backup"),
              "a backup with no --to lands there")
        own = [e["path"] for e in (made3 or {}).get("excluded", [])
               if e["path"].startswith("home/backup/20261018-000000/")]
        check(not own, "and does not list its own half-written copy as "
              "skipped - it lists %r" % (own[:2],))
        again, _said = quietly(tool.take, label="20261018-000001")
        names = [f["path"] for f in (again or {}).get("files", [])]
        check(again is not None and not any("/backup/" in n for n in names),
              "and the next backup does not copy backups into itself")
        listed = [name for name, _p, _m in tool.backups()]
        check("20261018-000000" in listed and "20260901-000000" in listed,
              "a backup taken in the old place is still listed beside the "
              "new ones - %r" % (listed,))
        check(listed and listed[0] == "20261018-000001",
              "newest first - %r" % (listed[:1],))
        os.environ["HERON_BACKUP"] = both

        print()
        print("16. A FOLDER THAT DOES NOT EXIST YET IS LEFT OUT AND NAMED")
        nowhere = os.path.join(work, "profile-without-a-home", ".heron")
        PATHS.data_home = lambda environ=None, system=None: nowhere
        made4, _said = quietly(tool.take, label="no-home-yet")
        parts = [p.get("name") for p in (made4 or {}).get("parts", [])]
        absent = [p.get("source") for p in (made4 or {}).get("absent", [])]
        check(parts == ["appdata"] and absent == [nowhere],
              "the add-in's folder is backed up and the missing home is named "
              "- parts %r, absent %r" % (parts, absent))
        check(not os.path.exists(nowhere), "and no home was made for it")
        PATHS.data_home = lambda environ=None, system=None: home

        print()
        print("17. A RESTORE REFUSES A FOLDER IT CANNOT PUT BACK, BEFORE WRITING")
        moved = os.path.join(work, "no-such-profile", "someone", ".heron")
        manifest_path = os.path.join(made2.get("at", work), tool.MANIFEST)
        if os.path.isfile(manifest_path):
            recorded = json.loads(io.open(manifest_path, encoding="utf-8").read())
            for part in recorded.get("parts", []):
                if part.get("name") == "home":
                    part["source"] = moved
            io.open(manifest_path, "w", encoding="utf-8").write(
                json.dumps(recorded, indent=2, sort_keys=True))
        put(appdata, "config/heron.config", u"write.enabled = UNTOUCHED\n")
        (restored, problems), _said = quietly(tool.restore, made2.get("at", work),
                                              confirm=True)
        config = io.open(os.path.join(appdata, "config", "heron.config"),
                         encoding="utf-8").read()
        check(problems and not restored,
              "the restore refuses - %r / %r" % (restored, problems[:1]))
        check("UNTOUCHED" in config and not os.path.exists(
                  os.path.join(work, "no-such-profile")),
              "and wrote nothing, in either folder")

    finally:
        PATHS.data_home = real_home
        for k, v in kept.items():
            if v is None:
                os.environ.pop(k, None)
            else:
                os.environ[k] = v
        shutil.rmtree(work, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the round trip is byte for byte, the derived index is left")
    print("out with its reason, an unanticipated file is kept anyway, and a")
    print("restore refuses a damaged copy and saves what it overwrites.")
    print()
    print("It does not prove a real disaster is survivable. The default copy")
    print("sits beside what it protects.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
