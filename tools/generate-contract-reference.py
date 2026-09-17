# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-DOC-017
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The Documentation Agent - what is BUILT, from the contracts and the
headers, and where those two disagree.

    python tools/generate-contract-reference.py
    HERON_CONTRACT_REFERENCE_OUT=somewhere.html python tools/...

docs/28: `HERON-DEV-DOC-017` Documentation Agent - "Generates docs from
registries and metadata." T1, so nothing here asks a model anything.

WHY THIS IS NOT A SIXTH COPY OF THE AGENT MAP
-----------------------------------------------
Five generators already exist and four of them are claimed by the
Documentation department. Every one reads a register:

    generate-agent-map.py        docs/28 - the 250 rows
    generate-api-docs.py         the MCP server's signatures
    generate-fragment-catalog.py the fragment library
    generate-skill-catalog.py    the skill library

All four document what Heron OFFERS. This one is in the DEVELOPMENT
department and documents what was BUILT, and its source is the one
nothing reads: `brain/agents/*.yaml` - the CONTRACTS - together with the
metadata header of every file that claims an agent.

docs/28 is the plan. A contract is the promise. The header is the claim.
This page is the third of those, and the only one that can be checked
against the other two.

THE CONCLUSION IT CARRIES: A PROMISE THE CODE DOES NOT KEEP
-------------------------------------------------------------
Each contract declares its `failures` - the refusals a caller may have to
handle. Every agent's own suite checks that its own module names its own
declared failures, one agent at a time, and that check has never been run
across all of them at once.

Two ways it goes wrong, and they fail in opposite directions:

  DECLARED, NEVER NAMED   the contract promises a refusal the code cannot
                          produce. A caller writes a branch for something
                          that never happens, and believes it is covered.
  NAMED, NEVER DECLARED   the code refuses with a word the contract does
                          not list. A caller handling every declared
                          failure still meets an unhandled one, and that
                          is the direction that breaks at run time.

Neither is visible in one file. Both are visible in one table.

WHAT IT DOES NOT RE-IMPLEMENT
-------------------------------
`heron_contract.validate` already says everything else wrong with a
contract - a missing description, a type that is not one of the eight, a
version that is not semver, a field docs/28 owns. It is CALLED. Writing a
second opinion here would be a second answer to one question, which is
what Golden Rule 3 exists to stop, and the two would drift.

