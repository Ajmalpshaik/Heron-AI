# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
QA checklists (docs/50) - kept, checked against the contract, and run check by
check through one door, with a NOT RUN never counted as a pass.

    python tests/test_checklists.py

WHAT IT PROVES
  1. A CHECKLIST IS CHECKED AGAINST EACH FRAGMENT'S OWN CONTRACT WHEN IT IS
     SAVED - a capability that changes the model, one Heron does not have, a
     value the fragment does not take, one it cannot run without, a rule on an
     output it does not declare or that is text, a comparison that is not one,
     and an ids output that holds elements rather than ids - each refused by
     name, and nothing is kept.
  2. IT IS KEPT WHOLE under the commands' data home, found by its name, never
     silently replaced, and a broken file is named rather than hidden.
  3. THE PASS RULE reads the add-in's own shapes - a number, "N item(s)",
     "N entry(ies)", true/false - and "(null)" or text is NOT RUN, not 0.
  4. A RUN asks once per check, in order; a check not run says why and is
     never counted as a pass; the failing ids are offered only when every one
     of them arrived.
  5. THE WORDING says "meets the value checked" and never calls a model
     compliant, clean or healthy - in any sentence a run or a save can say.
  6. THE SERVER'S RUN goes through revit_read's own body, one check at a
     time under the lock, refuses a DRAFT, and sends nothing when Revit is not
     connected - against a stand-in server, as tests/test_commands.py does. A
     read that raises still ends in a final, non-running result; more than
     three list ids offer no Select, a dictionary keyed by id does; values
     given as a name -> value set are taken and bad shapes refused in words.
  Also: float and HashSet outputs refused, true/false and "0" read as numbers,
  a rule sent twice refused, and hand-edited files of the wrong shape listed as
  broken while the list keeps working.
  7. HERON'S STARTER STILL FITS TODAY'S CARDS - each of its checks is saved
     through the real cards, read as the server reads them - and the
     --fragments listing names only PROVEN reads with a number to compare.
     This part needs PyYAML, as every reader of a card does; without it, it
     says NOT RUN and the rest still counts.
  The MCP tool itself, through the real fragment contracts, is
  tests/test_checklist_tool.py - it needs the MCP SDK, and this runs everywhere.
