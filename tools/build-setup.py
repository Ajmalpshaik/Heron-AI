# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-PKG-012
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

r"""
Build Heron's Setup for modellers - one file, per user (D-167).

    python tools/build-setup.py                    build every asset, then the Setup
    python tools/build-setup.py --dist dist        use assets already built there
    python tools/build-setup.py --list             say what it would do, build nothing

WHAT IT MAKES
-------------
    dist/HeronSetup-<version>-unsigned.exe     with no sign command - NOT RUN signing
    dist/HeronSetup-<version>.exe              with one (--sign, or HERON_SIGN_COMMAND)

Inside the Setup is the same thing a downloaded release is, so nothing new is
invented to install it:

    Heron AI\                   heron-project.zip unzipped - the brain, the MCP
                                server, the two installers at the top
    Heron AI\payload\           every <product>-<year>.zip, heron-products.json
                                and checksums.txt - the folder `heron-install
                                --from` reads, every zip checked before use

and platform/Heron.Setup/HeronSetup.iss, which puts that folder in place and
opens HeronInstaller.exe --from its payload (heron-install itself, when run
silent). It decides nothing about what goes where.

SIGNING IS NOT RUN UNTIL A CERTIFICATE EXISTS (D-167 item 6). With no sign
command the Setup is built unsigned and its NAME says so, so an unsigned file is
never mistaken for a signed one. A sign command is an Inno Setup sign tool line
with $f where the file goes - whichever service signs it, the build only passes
the line on. No certificate is bought or made here.

WHAT IT NEEDS
-------------
Windows and Inno Setup 6 (ISCC.exe) - to BUILD the Setup, never to run it.
Found at HERON_ISCC, or in the usual per-user and all-users folders. Without it
this exits 3, NOT RUN, which is never a pass. And the .NET 10 SDK, when it builds
the assets itself (tools/build-release-assets.py).

Exit 0 = the Setup was built. 1 = something failed, and it says which.
3 = could not run here: not Windows, or no Inno Setup.
"""

import argparse
import hashlib
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPT = os.path.join(ROOT, "platform", "Heron.Setup", "HeronSetup.iss")
ICON = os.path.join(ROOT, "platform", "Heron.Installer.App", "Resources", "Heron.ico")
MANIFEST = os.path.join(ROOT, "platform", "heron-products.json")
WORKSPACE = "heron-project.zip"
CHECKSUMS = "checksums.txt"

#: Copied beside Heron in the Setup - the licence travels with what it licenses.
ALSO_SHIP = ("LICENSE", "NOTICE")

NOT_RUN = 3


def version():
    """
    The highest product version in the manifest - the same rule
    UpdateCheck.HighestVersion reads, so the Setup and the update check never
    disagree about which Heron this is.
    """
    with io.open(MANIFEST, encoding="utf-8-sig") as fh:
        data = json.load(fh)
    best, best_parts = None, None
    for product in data.get("products", []):
        text = product.get("version") or ""
        try:
            parts = [int(p) for p in text.split(".")]
        except ValueError:
            continue
        if best_parts is None or parts > best_parts:
            best, best_parts = text, parts
    if best is None:
        raise ValueError("No product in %s carries a version, so the Setup cannot be named." % MANIFEST)
    return best


def find_iscc():
    """ISCC.exe, or None. HERON_ISCC wins; then the two places Inno Setup installs."""
    named = os.environ.get("HERON_ISCC")
    if named:
        return named if os.path.isfile(named) else None
    places = [
        os.path.join(os.environ.get("LOCALAPPDATA", ""), "Programs", "Inno Setup 6", "ISCC.exe"),
        os.path.join(os.environ.get("ProgramFiles(x86)", ""), "Inno Setup 6", "ISCC.exe"),
        os.path.join(os.environ.get("ProgramFiles", ""), "Inno Setup 6", "ISCC.exe"),
    ]
    for place in places:
        if place and os.path.isfile(place):
            return place
    return shutil.which("ISCC") or shutil.which("iscc")


def unzip_safely(archive, into):
    """
    Unpack, refusing any entry that would land outside `into` - the same guard
    the installer's own unpacking keeps (ReleaseAssets), for the same reason.
    """
    base = os.path.realpath(into)
    with zipfile.ZipFile(archive) as zf:
        for entry in zf.infolist():
            target = os.path.realpath(os.path.join(base, entry.filename))
            if target != base and not target.startswith(base + os.sep):
                raise ValueError("%s holds '%s', which would land outside the folder. "
                                 "Nothing was staged." % (archive, entry.filename))
        zf.extractall(base)


