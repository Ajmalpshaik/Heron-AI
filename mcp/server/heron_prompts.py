#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-SRV-001
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
The slash commands a modeller starts a common job with: MCP prompts (D-210
item 3, docs/04 §8.13).

A host lists every prompt a server serves as a command. Claude Code lists one
as `/heron:check-model (MCP)` for today's user-scope `heron` server, and runs
it from `/mcp__heron__check-model` too (code.claude.com/docs/en/mcp, "Use MCP
prompts as commands", read 2026-11-28). Under the plugin the server is
`plugin:heron:heron`, and the command's exact name there is a PC check, not a
claim. Running one puts its text in the chat as the modeller's own turn, and
the chat goes on from there.

A PROMPT ONLY NAMES A JOB. Its text is the sentence a modeller would type
anyway, in Revit words. It decides nothing and classifies nothing (D-01), and
it orders no tool call (D-202): the host reads it as it reads any turn, with
the server's instructions and the tools' own descriptions, and decides. Every
word is for every modeller who installs Heron - nothing from one office, one
country or one project.

A PROMPT NAMES ONLY A TOOL THIS SERVER SERVES. Off Windows the server serves
its library alone (D-132), so a prompt that named a revit_ tool would point a
chat at a tool it was never offered. `register` leaves out a prompt whose words
name a tool the server does not serve - never listed and then refused, the
tools' own rule - and says so on stderr. tests/test_mcp_stdio.py holds it
against a real server, and holds that the check finds such a prompt.

NO SIZE BUDGET MOVES. A host lists prompts on their own (prompts/list), not
in the server's instructions or in any tool's description, so neither the
4,096 characters a host gives of the instructions nor ALWAYS_LOADED's budget
is touched (tests/test_tool_sizes.py).

ONE ARGUMENT, AND ONE WORD. Claude Code splits what follows a command on
spaces, one token to an argument (the same page), so `/heron:build-family
pipe tag` would give `family` the word "pipe". The one argument here asks for
one word, and says so. Left out, the text asks the modeller instead.

No SDK is imported until `register` runs, so a suite reads this table without
one. No file of the server is imported here; the entry file imports this one,
below its last tool, and calls `register` once.
"""

import collections
import inspect
import re
import sys

# One prompt. `argument` is the name of its one optional argument, or None;
# `text_with` is its text when that argument is given, with the argument's
# place written `{name}`.
Job = collections.namedtuple(
    "Job", "name title description argument argument_says text text_with")

PROMPTS = (
    Job(name="check-model",
        title="Check my model",
        description="Check the model open now for what will cost you time - empty "
                    "sheets, views with no template, imports, MEP connected to "
                    "nothing. It only reads.",
        argument=None, argument_says=None,
        text="Check the Revit model I have open now and tell me what will cost me "
             "time: sheets with nothing on them, views no template controls, CAD "
             "imported where a link belongs, MEP elements connected to nothing, and "
             "tags or notes that disagree with the model. Only read it - change "
             "nothing - and say which reply each number came from.",
        text_with=None),
    Job(name="build-family",
        title="Build a family",
        description="Build a new Revit family a step at a time. Optional: what kind, "
                    "in one word - door, tag, fitting.",
        argument="family",
        argument_says="What kind of family, in one word - for example door, tag or "
                      "fitting. Leave it out to be asked.",
        text="I want to build a new Revit family. Heron has a method for building "
             "families (heron_method holds it): take me through it a step at a "
             "time, and ask me what the family is, and for any size or name you need.",
        text_with="I want to build a new Revit {family} family. Heron has a method "
                  "for building families (heron_method holds it): take me through it "
                  "a step at a time, and ask me for any size or name you need."),
    Job(name="revit-connected",
        title="Is Revit connected?",
        description="Whether Heron can reach Revit on this PC, and which model it "
                    "would work on.",
        argument=None, argument_says=None,
        text="Is Revit connected to Heron right now? Tell me which Revit and which "
             "model you would work on. Change nothing.",
        text_with=None),
    Job(name="what-heron-can-do",
        title="What can Heron do?",
        description="What Heron can do for you in Revit, and how much of it is "
                    "proven on a real model.",
        argument=None, argument_says=None,
        text="What can Heron do for me in Revit? Tell me the jobs it knows by name, "
             "and how much of it is proven on a real model.",
        text_with=None),
)

# A word shaped like a Heron tool's name. Every tool is heron_ or revit_
# (heron_tools.TOOLS), so this finds every tool a prompt's words could name.
TOOL_SHAPED = re.compile(r"\b(?:heron|revit)_[a-z0-9_]+\b")


def job(name):
    """The prompt called `name`, or None."""
    return next((each for each in PROMPTS if each.name == name), None)


def render(prompt, value=None):
    """
    The words `prompt` (a Job) puts in the chat: its text, or with its
    argument given and not blank, `text_with` with the value in its place.
    The value is the modeller's own word, put in as they typed it, its spaces
    closed up.
    """
    value = " ".join(str(value or "").split())
    if prompt.argument is None or not value:
        return prompt.text
    return prompt.text_with.replace("{%s}" % prompt.argument, value)


def named_tools(prompt):
    """Every tool-shaped word in what `prompt` shows or says - its title,
    description, argument and both texts - sorted, each once."""
    words = (prompt.title, prompt.description, prompt.argument_says,
             prompt.text, prompt.text_with)
    return sorted(set(TOOL_SHAPED.findall(" ".join(w for w in words if w))))


def unserved(prompt, served):
    """The tools `prompt` names that are not in `served` - empty when every
    one it names is served."""
    served = set(served)
    return [name for name in named_tools(prompt) if name not in served]


def _handler(prompt):
    """The function the SDK calls for `prompt`. A prompt with an argument
    takes it as optional text, described for the host in the prompt's own
    words; the SDK reads both off the function's signature."""
    if prompt.argument is None:
        def say():
            return render(prompt)
        return say
    if prompt.argument != "family":
        # The signature below names its argument, so a new argument name
        # needs its own; failing here is louder than serving the wrong one.
        raise ValueError("heron_prompts has no handler for an argument called %r"
                         % prompt.argument)
    from typing import Annotated
    from pydantic import Field                     # the SDK's own dependency

    def say(family: Annotated[str, Field(description=prompt.argument_says)] = ""):
        return render(prompt, family)
    return say


def register(server, served, jobs=PROMPTS, said=None):
    """
    Register every prompt in `jobs` on `server` whose words name only tools in
    `served`, and return their names. A prompt that names another is left out
    and said on `said` (stderr by default), never registered half-checked.
    The title is passed only to an SDK whose prompt() takes one.
    """
    said = said or sys.stderr.write
    takes_title = "title" in inspect.signature(server.prompt).parameters
    kept = []
    for prompt in jobs:
        missing = unserved(prompt, served)
        if missing:
            said("Heron left out the prompt %s: its words name %s, which this "
                 "server does not serve.\n" % (prompt.name, ", ".join(missing)))
            continue
        options = {"name": prompt.name, "description": prompt.description}
        if takes_title:
            options["title"] = prompt.title
        server.prompt(**options)(_handler(prompt))
        kept.append(prompt.name)
    return kept


if __name__ == "__main__":
    # The list, as a person reads it: `python mcp/server/heron_prompts.py`.
    for each in PROMPTS:
        print("%-20s %s - %s" % (each.name, each.title, each.description))
        if each.argument:
            print("%-20s optional `%s`: %s" % ("", each.argument, each.argument_says))
