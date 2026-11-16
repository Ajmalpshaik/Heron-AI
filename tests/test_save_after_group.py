#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Heron saves the model it works in - after its Undo entry has closed, from the
add-in, only when the modeller asks, and never under a name Heron chose.
OPEN-QUESTIONS Q-69, Shape 2, accepted by Ajmal PS on 2026-11-16. Runs without
Revit.

    python tests/test_save_after_group.py

WHY IT IS SHAPED THIS WAY. Revit saves nothing while a transaction group the
API opened is still open, and every change Heron makes runs inside ONE group
for its one Undo entry (Article 8). So a fragment can never save the model it
runs in. Two whole builds were lost unsaved (row 5b-513). The add-in does the
save itself, once the group has closed, and only when:

  * the fragment ASKED for it - SAVE_DOCUMENT version 3, through the ambient
    need `saveWhenKept`, given the model's own file or a path the modeller typed;
  * the group was KEPT. A preview, a Discard or a proof's rolled-back run never
    saves: a save cannot be rolled back (D-55, the owner's point 2);
  * the job is a Publish job and the switches still say so when it saves.

A Save As renames the model, and on Revit 2020 to 2023 - no CreationGUID - and
for every family the pin holds only its title. So the reply names the model as
the change was made, and the chat's pin follows the new name ONLY because the
reply says this run did the Save As of that model (heron_write.after_save).

Nothing here syncs (Article 10): a save of a workshared local copy is not a
sync, and it says so.

WHAT THIS DOES NOT PROVE: that Revit saves from the ExternalEvent callback at
all, what dialogs it raises, how long a big model takes, or that the file on
disk moves. That is NEEDS-CHECKING Group EN, on a real Revit.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_write as WRITE                                 # noqa: E402
from heron_write import DocumentPin                         # noqa: E402
import heron_tools as TOOLS                                 # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
RUN = os.path.join(ADDIN, "RevitFragment.cs")
SAVE = os.path.join(ADDIN, "RevitFragmentSave.cs")
CARD = os.path.join(ROOT, "brain", "fragments", "save-document")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    """The file's text, or "" when it is not there - so a missing file is a
    set of failures, never a traceback."""
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return ""


def code(text):
    """C# with its comments and string literals blanked, so a call named in a
    comment or a sentence is never read as a call."""
    text = re.sub(r"/\*.*?\*/", " ", text, flags=re.S)
    text = re.sub(r'@"(?:[^"]|"")*"', '""', text)
    text = re.sub(r'"(?:\\.|[^"\\\n])*"', '""', text)
    return re.sub(r"//[^\n]*", "", text)


def at(text, needle):
    """Where `needle` is, or -1 - asked by name so an ordering check can never
    pass on two absences (heron-ship 2a: str.find returns -1)."""
    return text.find(needle) if needle else -1


def card_section():
    print("SAVE_DOCUMENT version 3 - the card")
    yaml_text = read(os.path.join(CARD, "fragment.yaml"))
    source = read(os.path.join(CARD, "impl", "any", "fragment.cs"))
    cases = read(os.path.join(CARD, "tests", "cases.yaml"))
    try:
        import yaml
        card = yaml.safe_load(yaml_text) or {}
    except Exception:                                   # noqa: BLE001 - reported
        card = {}
    check(card.get("version") == 3, "the card is version 3")
    check(card.get("heron-status") == "DRAFT",
          "and DRAFT: its C# moved, so version 2's proof no longer describes it "
          "(D-30), and a kept change from it is refused until it is proved (D-117)")
    check(card.get("risk") == "PUBLISH", "still behind the Publish switch")
    check(isinstance(card.get("proof"), dict) and card["proof"].get("date") == "2026-10-07",
          "version 2's proof block is kept as the record of what it proved")

    contract = card.get("contract") or {}
    needs = dict((n.get("name"), n) for n in contract.get("needs") or [])
    provides = dict((n.get("name"), n) for n in contract.get("provides") or [])
    ask = needs.get("saveWhenKept")
    check(ask is not None and ask.get("source") == "ambient"
          and ask.get("type") == "Action<string, bool>",
          "it declares the ambient need the add-in saves through, typed as the "
          "add-in binds it")
    for name in ("saveAsPath", "replaceFile", "documentPath"):
        need = needs.get(name) or {}
        check(need.get("source") == "request" and str(need.get("optional")).lower() == "true",
              "'%s' comes from the request and may be left out" % name)
    check((needs.get("replaceFile") or {}).get("type") == "bool",
          "replaceFile is a bool - left out is false, Revit's own default")
    asked = provides.get("saveAsked") or {}
    check(asked.get("role") == "result" and asked.get("type") == "bool",
          "saveAsked is a result: what the run asked for is the thing a proof judges")

    bare = code(source)
    check("saveWhenKept(" in bare, "the fragment asks for the save through the need")
    check(re.search(r"\bdoc\s*\.\s*Save(As)?\s*\(", bare) is None,
          "and never saves the document it works in itself - Revit refuses that "
          "while Heron's group is open")
    check(re.search(r"\.\s*SaveAs\s*\(", bare) is None,
          "nor calls SaveAs on anything")
    check(re.search(r"SynchronizeWithCentral\s*\(", bare) is None,
          "nor synchronises (Article 10)")
    # A special folder (the user's documents, the desktop) is held by
    # tools/check-structure.py for every file outside the path manager, so it
    # is not named again here.
    for maker in ("Path.Combine(", "Path.ChangeExtension(", "GetTempPath(",
                  "GetRandomFileName("):
        check(maker not in bare, "it builds no file name with %s - Heron never chooses "
                                 "one" % maker.rstrip("("))
    check("saveWhenKept(saveAs" in bare,
          "a Save As goes to the path the modeller typed, handed over as it came")
    check("rolled back" in cases and "NOT saved" in cases,
          "its cases say a rolled-back run saves nothing")


def addin_section():
    print()
    print("The add-in - the save after the group, only when kept")
    run = read(RUN)
    save = read(SAVE)
    run_code = code(run)
    save_code = code(save)
    check(bool(save), "RevitFragmentSave.cs exists - the save in a file of its own")
    check('const string SaveWhenKeptNeed = "saveWhenKept";' in run,
          "RevitFragment.cs names the ambient need beside the other three")
    binding = run_code[at(run_code, "if (name == SaveWhenKeptNeed)"):]
    binding = binding[:at(binding, "continue;") + 1] if binding else ""
    check("needs_write_path" in run[at(run, "if (name == SaveWhenKeptNeed)"):
                                    at(run, "if (name == SaveWhenKeptNeed)") + 1600],
          "the need is refused on the read path - a read has no group to close "
          "and nothing to keep")
    check("SaveAsk.Bind(" in binding, "and bound to the add-in's own record of the ask")

    group_at = at(run_code, "new TransactionGroup(target, label)")
    kept_at = at(run_code, "groupKept = apply && group.GetStatus() == TransactionStatus.Committed")
    threw_at = at(run_code, "if (threw != null) return threw;")
    call_at = at(run_code, "SaveAfterTheGroup(")
    check(group_at >= 0 and kept_at > group_at and threw_at > kept_at,
          "whether the group was KEPT is read inside it, from Revit's status - "
          "not from the request's apply")
    check(call_at > threw_at and threw_at >= 0,
          "the save is asked for AFTER the group's block has closed and every "
          "refusal has returned")
    tail = run_code[call_at:call_at + 400] if call_at >= 0 else ""
    check("groupKept" in tail and "sendsOut" in tail,
          "and is handed whether the group was kept and whether this is a Publish job")
    check(at(run_code, "WithVerdict(answer") >= 0 and call_at > at(run_code, "WithVerdict(answer"),
          "after the reply is built - so the reply names the model as the change "
          "was made, and the save's own block says what the save did")

    first_save = min([i for i in (at(save_code, ".Save("), at(save_code, ".SaveAs("))
                      if i >= 0] or [-1])
    check(first_save >= 0, "the add-in's code saves through Document.Save or SaveAs")
    for guard, what in (("if (ask == null || !ask.Asked) return reply;",
                         "nothing at all unless the fragment asked"),
                        ("if (!kept)", "nothing unless the group was kept"),
                        ("if (!sendsOut)", "nothing unless the job is a Publish job"),
                        ("HeronRunPolicy.Withdrawn(risk, HeronConfig.Load()",
                         "and the switches are read again just before the save")):
        where = at(save_code, guard)
        check(0 <= where < first_save, "%s - checked before any save" % what)
    check(re.search(r"SynchronizeWithCentral\s*\(", save_code) is None,
          "the add-in's save never synchronises (Article 10)")
    sets = re.findall(r"OverwriteExistingFile\s*=\s*([^;]+);", save_code)
    check(sets == ["true"] and re.search(
        r"if\s*\(\s*ask\.Replace\s*\)\s*options\.OverwriteExistingFile\s*=\s*true;",
        save_code) is not None,
          "a file is replaced only when the modeller said so - Revit's default, "
          "false, otherwise")
    for maker in ("Path.Combine(", "Path.ChangeExtension(", "GetTempPath(",
                  "GetRandomFileName("):
        check(maker not in save_code, "the add-in builds no file name with %s"
                                      % maker.rstrip("("))
    saveas = save_code[at(save_code, "private static string SaveAsNow("):]
    check("IsWorkshared" in saveas and "PathOf(target)" in saveas,
          "a Save As refuses a workshared model and one that already has a file")
    check("cannot be undone" in save.lower() or "CANNOT BE UNDONE" in save,
          "a save that was done says it cannot be undone")
    check("never saves" in save, "a rolled-back run says it never saves")
    check("NOTHING WENT TO CENTRAL" in save,
          "a workshared local save says nothing went to central")
    check('Json.Str("creationGuid"' in save and 'Json.Str("documentPath"' in save,
          "a Save As reports the model's new identity, for the pin to follow")


def server_section():
    print()
    print("The chat's half - the reply, and the pin following a Save As")
    after = getattr(WRITE, "after_save", None)
    check(after is not None, "heron_write has after_save at all")
    if after is None:
        after = lambda pin, reply: (reply, [])      # so the checks below RUN and FAIL

    pin = DocumentPin()
    plain = {"ok": True, "document": "Project1", "documentPath": r"C:\a\Project1.rvt"}
    pin.check(plain)
    reply, lines = after(pin, plain)
    check(reply == plain and lines == [], "a reply with no save says nothing about one")

    said = ("SAVED. 'Project1' was saved to C:\\a\\Project1.rvt once Heron's Undo entry "
            "had closed. A SAVE CANNOT BE UNDONE.")
    done = dict(plain, save={"asked": True, "kind": "save", "done": True, "said": said})
    reply, lines = after(pin, done)
    check(any(said in line for line in lines), "a kept save is said in the add-in's words")
    check(pin.title == "Project1", "and a plain Save leaves the pin where it was")

    bare = dict(plain, save={"asked": True, "kind": "save", "done": True})
    _, lines = after(pin, bare)
    check(any("cannot be undone" in line.lower() for line in lines),
          "a save the add-in reports without its sentence still says it cannot be undone")

    discarded = dict(plain, save={"asked": True, "kind": "save", "done": False,
                                  "said": "NOT SAVED. You pressed Discard."})
    _, lines = after(pin, discarded)
    check(any("NOT SAVED" in line for line in lines), "a Discard says nothing was saved")

    unsaved = {"ok": True, "document": "Project1", "documentPath": None}
    pin = DocumentPin()
    pin.check(unsaved)
    saved_as = dict(unsaved, save={"asked": True, "kind": "saveAs", "done": True,
                                   "said": "SAVED AS.", "document": "Shell A",
                                   "documentPath": r"C:\jobs\Shell A.rvt"})
    reply, lines = after(pin, saved_as)
    check(pin.title == "Shell A", "a Save As of the pinned model moves the pin")
    check(reply.get("document") == "Shell A"
          and reply.get("documentPath") == r"C:\jobs\Shell A.rvt",
          "and the reply read after it names the model by its new name and file - "
          "so it is not taken for a model never saved")
    check(any("same model" in line for line in lines),
          "and the chat is told it is the same model under a new name")

    pin = DocumentPin()
    pin.check({"ok": True, "document": "Tower", "documentPath": r"C:\a\Tower.rvt"})
    elsewhere = dict(unsaved, document="Annexe", save=saved_as["save"])
    _, lines = after(pin, elsewhere)
    check(pin.title == "Tower" and any("use this model" in line for line in lines),
          "a Save As the pin cannot place leaves it where it was, and says how to move")

    server = TOOLS.server_text()
    start = server.find("def revit_change(")
    body = server[start:server.find("@server.tool()", start)] if start >= 0 else ""
    check_at = at(body, "pinned.check(reply")
    follow_at = at(body, "after_save(pinned, reply)")
    never_at = at(body, "_never_saved_line(reply)")
    check(0 <= check_at < follow_at < never_at,
          "revit_change follows a Save As only after the pin has passed the "
          "reply, and before it reads whether the model has a file")
    check("lines.extend(saved)" in body, "and prints what the save did")


def main():
    print("Q-69 - Heron saves the model it works in, after the group")
    card_section()
    addin_section()
    server_section()
    print()
    if FAILURES:
        print("FAILED - %d check(s)" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("PASSED - a save follows a kept change, after the group, to a file the "
          "modeller named; a preview saves nothing; a Save As keeps the chat on the model.")
    return 0


if __name__ == "__main__":
    try:
        import yaml  # noqa: F401
    except ImportError:
        print("NOT RUN - this needs PyYAML (pip install --user pyyaml).")
        sys.exit(3)
    sys.exit(main())
