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

import hashlib
import io
import json
import math
import os
import re
import shutil
import sys
import time
import uuid

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
RULE = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*(==|!=|<=|>=|<|>)\s*(-?\d+(?:\.\d+)?|true|false)\s*$",
                  re.I)

# Each result's colour, decided here and drawn on the Companion page. THE
# BRAND'S COLOURS (D-149, docs/brand/README.md): Success, Error, and Warning
# for a check that did not run - it needs a look, and must never read as a
# pass.
STATES = (("pass", "PASS", "#3ecf8e", "meets the value checked"),
          ("fail", "FAIL", "#ff5a4e", "does not meet the value checked"),
          ("not run", "NOT RUN", "#f5a524", "not run - see why; never counted as a pass"))
LABEL = dict((key, label) for key, label, _c, _m in STATES)

# HOW MUCH A FAILURE MATTERS, AND WHERE THE RULE COMES FROM (item 4 of the
# plan after D-155's comparison). Each is optional and the modeller's own:
# Heron never sets a severity, and a clause is quoted, never checked.
SEVERITIES = ("high", "medium", "low")
CLAUSE_MAX = 200
ADVICE_MAX = 300

# WHAT CHANGED SINCE THE LAST RUN of the same checklist on the same model. A
# run is kept once it finished; the newest RUNS_KEPT of a checklist ON ONE
# MODEL are kept, each model in its own folder. A model is its file path: a
# model with none (never saved, or detached and not saved) is never compared
# and its runs are not kept. RUNS_FORMAT 2 notes each check's values; a run
# kept as format 1 still reads, and is never compared with (row 5b-1613).
RUNS_FOLDER = "runs"
RUNS_FORMAT = 2
RUNS_READ = (1, 2)
RUNS_KEPT = 20
CHANGES = (("new", "NEW", "failed now, and did not last time"),
           ("open", "STILL OPEN", "failed last time too"),
           ("fixed", "FIXED", "failed last time, meets the value checked now"),
           ("", "", "met the value checked both times"),
           ("unknown", "NOT COMPARED", "not run, now or last time, so nothing can be said of it"))
CHANGE_LABEL = dict((key, label) for key, label, _m in CHANGES)

# How a declared output type reads as a number - ONLY what the add-in's
# RevitFragment.Describe renders as one: an int, a long or a double as its
# text, a bool as true/false, and a collection as "N item(s)" or "N entry(ies)"
# - but only one that is a non-generic ICollection or IDictionary at run time.
# A float, a short or a decimal is none of Describe's number types and arrives
# as its type name; a HashSet or an ISet is not a non-generic ICollection and
# arrives as "HashSet`1". A check on one could never run, so it is refused when
# saved rather than NOT RUN every time.
_NUMBER_TYPES = ("int", "long", "double", "Int32", "Int64", "Double")
_FLAG_TYPES = ("bool", "Boolean")
_COLLECTION = re.compile(r"^((IList|List|ICollection|IReadOnlyList|IReadOnlyCollection|IDictionary|"
                         r"Dictionary|IReadOnlyDictionary|SortedDictionary)<.*>|[A-Za-z0-9_.]+\[\])$")
_ID_LIST = re.compile(r"^((IList|List|ICollection|IReadOnlyList|IReadOnlyCollection)<ElementId>|"
                      r"ElementId\[\])$")
_ID_KEYS = re.compile(r"^(IDictionary|Dictionary|IReadOnlyDictionary|SortedDictionary)<ElementId,")
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
    if shape in _FLAG_TYPES:
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


def _target(value):
    """A rule's number as the chat or a file gave it: a number, a numeric
    text such as "0", or true/false read as 1/0 - else None."""
    if isinstance(value, bool):
        return 1 if value else 0
    if _number(value):
        return int(value) if float(value).is_integer() else value
    if isinstance(value, str):
        text = value.strip().lower()
        if text in ("true", "false"):
            return 1 if text == "true" else 0
        try:
            number = float(text)
        except ValueError:
            return None
        if math.isfinite(number):
            return int(number) if number.is_integer() else number
    return None


def parse_rule(text):
    """'failures == 0' -> ('failures', '==', 0); 'workshared == true' -> 1.
    ChecklistError otherwise."""
    found = RULE.match(text) if isinstance(text, str) else None
    if not found:
        raise ChecklistError("The rule '%s' must be one output, one of %s and a number or "
                             "true/false - 'failures == 0', say." % (text or "", " ".join(OPERATORS)))
    return found.group(1), found.group(2), _target(found.group(3))


