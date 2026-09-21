#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The deploy script, after it was taught to deploy more than one product.

WHY THIS FILE EXISTS
--------------------
tools/deploy-addin.ps1 deployed exactly one thing until 2026-09-21. Every
path in it said Heron.Revit.Addin. Stage 3 needs it to deploy any product,
because R-31 refuses a second copy of the copy rule - so it was generalised
rather than duplicated.

That script is PROVEN, rollback included (2026-09-19), and it carries guards
that were each found by something going wrong once: the runtime check that
refuses a 2027 build for Revit 2024, the deps.json that .NET 8 fails without,
the verify-what-was-written step. IT WAS CHANGED ON A MACHINE WITH NO
POWERSHELL, so not one line of it could be run.

This is what can honestly be checked instead:

    the -Product parameter exists and defaults to the add-in that already ships
    THAT DEFAULT RESOLVES TO EXACTLY THE FOUR VALUES THAT WERE HARDCODED
    no product identity is hardcoded in the logic any more
    every guard that was there before is still there
    it is still ASCII, which Windows PowerShell 5.1 needs

THE SECOND ONE IS THE POINT. If `-Product heron-bridge` resolves to Heron,
Heron.addin, Heron.Revit.Addin.dll and revit\\Heron.Revit.Addin, then a run
with no -Product does today what it did yesterday. That is the claim this
change rests on, and it is checked here against the manifest rather than
trusted.

WHAT IT CANNOT PROVE
--------------------
That the script runs. It is PowerShell for Windows and this is Linux. A green
run here is a text check, not a deploy - the real thing is owed on the owner's
machine, alongside Stage 2.

    python tests/test_deploy_script.py
