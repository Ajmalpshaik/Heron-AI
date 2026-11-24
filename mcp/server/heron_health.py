#!/usr/bin/env python3
# Heron-Agent:  HERON-OPS-HLT-004
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Platform Health. One state per component, and one state overall.

The registry asks for HEALTHY / WARNING / DEGRADED / FAILED across every
component, and the four are only worth having if they mean different things:

    HEALTHY   working as intended. Nothing to say.
    WARNING   working, but something deserves to be noticed.
    DEGRADED  partly working. Something that should be usable is not.
    FAILED    not working. Heron itself is broken, not merely idle.

THE DISTINCTION THAT MATTERS MOST HERE: no Revit connected is **not a
failure**. It is Heron's designed resting state - a Revit that was never
connected is invisible on purpose, because nothing should reach a model the
user did not offer up. Reporting that as FAILED would teach the user to ignore
the word, and then it would be ignored on the day it means something.

So FAILED is reserved for Heron being unable to do its job at all, and "there
is nothing to talk to yet" is a WARNING that says which button to press.

WRITING BEING ENABLED IS A WARNING, and it is meant to be a loud one. A
config that permits a change to somebody's model is the single most
consequential state this file can report, and the whole point of a health
check is to say so before something goes wrong rather than afterwards.

THAT PARAGRAPH USED TO SAY THE WRITE PATH "HAS NEVER BEEN COMPILED OR RUN",
and both halves of that are now false - it compiles on all eight Revit
releases, and NEEDS-CHECKING records it moving three ducts under a single
undo entry. This is the THIRD place that sentence was written down;
HeronPermissions was corrected on 2026-09-20 and RevitWrite's banner on
2026-09-21 (FRAGMENT-ISSUES section 5b rows 9 and 26). What was still true
after that was narrower: nobody had put a tape measure on the result - D3 in
NEEDS-CHECKING.md, "move them, then MEASURE one". On 2026-10-17 somebody did,
in Revit 2024: three free ducts +200.0000 mm, a pinned one beside them not
moved, one undo. So the message below says what was measured and how little
of the ground it covers. Whether writing should be on at all is still D-19's
question and the owner's. A warning a reader can disprove in a minute
discredits the half that still matters.
"""

import ntpath

HEALTHY = "HEALTHY"
WARNING = "WARNING"
DEGRADED = "DEGRADED"
FAILED = "FAILED"

# Worst wins, and the order is the definition of "worst".
SEVERITY = {HEALTHY: 0, WARNING: 1, DEGRADED: 2, FAILED: 3}


class Component(object):
    """One named part of Heron, its state, and why."""

    def __init__(self, name, state, detail):
        self.name = name
        self.state = state
        self.detail = detail

    def __repr__(self):
        return "<%s %s>" % (self.name, self.state)


class Health(object):
    """Every component, and the rollup."""

    def __init__(self, components):
        self.components = components

    @property
    def state(self):
        """The worst state among the components. HEALTHY if there are none."""
        if not self.components:
            return HEALTHY
        return max((c.state for c in self.components), key=lambda s: SEVERITY[s])

    def worst(self):
        """The components actually responsible for the rollup."""
        return [c for c in self.components if c.state == self.state]

    def describe(self):
        """As a person reads it: the rollup, then only what is not healthy."""
        lines = ["Heron: %s" % self.state]

        notable = [c for c in self.components if c.state != HEALTHY]
        if not notable:
            lines.append("  Every component healthy.")
            return "\n".join(lines)

        for c in sorted(notable, key=lambda c: -SEVERITY[c.state]):
            lines.append("  %-9s %-12s %s" % (c.state, c.name, c.detail))
        return "\n".join(lines)


def assess(live=None, starting=None, mismatched=None, config_problems=None,
           writing=False, protocol_version=None, versions_agree=True,
           discovery_readable=True, writing_said_by=None):
    """
    Build the picture from facts already gathered elsewhere.

    Everything arrives as an argument rather than being discovered here, for
    one reason: it makes this testable without Revit, without a bridge and
    without a config file - which is the only way it was ever going to be
    tested at all on the machine it was written on.

    `writing_said_by` names the Revits whose word `writing` is (Q-70) - the
    words writing_said_by() below gives. None leaves the write gate's words
    as D-166 has them: "a connected Revit", or no Revit's word at all.
    """
    live = live or []
    starting = starting or []
    mismatched = mismatched or []
    config_problems = config_problems or []

    components = []

    # --- Heron's own settings ---------------------------------------------
    if config_problems:
        components.append(Component(
            "config", WARNING,
            "%d problem(s): %s" % (len(config_problems), config_problems[0])))
    else:
        components.append(Component("config", HEALTHY, "readable, values sane"))

    # --- where sessions announce themselves -------------------------------
    if not discovery_readable:
        # Heron cannot find a Revit even if one is connected. That is Heron
        # broken, not Heron idle, and it is the one thing here that earns
        # FAILED.
        components.append(Component(
            "discovery", FAILED,
            "the folder where Revit sessions announce themselves cannot be read, "
            "so Heron cannot find any Revit at all"))
    else:
        components.append(Component("discovery", HEALTHY, "readable"))

    # --- the bridge -------------------------------------------------------
    if live:
        if starting or mismatched:
            components.append(Component(
                "bridge", DEGRADED,
                "%d session(s) answering, %d still starting, %d on another protocol"
                % (len(live), len(starting), len(mismatched))))
        else:
            components.append(Component(
                "bridge", HEALTHY,
                "%d session(s) answering" % len(live)))
    elif mismatched:
        components.append(Component(
            "bridge", DEGRADED,
            "%d session(s) connected but speaking a different protocol - restart that "
            "Revit to finish updating" % len(mismatched)))
    elif starting:
        components.append(Component(
            "bridge", WARNING,
            "%d Revit(s) running but not answering yet - probably still starting up"
            % len(starting)))
    else:
        # The designed resting state. Not a fault.
        components.append(Component(
            "bridge", WARNING,
            "no Revit connected. Open Revit and press Heron > AI Bridge > Heron - a Revit that "
            "was never connected is invisible to Heron by design"))

    # --- the two halves understanding each other --------------------------
    if not versions_agree:
        components.append(Component(
            "versions", DEGRADED,
            "the add-in and this client are different versions - half-understanding "
            "each other is how a wrong answer looks exactly like a right one"))
    elif live:
        components.append(Component(
            "versions", HEALTHY,
            "add-in and client agree%s"
            % ("" if protocol_version is None else " on protocol %s" % protocol_version)))

    # --- can Heron change the model right now? ----------------------------
    # SINCE D-166 EACH REVIT HOLDS ITS OWN, so `writing` is what the Revits
    # that answered said - True if Changes is on in any of them, False if it
    # is off in all - and None when no Revit said, which is never "off": off
    # is the default, and a default is not an answer. When the caller names
    # those Revits (Q-70, writing_said_by below), the line names them too.
    if writing is True:
        components.append(Component(
            "write gate", WARNING,
            "Changes is ON %s, so Heron is permitted to change its "
            "model. Its move was measured once, in Revit 2024 on free ducts - D3 in "
            "NEEDS-CHECKING.md - and nothing else it can change has been measured that "
            "way; whether writing should be on is still the owner's decision (D-19) - "
            "turn it off in Heron's Settings unless you are deliberately testing it"
            % (writing_said_by or "in a connected Revit")))
    elif writing is False:
        components.append(Component(
            "write gate", HEALTHY,
            "closed%s - Heron cannot change the model"
            % (" (%s)" % writing_said_by if writing_said_by else "")))
    else:
        components.append(Component(
            "write gate", HEALTHY,
            "held in each Revit, off whenever it starts - turned on only in Heron's "
            "Settings in Revit (D-166); no connected Revit said which way it is set"))

    return Health(components)


# WHETHER A REVIT WILL ASK KEEP OR DISCARD, IN ONE LINE A MODELLER CAN READ.
#
# The owner's Ask to Keep switch (D-126, D-138, D-150) decides whether a change
# made through revit_change is shown in a Keep or Discard box or kept at once,
# and it is read inside the add-in. Until 2026-11-12 the add-in told nobody, so
# a chat could not say beforehand which it would be. The add-in's `info` now
# carries what the switch says (heron_bridge_client.ask_to_keep_state), and
# revit_health prints one of these four under each connected Revit.
#
# NOT A COMPONENT, AND NOT A STATE. Neither setting is a fault and neither is
# Heron's to recommend: it is the owner's switch, said as it stands. And it is
# one line per REVIT, where a component is one per Heron - two Revits on one PC
# can run add-ins of different ages, and one of them may not say.
#
# ON AND OFF ARE SAID OF revit_change AND NAME WHAT IS DIFFERENT. The first
# wording said "after a change from the chat ... keeps nothing until the
# modeller chooses Keep" and "Undo takes it back in one step", and a review the
# same day (2026-11-12) found each untrue of changes a chat really makes:
#   - revit_apply_move never reads the switch. It keeps the preview it showed,
#     on or off (RevitWrite.Apply; D-138 item 9).
#   - a publish - an export, a save, a sync - has left the model before the box
#     opens, so Discard cannot take it back, and with the switch off no Undo
#     can either (D-126; RevitFragment's `sendsOut`).
#   - a delete is asked in a Delete box of its own, whatever the switch says,
#     and a delete confirmed there is not asked Keep or Discard as well (D-142).
#   - a change Revit counted nothing in is kept without the box (D-126 item 6),
#     and so is the rest of a job the modeller chose in the box to keep (D-138).
#   - a DRY RUN - revit_change with dry_run (D-155) - is rolled back whichever
#     way the switch stands: never kept, so never asked about. Both lines were
#     written the day before it existed and named no such call, so "is kept at
#     once" was said of one that keeps nothing (added 2026-11-12, in review).
# The line says the switch alone. It does not say a change WILL be kept:
# whether the chat may change the model at all is the Changes switch.
#
# TWO LINES FOR AN ANSWER NOBODY GAVE, and neither borrows the default. "Off"
# is what the switch is when nobody has set it, which is a different fact from
# "this Revit said off", and a chat told the wrong one tells a modeller the box
# will not come while Revit is about to show it.
#   - the add-in answered and named no switch: it is older than the field, and
#     the line says so (the client's ASK_TO_KEEP_NOT_SENT, the same word here).
#   - no usable answer came back: the line says only that. It used to blame an
#     older add-in for that too, which is a guess about a Revit that was busy.
ASK_TO_KEEP_NOT_SENT = "not sent"

ASK_TO_KEEP_ON = (
    "Ask to Keep is ON in this Revit right now: a change made through revit_change "
    "is shown in Revit's Keep or Discard box and kept only on Keep. Different for: "
    "a job the modeller chose in that box to keep the rest of, and a change Revit "
    "counted nothing in (both kept unasked); a delete while Ask to Delete is on (asked in its own Delete box "
    "instead); an export, save or sync (already done when the box opens - Discard "
    "cannot take it back); a dry run (rolled back, so never kept and never asked "
    "about); and revit_apply_move (keeps the preview it showed, with no box).")
ASK_TO_KEEP_OFF = (
    "Ask to Keep is off in this Revit right now: a change made through revit_change "
    "is kept at once, with no Keep or Discard box, as one entry in Revit's Undo "
    "list. Different for: a delete while Ask to Delete is on (still asked first, in its own Delete box); an "
    "export, save or sync (Undo cannot take back what has left the model); and a "
    "dry run (rolled back, so never kept and never asked about).")
ASK_TO_KEEP_OLDER = (
    "Ask to Keep: this Revit's Heron does not say which way the switch is set - its "
    "Heron add-in is older than this chat's and does not report it. Look in Revit, "
    "under Heron > AI Bridge > Settings.")
ASK_TO_KEEP_NOT_READ = (
    "Ask to Keep: Heron could not read which way the switch is set from this Revit "
    "just now - it gave no answer Heron could use. Ask again, or look in Revit, under "
    "Heron > AI Bridge > Settings.")


# What switches_line says for each answer heron_bridge_client.switches_state
# can give. The word for an older add-in is the client's, held to one spelling.
SWITCHES_NOT_SENT = "not sent"
SWITCHES_OLDER = (
    "Changes, Admin and Publish: this Revit's Heron add-in is older than this chat's "
    "and does not report them - it still reads them from heron.config. Look in Revit, "
    "under Heron > AI Bridge > Settings, and update that add-in.")
SWITCHES_NOT_READ = (
    "Changes, Admin and Publish: Heron could not read them from this Revit just now. "
    "Ask again, or look in Revit, under Heron > AI Bridge > Settings.")


def switches_line(state):
    """
    The line for what one Revit said of Changes, Admin and Publish (D-166) -
    what heron_bridge_client.switches_state returned. Only the modeller turns
    one on, in Heron's Settings in Revit, and the line says so: the reader is
    often the AI, and it must learn where the switch is, not how to flip it.
    """
    if isinstance(state, dict) and all(state.get(k) is True or state.get(k) is False
                                       for k in ("changes", "admin", "publish")):
        said = ", ".join("%s %s" % (name, "ON" if state[key] else "off")
                         for name, key in (("Changes", "changes"), ("Admin", "admin"),
                                           ("Publish", "publish")))
        # WHERE IN THE WINDOW - row 5b-2019: told to turn Changes on, the
        # modeller could not find it, and the chat had to read the add-in's C#.
        line = ("%s in this Revit right now. Only the modeller turns one on, in Heron's "
                "Settings in Revit (Heron > AI Bridge > Settings) - one row each, Changes at "
                "the top, then Admin, then Publish; the button beside it reads OFF, and "
                "pressing it asks Turn on - and all three are off again whenever Revit "
                "starts." % said)
        locked = [name for name, key in (("Changes", "changes"), ("Admin", "admin"),
                                         ("Publish", "publish"))
                  if key in (state.get("locked") or [])]
        if locked:
            line += (" Held off on this PC by a Windows policy, so Settings cannot turn "
                     "them on - only the PC's administrator can lift it: %s." % ", ".join(locked))
        return line
    if isinstance(state, str) and state == SWITCHES_NOT_SENT:
        return SWITCHES_OLDER
    return SWITCHES_NOT_READ


def ask_to_keep_line(asks):
    """
    The line for what one Revit said of its Ask to Keep switch - what
    heron_bridge_client.ask_to_keep_state returned: True, False, the word for
    an add-in that sent nothing, or anything else - which is "could not read
    it", the line that claims least.

    `is`, not `==`: 1 and 0 equal True and False in Python, and a reply that
    carried a number carried no answer.
    """
    if asks is True:
        return ASK_TO_KEEP_ON
    if asks is False:
        return ASK_TO_KEEP_OFF
    if isinstance(asks, str) and asks == ASK_TO_KEEP_NOT_SENT:
        return ASK_TO_KEEP_OLDER
    return ASK_TO_KEEP_NOT_READ


# THE OWNER'S ASK TO DELETE SWITCH (D-197), as one Revit said it - one line
# under each connected Revit, beside Ask to Keep's. ON at every Revit start.
# Off, Revit shows neither its Delete box (D-142) nor a DRAFT's Keep or Discard
# (D-119), and the chat's yes in words, asked BEFORE the call, is the only one.
ASK_TO_DELETE_ON = (
    "Ask to Delete is ON in this Revit right now: a change that deletes is asked in "
    "Revit's own Delete box, and an unproven tool's one run in its Keep or Discard box.")
ASK_TO_DELETE_OFF = (
    "Ask to Delete is OFF in this Revit right now: Revit shows NO Delete box and no box "
    "for an unproven tool's one run. Ask the modeller in words, and wait for a yes, "
    "BEFORE any change that deletes (Constitution Article 7); say before trying an "
    "unproven tool that it will be kept with no box.")
ASK_TO_DELETE_OLDER = (
    "This Revit's Heron add-in is older than Ask to Delete, so it always shows its "
    "Delete box.")
ASK_TO_DELETE_NOT_READ = "Could not read whether this Revit shows its Delete box."


def ask_to_delete_line(asks):
    """
    The line for what one Revit said of Ask to Delete - what
    heron_bridge_client.ask_to_delete_state returned. `is`, not `==`, as
    ask_to_keep_line has it.
    """
    if asks is True:
        return ASK_TO_DELETE_ON
    if asks is False:
        return ASK_TO_DELETE_OFF
    if isinstance(asks, str) and asks == ASK_TO_KEEP_NOT_SENT:
        return ASK_TO_DELETE_OLDER
    return ASK_TO_DELETE_NOT_READ


# WHICH SETTINGS FILE A REVIT READS, AND WHETHER THIS CHAT'S COPY IS THE SAME -
# Q-70, decided 2026-11-15 on the owner's delegation, narrowed by D-166.
#
# On a PC whose Claude app is a packaged app, Windows keeps a private copy of
# the Heron folder for the app and everything it starts, so there are two
# heron.config files: the chat reads the copy, and a Revit the owner started
# reads the real one. The add-in's `info` names the file it reads
# (heron_bridge_client.settings_state), and revit_health prints the line below
# under a connected Revit only when this chat's own copy is not that file - by
# its name, by a file found on one side only, or by Ask to Keep.
#
# NOT CHANGES, ADMIN OR PUBLISH. Q-70 first stated those three here, on a line
# printed under every Revit as read from the file it named, and compared the
# chat's copy of them with Revit's. Since D-166 (2026-11-16) Revit holds them
# in memory, set only in Heron's Settings and off whenever it starts, and no
# file decides them - so switches_line above states them, from Revit's own
# word, no message about them names the file, and nothing here compares or
# mentions them. What is left in the file - Ask to Keep, the Companion, the
# waiting times - is what this compares, and with one file there is nothing
# to say.
#
# THE SAME NAME CAN BE TWO FILES. Measured on the owner's laptop on 2026-11-14
# (NEEDS-CHECKING NP6): from the chat's shell the real path, spelled in full,
# reads the app's copy. So a name that matches proves nothing on such a PC,
# and the second line is said too when the names match and Ask to Keep does
# not, or when one side found the file and the other did not.
#
# NOT A COMPONENT, for ask_to_keep_line's reason: one line per Revit, and no
# setting is a fault. The rollup's write gate takes Changes from the Revits
# that said it (switches_line's answer), and writing_said_by() names them.
SETTINGS_NOT_SENT = "not sent"

# THE WAY TO UPDATE IT IS SAID HERE. Seen 2026-11-19 (issue #1051 item D, row
# 5b-1966): Revit 2020's add-in was older than the chat, this was the one line
# it got, and nothing said what to do - the family method then named a tool
# that does not update an add-in. Every add-in from before the settings file
# was reported gets this line; one newer than that and older than a later
# field would not, and would need the same words on that field's line.
SETTINGS_OLDER = (
    "Settings file: this Revit's Heron does not report its settings file - its "
    "Heron add-in is older than this chat's, so whether this chat reads the same "
    "file cannot be said here. To update that add-in: close that Revit, then run "
    "HeronInstaller.exe - or, building Heron from source, tools/setup.ps1, which "
    "builds and deploys it (tools/deploy-addin.ps1 alone builds nothing).")
SETTINGS_NOT_READ = (
    "Settings file: Heron could not read which settings file this Revit reads just "
    "now - it gave no answer Heron could use. Ask again.")

def _on(value):
    return "on" if value else "off"


def same_name(one, other):
    """Whether two settings files are named alike, by Windows' rule - case and
    separators aside. NOT whether they are one file: see above."""
    if not one or not other:
        return False
    return ntpath.normcase(ntpath.normpath(one)) == ntpath.normcase(ntpath.normpath(other))


def settings_differ(said, asks, here_file, here_found, here):
    """
    How this chat's own copy differs from what one Revit said: None when the
    Revit said nothing usable, else {"named_alike", "found_differs",
    "switches"}, where `switches` lists (name, this Revit's, this chat's) for
    Ask to Keep when the two disagree - the one switch D-166 left in the file.
    Ask to Keep is compared only when `asks` is a real true or false.

    said        what heron_bridge_client.settings_state returned
    asks        what heron_bridge_client.ask_to_keep_state returned
    here_file   this chat's own settings file, or None when it has none
    here_found  whether that file is there
    here        heron_config.switches() of this chat's own copy - since D-166
                only Ask to Keep
    """
    if not isinstance(said, dict):
        return None
    here = here or {}
    named_alike = same_name(said.get("file"), here_file)
    disagree = []
    if isinstance(asks, bool) and asks != bool(here.get("ask_to_keep")):
        disagree.append(("Ask to Keep", asks, bool(here.get("ask_to_keep"))))
    return {"named_alike": named_alike,
            "found_differs": named_alike and bool(said.get("found")) != bool(here_found),
            "switches": disagree}


def differs(found):
    """Whether settings_differ's answer means this chat's copy is not Revit's."""
    return bool(found) and (not found["named_alike"] or found["found_differs"]
                            or bool(found["switches"]))