def _number(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def _text(value):
    """`value` stripped when it is text, else None - never an error."""
    return value.strip() if isinstance(value, str) else None


def _normalised(check):
    """A check as the chat gave it, with a `rule` text read into its three
    parts and a typed target read as a number. Never raises: what it cannot
    read stays for problems() to name."""
    if not isinstance(check, dict):
        return {"shapeProblem": "is not a set of named fields - sentence, capability, values, rule"}
    check = dict(check)
    parts = [k for k in ("output", "op", "target") if check.get(k) not in (None, "")]
    if check.get("rule") not in (None, ""):
        if parts:
            check["ruleProblem"] = ("give the rule once - as one text, 'failures == 0', or as output, "
                                    "op and target - not both")
        else:
            try:
                check["output"], check["op"], check["target"] = parse_rule(check["rule"])
            except ChecklistError as why:
                check["ruleProblem"] = str(why)
    elif "target" in check and _target(check.get("target")) is not None:
        check["target"] = _target(check["target"])
    return check


def _values_problem(values):
    """Why `values` is not a list of {name, value} texts, or None."""
    if not isinstance(values, list):
        return "its values must be a list of name and value pairs"
    for entry in values:
        if not isinstance(entry, dict) or not isinstance(entry.get("name"), str) \
                or not isinstance(entry.get("value"), str):
            return "each of its values must be a name and a value, both text"
    return None


def check_problems(check, contract, where):
    """Every reason one check cannot be saved. `contract` is what the server
    read from the fragment's card: {"risk", "needs", "provides", "selection"}
    - None when nothing in the library is called that."""
    found = []
    if check.get("shapeProblem"):
        return ["%s %s." % (where, check["shapeProblem"])]
    sentence = _text(check.get("sentence"))
    capability = (_text(check.get("capability")) or "").upper()
    if sentence is None and check.get("sentence") not in (None, ""):
        found.append("%s: the sentence must be text." % where)
        sentence = "-"
    if check.get("capability") not in (None, "") and _text(check.get("capability")) is None:
        found.append("%s: the capability must be its name, as text." % where)
        return found
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
    values = check.get("values") if check.get("values") is not None else []
    shape = _values_problem(values)
    if shape:
        found.append("%s: %s." % (where, shape))
        return found
    as_command = {"name": "check", "tool": "read", "capability": capability,
                  "values": values, "inputs": []}
    for problem in COMMANDS.problems(as_command, needs):
        found.append("%s: %s" % (where, problem))
    # WHAT IT CANNOT RUN WITHOUT is the server's to say, from the card through
    # heron_fragment.need_may_be_absent - the add-in's own binding rule,
    # mirrored there once - and never a second copy written here.
    required = contract.get("required")
    if required is None:
        found.append("%s: Heron could not read which values %s cannot run without." % (where, capability))
        return found
    given = set(v["name"] for v in values)
    for name in required:
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
        if not isinstance(output, str):
            found.append("%s: the rule's output must be its name, as text." % where)
        elif not output:
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
    if not isinstance(ids, str):
        found.append("%s: 'ids' must name one output, as text." % where)
    elif ids:
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
    found.extend(_labels_problems(check, where))
    return found


def _labels_problems(check, where):
    """Why a check's severity, clause or advice cannot be kept - each optional,
    each one line of the modeller's own words."""
    found = []
    severity = check.get("severity")
    if severity not in (None, ""):
        if not isinstance(severity, str) or severity.strip().lower() not in SEVERITIES:
            found.append("%s: the severity must be one of %s, not '%s'."
                         % (where, ", ".join(SEVERITIES), severity))
    for key, most, what in (("clause", CLAUSE_MAX, "the BEP or standard clause it comes from"),
                            ("advice", ADVICE_MAX, "how to fix a failure")):
        value = check.get(key)
        if value in (None, ""):
            continue
        if not isinstance(value, str):
            found.append("%s: '%s' - %s - must be text." % (where, key, what))
        elif len(value.strip()) > most or "\n" in value.strip() or "\r" in value.strip():
            found.append("%s: '%s' must be one line of at most %d characters." % (where, key, most))
    return found


def problems(checklist, contracts):
    """Every reason `checklist` cannot be saved as it is, in plain words. Empty
    when it can. `contracts` maps each CAPABILITY to check_problems' contract."""
    if not isinstance(checklist, dict):
        return ["A checklist must be a set of named fields."]
    found = []
    name = _text(checklist.get("name")) or ""
    if checklist.get("name") not in (None, "") and _text(checklist.get("name")) is None:
        found.append("The name must be text.")
    elif not name:
        found.append("It has no name. Give it the name the modeller will look for.")
    elif len(name) > NAME_MAX or not slug(name):
        found.append("The name '%s' must be 1 to %d characters with at least one letter or digit."
                     % (name, NAME_MAX))
    if checklist.get("description") is not None and not isinstance(checklist.get("description"), str):
        found.append("The description must be one line of text.")
    checks = checklist.get("checks")
    if not isinstance(checks, list) or not checks:
        found.append("It has no checks.")
        return found
    if len(checks) > MAX_CHECKS:
        found.append("It has %d checks; one checklist holds at most %d, because a run is one read "
                     "per check. Split it in two." % (len(checks), MAX_CHECKS))
    for number, check in enumerate(checks, 1):
        check = _normalised(check)
        where = "Check %d (%s)" % (number, (_text(check.get("sentence")) or "")[:60] or "no sentence")
        capability = (_text(check.get("capability")) or "").upper()
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
            # The modeller's own labels, each optional (the plan's item 4).
            "severity": (check.get("severity") or "").strip().lower(),
            "clause": (check.get("clause") or "").strip(),
            "advice": (check.get("advice") or "").strip(),
            # WHAT IT READS FROM THE SELECTION: a need the request does not
            # fill is taken from what is selected in Revit when it runs.
            "selection": list(contract.get("selection") or []),
        })
    kept = {
        "heron-checklist": FORMAT,
        "name": checklist["name"].strip(),
        "description": _text(checklist.get("description")) or "",
        "checks": kept_checks,
        "saved": now or time.strftime("%Y-%m-%dT%H:%M:%S"),
    }
    COMMANDS.write_whole(path, kept)
    return path