"""

from __future__ import print_function

import io
import json
import os
import re
import shutil
import sys
import tempfile
import threading

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_checklists as Q                                  # noqa: E402

FAILURES = []
BANNED = re.compile(r"\b(compliant|compliance|clean|healthy)\b", re.I)


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# A stand-in for what the server reads off three real cards.
CONTRACTS = {
    "CHECK_MODEL_STANDARDS": {
        "risk": "READ",
        "needs": [{"name": "doc", "type": "Document"},
                  {"name": "requiredParameters", "type": "IList<string>", "source": "request"},
                  {"name": "maxRows", "type": "int", "source": "request", "optional": "true"}],
        "provides": [{"name": "failures", "type": "int"},
                     {"name": "offenders", "type": "IList<ElementId>"},
                     {"name": "findings", "type": "IList<string>"},
                     {"name": "report", "type": "string"},
                     {"name": "elements", "type": "IList<Element>"},
                     {"name": "ratio", "type": "float"},
                     {"name": "seen", "type": "HashSet<ElementId>"},
                     {"name": "workshared", "type": "bool"},
                     {"name": "elementTotal", "type": "int"}],
        "required": ["requiredParameters"], "selection": []},
    "FIND_CLASHES": {
        "risk": "READ",
        "needs": [{"name": "elements", "type": "IList<Element>"}],
        "provides": [{"name": "clashes", "type": "IDictionary<ElementId, ICollection<ElementId>>"},
                     {"name": "clashing", "type": "int"}],
        "required": [], "selection": ["elements"]},
    "DELETE_ELEMENTS": {"risk": "MODIFY", "needs": [], "provides": [], "required": [], "selection": []},
}


def good():
    return {"name": "Before Issue", "description": "the week's checks",
            "checks": [{"sentence": "every duct has a System Name",
                        "capability": "check_model_standards",
                        "values": [{"name": "requiredParameters", "value": "System Name"}],
                        "rule": "failures == 0", "ids": "offenders"},
                       {"sentence": "nothing clashes", "capability": "FIND_CLASHES",
                        "values": [], "rule": "clashing <= 2", "ids": "clashes"}]}


def bad_with(**change):
    checklist = good()
    checklist["checks"][0].update(change)
    return checklist


def shape(home):
    print("1. checked against the contract when saved")
    check(Q.problems(good(), CONTRACTS) == [], "a good checklist has no problems")
    cases = [
        (bad_with(capability="DELETE_ELEMENTS", values=[]), "declared MODIFY, and a check may only read",
         "a capability that changes the model is refused by its risk"),
        (bad_with(capability="NO_SUCH"), "Heron has nothing called NO_SUCH", "an unknown capability"),
        (bad_with(values=[{"name": "requiredParameters", "value": "x"}, {"name": "maxRowz", "value": "5"}]),
         "'maxRowz' is not a value CHECK_MODEL_STANDARDS takes", "a value the fragment does not take"),
        (bad_with(values=[]), "cannot run without a value for 'requiredParameters'",
         "a value the fragment cannot run without"),
        (bad_with(values=[{"name": "requiredParameters", "value": "{level}"}]), "'{level}'",
         "an {input}, which a checklist has none of"),
        (bad_with(rule="failurs == 0"), "'failurs' is not something CHECK_MODEL_STANDARDS leaves behind",
         "a rule on an output it does not declare, naming what it does"),
        (bad_with(rule="report == 0"), "'report' is string, not a number",
         "a rule on a text output"),
        (bad_with(rule="failures ~ 0"), "must be one output", "a comparison that is not one"),
        (bad_with(rule="failures == lots"), "must be one output", "a rule that compares with a word"),
        (bad_with(ids="elements"), "holds elements, and Revit sends back their names",
         "an ids output that holds elements, not ids"),
        (bad_with(ids="findings"), "holds no element ids", "an ids output that holds text"),
        (bad_with(sentence=""), "has no sentence", "a check with no sentence"),
        (bad_with(sentence="two\nlines"), "one line", "a sentence of two lines"),
    ]
    for checklist, words, what in cases:
        said = " ".join(Q.problems(checklist, CONTRACTS))
        check(words in said, "%s - %r" % (what, said[:140]) if words not in said else what)
    more = [
        (bad_with(rule="ratio == 0"), "'ratio' is float, not a number",
         "a float output - Describe sends its type name - is refused when saved"),
        (bad_with(rule="seen == 0"), "'seen' is HashSet<ElementId>, not a number",
         "a HashSet output - not a non-generic ICollection - is refused when saved"),
        (bad_with(ids="seen"), "holds no element ids", "and a HashSet of ids cannot carry the ids either"),
        (bad_with(rule="failures == 0", output="failures"), "give the rule once",
         "a rule sent both as text and as output names one sentence, not 'None'"),
        (bad_with(values="requiredParameters=x"), "its values must be a list",
         "values that are not a list are refused in words"),
        (bad_with(values=["requiredParameters=x"]), "each of its values must be a name and a value",
         "a value that is not a name and value is refused in words"),
        (bad_with(values=[{"name": "requiredParameters", "value": 3}]), "both text",
         "a value that is not text is refused in words"),
        (bad_with(capability=42), "the capability must be its name, as text", "a capability that is not text"),
        (bad_with(sentence=["x"]), "the sentence must be text", "a sentence that is not text"),
        (bad_with(ids=7), "'ids' must name one output", "ids that are not text"),
        (dict(good(), name=7), "The name must be text", "a name that is not text"),
        (dict(good(), checks=["x"]), "is not a set of named fields", "a check that is not a set of fields"),
    ]
    for checklist, words, what in more:
        said = " ".join(Q.problems(checklist, CONTRACTS))
        check(words in said, what if words in said else "%s - %r" % (what, said[:200]))
    for rule, want in (("workshared == true", ("workshared", "==", 1)),
                       ("workshared != false", ("workshared", "!=", 0))):
        check(Q.parse_rule(rule) == want, "%r reads true/false as 1/0" % rule)
    check(Q.problems(bad_with(rule="workshared == true"), CONTRACTS) == [], "a flag rule saves")
    typed = bad_with(rule=None, output="failures", op="==", target="0")
    check(Q.problems(typed, CONTRACTS) == [] and Q._normalised(typed["checks"][0])["target"] == 0,
          "a target sent as the text \"0\" is the number 0")
    absent_ok = dict(CONTRACTS, CHECK_MODEL_STANDARDS=dict(CONTRACTS["CHECK_MODEL_STANDARDS"], required=[]))
    check(Q.problems(bad_with(values=[]), absent_ok) == [],
          "what may be left out is the server's need_may_be_absent answer, never guessed here")
    check(any("which values" in p for p in Q.problems(good(), dict(
        CONTRACTS, CHECK_MODEL_STANDARDS=dict(CONTRACTS["CHECK_MODEL_STANDARDS"], required=None)))),
        "a contract that cannot say what is required is refused, never passed")
    many = good()
    many["checks"] = many["checks"] * (Q.MAX_CHECKS // 2 + 1)
    check(any("at most %d" % Q.MAX_CHECKS in p for p in Q.problems(many, CONTRACTS)),
          "a list longer than one run should hold is refused")
    check(Q.problems(dict(good(), checks=[]), CONTRACTS) == ["It has no checks."], "a list with no checks")
    check(any("Heron could not read what" in p for p in Q.problems(
        good(), dict(CONTRACTS, CHECK_MODEL_STANDARDS=dict(CONTRACTS["CHECK_MODEL_STANDARDS"], needs=None)))),
        "an unreadable contract is refused, never passed")
    try:
        Q.save(bad_with(rule="nope"), CONTRACTS, home=home)
        check(False, "a bad checklist is refused")
    except Q.ChecklistError as why:
        check(str(why).startswith("Not saved"), "a bad checklist is refused whole")
    check(Q.all_checklists(home) == ([], []), "and nothing was kept")


def keep(home):
    print("2. kept, found, listed, never silently replaced")
    path = Q.save(good(), CONTRACTS, home=home, now="2026-11-08T10:00:00")
    check(path.endswith(os.path.join("checklists", "personal", "before-issue.json")),
          "kept under the data home as its slug, beside the saved commands")
    with io.open(path, encoding="utf-8") as fh:
        kept = json.load(fh)
    first, second = kept["checks"]
    check(kept["heron-checklist"] == 1 and first["capability"] == "CHECK_MODEL_STANDARDS",
          "the format is marked and the capability upper-cased")
    check((first["output"], first["op"], first["target"]) == ("failures", "==", 0)
          and first["idsKind"] == "list" and second["idsKind"] == "keys",
          "the rule is kept in its three parts, and how its ids arrive")
    check(second["selection"] == ["elements"] and first["selection"] == [],
          "a check that reads the selection says so")
    check(not os.path.exists(path + ".part"), "no half-written file is left")
    check(Q.find("before issue", home)["name"] == "Before Issue", "found by its name, any case")
    try:
        Q.save(good(), CONTRACTS, home=home)
        check(False, "a second save under the same name is refused")
    except Q.ChecklistError as why:
        check("already saved" in str(why), "a second save under the same name is refused")
    Q.save(dict(good(), description="replaced"), CONTRACTS, home=home, replace=True)
    check(Q.find("Before Issue", home)["description"] == "replaced", "replace=True replaces it")
    with io.open(os.path.join(Q.folder(home), "broken.json"), "w", encoding="utf-8") as fh:
        fh.write(u"{not json")
    with io.open(os.path.join(Q.folder(home), "a-command.json"), "w", encoding="utf-8") as fh:
        fh.write(u'{"heron-command": 1}')
    bad_files = {
        "no-name.json": {"heron-checklist": 1, "name": 5, "checks": [dict(first)]},
        "checks-text.json": {"heron-checklist": 1, "name": "x", "checks": "nope"},
        "check-text.json": {"heron-checklist": 1, "name": "x", "checks": ["nope"]},
        "bad-op.json": {"heron-checklist": 1, "name": "x", "checks": [dict(first, op="~")]},
        "word-target.json": {"heron-checklist": 1, "name": "x", "checks": [dict(first, target="lots")]},
        "bad-values.json": {"heron-checklist": 1, "name": "x", "checks": [dict(first, values="a=b")]},
        "no-output.json": {"heron-checklist": 1, "name": "x", "checks": [dict(first, output=None)]},
    }
    for file_name, data in bad_files.items():
        with io.open(os.path.join(Q.folder(home), file_name), "w", encoding="utf-8") as fh:
            fh.write(json.dumps(data))
    found, broken = Q.all_checklists(home)
    check([c["name"] for c in found] == ["Before Issue"]
          and broken == sorted(["a-command.json", "broken.json"] + list(bad_files)),
          "a broken file, one that is not a checklist, and every file of the wrong shape are named, "
          "not hidden - found %r" % broken)
    check(len(Q.listing(home)["lists"]) == 1 and Q.describe(found[0]), "and the list still works around them")
    for file_name in bad_files:
        os.remove(os.path.join(Q.folder(home), file_name))
    found, broken = Q.all_checklists(home)
    listed = Q.listing(home)
    check(listed["lists"][0]["sentences"] == ["every duct has a System Name", "nothing clashes"]
          and listed["broken"] == broken, "the page's list is ready to draw")
    os.remove(os.path.join(Q.folder(home), "broken.json"))
    os.remove(os.path.join(Q.folder(home), "a-command.json"))
    try:
        Q.find("nothing here", home)
        check(False, "an unknown name is refused")
    except Q.ChecklistError as why:
        check("Saved: Before Issue" in str(why), "an unknown name is refused, naming what is saved")
    check(Q.starter("Mine")["name"] == "Mine" and Q.STARTER["name"] != "Mine",
          "the starter is copied under the modeller's name; the example is untouched")
    check(not os.path.exists(os.path.join(Q.folder(home), Q.slug(Q.STARTER["name"]) + ".json")),
          "and it is never saved by itself")


def rules():
    print("3. the pass rule reads the add-in's own shapes")
    for sent, number in (("0", 0), ("12", 12), ("2.5", 2.5), ("3 item(s) [1, 2, 3]", 3),
                         ("0 item(s)", 0), ("7 entry(ies)", 7), ("true", 1), ("false", 0), (4, 4)):
        check(Q.as_number(sent) == number, "%r reads as %r" % (sent, number))
    for sent in ("(null)", "", "Level 1", "NaN", None, "ElementId"):
        check(Q.as_number(sent) is None, "%r is no number - the check is NOT RUN, never 0" % (sent,))
    check(Q.parse_rule("failures <= 3") == ("failures", "<=", 3)
          and Q.parse_rule(" x>=1.5 ") == ("x", ">=", 1.5), "a rule is read in three parts")
    check(all(Q.holds(v, op, 2) == want for v, op, want in
              ((2, "==", True), (3, "!=", True), (1, "<", True), (2, "<=", True), (3, ">", True),
               (2, ">=", True), (3, "==", False), (2, "<", False))), "every comparison holds as written")
    check(Q.ids_from("list", "o", {"o": "2 item(s) [501, 502]"}, {}) == (["501", "502"], 2, True),
          "a short list of ids arrives whole")
    check(Q.ids_from("list", "o", {"o": "40 item(s) [501, 502, 503, ...]"}, {}) == (["501", "502", "503"], 40, False),
          "a long one arrives as its count and first three, and is not complete")
    check(Q.ids_from("keys", "c", {"c": "2 entry(ies)"}, {"c": {"9": "1 item(s)", "8": "2 item(s)"}})[2],
          "a dictionary keyed by id arrives whole")


class _Ask(object):
    """What the server's ask would say for each capability, recording each call."""

    def __init__(self, answers):
        self.answers, self.asked = answers, []

    def __call__(self, check):
        self.asked.append(check["capability"])
        return self.answers[check["capability"]]


