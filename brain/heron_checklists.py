# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
QA checklists - a named list of checks, each written in the modeller's own
words, saved once and run in one go (docs/50).

    python brain/heron_checklists.py               # list this PC's saved checklists
    python brain/heron_checklists.py --fragments   # which capabilities a check can use today

WHAT IT IS FOR
--------------
A modeller checks the same things before every issue: every link loaded,
every room enclosed, nothing referencing a file that is gone. Each of those is
already a PROVEN read in the fragment library. What was missing is the saved
list and the one press that runs it (docs/48 s5 item 4).

    {"heron-checklist": 1, "name": "Before issue",
     "checks": [{"sentence": "every linked model is loaded",
                 "capability": "LIST_LINKED_MODELS", "values": [],
                 "output": "notLoaded", "op": "==", "target": 0,
                 "ids": "notLoaded", "idsKind": "list"}]}

THE CHAT WRITES THE CHECK; THIS ONLY CHECKS WHAT IS SAVED. Turning "every
duct has a System Name" into a capability, its values and a rule is the
host's work (D-01) - Heron never classifies intent. What is checked here is
that the saved check can run as written: the capability only reads (READ or
ANALYZE), every value is one its fragment takes (heron_commands' own check,
called), a value it cannot run without is given, the rule's output is one the
fragment declares and holds a number, and the ids output holds element ids.

