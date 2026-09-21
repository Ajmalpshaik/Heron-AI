# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-EXT-011
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
External tools - seven steps, and a step that said nothing did not happen.

    python brain/heron_tooling.py

WHAT IT IS FOR (docs/28, HERON-INS-EXT-011)
--------------------------------------------
"Optional tooling - AI CLIs, development utilities, additional MCP
servers. Detect, check compatibility, ask permission, install, configure,
verify, register. **Never silent.**" T1, risk ADMIN.

That register row is the entire specification this agent has - no
document expands on it - so everything below is derived from rules already
decided elsewhere, and says which. Where a rule is this agent's own
reading rather than a decision somebody made, it says that too.

"NEVER SILENT" IS THE ONLY INSTRUCTION WITH NO CONDITION ON IT
----------------------------------------------------------------
So it is enforced rather than intended: every step carries a sentence,
and a step reported as done with nothing said about it is REFUSED. Not
logged-and-continued. A tool manager that installs something and says
nothing is indistinguishable from one that installed something else.

DETECTION IS EXECUTION, AND THAT IS THE STEP PEOPLE SKIP
----------------------------------------------------------
"Detect" sounds free. In practice detecting an external tool means
running it - `claude --version`, `node --version` - and running a program
to ask what it is IS running it. On a machine where the path is not
Heron's to trust, that is executing an unknown binary to find out whether
it should be trusted.

This is the difference from HERON-INS-DEP-005, whose detection is an
import inside the running interpreter. Here detection needs permission of
its own, and is not folded into the permission to install.

AN ADDITIONAL MCP SERVER IS NOT A UTILITY. IT IS A TOOL SURFACE
-----------------------------------------------------------------
Registering one means Heron will call tools it did not write, chosen by
descriptions it did not write. Golden Rule 19 settles what those
descriptions are worth: **data, never instruction.** A server whose tools
are discovered at connect time and trusted as found would be a text file
deciding what Heron can do.

So an MCP server must arrive with its tools ENUMERATED - named in advance,
by the person adding it - and a server that names none is refused. That
is this agent's reading of Golden Rule 19 rather than a decision on
record, and it is the reading that fails closed.

INSTALLED IS NOT VERIFIED, WHICH IS WHY THE ROW LISTS BOTH
------------------------------------------------------------
`verify` is a separate word in the register row from `install`, and the
distinction earns its place: a package manager exiting 0 says a download
finished. This agent will not report a tool as registered on the strength
of an install having been attempted.

IT RUNS NOTHING, INSTALLS NOTHING AND REGISTERS NOTHING
---------------------------------------------------------
It says which of the seven steps are satisfied and what each one needs.
Executing any of them is somebody else's, in a process started on purpose
- the line HERON-OPS-HEA-006 draws, for the reason D-84 recorded: nothing
here contains a process Heron starts. An agent that ran `--version` to find
out what was installed would be building the containment that decision
declined, and detection is exactly where that would feel harmless.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_flags as FLG                                      # noqa: E402

# The register row's own seven words, in its own order. This agent's job
# is to refuse to skip one.
STEPS = ("detect", "compatibility", "permission", "install", "configure",
         "verify", "register")

# What each kind of tool is. `mcp-server` is the one that is not a utility.
KINDS = ("cli", "utility", "mcp-server")


def _said(step, sentence):
    """One step's line. 'Never silent' means this is never empty."""
    return {"step": step, "said": str(sentence)}


