#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The one command a new user runs, checked on a machine that cannot run it.

    python tests/test_setup_script.py

`tools/setup.ps1` is the LAST tool in `tools/` that no suite names at all,
measured 2026-09-22 against every file in `tests/`. It is also the first
thing anybody ever runs: it detects the installed Revits, builds for each,
deploys, and then PRINTS THE INSTRUCTIONS the user follows next.

IT CANNOT BE RUN HERE AND THIS SUITE DOES NOT PRETEND TO. PowerShell is not
on this container and the script builds C# against a Revit that is not here.
So this asks only the half that is answerable WITHOUT Windows: does what the
script says agree with what the repository does? That is not a small half -
every defect a user meets in the first five minutes lives in exactly that
gap.

WHAT IT DERIVES RATHER THAN TYPES
---------------------------------
THE RIBBON PATH IS READ OUT OF THE ADD-IN'S OWN CONSTANTS, never typed here.
`HeronApplication.cs` carries the reason in a comment beside them:

    These two strings are the ribbon path users are TOLD, so anything
    that prints it - the MCP server, the health report, the deploy
    script - has to agree with them. "Heron > AI Bridge > Heron".

D-85 renamed that tab on 2026-09-20 and moved eight printed strings with it,
and its consequences section says what happens to a ninth that is missed: *a
tab renamed without them is a tool that tells the user to press a button that
no longer exists.* `setup.ps1` was the ninth. It sent every new user to a tab
called `Heron AI`, which D-85 had renamed to `Heron` - measured against the
add-in constants, the product manifest, `test_ribbon_tab_sharing.py` and the
one real-Revit observation in `NEEDS-CHECKING`, all four agreeing.

So this check is written to hold for the NEXT rename as well: it compares the
script against the constants, and a tab renamed in the add-in fails here
until the script follows.

ITS PYTHON CHECK ASKS FOR EVERY REQUIRED PACKAGE, NOT ONLY `mcp` (row
5b-201). Until 2026-10-09 `Get-PythonStatus` ran `import mcp` and nothing
else, and setup printed a pass line on that alone - while `requirements.txt`
calls PyYAML the one REQUIRED package and installing `mcp` does not bring it
(measured 2026-09-24). The required list is read out of `requirements.txt`
by `tools/check-dependencies.py`'s own reader, never typed here, so a package
added there fails this suite until the setup check asks for it too.