WHAT A CONTRACT CANNOT TELL YOU, AND IT IS SAID ON THE PAGE
-------------------------------------------------------------
A tool-layer agent has no contract at all, and neither does a C# one.
Those are not missing files - it is the established shape of this
repository - so they appear as BUILT WITHOUT A CONTRACT rather than as a
gap. Counting them as unfinished would put 30-odd false failures on a
page whose whole purpose is that the failures on it are real.
"""

import ast
import datetime
import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_contract as CON                                  # noqa: E402

OUT = os.environ.get("HERON_CONTRACT_REFERENCE_OUT",
                     "contract-reference.html")

# Where a file may claim an agent. `docs` is not one of them - docs/28 is
# the register, and a register listing an agent is not a file being it.
LAYERS = ("brain", "revit", "mcp", "platform", "tools", "tests")

# ANCHORED TO A COMMENT MARKER, EITHER LANGUAGE. The first version
# began `^#{1,2}` with the `//` optional after it, so it matched a
# Python header and NEVER MATCHED A C# ONE - and the entire revit/
# layer was invisible to this page, 26 agents of it. The page said
# 45 agents were built without a contract when the real number is
# 57. tools/agent-count.py anchors the same way and has all along;
# this is that expression, not a second one.
HEADER = re.compile(r"^\s*(?://|#)\s*Heron-(\w+)\s*:\s*(.+?)\s*$",
                    re.M)

# A refusal name, once it has been found where a refusal lives. NO
# UNDERSCORE IS REQUIRED, and the first version of this required one. A
# refusal can be a single word - INCOMPLETE, MODIFIED, PINNED are three
# real ones - and demanding a second word reported four agents as having
# broken a promise they keep on the line the search was standing on.
REFUSAL = re.compile(r"^([A-Z][A-Z0-9_]{2,})$")

# The same name, as a raise writes it: the word before the colon in the
# message. `heron_secrets` raises NO_SUCH_HANDLE that way rather than
# returning it, and a reader of the contract has to handle it either way.
RAISED = re.compile(r"^([A-Z][A-Z0-9_]{2,}):")

# What a refusal name looks like loose in a sentence. Used ONLY to excuse
# a declared failure, never to accuse - see mentions().
ANYWHERE = re.compile(r"\b[A-Z][A-Z0-9_]{2,}\b")


def claims():
    """Which file claims which agent, and what its header says about it."""
    found = {}
    for layer in LAYERS:
        for here, dirs, files in os.walk(os.path.join(ROOT, layer)):
            dirs[:] = [d for d in dirs
                       if d not in ("__pycache__", "bin", "obj", ".git")]
            for name in sorted(files):
                if not name.endswith((".py", ".cs", ".ps1")):
                    continue
                path = os.path.join(here, name)
                try:
                    head = io.open(path, encoding="utf-8").read(2000)
                except (IOError, UnicodeDecodeError):
                    continue
                fields = dict(HEADER.findall(head))
                # ONE HEADER LINE MAY CLAIM SEVERAL AGENTS, comma
                # separated. 31 files in this repository do -
                # RevitOperations.cs carries three, a session file carries
                # four - and reading the line whole made
                # "HERON-REVIT-DOC-004, HERON-REVIT-SEL-008,
                # HERON-REVIT-CAT-009" a single agent that exists nowhere,
                # while all three real ones looked unclaimed. tools/
                # agent-count.py, which owns the count, has split on the
                # comma all along; this did not, and every one of those
                # 31 files was misread.
                for agent in [one.strip()
                              for one in (fields.get("Agent") or "").split(",")
                              if one.strip()]:
                    if agent == "none":
                        continue
                    found.setdefault(agent, []).append({
                        "file": os.path.relpath(path, ROOT).replace("\\", "/"),
                        "layer": fields.get("Layer", ""),
                        "step": fields.get("Step", ""),
                        "status": fields.get("Status", ""),
                        "since": fields.get("Since", ""),
                    })
    return found


def refusals_in(path):
    """
    Every refusal the module can actually produce.

    BY POSITION, NOT BY SHAPE, and the first version of this got that
    wrong in a way worth writing down. It read every SCREAMING_SNAKE
    string constant in the file and called each one a refusal. Across 121
    contracts that reported 135 refusals nothing declared - and they were
    capability names (COUNT_DUCTS, TAG_SHEET), stated shapes
    (SCREAMING_SNAKE_CASE), module constants (MINIMUM_DOTNET_MAJOR) and
    deliberate examples an agent quotes to say it does NOT do that
    (PROBABLY_FINE, A_FAILURE_NOTHING_NAMES, TOTALLY_SAFE).

    A page of 135 findings that are all wrong is worse than no page: it
    teaches the reader to skip the table, which is where the real ones
    are.

    So a refusal is recognised where this repository PUTS one:

      {"refused": "NAME"}   the value under a `refused` key, which is how
                            every brain/ agent returns one
      raise E("NAME: ...")  the word before the colon, which is how the
                            kernel raises one - `heron_secrets` raises
                            NO_SUCH_HANDLE rather than returning it, and
                            a caller has to handle it either way

    Both are read from the parse tree, so a name quoted in a docstring
    EXPLAINING why a refusal is not raised is not counted as raising it.
    """
    try:
        source = io.open(path, encoding="utf-8").read()
    except (IOError, UnicodeDecodeError):
        return set()
    if not path.endswith(".py"):
        return set()

    try:
        tree = ast.parse(source)
    except SyntaxError:                                  # pragma: no cover
        return set()

    # A CARD IS NOT A REFUSAL. This repository writes a per-item reason
    # the same way it writes a refusal - {"refused": "NAME"} - and the
    # difference is where the dict goes. One is RETURNED, and a caller has
    # to handle it. The other is APPENDED to a list of items that were
    # excluded, and the call itself succeeded. HERON-RAG-FMT-004 excludes
    # a fragment with WRONG_REVIT_VERSION and returns an answer; declaring
    # that as a failure would tell a caller to handle a refusal that never
    # comes back.
    #
    # Found by this agent reporting it, and it was this agent that was
    # wrong. Which is the point of checking every finding against the
    # source before the page is believed.
    carded = set()
    for node in ast.walk(tree):
        if (isinstance(node, ast.Call)
                and isinstance(node.func, ast.Attribute)
                and node.func.attr in ("append", "extend")):
            for arg in node.args:
                for inner in ast.walk(arg):
                    if isinstance(inner, ast.Dict):
                        carded.add(id(inner))

    out = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Dict) and id(node) not in carded:
            for key, value in zip(node.keys, node.values):
                if (isinstance(key, ast.Constant) and key.value == "refused"
                        and isinstance(value, ast.Constant)
                        and isinstance(value.value, str)):
                    found = REFUSAL.match(value.value)
                    if found:
                        out.add(found.group(1))
        elif isinstance(node, ast.Call):
            # dict(refused="NAME") and .get("refused") never carry one, so
            # only the keyword form is read.
            for word in node.keywords:
                if (word.arg == "refused"
                        and isinstance(word.value, ast.Constant)
                        and isinstance(word.value.value, str)):
                    found = REFUSAL.match(word.value.value)
                    if found:
                        out.add(found.group(1))
        elif isinstance(node, ast.Raise) and node.exc is not None:
            call = node.exc
            args = call.args if isinstance(call, ast.Call) else []
            for arg in args[:1]:
                if isinstance(arg, ast.Constant) and isinstance(arg.value,
                                                                str):
                    found = RAISED.match(arg.value)
                    if found:
                        out.add(found.group(1))
                elif isinstance(arg, ast.BinOp) or isinstance(arg,
                                                              ast.JoinedStr):
                    # A message built with % or an f-string still starts
                    # with its literal head, and that is where the name is.
                    head = arg
                    while isinstance(head, ast.BinOp):
                        head = head.left
                    if isinstance(head, ast.JoinedStr) and head.values:
                        head = head.values[0]
                    if isinstance(head, ast.Constant) and isinstance(
                            head.value, str):
                        found = RAISED.match(head.value)
                        if found:
                            out.add(found.group(1))
    return out


def mentions(path, seen=None):
    """
    Every refusal name this module could pass on, itself or through the
    agents it calls.

    A DIFFERENT QUESTION FROM refusals_in, AND DELIBERATELY WIDER. The
    two directions need different evidence, and the reason is worth
    stating because getting it backwards makes the page useless:

      TO SAY A PROMISE IS BROKEN you have to be sure the module cannot
      produce that refusal at all - so anything that even mentions the
      name counts against you, including a refusal produced by an agent
      this one calls. HERON-IMP-FEX-004 declares NOT_A_FOLDER and never
      writes it: HERON-IMP-FIL-002 does, and FEX-004 hands the answer
      back unaltered. That is composition working, not a broken promise.

      TO SAY A REFUSAL IS UNDECLARED you have to be sure it IS one - so
      only the positional forms count, and a name in a docstring or a
      capability called COUNT_DUCTS does not.

    Both directions conservative, in opposite directions. One level of
    import is followed, which is as far as this repository's agents
    delegate.
    """
    seen = set() if seen is None else seen
    if path in seen or not path.endswith(".py"):
        return set()
    seen.add(path)
    try:
        source = io.open(path, encoding="utf-8").read()
        tree = ast.parse(source)
    except (IOError, UnicodeDecodeError, SyntaxError):
        return set()

    out = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Constant) and isinstance(node.value, str):
            out.update(ANYWHERE.findall(node.value))

    if len(seen) == 1:
        here = os.path.dirname(path)
        for node in ast.walk(tree):
            names = []
            if isinstance(node, ast.Import):
                names = [one.name for one in node.names]
            elif isinstance(node, ast.ImportFrom) and node.module:
                names = [node.module]
            for name in names:
                if not name.startswith("heron_"):
                    continue
                for folder in (here, os.path.join(ROOT, "brain")):
                    nearby = os.path.join(folder, name + ".py")
                    if os.path.isfile(nearby):
                        out |= mentions(nearby, seen)
                        break
    return out


def collect():
    by_file = claims()
    known = set(CON.registry_ids())
    rows, notes = [], []
    seen = set()

    # `contracts()` returns (path, data) already loaded, so the file is
    # read once by its owner rather than twice by two readers who could
    # disagree about what is in it.
    for path, contract in CON.contracts():
        name = os.path.splitext(os.path.basename(path))[0]
        seen.add(name)
        if not isinstance(contract, dict):
            # REPORTED, NOT DROPPED. A page quietly missing a contract is
            # worse than a page saying it could not read one.
            notes.append("%s does not parse as a mapping, so nothing here "
                         "can say what it promises" % name)
            continue

        # heron_contract owns everything else wrong with a contract. It is
        # asked; a second opinion written here would drift from it.
        problems = CON.validate(contract, known, where=name)

        # A SUITE IS NOT THE AGENT. tests/ files carry the same
        # `Heron-Agent` header - which is right, it is how a suite says
        # what it proves - so they claim the id too. But a refusal name
        # built into a FIXTURE is not a refusal the agent produces, and
        # counting one reported HERON-IMP-CLS-003 as producing
        # NOT_A_FOLDER, which appears nowhere in heron_classify.py. The
        # suite is still shown: which file proves an agent is worth a
        # column. It is just not read for what the agent does.
        every = by_file.get(contract.get("agent") or name, [])
        files = [one for one in every if one["layer"] != "test"]
        suites = [one for one in every if one["layer"] == "test"]
        declared = sorted(contract.get("failures") or [])
        named, could = set(), set()
        for one in files:
            named |= refusals_in(os.path.join(ROOT, one["file"]))
            could |= mentions(os.path.join(ROOT, one["file"]))

        rows.append({
            "agent": contract.get("agent") or name,
            "version": str(contract.get("version") or ""),
            "files": files,
            "suites": [one["file"] for one in suites],
            "layer": files[0]["layer"] if files else "",
            "status": files[0]["status"] if files else "",
            "step": files[0]["step"] if files else "",
            "input": _fields(contract, "input"),
            "output": _fields(contract, "output"),
            "failures": declared,
            "unreachable": [f for f in declared if files and f not in could],
            "undeclared": sorted(one for one in named
                                 if one not in declared) if files else [],
            "tools": sorted(str(one) for one in
                            (contract.get("allowed-tools") or [])),
            "problems": problems,
            "no_file": not files,
        })

    # BUILT WITHOUT A CONTRACT is the established shape of this repository,
    # not a gap: a tool-layer agent and a C# one carry none.
    without = []
    for agent, every in sorted(by_file.items()):
        if agent in seen:
            continue
        files = [one for one in every if one["layer"] != "test"]
        suites = [one["file"] for one in every if one["layer"] == "test"]
        # AN AGENT CLAIMED ONLY BY ITS SUITE IS SHOWN, NOT DROPPED. Two
        # are: HERON-RAG-RIX-011 and HERON-RAG-DUP-012 live inside the RAG
        # library and only tests/test_maintenance.py names them. They are
        # built and they are proved; nothing that implements them says so,
        # and a reader looking for where one lives has nowhere to go.
        # Skipping them made this page disagree with agent-count.py by
        # exactly two and say nothing about why. Golden Rule 14.
        without.append({"agent": agent, "files": files, "suites": suites,
                        "layer": files[0]["layer"] if files else "test",
                        "only_a_suite": not files})
    return rows, without, notes


def _fields(contract, side):
    block = contract.get(side) or {}
    if not isinstance(block, dict):
        return []
    out = []
    for name in sorted(block):
        spec = block[name] if isinstance(block[name], dict) else {}
        out.append({
            "name": name,
            "type": str(spec.get("type") or ""),
            "required": bool(spec.get("required")),
            "description": " ".join(str(spec.get("description") or "").split()),
        })
    return out


HTML = u"""<title>Heron Contract Reference</title>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<style>
:root{
  --ink:#16232B; --muted:#66767C; --ground:#EFF2F1; --card:#FAFBFA;
  --rule:#C6D0D1; --accent:#1D5C68; --warn:#A9660F; --bad:#9E3B33;
  --safe:#3D7A55;
}
@media (prefers-color-scheme:dark){:root{
  --ink:#DFE7E8; --muted:#84969B; --ground:#0F1719; --card:#161F22;
  --rule:#2B3A3E; --accent:#63BAC5; --warn:#DFA246; --bad:#E0796F;
  --safe:#71B98A;}}
*{box-sizing:border-box}
body{margin:0;background:var(--ground);color:var(--ink);
     font:14px/1.55 -apple-system,"Segoe UI",system-ui,sans-serif}
header{padding:18px 20px;border-bottom:1px solid var(--rule);background:var(--card)}
h1{margin:0 0 4px;font-size:19px;letter-spacing:-.01em}
.sub{color:var(--muted);font-size:13px;max-width:80ch}
.bar{display:flex;flex-wrap:wrap;gap:8px;padding:12px 20px;
     border-bottom:1px solid var(--rule);background:var(--card);
     position:sticky;top:0;z-index:5}
input,select{font:inherit;padding:6px 9px;border:1px solid var(--rule);
     border-radius:3px;background:var(--ground);color:var(--ink)}
input{flex:1;min-width:200px}
.notes{margin:0;padding:12px 20px;border-bottom:1px solid var(--rule);
       background:var(--card);font-size:13px}
.notes h3{margin:0 0 4px;font-size:13px;color:var(--bad)}
.notes ul{margin:4px 0 0;padding-left:18px;color:var(--bad)}
main{padding:14px 20px 60px;display:grid;gap:10px;
     grid-template-columns:repeat(auto-fill,minmax(380px,1fr))}
.a{border:1px solid var(--rule);border-radius:4px;background:var(--card);
   padding:11px 13px}
.a.flag{border-color:var(--bad)}
.a h2{margin:0;font:13px ui-monospace,Consolas,monospace;letter-spacing:-.005em}
.where{color:var(--muted);font-size:12px;margin:5px 0 7px;
       font-family:ui-monospace,Consolas,monospace}
.tags{display:flex;flex-wrap:wrap;gap:5px;margin:4px 0 7px}
.tag{font:10px ui-monospace,Consolas,monospace;letter-spacing:.06em;
   text-transform:uppercase;border:1px solid currentColor;border-radius:2px;
   padding:1px 5px;color:var(--muted)}
.tag.safe{color:var(--safe)} .tag.bad{color:var(--bad)} .tag.warn{color:var(--warn)}
.tag.read{color:var(--accent)}
table{border-collapse:collapse;width:100%;font-size:12.5px;margin-top:4px}
th{text-align:left;font:10px ui-monospace,Consolas,monospace;
   letter-spacing:.06em;text-transform:uppercase;color:var(--muted);
   padding:3px 6px 3px 0;border-bottom:1px solid var(--rule)}
td{padding:4px 6px 4px 0;border-bottom:1px solid var(--rule);
   vertical-align:top}
td.n{font-family:ui-monospace,Consolas,monospace;white-space:nowrap}
td.d{color:var(--muted)}
h4{margin:9px 0 2px;font:10px ui-monospace,Consolas,monospace;
   letter-spacing:.06em;text-transform:uppercase;color:var(--muted)}
.f{font:11.5px ui-monospace,Consolas,monospace;color:var(--muted);
   word-break:break-word}
.f b{color:var(--bad);font-weight:400}
.says{font-size:12.5px;color:var(--bad);margin:6px 0 0}
footer{padding:16px 20px 40px;color:var(--muted);font-size:12.5px;
       max-width:88ch}
.none{color:var(--muted);font-size:12.5px;padding:20px}
</style>
<header>
  <h1>Heron Contract Reference</h1>
  <div class="sub" id="sub"></div>
</header>
<div class="bar">
  <input id="q" placeholder="filter by agent, file, field or refusal">
  <select id="only">
    <option value="">every agent</option>
    <option value="flag">only the ones with a finding</option>
    <option value="none">only the ones built without a contract</option>
  </select>
</div>
<div class="notes" id="notes" hidden></div>
<main id="main"></main>
<footer id="foot"></footer>
<script id="data" type="application/json">__DATA__</script>
<script>
var D = JSON.parse(document.getElementById('data').textContent);
document.getElementById('sub').textContent = D.subtitle;
document.getElementById('foot').textContent = D.footer;

if (D.notes.length) {
  var n = document.getElementById('notes');
  n.hidden = false;
  n.innerHTML = '<h3>Could not be read</h3><ul>' +
    D.notes.map(function (t) { return '<li>' + esc(t) + '</li>'; }).join('') +
    '</ul>';
}

function esc(s) {
  return String(s == null ? '' : s).replace(/[&<>]/g, function (c) {
    return {'&': '&amp;', '<': '&lt;', '>': '&gt;'}[c];
  });
}

function fields(list, side) {
  if (!list.length) return '<h4>' + side + '</h4><div class="f">none</div>';
  return '<h4>' + side + '</h4><table><tr><th>field</th><th>type</th>' +
    '<th>says</th></tr>' + list.map(function (f) {
      return '<tr><td class="n">' + esc(f.name) +
        (f.required ? ' *' : '') + '</td><td class="n">' + esc(f.type) +
        '</td><td class="d">' + esc(f.description) + '</td></tr>';
    }).join('') + '</table>';
}

function card(r) {
  var flag = r.unreachable.length || r.undeclared.length ||
             r.problems.length || r.no_file;
  var tags = ['<span class="tag read">' + esc(r.layer || '?') + '</span>'];
  if (r.status) tags.push('<span class="tag">' + esc(r.status) + '</span>');
  tags.push('<span class="tag">v' + esc(r.version) + '</span>');
  tags.push('<span class="tag ' + (r.tools.length ? 'warn' : 'safe') + '">' +
            (r.tools.length ? r.tools.length + ' tool(s)' : 'no tools') +
            '</span>');
  if (r.suites.length) tags.push('<span class="tag safe">proved</span>');

  var says = [];
  r.problems.forEach(function (p) { says.push(esc(p)); });
  if (r.undeclared.length) {
    says.push('<b>' + r.undeclared.map(esc).join(', ') + '</b> ' +
      (r.undeclared.length === 1 ? 'is a refusal this code produces that ' +
       'the contract does not declare' : 'are refusals this code produces ' +
       'that the contract does not declare') +
      ' &mdash; a caller handling every declared failure still meets it.');
  }
  if (r.unreachable.length) {
    says.push('<b>' + r.unreachable.map(esc).join(', ') + '</b> ' +
      (r.unreachable.length === 1 ? 'is declared but this code cannot ' +
       'produce it' : 'are declared but this code cannot produce them') +
      ' &mdash; a caller writes a branch for something that never happens.');
  }
  if (r.no_file) says.push('<b>No file claims this agent.</b>');

  return '<div class="a' + (flag ? ' flag' : '') + '" data-flag="' +
    (flag ? 'flag' : '') + '" data-text="' +
    esc((r.agent + ' ' + r.layer + ' ' +
         r.files.map(function (f) { return f.file; }).join(' ') + ' ' +
         r.failures.join(' ') + ' ' +
         r.input.concat(r.output).map(function (f) { return f.name; })
          .join(' ')).toLowerCase()) + '">' +
    '<h2>' + esc(r.agent) + '</h2>' +
    '<div class="where">' +
      (r.files.map(function (f) { return esc(f.file); }).join('<br>') ||
       'no file') +
      (r.suites.length ? '<br>' + r.suites.map(esc).join('<br>') : '') +
    '</div>' +
    '<div class="tags">' + tags.join('') + '</div>' +
    (says.length ? '<div class="says">' + says.join('<br>') + '</div>' : '') +
    fields(r.input, 'input') + fields(r.output, 'output') +
    '<h4>refusals</h4><div class="f">' +
      (r.failures.length ? r.failures.map(function (f) {
        return r.unreachable.indexOf(f) < 0 ? esc(f) : '<b>' + esc(f) + '</b>';
      }).join(', ') : 'none declared') +
      (r.undeclared.length ? ', <b>' + r.undeclared.map(esc).join(', ') +
       '</b>' : '') +
    '</div></div>';
}

function bare(r) {
  return '<div class="a' + (r.only_a_suite ? ' flag' : '') +
    '" data-flag="' + (r.only_a_suite ? 'flag' : 'none') + '" data-text="' +
    esc((r.agent + ' ' + r.layer + ' ' +
         r.files.map(function (f) { return f.file; }).join(' ')
        ).toLowerCase()) + '">' +
    '<h2>' + esc(r.agent) + '</h2>' +
    '<div class="where">' +
      (r.files.map(function (f) { return esc(f.file); }).join('<br>') ||
       'no implementing file') +
      (r.suites.length ? '<br>' + r.suites.map(esc).join('<br>') : '') +
    '</div><div class="tags"><span class="tag read">' + esc(r.layer) +
    '</span><span class="tag">no contract</span>' +
    (r.only_a_suite ? '<span class="tag bad">only a suite</span>' : '') +
    '</div><div class="f">' + (r.only_a_suite
      ? '<b>No file that implements this agent claims it.</b> It is built ' +
        'and it is proved, but only its suite carries the header, so a ' +
        'reader looking for where it lives has nowhere to go.'
      : 'A ' + esc(r.layer) + '-layer agent carries no contract YAML. That ' +
        'is the shape of this repository, not a gap.') + '</div></div>';
}

var ALL = D.rows.map(card).concat(D.without.map(bare)).join('');
document.getElementById('main').innerHTML = ALL;

function apply() {
  var q = document.getElementById('q').value.trim().toLowerCase();
  var only = document.getElementById('only').value;
  var shown = 0;
  [].forEach.call(document.querySelectorAll('.a'), function (el) {
    var ok = (!q || el.dataset.text.indexOf(q) >= 0) &&
             (!only || el.dataset.flag === only);
    el.hidden = !ok;
    if (ok) shown++;
  });
  document.getElementById('sub').textContent =
    D.subtitle + '  Showing ' + shown + '.';
}
document.getElementById('q').addEventListener('input', apply);
document.getElementById('only').addEventListener('change', apply);
</script>
"""


def main():
    rows, without, notes = collect()
    unreachable = sum(len(one["unreachable"]) for one in rows)
    undeclared = sum(len(one["undeclared"]) for one in rows)
    problems = sum(len(one["problems"]) for one in rows)
    declared = sum(len(one["failures"]) for one in rows)
    fields = sum(len(one["input"]) + len(one["output"]) for one in rows)
    orphans = [one for one in without if one["only_a_suite"]]

    payload = {
        "rows": rows, "without": without, "notes": notes,
        "subtitle": (
            "%d contract(s), %d field(s) and %d declared refusal(s), read "
            "from brain/agents/ and the metadata header of every file that "
            "claims an agent, at %s. %d agent(s) are built without a "
            "contract, which is the shape of this repository rather than a "
            "gap. %d refusal(s) the code produces are undeclared; %d "
            "declared refusal(s) the code cannot produce.%s"
            % (len(rows), fields, declared,
               datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
               len(without), undeclared, unreachable,
               "" if not orphans else
               "  %d agent(s) are claimed by nothing but their own suite."
               % len(orphans))),
        "footer": (
            "Generated by tools/generate-contract-reference.py. docs/28 is "
            "the plan, a contract is the promise, and a file header is the "
            "claim - this page is the third, and the only one that can be "
            "checked against the other two. Everything else wrong with a "
            "contract is asked of brain/heron_contract.validate rather than "
            "judged again here. A refusal is recognised where this "
            "repository puts one - the value under a `refused` key, or the "
            "word before the colon in a raise - so a name quoted in a "
            "docstring is not counted, and a suite that carries the same "
            "agent header is shown but never read for what the agent does. "
            "To excuse a declared refusal, the agents this one calls count "
            "too: a pass-through is composition, not a broken promise. "
            "Re-run rather than edit."),
    }

    text = HTML.replace("__DATA__", json.dumps(payload, sort_keys=True))
    io.open(OUT, "w", encoding="utf-8").write(text)

    def w(line):
        sys.stdout.write(line.encode("ascii", "replace").decode("ascii"))

    w("%d contract(s), %d field(s), %d declared refusal(s)\n"
      % (len(rows), fields, declared))
    # NOT "(tool and C# layers)", which this line said until 2026-09-17 and
    # which was wrong about 23 of its own findings: HERON-RAG-CTX-007,
    # HERON-RAG-LIB-001, HERON-KRN-DEP-013 and the five Standards rows served
    # by brain/heron_company.py are all layer BRAIN. The count was right; the
    # sentence beside it told the reader nothing was missing, and for 23 of
    # them that is not true - which is worse than no sentence at all, because
    # a reader acts on the words rather than on the number.
    #
    # It now REPORTS the layers instead of asserting them, and it still does
    # not say whether a brain-layer agent OUGHT to carry a contract. Nobody
    # has decided that, and a generator must not decide it by implication.
    layers = {}
    for one in without:
        key = one.get("layer") or "unknown"
        layers[key] = layers.get(key, 0) + 1
    w("%d agent(s) built without a contract (%s)\n"
      % (len(without),
         ", ".join("%s %d" % (k, layers[k]) for k in sorted(layers))))
    for one in orphans:
        w("  ONLY A SUITE: %s is claimed by %s and by no file that "
          "implements it\n" % (one["agent"], ", ".join(one["suites"])))
    for one in rows:
        for name in one["undeclared"]:
            w("  UNDECLARED: %s produces %s and its contract does not say "
              "so\n" % (one["agent"], name))
        for name in one["unreachable"]:
            w("  UNREACHABLE: %s declares %s and cannot produce it\n"
              % (one["agent"], name))
        for line in one["problems"]:
            w("  %s\n" % line)
        if one["no_file"]:
            w("  NO FILE: %s has a contract and nothing claims it\n"
              % one["agent"])
    if not (undeclared or unreachable or problems):
        w("  every declared refusal is reachable and every refusal is "
          "declared\n")
    w("wrote %s (%d KB)\n" % (OUT, len(text) // 1024))
    return 0


if __name__ == "__main__":
    sys.exit(main())