def _disagreement(switches):
    """"Ask to Keep (this Revit: on, this chat's: off)"."""
    parts = ["%s (this Revit: %s, this chat's: %s)" % (name, _on(revit), _on(chat))
             for name, revit, chat in switches]
    return parts[0] if len(parts) == 1 else ", ".join(parts[:-1]) + " and " + parts[-1]


def settings_lines(said, asks, here_file, here_found, here):
    """
    The line for one Revit when this chat's own copy is not the settings file
    that Revit said it reads - one line, in Revit's words - and none when it is;
    the arguments are settings_differ's. An older add-in and an answer that
    could not be read get one line each, and neither claims a file.

    NO LINE WHEN THE FILE IS ONE (D-166). Q-70 first printed a line under
    every Revit - its switches, read just now from the file it named. D-166
    took the switches out of the file, and switches_line says them; a file
    line beside it in the ordinary case would be a message beside the switches
    naming the file, which D-166's refusals and messages never do.
    """
    if isinstance(said, str) and said == SETTINGS_NOT_SENT:
        return [SETTINGS_OLDER]
    if not isinstance(said, dict):
        return [SETTINGS_NOT_READ]

    found = settings_differ(said, asks, here_file, here_found, here)
    if not differs(found):
        return []

    chat = here_file or "no settings file of its own"
    believe = ("This Revit reads its own file, so its settings are the ones that "
               "count.")
    if not found["named_alike"]:
        if found["switches"]:
            second = ("This chat's own settings file differs from this Revit's: this chat "
                      "reads %s, this Revit reads %s, and they disagree on %s. %s"
                      % (chat, said["file"], _disagreement(found["switches"]), believe))
        else:
            second = ("This chat reads a different settings file from this Revit's: this "
                      "chat reads %s, this Revit reads %s, so a setting changed in this "
                      "Revit is not seen in this chat's file. %s"
                      % (chat, said["file"], believe))
        return [second]

    two = ("so this chat's copy and this Revit's are two files - a Claude app that keeps "
           "its own copy of the Heron folder does this")
    if found["switches"]:
        second = ("This chat's own copy of the settings file differs from this Revit's, "
                  "though both are named %s: they disagree on %s, %s. %s"
                  % (said["file"], _disagreement(found["switches"]), two, believe))
    else:
        second = ("This chat's own copy of the settings file is not this Revit's, though "
                  "both are named %s: this Revit %s a file there and this chat %s, %s. "
                  "%s"
                  % (said["file"], "found" if said.get("found") else "found no",
                     "found one" if here_found else "found none", two, believe))
    return [second]


