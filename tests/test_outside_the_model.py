#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A job that works only outside the model opens no transaction group in it (D-192).

    python tests/test_outside_the_model.py

WHY
---
Every write opened Heron's TransactionGroup in the model it was aimed at -
even a job that changes nothing there:

  A NEW FAMILY (row 5b-1912, issue #1121)   CREATE_FAMILY_DOCUMENT, run on
        2026-11-19 on Revit 2020 while a real project was pinned, opened a
        write in that project, a model nobody chose for the job.
  AN EXPORT OF FAMILIES (rows 21, 5b-1407, 5b-1411, issue #941)
        EXPORT_FAMILIES opens each family with EditFamily, which Revit refuses
        while a transaction is open in the project: every family came back
        "Revit refused to open it for editing", on Revit 2020 and 2024.

So a card may declare `noGroupOpen`: it works only outside the model it is
aimed at, and an Admin or Publish job then runs it with NO group open there
- measured as a read is, before and after, and refused loudly if that model
moved. This suite holds:

  * the need is the add-in's, bound true, and only an Admin or Publish job
    may declare it - a change to the model, or a card that also opens its
    own transactions, is refused before anything runs
  * the branch that runs it opens no TransactionGroup and no Transaction,
    counts the model before the run, hears Revit's commits in it, and is
    refused `changed_outside_group` if the model moved
  * its verdict says what is true: kept with no undo entry, or - from a
    proving tool, without apply - nothing rolled back and the outside work
    standing; and `ranOutsideTheModel` says which path it was
  * the server reads that verdict as KEPT or ROLLED BACK as before, the run
    record says the outside work stands, and the proof says "no transaction
    group", never "run inside a transaction"
  * the two cards declare it, and no card below Admin does

WHAT IT CANNOT SAY
------------------
That Revit opens a family for editing, or a new family's window, with no
group open; that nothing in the model is counted changed when it does; and
what the modeller sees. The C# is read as TEXT here: NEEDS-CHECKING Group OX.

MEASURED AGAINST THE CODE AS IT STOOD: every check fails, no traceback.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FRAGMENT = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")
CLIENT = os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py")
CARDS = os.path.join(ROOT, "brain", "fragments")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return ""


def body_of(text, signature):
    """A method's body, braces balanced - or "" when it is not there."""
    at = text.find(signature)
    if at < 0:
        return ""
    open_at = text.find("{", at)
    depth = 0
    index = open_at
    while 0 <= index < len(text):
        char = text[index]
        if char in "\"'":
            # A string or char literal: a brace inside one is not a brace.
            end = index + 1
            while end < len(text) and text[end] != char:
                end += 2 if text[end] == "\\" else 1
            index = end + 1
            continue
        if char == "/" and text[index:index + 2] == "//":
            index = text.find("\n", index)
            continue
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[open_at:index + 1]
        index += 1
    return ""


def code_only(text):
    return "\n".join(line for line in text.splitlines()
                     if not line.strip().startswith("//"))


def joined(text):
    """A C# sentence's pieces, joined - "a " + "b" read as "a b"."""
    return re.sub(r'"\s*\+\s*"', "", text)


def the_need(raw):
    print("\n1. the need, and who may declare it")
    check('private const string NoGroupNeed = "noGroupOpen";' in raw,
          "the add-in names the need noGroupOpen")
    check("var wantsNoGroup = DeclaresNeed(needs, NoGroupNeed);" in raw,
          "Run reads whether the card declares it")
    binder = raw[raw.find("if (name == NoGroupNeed)"):][:600]
    check("if (name == NoGroupNeed)" in raw and '.Append(" = true;\\n")' in binder,
          "the binder binds it true, as it binds noTransactionOpen")
    guard = raw[raw.find("if (wantsNoGroup && writing)"):][:1800]
    check("if (wantsNoGroup && writing)" in raw
          and '"run_fragment_admin"' in guard and '"run_fragment_publish"' in guard
          and "!outward || ownsTransactions" in guard
          and 'Json.Error("bad_contract"' in guard,
          "only an Admin or a Publish job may declare it, and never beside "
          "editScopeFailures - otherwise bad_contract, before anything runs")
    run = raw[raw.find("public static string Run("):]
    check(-1 < run.find("if (wantsNoGroup && writing)") < run.find("var early = bindAndCompile();"),
          "and that is refused before the binding and the compile")
    scan = raw[raw.find("var opens = HeronSourceScan.OpensTransaction(source);"):][:400]
    check("wantsNoGroup" in scan,
          "a card that declares it and opens a transaction is refused by the source scan")


def the_branch(raw):
    print("\n2. the branch opens no group, and measures the model")
    start = raw.find("else if (wantsNoGroup)")
    end = raw.find("// ONE TRANSACTION GROUP, so one Ctrl+Z", start)
    branch = code_only(raw[start:end]) if start > 0 and end > start else ""
    check(branch != "", "a branch of its own, before the group's")
    check(branch != "" and "new TransactionGroup(" not in branch
          and "new Transaction(" not in branch,
          "it opens no TransactionGroup and no Transaction")
    count = branch.find("counted = ReadFootprint.Of(target);")
    ran = branch.find("threw = RunScript(")
    check(-1 < count < ran, "the model is counted before the run")
    check("new RevitChangedSet(" in branch and "outsideChanges.Begin();" in branch
          and branch.find("outsideChanges.Begin();") < ran,
          "and Revit's commits in it are heard from before the run")
    check("OutsideChangedModel(name, target, counted, described, threw)" in branch
          and "target.IsModifiable" in branch,
          "after it: the model counted again, and a transaction left open refused")
    check("HeronRunClock.Tripped" in branch and "Guarded(threw," in branch
          and "HeronDialogNote.Outcome.OutsideOnly" in branch,
          "a stop, the run guard and a Revit dialog each refuse it, as on the read path")
    check("ranOutside = true;" in branch, "and the verdict is told which path ran")
    for name in ("KeepAndWatch(", "AskTheModeller(", "DeleteGate(", "RollBackAndCheck(",
                 "Assimilate("):
        check(name not in branch, "no %s - there is nothing in the model to keep or "
                                  "roll back" % name.rstrip("("))

    moved = joined(code_only(body_of(raw, "private static string OutsideChangedModel(")))
    check('Json.Error("changed_outside_group"' in moved
          and "described.Events" in moved and "after.Placed - before.Placed" in moved,
          "the model moving - by the count or by any commit in it - is "
          "changed_outside_group")
    check("Heron had no group to " in moved and "roll it back with" in moved,
          "and says nothing rolled it back")


def the_verdict(raw):
    print("\n3. the verdict says what is true")
    tail = raw[raw.find("if (!writing) return WithCount(answer, counted);"):][:400]
    check("if (ranOutside) return OutsideVerdict(" in tail,
          "a job run outside the model has its own verdict, not WithVerdict's")
    verdict = joined(body_of(raw, "private static string OutsideVerdict("))
    check('Json.Bool("ranOutsideTheModel", true)' in verdict,
          "it says so in a field every reader can ask")
    check('Json.Bool("rolledBack", !applied)' in verdict
          and 'Json.Bool("rollbackReported", false)' in verdict
          and 'Json.Bool("outsideStands", !applied)' in verdict,
          "no rollback is reported; without apply the model is as it was by count, "
          "and the outside work stands")
    check('kind = "kept";' in verdict and 'kind = "rolledBack";' in verdict,
          "kept with apply, the row 5b-1594 shape without")
    check("brief = null;" in verdict,
          "and only the kept run has a short form - one not kept is printed whole, as "
          "row 5b-407's rule asks of every rollback")
    check("UndoRule" not in verdict and "no undo entry" in verdict.lower()
          and "STILL STANDS" in verdict,
          "it never promises an undo step, and says the outside work stands")


def core_piece(function, names):
    """One function of heron_server_core.py and the words it uses, run alone -
    or None when either is missing, so the check fails rather than crashes."""
    import ast
    path = os.path.join(ROOT, "mcp", "server", "heron_server_core.py")
    body = []
    for node in ast.parse(read(path)).body:
        if isinstance(node, ast.Assign) and any(
                isinstance(t, ast.Name) and t.id in names for t in node.targets):
            body.append(node)
        elif isinstance(node, ast.FunctionDef) and node.name == function:
            body.append(node)
    space = {}
    try:
        exec(compile(ast.Module(body=body, type_ignores=[]), path, "exec"), space)
    except Exception as exc:                                  # noqa: BLE001
        print("  (could not run %s alone: %s)" % (function, exc))
        return None
    return space.get(function)


def the_readers():
    print("\n4. the server, the run record and the proof read it")
    try:
        import heron_run_records as runs
        import heron_failure as failures
    except Exception as exc:                                  # noqa: BLE001
        check(False, "the server modules import (%s)" % exc)
        return
    # `_change_outcome` is read out of heron_server_core.py and run alone:
    # importing that module needs the MCP SDK, and CI has none.
    outcome = core_piece("_change_outcome", ("KEPT", "ROLLED_BACK", "UNKNOWN"))
    kept = {"ok": True, "verdictKind": "kept", "applied": True, "rolledBack": False,
            "rollbackReported": False, "outsideStands": False, "ranOutsideTheModel": True}
    trial = dict(kept, verdictKind="rolledBack", applied=False, rolledBack=True,
                 outsideStands=True)
    check(outcome is not None and outcome(kept) == "KEPT",
          "a kept run outside the model is KEPT, as an Admin job was")
    check(outcome is not None and outcome(trial) == "ROLLED BACK",
          "a proving tool's run without apply is ROLLED BACK")
    ended = getattr(runs, "_ended", None)
    check(ended is not None and "outside the model still stands" in (ended(trial) or ""),
          "and its run record says what it did outside the model stands")
    row = getattr(failures, "_KNOWN", {}).get("changed_outside_group")
    check(row is not None and row[0] == failures.UNKNOWN
          and row[1] == failures.LOOK_AT_THE_MODEL,
          "changed_outside_group is OUTCOME UNKNOWN - look at the model - never a refusal")
    client = read(CLIENT)
    at = client.find("if reply.get(\"ranOutsideTheModel\") is True:")
    told = client[at:][:2000] if at >= 0 else ""
    check(at >= 0 and "NO transaction group open" in told
          and -1 < told.find("NO transaction group") < told.find("elif rolled is True:"),
          "a proof of it says no transaction group was open, before any "
          "'run inside a transaction' could be said")


def the_cards():
    print("\n5. the cards")
    import yaml
    declaring = {}
    for name in sorted(os.listdir(CARDS)):
        path = os.path.join(CARDS, name, "fragment.yaml")
        if not os.path.isfile(path):
            continue
        try:
            card = yaml.safe_load(read(path)) or {}
        except yaml.YAMLError:
            continue
        needs = ((card.get("contract") or {}).get("needs") or [])
        if any(isinstance(n, dict) and n.get("name") == "noGroupOpen" for n in needs):
            declaring[name] = (card.get("risk"), card.get("heron-status"), card.get("version"))
    for name, version in (("create-family-document", 4), ("export-families", 2)):
        got = declaring.get(name)
        check(got is not None and got[2] == version,
              "%s declares noGroupOpen, at version %d" % (name, version))
    wrong = sorted(n for n, (risk, _s, _v) in declaring.items()
                   if str(risk).upper() not in ("ADMIN", "PUBLISH"))
    check(declaring and not wrong,
          "every card that declares it is an Admin or a Publish job%s"
          % ("" if not wrong else " - NOT: " + ", ".join(wrong)))
    created = read(os.path.join(CARDS, "create-family-document", "fragment.yaml"))
    check("SINCE VERSION 4 (D-192)" in created,
          "create-family-document says the window now opens in the same call")


def main():
    raw = read(FRAGMENT)
    the_need(raw)
    the_branch(raw)
    the_verdict(raw)
    the_readers()
    the_cards()
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a job that works only outside the model opens no group in it (D-192)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