"""

import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPT = os.path.join(ROOT, "tools", "deploy-addin.ps1")
MANIFEST = os.path.join(ROOT, "platform", "heron-products.json")

# What every path in the script said before 2026-09-21. The default must
# still resolve to these, or an install that has worked since Step 1 moves
# underneath somebody without being asked.
BRIDGE = "heron-bridge"
WAS_FOLDER = "Heron"
WAS_ADDIN = "Heron.addin"
WAS_ASSEMBLY = "Heron.Revit.Addin.dll"
WAS_PROJECT = "Heron.Revit.Addin"

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    if not os.path.exists(SCRIPT):
        print("FAILED  %s is missing" % SCRIPT)
        return 1

    raw = io.open(SCRIPT, "rb").read()

    # READ AS BYTES, THEN NORMALISED - and the two halves are for different
    # questions, which is why they are kept apart.
    #
    # `raw` is the file as Windows PowerShell 5.1 will actually read it, and the
    # ASCII check below is a statement about exactly those bytes. It must never
    # be run against a cleaned-up copy.
    #
    # `text` is for asking what the script SAYS, and that question has nothing
    # to do with line endings. It is normalised because of what happens on the
    # one machine that can run this script for real:
    #
    #   Ajmal's PC has core.autocrlf=true in the system gitconfig and this
    #   repository tracks no .gitattributes, so git stores LF and writes CRLF.
    #   Measured 2026-09-21: deploy-addin.ps1 is 578 CRLF and 0 bare LF on disk,
    #   while its blob has no CR at all.
    #
    # Any assertion below that spans a line break - and there is one, the
    # backup-before-delete order check - then searches for "\n\n" in a file that
    # holds "\r\n\r\n" and can NEVER match. THE TEST FAILED ON WINDOWS AND
    # PASSED IN CI, which is the worst shape a check can have: CI is Linux, so
    # it never saw it, and the failure surfaced only on the machine that had
    # just proved the script works. Found while running Group AA (NEEDS-CHECKING).
    #
    # Normalising here rather than loosening the assertion: the assertion is
    # precise on purpose - see the comment at the order check - and precision is
    # the thing worth keeping.
    text = raw.replace(b"\r\n", b"\n").decode("utf-8", "replace")

    manifest = json.loads(io.open(MANIFEST, encoding="utf-8-sig").read())
    products = dict((p["id"], p) for p in manifest["products"])

    print("Windows PowerShell 5.1 can read it")
    try:
        raw.decode("ascii")
        check(True, "the file is ASCII, so 5.1 will not misread it as ANSI")
    except UnicodeDecodeError as e:
        check(False, "the file is ASCII (%s)" % e)

    print()
    print("It takes a product, and the default is the one that already ships")
    check(re.search(r"\[string\]\s*\$Product\s*=\s*\"heron-bridge\"", text) is not None,
          "-Product defaults to heron-bridge")
    check("$RevitVersion = \"2024\"" in text, "and -RevitVersion still defaults to 2024")

    print()
    print("THE DEFAULT RESOLVES TO WHAT WAS HARDCODED - the claim this rests on")
    bridge = products.get(BRIDGE)
    check(bridge is not None, "heron-bridge is in the product list")
    if bridge:
        check(bridge["folder"] == WAS_FOLDER,
              "folder is %r, which is where it has always been deployed" % WAS_FOLDER)
        check(bridge["addin"] == WAS_ADDIN, "addin is %r" % WAS_ADDIN)
        check(bridge["assembly"] == WAS_ASSEMBLY, "assembly is %r" % WAS_ASSEMBLY)
        # The script derives the project folder by dropping .dll.
        check(re.sub(r"\.dll$", "", bridge["assembly"]) == WAS_PROJECT,
              "and the project folder derives to %r" % WAS_PROJECT)

    print()
    print("Nothing about a product is written into the logic any more")
    # The three remaining mentions are in the comment that explains the
    # change. A mention in code would mean a product had been left behind.
    code = "\n".join(line for line in text.split("\n")
                     if not line.lstrip().startswith("#"))
    check("Heron.Revit.Addin" not in code,
          "no code line names Heron.Revit.Addin")
    check('Join-Path $target "Heron"' not in code,
          "the deploy folder is not hardcoded")
    check('"Heron.addin"' not in code,
          "the manifest file name is not hardcoded")
    check("$productFolder" in code and "$productAssembly" in code
          and "$productAddin" in code,
          "all three come from the product list instead")

    print()
    print("A product that cannot be deployed is refused, not half-deployed")
    check("There is no Heron product called" in text,
          "an unknown -Product is refused, and the message lists the real ones")
    check("is a tab built by its pieces" in text,
          "a HEADING is refused - it installs nothing of its own (D-93)")
    check("The Heron product list is not at" in text,
          "a missing product list is refused rather than guessed around")

    print()
    print("Every guard that was there before is still there")
    for guard, why in [
        ("Get-RevitBlockReason",
         "it still refuses while Revit is open - a loaded assembly cannot be replaced"),
        ("Save-PreviousInstall",
         "it still backs up what it is about to replace, so -Rollback has somewhere to go"),
        ("Get-AssemblyTargetFramework",
         "it still reads what the build was made for, rather than trusting the folder"),
        ("Get-ExpectedTargetFramework",
         "it still knows which runtime each release needs, and refuses a release it does not"),
        ("deps.json",
         "it still copies the runtime metadata .NET 8 and .NET 10 fail without"),
        ("RevitAPI*",
         "it still refuses to redistribute Autodesk's assemblies (docs/07 section 4)"),
    ]:
        check(guard in text, why)

    print()
    print("It still verifies what was WRITTEN, not what was intended")
    check("$deployedDll  = Join-Path $addinDir $productAssembly" in text,
          "the deployed assembly is checked by the product's real name")
    check("is not there. Nothing was installed." in text,
          "and a deploy that finished without the file says so")

    print()
    print("It REPLACES the previous install rather than copying over it")
    # R-38/R-38b/R-38c. Copying over leaves every file the old version
    # shipped and the new one dropped, and Revit loads the folder rather
    # than the build. Stage 3 asked for this in as many words.
    check("Remove-Item $addinDir -Recurse -Force -ErrorAction SilentlyContinue" in text,
          "the previous install is deleted before the copy (R-38)")
    check("Refusing to delete" in text,
          "and only ever this product's own folder under this release")
    check("NOTHING FURTHER WAS CHANGED" in text,
          "a delete that did not finish STOPS rather than copying (R-38b)")
    # Checked against the CODE, not the file: the comment above the delete
    # says ".old" on purpose, to record what this deliberately does not do.
    check(".old" not in code and "Rename-Item" not in code,
          "nothing is renamed aside - no .old folder is ever made (R-38c)")
    # The backup is taken FIRST, so the delete always has a way back.
    #
    # THE LITERAL SPANS TWO LINE BREAKS ON PURPOSE, so do not shorten it.
    # "Save-PreviousInstall" appears three times - the function, the call in the
    # -Remove branch, and the call before the replace - and `index` finds the
    # FIRST. That is the function definition, which sits above the delete
    # whatever the order of the calls, so a shorter anchor passes without
    # checking anything. Anchoring to the blank line and the "# REPLACE" comment
    # names the one call site this is about.
    #
    # It only works because `text` had its line endings normalised at the read.
    # If this ever fails on Windows and passes in CI, that normalisation is what
    # went missing - not the script's ordering.
    #
    # `find`, NOT `index`. An anchor that has moved makes `index` raise
    # ValueError, which kills the run on the spot: every check below this line
    # goes unreported, and the traceback names a substring rather than the
    # requirement. That is exactly how this file behaved on Windows before
    # 2026-09-21 - it did not say "the backup might be taken after the delete",
    # it crashed. Reporting which half went missing is the difference between a
    # test and an accident.
    backup_first = text.find("Save-PreviousInstall\n\n# REPLACE")
    delete_after = text.find("Remove-Item $addinDir -Recurse -Force -ErrorAction")
    check(backup_first != -1,
          "the backup call still sits immediately above the REPLACE block")
    check(delete_after != -1,
          "the delete of the previous install is still where it was")
    check(backup_first != -1 and delete_after != -1
          and backup_first < delete_after,
          "and the backup is taken before the delete, not after")

    print()
    print("A manifest that does not say what the script expects is refused")
    check("$manifestXml.Contains($needle)" in text,
          "the rewrite refuses when it would match nothing, rather than "
          "writing a manifest pointing one folder up")

    print()
    print("The Addins path has ONE owner, and it is not this file")
    module = io.open(os.path.join(ROOT, "tools", "HeronRevit.ps1"),
                     encoding="utf-8").read()
    check("function Get-RevitAddinsFolder" in module,
          "HeronRevit.ps1 owns it")
    check("$target   = Get-RevitAddinsFolder -RevitVersion $RevitVersion" in text,
          "and this script asks rather than spelling it again")
    check('Join-Path $env:APPDATA "Autodesk' not in code,
          "so the path appears nowhere in this script's own logic")
    # A function used before it is dot-sourced is a runtime failure on the
    # first line that matters, and nothing here can run PowerShell to find out.
    # `find` for the same reason as the backup-before-delete check above: a
    # moved anchor must report itself, not raise and take the rest of the run
    # down with it.
    dot_sourced = text.find('. (Join-Path $PSScriptRoot "HeronRevit.ps1")')
    first_use = text.find("$target   = Get-RevitAddinsFolder")
    check(dot_sourced != -1, "HeronRevit.ps1 is dot-sourced at all")
    check(dot_sourced != -1 and first_use != -1 and dot_sourced < first_use,
          "and the module is dot-sourced BEFORE the function is called")

    print()
    print("Every Directory.Build file it names EXISTS - row 5b-99")
    # DERIVED, NOT TYPED. This script names Directory.Build.props five times
    # and named Directory.Build.targets once, and that file does not exist -
    # while Directory.Build.props says in capitals "IT MUST BE SET HERE, IN
    # .props, AND NOT IN A .targets FILE" and records the measurement that
    # proved .targets is too late: OutDir and ProjectDepsFilePath still
    # pointing at the flat folder, and Revit 2027 refusing to load.
    #
    # So the one wrong pointer sent a reader to the exact dead end #242 wrote
    # down so nobody would walk it again. Asked of the repository rather than
    # pinned to a name, so it also catches the next one.
    named = sorted(set(re.findall(r"Directory\.Build\.[A-Za-z]+", text)))
    check(named, "it names at least one Directory.Build file (%s)"
          % ", ".join(named) if named else "it names one at all")
    for which in named:
        check(os.path.exists(os.path.join(ROOT, which)),
              "%s exists in the repository" % which)

    print()
    print("The one flag that makes a downloaded Heron installable - D-96")
    # THIS IS NOT THIS SCRIPT, IT IS ITS CALLER, and it is here because
    # nothing anywhere held it and the ruling that made it load-bearing is
    # one day old.
    #
    # AA9, on a real machine, 2026-09-21: a Heron downloaded as a zip carries
    # ZoneId=3 on all 2130 files, and PowerShell at the Windows default of
    # RemoteSigned refuses an unsigned downloaded script BEFORE its first
    # line runs. So Unblock-File, which lives inside this script, cannot
    # clear the mark that stops this script running. Signing was considered
    # and refused. D-96: the installer is the only supported route for a
    # downloaded copy - which makes `-ExecutionPolicy Bypass` in the
    # installer the thing that makes that route work at all.
    #
    # Remove those two lines and every downloaded install breaks, on every
    # machine left at the Windows default, and no test said so. Row 5b-96.
    adapters_path = os.path.join(ROOT, "platform", "Heron.Installer",
                                 "WindowsAdapters.cs")
    adapters = io.open(adapters_path, encoding="utf-8").read()
    policy = adapters.find('ArgumentList.Add("-ExecutionPolicy")')
    bypass = adapters.find('ArgumentList.Add("Bypass")')
    check(policy != -1, "the installer passes -ExecutionPolicy to PowerShell")
    check(bypass != -1, "and the value it passes is Bypass")
    # BOTH POSITIONS FIRST. str.find gives -1 for a string that is not there,
    # and -1 < every real position - the false green this suite already paid
    # for once in row 5b-79.
    check(policy != -1 and bypass != -1 and bypass > policy,
          "in that order, so the value follows the flag it belongs to")
    check("-NoProfile" in adapters,
          "and -NoProfile is still there, so a user's own profile cannot "
          "change what Heron's scripts see")
    check("D-96" in adapters,
          "and the code says WHY it cannot be removed, not only that it is "
          "safe - the comment argued safety and said nothing about the "
          "install breaking without it")

    print()
    print("The download mark is cleared, which it was not before 2026-09-21")
    # R-37. It lived only in the Stage 2 proof script until that script was
    # deleted for being a second copy of the deploy rule. Deleting a file that
    # carried a requirement is how a requirement goes missing, so it is
    # checked here rather than remembered.
    check("Unblock-File -Path $_.FullName" in text,
          "every deployed file has its zone marker cleared (R-37)")
    check("Unblock-File -Path $manifest" in text,
          "and so does the manifest, which is the first file Revit reads")

    print()
    print("One backup per product, which is a path CHANGE and is recorded")
    check("$backupDir      = Join-Path $env:LOCALAPPDATA "
          "\"Heron\\install-backup\\$RevitVersion\\$productFolder\"" in text,
          "the backup folder now carries the product")
    check("CHANGE OF PATH" in text,
          "and the script says so, so a rollback that cannot find an old "
          "backup is explained rather than surprising")

    # AND THE NOTE BESIDE IT WAS WRONG FOR THE PRODUCT THAT MATTERS MOST.
    # It said "No old backup is deleted or moved" until AA9 ran on a real
    # machine: heron-bridge's productFolder is Heron, so its NEW backupDir
    # is install-backup\<version>\Heron - which is exactly where the OLD
    # layout put the backed-up folder. The first new deploy writes over it.
    # Nothing -Rollback could have used is lost, but the sentence was not
    # true. Row 5b-96, and this pins the correction to the path above.
    # THE OLD SENTENCE IS KEPT, AS A QUOTE. Deleting it would lose the half
    # worth keeping - that it was true when it was written - so what this
    # asks is that it appears ONCE and that the once is the correction
    # recording it, not a claim still standing. The first draft of this
    # check asked for the words to be absent and went red against the right
    # answer.
    #
    # ASKED OF THE PROSE, NOT OF THE LINES. A comment wraps, so the claim
    # sits across two lines with `# ` between them and a plain `in text`
    # cannot see it - the first draft of this check reported the old file
    # as having ZERO of them, which would have read as "already fixed".
    # The comment bodies are joined into one run of words first.
    prose = re.sub(r"\s+", " ", " ".join(
        line.lstrip().lstrip("#").strip() for line in text.split("\n")
        if line.lstrip().startswith("#")))
    claim = "No old backup is deleted or moved"
    check(prose.count(claim) == 1,
          "the old claim appears once in the comments, not twice (%d)"
          % prose.count(claim))
    check(('This said "%s" until' % claim) in prose,
          "and that once is the paragraph correcting it, not a claim that "
          "is still standing")
    check("THE NEW PATH LANDS ON THE OLD BACKUP" in text.upper(),
          "and it names the one product that it does happen to")
    check("$productFolder" in text and "install-backup" in text,
          "while the path itself is still built from the product, which is "
          "what makes the collision possible and is still right")

    print()
    print("The backup is built aside, and a half-written one is refused")

    # THE ORDER IS THE RULE. Until 2026-09-21 the previous backup was deleted
    # and the new one copied into the empty folder, so a copy that died
    # halfway left PART of an install where a whole one had been - and
    # -Rollback restored it, because it only ever looked for the folder.
    # Row 5b-79. Both halves are pinned here because neither can be run.
    staged = text.find("$staging = \"$backupDir.incomplete\"")
    copied = text.find("Copy-Item $addinDir -Destination $stagedAddinDir")
    wrote = text.find("Set-Content -Path $stagedRecord")
    swapped = text.find("Move-Item $staging -Destination $backupDir")

    check(staged > 0 and copied > staged,
          "the copy goes to a staging folder, not straight over the backup")
    check(wrote > copied > 0,
          "replaced.json is written after the copy, so it marks a finished one")
    check(swapped > wrote > 0,
          "and only then does the old backup go - the one way back is never "
          "spent before the second one exists")
    check("Remove-Item $backupDir -Recurse -Force" not in
          text[:copied if copied > 0 else len(text)],
          "nothing deletes the backup folder before the copy has run")
    check("not called atomic" in text.lower(),
          "and it does not claim to be atomic, because a rename is not a "
          "transaction")

    check("it holds no replaced.json" in text,
          "-Rollback refuses a backup with no completion mark rather than "
          "putting back half an install")
    check("something has changed it since" in text,
          "and refuses one whose file count no longer matches its own record")
    # BOTH ENDS HAVE TO BE FOUND. str.find gives -1 for a string that is not
    # there, and -1 is less than every real position - so an ordering check
    # written with two bare find() calls passes LOUDEST when the guard it is
    # checking has been deleted. This suite's own first draft did exactly
    # that on 2026-09-21: the guard was commented out and the check stayed
    # green. Row 5b-79 records it, and it is why every position below is
    # asserted to exist first.
    refusal = text.find("if (-not (Test-Path $backupRecord))")
    restore = text.find("Copy-Item $backupAddinDir -Destination $addinDir")
    check(refusal > 0 and restore > 0 and refusal < restore,
          "both refusals come BEFORE the live install is touched, so a "
          "refused rollback changes nothing")

    print()
    if FAILURES:
        print("FAILED (%d)" % len(FAILURES))
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("The deploy script takes a product, its default still resolves to the")
    print("add-in it has always deployed, and every guard it carried is intact.")
    print()
    print("IT HAS NOT BEEN RUN. This is a text check on a PowerShell script from")
    print("a Linux machine. Whether it still deploys is owed on Windows, and")
    print("until then the rollback proof of 2026-09-19 is STALE for this file.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