def _check_shape(check):
    """Why one saved check cannot be run as it is, or None - the fields every
    reader below takes for granted (list, the page, judge)."""
    if not isinstance(check, dict):
        return "a check is not a set of named fields"
    for key in ("sentence", "capability", "output"):
        if not isinstance(check.get(key), str) or not check[key].strip():
            return "a check has no %s" % key
    if _values_problem(check.get("values")):
        return "a check's values are not name and value texts"
    if check.get("op") not in OPERATORS:
        return "a check's comparison is not one of %s" % " ".join(OPERATORS)
    if not _number(check.get("target")):
        return "a check's rule compares with no number"
    if not isinstance(check.get("ids", ""), str) or check.get("idsKind", "") not in ("", "list", "keys"):
        return "a check's ids are not named as text"
    selection = check.get("selection", [])
    if not isinstance(selection, list) or not all(isinstance(n, str) for n in selection):
        return "a check's selection is not a list of names"
    if check.get("severity", "") not in ("",) + SEVERITIES:
        return "a check's severity is not one of %s" % ", ".join(SEVERITIES)
    if not all(isinstance(check.get(key, ""), str) for key in ("clause", "advice")):
        return "a check's clause or advice is not text"
    # THE SAVE'S OWN LIMITS, ON LOAD TOO (row 5b-1618): a file made by hand or
    # shared from another PC never reached save(), and its words are printed
    # into the chat. One that breaks them is refused like any other bad field.
    sentence = check["sentence"].strip()
    if len(sentence) > SENTENCE_MAX or "\n" in sentence or "\r" in sentence:
        return "a check's sentence is not one line of up to %d characters" % SENTENCE_MAX
    labels = _labels_problems(check, "a check")
    if labels:
        return "a check breaks the limits a save holds it to - %s" % labels[0][len("a check: "):].rstrip(".")
    return None


