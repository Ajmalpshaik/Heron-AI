#!/usr/bin/env python3
# Heron-Agent:  HERON-INS-PKG-012
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Build every deployable product for every Revit release, and lay the results
out as the assets a GitHub release carries.

    python tools/build-release-assets.py --out dist
    python tools/build-release-assets.py --out dist --releases 2024,2027
    python tools/build-release-assets.py --out dist --list

WHY THIS EXISTS
---------------
Stage 5 of the installer plan says the installer reads the manifest FROM A
NAMED RELEASE and downloads each ticked product's asset. On 2026-09-21 this
repository had **no releases at all** and nothing that could make one, so
Stage 5 had nothing to talk to. This is the missing half: the thing that
produces what Stage 5 consumes.

NOTHING HERE NAMES A PRODUCT - R-3, D-93. The product list is read from
platform/heron-products.json exactly as the installer and the deploy script
read it. Adding Heron Structure next year is a line in that file and never a
line in here.

CONFIGURATION IS Release, AND THAT IS THE WHOLE POINT OF SAYING SO. The
installer window drives tools/deploy-addin.ps1 with -Configuration Release and
always has. A release built in Debug is a release the installer cannot see -
the trap that cost a full round trip on 2026-09-21, recorded in
docs/NEEDS-CHECKING.md. It is not a parameter here, because there is exactly
one right answer and a flag would let somebody choose the wrong one.

WHAT AN ASSET IS
----------------
One zip per product per Revit release, named from the manifest:

    <product id>-<release>.zip        e.g. heron-bridge-2024.zip

and its contents are the BUILD OUTPUT FOLDER for that pair - the assemblies,
the .addin manifest's assembly, the deps.json where the runtime needs one.
That is exactly what tools/deploy-addin.ps1 copies from, so a downloaded and
extracted asset can be handed to the same proven copy rule rather than to a
second one (R-31).

Beside them:

    heron-project.zip      what a person downloads and keeps - the brain,
                           skills, MCP and docs (R-51), and at its root the
                           two installers, HeronInstaller.exe and
                           heron-install.exe, with the two scripts they drive
    heron-products.json    the manifest, so the installer reads the product
                           list from the release rather than from a branch
    checksums.txt          SHA-256 of every file above - R-12, verified
                           before use, and a file that fails NEVER reaches
                           the Addins folder

EVERY DEPLOYABLE PRODUCT IS BUILT, whatever its state. A PROVING product is
not offered by the installer - InstallerScreen refuses anything but SHIPPED -
but which products are offerable is the manifest's business and not this
file's. Shipping the asset costs a zip and means the day a product becomes
SHIPPED, the release already carries it.

WHAT THIS CANNOT DO
-------------------
Publish. It writes a folder; a workflow uploads it. Keeping the two apart is
what lets this run on a laptop with no GitHub token at all.

And it cannot tell you the build is CORRECT. It tells you the compiler was
happy. Whether the add-in loads into Revit is docs/NEEDS-CHECKING.md's job.

