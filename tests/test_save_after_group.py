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
  * the group was KEPT, or the run was a PREVIEW whose rollback held - the change
    undone, the save not, and the reply says both (D-171, superseding D-55 in
    part; the owner's "okkey" of 2026-11-16). A DISCARD saves nothing: the box is
    answered before the save, so a save follows only a Keep (D-171 point 5);
  * the job is a Publish job and the switches still say so when it saves.

ANOTHER OPEN DOCUMENT - version 2's job, a nested family's own window - goes the
same way since 2026-11-16 (row 5b-1824): the fragment asks through a second
ambient need, `saveOtherWhenKept`, and the add-in saves it and closes its window
after the group, by the same rules. Until then it was saved inside the run,
before Revit's Keep or Discard box, so a Discard still wrote that file. And the
box tells a publish whose save waits for Keep that nothing is saved yet - not
that its save "is done already".

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
    check("PREVIEW" in cases and "the save was done" in cases,
          "its cases run as previews, and a preview's save is looked for (D-171)")
    check("DISCARD" in cases and "NOT saved" in cases,
          "and a Discard is a negative: nothing saved")
    check("rolled-back run saves nothing" not in source
          and "preview too, which rolls the change back and still saves" in source,
          "the fragment tells the modeller a preview still saves - not that it saves nothing")

    # ROW 5b-1824: ANOTHER OPEN DOCUMENT IS SAVED AFTER THE GROUP TOO. Version
    # 2's `found.Save(...)` ran inside the run, before Revit's Keep or Discard
    # box, so a Discard still wrote that file and closed its window. Now the
    # run writes and closes NOTHING itself, so whatever the modeller answers
    # reaches only the add-in's act after the group.
    other = needs.get("saveOtherWhenKept")
    check(other is not None and other.get("source") == "ambient"
          and other.get("type") == "Action<string, bool>",
          "it declares the ambient need the add-in saves ANOTHER open document "
          "through - its file, and whether to close its window - typed as bound")
    check("saveOtherWhenKept(" in bare,
          "and asks for that save through it rather than making it")
    check(re.search(r"\.\s*Save\s*\(", bare) is None,
          "the fragment saves NO document itself - not even another one - so a "
          "Discard has nothing of its to take back (D-171 point 5)")
    check(re.search(r"\.\s*Close\s*\(", bare) is None,
          "nor closes any window inside the run: the close follows the save, after "
          "the group")
    close_asked = provides.get("closeAsked") or {}
    check(close_asked.get("role") == "result" and close_asked.get("type") == "bool",
          "closeAsked is a result: what the run asked the add-in to close")
    for gone in ("saved", "savedTo", "closed"):
        check(gone not in provides,
              "'%s' is no longer a result: the run writes and closes nothing, so it "
              "has nothing of its own to report - the reply's save block does" % gone)
    check("and so does a Discard" not in source and "SAVED INSIDE THE RUN" not in yaml_text,
          "neither the C# nor the card says another document is still saved inside "
          "the run")
    check("kind saveOther" in cases and "window left open" in cases,
          "its cases look for another document's save in the reply's save block, and a "
          "Discard's negative leaves that window open")


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
    # D-171, read back to the owner on 2026-11-16 ("okkey"): A PREVIEW SAVES
    # TOO - only when a save was asked for - and a DISCARD saves nothing,
    # because the box is answered before the save (point 5).
    for guard, what in (("if (ask == null || !ask.Asked) return reply;",
                         "nothing at all unless the fragment asked - a preview with no "
                         "save asked for saves nothing"),
                        ("if (answered != null && answered != Keep && answered != RestOfJob)",
                         "nothing after a Discard or a closed box - a save follows only a Keep"),
                        ("else if (tried && !kept)",
                         "nothing when Heron asked to keep and Revit did not report it kept"),
                        ("else if (!kept && !rollbackHeld)",
                         "nothing after a preview whose rollback did not hold"),
                        ("else if (!sendsOut)", "nothing unless the job is a Publish job"),
                        # The switches as this Revit holds them (D-166): read
                        # from heron.config until the merge that brought D-166
                        # in, which the switches left that day.
                        ("HeronRunPolicy.Withdrawn(risk, HeronPermissions.Switches()",
                         "and the switches are read again just before the save, as "
                         "this Revit holds them")):
        where = at(save_code, guard)
        check(0 <= where < first_save, "%s - checked before any save" % what)
    check(re.search(r"if\s*\(\s*!kept\s*\)", save_code) is None,
          "and NO guard refuses a run only for being rolled back: a preview that "
          "asked for a save saves (D-171 point 6)")
    check("SaveNow(target, kept)" in save_code and "SaveAsNow(target, ask, kept)" in save_code,
          "the save is told whether the change was kept, so a preview says so")
    call = run_code[call_at:call_at + 400] if call_at >= 0 else ""
    check("rolledBack != null && rolledBack.Held" in call,
          "Run hands it whether Revit reported the rollback held - read, not assumed")
    check("THE CHANGE WAS ROLLED BACK" in save and "AND THE SAVE WAS DONE" in save,
          "a preview that saved says the change was rolled back and the save was done")
    check('Json.Bool("preview", !kept)' in save,
          "and marks the save block a preview's")
    check("a save follows only a Keep" in save, "a Discard says a save follows only a Keep")
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
    check("never saves" not in save,
          "and nothing says a rolled-back run never saves - the rule before D-171")
    check("NOTHING WENT TO CENTRAL" in save,
          "a workshared local save says nothing went to central")
    check('Json.Str("creationGuid"' in save and 'Json.Str("documentPath"' in save,
          "a Save As reports the model's new identity, for the pin to follow")

    other_section(run, run_code, save, save_code)


def other_section(run, run_code, save, save_code):
    """Row 5b-1824: ANOTHER open document, saved after the group by the same
    rules - and Revit's Keep or Discard box saying so."""
    print()
    print("Another open document - after the group too (row 5b-1824)")
    check('const string SaveOtherWhenKeptNeed = "saveOtherWhenKept";' in run,
          "RevitFragment.cs names the second ambient need beside the first")
    other_at = at(run, "if (name == SaveOtherWhenKeptNeed)")
    branch = run[other_at:other_at + 1600] if other_at >= 0 else ""
    check("needs_write_path" in branch,
          "it is refused on the read path - a read has no group to close")
    check("SaveAsk.BindOther(" in branch
          and 'lines.Append("Action<string, bool> ")' in branch,
          "and bound to the add-in's own record of the ask, typed as the card declares it")

    # ONE DISPATCH AFTER EVERY GUARD - so a Discard, a closed box, a run Revit
    # did not report kept, a rollback that did not hold, a run that is not a
    # Publish job and a withdrawn switch stop BOTH saves, and a preview that
    # asked for one makes either.
    dispatch = re.search(
        r"block\s*=\s*ask\.Other\s*\?\s*SaveOtherNow\(\s*app\s*,\s*target\s*,\s*ask\s*,\s*kept\s*\)"
        r"\s*:\s*saveAs\s*\?\s*SaveAsNow\(\s*target\s*,\s*ask\s*,\s*kept\s*\)"
        r"\s*:\s*SaveNow\(\s*target\s*,\s*kept\s*\)\s*;", save_code)
    check(dispatch is not None,
          "the save of another document is reached by the same one line as the save "
          "of the model Heron works in")
    where = dispatch.start() if dispatch else -1
    for guard, what in (("if (answered != null && answered != Keep && answered != RestOfJob)",
                         "a Discard or a closed box"),
                        ("else if (tried && !kept)", "a run Revit did not report kept"),
                        ("else if (!kept && !rollbackHeld)", "a preview whose rollback did not hold"),
                        ("else if (!sendsOut)", "a run that is not a Publish job"),
                        # D-166: the switches are Revit's in memory, no longer
                        # heron.config's, so the guard reads HeronPermissions.
                        ("HeronRunPolicy.Withdrawn(risk, HeronPermissions.Switches()", "a switch turned off")):
        check(0 <= at(save_code, guard) < where,
              "%s saves NEITHER document - its guard comes before that line" % what)

    start = at(save_code, "private static string SaveOtherNow(")
    body = save_code[start:] if start >= 0 else ""
    nxt = re.search(r"\n\s*(?:private|internal) static ", body[10:])
    body = body[:nxt.start() + 10] if nxt else body
    check(bool(body), "SaveOtherNow is there")
    save_in = at(body, ".Save(new SaveOptions())")
    close_in = at(body, ".Close(false)")
    check(0 <= save_in < close_in,
          "it saves first and closes the window only after - the close version 2's "
          "closeWindow asked for, kept in one call")
    check(0 <= at(body, "ActiveUIDocument") < close_in,
          "it never closes the window in front - Revit lets no tool close that one")
    check(0 <= at(body, "IsModified") < close_in,
          "nor a document that still has unsaved changes - Close(false) would lose them")
    check(0 <= at(body, "OpenToSave(") < save_in,
          "it asks first whether the document can be saved at all - read-only, or a "
          "transaction open in it")
    elsewhere = save_code[:start] + save_code[start + len(body):] if start >= 0 else ""
    check(start >= 0 and re.search(r"\.\s*Close\s*\(", elsewhere) is None,
          "and nothing else in the add-in's save closes a window")
    check('Json.Str("kind", "saveOther")' in save,
          "its save block is its own kind, so the chat's pin never follows it")
    check('Json.Bool("closeAsked"' in save and 'Json.Bool("closed"' in save,
          "and says whether a close was asked and whether the window is closed")
    check("Its window was left open" in save,
          "a save not done of a window asked closed says the window was left open")
    check("Heron saves one document per run" in save
          and "throw new InvalidOperationException(" in save,
          "a run that asks for two saves is stopped in the run - so the box and the "
          "save block never speak for one of two")

    # THE BOX (D-126) IS ASKED INSIDE THE GROUP, before either save, and says
    # what waits for Keep: never "done already" for a save that has not
    # happened. Its words are HeronKeepQuestion's, proved by
    # tests/Heron.KeepQuestion.TestHost through tests/test_keep_question.py.
    waits_at = at(run_code, "var waitsForKeep = SaveWaitsForKeep(globals.__heron, target);")
    ask_at = at(run_code, "answered = AskTheModeller(")
    check(0 <= waits_at < ask_at,
          "Run reads what the fragment asked to be saved before the box is shown")
    call = run_code[ask_at:ask_at + 300] if ask_at >= 0 else ""
    check("waitsForKeep" in call, "and hands it to the box")
    check("HeronKeepQuestion.WaitsForKeep(" in save_code,
          "the sentence comes from HeronKeepQuestion, which the host proves")


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

    preview = dict(plain, save={"asked": True, "kind": "save", "done": True, "preview": True})
    _, lines = after(pin, preview)
    check(any("ROLLED BACK" in line and "SAVE WAS DONE" in line
              and "cannot be undone" in line for line in lines),
          "a preview's save reported without its sentence still says the change was "
          "rolled back, the save done, and that it cannot be undone (D-171)")

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

    # ROW 5b-1824: another open document's save is said in the add-in's words
    # and never moves the pin - the chat still works on the model it was in.
    pin = DocumentPin()
    pin.check({"ok": True, "document": "HeronNest_Car", "documentPath": r"C:\w\HeronNest_Car.rfa"})
    part = ("SAVED. 'HeronNest_Hub' was saved to C:\\w\\HeronNest_Hub.rfa once Heron's Undo "
            "entry had closed. A SAVE CANNOT BE UNDONE. Its window is closed.")
    nested = {"ok": True, "document": "HeronNest_Car", "documentPath": r"C:\w\HeronNest_Car.rfa",
              "save": {"asked": True, "kind": "saveOther", "done": True, "closed": True,
                       "savedDocument": "HeronNest_Hub", "said": part}}
    reply, lines = after(pin, nested)
    check(any(part in line for line in lines) and pin.title == "HeronNest_Car"
          and reply.get("document") == "HeronNest_Car",
          "another document's save is said in the add-in's words, and the chat stays on "
          "the model it works in")

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


def client_section():
    print()
    print("The proving client - where a preview is run today (D-171)")
    sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
    try:
        import heron_bridge_client as CLIENT
    except Exception as error:                          # noqa: BLE001 - reported
        check(False, "heron_bridge_client imports (%s)" % error)
        return
    sentence = getattr(CLIENT, "save_sentence", None)
    check(sentence is not None, "the client has a way to say what a save did at all")
    if sentence is None:
        sentence = lambda reply: None                   # so the checks below RUN and FAIL
    said = ("THE CHANGE WAS ROLLED BACK - this run was a preview - AND THE SAVE WAS DONE: ... "
            "A SAVE CANNOT BE UNDONE.")
    line = sentence({"ok": True, "rolledBack": True,
                     "save": {"asked": True, "done": True, "preview": True, "said": said}}) or ""
    check(line.startswith("SAVED") and said in line,
          "a preview that saved is printed SAVED, in the add-in's words, so ROLLED BACK "
          "is never read as nothing written to disk")
    check(sentence({"ok": True, "rolledBack": True}) is None,
          "and a run that asked for no save prints nothing about one")
    check((sentence({"save": {"done": False, "said": "NOT SAVED. You pressed Discard."}})
           or "").startswith("NOT SAVED"),
          "a save not done is printed NOT SAVED")
    client = read(os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py"))
    fragment_at = client.find("def cmd_fragment(")
    fragment_body = client[fragment_at:client.find("\ndef ", fragment_at + 10)]
    check("print_save(reply)" in fragment_body,
          "`fragment` prints it under the verdict")
    check('told += " - and the save asked for with it: %s" % said' in client,
          "and a `validate` phase writes it into the run record a proof is drafted from")


def main():
    print("Q-69 - Heron saves the model it works in, after the group")
    card_section()
    addin_section()
    server_section()
    client_section()
    print()
    if FAILURES:
        print("FAILED - %d check(s)" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("PASSED - a save follows a Keep or a preview that asked for one, after the "
          "group, to a file the modeller named - of the model Heron works in or of another "
          "open document; a Discard saves neither; the box says what waits for Keep; a "
          "Save As keeps the chat on the model.")
    return 0


if __name__ == "__main__":
    try:
        import yaml  # noqa: F401
    except ImportError:
        print("NOT RUN - this needs PyYAML (pip install --user pyyaml).")
        sys.exit(3)
    sys.exit(main())
