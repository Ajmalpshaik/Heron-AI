#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A rollback never takes earlier kept work with it, silently.

    python tests/test_rollback_witness.py

WHY THIS EXISTS
---------------
A fragment's preview is the run itself, rolled back (D-55), so Heron rolls
back far more than it keeps. Section 1c of FRAGMENT-ISSUES holds three
rollbacks that did not hold while the reply said they had, and nothing at all
checked whether a rollback had also taken the elements the LAST KEPT job made.
Since 2026-10-17 every write job is watched: the last kept job's elements are
counted before it starts and looked for after its rollback, and what the job
itself added is counted back as a second witness beside Revit's status.

TWO HALVES, BECAUSE ONE OF THEM CANNOT BE RUN HERE
---------------------------------------------------
  THE RULE      what is watched, when a loss is blamed on a rollback and when
                it is not, and the words - HeronRollbackWitness.cs, which names
                no Autodesk type and is linked by source into
                tests/Heron.RollbackWitness.TestHost. Built and RUN here.
  THE WIRING    that EVERY rollback of a transaction group, in RevitFragment
                and in RevitWrite's move, goes through the one helper that
                checks; that every write job is watched from before its group
                starts; that the listener is subscribed for the add-in's whole
                life; and that the reply speaks from what was checked. That
                half calls the Revit API, so it is read as TEXT - a weak test
                of behaviour and a strong one of the defect this guards, which
                is a new early return that rolls back without looking.

Neither half proves Revit reports what the listener relies on. That is the
PC's, and NEEDS-CHECKING says what it owes.

MEASURED AGAINST THE CODE AS IT STOOD, which is what makes this a test: with
RevitFragment.cs, RevitWrite.cs and HeronApplication.cs from before the change
and no HeronRollbackWitness.cs, the wiring checks fail and the host does not
build - and this suite says so and exits 1 rather than raising.

IT EXITS 3 WHEN .NET IS ABSENT AND THE WIRING PASSED, and that is NOT a pass:
the rule was not run.
"""

import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.append(os.path.join(ROOT, "brain"))

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
MOVER = os.path.join(ADDIN, "RevitWrite.cs")
STARTUP = os.path.join(ADDIN, "HeronApplication.cs")
WITNESS = os.path.join(ADDIN, "HeronRollbackWitness.cs")
HOST = os.path.join(ROOT, "tests", "Heron.RollbackWitness.TestHost")
HOST_PROJECT = "tests/Heron.RollbackWitness.TestHost/Heron.RollbackWitness.TestHost.csproj"
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
    """C# with comments removed and string and char contents emptied.

    A check that a call is present must not be satisfied by a comment that
    names it - the comments in these files name every call this looks for -
    and a `//` or a brace inside a string must not be read as code. Walked a
    character at a time, because a regex cannot tell the two apart. Lengths
    are NOT kept; nothing here maps a position back to the raw text.
    """
    out = []
    i, n = 0, len(text)
    while i < n:
        two = text[i:i + 2]
        c = text[i]
        if two == "//":
            while i < n and text[i] != "\n":
                i += 1
        elif two == "/*":
            end = text.find("*/", i + 2)
            i = n if end < 0 else end + 2
            out.append(" ")
        elif c == "@" and text[i + 1:i + 2] == '"':
            i += 2
            while i < n:
                if text[i:i + 2] == '""':
                    i += 2
                elif text[i] == '"':
                    i += 1
                    break
                else:
                    i += 1
            out.append('""')
        elif c == '"' or c == "'":
            i += 1
            while i < n and text[i] != c:
                i += 2 if text[i] == "\\" else 1
            i += 1
            out.append(c + c)
        else:
            out.append(c)
            i += 1
    return "".join(out)


def block_after(code, at):
    """The braces-balanced block starting at the first `{` after `at`."""
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


def body_of(code, header):
    at = code.find(header)
    return "" if at < 0 else block_after(code, at)


def without(code, *bodies):
    """`code` with each named body cut out - the helpers' own insides."""
    for body in bodies:
        if body:
            code = code.replace(body, "{}", 1)
    return code


def statement_at(code, at):
    """From `at` to the `;` that ends the statement, parentheses counted."""
    depth = 0
    for i in range(at, len(code)):
        if code[i] in "([{":
            depth += 1
        elif code[i] in ")]}":
            depth -= 1
        elif code[i] == ";" and depth <= 0:
            return code[at:i + 1]
    return code[at:]


def previous_statements(code, at, count=2):
    """Up to `count` statements ending just before `at`, inside its block.

    Two, because a rollback's answer is often held in a variable and read
    into a second one - `var after = ...; var undone = after.Held;` - before
    the return that reports it.
    """
    taken = []
    end = at
    for _ in range(count):
        before = code[:end].rstrip()
        if not before.endswith(";"):
            break
        start = max(before.rfind(";", 0, len(before) - 1), before.rfind("{"), before.rfind("}"))
        taken.insert(0, before[start + 1:])
        if code[start] != ";":
            break
        end = start + 1
    return " ".join(taken)