def _read(path):
    """One saved checklist, its WHOLE shape checked - ValueError otherwise, so
    a file somebody edited by hand lands in the broken list and never stops
    the list, the chat or the Companion's poll."""
    with io.open(path, "r", encoding="utf-8") as fh:
        data = json.load(fh)
    if not isinstance(data, dict) or data.get("heron-checklist") != FORMAT:
        raise ValueError("not a Heron checklist of format %d" % FORMAT)
    if not isinstance(data.get("name"), str) or not data["name"].strip():
        raise ValueError("it has no name")
    if not isinstance(data.get("description", ""), str):
        raise ValueError("its description is not text")
    checks = data.get("checks")
    if not isinstance(checks, list) or not checks:
        raise ValueError("it has no checks")
    for check in checks:
        why = _check_shape(check)
        if why:
            raise ValueError(why)
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
    """Remove one saved checklist AND THE RUNS KEPT OF IT (row 5b-1623) - each
    holds the model's full path, the check sentences and the failing ids, and
    a list saved again under the same name would be compared with the old
    one's runs. Returns its name as it was saved. A runs folder that cannot be
    removed is said, never hidden: the list is gone, the runs are not."""
    checklist = find(name, home)
    os.remove(_path(name, home))
    runs = runs_folder(name, home)
    if runs and os.path.isdir(runs):
        try:
            shutil.rmtree(runs)
        except OSError as why:
            raise ChecklistError("The checklist '%s' was deleted, but the runs kept of it could "
                                 "not be removed from %s: %s" % (checklist["name"], runs, why))
    return checklist["name"]


def rule_text(check):
    return "%s %s %s" % (check.get("output"), check.get("op"), _shown(check.get("target")))


def check_line(check):
    """One check in one line, for the chat."""
    extra = []
    if check.get("severity"):
        extra.append("severity %s" % check["severity"])
    if check.get("clause"):
        extra.append("clause %s" % check["clause"])
    if check.get("advice"):
        extra.append("advice: %s" % check["advice"])
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


def ids_from(kind, name, provides, entries, sent=None):
    """(ids, total, complete) of a declared ids output, from what arrived.

    A DICTIONARY keyed by element id arrives whole: the add-in sends every
    entry beside its count (providesEntries, row 5b-1341). A LIST of ids
    arrives as its count and its first three (RevitFragment.Describe - D-26:
    a list of ids is a quantity) - UNLESS the run asked for it by name, when
    the add-in sends it whole in `providesIds` with its total (D-158, `sent`
    here), complete up to the add-in's IdsSentPerList. Without that - an
    add-in from before it - a list is complete only when it holds three or
    fewer. `complete` is what decides whether Select in Revit is offered."""
    total = as_number((provides or {}).get(name))
    whole = (sent or {}).get(name) if kind == "list" else None
    if isinstance(whole, dict) and isinstance(whole.get("ids"), list):
        ids = [str(i) for i in whole["ids"] if str(i).lstrip("-").isdigit()]
        count = whole.get("total")
        if not isinstance(count, int) or isinstance(count, bool) or count < 0:
            # NO COUNT, NO SELECT (row 5b-1620): without the total beside them
            # nothing says these ids are all of them - so they are never
            # taken as complete. The total shown is the reply's own count.
            return ids, total if isinstance(total, int) else len(ids), False
        return ids, count, len(ids) == count
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
           "selection": list(check.get("selection") or []),
           # Its values, so an edited check is never matched with the old one.
           "values": ["%s=%s" % (v.get("name"), v.get("value")) for v in check.get("values") or []
                      if isinstance(v, dict)],
           "severity": check.get("severity") or "", "clause": check.get("clause") or "",
           "advice": check.get("advice") or "", "change": "", "changeLabel": "", "changeWhy": ""}
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
        ids, total, complete = ids_from(check.get("idsKind"), check["ids"], provides,
                                        ran.get("entries"), ran.get("ids"))
        row["ids"], row["idsTotal"], row["idsComplete"] = (ids if complete else []), total, complete
        sent = (ran.get("ids") or {}).get(check["ids"])
        if isinstance(sent, dict) and isinstance(sent.get("ids"), list) and not complete and (
                not isinstance(sent.get("total"), int) or isinstance(sent.get("total"), bool)):
            row["why"] += (" Revit sent %d element id(s) in '%s' without saying how many the list "
                           "holds, so Select in Revit is not offered rather than selecting part of "
                           "what failed." % (len(ids), check["ids"]))
        elif total and not complete:
            row["why"] += (" Heron received %d of the %d element ids in '%s', so Select in Revit is "
                           "not offered rather than selecting part of what failed. %s"
                           % (len(ids), total, check["ids"],
                              "Revit sent the first %d - the most it sends of one list." % len(ids)
                              if (ran.get("ids") or {}).get(check["ids"]) else
                              "This Heron in Revit sends a list of ids as its count and its first "
                              "three; an updated one sends them all."))
    return _labelled(row)


def _labelled(row):
    row["label"] = LABEL[row["state"]]
    return row


CHANGED_DURING = ("The model changed in Revit while the checklist ran, so its checks did not all "
                  "read the same model. Press Run again for results from one state of it.")