def running(home):
    print("4. a run: one read a check, NOT RUN never a pass")
    checklist = Q.find("Before Issue", home)
    third = dict(checklist["checks"][0], sentence="every pipe has a System Name")
    checklist["checks"].append(dict(third, capability="CHECK_PIPES"))
    ask = _Ask({"CHECK_MODEL_STANDARDS": {"ran": True, "document": "Tower MEP",
                                          "provides": {"failures": "2", "offenders": "2 item(s) [501, 502]"}},
                "FIND_CLASHES": {"ran": True, "document": "Tower MEP",
                                 "provides": {"clashing": "1"}},
                "CHECK_PIPES": {"ran": False, "why": "CHECK_PIPES is DRAFT - no recorded proof."}})
    steps = []
    answer = Q.run(checklist, ask, progress=steps.append)
    rows = answer["rows"]
    check(ask.asked == ["CHECK_MODEL_STANDARDS", "FIND_CLASHES", "CHECK_PIPES"],
          "each check is asked once, in order")
    check([r["state"] for r in rows] == ["fail", "pass", "not run"], "FAIL, PASS and NOT RUN, as read")
    check(rows[0]["ids"] == ["501", "502"] and rows[0]["idsComplete"], "a failing check carries its ids")
    check(rows[2]["why"].startswith("CHECK_PIPES is DRAFT") and rows[2]["value"] is None,
          "a check not run says why, with no value")
    check(answer["counts"] == {"pass": 1, "fail": 1, "not run": 1, "total": 3, "done": 3},
          "NOT RUN is its own count, never added to the passes")
    check("1 meet the value checked, 1 do not, 1 not run" in answer["said"]
          and "A check not run is not a pass" in answer["said"] and "Tower MEP" in answer["said"],
          "the sentence counts each, names the model and says a not-run is not a pass")
    check(len(steps) == 2 and steps[0]["running"] and steps[0]["counts"]["done"] == 1,
          "the page is told after each check but the last, marked running")
    partial = Q.judge(1, dict(checklist["checks"][0]),
                      {"ran": True, "provides": {"failures": "40", "offenders": "40 item(s) [1, 2, 3, ...]"}})
    check(partial["state"] == "fail" and partial["ids"] == [] and not partial["idsComplete"]
          and "3 of the 40" in partial["why"], "ids that did not all arrive are not offered, and why is said")
    gone = Q.judge(1, dict(checklist["checks"][0]), {"ran": True, "provides": {"findings": "0 item(s)"}})
    check(gone["state"] == "not run" and "did not send back 'failures'" in gone["why"],
          "an output that did not come back is NOT RUN, never a pass")
    null = Q.judge(1, dict(checklist["checks"][0]), {"ran": True, "provides": {"failures": "(null)"}})
    check(null["state"] == "not run", "an output that came back as nothing is NOT RUN")
    check(answer["run"] and all(st["run"] == answer["run"] for st in steps),
          "every step of one run carries the same token, for the page's Select")
    calls = []

    def breaks(check_):
        calls.append(1)
        if len(calls) == 2:
            raise RuntimeError("the pipe closed")
        return ask(check_)
    stopped = Q.run(checklist, breaks)
    check(stopped["running"] is False and stopped["stopped"].startswith("The run stopped at check 2")
          and [r["state"] for r in stopped["rows"]] == ["fail", "not run", "not run"]
          and "the pipe closed" in stopped["rows"][2]["why"] and len(calls) == 2,
          "an ask that raises ends the run in words: what was read is kept, the rest NOT RUN, never running")
    moving = iter([10, 10, 14])
    changed = Q.run(checklist, lambda c: dict(ask(c), changes=next(moving)))
    check(changed["findings"] == [Q.CHANGED_DURING] and Q.CHANGED_DURING in Q.text(changed),
          "a model that changed between two checks is said, in the result and in the chat")
    blank = Q.run({"name": "x", "checks": checklist["checks"]}, lambda c: {"ran": False, "why": "gone"})
    check(blank["counts"]["pass"] == 0 and blank["counts"]["not run"] == 3,
          "nothing run is nothing passed")
    return answer