def calls(code, name):
    """Positions of `name(` called - not declared - in `code`."""
    found = []
    for m in re.finditer(r"(?<![\w])%s\s*\(" % re.escape(name), code):
        head = code[max(0, m.start() - 80):m.start()]
        if re.search(r"(static|internal|private|public)[\w\s.<>,]*\s$", head):
            continue
        found.append(m.start())
    return found


# Returns inside a write job's group that do not roll it back, each with why.
# Matched against the statement as code_only leaves it. A NEW return that
# rolls nothing back must be added here with its reason, or this suite fails.
NOT_A_ROLLBACK = [
    (r"^return\s+null\s*;$",
     "RunSetupSteps: no step to run, or every step ran - nothing to roll back"),
    (r"^return\s+setupError\s*;$",
     "RunSetupSteps rolled the group back through the helper before answering"),
]
LEFT_OPEN = "IsModifiable"   # a transaction the fragment left open: Heron cannot roll it back at all


def returns_roll_back(code, body, where):
    """Every return in `body` rolls the group back through the helper."""
    for m in re.finditer(r"\breturn\b", body):
        statement = statement_at(body, m.start())
        flat = " ".join(statement.split())
        if any(re.match(pattern, flat) for pattern, _ in NOT_A_ROLLBACK):
            continue
        if LEFT_OPEN in body[max(0, m.start() - 120):m.start()]:
            continue
        line = code.count("\n", 0, code.find(body)) + body.count("\n", 0, m.start()) + 1
        check("RollBackAndCheck(" in statement
              or "RollBackAndCheck(" in previous_statements(body, m.start()),
              "%s: the return near code line %d rolls the group back through the "
              "helper - `%s`" % (where, line, flat[:70]))


