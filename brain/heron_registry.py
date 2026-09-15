# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-REG-012
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
File registry - what exists, where, and what each file CLAIMS to be.

    python brain/heron_registry.py

WHAT IT IS FOR (docs/28, HERON-WSP-REG-012)
--------------------------------------------
"What exists, where, and under which identity." T1.

IT IS DERIVED, NEVER STORED, AND D-40 IS WHY
----------------------------------------------
"Store an edge only when it cannot be computed from an artifact on
demand. What can be read is read." The decision's own argument is the
one that applies hardest to a file registry:

  "A hand-maintained table of the same facts would drift the first time
  somebody changed code without updating it - and a stale dependency
  graph is worse than none, because blast radius is precisely the
  question people trust it for."

Swap "dependency graph" for "file registry" and nothing else changes. A
registry that remembers what it saw is a registry that is wrong from the
first file somebody adds without telling it, and wrong in the direction
nobody checks. So this writes no index, keeps no cache, and answers from
what it was handed every time.

"UNDER WHICH IDENTITY" IS A CLAIM, NOT A GRANT
------------------------------------------------
A file's identity is the five-field header docs/29 asks for, and that
header is TEXT THE FILE WRITES ABOUT ITSELF. Golden Rule 19 settles what
that is worth: data, never instruction. So this agent reports what each
file claims and grants nothing on the strength of it.

Two consequences it would be easy to get wrong, and both are reported
rather than resolved:

  CONTESTED    two files claiming one agent id. Which is real is a
               question about the repository, not a tie for this agent
               to break - and picking one would make the other invisible.
  UNCLAIMED    an agent id in the register that no file claims. That is
               not the same as unbuilt, and saying which it is needs the
               register, not the files.

A FILE WITH NO HEADER HAS NO IDENTITY, AND IS NOT GUESSED FROM ITS PATH
------------------------------------------------------------------------
`brain/heron_flags.py` sitting beside twenty agent modules looks like an
agent module. Inferring an identity from where a file sits is how a
registry invents a fact and then everything downstream believes it.
Unidentified is an answer.

