#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Every value a caller types anywhere in the library has a hint, and no hint
offers a way to type something Revit refuses. Needs no Revit.

    python tests/test_how_to_type.py

WHY. `how_to_type` is the one copy of "what to write in this blank" - the
server prints it beside every value heron_resolve says a caller supplies, and
tools/generate-jobs.py prints it in every job file. Measured on 2026-09-22
across all 765 caller-supplied values, 58 distinct types:

  - 14 types got NO hint, including `double` (102 values) and `int` (27),
    where a modeller types "250mm" and FromRequest refuses it.
  - FOUR got a WRONG one. `IList<Reference>`, `IList<Color>` and both
    request-sourced dictionaries matched the generic list rule and were told
    "comma separated". Revit refuses a Reference by name, and splits the other
    three on SEMICOLONS - so following the hint was the one way guaranteed to
    fail.

The existing check in test_generate_jobs.py names five hand-picked shapes. A
hand-picked list is how the other 53 went unexamined.

WHAT IT PROVES
  1. Every caller-supplied type in the library has a hint, OR is named in
     NO_HINT_ON_PURPOSE with a reason. Read through heron_bridge_client's
     fragment_needs - the reader the server uses, which
     test_fragment_needs_reader.py already holds against PyYAML - so this is
     not a second parser with its own opinion of what a need is.
  2. Every type Revit refuses says CANNOT BE TYPED and never offers a
     separator. The refusals come from receivable() in generate-jobs.py, whose
     RECEIVABLE list test_generate_jobs.py already checks against the C#.
  3. The three semicolon-shaped types say SEMICOLONS and not "comma separated"
     - and the two dictionary hints carry NamedValues' OWN example strings,
     read out of RevitFragment.cs, so the hint and the refusal cannot disagree.
  4. The plain-value traps carry FromRequest's own words: "250, not 250mm" is
     read out of the C# and required in both number hints.
  5. Every NO_HINT_ON_PURPOSE entry has a reason AND still returns no hint, so
     the list cannot keep a type that has since been given one.
  6. brain/, mcp/server/ and mcp/client/ share no module name. heron_brain.py
     puts brain/ at sys.path[0], so a shared name is silently shadowed - the
     import succeeds, binds the wrong module and fails one layer later. It
     happened on 2026-09-22 to a module called heron_contract.
  7. The two guards closed PR #289 carried and nothing on main held: the
     override hint names every setting the add-in's own refusal lists (read
     out of RevitFragment.cs, not retyped), and heron_resolve reports an
     unreadable contract rather than printing it as nothing to type.
  8. A plain `Element` need is told what the add-in accepts for its NAME:
     `selected` for one particular element, a type by name only where the
     name ends the way IsTypeNeedName says a type's does - both read out of
     RevitFragment.cs - checked for every such need in the library, through
     heron_resolve's own contract block and generate-jobs' job lines, the two
     places the hint is printed. FRAGMENT-ISSUES row 5b-190.
  9. An `ElementId` need is told what the add-in accepts for its NAME: a name
     only where OneIdNamed has a rule for it, and otherwise CANNOT BE TYPED BY
     NAME and what binds anyway - a blank or `none` for one id, `selected` or
     a blank for a list. The table and those answers are read out of
     RevitFragment.cs, and every id need in the library is checked through
     how_to_type and heron_resolve's own contract block, and generate-jobs'
     printer is handed a made-up fragment holding each kind. The brain holds
     the one reader of the table and generate-jobs asks it. FRAGMENT-ISSUES
     row 5b-191.

WHAT IT CANNOT DO
  It does not prove a hint is TRUE beyond the strings it reads out of the C#.
  FromRequest is the authority and needs Revit to run. A refusal whose wording
  disagrees with a hint is a bug in how_to_type, not in whoever typed it.

  Clause 3 names its three types by hand. A NEW semicolon-shaped type would
  pass clause 1 on any non-empty hint, including a wrong "comma separated".
  Deriving the separator from the C# was considered and rejected: the split
  hides behind five different helper methods, and a parser that misread one
  would be a false green with a confident name.