def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def stage(dist, stage_root):
    """
    The folder the Setup carries, built fresh under `stage_root`. Returns its
    path. Refuses a dist folder whose build did not finish: checksums.txt is
    written last (build-release-assets.py), so a folder without it is not one.

    STAGED IN A TEMPORARY FOLDER, NEVER INSIDE THE REPOSITORY. A whole copy of
    Heron left in dist/setup-stage was read by tools/check-docs.py as 2,915
    broken links and by check-structure.py as code in an undeclared part
    (2026-11-16) - dist/ is ignored by git, not by the gates.
    """
    for need in (WORKSPACE, CHECKSUMS):
        if not os.path.isfile(os.path.join(dist, need)):
            raise IOError("%s holds no %s, so its build did not finish. Run "
                          "python tools/build-release-assets.py --out %s first." % (dist, need, dist))

    heron = os.path.join(stage_root, "Heron AI")
    payload = os.path.join(heron, "payload")
    os.makedirs(payload)

    unzip_safely(os.path.join(dist, WORKSPACE), heron)

    for name in sorted(os.listdir(dist)):
        full = os.path.join(dist, name)
        if not os.path.isfile(full) or name == WORKSPACE:
            continue
        if name.lower().startswith("heronsetup-"):
            continue                      # an earlier Setup is not part of the next one
        shutil.copy2(full, os.path.join(payload, name))

    for name in ALSO_SHIP:
        src = os.path.join(ROOT, name)
        if os.path.isfile(src):
            shutil.copy2(src, os.path.join(heron, name))

    for need in ("HeronInstaller.exe", "heron-install.exe"):
        if not os.path.isfile(os.path.join(heron, need)):
            raise IOError("%s has no %s at its top, so the Setup would have nothing to hand "
                          "over to. Rebuild the assets." % (WORKSPACE, need))
    return heron


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dist", default=None,
                        help="use the assets already built in this folder")
    parser.add_argument("--out", default="dist", help="where the Setup goes (default: dist)")
    parser.add_argument("--sign", default=os.environ.get("HERON_SIGN_COMMAND"),
                        help="an Inno Setup sign-tool line with $f for the file; "
                             "default HERON_SIGN_COMMAND, else the Setup is built unsigned")
    parser.add_argument("--list", action="store_true", help="say what would happen, build nothing")
    args = parser.parse_args()

    out = os.path.abspath(args.out)
    dist = os.path.abspath(args.dist) if args.dist else out
    ver = version()
    name = "HeronSetup-%s%s" % (ver, "" if args.sign else "-unsigned")
    iscc = find_iscc()

    print("Heron %s - the Setup would be %s" % (ver, os.path.join(out, name + ".exe")))
    print("  assets:  %s" % (dist if args.dist else "built now into " + out))
    print("  signing: %s" % ("by the command given" if args.sign else
                             "NOT RUN - no sign command, so it is built unsigned and named so"))
    print("  Inno Setup: %s" % (iscc or "NOT FOUND"))

    if args.list:
        print("--list was asked for, so nothing was built.")
        return 0

    if os.name != "nt":
        print("NOT RUN: the Setup is built on Windows - Inno Setup is a Windows program.")
        return NOT_RUN
    if not iscc:
        print("NOT RUN: Inno Setup 6 is not on this PC, so the Setup cannot be built. Install it "
              "(winget install --id JRSoftware.InnoSetup --scope user), or name ISCC.exe in HERON_ISCC.")
        return NOT_RUN

    if not args.dist:
        print()
        print("Building every product for every Revit year first ...")
        done = subprocess.call([sys.executable, os.path.join(ROOT, "tools", "build-release-assets.py"),
                                "--out", out], cwd=ROOT)
        if done != 0:
            print("The assets did not all build, so no Setup was made.")
            return 1

    stage_root = tempfile.mkdtemp(prefix="heron-setup-stage-")
    try:
        return make(args, dist, out, ver, name, iscc, stage_root)
    finally:
        shutil.rmtree(stage_root, ignore_errors=True)


def make(args, dist, out, ver, name, iscc, stage_root):
    """Stage, run Inno Setup, and say what was made. The exit code."""
    try:
        heron = stage(dist, stage_root)
    except (IOError, OSError, ValueError) as e:
        print(str(e))
        return 1

    command = [iscc, "/Q",
               "/DStage=%s" % heron,
               "/DAppVersion=%s" % ver,
               "/DOutputDir=%s" % out,
               "/DOutputName=%s" % name,
               "/DIconFile=%s" % ICON]
    if args.sign:
        command += ["/DSign", "/Sheronsign=%s" % args.sign]
    command.append(SCRIPT)

    print()
    print("Making the Setup with Inno Setup ...")
    if subprocess.call(command, cwd=ROOT) != 0:
        print("Inno Setup did not finish, so no Setup was made. Its own lines above say why.")
        return 1

    made = os.path.join(out, name + ".exe")
    if not os.path.isfile(made):
        print("Inno Setup said it finished, but %s is not there." % made)
        return 1

    print()
    print("Made %s" % made)
    print("  %.1f MB, SHA-256 %s" % (os.path.getsize(made) / 1048576.0, sha256(made)))
    print("  %s" % ("signed by the command given - check it: Get-AuthenticodeSignature" if args.sign
                    else "UNSIGNED - Windows will say it is from an unknown publisher"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
