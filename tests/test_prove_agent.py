#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The only route 26 add-in agents have out of DRAFT, and nothing was holding it.

    python tests/test_prove_agent.py

`tools/prove-agent.py` is 934 lines and no suite loaded or ran it - one of
four tools measured 2026-09-22 that a suite NAMES in prose and never
executes, and that CI does not run either. A call site is not an execution,
and a path in a docstring is neither.

It is the file that turns evidence into a signed proof. Its own header states
the three rules it copies from the fragment path, and the first is the one
this suite is built around:

    The machine never signs. `by:` is required and written EMPTY.

WHAT IT CANNOT DO HERE. `track`, `vary` and `sessions` need two live Revit
sessions with different models open, so they are NOT run - that is `T1` and
its neighbours in `docs/NEEDS-CHECKING.md`. Everything this suite touches
happens before a bridge is opened, or after one has closed: the refusals, the
fingerprint, the comparison, and the signing.

EVERY CASE BUILDS ITS OWN TREE, with `ROOT`, `REGISTRY`, `DRAFTS` and
`PROOFS` pointed at a temp folder. A case that signed into `brain/` would be
writing a proof, which is the one thing a test must never do.
"""

import importlib.util
import io
import json
import os
import re
import shutil
import sys
import tempfile
import types

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "prove-agent.py")
CLIENT = os.path.join(ROOT, "mcp", "client")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("prove_agent", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


AGENT = "HERON-REVIT-LVL-027"

DRAFT = {
    "agent": AGENT,
    "name": "List levels",
    "operation": "list_levels",
    "arguments": "(none)",
    "date": "2026-09-22",
    "by": "",
    "source": "revit/Heron.Revit.Addin/RevitLevels.cs",
    "fingerprint": "abc123def456aaaa",
    "first_model": "A (10 elements), session 1",
    "second_model": "B (20 elements), session 2",
    "moved": "levelCount: 2 -> 5",
    "held_same": "(none)",
    "gaps": ["a negative case in D-30's original sense was not run"],
}


def tree(tool, agents=(AGENT,)):
    """A repository of the right shape, in a temp folder."""
    home = tempfile.mkdtemp(prefix="heron-prove-agent-")
    addin = os.path.join(home, "revit", "Heron.Revit.Addin")
    os.makedirs(addin)
    os.makedirs(os.path.join(home, "docs"))
    rows = ["| Id | Name | Step |", "|---|---|---|"]
    for one in agents:
        rows.append("| `%s` | **List levels** | 17 |" % one)
        io.open(os.path.join(addin, "%s.cs" % one), "w", encoding="utf-8",
                newline="\n").write("// Heron-Agent:  %s\nclass X {}\n" % one)
    io.open(os.path.join(home, "docs", "28-agent-registry.md"), "w",
            encoding="utf-8", newline="\n").write("\n".join(rows) + "\n")

    tool.ROOT = home
    tool.REGISTRY = os.path.join(home, "docs", "28-agent-registry.md")
    tool.DRAFTS = os.path.join(home, "brain", "agent-proof-drafts")
    tool.PROOFS = os.path.join(home, "brain", "agent-proofs")
    return home


def sign(tool, name, agent=AGENT):
    """Draft, then accept under `name`. Returns (exit, proof, draft is left)."""
    tool.write_draft(agent, dict(DRAFT, agent=agent))
    code = tool.cmd_accept(types.SimpleNamespace(agent=agent, by=name))
    proof = tool.read_yaml(tool.proof_path(agent))
    return code, proof, os.path.exists(tool.draft_path(agent))


def quiet(fn, *a, **k):
    """Run it with its own chatter swallowed."""
    was = sys.stdout
    sys.stdout = io.StringIO()
    try:
        return fn(*a, **k), was.write("") or sys.stdout.getvalue()
    finally:
        sys.stdout = was


class _Pipe(object):
    """A pipe handle that answers every line it is sent, and keeps the ops."""

    def __init__(self, said, answer):
        self.said, self.answer, self.out = said, answer, b""

    def write(self, data):
        for line in data.decode("utf-8").splitlines():
            if line.strip():
                self.said.append(json.loads(line).get("op"))
                self.out += (json.dumps(self.answer) + "\n").encode("utf-8")

    def read(self, size):
        chunk, self.out = self.out[:size], self.out[size:]
        return chunk

    def close(self):
        pass


class _Stub(object):
    """A Revit for cmd_vary: one reply per value of the varied argument."""

    def __init__(self, replies):
        self.pid, self.replies = "1", replies

    def request(self, op, op_args=None, **_kw):
        if op == "count_elements":
            return {"ok": True, "document": "A", "count": 10}
        return dict(self.replies[list(op_args.values())[0]], ok=True)


def vary_gap(tool, operation, replies):
    """Run cmd_vary on a stub Revit; the draft's text, or '' if none."""
    was = tool.bridges
    tool.bridges = lambda client_id=None: [_Stub(replies)]
    try:
        code, _ = quiet(tool.cmd_vary, types.SimpleNamespace(
            agent=AGENT, operation=operation, session="1",
            arg_name="category", arg_values=",".join(replies), client_id=None))
    finally:
        tool.bridges = was
    path = tool.draft_path(AGENT)
    if code != 0 or not os.path.exists(path):
        return ""
    text = io.open(path, encoding="utf-8").read()
    os.remove(path)
    return text


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/prove-agent.py loads")
    if tool is None:
        print()
        print("FAILED - it did not import")
        return 1

    for name in ("cmd_accept", "cmd_review", "cmd_check", "cmd_track",
                 "cmd_vary", "write_draft", "read_yaml", "as_yaml",
                 "fingerprint", "compare", "parse_args", "registry_agents"):
        check(callable(getattr(tool, name, None)), "and it has %s()" % name)
    for name in ("ROOT", "REGISTRY", "DRAFTS", "PROOFS"):
        check(getattr(tool, name, None) is not None,
              "and names %s, so a test can point it elsewhere" % name)
    if not callable(getattr(tool, "cmd_accept", None)):
        print()
        print("FAILED - nothing to sign with")
        return 1

    home = tree(tool)
    try:
        print()
        print("1. THE SIGNATURE IS WRITTEN AS THE PERSON TYPED IT")
        print("   It is the scarcest input this repository has, and a proof")
        print("   that misquotes it is a record nobody can stand behind.")
        for label, name in (("an ordinary name", "Ajmal PS"),
                            ("an apostrophe", "Ajmal O'Shea"),
                            ("a colon and a comma", "Ajmal PS, BIM Lead: Heron")):
            (code, proof, left), _ = quiet(sign, tool, name)
            check(code == 0, "%s is accepted" % label)
            check(proof is not None and proof.get("by") == name,
                  "and reads back as %r, not %r"
                  % (name, proof.get("by") if proof else None))

        print()
        print("2. A NAME IT CANNOT WRITE IS REFUSED, AND THE DRAFT SURVIVES")
        print("   Not a theoretical input: a name pasted across a line break")
        print("   carries one. The draft is the evidence, and `accept` deletes")
        print("   it - so a bad write with the draft already gone spends a")
        print("   signature on a record that cannot be made again.")
        (code, proof, left), spoke = quiet(sign, tool, "Ajmal\nPS")
        check(code != 0, "a name with a line break is refused, exit %r" % code)
        check(left, "and the draft it was signing is STILL THERE")
        check(proof is None or proof.get("by") != "'Ajmal",
              "and no proof was left carrying %r as the signature"
              % (proof.get("by") if proof else None))
        if os.path.exists(tool.draft_path(AGENT)):
            os.remove(tool.draft_path(AGENT))
        if os.path.exists(tool.proof_path(AGENT)):
            os.remove(tool.proof_path(AGENT))

        print()
        print("3. THE STATUS IN THE PROOF IS THE TOOL'S OWN")
        print("   cmd_accept sets heron-status: DRAFT and says out loud that")
        print("   promotion is a separate, deliberate act. Nothing arriving")
        print("   through --by may overrule that.")
        (code, proof, left), _ = quiet(
            sign, tool, "Ajmal PS\nheron-status: PROVEN")
        got = proof.get("heron-status") if proof else None
        check(got in (None, "DRAFT"),
              "the proof's heron-status is not set from the name, and it is %r"
              % (got,))
        if os.path.exists(tool.draft_path(AGENT)):
            os.remove(tool.draft_path(AGENT))
        if os.path.exists(tool.proof_path(AGENT)):
            os.remove(tool.proof_path(AGENT))

        print()
        print("3b. A DRAFT IT CANNOT WRITE IS REFUSED TOO")
        print("    `track` builds the draft out of a model's own answers, and")
        print("    a document title carries whatever the person who saved it")
        print("    typed. The same guard has to stand at both writers.")
        refused = False
        try:
            tool.write_draft(AGENT, dict(DRAFT, first_model="A\nsession 1"))
        except ValueError:
            refused = True
        check(refused, "a draft field with a line break is refused")
        check(not os.path.exists(tool.draft_path(AGENT)),
              "and no half-written draft is left behind")

        print()
        print("3c. THE FILE ON DISK IS READ BACK BEFORE THE DRAFT IS DELETED")
        print("    The guard above rules out what this writer cannot")
        print("    represent. This rules out everything else - a short write,")
        print("    a full disk - and the draft is the only other copy.")
        tool.write_draft(AGENT, dict(DRAFT))
        was_read = tool.read_yaml
        tool.read_yaml = lambda path: (
            {"by": "somebody else"} if path == tool.proof_path(AGENT)
            else was_read(path))
        try:
            code, spoke = quiet(tool.cmd_accept,
                                types.SimpleNamespace(agent=AGENT, by="Ajmal PS"))
        finally:
            tool.read_yaml = was_read
        check(code == 1,
              "a proof that does not read back as signed fails, exit %r" % code)
        check(os.path.exists(tool.draft_path(AGENT)),
              "and the draft it came from is STILL THERE")
        os.remove(tool.draft_path(AGENT))
        if os.path.exists(tool.proof_path(AGENT)):
            os.remove(tool.proof_path(AGENT))

        print()
        print("4. D-30: a proof is signed under a NAME, not a tick")
        tool.write_draft(AGENT, dict(DRAFT))
        code, _ = quiet(tool.cmd_accept,
                        types.SimpleNamespace(agent=AGENT, by="   "))
        check(code == 1, "an empty --by is refused, exit %r" % code)
        check(os.path.exists(tool.draft_path(AGENT)),
              "and the draft is untouched")
        os.remove(tool.draft_path(AGENT))

        print()
        print("5. The prover has NO WRITE PATH into what it proves")
        print("   'it never promotes an agent' is a property of the code's")
        print("   reach, not of its good intentions.")
        was = tool.DRAFTS
        tool.DRAFTS = os.path.join(tool.ROOT, "revit", "Heron.Revit.Addin")
        refused = False
        try:
            tool.write_draft(AGENT, dict(DRAFT))
        except ValueError:
            refused = True
        finally:
            tool.DRAFTS = was
        check(refused, "a draft aimed inside revit/ is refused")

        print()
        print("6. The fingerprint is about the CODE, not about the machine")
        print("   Hashing raw bytes made all sixteen proven fragments read")
        print("   STALE the first time they were read in a Linux container.")
        lf = os.path.join(home, "revit", "Heron.Revit.Addin", "lf.cs")
        crlf = os.path.join(home, "revit", "Heron.Revit.Addin", "lf2.cs")
        io.open(lf, "wb").write(b"// Heron-Agent:  X\nclass A {}\n")
        io.open(crlf, "wb").write(b"// Heron-Agent:  X\r\nclass A {}\r\n")
        a, b = tool.fingerprint(lf), tool.fingerprint(crlf)
        check(a is not None and b is not None, "both files fingerprint")
        check(a != b, "two DIFFERENT paths give different hashes (the path is "
                      "part of it), and they are %r / %r" % (a, b))
        same = os.path.join(home, "same.cs")
        io.open(same, "wb").write(b"class A {}\n")
        one = tool.fingerprint(same)
        io.open(same, "wb").write(b"class A {}\r\n")
        check(one == tool.fingerprint(same),
              "and the SAME file with CRLF endings hashes the same")

        print()
        print("7. compare(): what counts as the answer moving")
        envelope = {"ok": True, "document": "A", "levelCount": 2}
        other = {"ok": True, "document": "B", "levelCount": 2}
        moved, held = tool.compare(envelope, other)
        check(not moved, "the model's own NAME moving is not the answer moving")
        moved, held = tool.compare({"levelCount": 2}, {"levelCount": 5})
        check(moved == [("levelCount", 2, 5)],
              "a number that moved is reported, and it gave %r" % (moved,))
        moved, held = tool.compare(
            {"elements": [{"name": "box.dwg"}]},
            {"elements": [{"name": "site.dwg"}]})
        check(any(k.endswith(".name") for k, _x, _y in moved),
              "a NAME inside a list is the answer, and it gave %r" % (moved,))
        moved, held = tool.compare({"elements": 7}, {"elements": 9})
        check(not moved,
              "but `elements` as a bare total is envelope, and it gave %r"
              % (moved,))

        print()
        print("8. parse_args refuses what it cannot read")
        bad = False
        try:
            tool.parse_args(["category"])
        except ValueError:
            bad = True
        check(bad, "--arg with no '=' is refused")
        check(tool.parse_args(["category=Pipes"]) == {"category": "Pipes"},
              "and a good one is read")

        print()
        print("9. vary() refuses fewer inputs than D-53 asks for")
        print("   brain/heron_validate.py refuses two for a fragment, and one")
        print("   decision may not have two bars.")
        code, spoke = quiet(tool.cmd_vary, types.SimpleNamespace(
            agent=AGENT, operation="read_parameters", session="1",
            arg_name="category", arg_values="Pipes,Ducts", client_id=None))
        check(code == 1, "two values are refused before any Revit is opened")
        check(str(getattr(tool, "MIN_TRACKING_ROWS", 0)) in spoke,
              "and it says how many it wants, from its own constant")

        print()
        print("10. A COMMAND HANDS BACK THE MODEL IT HELD WHEN IT ENDS")
        print("    Each run is a conversation of its own (D-141), so a model it")
        print("    held stayed held, and the next command was refused \"in use")
        print("    by another chat\" - by this one (row 5b-1338). The real")
        print("    bridge client, a stub Revit behind a stub pipe.")
        section_10(tool)

        print()
        print("11. vary(): A NUMBER THAT ECHOES THE REQUEST IS NOT AN ANSWER")
        print("    select_by_category's `categories` counts the names asked")
        print("    for, so a category the model has none of answers")
        print("    `categories: 1, selected: 0` - and was never seen as empty")
        print("    (row 5b-1339).")
        mep = {"Ducts": {"categories": 1, "selected": 728},
               "Pipes": {"categories": 1, "selected": 488},
               "Flex Pipes": {"categories": 1, "selected": 0}}
        text = vary_gap(tool, "select_by_category", mep)
        check("NEGATIVE LEG WAS RUN" in text and "category=Flex Pipes" in text,
              "select_by_category's `categories: 1, selected: 0` is recorded "
              "as D-30's negative leg")
        check("categories: 1, selected: 0" in text,
              "and the echoed number is still in the tracking row as evidence")
        found = dict((k, {"categories": 1, "selected": n})
                     for k, n in (("Ducts", 728), ("Pipes", 488), ("Air Terminals", 309)))
        text = vary_gap(tool, "select_by_category", found)
        check(text and "NEGATIVE LEG WAS RUN" not in text,
              "but `categories: 1, selected: 309` is not empty")
        zero = dict((n, 0) for n in ("distinctParameters", "elements", "listed",
                                     "notListed", "typesRead"))
        params = {"Ducts": dict(zero, distinctParameters=69, elements=728,
                                listed=69, typesRead=3),
                  "Pipes": dict(zero, distinctParameters=72, elements=488,
                                listed=72, typesRead=1),
                  "Flex Pipes": dict(zero)}
        text = vary_gap(tool, "read_parameters", params)
        check("NEGATIVE LEG WAS RUN" in text and "category=Flex Pipes" in text,
              "read_parameters' all-zero reply is still recorded as empty")
        other = dict((k, {"categories": 1, "selected": 0, "x": n})
                     for k, n in (("Ducts", 1), ("Pipes", 2), ("Flex Pipes", 3)))
        text = vary_gap(tool, "list_somethings", other)
        check(text and "NEGATIVE LEG WAS RUN" not in text,
              "and an operation not named as echoing is judged on every number")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a signature is recorded as it was given, or it is refused")
    print("with the evidence still there to sign again.")
    return 0