"""

import importlib.util
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tools"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

REVIT_FRAGMENT = os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                              "RevitFragment.cs")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load_generate_jobs():
    spec = importlib.util.spec_from_file_location(
        "generate_jobs", os.path.join(ROOT, "tools", "generate-jobs.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def caller_types(client):
    """Every caller-supplied type in the library, spaces stripped the way
    FromRequest strips them, with one fragment that asks for each."""
    found = {}
    fragments = os.path.join(ROOT, "brain", "fragments")
    for name in sorted(os.listdir(fragments)):
        needs = client.fragment_needs(
            os.path.join(fragments, name, "fragment.yaml")) or []
        for need in needs:
            if need.get("source") == "request":
                kind = (need.get("type") or "").replace(" ", "")
                found.setdefault(kind, name)
    return found


def modules(*parts):
    where = os.path.join(ROOT, *parts)
    return set(f[:-3] for f in os.listdir(where)
               if f.endswith(".py") and not f.startswith("__"))


def plain_element_needs(client):
    """(folder, need name, capability) for every caller-supplied need declared
    exactly `Element`. The capability is the folder in upper case, which is
    the naming rule heron_fragment's validator enforces."""
    found = []
    fragments = os.path.join(ROOT, "brain", "fragments")
    for name in sorted(os.listdir(fragments)):
        needs = client.fragment_needs(
            os.path.join(fragments, name, "fragment.yaml")) or []
        for need in needs:
            if (need.get("source") == "request"
                    and (need.get("type") or "").replace(" ", "") == "Element"):
                found.append((name, need.get("name"),
                              name.upper().replace("-", "_")))
    return found


def id_needs(client):
    """(folder, need name, type, capability) for every caller-supplied need
    whose type is an `ElementId` or a list of them - the shapes how_to_type's
    id rule answers."""
    found = []
    fragments = os.path.join(ROOT, "brain", "fragments")
    for name in sorted(os.listdir(fragments)):
        needs = client.fragment_needs(
            os.path.join(fragments, name, "fragment.yaml")) or []
        for need in needs:
            kind = (need.get("type") or "").replace(" ", "")
            if (need.get("source") == "request"
                    and re.search(r"\bElementId>?$", kind)):
                found.append((name, need.get("name"), kind,
                              name.upper().replace("-", "_")))
    return found


def method_body(csharp, signature):
    """One method of RevitFragment.cs, from its signature to the next one.
    Empty when the signature is missing, so a renamed method is a failed check
    rather than a traceback that ends the suite."""
    start = csharp.find(signature)
    if start < 0:
        return ""
    end = csharp.find(chr(10) + "        private static", start + 1)
    return csharp[start:end if end > start else len(csharp)]


def lift(text, names, space):
    """Top-level functions cut out of a module's source and defined in `space`.

    heron_mcp_server needs the MCP SDK to import, and CI leaves the SDK out on
    purpose - test_values_crossing.py says why a suite that imports it stops
    guarding anything. So the functions are run from their source text, which
    is still the code heron_resolve runs. None when one cannot be found.
    """
    ends = re.compile(chr(10) + r"(?=\S)")
    pieces = []
    for name in names:
        start = text.find(chr(10) + "def %s(" % name)
        if start < 0:
            return None
        end = ends.search(text, start + 1)
        pieces.append(text[start + 1:end.start() + 1 if end else len(text)])
    try:
        exec("".join(pieces), space)                          # noqa: S102
    except SyntaxError:
        return None
    return space


