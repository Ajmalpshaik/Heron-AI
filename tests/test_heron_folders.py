#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The folders the install scripts need, written by the add-in's own path code - Q-58.

    python tests/test_heron_folders.py

WHY THIS EXISTS
---------------
platform/README.md rule 3 says `HeronPaths` is the only thing that builds a
Heron path, and the install scripts built two anyway - the Revit Addins
folder and the install backup - because a `.ps1` cannot call a C# class and
runs before any Heron assembly is on the PC. Q-58 was decided on 2026-11-15
on the owner's delegation: **the add-in's path code writes a folder list the
install script reads, and a check compares the written list with the code.**

This is that check, and the claims it holds:

  1. `HeronPaths` owns both folders, and the generator ASKS it - the host
     links `HeronPaths.cs` itself and spells no part of either path. Since
     2026-11-16 it also owns Revit's ALL-USER Addins folders, which the
     second-copy check reads and Heron never writes (D-167).
  2. `platform/heron-folders.json` is exactly what the code says today. A
     hand edit fails; a regeneration passes. It names a Windows special
     folder and the parts under it, never one machine's own path.
  3. The PowerShell scripts read the list and build no Heron path of their
     own: no `$env:APPDATA`, no `$env:LOCALAPPDATA`, no `Revit\\Addins` and no
     `install-backup` in their code, every folder they ask for is in the list,
     and every blank in it is filled.
  4. The list goes wherever the scripts go - heron-project.zip carries it -
     and Windows PowerShell 5.1 can read it with no module: ASCII JSON.

Part 2 BUILDS AND RUNS C#, so it needs a .NET SDK and a runtime that meet.
Without one it is NOT RUN and this exits 3, which is not a pass. Parts 1, 3
and 4 read files and run anywhere.

WHAT IT CANNOT PROVE: that the scripts RUN. They are PowerShell for Windows
and this is Linux - the run is NEEDS-CHECKING row AB11.
"""

import importlib.util
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile

# A redirected run on Windows encodes print() in the ANSI code page, and a
# path or a quote out of a file can carry a character it has not got.
for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "heron-folders.py")
LIST = os.path.join(ROOT, "platform", "heron-folders.json")
PATHS_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronPaths.cs")
HOST = os.path.join(ROOT, "tests", "Heron.Folders.TestHost")
HOST_PROJ = os.path.join(HOST, "Heron.Folders.TestHost.csproj")
HOST_CODE = os.path.join(HOST, "Program.cs")
TOOLS = os.path.join(ROOT, "tools")
REVIT_PS = os.path.join(TOOLS, "HeronRevit.ps1")
DEPLOY_PS = os.path.join(TOOLS, "deploy-addin.ps1")
RELEASE = os.path.join(TOOLS, "build-release-assets.py")
COULD_NOT_RUN = 3

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return None


def cs_code(text):
    """The C# with its comments taken out, so a comment cannot pass a check."""
    text = re.sub(r"/\*.*?\*/", "", text or "", flags=re.S)
    return "\n".join(line for line in text.split("\n")
                     if not line.lstrip().startswith("//"))


def ps_code(text):
    """The PowerShell with its comments taken out: `<# #>` blocks and `#` lines.

    A comment that explains where a folder used to be spelled is history, and
    deploy-addin.ps1 keeps a great deal of it on purpose. Only code builds a
    path, so only code is asked.
    """
    text = re.sub(r"<#.*?#>", "", text or "", flags=re.S)
    return "\n".join(line for line in text.split("\n")
                     if not line.lstrip().startswith("#"))


def py_code(text):
    """The Python with its docstrings and `#` lines taken out, as ps_code."""
    text = re.sub(r'"""[\s\S]*?"""|\'\'\'[\s\S]*?\'\'\'', "", text or "")
    return "\n".join(line for line in text.split("\n")
                     if not line.lstrip().startswith("#"))


def load_tool():
    """tools/heron-folders.py as a module, or None when it is not there."""
    if not os.path.isfile(TOOL):
        return None
    spec = importlib.util.spec_from_file_location("heron_folders_tool", TOOL)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def placeholders(parts):
    return sorted(set(re.findall(r"\{(\w+)\}", " ".join(parts))))