Exit 0 = every asset built. Exit 1 = a build failed, and it says which.
Exit 3 = could not run: no dotnet on this machine. NOT a pass.
"""

import argparse
import hashlib
import io
import json
import os
import shutil
import re
import subprocess
import sys
import tempfile
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_dotnet as NET                                     # noqa: E402
# NOT heron_fragment: .github/workflows/release.yml installs no PyYAML before
# it runs this, and heron_relpath imports nothing but `os`.
import heron_relpath as RELPATH                                # noqa: E402

MANIFEST = os.path.join(ROOT, "platform", "heron-products.json")

# ONE RIGHT ANSWER, SO NOT A FLAG. See the module docstring.
CONFIGURATION = "Release"

NO_DOTNET = 3


def products():
    """
    Every product that has files to deploy, read from the manifest.

    A HEADING HAS NO FILES OF ITS OWN and is skipped - D-93. It is a tab built
    by its pieces, and the pieces are in this same list.
    """
    manifest = json.loads(io.open(MANIFEST, encoding="utf-8-sig").read())
    found = []
    for product in manifest["products"]:
        if not product.get("folder") or not product.get("assembly"):
            continue
        found.append(product)
    return found


def has_project(product):
    """Whether there is source to build for that product."""
    return os.path.isdir(os.path.join(ROOT, "revit", project_of(product)))


def sort_by_project(deployable):
    """
    Split the products into the ones with code and the ones without.

    A PLANNED PRODUCT IS IN THE MANIFEST SO ITS SHAPE IS KNOWN, not because it
    exists - Stage 1, and the installer already refuses to offer one. So it
    carries a folder and an assembly name while `revit/<project>/` is not
    there at all. heron-mep is exactly that today.

    Skipping it is right. Skipping it QUIETLY is not, which is why it is
    counted and printed rather than filtered away.

    AND A MISSING PROJECT IS ONLY FORGIVEN FOR A PLANNED PRODUCT. Anything
    further along has code by definition, so an absent folder means something
    was deleted or renamed and the release would ship without it - the silent
    half-delivery AJ Tools' lesson L3 is about. That is a failure here.
    """
    buildable, planned, broken = [], [], []
    for product in deployable:
        if has_project(product):
            buildable.append(product)
        elif product.get("state") == "PLANNED":
            planned.append(product)
        else:
            broken.append(product)
    return buildable, planned, broken


def project_of(product):
    """
    The project folder, derived by dropping .dll from the assembly name.

    THE SAME DERIVATION tools/deploy-addin.ps1 and BuildsOnDisk use. Three
    places asking the same question three ways is two of them going stale, so
    this one is spelled to match, and tests/test_release_assets.py holds them
    together.
    """
    assembly = product["assembly"]
    return assembly[:-4] if assembly.lower().endswith(".dll") else assembly


def build_output(product, release):
    """Where Directory.Build.props puts that pair's build."""
    return os.path.join(ROOT, "revit", project_of(product), "bin", "x64",
                        CONFIGURATION, release)


def asset_name(product, release):
    """<id>-<release>.zip, derived from the manifest and never typed."""
    return "%s-%s.zip" % (product["id"], release)


