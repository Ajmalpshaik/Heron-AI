# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-BLD-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent Builder - it derives the implementation from the contract, writes no
test, and runs nothing at all.

    python brain/heron_builder.py

WHAT IT IS FOR (docs/28, HERON-AHR-BLD-004)
--------------------------------------------
"Implements it." The Architect designed a contract; this turns that contract
into the file that will keep it.

DERIVED FROM THE CONTRACT, NOT FILLED INTO A TEMPLATE
------------------------------------------------------
The function's parameters are the contract's `input` names, required ones
first. The failure states it can return are the contract's `failures`,
named in the source so HERON-AHR-VAL-013's check finds them. The docstring
quotes the contract's own output description.

A stored template would be a second copy of a shape that already exists in
data, and this repository's whole register of mistakes is copies going
stale. `tools/new-agent.py` has templates because a person runs it and
fills them in; an agent has the contract in its hand and can do better.

IT WRITES NO TEST, AND THAT IS NOT AN OMISSION
------------------------------------------------
docs/24 will not let an agent into TESTING unless the test's author and the
implementer are different - HERON-AHR-DEP-012 enforces it, and refuses when
they match. So a test written by this agent could never carry the agent it
tests into TESTING. It would be a file somebody has to notice and throw
away, and until they noticed it would look like the test had been done.

So the Builder writes the implementation and the contract, names itself as
`implemented-by`, and returns `test_owed` saying which file somebody else
has to write. The gate downstream is what makes that real; this just stops
pretending otherwise.

IT RUNS NOTHING. Q-56
----------------------
Generating code and then executing it is the point at which the sandbox's
containment would have to be real, and it is not. HERON-AHR-SBX-016 says so
at the top of its own file: it restrains a cooperating agent and it does not
contain a hostile one. Q-56 in docs/OPEN-QUESTIONS.md is that question, open
and waiting on the owner.

Nothing here imports the sandbox, executes, evaluates or compiles anything.
The Builder produces text. Running it is a separate decision that somebody
has to take deliberately, which is the correct shape for it whichever way
Q-56 is answered.

THE HEADER CLAIMS NOTHING
--------------------------
Generated files carry `Heron-Agent: none` and `Heron-Status: DISCOVERED`.
tools/agent-count.py reports an agent as BUILT when a source file claims it,
so a stub claiming its agent would raise the built count for work nobody has
done. That exact mistake happened once already, through a docstring that
quoted the header, and it is why the count is derived rather than stated.