def wiring():
    raw = read(EXECUTOR)
    raw_mover = read(MOVER)
    raw_startup = read(STARTUP)
    check(raw is not None and raw_mover is not None and raw_startup is not None,
          "RevitFragment.cs, RevitWrite.cs and HeronApplication.cs are there to read")
    code = code_only(raw or "")
    mover = code_only(raw_mover or "")
    startup = code_only(raw_startup or "")

    helper = body_of(code, "RollBackAndCheck(TransactionGroup")
    keep = body_of(code, "string Keep(TransactionGroup")
    group_rollback = body_of(code, "bool SafeRollBack(TransactionGroup")
    step_rollback = body_of(code, "void SafeRollBack(Transaction ")

    print("\n1. one helper rolls every transaction group back, and checks")
    check(bool(helper), "RevitFragment has the rollback-and-check helper")
    check("SafeRollBack(group)" in helper.replace(" ", "")
          and "Witness.RolledBack(" in helper,
          "and it rolls back, then asks the witness what that left")
    check(bool(group_rollback) and "TransactionStatus.RolledBack" in group_rollback,
          "SafeRollBack still answers Revit's own status after the attempt")
    for name, text, cut in (("RevitFragment.cs", code, (helper, group_rollback, step_rollback)),
                            ("RevitWrite.cs", mover, ())):
        rest = without(text, *cut)
        check(re.search(r"SafeRollBack\s*\(\s*group\b", rest) is None,
              "%s rolls no group back except through the helper" % name)
        check(re.search(r"\.\s*RollBack\s*\(", rest) is None,
              "%s calls RollBack() nowhere outside the two SafeRollBacks" % name)
        used = calls(text, "RollBackAndCheck")
        check(len(used) > 0, "%s calls the helper - %d time(s)" % (name, len(used)))
        for at in used:
            lead = re.sub(r"RevitFragment\s*\.\s*$", "", text[max(0, at - 60):at]).rstrip()
            check(lead.endswith(("=", "(", ",")) or lead.endswith("return"),
                  "%s: the helper's answer at code line %d is used, never dropped"
                  % (name, text.count("\n", 0, at) + 1))

    print("\n2. every write job is watched, from before its group starts")
    for name, text in (("RevitFragment.cs", code), ("RevitWrite.cs", mover)):
        groups = [m.start() for m in re.finditer(r"new\s+TransactionGroup\s*\(", text)]
        check(len(groups) > 0, "%s opens %d transaction group(s)" % (name, len(groups)))
        for at in groups:
            line = text.count("\n", 0, at) + 1
            check(re.search(r"using\s*\(\s*var\s+watch\s*=\s*(RevitFragment\s*\.\s*)?Watch\s*\("
                            r"[^;]*\)\s*\)\s*using\s*\(\s*var\s+group\s*=\s*$",
                            text[max(0, at - 160):at]) is not None,
                  "%s: the group opened near code line %d has a watch opened just "
                  "before it, disposed with it" % (name, line))
            returns_roll_back(text, block_after(text, at), "%s group" % name)
        assimilated = [m.start() for m in re.finditer(r"\.\s*Assimilate\s*\(", without(text, keep))]
        check(not assimilated,
              "%s assimilates a group only through Keep, which records what to watch" % name)
        check(re.search(r"\bKeep\s*\(\s*group\s*,", text) is not None,
              "%s keeps a job through Keep" % name)
    setup = body_of(code, "string RunSetupSteps(")
    signature = code[code.find("string RunSetupSteps("):code.find(setup) if setup else 0]
    check(bool(setup) and re.search(r"HeronRollbackWitness\.Watch\s+watch", signature) is not None,
          "the setup steps are handed the job's watch")
    returns_roll_back(code, setup, "RunSetupSteps")
    check("Assimilate()" in keep.replace(" ", "")
          and "TransactionStatus.Committed" in keep and "Witness.Assimilated(" in keep,
          "Keep records what to watch only when Revit says the group committed")

    print("\n3. the listener runs for the add-in's whole life, and holds no Document")
    on_startup = body_of(startup, "Result OnStartup(")
    on_shutdown = body_of(startup, "Result OnShutdown(")
    check("RevitFragment.Listen(" in on_startup.replace(" ", ""),
          "OnStartup subscribes the DocumentChanged listener")
    check("RevitFragment.StopListening(" in on_shutdown.replace(" ", ""),
          "and OnShutdown takes it off again")
    check("DocumentChanged+=OnDocumentChanged" in body_of(code, "void Listen(").replace(" ", "")
          and "DocumentChanged-=OnDocumentChanged" in body_of(code, "void StopListening(").replace(" ", ""),
          "the two are a matched pair on the same handler")
    handler = body_of(code, "void OnDocumentChanged(")
    flat = handler.replace(" ", "")
    for operation, reads in (("TransactionCommitted", "GetAddedElementIds()"),
                             ("TransactionGroupRolledBack", "GetDeletedElementIds()"),
                             ("TransactionUndone", "GetTransactionNames()"),
                             ("TransactionRedone", "GetTransactionNames()")):
        check(("UndoOperation." + operation) in flat and reads in flat,
              "the handler reads %s for %s" % (reads, operation))
    check(flat.lstrip("{").lstrip().startswith("try") and "catch(Exceptionex)" in flat,
          "and nothing escapes it - it runs inside Revit's own commit")
    check(0 <= flat.find("Witness.Any") < flat.find("e.Operation")
          and 0 <= flat.find("Witness.Armed") < flat.find("e.Operation"),
          "and it stops at once when nothing is kept and no job is armed")
    check(re.search(r"\bstatic\s+(readonly\s+)?Document\s+\w+\s*[;=]", code) is None
          and re.search(r"\bstatic\s+(readonly\s+)?Document\s+\w+\s*[;=]", mover) is None,
          "no static field holds a Document - the sample is keyed by identity strings")
    key = body_of(code, "string WitnessKey(")
    check(all(part in key for part in ("CreationGuid(", "PathName", "Title")),
          "the key is the model's own id, its path and its title")

    print("\n4. the reply speaks from what was checked")
    verdict = body_of(code, "string WithVerdict(")
    check(re.search(r"held\s*=\s*rolledBack\s*!=\s*null\s*&&\s*rolledBack\.Held", verdict) is not None,
          "`rolledBack` on the reply means the rollback HELD, as far as it was checked")
    check('Json.Bool("rolledBack", held)' in (raw or "")
          and 'Json.Bool("rollbackReported"' in (raw or "")
          and '"rollbackCheck' in (raw or ""),
          "beside Revit's own status and the counts behind both")
    check(".Also()" in verdict and "keptNote" in verdict,
          "the verdict carries the loss, and what the start of the job found")
    check(".Account()" in body_of(code, "string RefusedBy("),
          "a refusal after Revit would not keep a write says what the rollback left")
    check("and the model is as it was" not in (raw or ""),
          "no setup refusal says the model is as it was without having looked")
    for name, text in (("RevitFragment.cs", raw or ""), ("RevitWrite.cs", raw_mover or "")):
        check("rolled back completely" not in text
              or re.search(r"rolledBack\s*\?\s*\"The move failed and was rolled back completely",
                           text) is not None,
              "%s says \"rolled back completely\" only on the checked answer" % name)

    print("\n5. the rule lives in one Revit-free file, built for every release")
    witness = read(WITNESS)
    check(witness is not None, "HeronRollbackWitness.cs is there")
    check(witness is not None and "Autodesk" not in code_only(witness),
          "and names no Autodesk type, so a host can link it without Revit")
    try:
        import heron_dotnet as DOTNET                               # noqa: E402
        listed = HOST_PROJECT in DOTNET.PROJECTS
    except Exception as failure:                                    # noqa: BLE001
        print("  (heron_dotnet could not be read: %s)" % failure)
        listed = False
    check(listed, "the host is in heron_dotnet.PROJECTS, so check-compile builds it on all eight")


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
    print("\n6. the rule itself, built and run without Revit")
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

    dll = os.path.join(HOST, out_dir, "Heron.RollbackWitness.TestHost.dll")
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
    print("PASS    every rollback is checked, and a loss of kept work is said")
    return 0


if __name__ == "__main__":
    sys.exit(main())
