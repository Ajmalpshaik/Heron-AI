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
  7. Nothing touches the real %APPDATA% - every case runs in a temp folder.

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
    kept = {k: os.environ.get(k) for k in ("HERON_DATA", "HERON_BACKUP")}
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

    finally:
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
