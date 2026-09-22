#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The release assets - what goes in one, and the three rules that would be
silent if they were wrong.

    python tests/test_release_assets.py

WHAT THIS PROVES
----------------
    no product is named in the builder or the workflow - R-3
    the configuration is Release, and is NOT a flag somebody can get wrong
    the project folder is derived the SAME WAY in all three places that ask
    a PLANNED product with no source is skipped OUT LOUD, not filtered away
    a product PAST PLANNED with no source FAILS rather than shipping short
    Autodesk's assemblies never leave this machine inside an asset
    checksums.txt is written LAST, so its presence means a finished build
    the manifest travels with the assets, so the installer reads the release
    the workflow calls the tool rather than repeating any of its rules
    the workflow publishes a DRAFT, because nothing is signed yet

WHAT IT CANNOT PROVE
--------------------
That an asset installs. Nothing here runs dotnet, unzips anything into a
Revit folder, or looks at Revit. A green run is a statement about the packing
rule, not about the package.

And it cannot prove the workflow runs. It is YAML for GitHub's runner and
this is not one.
"""

import io
import json
import os
import re
import shutil
import sys
import tempfile
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tools"))

TOOL = os.path.join(ROOT, "tools", "build-release-assets.py")
WORKFLOW = os.path.join(ROOT, ".github", "workflows", "release.yml")
DEPLOY = os.path.join(ROOT, "tools", "deploy-addin.ps1")
ADAPTERS = os.path.join(ROOT, "platform", "Heron.Installer", "WindowsAdapters.cs")
MANIFEST = os.path.join(ROOT, "platform", "heron-products.json")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    return io.open(path, encoding="utf-8").read()


def code_of(text):
    """The Python with its comments and docstring taken out."""
    text = re.sub(r'""".*?"""', "", text, flags=re.S)
    return "\n".join(line for line in text.split("\n")
                     if not line.lstrip().startswith("#"))


def main():
    for path in (TOOL, WORKFLOW, DEPLOY, ADAPTERS):
        if not os.path.exists(path):
            print("FAILED  %s is missing" % path)
            return 1

    # IMPORTED BY PATH, because the file name has hyphens in it and cannot be
    # imported by name. importlib is the only way in, and doing it here rather
    # than at the top keeps the failure readable if the module has a syntax
    # error.
    import importlib.util
    spec = importlib.util.spec_from_file_location("build_release_assets", TOOL)
    BRA = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(BRA)

    tool = read(TOOL)
    tool_code = code_of(tool)
    workflow = read(WORKFLOW)
    deploy = read(DEPLOY)
    adapters = read(ADAPTERS)
    manifest = json.loads(io.open(MANIFEST, encoding="utf-8-sig").read())

    print("NOT ONE PRODUCT IS NAMED - R-3, and the point of the whole design")
    # The heading's own name is the platform's name and is exempt everywhere
    # else in this repository; every other id is a product, and a product
    # named in the builder means adding one is no longer a line in a file.
    heading = None
    for product in manifest["products"]:
        if not product.get("partOf") and not product.get("folder"):
            heading = product["id"]
    for product in manifest["products"]:
        if product["id"] == heading:
            continue
        check(product["id"] not in tool_code,
              "the builder never says %r" % product["id"])
        check(product["id"] not in workflow,
              "the workflow never says %r" % product["id"])

    print()
    print("THE CONFIGURATION IS Release, AND IS NOT A FLAG - trap 1")
    # The installer window drives deploy-addin.ps1 with -Configuration Release
    # and always has. Nine correct Debug builds sat in a folder the window
    # never opens and it cost a full round trip on 2026-09-21. A release built
    # in Debug is a release the installer cannot see, so there is exactly one
    # right answer and it must not be selectable.
    check(getattr(BRA, "CONFIGURATION", None) == "Release",
          "the builder's CONFIGURATION is Release")
    check("--configuration" not in tool and "--config" not in tool,
          "and there is no flag to change it")
    check("-c\", CONFIGURATION" in tool or "-c', CONFIGURATION" in tool,
          "and the build command uses it rather than a literal")
    check("Debug" in tool,
          "and the file says why, naming the trap rather than only the answer")

    print()
    print("THE PROJECT FOLDER IS DERIVED THE SAME WAY IN ALL THREE PLACES")
    # deploy-addin.ps1 does it in PowerShell, BuildsOnDisk in C#, this in
    # Python. Three answers to one question is two of them going stale, so
    # each is checked to still say "drop .dll".
    project_of = getattr(BRA, "project_of", None)
    check(project_of is not None, "the builder has a project_of at all")
    if project_of is None:
        project_of = lambda p: p["assembly"]               # noqa: E731
    check(project_of({"assembly": "Heron.Revit.Addin.dll"}) == "Heron.Revit.Addin",
          "the builder drops .dll")
    check(project_of({"assembly": "Weird.DLL"}) == "Weird",
          "whatever the case of the extension")
    check("$productAssembly -replace '\\.dll$', ''" in deploy,
          "the deploy script drops .dll the same way")
    check('EndsWith(".dll", StringComparison.OrdinalIgnoreCase)' in adapters,
          "and so does the window's BuildsOnDisk")

    print()
    print("A PLANNED PRODUCT WITH NO SOURCE IS SKIPPED OUT LOUD")
    # heron-mep is in the manifest so its shape is known, and revit/Heron.MEP/
    # does not exist. Filtering it away silently would make the asset list
    # read as the whole product list.
    sort_by_project = getattr(BRA, "sort_by_project", None)
    check(sort_by_project is not None, "the builder sorts products by whether "
                                       "they have source")
    if sort_by_project is None:
        sort_by_project = lambda d: (d, [], [])            # noqa: E731

    planned = {"id": "ghost-planned", "assembly": "No.Such.Project.dll",
               "folder": "NoSuch", "state": "PLANNED"}
    real = {"id": "ghost-real", "assembly": "Heron.Revit.Addin.dll",
            "folder": "Heron", "state": "SHIPPED"}
    buildable, skipped, broken = sort_by_project([real, planned])
    check([p["id"] for p in buildable] == ["ghost-real"],
          "a product with source is buildable")
    check([p["id"] for p in skipped] == ["ghost-planned"],
          "a PLANNED product with no source is set aside, not built")
    check(broken == [], "and it is not treated as a fault")
    check("not built yet" in tool,
          "and the run says so on screen, every time")

    print()
    print("A PRODUCT PAST PLANNED WITH NO SOURCE IS A FAILURE")
    # Anything further along has code by definition, so an absent folder means
    # something was renamed or deleted. Publishing without it installs nothing
    # on that product and says nothing - AJ Tools' lesson L3.
    for state in ("SHIPPED", "PROVING", None):
        gone = {"id": "ghost-gone", "assembly": "No.Such.Project.dll",
                "folder": "NoSuch", "state": state}
        buildable, skipped, broken = sort_by_project([gone])
        check([p["id"] for p in broken] == ["ghost-gone"],
              "state %r with no source is a fault, not a skip" % state)
    check("past PLANNED has no source" in tool,
          "and the message says what went wrong rather than only that it did")

    print()
    print("AUTODESK'S ASSEMBLIES NEVER LEAVE THIS MACHINE - docs/07 section 4")
    # The same rule deploy-addin.ps1 keeps when it copies. They arrive from
    # NuGet during a build and must not travel inside a release asset.
    pack = code_of(tool)
    check('startswith("RevitAPI")' in pack,
          "RevitAPI* is excluded from every asset")
    check('startswith("AdWindows")' in pack,
          "and so is AdWindows*")
    check('Name -notlike "RevitAPI*"' in deploy,
          "which is the rule the deploy script already keeps")

    print()
    print("checksums.txt IS WRITTEN LAST, so its presence means a finished build")
    # The same shape as replaced.json in deploy-addin.ps1: a completion mark
    # written after the thing it marks, so a folder without it is one whose
    # build did not finish.
    #
    # BOTH POSITIONS ASSERTED FIRST. str.find gives -1 for a string that is
    # not there, and -1 is less than every real position - the false green
    # this repository has paid for twice (row 5b-79).
    packed = tool_code.find("path = pack(product, release, out_dir)")
    failed_stop = tool_code.find("NO CHECKSUMS WERE WRITTEN")
    wrote = tool_code.find("lines = checksums(out_dir)")
    check(packed > 0, "the assets are packed")
    check(wrote > 0, "the checksums are written")
    check(packed > 0 and wrote > 0 and packed < wrote,
          "and packing comes first, so the mark follows what it marks")
    check(failed_stop > 0 and failed_stop < wrote,
          "a failed build returns BEFORE any checksum is written, so a short "
          "folder can never look publishable")

    print()
    print("THE MANIFEST TRAVELS WITH THE ASSETS - Stage 5 item 1")
    # The installer must read the product list from the release it is
    # installing, never from whatever a branch says today.
    # ANCHORED ON THE COPY, NOT ON THE NAME. The first draft of this check
    # searched for the string "heron-products.json" and found the MANIFEST
    # constant at the top of the file - which sits above everything, so the
    # ordering check went red against a correct builder. A check that names a
    # string rather than the statement it is about points wherever that string
    # happens to appear first.
    copied = tool_code.find("shutil.copyfile(MANIFEST")
    check(copied > 0,
          "the product list is copied into the asset folder")
    check(copied > 0 and failed_stop > 0 and failed_stop < copied,
          "and only after every build succeeded")

    print()
    print("THE OUTPUT FOLDER IS EMPTIED FIRST")
    # A leftover asset from an earlier run would be checksummed and published
    # beside the new ones, and nothing downstream could tell that one of them
    # is from another version. It is the stale-build defect this repository
    # just fixed in the deploy script, one level up.
    check("shutil.rmtree(out_dir)" in tool_code,
          "a previous run's assets are removed rather than merged with")

    print()
    print("THE WORKFLOW CALLS THE TOOL AND REPEATS NONE OF ITS RULES")
    check("python tools/build-release-assets.py" in workflow,
          "it runs the builder")
    for rule, why in [
        ("dotnet build", "it does not build anything itself"),
        ("sha256", "it does not checksum anything itself"),
        ("RevitAPI", "it does not decide what to exclude"),
        ("2020", "it does not carry a list of Revit releases"),
    ]:
        check(rule not in workflow, why)
    check("Release" in workflow,
          "and it still says which configuration, because a reader needs it")

    print()
    print("IT PUBLISHES A DRAFT, because nothing is signed yet")
    # Stage 8 is code signing and is deliberately last. A draft is visible to
    # the owner and to nobody else, so a release can be built and inspected
    # long before it is one somebody could find and install.
    check("--draft" in workflow, "the release is created as a draft")
    check("refs/tags/v" in workflow,
          "and only a tag publishes - a rehearsal builds and stops")
    check("Not signed" in workflow or "not signed" in workflow.lower(),
          "and the notes say it is unsigned rather than leaving it to be "
          "discovered at SmartScreen")

    print()
    print("IT IS NOT THE GATES FILE, and invents no gate")
    # brain/heron_tag.py and brain/heron_qa.py parse gates.yml for the word
    # python followed by a path under tools/ naming a check-something script.
    # A step here that looked like one would invent a release-blocking gate
    # nobody runs.
    invented = re.findall(r"python\s+tools/(check-[a-z-]+)\.py", workflow)
    check(not invented,
          "no step here looks like a gate: %s"
          % (", ".join(invented) if invented else "none does"))

    print()
    print("THE DOWNLOAD CARRIES THE BRAIN, NOT ONLY THE PLUGIN - R-51")
    # The owner's decision, 2026-09-22, answering Q-PE-12/13/14 at once: one
    # download holds the built plugin AND the brain, so the Connect button
    # stops opening a pipe nobody answers.
    #
    # RUN FOR REAL, not read. The zip is built into a temporary folder and
    # opened, because a list of folder names in a constant proves nothing
    # about what ends up inside - and what ends up inside is the whole claim.
    workspace = getattr(BRA, "workspace", None)
    keep = getattr(BRA, "WORKSPACE_KEEP", None)
    check(workspace is not None and keep is not None,
          "the builder can pack a workspace at all")

    if workspace is not None:
        made = tempfile.mkdtemp(prefix="heron-workspace-")
        try:
            path = workspace(made)
            with zipfile.ZipFile(path) as zf:
                names = zf.namelist()

            check(len(names) > 500,
                  "the workspace is not nearly empty: %d entries" % len(names))

            # THE BRAIN, BY NAME. Q-PE-13 was "nothing installs the brain";
            # this is the line that makes it false.
            for needed in (".mcp.json",
                           "platform/heron-products.json",
                           "mcp/server/heron_mcp_server.py"):
                check(needed in names, "it carries %s" % needed)
            for folder in ("brain/fragments/", "brain/skills/", "docs/"):
                check(any(n.startswith(folder) for n in names),
                      "it carries %s" % folder)

            # AND WHAT IT MUST NOT CARRY. tests/ is 171 MB and revit/ is 315 MB
            # of source; .git is every file ever deleted; __pycache__ differs
            # between machines. None of it does anything on a modeller's PC.
            for never in ("tests/", "revit/", "tools/", ".git/"):
                offenders = [n for n in names if n.startswith(never)]
                check(not offenders,
                      "and nothing from %s%s" % (never, "" if not offenders
                                                 else " - found %d" % len(offenders)))
            junk = [n for n in names if "__pycache__" in n or n.endswith(".pyc")]
            check(not junk, "and no __pycache__ or .pyc%s"
                  % ("" if not junk else " - found %d" % len(junk)))
        finally:
            shutil.rmtree(made, ignore_errors=True)

    # A FOLDER THAT MOVED MUST STOP THE RELEASE, not quietly ship a workspace
    # with no brain in it. The first anybody would know is a Connect button
    # that still answers nothing.
    was = BRA.WORKSPACE_KEEP
    try:
        BRA.WORKSPACE_KEEP = was + ["brain-renamed-yesterday"]
        made = tempfile.mkdtemp(prefix="heron-workspace-")
        try:
            refused = False
            try:
                workspace(made)
            except IOError as e:
                refused = "R-51" in str(e) and "brain-renamed-yesterday" in str(e)
            check(refused,
                  "a folder named in WORKSPACE_KEEP that is not there stops the "
                  "release, and the refusal names it")
        finally:
            shutil.rmtree(made, ignore_errors=True)
    finally:
        BRA.WORKSPACE_KEEP = was

    check("heron-project.zip" in tool,
          "and the workspace has a name of its own beside the product zips")
    check("if not failed" in tool,
          "it is packed only when every product built - a workspace beside a "
          "half-built release is a download that installs nothing")

    print()
    if FAILURES:
        print("FAILED (%d)" % len(FAILURES))
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("A release carries every deployable product for every Revit release")
    print("Heron supports, built in Release, with the product list beside them")
    print("and a checksum over all of it written last.")
    print()
    print("NOTHING WAS BUILT AND NOTHING WAS PUBLISHED HERE. This is a check on")
    print("the packing rule, not on the package - whether an asset installs is")
    print("docs/NEEDS-CHECKING.md's question and needs a real machine.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