def build(product, release):
    """
    Build one product for one release. Returns None, or why it failed.

    -p:EnableWindowsTargeting=true because 2025 to 2027 are windows-suffixed
    runtimes and this may be running on Linux - docs/30. It is a no-op on
    Windows, where the SDK already has the targeting packs.
    """
    project = project_of(product)
    csproj = os.path.join("revit", project, project + ".csproj")

    if not os.path.exists(os.path.join(ROOT, csproj)):
        return "%s does not exist, so '%s' cannot be built" % (csproj, product["id"])

    proc = subprocess.Popen(
        ["dotnet", "build", csproj, "-c", CONFIGURATION,
         "-p:RevitVersion=" + release,
         "-p:EnableWindowsTargeting=true",
         "--nologo", "-v", "quiet"],
        cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    out = proc.communicate()[0].decode("utf-8", "replace")

    if proc.returncode != 0:
        return out.strip() or "dotnet build exited %d and said nothing" % proc.returncode

    folder = build_output(product, release)
    if not os.path.isdir(folder):
        return ("the build reported success but %s is not there, so there is "
                "nothing to package" % os.path.relpath(folder, ROOT))

    main = os.path.join(folder, product["assembly"])
    if not os.path.exists(main):
        return ("the build reported success but %s is not in %s"
                % (product["assembly"], os.path.relpath(folder, ROOT)))

    return None


def pack(product, release, out_dir):
    """
    Zip one build output folder. Returns the asset's path.

    AUTODESK'S ASSEMBLIES ARE NEVER REDISTRIBUTED - docs/07 section 4, and the
    same rule tools/deploy-addin.ps1 keeps when it copies. RevitAPI*.dll and
    AdWindows* arrive from the NuGet packages during a build and must not
    leave this machine inside a release asset.
    """
    folder = build_output(product, release)
    path = os.path.join(out_dir, asset_name(product, release))

    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as zf:
        for base, _, files in os.walk(folder):
            for name in sorted(files):
                if name.startswith("RevitAPI") or name.startswith("AdWindows"):
                    continue
                full = os.path.join(base, name)
                zf.write(full, os.path.relpath(full, folder))

    return path


# WHAT A USER'S FOLDER HOLDS, AND WHAT IT DELIBERATELY DOES NOT - R-51.
#
# The owner's decision on 2026-09-22: one download carries the built plugin AND
# the brain, so the Connect button stops opening a pipe nobody answers
# (Q-PE-13). What the user keeps is the folder Claude Code is opened in
# afterwards (Q-PE-14, R-52).
#
# IT IS NOT A COPY OF THE REPOSITORY. tests/ is 171 MB and revit/ is 315 MB of
# source and build output; neither does anything on a modeller's PC. tools/ is
# how Heron is DEVELOPED, and .claude/ is house rules for working ON Heron
# rather than anything Heron does for a modeller - brain/skills/ is that, and
# it is included.
#
# EXCEPT THREE FILES OF tools/, NAMED ONE BY ONE - row 5b-371. The installers
# drive deploy-addin.ps1 and HeronRevit.ps1 from the folder they sit in, and
# docs/WHAT-TO-INSTALL.md tells a new person to run check-dependencies.py
# against the requirements files beside it. Without them the download held an
# installer step and a check step that could not run on the PC they were
# written for. A file named here, never the folder: the rest of tools/ is
# still how Heron is developed.
#
# The first cut errs WIDE on purpose: docs/ is 5.8 MB and the AI reads it.
# Shipping more than needed is recoverable; shipping less is a user whose
# question has no answer on their disk.
WORKSPACE_KEEP = [
    "brain",                        # fragments, skills, agents - the knowledge
    "mcp",                          # the server .mcp.json starts
    "docs",                         # what the AI reads to answer
    ".mcp.json",                    # Claude Code reads this from the folder it opens
    "README.md",
    "requirements.txt",             # WHAT-TO-INSTALL step 4 installs from it
    "requirements-optional.txt",    # and check-dependencies reads both
    os.path.join("platform", "heron-products.json"),   # the installer's own list
    os.path.join("tools", "deploy-addin.ps1"),         # what an installer drives
    os.path.join("tools", "HeronRevit.ps1"),           # and what that dot-sources
    os.path.join("tools", "check-dependencies.py"),    # WHAT-TO-INSTALL section 5
]

# THE INSTALLERS RIDE IN THE DOWNLOAD - row 5b-371.
#
# NO RELEASE EVER CARRIED ONE. .gitignore keeps the built HeronInstaller.exe
# out of the repository on purpose, saying a downloader gets the release
# asset - and nothing made that asset. A new person who searched the
# repository and its releases for the installer, on 2026-10-08, found it in
# neither.
#
# BOTH DOORS, BECAUSE docs/WHAT-TO-INSTALL.md NAMES BOTH: the window for a
# person, `heron-install` for the AI (Stage 6). The executable each makes is
# read from its project's <AssemblyName>, never typed here.
#
# AT THE ROOT OF heron-project.zip, NOT BESIDE IT AS AN ASSET OF ITS OWN. Each
# finds the product list by walking up from its own folder to
# platform\heron-products.json, and runs the two scripts above from that same
# folder - so alone in a Downloads folder it could only say the product list
# is missing. Unzipped with the workspace, it is already at that root.
#
# SELF-CONTAINED, which Q-PE-7 recommends in so many words: "an installer
# with a prerequisite is not an installer". The framework-dependent build is
# a quarter of a megabyte and asks a modeller whose newest Revit is 2024 -
# which brings no .NET 8 - to install the .NET Desktop Runtime first.
# Measured 2026-10-08: 72 MB for the window and 35 MB for the command,
# compressed, one file each.
INSTALLERS = [
    os.path.join("platform", "Heron.Installer.App", "Heron.Installer.App.csproj"),
    os.path.join("platform", "Heron.Installer.Cli", "Heron.Installer.Cli.csproj"),
]

# Windows on x64 is the only machine Revit runs on, so it is the only one an
# installer for it is built for.
INSTALLER_RUNTIME = "win-x64"

# NEVER, wherever they appear. __pycache__ is a build artefact that differs
# between machines, and .git is the whole history - neither belongs in a
# handover folder, and .git would carry every deleted file ever committed.
WORKSPACE_NEVER = ("__pycache__", ".git", "bin", "obj")

WORKSPACE_NAME = "heron-project.zip"


def installer_name(csproj):
    """
    The executable a project publishes, from its own <AssemblyName>.

    READ, NOT TYPED. The window's project says why it is HeronInstaller and
    not Heron.Installer; a second spelling here is the one that goes stale.
    """
    text = io.open(os.path.join(ROOT, csproj), encoding="utf-8-sig").read()
    found = re.search(r"<AssemblyName>\s*([^<\s]+)\s*</AssemblyName>", text)
    if not found:
        raise IOError("%s names no <AssemblyName>, so there is no way to say "
                      "which executable it publishes" % csproj)
    return found.group(1) + ".exe"


def build_installers(stage_dir):
    """
    Publish every installer into stage_dir. Returns [(project, why)] for each
    that failed - empty when all of them built.

    -p:EnableWindowsTargeting=true for the same reason build() passes it: the
    release is built on Linux, and the window is a Windows window.
    """
    failed = []
    for csproj in INSTALLERS:
        if not os.path.exists(os.path.join(ROOT, csproj)):
            failed.append((csproj, "%s does not exist, so the release would "
                                   "carry no installer" % csproj))
            continue

        proc = subprocess.Popen(
            ["dotnet", "publish", csproj, "-c", CONFIGURATION,
             "-r", INSTALLER_RUNTIME,
             "--self-contained", "true",
             "-p:PublishSingleFile=true",
             "-p:IncludeNativeLibrariesForSelfExtract=true",
             "-p:EnableCompressionInSingleFile=true",
             "-p:DebugType=none",
             "-p:EnableWindowsTargeting=true",
             "-o", stage_dir,
             "--nologo", "-v", "quiet"],
            cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        out = proc.communicate()[0].decode("utf-8", "replace")

        if proc.returncode != 0:
            failed.append((csproj, out.strip() or
                           "dotnet publish exited %d and said nothing" % proc.returncode))
            continue

        exe = installer_name(csproj)
        if not os.path.exists(os.path.join(stage_dir, exe)):
            failed.append((csproj, "the publish reported success but %s is "
                                   "not in what it wrote" % exe))
    return failed


def workspace(out_dir, installers_dir=None):
    """
    Zip what the user keeps beside the plugin. Returns the asset's path.

    `installers_dir` holds what build_installers() published; every file in it
    goes at the ROOT of the zip, the folder the installers look for their
    product list from. None packs the workspace alone, which is what a test
    with no dotnet asks for - main() always passes one.

    NAMED heron-project.zip BECAUSE docs/07 AND Q-PE-14 ALREADY CALL IT THE
    PROJECT FOLDER, and a second word for one thing is how two words drift
    apart. Recorded once, because it is a real risk and not a comfortable one:
    to a Revit modeller "project" means the .rvt they have open, and this is
    not that. If it confuses the first user it is a rename, not a redesign.
    """
    path = os.path.join(out_dir, WORKSPACE_NAME)

    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as zf:
        if installers_dir:
            for name in sorted(os.listdir(installers_dir)):
                zf.write(os.path.join(installers_dir, name), name)

        for keep in WORKSPACE_KEEP:
            full = os.path.join(ROOT, keep)

            if os.path.isfile(full):
                zf.write(full, keep)
                continue

            if not os.path.isdir(full):
                # SAID, NEVER SKIPPED IN SILENCE. A folder that was renamed and
                # not renamed here would otherwise ship a workspace missing the
                # brain, and the first anybody knew would be a Connect button
                # that still answers nothing.
                raise IOError(
                    "R-51 says the download carries the brain, and '%s' is not "
                    "in this repository. Either it moved and WORKSPACE_KEEP did "
                    "not, or it was deleted. Publishing without it would ship a "
                    "workspace that cannot answer anything." % keep)

            for base, dirs, files in os.walk(full):
                dirs[:] = [d for d in dirs if d not in WORKSPACE_NEVER]
                for name in sorted(files):
                    if name.endswith(".pyc"):
                        continue
                    one = os.path.join(base, name)
                    zf.write(one, os.path.relpath(one, ROOT))

    return path


def checksums(out_dir):
    """
    SHA-256 of every asset, for R-12.

    WRITTEN LAST, and that is deliberate: it is the completion mark for the
    whole folder, the same way replaced.json marks a finished backup in
    tools/deploy-addin.ps1. A dist folder without it is one whose build did
    not finish.
    """
    lines = []
    for name in sorted(os.listdir(out_dir)):
        if name == "checksums.txt":
            continue
        digest = hashlib.sha256()
        with open(os.path.join(out_dir, name), "rb") as fh:
            for chunk in iter(lambda: fh.read(65536), b""):
                digest.update(chunk)
        lines.append("%s  %s" % (digest.hexdigest(), name))

    path = os.path.join(out_dir, "checksums.txt")
    io.open(path, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
    return lines


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", default="dist",
                        help="where to write the assets (default: dist)")
    parser.add_argument("--releases", default=None,
                        help="comma-separated Revit releases; default is every "
                             "release Heron supports")
    parser.add_argument("--list", action="store_true",
                        help="say what would be built, and build nothing")
    args = parser.parse_args()

    wanted = NET.RELEASES if not args.releases else [
        r.strip() for r in args.releases.split(",") if r.strip()]

    unknown = [r for r in wanted if r not in NET.RELEASES]
    if unknown:
        print("FAILED  Heron does not support Revit %s." % ", ".join(unknown))
        print("        Supported: %s. Add a release to brain/heron_dotnet.py"
              % ", ".join(NET.RELEASES))
        print("        and Directory.Build.props before asking for it here.")
        return 1

    deployable = products()
    buildable, planned, broken = sort_by_project(deployable)

    if broken:
        print("FAILED  a product that is past PLANNED has no source to build.")
        for product in broken:
            print("  - '%s' is %s, and revit/%s/ is not there."
                  % (product["id"], product.get("state") or "unstated",
                     project_of(product)))
        print()
        print("        Either the folder was renamed and the manifest was not,")
        print("        or the product's state is ahead of its code. Publishing")
        print("        without it would install nothing on that product and say")
        print("        nothing about why - AJ Tools' lesson L3.")
        return 1

    pairs = [(p, r) for p in buildable for r in wanted]

    print("%d product(s) x %d release(s) = %d asset(s), all in %s"
          % (len(buildable), len(wanted), len(pairs), CONFIGURATION))
    for product in buildable:
        print("    %-16s %s" % (product["id"], product["assembly"]))

    # NOT SILENT. A product the manifest describes and this cannot build is
    # said out loud, every run, so nobody reads the asset list as the whole
    # product list.
    for product in planned:
        print("    %-16s not built yet - it is in the plan, and revit/%s/ does "
              "not exist" % (product["id"], project_of(product)))
    print("    and in %s, the installers: %s"
          % (WORKSPACE_NAME, ", ".join(installer_name(p) for p in INSTALLERS)))
    print()

    if args.list:
        for product, release in pairs:
            print("    %s" % asset_name(product, release))
        print("    %s" % WORKSPACE_NAME)
        return 0

    if shutil.which("dotnet") is None:
        print("NOT RUN  no `dotnet` on this machine, so nothing can be built.")
        print()
        print("         This is the machine, not the change:")
        print("             apt-get update && apt-get install -y dotnet-sdk-10.0")
        print()
        print("         Exit 3 means COULD NOT RUN. It is not a pass.")
        return NO_DOTNET

    out_dir = args.out if os.path.isabs(args.out) else os.path.join(ROOT, args.out)

    # EMPTIED FIRST. A leftover asset from a previous run would be checksummed
    # and published alongside the new ones, and nothing downstream could tell
    # that one of them is from another version - the same shape of defect as
    # the stale build the deploy script now refuses.
    if os.path.isdir(out_dir):
        shutil.rmtree(out_dir)
    os.makedirs(out_dir)

    failed = []
    for product, release in pairs:
        print("  building %s for Revit %s" % (product["id"], release))
        why = build(product, release)
        if why:
            print("    FAILED")
            failed.append((product["id"], release, why))
            continue
        path = pack(product, release, out_dir)
        print("    %s  %d bytes" % (os.path.basename(path), os.path.getsize(path)))

    # THE INSTALLERS, ONCE - row 5b-371. Release-independent like the brain,
    # and built only when every product did: an installer beside a half-built
    # release is one that offers a Revit it then cannot fill.
    installers_dir = None
    if not failed:
        installers_dir = tempfile.mkdtemp(prefix="heron-installers-")
        print("  building the installers - the window and the command")
        for csproj, why in build_installers(installers_dir):
            print("    FAILED")
            failed.append((csproj, None, why))

    # THE BRAIN, ONCE, BESIDE THE EIGHT RELEASES OF PLUGIN - R-51. It follows
    # no Revit release, so it is packed once rather than eight times, and it is
    # packed BEFORE the checksums because checksums.txt is the completion mark
    # for the whole folder.
    try:
        if not failed:
            print("  packing the workspace - brain, skills, MCP, docs and the installers")
            made = workspace(out_dir, installers_dir)
            print("    %s  %d bytes" % (os.path.basename(made), os.path.getsize(made)))
    finally:
        if installers_dir:
            shutil.rmtree(installers_dir, ignore_errors=True)

    if failed:
        print()
        print("FAILED (%d)" % len(failed))
        for product_id, release, why in failed:
            if release:
                print("  - %s for Revit %s:" % (product_id, release))
            else:
                print("  - %s:" % product_id)
            for line in why.strip().split("\n")[-6:]:
                print("      %s" % line)
        print()
        print("NO CHECKSUMS WERE WRITTEN, so this folder is not publishable.")
        print("That is the point: a release missing one release's asset installs")
        print("nothing on that Revit and says nothing about why.")
        return 1

    # THE MANIFEST TRAVELS WITH THE ASSETS - Stage 5 item 1. The installer
    # reads the product list from the release it is installing, never from
    # whatever a branch says today.
    shutil.copyfile(MANIFEST, os.path.join(out_dir, "heron-products.json"))

    lines = checksums(out_dir)
    print()
    # RELPATH, NOT os.path.relpath. `--out` may name another drive, and on
    # Windows the raw call raised HERE - after every asset was packed and
    # checksums.txt, the completion mark, written - so a finished release
    # exited 1 with a traceback (row 5b-152).
    print("%d asset(s) and the product list in %s"
          % (len(lines) - 1, RELPATH.relpath(out_dir, ROOT)))
    print("checksums.txt covers %d file(s)" % len(lines))
    print()
    print("NOTHING HAS BEEN PUBLISHED, and nothing has been installed. This")
    print("built and packaged; whether the add-in loads into Revit is")
    print("docs/NEEDS-CHECKING.md's question and needs a real machine.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