def section_10(tool):
    run = getattr(tool, "run", None)
    check(callable(run), "the tool has run(), which ends every command")
    if not callable(run):
        run = tool.main
    if CLIENT not in sys.path:
        sys.path.insert(0, CLIENT)
    try:
        import heron_bridge_client as C
    except BaseException:                          # noqa: BLE001
        check(False, "the bridge client imports")
        return
    saved = dict((k, os.environ.get(k)) for k in
                 ("HERON_CONVERSATION_ID", "HERON_CLIENT_ID"))
    was_discover, was_sessions = C.discover, tool.cmd_sessions
    said = []

    def revit():
        bridge = C.Bridge({"pid": 7, "pipeName": "heron-7", "token": "t"}, "x.json")
        bridge._connect = lambda timeout: _Pipe(said, {"ok": True, "document": "A",
                                                       "count": 10})
        return bridge
    try:
        os.environ.pop("HERON_CONVERSATION_ID", None)
        C._ASKED.clear()
        C.discover = lambda prune=True: ([revit()], [], [], [])
        code, _ = quiet(run, ["sessions"])
        check(code == 0 and "count_elements" in said,
              "`sessions` asked the stub Revit about its model")
        check(said[-1:] == ["release_models"],
              "and handed it back when it ended - %r" % (said,))
        check(not C._ASKED, "and the client forgets the Revit it handed back to")

        del said[:]

        def broken(args):
            tool.bridges(args.client_id)[0].request("count_elements")
            raise RuntimeError("a command that dies half way")
        tool.cmd_sessions = broken
        died = False
        try:
            quiet(run, ["sessions"])
        except RuntimeError:
            died = True
        check(died and said[-1:] == ["release_models"],
              "a command that raises still hands back - %r" % (said,))
        tool.cmd_sessions = was_sessions

        del said[:]
        C._ASKED.clear()
        os.environ["HERON_CONVERSATION_ID"] = "a-series-of-commands"
        quiet(run, ["sessions"])
        check("count_elements" in said and "release_models" not in said,
              "with HERON_CONVERSATION_ID set nothing is handed back - the "
              "series' owner does that")
        C._ASKED.clear()

        del said[:]
        os.environ.pop("HERON_CONVERSATION_ID", None)
        quiet(run, [])
        check(said == [], "a run that opened no Revit sends nothing")
    finally:
        C.discover, tool.cmd_sessions = was_discover, was_sessions
        C._ASKED.clear()
        for key, value in saved.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value

    text = io.open(TOOL, encoding="utf-8").read()
    tail = text[text.rfind('if __name__ == "__main__":'):]
    check(re.search(r"sys\.exit\(run\(\)\)", tail) is not None,
          "and the command line goes through run(), so it is done every time")


if __name__ == "__main__":
    sys.exit(main())