A DRY RUN BY DEFAULT
---------------------
`build()` returns the files it would write. Passing `write=True` writes them,
and even then it refuses if any destination exists: a builder that overwrites
an implementation somebody was working on is worse than one that stops.
"""

import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Where an implementation may go. `brain` may not depend on `mcp` or
# `revit` (D-48), and this agent only ever writes Python.
LAYERS = {"brain": "brain", "mcp": os.path.join("mcp", "server"),
          "platform": "platform"}


def module_name(name):
    """'Queue Manager' -> 'queue_manager'. Predictable, and never clever."""
    slug = re.sub(r"[^a-z0-9]+", "_", str(name or "").lower()).strip("_")
    slug = re.sub(r"_agent$", "", slug)
    return slug or "agent"


def _fields(block):
    """[(name, spec)] with the required ones first, then alphabetical."""
    items = [(k, v if isinstance(v, dict) else {})
             for k, v in (block or {}).items()]
    return sorted(items, key=lambda kv: (not kv[1].get("required"), kv[0]))


def implementation(contract, name, layer="brain"):
    """
    The module's text, derived from the contract.

    Every part of it comes from the contract: the signature from `input`,
    the failure states from `failures`, the return shape from `output`.
    """
    agent = contract["agent"]
    module = module_name(name)
    inputs = _fields(contract.get("input"))
    outputs = _fields(contract.get("output"))
    failures = list(contract.get("failures") or [])

    signature = ", ".join(
        key if spec.get("required") else "%s=None" % key
        for key, spec in inputs) or ""

    lines = [
        "# -*- coding: utf-8 -*-",
        "# Heron-Agent:  none",
        "# Heron-Step:   15",
        "# Heron-Status: DISCOVERED",
        "# Heron-Since:  0.1.0",
        "# Heron-Layer:  %s" % layer,
        "# See docs/29-metadata-standard.md",
        "",
        '"""',
        "%s - TODO: one line saying what it is FOR, not what it is." % name,
        "",
        "Its contract is brain/agents/%s.yaml, and the contract is the" % agent,
        "promise - this file is only how the promise is kept today.",
        "",
        "THE HEADER CLAIMS NOTHING ON PURPOSE. `Heron-Agent: none` and",
        "DISCOVERED, because tools/agent-count.py reports an agent as BUILT",
        "when a source file claims it, and nobody has done this work yet.",
        "Put %s in the header when the promise below is kept." % agent,
        "",
        "ITS TEST IS OWED AND IS NOT HERE. docs/24 will not let this agent",
        "into TESTING unless the test's author and the implementer differ,",
        "so a test generated beside this file could never carry it there.",
        '"""',
        "",
        "# The failure states the contract declares. Every one of them has to",
        "# be reachable from the code below, or HERON-AHR-VAL-013 will say so.",
        "FAILURES = (",
    ]
    for state in failures:
        lines.append('    "%s",' % state)
    lines += [
        ")",
        "",
        "",
        "def run(%s):" % signature,
        '    """',
        "    TODO: keep the contract's promise.",
        "",
        "    Returns %s." % (", ".join(key for key, _spec in outputs)
                             or "what the contract's `output` declares"),
        "    Refuses with one of FAILURES, and with nothing else - a caller",
        "    handling failures exhaustively has no branch for a state the",
        "    contract never declared.",
        '    """',
        '    raise NotImplementedError(',
        '        "%s is DISCOVERED: the contract is written and nothing "' % agent,
        '        "keeps it yet.")',
        "",
    ]
    return "\n".join(lines)


def build(contract, built_by, name=None, layer="brain", write=False,
          root=None, known_ids=None):
    """
    {files, test_owed, implemented_by, unjudged, why} - or a refusal.

    A dry run by default: `files` is what WOULD be written. Nothing here
    executes, imports or compiles what it produced.
    """
    if not isinstance(contract, dict) or not contract.get("agent"):
        return {"refused": "NO_CONTRACT",
                "why": "the Builder implements a contract "
                       "(HERON-AHR-ARC-003's output), and none was given. "
                       "Building from a description would produce a file "
                       "whose promise nobody wrote down."}

    # THE BUILDER MUST NAME ITSELF. docs/24's TESTING gate compares the
    # implementer with the test's author, and an unnamed implementer makes
    # that comparison impossible rather than merely awkward.
    built_by = str(built_by or "").strip()
    if not built_by:
        return {"refused": "NO_BUILDER_NAMED",
                "why": "whoever builds this has to be named. docs/24 lets an "
                       "agent into TESTING only when the test's author and "
                       "the implementer differ, and an unnamed implementer "
                       "cannot be compared with anybody."}

    import heron_contract as CON
    if known_ids is None:
        try:
            known_ids = CON.registry_ids()
        except IOError as exc:
            return {"refused": "REGISTER_UNREADABLE", "why": str(exc)}

    problems = CON.validate(contract, known_ids, where="the contract")
    if problems:
        return {"refused": "CONTRACT_INVALID",
                "why": "it does not validate, so there is no promise to "
                       "implement: %s" % "; ".join(problems)}

    if layer not in LAYERS:
        return {"refused": "NO_SUCH_LAYER",
                "why": "'%s' is not a layer. docs/02 has %s, and an "
                       "implementation put in the wrong one is a dependency "
                       "tools/check-structure.py will refuse (D-48)."
                       % (layer, ", ".join(sorted(LAYERS)))}

    agent = contract["agent"]
    name = name or agent
    module = module_name(name)
    root = root or ROOT

    files = {
        os.path.join(LAYERS[layer],
                     "heron_%s.py" % module).replace(os.sep, "/"):
            implementation(contract, name, layer),
    }

    # NEVER OVERWRITE. Every destination is checked before any of them is
    # written, so a refusal leaves nothing half-built.
    existing = sorted(path for path in files
                      if os.path.exists(os.path.join(root, path)))
    if existing:
        return {"refused": "WOULD_OVERWRITE",
                "why": "%s already exists. A builder that overwrites an "
                       "implementation somebody was working on is worse than "
                       "one that stops." % ", ".join(existing)}

    written = []
    if write:
        for path, text in sorted(files.items()):
            full = os.path.join(root, path)
            try:
                folder = os.path.dirname(full)
                if folder and not os.path.isdir(folder):
                    os.makedirs(folder)
                with io.open(full, "w", encoding="utf-8") as handle:
                    handle.write(text)
            except (IOError, OSError) as exc:
                return {"refused": "WRITE_FAILED",
                        "why": "%s could not be written: %s. %d file(s) were "
                               "written before it and are listed in the "
                               "refusal rather than left unmentioned."
                               % (path, exc, len(written)),
                        "written": written}
            written.append(path)

    test_owed = "tests/test_%s.py" % module
    unjudged = [
        "NOTHING WAS RUN. The Builder produces text and does not execute, "
        "import or compile it. Running a newly generated agent is where a "
        "sandbox's containment would have to be real, and HERON-AHR-SBX-016 "
        "says at the top of its own file that it restrains a cooperating "
        "agent and does not contain a hostile one - Q-56, open.",
        "the implementation is a STUB. It raises NotImplementedError, its "
        "header claims no agent and reads DISCOVERED, so the built count "
        "does not move for work nobody has done.",
        "whether the derived signature is the right shape is a reading. The "
        "parameters are the contract's inputs, required first; whether those "
        "are the right inputs was the Architect's question and is still "
        "unjudged there.",
    ]

    return {"files": files, "written": written, "test_owed": test_owed,
            "implemented_by": built_by, "unjudged": unjudged,
            "why": "%s: %d file(s) %s, %d failure state(s) named, and "
                   "%s is owed by somebody who is not %s."
                   % (agent, len(files), "written" if write else "planned",
                      len(contract.get("failures") or []), test_owed,
                      built_by)}