def writing_said_by(revits, writing):
    """
    Who said the write gate's Changes, for assess(writing_said_by=...).
    `revits` are the sessions whose word it is - those with Changes on when
    `writing`, else every one that said - each with .revit_version and .pid.
    """
    named = " and ".join("Revit %s, session %s" % (b.revit_version, b.pid) for b in revits)
    if writing:
        return "in %s, as %s has it" % (named, "that Revit" if len(revits) == 1 else "each")
    return "as %s, %s it" % (named, "has" if len(revits) == 1 else "each have")


# ONE RULE FOR THE WRITE GATE, FOR revit_health AND heron_diagnose - row
# 5b-1744. The diagnosis read its write gate from this chat's own copy of
# heron.config while revit_health took each Revit's word, so the two could
# give opposite words in one chat; since D-166 no file holds Changes at all -
# each Revit holds its own, in memory, set only in its Settings window. The
# rule was revit_health's own, written inline; both call it here now, and
# neither decides any of it itself.
def write_gate(live, held):
    """
    (writing, writing_said_by) for assess(), from what the connected Revits
    said of Changes, Admin and Publish - `held` is {pid: what
    heron_bridge_client.switches_state returned}:

        Changes ON in any Revit that said   (True, those Revits named)
        off in every Revit that said        (False, every one that said named)
        no Revit said - none connected, an
        older add-in, an answer not read    (None, None): not known

    NEVER FROM A FILE (D-166), and never "off" for a Revit that said nothing:
    off is the default, and a default is not an answer.
    """
    held = held or {}
    reported = [b for b in live or [] if isinstance(held.get(b.pid), dict)]
    if not reported:
        return None, None
    changes_on = [b for b in reported if held[b.pid].get("changes") is True]
    writing = bool(changes_on)
    return writing, writing_said_by(changes_on or reported, writing)
