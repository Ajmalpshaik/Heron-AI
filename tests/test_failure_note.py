#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A fragment that writes never leaves a Revit failure to Revit's own handling.

    python tests/test_failure_note.py

WHY THIS EXISTS
---------------
Until 2026-09-23 the fragment write path - RevitFragment.cs, the one every
write capability runs through - had NO failure handling. The move path,
RevitWrite.cs, always had: a preprocessor that dismisses and counts warnings
and rolls the job back on an error. Without one, every failure Revit posts at
commit goes to Revit's own handling, and Revit's handling of an ERROR is a
resolution - one of which can be to DELETE the elements the error names.
The earlier-brain plan recorded it as H4 and gave it to package C3.

TWO HALVES, BECAUSE ONE OF THEM CANNOT BE RUN HERE
---------------------------------------------------
  THE RULE      what is dismissed, what rolls back, and what the modeller is
                told - HeronFailureNote.cs, which names no Autodesk type and is
                linked by source into tests/Heron.FailureNote.TestHost. Built
                and RUN here, so its checks are real behaviour.
  THE WIRING    that every transaction the write path opens is put under the
                rule before anything runs in it, and that the answers carry
                what Revit said. That half calls the Revit API and cannot run
                off a Revit, so it is read as TEXT - a weak test of behaviour
                and a strong one of the defect, which was a missing line.

Neither half proves Revit then does what it is told. That is the PC's, on a
detached copy, and NEEDS-CHECKING says what it owes.

MEASURED AGAINST THE CODE AS IT STOOD, which is what makes this a test: with
RevitFragment.cs from before the change and no HeronFailureNote.cs, the
wiring checks fail and the host does not build - and this suite says so and
exits 1 rather than raising.