def wording(answer):
    print("5. the wording")
    said = Q.text(answer)
    check("PASS" in said and "FAIL" in said and "NOT RUN" in said, "the chat sees PASS, FAIL and NOT RUN")
    check("Nothing in the model was changed" in said and "nothing to undo, keep or discard" in said,
          "and that a run changed nothing, so there is nothing to undo")
    words = [said, json.dumps(answer), json.dumps(Q.STARTER), json.dumps(Q.STATES)]
    words += [p for p in Q.problems(bad_with(rule="report == 0", ids="elements"), CONTRACTS)]
    hits = [m.group(0) for text in words for m in BANNED.finditer(text)]
    check(not hits, "never compliant, clean or healthy%s" % (" - found %s" % hits if hits else ""))
    check([s[0] for s in Q.STATES] == ["pass", "fail", "not run"]
          and dict((s[0], s[2]) for s in Q.STATES) == {"pass": "#3ecf8e", "fail": "#ff5a4e",
                                                       "not run": "#f5a524"},
          "the states are the brand's Success, Error and Warning (D-149)")
    brand = io.open(os.path.join(ROOT, "docs", "brand", "README.md"), encoding="utf-8").read().upper()
    check(all(("`%s`" % s[2].upper()) in brand for s in Q.STATES), "each colour is in docs/brand's table")