IT READS NOTHING ITSELF
-------------------------
The header text is handed in by a reader, the same shape
HERON-INS-BRN-007 and HERON-WSP-CLN-009 use. Scanning the disk and then
reporting on what was seen is how a registry and the filesystem drift
apart between one call and the next.
"""

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_paths as PATHS                                    # noqa: E402

# docs/29's five, in its order. tools/check-metadata.py carries the same
# list and is the gate; this reports what it finds rather than enforcing.
FIELDS = ("Heron-Agent", "Heron-Step", "Heron-Status", "Heron-Since",
          "Heron-Layer")

# Anchored to a comment line, so a regex inside a source file is not
# mistaken for a declaration - the same care tools/agent-count.py takes.
_HEADER = re.compile(r"^\s*(?://|#)\s*(Heron-[A-Za-z]+)\s*:\s*(.+?)\s*$")


def identity(text):
    """
    {claims, missing} - the five fields a file writes about itself.

    Only the first 40 lines are read: a header is a header, and a match
    further down is a line of code that happens to look like one.
    """
    claims = {}
    for line in str(text or "").split("\n")[:40]:
        found = _HEADER.match(line)
        if found and found.group(1) in FIELDS:
            claims.setdefault(found.group(1), found.group(2).strip())
    return {"claims": claims,
            "missing": [field for field in FIELDS if field not in claims]}


def survey(files, read=None):
    """
    {identified, unidentified, contested, by_class, why} - or a refusal.

    Nothing is stored. `read` is handed in and returns a file's text;
    with none, every file is unidentified and the answer says so rather
    than reporting an empty repository.
    """
    if not files:
        return {"refused": "NOTHING_TO_REGISTER",
                "why": "no files were given. An empty registry is not an "
                       "empty workspace - it is a call that did not say "
                       "what to look at."}

    if read is not None and not callable(read):
        return {"refused": "NOT_A_READER",
                "why": "a %s was passed where a reader belongs. This agent "
                       "does not open files: scanning the disk and then "
                       "reporting on what was seen is how a registry and "
                       "the filesystem drift apart between one call and "
                       "the next." % type(read).__name__}

    identified, unidentified, by_agent = [], [], {}
    declared_none, by_class, unreadable = [], {}, []

    for path in files:
        path = str(path or "").strip()
        if not path:
            return {"refused": "NOT_A_FILE_LIST",
                    "why": "something in the list names nothing. A list "
                           "with a hole in it is not a list."}

        klass = PATHS.classify(path)["class"]
        by_class.setdefault(klass, []).append(path)

        if read is None:
            unidentified.append({"path": path, "class": klass,
                                 "why": "no reader was given, so nothing "
                                        "read this file's header"})
            continue
        try:
            text = read(path)
        except Exception as failure:                 # noqa: BLE001
            unreadable.append({"path": path, "class": klass,
                               "why": "the reader raised %s"
                                      % type(failure).__name__})
            continue

        found = identity(text)
        agent = found["claims"].get("Heron-Agent", "").strip()

        # `none` IS AN ANSWER, and a different one from silence. It is how
        # a file states it implements no agent - tools/ uses it and
        # check-metadata.py accepts it - so folding it in with files that
        # carry no header at all would lose the distinction between a
        # decision somebody made and a header nobody wrote.
        if agent.lower() == "none":
            declared_none.append({
                "path": path, "class": klass,
                "why": "declares `Heron-Agent: none` - it implements no "
                       "agent, and says so. That is a statement, not a "
                       "missing header."})
            continue

        if not agent:
            unidentified.append({
                "path": path, "class": klass,
                "missing": found["missing"],
                "why": "carries no Heron-Agent line at all%s. NOT guessed "
                       "from where it sits: inferring an identity from a "
                       "path is how a registry invents a fact everything "
                       "downstream then believes"
                       % ("" if not found["claims"] else
                          " (it does carry %s)"
                          % ", ".join(sorted(found["claims"])))})
            continue

        entry = {"path": path, "agent": agent, "class": klass,
                 "claims": found["claims"], "missing": found["missing"]}
        identified.append(entry)
        by_agent.setdefault(agent, []).append(path)

    contested = [{"agent": agent, "paths": sorted(paths),
                  "why": "%d files claim %s. Which is real is a question "
                         "about the repository, not a tie for this agent "
                         "to break - picking one would make the others "
                         "invisible." % (len(paths), agent)}
                 for agent, paths in sorted(by_agent.items())
                 if len(paths) > 1]

    return {
        "identified": identified, "unidentified": unidentified,
        "declared_none": declared_none,
        "contested": contested, "unreadable": unreadable,
        "by_class": dict((k, sorted(v)) for k, v in by_class.items()),
        "agents": sorted(by_agent),
        "why": "%d file(s): %d claim an agent (%d distinct), %d declare "
               "`none` on purpose, %d carry no header, %d could not be "
               "read. %d agent id(s) claimed by more than one file."
               % (len(files), len(identified), len(by_agent),
                  len(declared_none), len(unidentified), len(unreadable),
                  len(contested)),
        "unjudged": [
            "NOTHING WAS STORED. D-40: what can be read is read. A registry "
            "that remembered what it saw would be wrong from the first file "
            "somebody adds without telling it, and wrong in the direction "
            "nobody checks.",
            "AN IDENTITY HERE IS A CLAIM, NOT A GRANT. The header is text "
            "the file writes about itself, which Golden Rule 19 makes data "
            "rather than instruction - this reports it and grants nothing "
            "on the strength of it.",
            "a contested id is REPORTED, not resolved, and an agent id no "
            "file claims is not visible here at all: that needs the "
            "register (HERON-AHR-REG-008), and 'no file claims it' is not "
            "the same answer as 'nobody built it'.",
            "the class of each path is HERON-WSP-PTH-007's, and on a source "
            "checkout rather than an installed workspace it is answering a "
            "different question - see that agent for why.",
        ],
    }


def main(argv):
    print("FILE REGISTRY   what exists, and what each file CLAIMS to be")
    print("=" * 72)

    # Read from this repository, which is the only honest demo: a made-up
    # file list would show the agent agreeing with a fixture.
    here = [os.path.join("brain", name) for name in
            sorted(os.listdir(os.path.join(ROOT, "brain")))
            if name.endswith(".py")][:12]
    here.append("brain/retrieval-history.md")   # a real non-module file

    def reader(path):
        full = os.path.join(ROOT, path)
        with open(full, encoding="utf-8", errors="replace") as fh:
            return fh.read(4000)

    answer = survey(here, read=reader)
    print("  %s" % answer["why"])

    print()
    print("  IDENTIFIED (the first few):")
    for entry in answer["identified"][:5]:
        print("    %-30s %s" % (os.path.basename(entry["path"]),
                                entry["agent"]))

    for label, key in (("DECLARES `none` ON PURPOSE:", "declared_none"),
                       ("CARRIES NO HEADER AT ALL:", "unidentified")):
        if not answer[key]:
            continue
        print()
        print("  %s" % label)
        for entry in answer[key]:
            print("    %-30s %s" % (os.path.basename(entry["path"]),
                                    entry["why"][:56]))

    print()
    print("  And with no reader at all, nothing is invented:")
    answer = survey(["brain/heron_flags.py"])
    print("    identified=%d  unidentified=%d"
          % (len(answer["identified"]), len(answer["unidentified"])))
    print("    %s" % answer["unidentified"][0]["why"])

    print()
    print("  Two files claiming one id is REPORTED, never resolved:")
    answer = survey(["a/one.py", "b/two.py"],
                    read=lambda p: "# Heron-Agent:  HERON-OPS-FLG-009\n")
    print("    %s" % answer["contested"][0]["why"][:96])

    print()
    print("  A registry that REMEMBERS what it saw is wrong from the first")
    print("  file somebody adds without telling it - and wrong in the")
    print("  direction nobody checks. D-40: what can be read is read.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
