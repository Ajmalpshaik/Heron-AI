# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The proof path for ADD-IN agents - the one thing 26 of them have never had.

    python tools/prove-agent.py sessions
    python tools/prove-agent.py track HERON-REVIT-LVL-027 --first 36860 --second 64416
    python tools/prove-agent.py review
    python tools/prove-agent.py review HERON-REVIT-LVL-027
    python tools/prove-agent.py accept HERON-REVIT-LVL-027 --by "Ajmal PS"

WHY THIS EXISTS: EVERY ADD-IN AGENT IS DRAFT, AND NONE OF THEM CAN STOP BEING
------------------------------------------------------------------------------
Measured 2026-09-18: all 26 `.cs` files in `revit/Heron.Revit.Addin/` carry
`Heron-Status: DRAFT`, including agents built weeks earlier and run against
real models many times. Not one has ever been promoted, and the reason is
structural rather than neglect - the whole proving apparatus is fragment
shaped. `tools/batch-prove.py` reads `brain/fragments/*/fragment.yaml`;
`brain/heron_validate.py`, the drafts in `brain/proof-drafts/` and
`tools/check-signatures.py` are all per-fragment. An add-in operation has no
route at all.

Ten agents were proved by hand on 2026-09-17 against two models. Nothing in
the repository records that, because there was nowhere to record it. This is
the nowhere.

THE THREE RULES ARE COPIED FROM THE FRAGMENT PATH, NOT REINVENTED
-------------------------------------------------------------------
1. **The machine never signs.** `by:` is required and written EMPTY. A draft
   that reaches a proof without a person's name fails loudly instead of
   counting as proven quietly.

2. **The prover has no write path into what it proves.** `write_draft` refuses
   any path under `revit/`, so "it never promotes an agent" is a property of
   the code's reach rather than of its good intentions. The fragment side
   enforces the same thing by folder layout and says so in
   `brain/proof-drafts/README.md`.

3. **`gaps:` is read first.** It lists what the run could NOT establish. A
   draft with gaps is a job half done, not a proof with footnotes.

WHY TRACKING INSTEAD OF A NEGATIVE CASE
-----------------------------------------
[D-30](../docs/DECISIONS.md) wants a positive case and a negative one. A
`list_*` agent has no negative case in any model: it describes whatever
document it is handed, so there is no input that makes it correctly return
nothing. `fragment-proving` names the substitute for exactly this shape, and
it is [D-53](../docs/DECISIONS.md) TRACKING: **the answer must follow the
input.**

An agent that fell back to a cached `Document`, to the active view, or to the
whole session would report the same numbers whatever model was in front of it.
Running it against TWO models and requiring the answer to MOVE is what rules
that out. One model proves nothing; it is the second that carries the argument.

So `track` needs two live Revit sessions with different models open, and
refuses if the two report the same document.

WHERE A PROOF LIVES, AND WHY NOT IN THE AGENT
-----------------------------------------------
A fragment carries its `proof:` block inside its own `fragment.yaml`. An
add-in agent is a `.cs` file and cannot. So proofs go to
`brain/agent-proofs/<ID>.yaml`, one per agent, beside `brain/agents/<ID>.yaml`
which holds the CONTRACT.

That pairing is deliberate: **the contract is the promise and the proof is the
evidence**, two files with one key, and neither pretending to be the other.

THE FINGERPRINT NORMALISES, AND THAT IS NOT COSMETIC
------------------------------------------------------
It hashes the agent's source with `/` separators and `\\n` line endings. The
fragment side learned this the expensive way on 2026-09-06: hashing raw bytes
and the operating system's own path spelling made the number a fact about
Windows rather than about the code, and ALL SIXTEEN proven fragments reported
STALE the first time they were read in a Linux container. None of them was.