# ------------------------------------------------------------- the server's run

class _FakeServer(object):
    """The MCP server module's names _checklist_ask and _run_checklist use."""

    class NotBound(Exception):
        pass

    def __init__(self, replies, status="PROVEN", bound=True):
        self.replies, self.sent, self.held = replies, [], []
        outer = self
        self._revit_lock = threading.RLock()

        class binding(object):
            pid = 11

            @staticmethod
            def resolve():
                if not bound:
                    raise outer.NotBound("No Revit is connected.")

        class bridge(object):
            KEEPABLE_STATUSES = ("PROVEN", "PRODUCTION")

            @staticmethod
            def proof_of(folder):
                return {"status": status, "stale": False}

        class pinned(object):
            @staticmethod
            def check(reply):
                return None

        self.binding, self.bridge, self.pinned = binding, bridge, pinned
        self.changes = iter(())

        class companion(object):
            @staticmethod
            def live_changes(pid):
                return next(outer.changes, None)
        self.companion = companion

    def _fragment_for(self, capability):
        return (capability.lower().replace("_", "-"), "PROVEN")

    def _moved_since(self, identity):
        return None

    def _pin_identity(self):
        return ("Tower MEP", "", "11")

    def revit_read(self, capability, values, expect_from, ctx=None):
        if isinstance(self.replies.get(capability), Exception):
            raise self.replies[capability]
        held = self._revit_lock._is_owned()
        self.sent.append((capability, values, held))
        self.out["reply"] = self.replies[capability]
        self.out["ended"] = "ok"
        return "%s read Tower MEP." % capability

    def _through(self, tool, reply_out=None, origin="chat"):
        self.out = reply_out
        self.origin = origin
        return tool