A PASS MEANS "MEETS THE VALUE CHECKED", AND NOTHING MORE. The rule is the
modeller's; whether the model is right is the engineer's call (the host
instructions, heron_modelqa's own rule). A check that could not run is NOT
RUN, with why, and is never counted with the passes.

WHAT IS NOT CHECKED WHEN SAVED, AND IS ON EVERY RUN: whether the capability is
PROVEN, which model, whether Revit is there. A proof can go stale between
saving and running (D-30), so the run asks each time.

Stdlib only. Nothing here talks to Revit: the run is handed `ask`, which the
MCP server makes from revit_read's own body.
"""

from __future__ import print_function

import io
import json
import math
import os
import re
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_commands as COMMANDS  # noqa: E402  - the slug, the folder, the whole-file write, the value check

FORMAT = 1
FOLDER = "checklists"
NAME_MAX = COMMANDS.NAME_MAX
SENTENCE_MAX = 200
# A RUN IS ONE READ PER CHECK, so the length of a list is the length of a run.
# Twenty-five reads is already minutes on a large model; a longer list is two.
MAX_CHECKS = 25
# What a check may be: the read door's own levels (heron_tools.door_refusal
# refuses anything above ANALYZE at revit_read, which every run goes through).
READS = ("READ", "ANALYZE")
OPERATORS = ("==", "!=", "<=", ">=", "<", ">")
RULE = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*(==|!=|<=|>=|<|>)\s*(-?\d+(?:\.\d+)?)\s*$")

# Each result's colour, decided here and drawn on the Companion page. THE
# BRAND'S COLOURS (D-149, docs/brand/README.md): Success, Error, and Warning
# for a check that did not run - it needs a look, and must never read as a
# pass.
STATES = (("pass", "PASS", "#3ecf8e", "meets the value checked"),
          ("fail", "FAIL", "#ff5a4e", "does not meet the value checked"),
          ("not run", "NOT RUN", "#f5a524", "not run - see why; never counted as a pass"))
LABEL = dict((key, label) for key, label, _c, _m in STATES)

# How a declared output type reads as a number. `Describe` in the add-in sends
# a number as its text, a collection as "N item(s) [...]" or "N entry(ies)",
# and a bool as true/false (RevitFragment.Describe).
_NUMBER_TYPES = ("int", "long", "short", "double", "float", "decimal", "Int32", "Int64", "Double")
_COLLECTION = re.compile(r"^(IList|List|ICollection|IEnumerable|IReadOnlyList|IReadOnlyCollection|"
                         r"ISet|HashSet|IDictionary|Dictionary|IReadOnlyDictionary)<")
_ID_LIST = re.compile(r"^(IList|List|ICollection|IEnumerable|IReadOnlyList|IReadOnlyCollection|ISet|"
                      r"HashSet)<ElementId>$")
_ID_KEYS = re.compile(r"^(IDictionary|Dictionary|IReadOnlyDictionary)<ElementId,")
_ITEMS = re.compile(r"^(\d+) (?:item\(s\)|entry\(ies\))")
_SAMPLE = re.compile(r"\[(.*)\]\s*$")


class ChecklistError(Exception):
    """A checklist that cannot be saved, found or run - the message says why
    in the modeller's words."""


# ---------------------------------------------------------------- the folder

def folder(home=None, make=False):
    """`<home>/checklists/personal` - the saved commands' data home (D-135)."""
    return COMMANDS.store_folder(FOLDER, home, make)


slug = COMMANDS.slug


# ---------------------------------------------------------------- the shape

def _shape(type_text):
    return re.sub(r"\s+", "", str(type_text or ""))


def output_kind(type_text):
    """'number', 'flag' or 'count' when a declared output reads as a number,
    else None - text, a point or an element cannot be compared with one."""
    shape = _shape(type_text)
    if shape in _NUMBER_TYPES:
        return "number"
    if shape in ("bool", "Boolean"):
        return "flag"
    if _COLLECTION.match(shape):
        return "count"
    return None


def ids_kind(type_text):
    """'list' for a list of element ids, 'keys' for a dictionary keyed by
    them, else None. A list of ELEMENTS is not ids: the add-in sends their
    names, never their ids, so nothing on this side could select them."""
    shape = _shape(type_text)
    if _ID_LIST.match(shape):
        return "list"
    if _ID_KEYS.match(shape):
        return "keys"
    return None


def parse_rule(text):
    """'failures == 0' -> ('failures', '==', 0). ChecklistError otherwise."""
    found = RULE.match(text or "")
    if not found:
        raise ChecklistError("The rule '%s' must be one output, one of %s and a number - "
                             "'failures == 0', say." % (text or "", " ".join(OPERATORS)))
    number = float(found.group(3))
    return found.group(1), found.group(2), int(number) if number.is_integer() else number


def _number(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def _normalised(check):
    """A check as the chat gave it, with a `rule` text read into its three
    parts. Never raises: what it cannot read stays for problems() to name."""
    check = dict(check or {}) if isinstance(check, dict) else {}
    if check.get("rule") and not check.get("output"):
        try:
            check["output"], check["op"], check["target"] = parse_rule(check["rule"])
        except ChecklistError as why:
            check["ruleProblem"] = str(why)
    return check


def _required(needs):
    """The request values a fragment cannot run without - those its card does
    not mark optional. The add-in refuses an absent request need unless it is
    marked so (heron_fragment.need_may_be_absent's rule), so leaving one out
    would make every run of the check NOT RUN."""
    return [n.get("name") for n in needs or []
            if n.get("source") == "request" and str(n.get("optional")).strip().lower() != "true"]


def check_problems(check, contract, where):
    """Every reason one check cannot be saved. `contract` is what the server
    read from the fragment's card: {"risk", "needs", "provides", "selection"}
    - None when nothing in the library is called that."""
    found = []
    sentence = (check.get("sentence") or "").strip()
    capability = (check.get("capability") or "").strip().upper()
    if not sentence:
        found.append("%s has no sentence - the modeller's own words for what it checks." % where)
    elif len(sentence) > SENTENCE_MAX or "\n" in sentence or "\r" in sentence:
        found.append("%s: the sentence must be one line of up to %d characters." % (where, SENTENCE_MAX))
    if not capability:
        found.append("%s names no capability." % where)
        return found
    if contract is None:
        found.append("%s: Heron has nothing called %s." % (where, capability))
        return found
    risk = (contract.get("risk") or "").strip().upper()
    if risk not in READS:
        found.append("%s: %s is declared %s, and a check may only read (READ or ANALYZE)."
                     % (where, capability, risk or "with no risk Heron can read"))
        return found
    needs = contract.get("needs")
    if needs is None:
        found.append("%s: Heron could not read what %s takes, so it cannot be checked." % (where, capability))
        return found

    # THE SAVED COMMAND'S OWN VALUE CHECK, CALLED (docs/49): a value the
    # fragment does not take, one given twice, one over two lines - and an
    # {input}, which a checklist has none of.
    values = check.get("values") or []
    as_command = {"name": "check", "tool": "read", "capability": capability,
                  "values": values, "inputs": []}
    for problem in COMMANDS.problems(as_command, needs):
        found.append("%s: %s" % (where, problem))
    given = set((v or {}).get("name") for v in values)
    for name in _required(needs):
        if name not in given:
            found.append("%s: %s cannot run without a value for '%s'." % (where, capability, name))

    provides = contract.get("provides")
    if provides is None:
        found.append("%s: Heron could not read what %s leaves behind." % (where, capability))
        return found
    declared = dict((p.get("name"), p.get("type")) for p in provides if p.get("name"))
    leaves = ", ".join(sorted(declared)) or "nothing"
    if check.get("ruleProblem"):
        found.append("%s: %s" % (where, check["ruleProblem"]))
    else:
        output = check.get("output") or ""
        if not output:
            found.append("%s has no rule - one output, a comparison and a number, 'failures == 0'."
                         % where)
        elif output not in declared:
            found.append("%s: '%s' is not something %s leaves behind - it leaves: %s."
                         % (where, output, capability, leaves))
        elif output_kind(declared[output]) is None:
            found.append("%s: '%s' is %s, not a number Heron can compare." % (where, output, declared[output]))
        if check.get("op") not in OPERATORS:
            found.append("%s: the comparison must be one of %s, not '%s'."
                         % (where, " ".join(OPERATORS), check.get("op")))
        if not _number(check.get("target")):
            found.append("%s: the rule must compare with a number, not '%s'." % (where, check.get("target")))
    ids = check.get("ids") or ""
    if ids:
        if ids not in declared:
            found.append("%s: '%s' is not something %s leaves behind - it leaves: %s."
                         % (where, ids, capability, leaves))
        elif ids_kind(declared[ids]) is None:
            if "Element>" in _shape(declared[ids]):
                found.append("%s: '%s' holds elements, and Revit sends back their names, not their "
                             "ids, so Heron could not select them. Leave the ids out, or name an "
                             "output that holds element ids." % (where, ids))
            else:
                found.append("%s: '%s' is %s, which holds no element ids." % (where, ids, declared[ids]))
    return found


def problems(checklist, contracts):
    """Every reason `checklist` cannot be saved as it is, in plain words. Empty
    when it can. `contracts` maps each CAPABILITY to check_problems' contract."""
    if not isinstance(checklist, dict):
        return ["A checklist must be a set of named fields."]
    found = []
    name = (checklist.get("name") or "").strip()
    if not name:
        found.append("It has no name. Give it the name the modeller will look for.")
    elif len(name) > NAME_MAX or not slug(name):
        found.append("The name '%s' must be 1 to %d characters with at least one letter or digit."
                     % (name, NAME_MAX))
    checks = checklist.get("checks")
    if not isinstance(checks, list) or not checks:
        found.append("It has no checks.")
        return found
    if len(checks) > MAX_CHECKS:
        found.append("It has %d checks; one checklist holds at most %d, because a run is one read "
                     "per check. Split it in two." % (len(checks), MAX_CHECKS))
    for number, check in enumerate(checks, 1):
        check = _normalised(check)
        where = "Check %d (%s)" % (number, (check.get("sentence") or "").strip()[:60] or "no sentence")
        capability = (check.get("capability") or "").strip().upper()
        found.extend(check_problems(check, (contracts or {}).get(capability), where))
    return found


# ---------------------------------------------------------------- keep and read

def _path(name, home=None, make=False):
    where = folder(home, make=make)
    if where is None:
        raise ChecklistError("This PC has no folder for Heron's data, so nothing can be "
                             "saved or read here. On Windows it is %USERPROFILE%\\.heron.")
    return os.path.join(where, slug(name) + ".json")


def save(checklist, contracts, home=None, replace=False, now=None):
    """Check `checklist` against each fragment's contract and keep it. Returns
    the path. Raises ChecklistError naming every problem, and keeps nothing."""
    found = problems(checklist, contracts)
    if found:
        raise ChecklistError("Not saved:\n  - " + "\n  - ".join(found))
    path = _path(checklist["name"], home, make=True)
    if os.path.exists(path) and not replace:
        raise ChecklistError("A checklist called '%s' is already saved. Say to replace it, or give "
                             "this one another name." % checklist["name"])
    kept_checks = []
    for check in checklist["checks"]:
        check = _normalised(check)
        capability = check["capability"].strip().upper()
        contract = contracts[capability]
        declared = dict((p.get("name"), p.get("type")) for p in contract.get("provides") or [])
        kept_checks.append({
            "sentence": check["sentence"].strip(),
            "capability": capability,
            "values": [{"name": v["name"], "value": v["value"]} for v in check.get("values") or []],
            "output": check["output"], "op": check["op"], "target": check["target"],
            "ids": check.get("ids") or "",
            "idsKind": ids_kind(declared.get(check.get("ids"))) if check.get("ids") else "",
            # WHAT IT READS FROM THE SELECTION: a need the request does not
            # fill is taken from what is selected in Revit when it runs.
            "selection": list(contract.get("selection") or []),
        })
    kept = {
        "heron-checklist": FORMAT,
        "name": checklist["name"].strip(),
        "description": (checklist.get("description") or "").strip(),
        "checks": kept_checks,
        "saved": now or time.strftime("%Y-%m-%dT%H:%M:%S"),
    }
    COMMANDS.write_whole(path, kept)
    return path


def _read(path):
    with io.open(path, "r", encoding="utf-8") as fh:
        data = json.load(fh)
    if not isinstance(data, dict) or data.get("heron-checklist") != FORMAT \
            or not isinstance(data.get("checks"), list):
        raise ValueError("not a Heron checklist of format %d" % FORMAT)
    return data


def all_checklists(home=None):
    """(checklists, unreadable) - every saved checklist sorted by name, and the
    file names that could not be read, so a broken file is named, not hidden."""
    where = folder(home)
    if where is None or not os.path.isdir(where):
        return [], []
    found, broken = [], []
    for name in sorted(os.listdir(where)):
        if not name.endswith(".json"):
            continue
        try:
            found.append(_read(os.path.join(where, name)))
        except (OSError, ValueError):
            broken.append(name)
    found.sort(key=lambda c: c.get("name", "").lower())
    return found, broken


def find(name, home=None):
    """The checklist saved under `name`, matched by its slug, or ChecklistError."""
    path = _path(name, home)
    if not os.path.isfile(path):
        have = [c["name"] for c in all_checklists(home)[0]]
        raise ChecklistError("No checklist called '%s' is saved.%s" % (
            name, " Saved: %s." % ", ".join(have) if have else " None is saved yet."))
    try:
        return _read(path)
    except (OSError, ValueError) as why:
        raise ChecklistError("The checklist '%s' could not be read: %s" % (name, why))


def delete(name, home=None):
    """Remove one saved checklist. Returns its name as it was saved."""
    checklist = find(name, home)
    os.remove(_path(name, home))
    return checklist["name"]


def rule_text(check):
    return "%s %s %s" % (check.get("output"), check.get("op"), _shown(check.get("target")))


def check_line(check):
    """One check in one line, for the chat."""
    extra = []
    if check.get("ids"):
        extra.append("failing ids from %s" % check["ids"])
    if check.get("selection"):
        extra.append("reads what is selected in Revit (%s)" % ", ".join(check["selection"]))
    return "%s - %s, %s%s" % (check.get("sentence"), check.get("capability"), rule_text(check),
                              "; " + "; ".join(extra) if extra else "")


def describe(checklist):
    """One checklist in one line, for the chat."""
    return "%s - %d check(s)%s" % (checklist.get("name"), len(checklist.get("checks") or []),
                                   ": " + checklist["description"] if checklist.get("description") else "")


def listing(home=None):
    """What the Companion's QA Checklist list draws - ready, nothing to work out."""
    found, broken = all_checklists(home)
    return {"lists": [{"name": c.get("name"), "description": c.get("description") or "",
                       "checks": len(c.get("checks") or []),
                       "sentences": [k.get("sentence") for k in c.get("checks") or []]}
                      for c in found],
            "broken": broken, "noHome": folder(home) is None}


# ---------------------------------------------------------------- one run

def as_number(value):
    """The number a value the add-in sent stands for, or None. A number is its
    text; a collection is "N item(s) [...]" or "N entry(ies)"; a flag is
    true/false, read as 1/0. "(null)" - nothing was left - is None: a rule
    cannot be judged on nothing, so the check is NOT RUN, never a pass."""
    if isinstance(value, bool):
        return 1 if value else 0
    if _number(value):
        return value
    if not isinstance(value, str):
        return None
    text = value.strip()
    if text.lower() == "true":
        return 1
    if text.lower() == "false":
        return 0
    found = _ITEMS.match(text)
    if found:
        return int(found.group(1))
    try:
        number = float(text)
    except ValueError:
        return None
    if not math.isfinite(number):
        return None
    return int(number) if number.is_integer() else number


def holds(value, op, target):
    """Whether `value op target` holds."""
    return {"==": value == target, "!=": value != target, "<": value < target,
            "<=": value <= target, ">": value > target, ">=": value >= target}[op]


def ids_from(kind, name, provides, entries):
    """(ids, total, complete) of a declared ids output, from what arrived.

    A DICTIONARY keyed by element id arrives whole: the add-in sends every
    entry beside its count (providesEntries, row 5b-1341). A LIST of ids
    arrives as its count and its first three (RevitFragment.Describe - D-26:
    a list of ids is a quantity), so it is complete only when it holds three
    or fewer. `complete` is what decides whether Select in Revit is offered."""
    total = as_number((provides or {}).get(name))
    if kind == "keys":
        held = (entries or {}).get(name)
        ids = [k for k in (held or {}) if str(k).isdigit()] if isinstance(held, dict) else []
    elif kind == "list":
        sample = _SAMPLE.search(str((provides or {}).get(name) or ""))
        parts = [p.strip() for p in sample.group(1).split(",")] if sample else []
        ids = [p for p in parts if p.isdigit()]
    else:
        return [], total, False
    total = total if isinstance(total, int) else len(ids)
    return ids, total, len(ids) == total


def _shown(value):
    if value is None:
        return "-"
    if isinstance(value, float):
        return ("%.3f" % value).rstrip("0").rstrip(".")
    return str(value)


def judge(number, check, ran):
    """One row of a run. `ran` is what `ask` said: {"ran": False, "why"} when
    nothing was read, or {"ran": True, "provides", "entries"} from the reply."""
    row = {"n": number, "sentence": check.get("sentence"), "capability": check.get("capability"),
           "rule": rule_text(check), "value": None, "shown": "-", "state": "not run", "why": "",
           "ids": [], "idsTotal": 0, "idsComplete": False,
           "selection": list(check.get("selection") or [])}
    if not ran.get("ran"):
        row["why"] = ran.get("why") or "It was not run."
        return _labelled(row)
    provides = ran.get("provides") or {}
    output = check.get("output")
    if output not in provides:
        row["why"] = ("%s did not send back '%s', so the rule could not be judged."
                      % (check.get("capability"), output))
        return _labelled(row)
    value = as_number(provides[output])
    if value is None:
        row["why"] = ("'%s' came back as '%s', not a number, so the rule could not be judged."
                      % (output, str(provides[output])[:80]))
        return _labelled(row)
    row["value"], row["shown"] = value, _shown(value)
    if holds(value, check.get("op"), check.get("target")):
        row["state"] = "pass"
        return _labelled(row)
    row["state"] = "fail"
    row["why"] = "%s read %s; the rule is %s." % (output, _shown(value), row["rule"])
    if check.get("ids"):
        ids, total, complete = ids_from(check.get("idsKind"), check["ids"], provides, ran.get("entries"))
        row["ids"], row["idsTotal"], row["idsComplete"] = (ids if complete else []), total, complete
        if total and not complete:
            row["why"] += (" Heron received %d of the %d element ids in '%s' - Revit sends a list of "
                           "ids as its count and its first three - so Select in Revit is not offered."
                           % (len(ids), total, check["ids"]))
    return _labelled(row)


def _labelled(row):
    row["label"] = LABEL[row["state"]]
    return row


def result(checklist, rows, document=None, running=False):
    """What a run says - the rows, the counts and one sentence - ready to show."""
    counts = {"pass": 0, "fail": 0, "not run": 0}
    for row in rows:
        counts[row["state"]] += 1
    total = len(checklist.get("checks") or [])
    said = ("%s%s: %s - %d meet the value checked, %d do not, %d not run."
            % (checklist.get("name"), " on %s" % document if document else "",
               ("%d of %d check(s) so far" % (len(rows), total)) if running
               else "%d check(s)" % total,
               counts["pass"], counts["fail"], counts["not run"]))
    if counts["not run"]:
        said += " A check not run is not a pass."
    return {"name": checklist.get("name"), "description": checklist.get("description") or "",
            "document": document, "at": time.strftime("%H:%M:%S"), "running": bool(running),
            "rows": rows, "counts": dict(counts, total=total, done=len(rows)), "said": said,
            "states": [{"key": k, "label": label, "colour": c, "means": m} for k, label, c, m in STATES]}


def run(checklist, ask, progress=None):
    """Run every check, one read each, in order. `ask(check)` is the server's:
    it reads through revit_read's own body and returns what judge() takes.
    `progress(result)` is told after each check, for the Companion page."""
    rows, document = [], None
    checks = checklist.get("checks") or []
    for number, check in enumerate(checks, 1):
        ran = ask(check) or {}
        document = document or ran.get("document")
        rows.append(judge(number, check, ran))
        if progress is not None and number < len(checks):
            progress(result(checklist, rows, document, running=True))
    return result(checklist, rows, document)


def text(answer):
    """A run, for the chat: one line per check, then the counts."""
    lines = [answer["said"], ""]
    for row in answer["rows"]:
        line = "%-8s %s - %s, read %s" % (row["label"], row["sentence"], row["rule"], row["shown"])
        if row["why"] and row["state"] != "pass":
            line += "\n         %s" % row["why"]
        if row["ids"]:
            line += "\n         failing element ids: %s" % ", ".join(row["ids"][:20]) + (
                " and %d more" % (len(row["ids"]) - 20) if len(row["ids"]) > 20 else "")
        lines.append(line)
    lines.append("")
    lines.append("Nothing in the model was changed: each check is a read with no transaction "
                 "open, so there is nothing to undo, keep or discard.")
    return "\n".join(lines)


# ---------------------------------------------------------------- the starter

#: AN EXAMPLE TO COPY, NEVER SAVED BY ITSELF. The chat saves it under the
#: modeller's own name only when asked (heron_checklist action="starter"),
#: through save() and its checks like any other. Every check is a PROVEN read
#: that collects its own elements - none reads the selection - with an output
#: that is a number and one that holds element ids. Each rule is a starting
#: value: the modeller decides what their model should meet.
STARTER = {
    "name": "Model housekeeping",
    "description": "Links loaded, files found, rooms and Spaces placed and enclosed",
    "checks": [
        {"sentence": "every linked model is loaded", "capability": "LIST_LINKED_MODELS",
         "values": [], "rule": "notLoaded == 0", "ids": "notLoaded"},
        {"sentence": "every file the model references is found", "capability":
         "REPORT_EXTERNAL_REFERENCES", "values": [], "rule": "unresolved == 0", "ids": "unresolved"},
        {"sentence": "no room is left unplaced", "capability": "SELECT_UNENCLOSED_ROOMS",
         "values": [{"name": "includeRooms", "value": "true"},
                    {"name": "includeSpaces", "value": "false"}],
         "rule": "unplaced == 0", "ids": "unplaced"},
        {"sentence": "no room is left unenclosed", "capability": "SELECT_UNENCLOSED_ROOMS",
         "values": [{"name": "includeRooms", "value": "true"},
                    {"name": "includeSpaces", "value": "false"}],
         "rule": "unenclosed == 0", "ids": "unenclosed"},
        {"sentence": "no Space is left unplaced or unenclosed", "capability": "SELECT_UNENCLOSED_ROOMS",
         "values": [{"name": "includeRooms", "value": "false"},
                    {"name": "includeSpaces", "value": "true"}],
         "rule": "elements == 0"},
    ],
}


def starter(name=None):
    """A copy of STARTER under `name`, for save()."""
    copy = json.loads(json.dumps(STARTER))
    if (name or "").strip():
        copy["name"] = name.strip()
    return copy


def usable(fragments):
    """
    Which capabilities a check can use today, read from the cards - so no
    document types a list that goes stale: PROVEN or PRODUCTION, a proof that
    still describes the code, declared READ or ANALYZE, and at least one output
    that reads as a number. For each, (capability, number outputs, ids
    outputs, what it reads from the selection), sorted by capability.

    `fragments` is heron_fragment.load_all()'s answer (it needs PyYAML, so it
    is asked for only by --fragments, never by the store or the run).
    """
    import heron_fragment as FRAGMENT
    found = []
    for frag in fragments:
        if str(frag.status or "").upper() not in ("PROVEN", "PRODUCTION") or frag.proof_is_stale():
            continue
        if str(frag.data.get("risk") or "").upper() not in READS:
            continue
        provides = frag.provides()
        numbers = [p.get("name") for p in provides if output_kind(p.get("type"))]
        if not numbers:
            continue
        ids = [p.get("name") for p in provides if ids_kind(p.get("type"))]
        selection = [n.get("name") for n in frag.needs() if FRAGMENT.need_source(n) == "fragment"
                     and str(n.get("optional")).strip().lower() != "true"]
        found.append((frag.data.get("capability"), numbers, ids, selection))
    return sorted(found)


def main(argv):
    if "--fragments" in argv:
        import heron_fragment as FRAGMENT
        loaded, _problems = FRAGMENT.load_all()
        rows = usable(loaded.values())
        for capability, numbers, ids, selection in rows:
            print("%-38s rule on: %s" % (capability, ", ".join(numbers)))
            if ids:
                print("%-38s ids in:  %s" % ("", ", ".join(ids)))
            if selection:
                print("%-38s reads the selection: %s" % ("", ", ".join(selection)))
        print("%d capability(ies) a check can use today; %d with an output of element ids; "
              "%d of those collect their own elements" % (
                  len(rows), sum(1 for r in rows if r[2]), sum(1 for r in rows if r[2] and not r[3])))
        return 0
    if folder() is None:
        print("This PC has no folder for Heron's data.")
        return 0
    found, broken = all_checklists()
    if not found:
        print("No saved checklists in %s" % folder())
    for checklist in found:
        print(describe(checklist))
        for check in checklist.get("checks") or []:
            print("    " + check_line(check))
    for name in broken:
        print("could not read %s" % name)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