def new_token():
    """The id of one run. The Companion's Select sends it back, so a row's
    ids are never taken from a run that replaced the one on the page."""
    return uuid.uuid4().hex[:12]


def result(checklist, rows, document=None, running=False, token=None, findings=None, stopped=""):
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
    if stopped:
        said += " " + stopped
    return {"name": checklist.get("name"), "description": checklist.get("description") or "",
            "document": document, "at": time.strftime("%H:%M:%S"), "running": bool(running),
            "run": token or new_token(), "findings": list(findings or []), "stopped": stopped or "",
            "rows": rows, "counts": dict(counts, total=total, done=len(rows)), "said": said,
            "states": [{"key": k, "label": label, "colour": c, "means": m} for k, label, c, m in STATES],
            "changes": [{"key": k, "label": label, "means": m} for k, label, m in CHANGES if k],
            "history": None}


def finish(checklist, rows, document, why, token=None, findings=None):
    """A run that stopped part way: the rows it has, every check after them
    NOT RUN with `why`, and `running` False - so nothing ever shows "Running"
    for good, and a stopped run is never read as a finished one."""
    rows = list(rows)
    checks = checklist.get("checks") or []
    for number, check in enumerate(checks[len(rows):], len(rows) + 1):
        rows.append(judge(number, check, {"ran": False, "why": why}))
    return result(checklist, rows, document, running=False, token=token, findings=findings, stopped=why)


def run(checklist, ask, progress=None):
    """Run every check, one read each, in order. `ask(check)` is the server's:
    it reads through revit_read's own body and returns what judge() takes,
    with `changes` - the model's change count after the read, when known.
    `progress(result)` is told after each check, for the Companion page.

    A FAULT IN ONE ASK ENDS THE RUN IN WORDS, never as an exception half way:
    the checks not reached are NOT RUN saying why, and the result is final."""
    token, rows, document, findings, first = new_token(), [], None, [], None
    checks = checklist.get("checks") or []
    for number, check in enumerate(checks, 1):
        try:
            ran = ask(check) or {}
        except Exception as why:                     # noqa: BLE001 - a run never ends "running"
            return finish(checklist, rows, document,
                          "The run stopped at check %d: %s. Nothing more was sent to Revit."
                          % (number, why), token, findings)
        document = document or ran.get("document")
        changes = ran.get("changes")
        if isinstance(changes, int) and not isinstance(changes, bool):
            if first is None:
                first = changes
            elif changes != first and CHANGED_DURING not in findings:
                findings.append(CHANGED_DURING)
        rows.append(judge(number, check, ran))
        if progress is not None and number < len(checks):
            progress(result(checklist, rows, document, running=True, token=token, findings=findings))
    return result(checklist, rows, document, token=token, findings=findings)


def text(answer):
    """A run, for the chat: one line per check, then the counts."""
    lines = [answer["said"], ""]
    for row in answer["rows"]:
        line = "%-8s %s - %s, read %s" % (row["label"], row["sentence"], row["rule"], row["shown"])
        tags = [t for t in (row.get("changeLabel"),
                            row.get("severity") and "severity " + row["severity"],
                            row.get("clause") and "clause " + row["clause"]) if t]
        if tags:
            line += "  [%s]" % ", ".join(tags)
        if row["why"] and row["state"] != "pass":
            line += "\n         %s" % row["why"]
        if row.get("changeWhy"):
            line += "\n         %s" % row["changeWhy"]
        if row.get("advice") and row["state"] == "fail":
            line += "\n         to fix: %s" % row["advice"]
        if row["ids"]:
            line += "\n         failing element ids: %s" % ", ".join(row["ids"][:20]) + (
                " and %d more" % (len(row["ids"]) - 20) if len(row["ids"]) > 20 else "")
        lines.append(line)
    for finding in answer.get("findings") or []:
        lines.append("")
        lines.append(finding)
    lines.append("")
    lines.append("Nothing in the model was changed: each check is a read with no transaction "
                 "open, so there is nothing to undo, keep or discard.")
    return "\n".join(lines)


# ---------------------------------------------------------------- what changed

def runs_folder(name, home=None, make=False):
    """`<home>/checklists/runs/<slug of the checklist>` - beside the saved
    checklists, never inside them, so a run is never read as a checklist."""
    personal = folder(home, make=make)
    if personal is None:
        return None
    where = os.path.join(os.path.dirname(personal), RUNS_FOLDER, slug(name))
    if make and not os.path.isdir(where):
        os.makedirs(where)
    return where