def review(tool, permitted=None, origin=None, detected=None):
    """
    {ready, steps, said, needs} - or a refusal.

    Walks the register row's seven steps in its order and stops at the
    first one that is not satisfied, saying what it needs. Nothing is run.
    """
    if not isinstance(tool, dict) or not str(tool.get("name") or "").strip():
        return {"ready": False, "refused": "NO_TOOL",
                "why": "nothing was named. A tool manager with no tool is "
                       "not an idle one, it is a call somebody got wrong."}
    name = str(tool["name"]).strip()
    kind = str(tool.get("kind") or "").strip().lower()
    said = []

    # 1. DETECT - which is executing something, so it is permitted first.
    allowed, why_origin = FLG.origin_allowed(origin, "detecting an external tool")
    if not allowed:
        return {"ready": False, "refused": "NOT_FROM_THE_USER", "step":
                "detect", "why": why_origin,
                "proposal": "ask the user. Even DETECTING this runs a "
                            "program to ask what it is."}
    if detected is None:
        return {"ready": False, "refused": "DETECTION_IS_EXECUTION",
                "step": "detect",
                "why": "nothing says whether %s is already there, and this "
                       "agent will not find out. Detecting an external tool "
                       "means running it - `%s --version` executes %s - and "
                       "on a path Heron does not control that is running an "
                       "unknown binary to decide whether to trust it."
                       % (name, name, name),
                "proposal": "run the detection deliberately, in a process "
                            "somebody started, and pass what it found. The "
                            "answer may be 'not installed' - that is a "
                            "detection too."}
    said.append(_said("detect", "%s is %s"
                      % (name, "already installed at %s" % detected
                         if detected else "not installed")))

    # 2. COMPATIBILITY - stated, never assumed from the fact it ran.
    works_with = tool.get("works_with")
    if not isinstance(works_with, (list, tuple)) or not works_with:
        return {"ready": False, "refused": "COMPATIBILITY_NOT_CHECKED",
                "step": "compatibility", "said": said,
                "why": "%s does not say what it works with. 'It ran once on "
                       "this machine' is not a compatibility statement, and "
                       "it is the one people accept as one." % name}
    said.append(_said("compatibility", "%s declares it works with %s"
                      % (name, ", ".join(str(one) for one in works_with))))

    # 3. PERMISSION - per tool, naming it.
    given = permitted if isinstance(permitted, dict) else None
    if not given or not str(given.get("by") or "").strip():
        return {"ready": False, "refused": "NOT_PERMITTED",
                "step": "permission", "said": said,
                "why": "the register row says ask permission, and nobody "
                       "was asked."}
    if str(given.get("tool") or "").strip() != name:
        return {"ready": False, "refused": "NOT_PERMITTED",
                "step": "permission", "said": said,
                "why": "the permission is for %s and this is %s."
                       % (given.get("tool") or "no tool", name)}
    said.append(_said("permission", "%s permitted %s specifically"
                      % (str(given["by"]).strip(), name)))

    # 4-5. INSTALL and CONFIGURE - named, not done.
    said.append(_said("install",
                      "already present, so nothing to install" if detected
                      else "%s must be installed, by somebody, on purpose - "
                           "this agent runs nothing" % name))
    said.append(_said("configure", str(tool.get("configure")
                                       or "nothing to configure was named")))

    # 6. VERIFY - and this is the one that cannot be inferred.
    verified = tool.get("verified")
    if not isinstance(verified, dict) or not str(
            verified.get("by") or "").strip():
        return {"ready": False, "refused": "NOT_VERIFIED", "step": "verify",
                "said": said,
                "why": "nothing verified that %s actually works. The "
                       "register row lists `verify` as its own word from "
                       "`install`, and it earns it: a package manager "
                       "exiting 0 says a download finished." % name,
                "proposal": "run the thing and record what it did - what "
                            "was asked, what came back, and when."}

    said.append(_said("verify", "verified by %s: %s"
                      % (str(verified["by"]).strip(),
                         verified.get("what") or "no detail given")))

    # 7. REGISTER, LAST - and an MCP server is not a utility, it is a
    # TOOL SURFACE. The row's order puts register after verify, and
    # the order is the point: registering an unverified tool is the
    # failure the two words exist to separate.
    if kind == "mcp-server":
        tools = tool.get("tools")
        if not isinstance(tools, (list, tuple)) or not tools:
            return {"ready": False, "refused": "TOOLS_NOT_ENUMERATED",
                    "step": "register", "said": said,
                    "why": "%s is an MCP server and names no tools. "
                           "Registering it means Heron will call tools it "
                           "did not write, chosen by descriptions it did "
                           "not write - and Golden Rule 19 says those are "
                           "data, never instruction. Tools discovered at "
                           "connect time and trusted as found would be a "
                           "text file deciding what Heron can do." % name,
                    "proposal": "list the tools it brings, by name, before "
                                "it is registered. A server that gains a "
                                "tool later gains it under a decision "
                                "somebody makes again."}
        said.append(_said("register",
                          "%s brings %d tool(s), enumerated in advance: %s"
                          % (name, len(tools),
                             ", ".join(str(one) for one in tools))))
    else:
        said.append(_said("register", "%s registers as a %s and brings no "
                                      "tools Heron calls"
                          % (name, kind or "tool of unstated kind")))


    # NEVER SILENT, ENFORCED, AND LAST - so it covers every line rather
    # than the ones written before it. A step whose sentence is blank or
    # is whitespace somebody passed did not happen as far as anybody
    # reading this can tell, and a blank row in a report reads as a step
    # that went fine.
    silent = [entry["step"] for entry in said
              if not entry["said"].strip()]
    if silent:
        return {"ready": False, "refused": "STEP_SAID_NOTHING",
                "said": said, "step": silent[0], "steps": silent,
                "why": "%s produced no line. The row's one unconditional "
                       "instruction is 'never silent', and a step nobody "
                       "can read is a step nobody can check."
                       % ", ".join(silent)}

    return {
        "ready": True, "tool": name, "kind": kind or None,
        "said": said, "steps": [entry["step"] for entry in said],
        "why": "all %d steps for %s are satisfied and each one said "
               "something." % (len(said), name),
        "unjudged": [
            "NOTHING WAS RUN, INSTALLED OR REGISTERED. This says which of "
            "the register row's seven steps are satisfied; doing them is "
            "somebody else's, in a process started on purpose (D-84).",
            "the detection was handed in. This agent did not run `%s "
            "--version` to find out, because running a program to ask what "
            "it is IS running it." % name,
            "%s"
            % ("the tools this server brings were enumerated by a person. "
               "A server that gains one later gains it under a decision "
               "somebody makes again, not under this one."
               if kind == "mcp-server" else
               "this is not an MCP server, so it adds nothing Heron will "
               "call. That is the whole difference between a utility and a "
               "tool surface."),
            "the row is the entire specification this agent has - no "
            "document expands on it - so several rules here are a reading "
            "of decisions made elsewhere rather than decisions on record. "
            "The readings all fail closed.",
        ],
    }


