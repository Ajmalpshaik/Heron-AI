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

    `writing_said_by` names who said `writing` when a Revit did (Q-70) - the
    words writing_said_by() below gives. None leaves the write gate's words
    exactly as they were: the caller's own settings file.
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
    said = " (%s)" % writing_said_by if writing_said_by else ""
    if writing:
        components.append(Component(
            "write gate", WARNING,
            "write.enabled is TRUE%s, so Heron is permitted to change the model. Its move "
            "was measured once, in Revit 2024 on free ducts - D3 in NEEDS-CHECKING.md - and "
            "nothing else it can change has been measured that way; whether writing should "
            "be on is still the owner's decision (D-19) - set it back to false unless you "
            "are deliberately testing it" % said))
    else:
        components.append(Component(
            "write gate", HEALTHY,
            "closed%s - Heron cannot change the model" % said))

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
    "counted nothing in (both kept unasked); a delete (asked in its own Delete box "
    "instead); an export, save or sync (already done when the box opens - Discard "
    "cannot take it back); a dry run (rolled back, so never kept and never asked "
    "about); and revit_apply_move (keeps the preview it showed, with no box).")
ASK_TO_KEEP_OFF = (
    "Ask to Keep is off in this Revit right now: a change made through revit_change "
    "is kept at once, with no Keep or Discard box, as one entry in Revit's Undo "
    "list. Different for: a delete (still asked first, in its own Delete box); an "
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


# WHICH SETTINGS FILE A REVIT DECIDES BY, ITS SWITCHES, AND WHETHER THIS CHAT'S
# COPY SAYS THE SAME - Q-70, decided 2026-11-15 on the owner's delegation.
#
# On a PC whose Claude app is a packaged app, Windows keeps a private copy of
# the Heron folder for the app and everything it starts, so there are two
# heron.config files: the chat reads the copy, and a Revit the owner started
# reads the real one. Revit decides every change by its own (HeronPermissions
# loads it on every call), so it is the one to believe. The add-in's `info`
# says the file it read and Changes, Admin and Publish as it holds them
# (heron_bridge_client.settings_state); revit_health prints the first line
# below under each connected Revit, and a second when this chat's own copy
# differs - by its name, or by a switch, Ask to Keep included.
#
# THE SAME NAME CAN BE TWO FILES. Measured on the owner's laptop on 2026-11-14
# (NEEDS-CHECKING NP6): from the chat's shell the real path, spelled in full,
# reads the app's copy. So a name that matches proves nothing on such a PC,
# and the second line is said too when the names match and a switch does not,
# or when one side found the file and the other did not.
#
# NOT A COMPONENT, for ask_to_keep_line's reason: one line per Revit, and no
# setting is a fault. The rollup's write gate takes Changes from the Revits
# that said it, and writing_said_by() names them.
SETTINGS_NOT_SENT = "not sent"

# (the key in settings_state's answer, the switch's name as Revit shows it)
SETTINGS_SWITCHES = (("changes", "Changes"), ("admin", "Admin"),
                     ("publish", "Publish"))

SETTINGS_OLDER = (
    "Switches: this Revit's Heron does not report its settings - its Heron add-in "
    "is older than this chat's, so which settings file it decides by, and its "
    "Changes, Admin and Publish switches, cannot be said here. Look in Revit, under "
    "Heron > AI Bridge > Settings.")
SETTINGS_NOT_READ = (
    "Switches: Heron could not read this Revit's settings just now - it gave no "
    "answer Heron could use. Ask again, or look in Revit, under Heron > AI Bridge > "
    "Settings.")


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
    each that disagrees. Ask to Keep is compared only when `asks` is a real
    true or false.

    said        what heron_bridge_client.settings_state returned
    asks        what heron_bridge_client.ask_to_keep_state returned
    here_file   this chat's own settings file, or None when it has none
    here_found  whether that file is there
    here        heron_config.switches() of this chat's own copy
    """
    if not isinstance(said, dict):
        return None
    here = here or {}
    named_alike = same_name(said.get("file"), here_file)
    disagree = [(name, bool(said.get(key)), bool(here.get(key)))
                for key, name in SETTINGS_SWITCHES
                if bool(said.get(key)) != bool(here.get(key))]
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
    """"Changes (this Revit: on, this chat's: off) and Admin (...)"."""
    parts = ["%s (this Revit: %s, this chat's: %s)" % (name, _on(revit), _on(chat))
             for name, revit, chat in switches]
    return parts[0] if len(parts) == 1 else ", ".join(parts[:-1]) + " and " + parts[-1]


def settings_lines(said, asks, here_file, here_found, here):
    """
    The line for what one Revit said of its settings, and a second when this
    chat's own copy differs - each one line, in Revit's words; the arguments
    are settings_differ's. An older add-in and an answer that could not be
    read get one line each, and neither claims a switch or a file.
    """
    if isinstance(said, str) and said == SETTINGS_NOT_SENT:
        return [SETTINGS_OLDER]
    if not isinstance(said, dict):
        return [SETTINGS_NOT_READ]

    named = ", ".join("%s %s" % (name, _on(said.get(key)))
                      for key, name in SETTINGS_SWITCHES)
    if said.get("found"):
        first = ("Switches in this Revit: %s - read just now from %s, the file this "
                 "Revit decides every change by." % (named, said["file"]))
    else:
        first = ("Switches in this Revit: %s - each at its default, because this Revit "
                 "found no settings file at %s." % (named, said["file"]))

    found = settings_differ(said, asks, here_file, here_found, here)
    if not differs(found):
        return [first]

    chat = here_file or "no settings file of its own"
    believe = ("This Revit decides by its own file, so the switches above are the ones "
               "that count.")
    if not found["named_alike"]:
        if found["switches"]:
            second = ("This chat's own settings file differs from this Revit's: this chat "
                      "reads %s, this Revit reads %s, and they disagree on %s. %s"
                      % (chat, said["file"], _disagreement(found["switches"]), believe))
        else:
            second = ("This chat reads a different settings file from this Revit's: this "
                      "chat reads %s, this Revit reads %s. The switches agree just now, but "
                      "one pressed in this Revit is not seen in this chat's file. %s"
                      % (chat, said["file"], believe))
        return [first, second]

    two = ("so this chat's copy and this Revit's are two files - a Claude app that keeps "
           "its own copy of the Heron folder does this")
    if found["switches"]:
        second = ("This chat's own copy of the settings file differs from this Revit's, "
                  "though both are named %s: they disagree on %s, %s. %s"
                  % (said["file"], _disagreement(found["switches"]), two, believe))
    else:
        second = ("This chat's own copy of the settings file is not this Revit's, though "
                  "both are named %s: this Revit %s a file there and this chat %s, %s. "
                  "The switches agree just now. %s"
                  % (said["file"], "found" if said.get("found") else "found no",
                     "found one" if here_found else "found none", two, believe))
    return [first, second]


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