def _server_run():
    """The server's _checklist_ask and _run_checklist, lifted out of its source
    and run against a stand-in - the MCP SDK is not needed to read them."""
    sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
    import heron_tools as TOOLS
    text = TOOLS.server_text()
    start = text.index("\ndef _checklist_checks(")
    end = text.index("\ndef _checklist_on_page(")
    values = text[text.index("\ndef _values_array("):text.index("\ndef ", text.index("\ndef _values_array(") + 5)]
    return text, values + text[start:end]


def server(home):
    print("6. the server's run, against a stand-in")
    text, source = _server_run()
    sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
    import heron_commands as C

    def lifted(fake):
        class brain(object):
            @staticmethod
            def commands():
                return C

            @staticmethod
            def checklists():
                return Q
        names = {"brain": brain, "NotBound": fake.NotBound, "revit_read": fake.revit_read,
                 "DASHBOARD_SELECT_MOST": 500, "_companion_module": lambda: fake.companion}
        for name in ("binding", "bridge", "pinned", "_revit_lock", "_fragment_for", "_moved_since",
                     "_pin_identity", "_through"):
            names[name] = getattr(fake, name)
        exec(compile(source, "heron_mcp_server.py", "exec"), names)
        return names

    checklist = Q.find("Before Issue", home)
    fake = _FakeServer({"CHECK_MODEL_STANDARDS": {"ok": True, "document": "Tower MEP",
                                                  "provides": {"failures": "0", "offenders": "0 item(s)"}},
                        "FIND_CLASHES": {"ok": True, "document": "Tower MEP",
                                         "provides": {"clashing": "1", "clashes": "1 entry(ies)"},
                                         "providesEntries": {"clashes": {"77": "1 item(s) [78]"}}}})
    run = lifted(fake)["_run_checklist"]
    answer = run(checklist, origin="companion")
    check([s[0] for s in fake.sent] == ["CHECK_MODEL_STANDARDS", "FIND_CLASHES"]
          and fake.sent[0][1] == "requiredParameters=System Name" and fake.origin == "companion",
          "each check is one read through revit_read's own body, with its saved values")
    check(all(s[2] for s in fake.sent), "each read is taken under the lock every tool call holds")
    check([r["state"] for r in answer["rows"]] == ["pass", "pass"]
          and answer["identity"] == ["Tower MEP", "", "11"], "both read, and the run keeps the model it read")
    many = _FakeServer({"CHECK_MODEL_STANDARDS": {"ok": True, "document": "Tower MEP",
                                                  "provides": {"failures": "9",
                                                               "offenders": "9 item(s) [1, 2, 3, ...]"}},
                        "FIND_CLASHES": {"ok": True, "document": "Tower MEP",
                                         "provides": {"clashing": "4", "clashes": "4 entry(ies)"},
                                         "providesEntries": {"clashes": dict((str(70 + k), "1 item(s)")
                                                                             for k in range(4))}}})
    names = lifted(many)
    answer = names["_run_checklist"](checklist)
    first, second = answer["rows"]
    check(first["state"] == "fail" and first["select"] == "" and first["ids"] == []
          and "3 of the 9" in first["why"], "more than three list ids: no Select, and why")
    check(second["state"] == "fail" and second["idsComplete"] and second["select"] == "Select in Revit"
          and sorted(second["ids"]) == ["70", "71", "72", "73"],
          "a dictionary keyed by id arrives whole: Select is offered")
    names["DASHBOARD_SELECT_MOST"] = 2
    names["_checklist_select_labels"](answer)
    check(answer["rows"][1]["select"] == "Select the first 2 of 4", "past the cap the button says the first N of M")
    moving = _FakeServer(fake.replies)
    moving.changes = iter([5, 6])
    answer = lifted(moving)["_run_checklist"](checklist)
    check(answer["findings"] == [Q.CHANGED_DURING], "a model that changed between two checks is a finding")
    broken = _FakeServer(dict(fake.replies, FIND_CLASHES=RuntimeError("the pipe closed")))
    held = []
    answer = lifted(broken)["_run_checklist"](checklist, progress=held.append)
    check(answer["running"] is False and answer["failed"] and answer["rows"][0]["state"] == "pass"
          and answer["rows"][1]["state"] == "not run" and "the pipe closed" in answer["rows"][1]["why"],
          "a read that raises: a final result, the rest NOT RUN with the reason, marked failed")
    checks = names["_checklist_checks"]
    got = checks(json.dumps([{"sentence": "s", "capability": "C",
                              "values": {"requiredParameters": "System Name", "maxRows": 5, "on": True}}]))
    check(got[0]["values"] == [{"name": "requiredParameters", "value": "System Name"},
                               {"name": "maxRows", "value": "5"}, {"name": "on", "value": "true"}],
          "values given as a name -> value set are accepted")
    for bad, words in (('[{"values": 5}]', "values must be"), ('[{"values": ["a=b"]}]', "each value must be"),
                       ('["x"]', "must be a set of named fields"), ('{"a": 1}', "JSON list"),
                       ('[{"values": {"a": [1]}}]', "text or a number"), ("[{", "JSON list")):
        try:
            checks(bad)
            check(False, "%s is refused" % bad)
        except ValueError as why:
            check(words in str(why), "%s is refused in words: %s" % (bad, why))
    check("_revit_lock" in source and source.count("with _revit_lock:") == 1,
          "the lock is taken inside one check's ask - never around the whole list")
    draft = _FakeServer(fake.replies, status="DRAFT")
    answer = lifted(draft)["_run_checklist"](checklist)
    check(not draft.sent and all(r["state"] == "not run" and "DRAFT" in r["why"] for r in answer["rows"]),
          "a DRAFT capability is NOT RUN, and nothing is sent")
    gone = _FakeServer(fake.replies, bound=False)
    answer = lifted(gone)["_run_checklist"](checklist)
    check(not gone.sent and all("not connected" in r["why"] for r in answer["rows"])
          and answer["counts"]["pass"] == 0, "Revit not connected: every check NOT RUN, nothing sent")
    tool = text[text.index("\ndef heron_checklist("):text.index("\n@server.tool()", text.index("\ndef heron_checklist("))]
    check("revit_change" not in tool and "revit_change" not in source,
          "the chat's tool and its run never reach revit_change")


