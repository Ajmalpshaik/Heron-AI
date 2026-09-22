# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A supplied value that nothing read is NAMED, and a clean run stays silent.

    python tests/test_ignored_values.py

WHAT WENT WRONG
---------------
`RevitFragment.BindNeeds` walks the needs it EXPECTS and looks each one up in
what the caller supplied. It never walked the other way. So a supplied name
matching no need was read by nothing, reported by nothing, and the reply said
the whole thing had been applied - `categoryName` for `categories` is one
character wrong and a clean success.

It is the same silence that let a value split on a semicolon go out as a
success on 2026-09-22: walls asked to go red stayed white through four writes,
and every reply said it had worked.

WHY A TEST HOST AND NOT A PYTHON ASSERTION
------------------------------------------
The wording lives in C#, in `HeronIgnoredValues.cs`, and it is the ONLY thing
that now breaks that silence. `tests/Heron.IgnoredValues.TestHost` links it BY
SOURCE - the same pattern `Heron.BindingNote.TestHost` uses - so there is one
copy of the rule and no Revit is needed to prove it.

THE DOTNET-FINDING IS NOT REPEATED HERE. `tests/test_binding_note.py` already
worked out which target framework this machine can build, and a second copy
would be a second thing to keep in step. It is imported.

THE SEAM CHECK IS THE ONE THING THE HOST CANNOT MAKE. `Describe` takes three
collections of strings, so passing them in the wrong order COMPILES and the
message comes out confidently backwards - the same hazard `test_binding_note`
guards for its own two arguments. The production call is read here.
"""

import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HOST = os.path.join(ROOT, "tests", "Heron.IgnoredValues.TestHost")
CALLER = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")

sys.path.insert(0, os.path.join(ROOT, "tests"))
import test_binding_note as SHARED                              # noqa: E402


def crosses_the_seam():
    """The production call passes supplied, then consumed, then askable.

    All three are collections of strings. Swapped, this compiles and reports
    the names it DID read as the ones nothing read - a sentence that is wrong
    in the confident direction, which is exactly the shape row 75 had.
    """
    try:
        with io.open(CALLER, "r", encoding="utf-8") as fh:
            text = fh.read()
    except (IOError, OSError) as why:
        return False, "could not read %s: %s" % (CALLER, why)

    found = re.search(r"HeronIgnoredValues\.Describe\(([^)]*)\)", text)
    if not found:
        return False, "nothing in RevitFragment.cs calls HeronIgnoredValues.Describe"

    args = [a.strip() for a in found.group(1).split(",")]
    if args != ["supplied.Keys", "Consumed", "Askable"]:
        return False, ("the call passes %s - it must pass supplied.Keys, "
                       "Consumed, Askable in that order" % ", ".join(args))
    return True, "supplied.Keys, Consumed, Askable"


def main():
    tfm = SHARED._tfm()
    if tfm is None:
        print("NOT RUN - no .NET SDK this host can build with.")
        return 3

    out_dir = "bin/x64/Debug-%s/" % tfm
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024",
         "-p:HeronTfm=%s" % tfm, "-p:OutputPath=%s" % out_dir],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        print("FAILED - the test host did not build for %s." % tfm)
        said = (built.stdout or b"").decode("utf-8", "replace").strip()
        for line in said.splitlines()[-25:]:
            print("    %s" % line)
        return 1

    dll = os.path.join(HOST, out_dir, "Heron.IgnoredValues.TestHost.dll")
    if not os.path.exists(dll):
        print("FAILED - built, and %s is not there." % dll)
        return 1

    # The host prints its own checks; they are the output of this suite.
    ran = subprocess.call(["dotnet", dll])
    if ran != 0:
        return ran

    # LAST, so a failure here is never confused with the wording's own.
    ok, said = crosses_the_seam()
    print()
    print("  %s  the production call passes them in order - %s"
          % ("ok  " if ok else "FAIL", said))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