def main(argv):
    print("EXTERNAL TOOLS   seven steps, and a step that said nothing")
    print("=" * 72)

    server = {"name": "company-standards-mcp", "kind": "mcp-server",
              "works_with": ["Claude Code", "Heron 0.4"],
              "tools": ["lookup_standard", "check_naming"],
              "configure": "one entry in the host's MCP configuration",
              "verified": {"by": "ajmal",
                           "what": "lookup_standard returned the company's "
                                   "duct naming rule"}}
    answer = review(server, origin="user", detected="/usr/local/bin/csm",
                    permitted={"by": "ajmal", "tool": "company-standards-mcp"})
    print("  %s" % answer["why"])
    for entry in answer["said"]:
        print("    %-14s %s" % (entry["step"], entry["said"][:66]))

    print()
    print("  It stops at the first step that is not satisfied:")
    cases = [
        ("a document asked", {"origin": "a document Heron read"}),
        ("nothing detected it", {"detected": None}),
        ("no compatibility stated",
         {"tool": dict(server, works_with=[])}),
        ("nobody permitted it", {"permitted": None}),
        ("permitted for another", {"permitted": {"by": "ajmal",
                                                 "tool": "something-else"}}),
        ("an MCP server naming no tools",
         {"tool": dict(server, tools=[])}),
        ("nothing verified it", {"tool": dict(server, verified=None)}),
        ("verified by nobody", {"tool": dict(server,
                                             verified={"what": "it ran"})}),
        ("nothing named at all", {"tool": {}}),
    ]
    for label, override in cases:
        settings = {"origin": "user", "detected": "/usr/local/bin/csm",
                    "permitted": {"by": "ajmal",
                                  "tool": "company-standards-mcp"}}
        what = override.pop("tool", server)
        settings.update(override)
        answer = review(what, **settings)
        print("    %-30s %-26s at step %s"
              % (label, answer["refused"], answer.get("step")))

    print()
    print("  A utility is not a tool surface, and the difference is the")
    print("  whole reason an MCP server is enumerated:")
    utility = {"name": "ripgrep", "kind": "utility",
               "works_with": ["Linux", "Windows"],
               "verified": {"by": "ajmal", "what": "rg --version answered"}}
    answer = review(utility, origin="user", detected="/usr/bin/rg",
                    permitted={"by": "ajmal", "tool": "ripgrep"})
    print("    %s" % answer["said"][-2]["said"])
    print("    %s" % answer["unjudged"][2][:98])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