# WHY A RUN, OR ONE ROW OF IT, IS NOT COMPARED - each a true sentence the
# modeller can read. The rule for every doubt: NOT COMPARED, never a guess at
# FIXED or NEW (the review of #866, rows 5b-1613 to 5b-1616).
UNSAVED = ("Not compared: this model has no file path - it was never saved, or was detached "
           "and not saved - so its runs cannot be told from another model's. Save the model so "
           "runs can be compared. This run is not kept.")
NO_MODEL = ("Not compared, and not kept: no model was read in this run, so there is nothing to "
            "compare.")
FIRST = ("This is the first kept run of this checklist on this model, so there is nothing to "
         "compare with yet.")
OLD_FORMAT = ("Not compared: the last run was kept by an earlier Heron, which did not note each "
              "check's values, so whether this check was edited since cannot be told.")
EDITED = ("Not compared: no check with these words, capability, rule and values was in the last "
          "run - it was added or edited since.")
TWINS = ("Not compared: another check in this checklist has the same words, capability, rule and "
         "values, so the two cannot be told apart from one run to the next.")
SELECTION = ("Not compared: this check reads what is selected in Revit, and what was selected "
             "may not be what was selected last time.")
NOT_RUN = "Not compared: it was not run now or last time."
UNREADABLE = "Not compared: the run kept at %s could not be read (%s), and it may be the last " \
             "run of this checklist on this model, so nothing is compared across it."


def _model(answer):
    """["path", <the model's file path>] - what tells one model from another
    in a kept run - or None when the run has none to give.

    THE PATH ALONE (row 5b-1614). Revit's title is the file name of that path,
    so it adds nothing; Revit's process id changes every time Revit is
    started, so it is never part of it - the same file opened again next week
    is the same model. A Save As gives a new path, and is a new model. A model
    with no path - never saved, or detached and not saved - is None: two
    unsaved Project1s cannot be told apart, so they are never compared."""
    identity = answer.get("identity")
    if isinstance(identity, (list, tuple)) and len(identity) >= 2:
        path = str(identity[1] or "").strip()
        if path:
            return ["path", path]
    return None


def _why_no_model(answer):
    identity = answer.get("identity")
    return UNSAVED if isinstance(identity, (list, tuple)) and any(identity) else NO_MODEL


def _kept_model(kept):
    """The model a kept run was made on, as _model gives it - reading a run kept
    before the path alone was the identity (["identity", title, path, pid]) by
    its path. None when it cannot be told."""
    model = kept.get("model")
    if isinstance(model, list):
        if len(model) == 2 and model[0] == "path" and isinstance(model[1], str) and model[1].strip():
            return ["path", model[1].strip()]
        if len(model) == 4 and model[0] == "identity" and isinstance(model[2], str) \
                and model[2].strip():
            return ["path", model[2].strip()]
    return None


def _model_folder(base, model):
    """One model's own folder under a checklist's runs - named by a digest of
    its path, so a path is never a folder name."""
    return os.path.join(base, hashlib.sha1(model[1].encode("utf-8")).hexdigest()[:16])


def _key(row):
    """One check across two runs: its sentence, capability, rule AND VALUES,
    sorted - so a check reworded, given another rule or edited to other values
    is another check, never a false 'fixed' (row 5b-1613)."""
    return json.dumps([row.get("sentence"), row.get("capability"), row.get("rule"),
                       sorted(str(v) for v in row.get("values") or [])])


def _kept_row(row):
    return {"key": _key(row), "state": row.get("state"),
            "value": row.get("value"), "ids": list(row.get("ids") or []),
            "idsComplete": bool(row.get("idsComplete")),
            "selection": bool(row.get("selection"))}


#: "n000012-2026-11-12T14-05-09-<run>.json": the number a run was kept under,
#: then the PC's local time it was kept, for a person to read.
_NUMBERED = re.compile(r"^n(\d+)-")


def _order(file_name):
    """Where a kept run sorts among a model's others: BY THE NUMBER IT WAS KEPT
    UNDER, never by the time in its name (row 5b-1622). The PC's clock goes
    back at the end of summer time, or when it is corrected, and a run kept
    after that would sort before the one kept just before it - and be
    compared with the wrong run. A run kept before the number was added has
    none, and sorts by its name before every numbered one: it is older."""
    numbered = _NUMBERED.match(file_name)
    if numbered:
        return (1, int(numbered.group(1)), file_name)
    return (0, 0, file_name)