# --------------------------------------------------------------------- 1
def the_code_owns_both():
    print("1. HeronPaths owns both folders, and the generator asks it")
    code = cs_code(read(PATHS_CS))
    check("public static string RevitAddins(string" in code,
          "HeronPaths has RevitAddins(release) - the Revit Addins folder")
    check("public static string InstallBackup(string" in code,
          "HeronPaths has InstallBackup(release, folder) - the install a "
          "deploy last replaced")
    raw = read(PATHS_CS) or ""
    at = raw.find("public static string[] RevitAllUserAddins(string")
    check(at != -1, "HeronPaths has RevitAllUserAddins(release) - Revit's all-user "
                    "Addins folders")
    check(at != -1 and "HERON NEVER INSTALLS HERE" in raw[max(0, at - 2500):at],
          "and its own comment says Heron never installs there - read only")

    proj = read(HOST_PROJ)
    check(proj is not None, "the generator's host project is there")
    proj = proj or ""
    check("platform\\Heron.Core\\HeronPaths.cs" in proj,
          "it compiles HeronPaths.cs itself, by source - the code, not a copy")
    linked = re.findall(r'<Compile\s+Include="([^"]+)"', proj)
    check(linked and all(l.endswith("HeronPaths.cs") for l in linked),
          "and links no other file of Heron's (%s)" % ", ".join(linked or ["none"]))

    host = cs_code(read(HOST_CODE))
    check("HeronPaths.RevitAddins(" in host and "HeronPaths.InstallBackup(" in host,
          "the host asks HeronPaths for both folders")
    check("HeronPaths.RevitAllUserAddinsTemplate(" in host
          and "HeronPaths.RevitAllUserAddins(" in host,
          "and for the all-user ones, declared and resolved, to compare the two")
    # A PATH PART, alone or between separators - not a key like "RevitAddins"
    # or a file name like "Heron.Core/HeronPaths.cs", which name no folder.
    part = r"(autodesk|revit|addins|install-backup|heron)"
    spelled = [s for s in re.findall(r'"([^"\n]*)"', host)
               if re.search(r"(?i)^%s$|%s[\\/]|[\\/]%s([\\/]|$)" % (part, part, part), s)]
    check(not spelled,
          "and spells no part of either path itself%s"
          % ("" if not spelled else " - found %s" % spelled))
    print()