def real_cards(home):
    print("7. the starter against today's cards")
    try:
        import yaml  # noqa: F401
    except ImportError:
        print("  NOT RUN - PyYAML is not installed, and every reader of a card needs it")
        return
    sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
    import heron_bridge_client as bridge
    import heron_fragment as FRAGMENT
    loaded, _problems = FRAGMENT.load_all()
    by_capability = dict((f.data.get("capability"), f) for f in loaded.values())
    contracts = {}
    for check_ in Q.STARTER["checks"]:
        frag = by_capability.get(check_["capability"])
        card = os.path.join(frag.folder, "fragment.yaml") if frag else ""
        contracts[check_["capability"]] = {
            "risk": bridge.fragment_risk(card), "needs": bridge.fragment_needs(card),
            "provides": frag.provides() if frag else None, "selection": [],
            "required": [n.get("name") for n in frag.needs() if FRAGMENT.need_source(n) == "request"
                         and not FRAGMENT.need_may_be_absent(n)]} if frag else None
    said = Q.problems(Q.starter(), contracts)
    check(said == [], "every starter check passes the save's checks against its real card%s"
          % (" - %s" % said if said else ""))
    usable = dict((row[0], row) for row in Q.usable(loaded.values()))
    fits = [c for c in Q.STARTER["checks"]
            if c["capability"] in usable and Q.parse_rule(c["rule"])[0] in usable[c["capability"]][1]
            and (not c.get("ids") or c["ids"] in usable[c["capability"]][2])
            and not usable[c["capability"]][3]]
    check(len(fits) == len(Q.STARTER["checks"]),
          "each is a PROVEN read with a fresh proof, collects its own elements, and its rule and ids "
          "outputs are a number and element ids")
    statuses = set(str(by_capability[c].status).upper() for c in usable)
    check(statuses <= {"PROVEN", "PRODUCTION"} and "DELETE_ELEMENTS" not in usable,
          "--fragments names only PROVEN reads, never a capability that changes the model")
    path = Q.save(Q.starter("Starter copy"), contracts, home=home)
    check(os.path.isfile(path) and Q.find("Starter copy", home)["checks"][0]["idsKind"] == "list",
          "and it saves, under the modeller's own name")


def main():
    home = tempfile.mkdtemp(prefix="heron-checklists-")
    try:
        shape(home)
        keep(home)
        rules()
        wording(running(home))
        server(home)
        real_cards(home)
    finally:
        shutil.rmtree(home, ignore_errors=True)
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    QA checklists are checked when kept, run one read at a time, and a NOT RUN "
          "is never a pass")
    return 0


if __name__ == "__main__":
    sys.exit(main())