IT EXITS 3 WHEN .NET IS ABSENT AND THE WIRING PASSED, and that is NOT a pass:
the rule was not run.
"""

import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
NOTE = os.path.join(ADDIN, "HeronFailureNote.cs")
HOST = os.path.join(ROOT, "tests", "Heron.FailureNote.TestHost")
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


def code_only(text):
    """C# with its comments removed and its string contents blanked.

    A check that a call is present must not be satisfied by a comment that
    NAMES it - this file's own comments name every call it looks for - and a
    string that happens to contain `//` must not end the line early.
    Character by character, because a regex cannot tell a `//` inside a
    string from one that starts a comment.
    """
    out = []
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        two = text[i:i + 2]
        if two == "//":
            while i < n and text[i] != "\n":
                i += 1
            continue
        if two == "/*":
            end = text.find("*/", i + 2)
            i = n if end < 0 else end + 2
            out.append(" ")
            continue
        if c == "@" and i + 1 < n and text[i + 1] == '"':
            out.append('@"')
            i += 2
            while i < n:
                if text[i] == '"' and i + 1 < n and text[i + 1] == '"':
                    i += 2
                    continue
                if text[i] == '"':
                    break
                i += 1
            out.append('"')
            i += 1
            continue
        if c == '"':
            out.append('"')
            i += 1
            while i < n and text[i] != '"':
                i += 2 if text[i] == "\\" else 1
            out.append('"')
            i += 1
            continue
        if c == "'":
            end = i + 1
            while end < n and text[end] != "'":
                end += 2 if text[end] == "\\" else 1
            out.append("' '")
            i = end + 1
            continue
        out.append(c)
        i += 1
    return "".join(out)


def body_of(code, header):
    """The braces-balanced body that follows `header`, or '' when absent."""
    at = code.find(header)
    if at < 0:
        return ""
    start = code.find("{", at)
    if start < 0:
        return ""
    depth = 0
    for i in range(start, len(code)):
        if code[i] == "{":
            depth += 1
        elif code[i] == "}":
            depth -= 1
            if depth == 0:
                return code[start:i + 1]
    return ""


def wiring():
    """The half that reads RevitFragment.cs, and every other writer in revit/."""
    raw = read(EXECUTOR)
    check(raw is not None, "RevitFragment.cs is there to read")
    code = code_only(raw or "")

    print("\n1. every transaction the write path opens is put under the rule")
    opened = [m.start() for m in re.finditer(r"new\s+Transaction\s*\(", code)]
    check(len(opened) >= 2,
          "the write path opens %d transaction(s) - the fragment's and one per "
          "setup step" % len(opened))
    for at in opened:
        after = code[at:]
        runs = after.find("RunScript(")
        disciplined = after.find("Discipline(")
        where = code.count("\n", 0, at) + 1
        check(runs > 0 and 0 < disciplined < runs,
              "the transaction opened near code line %d is put under Discipline "
              "BEFORE its script runs" % where)
    calls = len(re.findall(r"(?<![\w.])Discipline\s*\(\s*\w", code)) - \
        len(re.findall(r"void\s+Discipline\s*\(", code))
    check(calls == len(opened),
          "one Discipline call per transaction: %d call(s), %d transaction(s)"
          % (calls, len(opened)))

    rule = body_of(code, "void Discipline(")
    check("SetFailuresPreprocessor(new FailureDiscipline(" in rule.replace(" ", "")
          or "SetFailuresPreprocessor(newFailureDiscipline(" in rule.replace(" ", ""),
          "Discipline sets Heron's preprocessor")
    check("SetClearAfterRollback(true)" in rule.replace(" ", ""),
          "and clears posted failures on a rollback, so no dialog follows one")
    check("SetFailureHandlingOptions(" in rule,
          "and hands the options back to the transaction - GetFailureHandlingOptions "
          "returns a COPY, so setting them and not handing them back does nothing")

    print("\n2. the preprocessor judges first, and never leaves anything to Revit")
    pre = body_of(code, "class FailureDiscipline")
    check(bool(pre), "the preprocessor is there")
    judge = pre.find("RollsBack(")
    dismiss = pre.find("DeleteWarning(")
    check(0 < judge < dismiss,
          "HeronFailureNote judges the whole batch BEFORE any warning is deleted")
    check("(int)FailureSeverity.Warning" in pre.replace(" ", ""),
          "and is handed Revit's own value for a warning, not a typed number")
    check("ProceedWithCommit" not in code,
          "nothing asks Revit to resolve and commit - that is Revit's own "
          "resolution, which this exists to keep out")
    returns = re.findall(r"return\s+FailureProcessingResult\.(\w+)", pre)
    check(returns.count("Continue") == 1 and set(returns) <= {"Continue",
                                                              "ProceedWithRollBack"},
          "it returns Continue once and otherwise rolls back: %s" % returns)
    check(pre.rfind("DeleteWarning(") < pre.find("FailureProcessingResult.Continue"),
          "and Continue comes only after every warning was deleted")

    print("\n3. what Revit said reaches the answer")
    refused = body_of(code, "string RefusedBy(")
    check("RefusedSentence()" in refused,
          "a refusal quotes what stopped it, in Revit's words")
    check(re.search(r"rolledBack\s*\?", refused) is not None,
          "and says the model is as it was ONLY when Revit confirmed the rollback")
    # READ IN THE RAW TEXT, because code_only blanks string contents and the
    # error code IS a string. What follows it must be the RefusedBy call, on
    # the same statement.
    for error in ("operation_failed", "setup_failed"):
        hits = [m.end() for m in re.finditer(r'Json\.Error\(\s*"%s",' % error, raw or "")]
        quoted = [at for at in hits
                  if re.match(r"\s*RefusedBy\(", (raw or "")[at:at + 80])]
        check(bool(quoted),
              "`%s` after a commit Revit would not keep carries RefusedBy" % error)
    verdict = body_of(code, "string WithVerdict(")
    check("DismissedSentence()" in verdict,
          "the verdict - the sentence the chat prints whole - says what was dismissed")
    check('"warnings"' in raw and '"warningsDismissed"' in raw,
          "and the reply carries the count and every dismissed kind")

    print("\n4. no writer in revit/ opens a transaction without a preprocessor")
    for folder, _, names in os.walk(os.path.join(ROOT, "revit")):
        if os.sep + "bin" in folder or os.sep + "obj" in folder:
            continue
        for name in sorted(names):
            if not name.endswith(".cs"):
                continue
            text = code_only(read(os.path.join(folder, name)) or "")
            if re.search(r"new\s+Transaction\s*\(", text):
                check("SetFailuresPreprocessor(" in text,
                      "%s opens a transaction and sets a failures preprocessor" % name)

    note = read(NOTE)
    check(note is not None, "HeronFailureNote.cs is there")
    check(note is not None and "Autodesk" not in code_only(note),
          "and names no Autodesk type, so a host can link it without Revit")


def _asked(what):
    try:
        out = subprocess.check_output(["dotnet", what], stderr=subprocess.STDOUT)
    except Exception:                                      # noqa: BLE001
        return None
    return out.decode("utf-8", "replace")


def _majors(said, prefix=None):
    out = []
    for line in (said or "").splitlines():
        parts = line.split()
        if prefix:
            if not line.startswith(prefix):
                continue
            parts = parts[1:]
        if not parts:
            continue
        try:
            out.append(int(parts[0].split(".")[0]))
        except ValueError:
            continue
    return sorted(set(out))


def _tfm():
    """The newest framework an installed SDK can build AND a runtime can run -
    tests/test_binding_note.py's rule, for the reasons it gives."""
    runtimes = _majors(_asked("--list-runtimes"), "Microsoft.NETCore.App")
    sdks = _majors(_asked("--list-sdks"))
    if not runtimes or not sdks:
        return None
    usable = [m for m in runtimes if m <= max(sdks)]
    return "net%d.0" % max(usable) if usable else None


def rule():
    """The half that runs: build the host and let it check the rule."""
    print("\n5. the rule itself, built and run without Revit")
    tfm = _tfm()
    if tfm is None:
        print("  NOT RUN - no framework here that an installed SDK can build AND")
        print("  a runtime can run. Linux: apt-get install -y dotnet-sdk-10.0")
        return COULD_NOT_RUN

    out_dir = "bin/x64/Debug-%s/" % tfm
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024", "-p:HeronTfm=%s" % tfm,
         "-p:OutputPath=%s" % out_dir, "--nologo", "-v", "q"],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        check(False, "the host builds for %s" % tfm)
        for line in (built.stdout or b"").decode("utf-8", "replace").splitlines()[-20:]:
            print("    %s" % line)
        return 1

    dll = os.path.join(HOST, out_dir, "Heron.FailureNote.TestHost.dll")
    ran = subprocess.run(["dotnet", dll], stdout=subprocess.PIPE,
                         stderr=subprocess.STDOUT)
    said = (ran.stdout or b"").decode("utf-8", "replace")
    for line in said.splitlines():
        print("  " + line)
    check(ran.returncode == 0, "the host's own checks pass (exit %d)" % ran.returncode)
    return 0


def main():
    wiring()
    host = rule()

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    if host == COULD_NOT_RUN:
        print("COULD NOT RUN - the wiring passed and the rule was not run. Exit 3,")
        print("which is NOT a pass.")
        return COULD_NOT_RUN
    print("PASS    every fragment-write transaction answers Revit's failures itself")
    return 0


if __name__ == "__main__":
    sys.exit(main())
