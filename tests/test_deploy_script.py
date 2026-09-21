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
    text = raw.decode("utf-8", "replace")
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
    check(text.index("Save-PreviousInstall\n\n# REPLACE") <
          text.index("Remove-Item $addinDir -Recurse -Force -ErrorAction"),
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
    check(text.index('. (Join-Path $PSScriptRoot "HeronRevit.ps1")')
          < text.index("$target   = Get-RevitAddinsFolder"),
          "and the module is dot-sourced BEFORE the function is called")

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