# --------------------------------------------------------------------- 2
def the_list_is_the_code(tool):
    """The list against the code. Returns False when it could not run."""
    print("2. platform/heron-folders.json is what the code says")
    check(tool is not None, "tools/heron-folders.py is there, the generator's door")
    if tool is None:
        print("        (nothing below can be asked without it)")
        print()
        return True     # a FAILURE already recorded, not a NOT RUN

    for name in ("build_host", "ask_host", "differences", "main"):
        check(callable(getattr(tool, name, None)), "it has %s()" % name)
    if FAILURES and not all(callable(getattr(tool, n, None))
                            for n in ("build_host", "ask_host", "differences", "main")):
        print()
        return True

    work = tempfile.mkdtemp(prefix="heron-folders-")
    try:
        code, dll = tool.build_host(work)
        if code == COULD_NOT_RUN:
            print("  NOT RUN  %s" % dll)
            print()
            return False
        check(code == 0, "the host builds%s" % ("" if code == 0 else " - %s" % dll))
        if code != 0:
            print()
            return True

        code, said = tool.ask_host(dll)
        check(code == 0, "the host answers%s" % ("" if code == 0 else " - %s" % said))
        if code != 0:
            print()
            return True

        committed = read(LIST)
        check(committed is not None, "the list is committed at platform/heron-folders.json")
        check(committed is not None and not tool.differences(committed, said),
              "and it is exactly what HeronPaths says today")

        # THE CLI'S OWN EXIT CODES, because a person and CI meet it as one.
        def run(*args):
            done = subprocess.run([sys.executable, TOOL] + list(args),
                                  stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            return done.returncode, done.stdout.decode("utf-8", "replace")

        code, out = run()
        check(code == 0, "`python tools/heron-folders.py` exits 0 on the committed "
                         "list (%d)" % code)

        # A HAND EDIT FAILS. The list is a copy the code wrote; one changed by
        # hand is a second owner of the path, which is the defect Q-58 closes.
        if committed is not None:
            edited = os.path.join(work, "edited.json")
            with io.open(edited, "w", encoding="utf-8", newline="\n") as fh:
                fh.write(committed.replace('"install-backup"', '"install-backups"'))
            check(tool.differences(io.open(edited, encoding="utf-8").read(), said),
                  "a list edited by hand differs from the code")
            code, out = run("--against", edited)
            check(code == 1, "and the check exits 1 on it (%d)" % code)
            check("--write" in out, "and says the command that regenerates it")

            # AND A REGENERATED ONE PASSES - written by --write into a copy,
            # never over the committed file.
            regenerated = os.path.join(work, "regenerated.json")
            shutil.copyfile(edited, regenerated)
            code, out = run("--write", "--to", regenerated)
            check(code == 0, "regenerating it exits 0 (%d)" % code)
            code, out = run("--against", regenerated)
            check(code == 0, "and the regenerated list passes the check (%d)" % code)

        missing = os.path.join(work, "no-such-list.json")
        code, out = run("--against", missing)
        check(code == 1 and "no folder list" in out.lower(),
              "a missing list fails by name, never as a pass (%d)" % code)

        # TEMPLATES, NOT ONE MACHINE'S PATHS. The same code run as two other
        # users - two homes, no XDG override - must write the same list, and
        # neither home may appear in it. Off Windows only: there a home is an
        # environment variable, and on Windows the profile is the profile.
        if os.name != "nt" and callable(getattr(tool, "scratch_user", None)):
            homes = [os.path.join(work, "home-a"), os.path.join(work, "home-b")]
            answers = [tool.ask_host(dll, env=tool.scratch_user(work, os.path.basename(h)))
                       for h in homes]
            check(all(c == 0 for c, _ in answers),
                  "the host answers for two other users%s"
                  % "".join(" - %s" % a for c, a in answers if c != 0))
            check(answers[0][1] == answers[1][1] == said,
                  "and writes the same list for both, and for the default one")
            check(not any(h in a for h in homes + [os.path.expanduser("~")]
                          for _, a in answers),
                  "and no user's own folder appears in it")

            # AND A PROFILE WITHOUT THE FOLDER IS A STOP, NEVER A RELATIVE
            # PATH WRITTEN DOWN AS IF IT WERE A TEMPLATE.
            bare = os.path.join(work, "bare-home")
            os.makedirs(bare)
            env = dict(os.environ, HOME=bare)
            for name in ("XDG_CONFIG_HOME", "XDG_DATA_HOME"):
                env.pop(name, None)
            code, why = tool.ask_host(dll, env=env)
            check(code == 1 and "relative path" in why,
                  "a profile with no application-data folder stops the host, "
                  "by name (%d)" % code)
        elif os.name != "nt":
            check(False, "the tool has scratch_user(), to ask as another user")
    finally:
        shutil.rmtree(work, ignore_errors=True)
    print()
    return True


# --------------------------------------------------------------------- 3
def the_scripts_read_it():
    print("3. the install scripts read the list and build no Heron path")
    committed = read(LIST)
    folders = {}
    if committed is not None:
        try:
            folders = json.loads(committed).get("folders", {})
        except ValueError:
            folders = {}

    revit = read(REVIT_PS) or ""
    code = ps_code(revit)
    check("function Get-HeronFolder" in code,
          "tools/HeronRevit.ps1 has Get-HeronFolder, the one reader of the list")
    check("platform\\heron-folders.json" in code and "ConvertFrom-Json" in code,
          "which reads platform\\heron-folders.json with ConvertFrom-Json - no module")
    # THE CALL IS READ OUT OF HeronPaths, never typed here: the script must
    # make the one HeronPaths makes, whatever it is. (Typed, it would also be
    # this file resolving a special folder by check-structure's reading.)
    made = re.search(r"Environment\.(\w+)\(\s*Environment\.SpecialFolder",
                     cs_code(read(PATHS_CS)))
    made = made.group(1) if made else None
    check(made is not None, "HeronPaths resolves its special folders through one call")
    check(made is not None and ("[Environment]::%s(" % made) in code
          and "[Environment+SpecialFolder]" in code,
          "and Get-HeronFolder makes the same call, with the folder named in the list")
    body = code[code.find("function Get-HeronFolder"):] if "function Get-HeronFolder" in code else ""
    stop = body.find("Test-Path")
    check(stop != -1 and "throw" in body[stop:stop + 600],
          "and STOPS, by name, when the list is not there")
    check("fetch them again" in body,
          "saying what to do about it - the Heron files are incomplete")

    addins = code[code.find("function Get-RevitAddinsFolder"):]
    addins = addins[:addins.find("\nfunction ", 1)] if "\nfunction " in addins[1:] else addins
    check("Get-HeronFolder -Name RevitAddins" in addins,
          "Get-RevitAddinsFolder asks the list rather than spelling the folder")

    # EVERY ALL-USER FOLDER THE LIST HAS, AND NO OTHER. A third one HeronPaths
    # declares later and the script never asks would be a folder Revit reads
    # and the second-copy check does not (D-167).
    alluser = code[code.find("function Get-RevitAllUserAddinsFolders"):] \
        if "function Get-RevitAllUserAddinsFolders" in code else ""
    alluser = alluser[:alluser.find("\nfunction ", 1)] if "\nfunction " in alluser[1:] else alluser
    asked_all = sorted(re.findall(r"Get-HeronFolder\s+-Name\s+(\w+)", alluser))
    listed_all = sorted(name for name, entry in folders.items()
                        if isinstance(entry, dict)
                        and entry.get("from") == "HeronPaths.RevitAllUserAddins")
    check(listed_all and asked_all == listed_all,
          "Get-RevitAllUserAddinsFolders asks the list for every all-user folder "
          "in it, and no other (%s)" % ", ".join(asked_all or ["none"]))

    deploy = ps_code(read(DEPLOY_PS))
    check(re.search(r"\$backupDir\s*=\s*Get-HeronFolder -Name InstallBackup", deploy) is not None,
          "deploy-addin.ps1 takes the backup folder from the list")
    line = re.search(r"\$backupDir\s*=.*", deploy)
    line = line.group(0) if line else ""
    check("release = $RevitVersion" in line and "folder = $productFolder" in line,
          "one backup per release per product, as before (row 5b-96)")

    # EVERY .ps1 IN tools/, so a third script cannot grow a copy unnoticed.
    spelled = []
    for name in sorted(os.listdir(TOOLS)):
        if not name.endswith(".ps1"):
            continue
        for n, text in enumerate(ps_code(read(os.path.join(TOOLS, name))).split("\n"), 1):
            if re.search(r"(?i)\$env:(LOCAL)?APPDATA\b|\$\{env:(LOCAL)?APPDATA\}"
                         r"|Revit\\Addins|install-backup", text):
                spelled.append("%s: %s" % (name, text.strip()))
    check(not spelled,
          "no install script's CODE resolves APPDATA or spells either folder%s"
          % ("" if not spelled else " - %d line(s):\n          %s"
             % (len(spelled), "\n          ".join(spelled))))

    # EVERY FOLDER ASKED FOR IS IN THE LIST, AND EVERY BLANK IS FILLED. A
    # name the list has not got, or a blank left unfilled, is a stop on the
    # modeller's PC that nothing here could otherwise see.
    asked = []
    for name in sorted(os.listdir(TOOLS)):
        if name.endswith(".ps1"):
            text = ps_code(read(os.path.join(TOOLS, name)))
            for m in re.finditer(r"Get-HeronFolder\s+-Name\s+(\w+)(?:\s+-Fill\s+@\{([^}]*)\})?", text):
                asked.append((name, m.group(1),
                              sorted(re.findall(r"(\w+)\s*=", m.group(2) or ""))))
    check(asked, "the scripts ask the list for something (%d call(s))" % len(asked))
    for script, folder, keys in asked:
        entry = folders.get(folder)
        check(entry is not None, "%s asks for %s, and the list has it" % (script, folder))
        if entry is not None:
            want = placeholders(entry.get("parts", []))
            check(keys == want, "and fills exactly its blanks %s (%s)"
                  % (want, keys))
    print()


# --------------------------------------------------------------------- 4
def the_list_travels():
    print("4. the list goes where the scripts go, and PowerShell 5.1 reads it")
    raw = None
    try:
        raw = io.open(LIST, "rb").read()
    except (IOError, OSError):
        pass
    check(raw is not None, "the list is there")
    raw = raw or b""
    try:
        raw.decode("ascii")
        ascii_only = True
    except UnicodeDecodeError:
        ascii_only = False
    check(ascii_only, "it is ASCII, which Windows PowerShell 5.1 reads whatever its code page")
    check(b"%APPDATA%" not in raw and b"%LOCALAPPDATA%" not in raw,
          "it names the special folder, not a variable a Store app can copy (D-135)")
    try:
        doc = json.loads(raw.decode("ascii", "replace")) if raw else None
    except ValueError:
        doc = None
    check(isinstance(doc, dict), "it is JSON, one object")
    folders = (doc if isinstance(doc, dict) else {}).get("folders", {})
    for name in ("RevitAddins", "InstallBackup"):
        entry = folders.get(name)
        check(isinstance(entry, dict) and isinstance(entry.get("under"), str)
              and isinstance(entry.get("parts"), list) and entry["parts"],
              "%s names a special folder and the parts under it" % name)
    revit = folders.get("RevitAddins") or {}
    backup = folders.get("InstallBackup") or {}
    # THE TWO PROMISES THESE FOLDERS CARRY, asked of what the code wrote:
    # per user and no administrator (docs/07 section 5), and the backup in
    # Heron's own folder, one per release per product (row 5b-96).
    check(revit.get("under") == "ApplicationData",
          "the add-ins go under the user's own roaming folder - no administrator")
    check(placeholders(revit.get("parts", [])) == ["release"],
          "one Addins folder per release")
    check(backup.get("under") == "LocalApplicationData"
          and (backup.get("parts") or [""])[0] == "Heron",
          "the backup is in Heron's own machine-local folder, never Autodesk's")
    check(placeholders(backup.get("parts", [])) == ["folder", "release"],
          "one backup per release per product")
    # REVIT'S ALL-USER FOLDERS, BOTH, for every release: ProgramData's for
    # 2020 to 2026 and Program Files' from 2027 (the revit-version-support
    # skill) - the per-user folder's own parts under another special folder.
    allusers = sorted((entry.get("under"), entry.get("parts"))
                      for entry in folders.values()
                      if isinstance(entry, dict)
                      and entry.get("from") == "HeronPaths.RevitAllUserAddins")
    check([under for under, _ in allusers] == ["CommonApplicationData", "ProgramFiles"],
          "Revit's all-user folders are ProgramData's (2020 to 2026) and Program "
          "Files' (2027 on), both kept (%s)" % ", ".join(u for u, _ in allusers))
    check(allusers and all(parts == revit.get("parts") for _, parts in allusers),
          "and each has the per-user folder's parts - one per release")

    spec = importlib.util.spec_from_file_location("bra_for_folders", RELEASE)
    bra = importlib.util.module_from_spec(spec)
    try:
        spec.loader.exec_module(bra)
        keep = getattr(bra, "WORKSPACE_KEEP", [])
    except Exception as exc:                                  # noqa: BLE001
        keep = []
        print("        (build-release-assets.py did not load: %s)" % exc)
    check(os.path.join("platform", "heron-folders.json") in keep,
          "heron-project.zip carries it beside the scripts that read it")
    print()


# --------------------------------------------------------------------- 5
#: Heron's own code - the parts tools/check-structure.py walks, and tools/.
CODE_PARTS = ("brain", "mcp", "platform", "revit", "tools")
SKIP_DIRS = {"bin", "obj", "build", ".vs", "__pycache__", "node_modules"}
CLIENT = os.path.join(ROOT, "mcp", "client")


def everything_else_asks():
    """Row 5b-1746: the Python and C# that show or look for the folder."""
    print("5. the Python and the C# ask the list too - row 5b-1746")
    # NO .py OR .cs OF HERON'S SPELLS EITHER FOLDER, HeronPaths.cs apart: it
    # is the owner. tools/heron-ready.py built the Addins folder from APPDATA,
    # the installer showed it as a literal, the bridge client printed it -
    # until 2026-11-16. Comments are history, and are not asked.
    words = re.compile(r"(?i)Revit\\+Addins|Revit/Addins"
                       r"|[\"']Revit[\"']\s*,\s*[\"']Addins[\"']|install-backup")
    spelled = []
    for part in CODE_PARTS:
        for dirpath, dirnames, filenames in os.walk(os.path.join(ROOT, part)):
            dirnames[:] = sorted(d for d in dirnames if d not in SKIP_DIRS)
            for name in sorted(filenames):
                if not name.endswith((".py", ".cs")):
                    continue
                path = os.path.join(dirpath, name)
                if os.path.abspath(path) == os.path.abspath(PATHS_CS):
                    continue
                text = read(path)
                code = cs_code(text) if name.endswith(".cs") else py_code(text)
                for line in code.split("\n"):
                    if words.search(line):
                        spelled.append("%s: %s" % (os.path.relpath(path, ROOT).replace(os.sep, "/"),
                                                   line.strip()[:120]))
    check(not spelled,
          "no .py or .cs file but HeronPaths.cs spells either folder in its CODE%s"
          % ("" if not spelled else " - %d line(s):\n          %s"
             % (len(spelled), "\n          ".join(spelled))))

    # THE PYTHON'S ONE READER, heron_bridge_client.heron_folder, asked here
    # with a stand-in for Windows' answer - the joining, the blanks and the
    # stops are Python and run anywhere. Windows' own answer is AB11's.
    if CLIENT not in sys.path:
        sys.path.insert(0, CLIENT)
    try:
        import heron_bridge_client as BRIDGE
    except Exception as exc:                                  # noqa: BLE001
        check(False, "mcp/client/heron_bridge_client.py loads (%s)" % exc)
        print()
        return
    reader = getattr(BRIDGE, "heron_folder", None)
    check(callable(reader), "the bridge client has heron_folder(), the list's Python reader")
    if not callable(reader):
        print()
        return
    committed = read(LIST)
    unders = sorted(set(e.get("under") for e in
                        (json.loads(committed).get("folders", {}).values() if committed else [])
                        if isinstance(e, dict)))
    known = sorted(getattr(BRIDGE, "SPECIAL_FOLDERS", {}))
    check(unders and set(unders) <= set(known),
          "it knows every special folder the list names (%s of %s)" % (unders, known))
    fake = lambda name: "C:\\Stand-in\\" + name                # noqa: E731
    got = None
    try:
        got = reader("RevitAddins", {"release": "2024"}, special=fake)
    except LookupError as exc:
        got = "LookupError: %s" % exc
    revit = (json.loads(committed).get("folders", {}).get("RevitAddins", {})
             if committed else {})
    want = "\\".join(["C:\\Stand-in\\" + str(revit.get("under"))]
                     + [p.replace("{release}", "2024") for p in revit.get("parts", [])])
    check(got == want, "it joins the list's parts under the folder Windows names (%s)" % got)

    def stops(*args, **kwargs):
        try:
            reader(*args, special=fake, **kwargs)
        except LookupError as exc:
            return str(exc)
        return None
    check(stops("RevitAddins", {"release": ".."}) is not None,
          "and refuses a blank that would step out of its folder")
    check("filled in" in (stops("RevitAddins") or ""),
          "and stops, by name, on a blank left unfilled")
    missing = os.path.join(tempfile.gettempdir(), "heron-no-such-folder-list.json")
    check("fetch them again" in (stops("RevitAddins", {"release": "2024"},
                                       list_path=missing) or ""),
          "and on a missing list, saying what to do about it")
    check("NoSuchFolder" in (stops("NoSuchFolder") or ""),
          "and on a folder the list has not got, by name")
    if os.name != "nt":
        try:
            reader("RevitAddins", {"release": "2024"})
            off = None
        except LookupError as exc:
            off = str(exc)
        check(off is not None and "not Windows" in off,
              "and off Windows Windows' own answer is refused by name, never guessed")
    print()


def main():
    tool = None
    try:
        tool = load_tool()
    except Exception as exc:                                  # noqa: BLE001
        print("  tools/heron-folders.py did not load: %s" % exc)

    the_code_owns_both()
    ran = the_list_is_the_code(tool)
    the_scripts_read_it()
    the_list_travels()
    everything_else_asks()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for f in FAILURES:
            print("  - %s" % f.split("\n")[0])
        return 1
    if not ran:
        print("NOT RUN - part 2 needs a .NET SDK and a runtime that meet. This is")
        print("exit 3, which is NOT a pass: the list was not compared with the code.")
        return COULD_NOT_RUN
    print("PASSED - the folder list is what HeronPaths says, and the scripts read it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