def main(argv):
    import heron_contract as CON
    known = CON.registry_ids()

    print("AGENT BUILDER   derived from the contract, and it runs nothing")
    print("=" * 70)

    contract = {
        "agent": "HERON-AHR-BLD-004",
        "version": "1.0.0",
        "input": {"view": {"type": "string", "required": True,
                           "description": "The view to read."},
                  "limit": {"type": "number", "required": False,
                            "description": "How many at most."}},
        "output": {"findings": {"type": "list",
                                "description": "What it found."}},
        "allowed-tools": [],
        "timeout-seconds": 30,
        "failures": ["NO_VIEW", "NOTHING_FOUND"],
        "retry": {"attempts": 0, "on-failures": []},
    }

    for label, kwargs in [
            ("no contract", dict(contract=None, built_by="a session")),
            ("nobody named as the builder",
             dict(contract=contract, built_by="")),
            ("a contract that does not validate",
             dict(contract=dict(contract, version="nope"),
                  built_by="a session")),
            ("a layer that does not exist",
             dict(contract=contract, built_by="a session", layer="nowhere")),
            ("a module name already taken",
             dict(contract=contract, built_by="a session",
                  name="Contract")),
            ("a fresh one, planned and not written",
             dict(contract=contract, built_by="a session",
                  name="Duct Sizing Reviewer")),
    ]:
        kwargs.setdefault("known_ids", known)
        answer = build(**kwargs)
        if answer.get("refused"):
            print("  %-42s %s" % (label, answer["refused"]))
            print("      %s" % answer["why"][:96])
        else:
            print("  %-42s planned" % label)
            print("      %s" % answer["why"])
            for path in sorted(answer["files"]):
                print("        would write  %s" % path)
            print("        owes a test  %s (not written here)"
                  % answer["test_owed"])

    print()
    answer = build(contract, "a session", name="Duct Sizing Reviewer",
                   known_ids=known)
    print(answer["files"][sorted(answer["files"])[0]])
    print("  Every part of that came out of the contract - the signature")
    print("  from its inputs, FAILURES from its failure states, the return")
    print("  line from its outputs. No template was filled in, and nothing")
    print("  above was executed.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