def _next_number(where):
    """One more than the highest number kept in this model's folder."""
    numbers = [_order(n)[1] for n in os.listdir(where) if n.endswith(".json")]
    return max(numbers + [0]) + 1


def _stamp_shown(file_name):
    """"n000012-2026-11-12T14-05-09-<run>.json" (or, kept by an earlier Heron,
    "2026-11-12T14-05-09-<run>.json") -> "2026-11-12 14:05"."""
    numbered = _NUMBERED.match(file_name)
    if numbered:
        file_name = file_name[numbered.end():]
    return file_name[:10] + " " + file_name[11:16].replace("-", ":")


def previous(answer, home=None):
    """{"run": the newest kept run of this checklist ON THIS MODEL or None,
    "why": why nothing is compared, or ""}.

    A run on another model is never compared - what changed between two
    models is not what changed in one. A KEPT RUN THAT CANNOT BE READ, newer
    than the one that would be compared with, ends the search: it may be this
    model's last run, and comparing across it would skip a run (row 5b-1615).
    The runs kept before each model had its own folder are read too, by path."""
    model = _model(answer)
    if model is None:
        return {"run": None, "why": _why_no_model(answer)}
    base = runs_folder(answer.get("name") or "", home)
    if base is None or not os.path.isdir(base):
        return {"run": None, "why": ""}
    candidates = []
    for where in (_model_folder(base, model), base):
        if os.path.isdir(where):
            candidates += [(n, os.path.join(where, n)) for n in os.listdir(where)
                           if n.endswith(".json") and os.path.isfile(os.path.join(where, n))]
    for name, path in sorted(candidates, key=lambda c: _order(c[0]), reverse=True):
        try:
            with io.open(path, "r", encoding="utf-8") as fh:
                kept = json.load(fh)
            readable = (isinstance(kept, dict) and kept.get("heron-checklist-run") in RUNS_READ
                        and isinstance(kept.get("rows"), list) and _kept_model(kept) is not None)
        except (OSError, ValueError):
            readable = False
        if not readable:
            return {"run": None, "why": UNREADABLE % (_stamp_shown(name), name), "unreadable": True}
        if _kept_model(kept) == model:
            return {"run": kept, "why": ""}
    return {"run": None, "why": ""}


def _not_compared(answer, said):
    """Every row NOT COMPARED, for a run with nothing it may be compared with."""
    for row in answer.get("rows") or []:
        row["change"], row["changeLabel"], row["changeWhy"] = "unknown", CHANGE_LABEL["unknown"], ""
    counts = {"new": 0, "open": 0, "fixed": 0, "unknown": len(answer.get("rows") or [])}
    answer["history"] = {"previous": None, "counts": counts, "said": said}
    return answer


def compare(answer, before, why_not=""):
    """Each row's change since `before` - NEW, STILL OPEN, FIXED, nothing, or
    NOT COMPARED - and `history` with the counts and one sentence. Changes the
    answer in place and returns it. With no `before`, every row is NOT
    COMPARED and `why_not` (or "the first kept run") is the sentence: a row
    that was not compared never reads as one that met the value both times.

    NOT RUN IS NEVER A CHANGE, and neither is anything that cannot be told: a
    check not run now or last time, one not in the last run (added or edited
    since), two checks that cannot be told apart, and a check that reads the
    selection - which is whatever was selected at that moment - are each NOT
    COMPARED, with why."""
    if not before:
        return _not_compared(answer, why_not or FIRST)
    counts = {"new": 0, "open": 0, "fixed": 0, "unknown": 0}
    old_format = before.get("heron-checklist-run") != RUNS_FORMAT
    earlier, twice = {}, set()
    for kept in before.get("rows") or []:
        if isinstance(kept, dict):
            if kept.get("key") in earlier:
                twice.add(kept.get("key"))
            earlier[kept.get("key")] = kept
    seen = set()
    for row in answer.get("rows") or []:
        key = _key(row)
        if key in seen:
            twice.add(key)
        seen.add(key)
    for row in answer.get("rows") or []:
        key = _key(row)
        then = earlier.get(key)
        now = row.get("state")
        why = ""
        if old_format:
            change, why = "unknown", OLD_FORMAT
        elif row.get("selection") or (then or {}).get("selection"):
            change, why = "unknown", SELECTION
        elif key in twice:
            change, why = "unknown", TWINS
        elif then is None:
            change, why = "unknown", EDITED
        elif now == "not run" or then.get("state") == "not run":
            change, why = "unknown", NOT_RUN
        elif now == "fail" and then.get("state") == "fail":
            change = "open"
            ids, old = row.get("ids") or [], then.get("ids") or []
            if row.get("idsComplete") and then.get("idsComplete"):
                added = len(set(ids) - set(old))
                gone = len(set(old) - set(ids))
                # "No longer failing", never "fixed": an element deleted since
                # is gone from the list too (row 5b-1619).
                why = ("Still failing: %d element(s) failing now that were not last time, %d "
                       "no longer failing." % (added, gone))
            else:
                why = "Still failing: %s read %s now, %s last time." % (
                    row.get("rule", "").split(" ")[0], row.get("shown"), _shown(then.get("value")))
        elif now == "fail":
            change, why = "new", "New: this check met the value checked last time."
        elif then.get("state") == "fail":
            change, why = "fixed", "Fixed: this check failed last time."
        else:
            change = ""
        row["change"], row["changeLabel"], row["changeWhy"] = change, CHANGE_LABEL[change], why
        if change:
            counts[change] += 1
    said = ("Since the last run on %s at %s: %d new, %d fixed, %d still open%s."
            % (answer.get("document") or "this model", before.get("at") or "an earlier time",
               counts["new"], counts["fixed"], counts["open"],
               ", %d not compared" % counts["unknown"] if counts["unknown"] else ""))
    answer["history"] = {"previous": before.get("at"), "counts": counts, "said": said}
    return answer