A staleness gate that cries wolf sixteen times out of sixteen stops being read,
which is the one habit this repository cannot afford.
"""

import argparse
import hashlib
import io
import os
import re
import sys
import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

DRAFTS = os.path.join(ROOT, "brain", "agent-proof-drafts")
PROOFS = os.path.join(ROOT, "brain", "agent-proofs")
REGISTRY = os.path.join(ROOT, "docs", "28-agent-registry.md")

NOT_ESTABLISHED = "NOT ESTABLISHED"

DRAFT_HEADER = {
    "heron-agent": "none",
    "heron-step": 17,
    "heron-status": "DRAFT",
    "heron-since": "0.1.0",
    "heron-layer": "brain",
}


def w(s):
    """stdout that survives a console that is not UTF-8."""
    sys.stdout.write(s.encode("ascii", "replace").decode("ascii"))


# --------------------------------------------------------------------------
# what the repository already knows

def registry_agents():
    """Every agent id in docs/28, with its name."""
    agents = {}
    if not os.path.exists(REGISTRY):
        return agents
    for line in io.open(REGISTRY, encoding="utf-8"):
        if not line.startswith("| `HERON-"):
            continue
        cols = [c.strip() for c in line.split("|")]
        if len(cols) < 4:
            continue
        agents[cols[1].strip("`")] = re.sub(r"\*\*|↗", "", cols[2]).strip()
    return agents


def source_of(agent_id):
    """The add-in file claiming this agent, or None.

    Anchored to a header comment, the same way agent-count.py and
    check-metadata.py find a claim - a regex in the body of a file is not a
    declaration.
    """
    base = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
    if not os.path.isdir(base):
        return None
    header = re.compile(r"^\s*//\s*Heron-Agent\s*:\s*(.+?)\s*$")
    for name in sorted(os.listdir(base)):
        if not name.endswith(".cs"):
            continue
        path = os.path.join(base, name)
        with io.open(path, encoding="utf-8", errors="replace") as fh:
            for i, line in enumerate(fh):
                if i > 40:
                    break
                m = header.match(line)
                if not m:
                    continue
                claimed = [a.strip() for a in m.group(1).split(",")]
                if agent_id in claimed:
                    return path
    return None


def fingerprint(path):
    """One hash over the source's CONTENT, normalised.

    `/` separators and `\\n` line endings, for the reason this module's own
    docstring records: a fingerprint that encodes the operating system is a
    fact about the machine rather than about the code, and it makes every
    proof unverifiable anywhere else.
    """
    if not path or not os.path.isfile(path):
        return None
    with io.open(path, "rb") as fh:
        raw = fh.read()
    text = raw.decode("utf-8", "replace").replace("\r\n", "\n").replace("\r", "\n")
    rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
    h = hashlib.sha256()
    h.update(rel.encode("utf-8"))
    h.update(b"\0")
    h.update(text.encode("utf-8"))
    return h.hexdigest()[:16]


# --------------------------------------------------------------------------
# talking to Revit

def bridges(client_id=None):
    """Every live Revit the bridge can see.

    A TOOL may import the client; `brain` may not, and D-48 says so - which is
    the whole reason this file is in tools/ rather than brain/, following
    tools/batch-prove.py rather than inventing a new arrangement.

    WHY THE CLIENT ID IS AN ARGUMENT, AND WHY IT DEFAULTS TO A DISTINCT ONE
    ------------------------------------------------------------------------
    The lease identifies a CHAT, one per Revit process (D-22). A tool running
    under its own id is a second chat as far as Heron is concerned, so every
    request to a Revit some chat is already using comes back `session_in_use` -
    which is the lease doing exactly its job, not a fault.

    Measured 2026-09-18: nine agents in a row refused, because the chat driving
    this had bound itself to one of the two sessions moments earlier to answer
    a question about rooms.

    So `--client-id` exists. Passing the id the chat uses makes the tool the
    SAME chat rather than a competing one, and the refusals stop. It is opt-in
    and not the default, because sharing an id means a proof run can land in
    the middle of somebody's conversation with the same Revit - which is
    exactly what the lease is for. The operator decides, knowing both.
    """
    os.environ["HERON_CLIENT_ID"] = client_id or os.environ.get(
        "HERON_CLIENT_ID", "heron-prove-agent")
    import heron_bridge_client as C
    live = C.discover()[0]
    return live


def parse_args(pairs):
    """`["category=Pipes"]` to `{"category": "Pipes"}`.

    Values stay STRINGS. The bridge and the add-in already agree on how to
    read a category name or a number out of one, and guessing a type here
    would put a third opinion between them.
    """
    out = {}
    for pair in pairs or ():
        if "=" not in pair:
            raise ValueError("--arg wants KEY=VALUE, and %r has no '='." % pair)
        key, value = pair.split("=", 1)
        key = key.strip()
        if not key:
            raise ValueError("--arg %r has an empty name." % pair)
        out[key] = value
    return out


def ask(bridge, operation, op_args=None):
    """Send one operation and return the reply, or a refusal dict."""
    try:
        return bridge.request(operation, op_args=op_args or None)
    except Exception as why:                                  # noqa: BLE001
        return {"ok": False, "error": "no_answer", "message": str(why)}


def describe(reply):
    """A model, named the way every Heron answer names one.

    The size comes from whichever key the operation happens to use - `count`
    from `count_elements`, `placedElements` from the list agents. Asked in
    order rather than assumed: the first draft looked only for
    `placedElements` and printed `(? elements)` against a reply that carried
    `count = 3513`, which is a tool describing its own ignorance as the
    model's.
    """
    for key in ("count", "placedElements", "elements", "total"):
        if isinstance(reply.get(key), (int, float)):
            return "%s (%s elements)" % (reply.get("document") or "(unnamed)",
                                         "{:,}".format(reply[key]))
    return reply.get("document") or "(unnamed)"


# --------------------------------------------------------------------------
# the draft

def draft_path(agent_id):
    return os.path.join(DRAFTS, "%s.yaml" % agent_id)


def proof_path(agent_id):
    return os.path.join(PROOFS, "%s.yaml" % agent_id)


def write_draft(agent_id, draft):
    """Write a draft. Refuses any path inside revit/.

    The refusal is the point. This tool cannot promote an agent because it
    cannot reach the file that would carry the promotion - the same guarantee
    brain/heron_validate.py gets from the fragment library's layout, expressed
    here as a check because the folders do not do it for us.
    """
    path = draft_path(agent_id)
    forbidden = os.path.join(ROOT, "revit")
    if os.path.abspath(path).startswith(os.path.abspath(forbidden) + os.sep):
        raise ValueError(
            "refusing to write a draft inside revit/. This tool has no write "
            "path into the add-in, and that is what makes 'it never promotes "
            "an agent' a guarantee rather than a promise.")
    if not os.path.isdir(DRAFTS):
        os.makedirs(DRAFTS)
    body = dict(DRAFT_HEADER)
    body.update(draft)
    with io.open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(as_yaml(body))
    return path


def as_yaml(body):
    """A small, predictable YAML writer.

    Hand-rolled rather than imported, because `yaml` is an OPTIONAL dependency
    here (requirements-optional.txt) and a proof tool that cannot run on a
    plain checkout is a proof tool nobody runs. The shape written is flat
    strings, numbers and one list - nothing that needs a real emitter.
    """
    out = []
    for key, value in body.items():
        if isinstance(value, bool):
            out.append("%s: %s" % (key, "true" if value else "false"))
        elif isinstance(value, (int, float)):
            out.append("%s: %s" % (key, value))
        elif isinstance(value, list):
            out.append("%s:" % key)
            for one in value:
                out.append("  - %s" % quoted(str(one)))
        elif value is None:
            out.append("%s: ''" % key)
        else:
            out.append("%s: %s" % (key, quoted(str(value))))
    return "\n".join(out) + "\n"


def quoted(text):
    """Single-quoted YAML, with the one escape that form needs."""
    return "'%s'" % text.replace("'", "''")


def read_yaml(path):
    """Read back what as_yaml wrote. Flat keys only, which is all this uses."""
    if not os.path.isfile(path):
        return None
    body = {}
    key = None
    for line in io.open(path, encoding="utf-8"):
        line = line.rstrip("\n")
        if line.startswith("  - ") and key:
            body.setdefault(key, [])
            if isinstance(body[key], list):
                body[key].append(unquoted(line[4:]))
            continue
        m = re.match(r"^([A-Za-z0-9_-]+):\s*(.*)$", line)
        if not m:
            continue
        key, raw = m.group(1), m.group(2)
        body[key] = [] if raw == "" else unquoted(raw)
    return body


def unquoted(text):
    text = text.strip()
    if len(text) >= 2 and text[0] == "'" and text[-1] == "'":
        return text[1:-1].replace("''", "'")
    return text


# --------------------------------------------------------------------------
# commands

def cmd_sessions(args):
    """What Revit sessions are visible, and what each has open."""
    live = bridges(args.client_id)
    if not live:
        w("No Revit is connected. Open one and click the Heron button.\n")
        return 1
    w("\n%d Revit session(s) connected:\n\n" % len(live))
    for b in live:
        reply = ask(b, "count_elements")
        if reply.get("ok"):
            w("  %-10s %s\n" % (getattr(b, "pid", "?"), describe(reply)))
        else:
            w("  %-10s %s - %s\n" % (getattr(b, "pid", "?"),
                                     reply.get("error"),
                                     (reply.get("message") or "")[:70]))
    w("\nTracking needs TWO, with DIFFERENT models open.\n")
    return 0


def cmd_track(args):
    """Run one agent against two models and draft the proof."""
    agents = registry_agents()
    if args.agent not in agents:
        w("'%s' is not in %s. An agent starts as a row there.\n"
          % (args.agent, os.path.relpath(REGISTRY, ROOT)))
        return 1

    source = source_of(args.agent)
    if source is None:
        w("Nothing in revit/Heron.Revit.Addin/ claims '%s', so there is no\n"
          "add-in agent to prove. This tool is for the add-in half only.\n"
          % args.agent)
        return 1

    live = {str(getattr(b, "pid", "")): b for b in bridges(args.client_id)}
    for want in (args.first, args.second):
        if str(want) not in live:
            w("No connected Revit has session %s. `sessions` lists them.\n" % want)
            return 1

    first, second = live[str(args.first)], live[str(args.second)]

    # THE MODEL IS NAMED WITH ITS SIZE, WHATEVER OPERATION WAS RUN.
    #
    # The fragment proofs record `model: PIPE (3,332 elements), Revit 2020,
    # session 8084` - the size is part of naming WHICH model, because two
    # files can share a title and a proof against the wrong one is worse than
    # no proof. Most list agents report their own element count, but
    # `list_rooms` reports rooms, so asking the operation alone produced
    # `test projject (size not reported elements)`. One extra cheap call, and
    # the record names the model properly every time.
    where_a = describe(ask(first, "count_elements"))
    where_b = describe(ask(second, "count_elements"))

    # THE SAME ARGUMENTS GO TO BOTH MODELS, AND THAT IS THE WHOLE POINT.
    #
    # Tracking asks whether the ANSWER follows the MODEL. Varying the input
    # between the two runs would let a different answer come from the
    # different input rather than from the different model, which is the one
    # thing this file exists to rule out. So `--arg` is parsed once and sent
    # twice, unchanged.
    #
    # WHY IT EXISTS. `track` sent a bare operation, and three operations
    # refuse without an argument - `read_parameters` answers `no_category`,
    # `select_by_category` answers `operation_failed`. Measured 2026-09-19:
    # HERON-REVIT-PAR-011 could not be tracked at all, then moved 0 -> 2
    # elements and 0 -> 99 parameters the moment `category=Pipes` was handed
    # to it by hand. The agent was never the problem; there was no box to
    # type in.
    try:
        op_args = parse_args(args.arg)
    except ValueError as why:
        w("%s\n" % why)
        w("Nothing was drafted.\n")
        return 1

    a = ask(first, args.operation, op_args)
    b = ask(second, args.operation, op_args)

    for label, reply, session in (("first", a, args.first),
                                  ("second", b, args.second)):
        if not reply.get("ok"):
            w("The %s run (session %s) was refused: %s - %s\n"
              % (label, session, reply.get("error"),
                 (reply.get("message") or "")[:100]))
            w("Nothing was drafted.\n")
            return 1

    # THE ONE REFUSAL THAT MATTERS. Two readings of the SAME model prove
    # nothing at all about whether the answer follows the input - they are one
    # case run twice, and a fragment that succeeds while doing nothing passes
    # ten runs and a thousand.
    if (a.get("document") or "?") == (b.get("document") or "?"):
        w("Both sessions have the SAME model open (%s).\n" % (a.get("document"),))
        w("Tracking needs two DIFFERENT models: the whole argument is that the\n"
          "answer MOVED to match the input. Two readings of one model cannot\n"
          "show that. Nothing was drafted.\n")
        return 1

    moved, same = compare(a, b)

    gaps = []

    # THIN TRACKING IS NOT STRONG TRACKING, AND THE FIRST VERSION OF THIS FILE
    # COULD NOT TELL THEM APART.
    #
    # Proved against LVL-027 on 2026-09-18: one number moved (onNoLevel, 3,497
    # -> 3,529) and FIVE held the same, because both models happen to hold two
    # levels and no grids. The draft read as a clean result. It is not one - an
    # agent that reported a constant `levelCount: 2` whatever it was handed
    # would produce exactly that row, and the single moving number is the only
    # thing standing between this evidence and no evidence at all.
    #
    # No threshold is invented. The RATIO is stated and the reader judges,
    # because how much movement is enough depends on the two models somebody
    # chose - which is a fact about the arrangement, not about the agent.
    if moved and len(moved) < len(same):
        gaps.append(
            "TRACKING IS THIN: %d number(s) moved and %d held the same. The "
            "ones that held are consistent with an agent reading the model AND "
            "with one that ignores it, so the argument rests on the %d that "
            "moved. Two models with more between them would carry it further"
            % (len(moved), len(same), len(moved)))

    if not moved:
        gaps.append(
            "NOTHING MOVED between the two models - every comparable number is "
            "identical. That is not a pass: it is what an agent falling back to "
            "a cached document or the active view would also produce. Either "
            "the two models genuinely hold the same counts, in which case a "
            "third model is needed, or the agent is not reading the one it was "
            "given")
    gaps.append(
        "a negative case in D-30's original sense was not run, and could not "
        "be: this agent describes whatever model it is handed, so no input "
        "makes it correctly return nothing. D-53 tracking is the substitute "
        "and it is what the two runs above are")

    draft = {
        "agent": args.agent,
        "name": agents[args.agent],
        "operation": args.operation,
        "date": datetime.date.today().isoformat(),
        "by": "",
        "source": os.path.relpath(source, ROOT).replace(os.sep, "/"),
        "fingerprint": fingerprint(source),
        "first_model": "%s, session %s" % (where_a, args.first),
        "second_model": "%s, session %s" % (where_b, args.second),
        "moved": ", ".join("%s: %s -> %s" % (k, x, y) for k, x, y in moved[:12])
                 or NOT_ESTABLISHED,
        "held_same": ", ".join("%s: %s" % (k, v) for k, v in same[:8]) or "(none)",
        "gaps": gaps,
    }
    path = write_draft(args.agent, draft)
    w("\nDrafted %s\n" % os.path.relpath(path, ROOT).replace(os.sep, "/"))
    w("  %d number(s) moved, %d held the same.\n" % (len(moved), len(same)))
    w("\nRead it, then sign it under your own name:\n")
    w("  python tools/prove-agent.py accept %s --by \"Your Name\"\n" % args.agent)
    w("\nA draft is not a proof. The last step is a judgement no machine makes.\n")
    return 0


# Keys that are the ENVELOPE rather than the answer. `document` is the model's
# own name - the input - and counting it would make every run pass.
ENVELOPE = ("ok", "document", "projectKey", "reads", "placedElements", "elements")

# How far into a list to look, and how many of its items. A reply that lists
# 3,000 elements should not produce 3,000 rows of evidence; the first few
# settle the question, and the LENGTH of the list is compared whatever it is.
LIST_ITEMS = 5


def _scalars(item, prefix, into_moved, into_same, other):
    """Compare one list item's own fields against the matching item.

    NAMES COUNT HERE, AND THEY DO NOT AT THE TOP LEVEL. The distinction is
    not a fudge: at the top level a string is nearly always the document
    name, which is what was ASKED. Inside a list it is a thing the agent
    FOUND in the model, which is what was ANSWERED. `linkedCad[0].name`
    is the answer and nothing else.
    """
    if not isinstance(item, dict) or not isinstance(other, dict):
        if item != other:
            into_moved.append((prefix, item, other))
        else:
            into_same.append((prefix, item))
        return
    for key in sorted(set(item) & set(other)):
        x, y = item.get(key), other.get(key)
        if isinstance(x, bool) or isinstance(y, bool):
            continue
        if not isinstance(x, (int, float, str)) or not isinstance(y, (int, float, str)):
            continue
        where = "%s.%s" % (prefix, key)
        if x != y:
            into_moved.append((where, x, y))
        else:
            into_same.append((where, x))


def compare(a, b):
    """Which answers moved between two replies, and which did not.

    TOP LEVEL: only NUMBERS, and only keys both replies carry. A string that
    differs up here is usually the document name, which is the input rather
    than the answer, and counting it as movement would make every run pass.

    INSIDE A LIST: the length, and then each item's own numbers AND names.
    Added 2026-09-19, because reading only the top level was quietly losing
    two agents that plainly do read the model:

      IMP-019   `linkedCadCount` is 1 in both models, so nothing moved - while
                `linkedCad[0].name` held `box.dwg` against
                `Project1 - Section - Section 1.dwg`. The count of one CAD
                link against one CAD link was the only thing this function
                was allowed to see.

      LVL-027   both models hold two levels named the same at the same
                elevations, so every top-level number held but `onNoLevel`.
                Inside `levels[]`: 8 and 8 elements against 29 and 7. The
                per-level counts are the agent's actual answer, and they move
                in BOTH directions at once - which is harder to fake than a
                single total, not easier.

    A list is compared POSITIONALLY, and that is a real limit rather than an
    oversight: two models may list the same things in a different order, and
    a positional comparison would call that movement. It is the right default
    because every reply seen so far sorts its list, and because the failure
    mode is a draft that looks STRONGER than it is - so the gap text, and a
    person, still have to read it. Row H1 of docs/NEEDS-CHECKING.md.
    """
    moved, same = [], []
    for key in sorted(set(a) & set(b)):
        if key in ENVELOPE:
            continue
        x, y = a.get(key), b.get(key)
        if isinstance(x, bool) or isinstance(y, bool):
            continue

        if isinstance(x, list) and isinstance(y, list):
            where = "%s[]" % key
            if len(x) != len(y):
                moved.append((where, len(x), len(y)))
            else:
                same.append((where, len(x)))
            for i in range(min(len(x), len(y), LIST_ITEMS)):
                _scalars(x[i], "%s[%d]" % (key, i), moved, same, y[i])
            continue

        if not isinstance(x, (int, float)) or not isinstance(y, (int, float)):
            continue
        if x != y:
            moved.append((key, x, y))
        else:
            same.append((key, x))
    return moved, same


def cmd_review(args):
    """Read a draft, gaps first."""
    if not os.path.isdir(DRAFTS):
        w("No drafts. Run `track` first.\n")
        return 0
    names = sorted(n[:-5] for n in os.listdir(DRAFTS) if n.endswith(".yaml"))
    if args.agent:
        names = [n for n in names if n == args.agent]
        if not names:
            w("No draft for %s.\n" % args.agent)
            return 1
    if not names:
        w("No drafts. Run `track` first.\n")
        return 0
    for name in names:
        body = read_yaml(draft_path(name))
        w("\n%s  %s\n" % (name, body.get("name", "")))
        w("  operation   %s\n" % body.get("operation"))
        w("  first       %s\n" % body.get("first_model"))
        w("  second      %s\n" % body.get("second_model"))
        w("  moved       %s\n" % body.get("moved"))
        w("  held same   %s\n" % body.get("held_same"))
        gaps = body.get("gaps") or []
        w("  gaps        %d\n" % len(gaps))
        for one in gaps:
            w("      - %s\n" % one)
        w("  signed by   %s\n" % (body.get("by") or "NOBODY YET"))
    w("\nRead `gaps` first. A draft with gaps is a job half done.\n")
    return 0


def cmd_accept(args):
    """Sign a draft into a proof. The name is the signature."""
    body = read_yaml(draft_path(args.agent))
    if body is None:
        w("No draft for %s. Run `track` first.\n" % args.agent)
        return 1
    if not args.by.strip():
        w("--by is empty. D-30 records a proof under a person's NAME, not a "
          "tick.\n")
        return 1

    body["by"] = args.by.strip()
    body["date"] = datetime.date.today().isoformat()
    body["heron-status"] = "DRAFT"          # promotion stays a separate act

    if not os.path.isdir(PROOFS):
        os.makedirs(PROOFS)
    with io.open(proof_path(args.agent), "w", encoding="utf-8",
                 newline="\n") as fh:
        fh.write(as_yaml(body))
    os.remove(draft_path(args.agent))

    w("\nRecorded %s\n"
      % os.path.relpath(proof_path(args.agent), ROOT).replace(os.sep, "/"))
    w("  signed by %s\n" % body["by"])
    w("\nThe draft is gone - one fact, one home.\n")
    w("`heron-status` in the agent's own file is UNCHANGED. Promoting it is a\n"
      "separate, deliberate act, and it is yours.\n")
    return 0


def cmd_check(_args):
    """Which recorded proofs have gone stale."""
    if not os.path.isdir(PROOFS):
        w("No proofs recorded yet.\n")
        return 0
    stale, fine, missing = [], [], []
    for name in sorted(os.listdir(PROOFS)):
        if not name.endswith(".yaml"):
            continue
        agent = name[:-5]
        body = read_yaml(proof_path(agent))
        source = source_of(agent)
        if source is None:
            missing.append(agent)
            continue
        now = fingerprint(source)
        if now != body.get("fingerprint"):
            stale.append(agent)
        else:
            fine.append(agent)
    w("\n%d proof(s): %d current, %d STALE, %d whose source is gone\n"
      % (len(fine) + len(stale) + len(missing), len(fine), len(stale),
         len(missing)))
    for agent in stale:
        w("  STALE    %s - its source changed after the proof was signed\n"
          % agent)
    for agent in missing:
        w("  NO FILE  %s - nothing in revit/ claims it any more\n" % agent)
    if stale or missing:
        w("\nA stale proof is not a wrong answer - it is an answer about code "
          "that has since moved on.\n")
        return 1
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Prove an add-in agent against real models, D-53 tracking.")
    sub = parser.add_subparsers(dest="command")

    sessions = sub.add_parser("sessions",
                              help="what Revit sessions are visible")
    sessions.add_argument("--client-id", default=None,
                          help="identify as this chat - see bridges()")

    track = sub.add_parser("track", help="run one agent against two models")
    track.add_argument("agent")
    track.add_argument("--operation", required=True,
                       help="the add-in operation, e.g. list_levels")
    track.add_argument("--first", required=True, help="first session id")
    track.add_argument("--second", required=True, help="second session id")
    track.add_argument("--arg", action="append", default=[], metavar="KEY=VALUE",
                       help="an operation argument, repeatable. The SAME "
                            "arguments go to both models - see cmd_track()")
    track.add_argument("--client-id", default=None,
                       help="identify as this chat, so a Revit the chat is "
                            "already using does not refuse. See bridges()")

    review = sub.add_parser("review", help="read drafts, gaps first")
    review.add_argument("agent", nargs="?")

    accept = sub.add_parser("accept", help="sign a draft into a proof")
    accept.add_argument("agent")
    accept.add_argument("--by", required=True, help="your name - the signature")

    sub.add_parser("check", help="which recorded proofs have gone stale")

    args = parser.parse_args(argv)
    if not args.command:
        parser.print_help()
        return 0
    return {
        "sessions": cmd_sessions,
        "track": cmd_track,
        "review": cmd_review,
        "accept": cmd_accept,
        "check": cmd_check,
    }[args.command](args)


if __name__ == "__main__":
    sys.exit(main())