WHAT IT DOES NOT ASK. Whether the script WORKS - that needs Windows, a .NET
SDK and a Revit, and it is `Z1` and its neighbours in `NEEDS-CHECKING.md`.
`test_ribbon_tab_sharing.py` owns whether the three add-ins may share one
tab; this owns only what the installer tells a person to do.
"""

import importlib.util
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SETUP = os.path.join(ROOT, "tools", "setup.ps1")
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronApplication.cs")
HELPERS = os.path.join(ROOT, "tools", "HeronRevit.ps1")
REQUIREMENTS = os.path.join(ROOT, "requirements.txt")
DEPENDENCIES = os.path.join(ROOT, "tools", "check-dependencies.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def text(path):
    if not os.path.exists(path):
        return None
    return io.open(path, encoding="utf-8", errors="replace").read()


def constant(body, name):
    """The value of `private const string <name> = "...";` - the house reader."""
    if body is None:
        return None
    m = re.search(r'const\s+string\s+%s\s*=\s*"([^"]*)"' % re.escape(name), body)
    return m.group(1) if m else None


def supported():
    """The Revit releases Heron builds for, read from the module that owns it.

    Never typed here. AJ Tools' L3 was a hardcoded version list left behind
    after per-version builds landed, and the installer silently installed
    nothing on three releases while the document advertised them.
    """
    try:
        sys.path.insert(0, os.path.join(ROOT, "brain"))
        import heron_dotnet                                  # noqa: E402
        return [str(r) for r in heron_dotnet.RELEASES]
    except BaseException:                                    # noqa: BLE001
        return None


def required_imports():
    """The import names requirements.txt calls REQUIRED, or None.

    Read by tools/check-dependencies.py's own manifest reader - the file's
    format has one reader, and the list is never typed here (row 5b-201).
    """
    try:
        spec = importlib.util.spec_from_file_location("check_dependencies",
                                                      DEPENDENCIES)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        packages, problems = module.read_manifest(module.REQUIRED)
    except BaseException:                                    # noqa: BLE001
        return None
    if problems or not packages:
        return None
    return [p.module for p in packages]


def function_body(body, name):
    """The text of `function <name>` up to the next function, or None."""
    m = re.search(r"(?ms)^function\s+%s\b(.*?)(?=^function\s|\Z)"
                  % re.escape(name), body or "")
    return m.group(1) if m else None


# One import the setup check runs and the flag it records the answer in:
#     & $candidate -c "import mcp" 2>$null | Out-Null
#     $result.HasMcp = ($LASTEXITCODE -eq 0)
PROBE = re.compile(r'-c\s+"import\s+([\w.]+)"[^\n]*\n\s*'
                   r'\$result\.(Has\w+)\s*=\s*\(\$LASTEXITCODE\s+-eq\s+0\)')


def pass_condition(body):
    """The `if (...)` guarding setup's Python pass line, or None.

    The pass line is the `Write-Ok` that prints the Python version; its
    guard is the nearest `if` STATEMENT above it - not the `$scope = if`
    expression that sits between them.
    """
    lines = body.splitlines()
    at = next((i for i, line in enumerate(lines)
               if "Write-Ok" in line and "$python.Version" in line), None)
    if at is None:
        return None
    for line in reversed(lines[:at]):
        stripped = line.strip()
        if stripped.startswith(("if ", "if(")) and "$python." in stripped:
            return stripped
    return None


def called(body):
    """Every `Verb-Noun` the script calls that looks like one of ours."""
    return set(re.findall(r"\b((?:Get|Write|Format|Test|New)-[A-Z][A-Za-z]+)\b", body))


def defined(body):
    return set(re.findall(r"^function\s+([A-Za-z]+-[A-Za-z]+)", body, re.M))


# What the script tells a user to type, read off the script rather than
# listed here - a path that stops existing must fail, and a path that is
# ADDED must be checked too, which a typed list would quietly skip.
TYPED_PATH = re.compile(r"(?:python|dotnet build|& )\s+([A-Za-z][\w.\\/-]*[\w])")


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    body = text(SETUP)
    addin = text(ADDIN)
    helpers = text(HELPERS)
    check(body is not None, "tools/setup.ps1 is there")
    check(addin is not None, "and the add-in that builds the ribbon is there")
    check(helpers is not None, "and tools/HeronRevit.ps1, which it dot-sources")
    if body is None or addin is None or helpers is None:
        print()
        print("FAILED - a file this suite compares could not be read")
        return 1

    print()
    print("1. THE RIBBON PATH IT PRINTS IS THE ONE THE ADD-IN BUILDS")
    print("   Derived from the add-in's constants, never typed in this file.")
    tab = constant(addin, "TabName")
    panel = constant(addin, "PanelName")
    check(tab is not None and panel is not None,
          "the add-in names its tab and panel as constants (%r, %r)" % (tab, panel))
    if tab and panel:
        # The script writes it with ASCII arrows; the add-in comment uses '>'.
        printed = [line.strip() for line in body.splitlines() if "Ribbon" in line]
        check(len(printed) == 1,
              "exactly one line prints the ribbon path, and %d do" % len(printed))
        said = printed[0] if printed else ""
        # The steps AFTER the word Ribbon, in order. Comparing the whole
        # string would pass on "Heron AI -> Heron" the moment the tab is
        # named "Heron", because that name is a prefix of the old one - which
        # is the defect this case exists for.
        tail = said.split("Ribbon", 1)[-1]
        tail = tail.split("(")[0]
        steps = [bit.strip().strip('"').strip()
                 for bit in tail.split("->") if bit.strip().strip('"').strip()]
        check(steps[:2] == [tab, panel],
              "the first two steps are the tab then the panel %r, and they are %r"
              % ([tab, panel], steps[:2]))
        check(len(steps) == 3 and steps[2],
              "and a third step names the button to press, not %r" % (steps,))

    print()
    print("2. Every helper it calls is defined where it dot-sources from")
    check('Join-Path $PSScriptRoot "HeronRevit.ps1"' in body,
          "it dot-sources tools/HeronRevit.ps1")
    ours = defined(helpers) | defined(body)
    missing = sorted(f for f in called(body)
                     if f.split("-")[1].startswith(("Python", "Running", "Revit"))
                     and f not in ours)
    check(not missing,
          "no helper is called that nothing defines, and it calls %r" % (missing,))

    print()
    print("3. Every path it tells a user to type is in the repository")
    told = set()
    for line in body.splitlines():
        if "Write-Host" not in line and "Join-Path $repoRoot" not in line:
            continue
        for hit in TYPED_PATH.findall(line):
            if ("\\" in hit or "/" in hit) and not hit.startswith("http"):
                told.add(hit.replace("\\", "/").strip('"'))
    check(len(told) >= 3,
          "it names at least three paths, and it named %d: %s"
          % (len(told), ", ".join(sorted(told))))
    gone = sorted(p for p in told if not os.path.exists(os.path.join(ROOT, p)))
    check(not gone, "every one of them exists, and these do not: %r" % (gone,))

    print()
    print("4. It discovers the Revits rather than carrying a list of years")
    print("   AJ Tools' L3: a hardcoded version list installed nothing on three")
    print("   releases while the document advertised them.")
    check(re.search(r"Get-ChildItem[^\n]*\$autodesk", body) is not None
          or "Get-ChildItem -Path $autodesk" in body,
          "it lists what is installed under Autodesk")
    check(re.search(r"'\^Revit \(\\d\{4\}\)\$'", body) is not None,
          "and matches any four-digit year rather than named ones")
    # A year in a comment or in an example a user reads is fine and often
    # necessary. A year in EXECUTABLE code is a list that goes stale, so the
    # help block, the comments and the printed strings come out first.
    code = re.sub(r"(?s)<#.*?#>", "", body)
    code = re.sub(r'"[^"\n]*"', '""', code)
    code = re.sub(r"(?m)#.*$", "", code)
    inline = sorted(set(re.findall(r"\b(20[123]\d)\b", code)))
    check(not inline,
          "no year survives into code, and these do: %r" % (inline,))
    # THE YEARS IT PRINTS ARE STILL PROMISES. One that the repository has
    # stopped building for is an instruction that fails on the user's first
    # command after a successful install.
    shown = sorted(set(re.findall(r"RevitVersion[ =]+(20[123]\d)", body)))
    releases = supported()
    check(releases is not None and len(releases) > 1,
          "the supported list is derived from brain/heron_dotnet, not typed")
    unbuilt = sorted(y for y in shown if releases and y not in releases)
    check(not unbuilt,
          "every year it shows a user is one Heron builds for, and these are "
          "not: %r" % (unbuilt,))

    print()
    print("5. A deploy that fails is a failure, not a success")
    print("   A PowerShell script does not set $LASTEXITCODE, so the old check")
    print("   read the dotnet build's - always 0 - and a failed deploy counted")
    print("   as installed.")
    after = body.split("deploy-addin.ps1")[-1]
    check("catch" in after.split("$succeeded +=")[0],
          "the deploy call is wrapped in try/catch")
    check(re.search(r"catch\s*\{[^}]*\$failed \+=", after, re.S) is not None,
          "and the catch records it as failed")
    check("$succeeded.Count -eq 0 -and $skipped.Count -gt 0" in body,
          "and nothing installed because every release was open exits non-zero")

    print()
    print("6. Its Python check asks for every REQUIRED package, not only mcp")
    print("   Row 5b-201: installing mcp does not bring PyYAML, and setup passed")
    print("   a PC that had mcp alone.")
    required = required_imports()
    check(required is not None,
          "the required packages are read from requirements.txt by "
          "check-dependencies' own reader, not typed here: %r" % (required,))
    required = required or []
    status = function_body(helpers, "Get-PythonStatus")
    advice = function_body(helpers, "Write-PythonAdvice")
    check(status is not None and advice is not None,
          "HeronRevit.ps1 defines Get-PythonStatus and Write-PythonAdvice")
    status = status or ""
    advice = advice or ""
    probes = dict(PROBE.findall(status))
    check("mcp" in probes,
          "Get-PythonStatus still asks for the MCP package the server runs on, "
          "and it asks for %r" % (sorted(probes),))
    guard = pass_condition(body) or ""
    check(bool(guard), "setup.ps1 guards its Python pass line with one `if`")
    for name in sorted(set(required) | {"mcp"}):
        flag = probes.get(name)
        if name in required:
            check(flag is not None,
                  "Get-PythonStatus imports %r, which requirements.txt calls "
                  "required, and records the answer - it imports %r"
                  % (name, sorted(probes)))
        check(flag is not None and ("$python.%s" % flag) in guard,
              "setup.ps1 prints its pass line only when %r imports too - "
              "the guard is %r" % (name, guard))
        check(flag is not None and ("-not $Python.%s" % flag) in advice,
              "Write-PythonAdvice speaks up when %r is missing rather than "
              "passing it" % name)
    # The install line is the one requirements.txt prints at its own head,
    # read from there - so the two cannot drift apart.
    manifest = text(REQUIREMENTS) or ""
    found = re.search(r"pip install[^\n]*-r\s+requirements\.txt", manifest)
    check(found is not None,
          "requirements.txt names its own install line at its head")
    line = found.group(0) if found else "pip install --user -r requirements.txt"
    check(line in advice,
          "Write-PythonAdvice hands the user that same line, %r" % line)
    check("pip install --user mcp" in advice,
          "and still hands the user the MCP package's line")

    print()
    print("7. It carries the five-field header docs/29 asks of every source file")
    head = body.splitlines()[:8]
    for field in ("Heron-Agent", "Heron-Step", "Heron-Status", "Heron-Since",
                  "Heron-Layer"):
        check(any(line.startswith("# %s:" % field) for line in head),
              "it declares %s" % field)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - what the installer tells a user to do is what the")
    print("repository actually does.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