def keep_run(answer, home=None, now=None):
    """Keep a FINISHED run, so the next one can say what changed. A run that
    stopped part way, or is still running, is not kept: its NOT RUN rows would
    make the next comparison say nothing. NOR IS A RUN WITH NO MODEL PATH: it
    can never be compared, so it would only take a slot (row 5b-1616). The
    newest RUNS_KEPT of this checklist ON THIS MODEL are kept - one checklist
    run on many models never pushes out another model's last run. Returns the
    path, or None when nothing was kept."""
    if answer.get("running") or answer.get("stopped") or not answer.get("rows"):
        return None
    model = _model(answer)
    if model is None:
        return None
    base = runs_folder(answer.get("name") or "", home, make=True)
    if base is None:
        return None
    where = _model_folder(base, model)
    if not os.path.isdir(where):
        os.makedirs(where)
    stamp = now or time.strftime("%Y-%m-%dT%H-%M-%S")
    kept = {"heron-checklist-run": RUNS_FORMAT, "name": answer.get("name"),
            "document": answer.get("document"), "model": model,
            # "2026-11-12T14-05-09" in the file name, "2026-11-12 14:05" to read.
            "at": stamp[:10] + " " + stamp[11:16].replace("-", ":"),
            "rows": [_kept_row(r) for r in answer.get("rows") or []]}
    # NUMBERED, SO THE ORDER IS THE ORDER THEY WERE KEPT IN - whatever the
    # PC's clock said (row 5b-1622). The time stays in the name to be read.
    path = os.path.join(where, "n%06d-%s-%s.json" % (_next_number(where), stamp,
                                                      answer.get("run") or new_token()))
    COMMANDS.write_whole(path, kept)
    names = sorted((n for n in os.listdir(where) if n.endswith(".json")), key=_order)
    for old in names[:-RUNS_KEPT]:
        try:
            os.remove(os.path.join(where, old))
        except OSError:
            pass
    return path


def history(answer, home=None, now=None):
    """Compare a finished run with the last one on this model, then keep it.
    Never raises: a history that cannot be read or written costs only the
    comparison, and the run's own result stands as it is."""
    if answer.get("running") or answer.get("stopped"):
        return answer
    try:
        found = previous(answer, home)
        compare(answer, found["run"], found["why"])
        if found.get("unreadable"):
            answer.setdefault("findings", []).append(found["why"])
    except Exception as why:                         # noqa: BLE001 - the run stands without it
        _not_compared(answer, "Not compared: what changed since the last run could not be read: "
                              "%s." % why)
    try:
        keep_run(answer, home, now)
    except Exception as why:                         # noqa: BLE001 - the run stands without it
        answer.setdefault("findings", []).append(
            "This run could not be kept for the next comparison: %s." % why)
    if (answer.get("history") or {}).get("said"):
        answer["said"] = answer.get("said", "") + " " + answer["history"]["said"]
    return answer


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