def main():
    import heron_bridge_client as CLIENT
    import heron_fragment as HF
    GJ = load_generate_jobs()

    # ASKED FOR, NOT ASSUMED - heron-ship s2a. Run against the code as it
    # stood before this change, the set does not exist; that must be ONE clean
    # failure and the rest of the suite must still run and report.
    silent = getattr(HF, "NO_HINT_ON_PURPOSE", None)
    check(silent is not None,
          "heron_fragment declares which types have no hint on purpose")
    if silent is None:
        silent = {}

    csharp = io.open(REVIT_FRAGMENT, encoding="utf-8").read()
    types = caller_types(CLIENT)

    # --- 1. every caller-supplied type has a hint, or a reason -------------
    print()
    print("Every value a caller types has something beside its blank")
    check(len(types) > 40,
          "the library declares %d distinct caller-supplied types - far fewer "
          "means the reader stopped finding them" % len(types))
    blank = sorted(kind for kind in types
                   if not HF.how_to_type(kind) and kind not in silent)
    check(not blank,
          "all %d are hinted or named with a reason%s" % (
              len(types), "" if not blank else " - BLANK: " + ", ".join(
                  "%s (%s)" % (kind, types[kind]) for kind in blank)))

    # --- 2. a refused type is never offered a way to type it ---------------
    print()
    print("A type Revit refuses says so, and offers no syntax")
    # `Element` is refused only when the need's NAME means one particular
    # element; as a TYPE it is typable and has its own test in
    # test_generate_jobs.py. receivable() needs the name to tell them apart,
    # and a type-level question cannot supply one.
    refused = sorted(kind for kind in types
                     if kind != "Element" and not GJ.receivable(kind)[0])
    check(refused, "some caller-supplied types are refused: %s" % refused)
    for kind in refused:
        hint = HF.how_to_type(kind)
        check("CANNOT BE TYPED" in hint and "comma separated" not in hint,
              "%s says CANNOT BE TYPED and offers no separator: %r"
              % (kind, hint))

    # --- 3. the semicolon types say semicolons -----------------------------
    print()
    print("A value Revit splits on semicolons is not told commas")
    for kind in ("IList<Color>", "IDictionary<string,double>",
                 "IDictionary<string,string>"):
        hint = HF.how_to_type(kind)
        check("SEMICOLON" in hint.upper() and "comma separated" not in hint,
              "%s says semicolons, not commas: %r" % (kind, hint))

    # NamedValues builds its refusal around an example of each shape. Read
    # them out of the C# rather than retyping them, so a change on either
    # side goes red here instead of drifting.
    shown = re.findall(r'\\"([^"\\]*=[^"\\]*;[^"\\]*)\\"', csharp)
    numbers = next((s for s in shown if "Walls=" in s), None)
    texts = next((s for s in shown if "view=" in s), None)
    check(numbers and texts,
          "NamedValues' two example strings are in RevitFragment.cs: %r, %r"
          % (numbers, texts))
    check(numbers and numbers in HF.how_to_type("IDictionary<string,double>"),
          "the number-table hint carries NamedValues' own example")
    check(texts and texts in HF.how_to_type("IDictionary<string,string>"),
          "the text-table hint carries NamedValues' own example")

    # --- 4. the plain-value traps, in FromRequest's own words ---------------
    print()
    print("The traps a modeller walks into, in the add-in's own words")
    check("250, not 250mm" in csharp,
          "FromRequest refuses a unit with '250, not 250mm'")
    for kind in ("int", "double"):
        check("not 250mm" in HF.how_to_type(kind),
              "%s warns against typing the unit: %r"
              % (kind, HF.how_to_type(kind)))
    check("true or false" in HF.how_to_type("bool"),
          "bool says true or false, because bool.TryParse refuses yes")
    check("is red" in csharp and "255,0,0" in HF.how_to_type("Color"),
          "Color gives three numbers, as OneColour's refusal does")
    # WITH THE NEED'S NAME since row 5b-191: whether an id is typed as a name
    # at all is decided by the name, and clause 9 holds the rest to the C#.
    check("never" in HF.how_to_type("ElementId", "levelId"),
          "an ElementId the add-in has a rule for is a NAME, never its number")
    check("`selected`" in HF.how_to_type("IList<ElementId>"),
          "a LIST of ids also takes the word `selected`")
    check("refused" in HF.how_to_type("View3D"),
          "View3D says a plan is refused")
    for value in ("Duplicate", "WithDetailing", "AsDependent"):
        check(value in HF.how_to_type("ViewDuplicateOption"),
              "ViewDuplicateOption names %s" % value)

    # --- 5. the silent list is honest --------------------------------------
    print()
    print("A type named as silent on purpose really is, and says why")
    for kind, reason in sorted(silent.items()):
        check(bool(reason and reason.strip()),
              "%s carries a reason" % kind)
        check(not HF.how_to_type(kind),
              "%s still has no hint - an entry kept after one was added is "
              "stale" % kind)

    # --- 6. no module shadows another --------------------------------------
    print()
    print("No module name is shared across the three import roots")
    roots = {"brain": modules("brain"),
             "mcp/server": modules("mcp", "server"),
             "mcp/client": modules("mcp", "client")}
    names = sorted(roots)
    for i, one in enumerate(names):
        for two in names[i + 1:]:
            shared = sorted(roots[one] & roots[two])
            check(not shared, "%s and %s share no module name%s"
                  % (one, two, "" if not shared else " - " + ", ".join(shared)))

    # --- 7. the two guards PR #289 carried, kept when it closed --------------
    #
    # #289 was closed as a duplicate of #284, and these were the two checks it
    # had that nothing on main held. Carried here so closing it lost nothing.
    print()
    print("The override hint names every setting the add-in accepts")
    # THE HINT THAT COST FOUR REFUSALS. On 2026-09-22 the nine settings were
    # learned one refusal at a time; a trimmed hint would send the next caller
    # straight back there. The list is read out of the add-in's own refusal,
    # never retyped, so a tenth setting added in C# goes red here first.
    start = csharp.find("is not a graphic override Heron can set. It takes")
    end = csharp.find("separated with", start)
    region = csharp[start:end] if start >= 0 and end > start else ""
    flat = re.sub(r'"\s*\+\s*"', "", region).split("It takes", 1)[-1]
    settings = [s.strip(' -"') for s in re.split(r",|\band\b", flat)
                if s.strip(' -"')]
    check(len(settings) >= 5,
          "the add-in's refusal lists its override settings: %s" % settings)
    override = HF.how_to_type("OverrideGraphicSettings")
    for setting in settings:
        check(setting in override,
              "the OverrideGraphicSettings hint names %s" % setting)

    print()
    print("heron_resolve says so when it cannot read a contract")
    # A needs list that cannot be read must not print as an empty one: 77
    # fragments legitimately need nothing typed, so silence is already an
    # answer and a failure rendered as silence sends the caller back to
    # guessing. Read as TEXT, because importing the server needs the MCP SDK.
    server = io.open(os.path.join(ROOT, "mcp", "server",
                                  "heron_mcp_server.py"),
                     encoding="utf-8").read()
    check("rather than listed short" in server,
          "an unreadable contract is reported, not printed as 'nothing to type'")

    # --- 8. a plain `Element` need is told what the add-in accepts -----------
    #
    # FRAGMENT-ISSUES row 5b-190. A plain `Element` is a type to build with or
    # one particular element, and only the need's NAME says which - so asked
    # with the type alone, how_to_type told every plain `Element` need in the
    # library to type a TYPE by name. Followed exactly on 2026-09-23 against
    # Project1 (`exemplar=M_Supply Diffuser: ...`), and OneElement refused it
    # and said to SELECT IT IN REVIT and pass `selected`. So each hint is held
    # to the add-in's own answers, read out of the C#: which need names mean a
    # type, which word means the selection, and what the refusal says to type.
    print()
    print("A plain `Element` need is told what the add-in accepts for it")

    # ASKED, NOT ASSUMED - heron-ship s2a. The one-argument function this
    # replaced is one clean failure here, and every check below still runs.
    takes_name = HF.how_to_type.__code__.co_argcount >= 2
    check(takes_name, "how_to_type is handed the need's name")

    def hint_for(name):
        return (HF.how_to_type("Element", name) if takes_name
                else HF.how_to_type("Element"))

    rule = re.search(r'EndsWith\("(\w+)",\s*StringComparison\.Ordinal\)',
                     method_body(csharp, "private static bool IsTypeNeedName"))
    check(rule is not None,
          "IsTypeNeedName in RevitFragment.cs decides by the need-name's ending")
    ending = rule.group(1) if rule else "Type"

    def add_in_says_type(name):
        return (name or "").endswith(ending)

    ours = getattr(HF, "is_type_need_name", None)
    check(ours is not None,
          "heron_fragment carries the add-in's rule for a type-named need")

    check('"selected"' in method_body(csharp,
                                      "private static bool IsSelectionWord"),
          "IsSelectionWord accepts `selected`, the word the hint names")
    refusal = method_body(csharp, "private static object OneElement")
    check("ONE PARTICULAR ELEMENT" in refusal
          and re.search(r"pass\W+selected", refusal) is not None,
          "and OneElement's refusal of a typed name says to pass `selected`")

    plain = plain_element_needs(CLIENT)
    check(plain, "the library declares plain `Element` needs to check")
    probes = [name for _folder, name, _cap in plain] + [
        "wallType", "Type", "type", "exemplar", "", None]
    if ours is not None:
        wrong = [name for name in probes if ours(name) != add_in_says_type(name)]
        check(not wrong, "heron_fragment's type-name rule is the add-in's "
                         "for every need name in the library%s"
              % ("" if not wrong else " - DISAGREES: %s" % wrong))

    for folder, name, _cap in plain:
        hint = hint_for(name)
        if add_in_says_type(name):
            check("element TYPE by name" in hint,
                  "%s's `%s` is named like a type and told to type one: %r"
                  % (folder, name, hint))
        else:
            check("`selected`" in hint and "TYPE by name" not in hint,
                  "%s's `%s` is told `selected`, never a type name: %r"
                  % (folder, name, hint))
    check("TYPE by name" not in hint_for(None),
          "with no name the hint is the particular element's, as in the add-in")
    check("element TYPE by name" in hint_for("wallType"),
          "and a need named like `wallType` is still told to type a TYPE")

    # THROUGH THE TWO PLACES THE HINT IS PRINTED, because the function was
    # never the whole defect: heron_resolve asked it without the name. So a
    # printer that forgets the name again goes red here, not in front of a
    # modeller.
    def element_row(lines, name):
        row = [line for line in lines if line.split()[:2] == [name, "Element"]]
        return row[0].strip() if row else "no row for it in %r" % lines

    def contract_lines(space, capability):
        try:
            return space["_contract_lines"](capability)
        except Exception as exc:                            # noqa: BLE001
            return ["(it raised %s: %s)" % (type(exc).__name__, exc)]

    space = lift(server, ["_fragment_for", "_contract_lines"],
                 {"os": os, "io": io, "sys": sys, "bridge": CLIENT,
                  "_repo_root": lambda: ROOT})
    check(space is not None,
          "heron_resolve's contract block can be lifted out of the server")
    for folder, name, capability in (plain if space is not None else []):
        row = element_row(contract_lines(space, capability), name)
        check(add_in_says_type(name)
              or ("`selected`" in row and "TYPE by name" not in row),
              "heron_resolve %s prints `%s` as the add-in reads it: %s"
              % (capability, name, row))

    # A TYPE-NAMED `Element` IS THE CASE THE LIBRARY CANNOT SHOW - none is
    # declared today - and it is the one where a printer that drops the name
    # still goes wrong, because the safe default then says `selected` where a
    # type name is wanted. So both printers are handed a made-up fragment
    # holding one of each, through the same code they run for a real one.
    class TwoElements(object):
        slug = "two-elements"
        data = {"risk": "READ"}

        def needs(self):
            return [{"name": "host", "type": "Element", "source": "request"},
                    {"name": "wallType", "type": "Element",
                     "source": "request"}]

        def provides(self):
            return []

    class OneFragment(object):
        """The bridge's needs reader, answering for the made-up fragment."""
        @staticmethod
        def fragment_needs(_path):
            return TwoElements().needs()

    alone = lift(server, ["_contract_lines"],
                 {"os": os, "io": io, "sys": sys, "bridge": OneFragment,
                  "_repo_root": lambda: ROOT,
                  "_fragment_for": lambda _capability: ("two-elements",
                                                        "DRAFT")})
    lines = contract_lines(alone, "TWO_ELEMENTS") if alone is not None else []
    typed, picked = element_row(lines, "wallType"), element_row(lines, "host")
    check("element TYPE by name" in typed,
          "heron_resolve hands the name through - `wallType` is told a "
          "type name: %s" % typed)
    check("`selected`" in picked and "TYPE by name" not in picked,
          "and `host` is told `selected`: %s" % picked)

    job = GJ.job_block(TwoElements(), False, {})
    hosts = [line for line in job if line.strip().startswith("host:")]
    types_ = [line for line in job if line.strip().startswith("wallType:")]
    check(hosts and all("`selected`" in line and "TYPE by name" not in line
                        for line in hosts),
          "generate-jobs writes `selected` beside a particular element: %s"
          % hosts)
    check(types_ and all("Basic Wall" in line for line in types_),
          "and a type name beside a type-named one: %s" % types_)

    # --- 9. an `ElementId` need is told what the add-in accepts for its NAME -
    #
    # FRAGMENT-ISSUES row 5b-191 - clause 8's defect one type over. An id is
    # resolved by the need's NAME: OneIdNamed finds a level for `levelId` and a
    # sheet for `sheetId`, and refuses a name it has no rule for whatever is
    # typed. how_to_type answered the type alone, so every id need was told to
    # type a name, and four in the library have no rule. So the table, and what
    # binds without one, are read out of the C#, and every id need in the
    # library is held to them - through how_to_type, through heron_resolve's
    # own contract block, and through generate-jobs' printer on a made-up
    # fragment.
    print()
    print("An `ElementId` need is told what the add-in accepts for its name")

    one_id = method_body(csharp, "private static object OneIdNamed")
    table = set(re.findall(r'name == "([A-Za-z]+)"', one_id))
    check(len(table) > 10 and "levelId" in table,
          "OneIdNamed's table of id names is read out of RevitFragment.cs: "
          "%d names" % len(table))
    check("said.Length == 0" in one_id and '"none"' in one_id
          and "return ElementId.InvalidElementId;" in one_id,
          "and a blank or `none` binds NO element whatever the name, before "
          "the table is asked")
    single = re.search(r'if \(wanted == "ElementId"\)\s*return (\w+)\(', csharp)
    check(single is not None and single.group(1) == "OneIdNamed",
          "one id goes straight to OneIdNamed, so `selected` is just a name "
          "there, refused without a rule")
    opens = csharp.find('wanted == "IList<ElementId>"')
    closes = csharp.find('wanted == "IList<Element>"', opens + 1)
    listed = csharp[opens:closes] if 0 <= opens < closes else ""
    check(0 <= listed.find("IsSelectionWord(text)") < listed.find("OneIdNamed(")
          and "IsNullOrWhiteSpace(text)) return ids;" in listed,
          "a list takes `selected` before the table is asked, and a blank is "
          "an empty list")

    # ONE READER OF THE TABLE, IN THE BRAIN. Asked for rather than called, so
    # the code as it stood before is one clean failure here - and the checks
    # below still run, against the table this clause read itself.
    ours = getattr(HF, "id_need_names", None)
    check(ours is not None,
          "heron_fragment carries the add-in's table of id names")
    try:
        theirs = ours() if ours is not None else None
    except RuntimeError as exc:
        theirs = "it raised: %s" % exc
    check(theirs == table,
          "and reads the same names out of the C#%s"
          % ("" if theirs == table else " - IT READ: %r" % (theirs,)))
    check(ours is not None and GJ.id_need_names is ours,
          "generate-jobs asks the brain's reader, so there is one, not two")

    offers_a_name = ("the NAME of the thing", "NAMES, never numbers")

    def says_right(hint, name, kind):
        """The hint, or a printed row, says what the add-in does with it."""
        many = "<" in kind
        if name in table:
            return "CANNOT BE TYPED" not in hint and (
                offers_a_name[1] in hint if many else offers_a_name[0] in hint)
        if ("CANNOT BE TYPED BY NAME" not in hint
                or any(offer in hint for offer in offers_a_name)):
            return False
        if many:
            return "`selected`" in hint and "empty list" in hint
        return "`none`" in hint and "blank" in hint and "NO element" in hint

    ids = id_needs(CLIENT)
    check(ids, "the library declares caller-supplied id needs to check")
    blind = sorted("%s.%s" % (folder, name)
                   for folder, name, _kind, _cap in ids if name not in table)
    check(blind, "and some have no rule, so there is something to catch: %s"
          % blind)
    for folder, name, kind, _cap in ids:
        hint = (HF.how_to_type(kind, name) if takes_name
                else HF.how_to_type(kind))
        check(says_right(hint, name, kind),
              "%s's `%s` (%s) %s: %r"
              % (folder, name, kind,
                 "has a rule and is told a name" if name in table
                 else "has no rule and is told what binds instead", hint))
    check(says_right(HF.how_to_type("ElementId"), None, "ElementId"),
          "with no name one id gets the no-rule answer, as in the add-in: %r"
          % HF.how_to_type("ElementId"))
    check(says_right(HF.how_to_type("IList<ElementId>"), None,
                     "IList<ElementId>"),
          "and so does a list: %r" % HF.how_to_type("IList<ElementId>"))

    def need_row(lines, name):
        row = [line for line in lines if line.split()[:1] == [name]]
        return row[0].strip() if row else "no row for it in %r" % lines

    for folder, name, kind, capability in (ids if space is not None else []):
        row = need_row(contract_lines(space, capability), name)
        check(says_right(row, name, kind),
              "heron_resolve %s prints `%s` as the add-in reads it: %s"
              % (capability, name, row))

    # A TABLE THAT CANNOT BE READ IS NOT A TABLE THAT SAYS YES. id_need_names
    # raises, and the hint must neither pass that on - it would end the
    # caller's heron_resolve - nor guess. Put back whatever was there, so
    # nothing after this reads the stand-in.
    def unreadable(*_args, **_kwargs):
        raise RuntimeError("a stand-in for an add-in that cannot be read")

    missing = object()
    kept = getattr(HF, "id_need_names", missing)
    HF.id_need_names = unreadable
    try:
        try:
            shown = [HF.how_to_type("ElementId", "levelId"),
                     HF.how_to_type("IList<ElementId>", "revisionIds")]
        except RuntimeError as exc:
            shown = ["it raised: %s" % exc]
    finally:
        if kept is missing:
            del HF.id_need_names
        else:
            HF.id_need_names = kept
    check(all("NOT KNOWN" in hint
              and not any(offer in hint for offer in offers_a_name)
              for hint in shown),
          "an add-in that cannot be read makes the hint say NOT KNOWN - no "
          "name offered, and no crash: %r" % shown)

    # GENERATE-JOBS' PRINTER, ON ONE MADE-UP FRAGMENT holding each kind.
    # heron_resolve's is held above, over the library, which has both kinds;
    # generate-jobs' is not run over the library here, so it is handed one of
    # each through the same code it runs for a real fragment. A printer that
    # dropped the name shows on the needs WITH a rule, which the no-name
    # answer calls untypeable.
    class FourIds(object):
        slug = "four-ids"
        data = {"risk": "READ"}

        def needs(self):
            return [{"name": "levelId", "type": "ElementId",
                     "source": "request"},
                    {"name": "parameterId", "type": "ElementId",
                     "source": "request"},
                    {"name": "categoryIds", "type": "ICollection<ElementId>",
                     "source": "request"},
                    {"name": "elementIds", "type": "IList<ElementId>",
                     "source": "request"}]

        def provides(self):
            return []

    job = GJ.job_block(FourIds(), False, {})
    for need in FourIds().needs():
        name, kind = need["name"], need["type"]
        lines = [line for line in job if line.strip().startswith(name + ":")]
        check(lines and all(says_right(line, name, kind) for line in lines),
              "generate-jobs writes `%s` (%s) as the add-in reads it: %s"
              % (name, "a rule" if name in table else "no rule", lines))

    print()
    if FAILURES:
        print("%d FAILURE(S):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("All clauses pass. A hint that is PRESENT and consistently shaped is")
    print("not proved TRUE - FromRequest is the authority, and it needs Revit.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
