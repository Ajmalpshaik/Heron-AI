#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-SRV-001, HERON-MCP-HLT-005, HERON-MCP-VER-007
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Heron's MCP server - Step 3, extended at Steps 5 and 6.

The moment the whole chain exists: a sentence in the AI host, through MCP, down
the named pipe, onto Revit's own thread, and back. Everything after this step
adds capability to a chain that already works, which is a far easier problem
than making the chain exist.

    python mcp/server/heron_mcp_server.py        # speaks MCP over stdio

THE TOOLS, and which of them can change anything:

    revit_health              reads      is Revit working, and what is open
    revit_select_by_category  reads      the Phase 0 goal - visible on screen
    heron_version             reads      do both halves agree
    revit_use_session         reads      which Revit this chat means
    revit_use_this_model      reads      which MODEL this chat means
    revit_preview_move        reads      what a move WOULD do. Changes nothing
    revit_apply_move          WRITES     the only tool here that can

Six of the seven only look. revit_apply_move is the exception, and it cannot
run on its own: it applies a preview the user has already seen, once, and the
add-in re-checks the model before it writes. Writing is switched off entirely
until write.enabled is set - see HeronPermissions.

    ===================== NOT PROVEN =====================
    The Step 6 half was written on a machine with no Revit.
    The add-in code behind revit_apply_move has never been
    compiled or run. See HANDOVER section 6.
    ======================================================

The answer is written for a person, not for a machine to parse. The host reads
it aloud, so it says what is true and what to do next - never a status code.

NAMING NOTE: this repository has a folder called `mcp/` and the MCP SDK is a
package called `mcp`. They do not collide: a real installed package always wins
over a same-named directory, verified rather than assumed. Do not "fix" this by
renaming the folder.
"""

import os
import sys

# The bridge client is the layer below this one. It stays dependency-free on
# purpose, so `doctor` keeps working on a machine where nothing else does.
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "client"))

import heron_bridge_client as bridge          # noqa: E402
from heron_session import SessionBinding, NotBound   # noqa: E402
from heron_write import (BadDistance, DocumentPin, PendingApproval,   # noqa: E402
                         describe, parse_millimetres)
from heron_failure import analyse, explain          # noqa: E402
import heron_tools as tools                         # noqa: E402
import heron_config as configuration                # noqa: E402
import heron_health as health                       # noqa: E402
import heron_brain as brain                         # noqa: E402
from mcp.server.fastmcp import FastMCP        # noqa: E402

server = FastMCP("heron")

# One chat, one Revit. Lives as long as this server does, which is as long as
# the chat does - the correct scope for a binding (docs/25).
binding = SessionBinding()

# One chat, one MODEL - the other half of the same question (Golden Rule 20),
# and the approval the user has actually been shown. Same scope, same reason.
pinned = DocumentPin()
approval = PendingApproval()


def _describe(b, reply):
    """One session, as a line a person can read."""
    where = "Revit %s, session %s" % (b.revit_version, b.pid)

    if reply is None:
        return "  %s - connected, but not answering right now." % where

    if not reply.get("ok"):
        # revit_busy and still_running are ordinary states, not faults. Their
        # message already says what to do, so pass it through rather than
        # inventing wording for it here.
        return "  %s - %s" % (where, reply.get("message") or reply.get("error"))

    document = reply.get("document")
    count = reply.get("count")
    line = "  %s - open model: %s" % (where, document)
    if count is not None:
        line += ", %s elements" % "{:,}".format(count)
    if reply.get("unsaved"):
        line += " (unsaved changes)"
    return line


@server.tool()
def revit_health() -> str:
    """
    Check whether Revit is reachable and what model is open.

    Reports every connected Revit session: its version, whether the bridge is
    answering, and which model it has open. Use it when the user asks whether
    Revit is working or connected, before relying on any other Revit answer,
    or when a Revit request has failed and the reason is not obvious.
    """
    try:
        live, starting, stale, mismatched = bridge.discover()
    except Exception as exc:                      # never let the tool itself fail
        return ("Heron could not read its own session list: %s: %s\n"
                "Run  python mcp/client/heron_bridge_client.py doctor  for the full picture."
                % (type(exc).__name__, exc))

    # The four-state rollup first, so "is anything wrong?" is answered before
    # the detail rather than having to be inferred from it. The detail below is
    # unchanged - it was proven in Steps 3 and 5 and is what a person actually
    # reads when something IS wrong.
    settings = configuration.load()
    rollup = health.assess(
        live=live, starting=starting, mismatched=mismatched,
        config_problems=configuration.problems(settings),
        writing=configuration.writing_enabled(settings),
        protocol_version=bridge.PROTOCOL_VERSION,
        versions_agree=not mismatched)

    lines = [rollup.describe(), ""]

    if live:
        lines.append("Revit is connected." if len(live) == 1
                     else "%d Revit sessions are connected." % len(live))
        for index, b in enumerate(live, 1):
            # Numbered, so the user can answer "1" rather than read back a
            # process id. Which one is in use is stated, not implied.
            mark = ""
            if b.pid == binding.pid:
                mark = "  <- in use%s" % ("" if binding.was_chosen else " (assumed, not chosen)")

            # COUNT ONLY THE SESSION THIS CHAT ALREADY HOLDS.
            #
            # count_elements is NOT lease-exempt, so asking it of every session
            # would claim a lease on every free Revit in the list - and then
            # merely asking "is Revit working?" would quietly take every Revit
            # the user has open, refusing the chats that were about to use them.
            # A health check that changes who owns what is not a health check.
            #
            # This contradicted the exemption built for exactly this reason one
            # commit earlier: looking must never be the act of claiming. Found
            # by auditing after the lease, not before it.
            #
            # For the rest, `availability` uses `info`, which is exempt. The
            # element count is lost for a session this chat does not hold, and
            # that is the right trade - "in use by another chat" is the more
            # useful fact anyway, and reading another chat's model through a
            # health check was never the point.
            if b.pid == binding.pid:
                detail = _describe(b, b.request("count_elements"))[1:]
            else:
                detail = "  Revit %s, session %s - %s" % (
                    b.revit_version, b.pid, bridge.availability(b))

            lines.append("  %d)%s%s" % (index, detail, mark))
            b.close()

        if len(live) > 1 and binding.pid is None:
            lines.append("")
            lines.append("More than one is connected, so Heron will ask which you mean before "
                         "sending anything to Revit.")

    if starting:
        lines.append("")
        for b in starting:
            lines.append("  Revit %s (session %s) is running but its bridge is not answering yet - "
                         "it is probably still starting up." % (b.revit_version, b.pid))

    if mismatched:
        lines.append("")
        for b in mismatched:
            lines.append("  Revit %s (session %s) speaks protocol %s and this client speaks %s. "
                         "Restart that Revit to finish updating; Heron will not talk across "
                         "protocols." % (b.revit_version, b.pid, b.protocol_version,
                                         bridge.PROTOCOL_VERSION))

    if not live and not starting and not mismatched:
        lines.append("No Revit is connected.")
        lines.append("")
        lines.append("Open Revit, then press  Heron AI > Heron  on the ribbon. The button lights up "
                     "when it is connected, and a Revit that was never connected is invisible to "
                     "Heron by design - nothing reaches a model the user did not offer up.")

    if stale:
        lines.append("")
        lines.append("  (cleared %d stale entry(ies) from a Revit that did not shut down cleanly)"
                     % len(stale))

    return "\n".join(lines)


@server.tool()
def revit_select_by_category(category: str = "ducts") -> str:
    """
    Select every element of one category in the open Revit model, so the user
    can see them highlighted on screen.

    Use when the user asks to select, highlight or find elements of a kind -
    "select all ducts". Selecting changes only what is highlighted, never the
    model itself, so it is safe and needs no confirmation.

    Heron currently understands ducts. Other categories arrive as each one is
    tried against a real model.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return str(unbound)

    reply = session.request("select_by_category", op_args={"category": category})
    session.close()

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        # revit_busy, unknown_category and no_document already say what to do.
        return reply.get("message") or reply.get("error") or "The request was refused."

    # Naming the document is not enough on its own. Two Revit sessions can
    # both have a model called Project1 open - it happened on the very first
    # run of this tool - and then "in Project1" identifies nothing. The
    # session is what actually distinguishes them, so it is always said.
    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)

    selected = reply.get("selected", 0)
    if selected == 0:
        return "No %s in %s. Nothing was selected." % (reply.get("category"), where)

    return ("Selected %s %s in %s.\n(%s)"
            % ("{:,}".format(selected), reply.get("category"), where, reply.get("scope")))


@server.tool()
def heron_version() -> str:
    """
    Report Heron's version, the protocol it speaks, and the version of each
    connected Revit add-in — and whether all three agree.

    Use when the user asks which version they are running, when reporting a
    problem, or when behaviour differs between two Revit sessions and the
    versions might explain it.
    """
    lines = ["Heron %s, bridge protocol %s." % (bridge.HERON_VERSION, bridge.PROTOCOL_VERSION)]

    try:
        live, starting, stale, mismatched = bridge.discover()
    except Exception as exc:
        lines.append("Could not read the session list: %s: %s" % (type(exc).__name__, exc))
        return "\n".join(lines)

    if not live and not mismatched:
        lines.append("")
        lines.append("No Revit is connected, so there is no add-in version to compare against.")
        return "\n".join(lines)

    lines.append("")
    disagreed = False

    for b in live:
        agrees = b.protocol_version == bridge.PROTOCOL_VERSION
        note = "" if agrees else "   <- protocol %s, MISMATCH" % b.protocol_version
        if not agrees:
            disagreed = True
        lines.append("  Revit %s (session %s) - add-in %s%s"
                     % (b.revit_version, b.pid, b.addin_version, note))
        b.close()

    for b in mismatched:
        disagreed = True
        lines.append("  Revit %s (session %s) - add-in %s   <- protocol %s, MISMATCH"
                     % (b.revit_version, b.pid, b.addin_version, b.protocol_version))

    lines.append("")
    if disagreed:
        # A mismatch is not a warning to work around. The add-in and the client
        # are two halves of one protocol, and half-understanding each other is
        # how a wrong answer looks exactly like a right one.
        lines.append("Those versions do not agree. Restart the Revit whose protocol differs so it "
                     "picks up the installed add-in - Heron refuses to talk across protocols "
                     "rather than guess what the other side meant.")
    else:
        lines.append("Everything agrees.")

    return "\n".join(lines)


@server.tool()
def revit_use_session(session: str) -> str:
    """
    Choose which connected Revit this chat should work with, when more than
    one is open.

    Pass the number from the list Heron offered, or the session id. Use this
    when the user says which Revit they mean, or when a request was refused
    because more than one is connected.

    The choice sticks for the rest of the conversation. If that Revit closes,
    Heron stops and says so rather than moving to another model on its own.
    """
    try:
        chosen = binding.choose(session)
    except NotBound as unbound:
        return str(unbound)

    document = "the open model"
    reply = chosen.request("count_elements")
    if reply and reply.get("ok"):
        document = "%s, %s elements" % (reply.get("document"),
                                        "{:,}".format(reply.get("count", 0)))
    chosen.close()

    return ("Now working with Revit %s (session %s) - %s.\n"
            "Every request goes there until you say otherwise. If it closes, Heron will stop "
            "rather than switch to another model."
            % (chosen.revit_version, chosen.pid, document))


@server.tool()
def revit_preview_move(category: str = "ducts", distance: str = "") -> str:
    """
    Show what moving elements up or down WOULD do, without changing anything.

    Use this whenever the user asks to move, raise, lower or shift elements.
    It changes nothing: it reports how many would move, how many would be
    skipped, and in which model. Call revit_apply_move afterwards only if the
    user says yes to what this describes.

    Distance is in millimetres - "200", "200 mm", "0.5 m". A negative distance
    moves down. Heron does not accept feet or inches.
    """
    try:
        millimetres = parse_millimetres(distance)
    except BadDistance as bad:
        return str(bad)

    try:
        session = binding.resolve()
    except NotBound as unbound:
        return str(unbound)

    reply = session.request("preview_move", op_args={
        "category": category,
        # As a string, matching what the add-in's JSON reader can read. The
        # number has already been validated here and is validated again there.
        "millimetres": repr(millimetres),
    })
    session.close()

    if reply is None:
        approval.clear()
        return "Revit %s (session %s) did not answer. Nothing was changed." % (
            session.revit_version, session.pid)

    if not reply.get("ok"):
        approval.clear()
        return reply.get("message") or reply.get("error") or "The request was refused."

    # GOLDEN RULE 20. The preview is about whichever model is in front. If
    # that is not the model this chat has been working on, say so and offer
    # nothing to approve - a preview the user skims for the model they THINK
    # they are in is exactly how the wrong building gets changed.
    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        approval.clear()
        return wrong_model

    summary = "%s up %s in %s" % (
        "{:,}".format(reply.get("willMove", 0)) + " " + str(reply.get("category")),
        describe(millimetres), reply.get("document"))

    approval.offer(reply.get("token"), summary)

    lines = ["This would move %s." % summary]

    skipped = reply.get("willSkip", 0)
    if skipped:
        lines.append("%s would be skipped - pinned, or owned by another user."
                     % "{:,}".format(skipped))

    lines.append("")
    lines.append("Nothing has been changed yet. Say yes and Heron will apply it, "
                 "and one Ctrl+Z in Revit puts it back.")
    lines.append("(The preview lasts about %s minutes, and Heron checks the model "
                 "again before it writes.)" % (int(reply.get("expiresInSeconds", 120)) // 60))

    return "\n".join(lines)


@server.tool()
def revit_apply_move() -> str:
    """
    Apply the move the user has just approved, after seeing revit_preview_move.

    Use ONLY when the user has seen a preview and said yes to it. It changes
    the model. There is nothing to apply until a preview has been shown, and
    each approval can be used once.
    """
    token, summary = approval.take()
    if token is None:
        return ("There is nothing waiting to be approved. Ask for the change and Heron "
                "will show what it would do first. Nothing has been sent to Revit.")

    try:
        session = binding.resolve()
    except NotBound as unbound:
        return str(unbound)

    # idempotent=False, and this is the whole reason that flag exists. If the
    # answer is lost after the request left this machine, whether Revit ran it
    # cannot be known from here - and asking again would move the same
    # elements a second time. Everything before Step 6 only read, so retrying
    # was free; this is the first request where it is not.
    reply = session.request("move_elements", op_args={"token": token}, idempotent=False)
    session.close()

    # Everything that is not a success goes through the Failure Analysis Agent,
    # including a reply that never arrived. `writes` is the important argument:
    # it is what makes an unrecognised or lost outcome fail closed rather than
    # look retryable.
    #
    # It comes from the tool registry rather than a literal True typed here.
    # A hand-typed flag can end up disagreeing with the declaration that says
    # what this tool does, and the direction it would disagree in - a write
    # being treated as a read - is the one that retries a move that already
    # happened.
    failure = analyse(reply, writes=tools.writes("revit_apply_move"))
    if failure is not None:
        return explain(failure)

    moved = "{:,}".format(reply.get("moved", 0))
    lines = ["Moved %s %s %s in %s." % (moved, reply.get("category"),
                                        reply.get("distance"), reply.get("document"))]

    if reply.get("skipped"):
        lines.append("%s were skipped - pinned, or owned by another user."
                     % "{:,}".format(reply.get("skipped")))

    if reply.get("warnings"):
        lines.append("Revit raised %s warning(s), which were allowed through."
                     % "{:,}".format(reply.get("warnings")))

    lines.append("")
    lines.append("One Ctrl+Z in Revit puts this back - it is a single undo step called "
                 "\"%s\"." % reply.get("undoEntry"))

    return "\n".join(lines)


@server.tool()
def revit_use_this_model() -> str:
    """
    Move this chat onto whichever model is now in front in Revit.

    Use when the user has deliberately switched project and says to work on
    this one - "use this model", "I'm in the other project now". Only needed
    after Heron has refused because the model changed.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return str(unbound)

    reply = session.request("count_elements")
    session.close()

    if reply is None or not reply.get("ok"):
        return "Heron could not read which model is open, so it has not moved the pin."

    approval.clear()      # whatever was pending described the OLD model
    title = pinned.repin(reply)

    return ("This chat is now working on %s. Anything pending from the previous model "
            "has been dropped." % title)


# ---------------------------------------------------------------------------
# The brain, reachable from a conversation
#
# Steps 7 to 14 built eight modules, seven fragments and ten skills that no tool
# could reach. docs/27 states Phase 2's third definition-of-done clause as *the
# Orchestrator resolves through capabilities rather than agent names*; the
# Orchestrator is the host, and the host only has what is declared here.
#
# All three read what Heron KNOWS and none of them sends anything to Revit.
# ---------------------------------------------------------------------------

# NOTHING BELOW CAN RUN A FRAGMENT, AND EVERY ANSWER SAYS SO.
#
# A fragment carries C# in impl/, and there is no executor: the bridge speaks a
# fixed set of operations and none of them compiles anything. D-28 chose Roslyn
# in-process for this and it is not built. So resolving a request to a
# capability tells the host WHAT WOULD DO THE JOB and nothing about whether it
# can be done today - and a tool that let that be inferred would be worse than
# no tool, because a plan built on it would fail at the last step.
_CANNOT_RUN = (
    "Heron can say what would do this. It cannot do it yet: a fragment's code "
    "has no way to reach Revit - the bridge speaks a fixed set of operations "
    "and none of them runs one.")

_NOT_PROVEN = (
    "Nothing here is proven. Every skill and every fragment is DRAFT, and "
    "D-30 promotes on one recorded proof containing a negative case, which "
    "needs a real model. See NEEDS-CHECKING.md.")


def _revit_version():
    """
    (release, how it was decided) for the version wall, without ever asking.

    LOOKING MUST NEVER BE THE ACT OF CLAIMING. revit_health learned that about
    the lease one commit after building it; the same applies here for a
    different reason. binding.resolve() can stop and ask which Revit the user
    means - a fair question before touching a model, an absurd one before
    reading a file that shipped with Heron. So this never calls it.

    Returns (None, None) when nothing is connected, and the caller then says
    the version filter did not run rather than quietly leaving it out.
    """
    try:
        live, _starting, _mismatched = binding.sessions()
    except Exception:
        return None, None

    try:
        if binding.pid is not None:
            for b in live:
                if b.pid == binding.pid:
                    return b.revit_version, ("chosen" if binding.was_chosen
                                             else "assumed, not chosen")
        if len(live) == 1:
            # An assumption, and named as one. It decides only which fragments
            # are OFFERED, never which model is touched - but rule 5 of the
            # handover's own list is that an assumption is not a choice, and
            # the cheapest place to keep that honest is where it is made.
            return live[0].revit_version, "the only Revit connected"
        return None, None
    finally:
        for b in live:
            b.close()


@server.tool()
def heron_capabilities() -> str:
    """
    List what Heron knows how to do - the jobs it can be asked for, what each
    one needs, and which of those needs nothing provides yet.

    Use this before planning any Revit work, to find out whether Heron has a
    way of doing it at all. Answers "what can you do?", "can you lay out
    sprinklers?", "do you know how to trace a system?". Reads only files that
    shipped with Heron; it never touches the model and needs no Revit.
    """
    try:
        found = brain.catalogue()
    except brain.BrainUnavailable as why:
        return str(why)

    ready = [s for s in found["skills"] if s["ready"]]
    waiting = [s for s in found["skills"] if not s["ready"]]

    lines = ["%d job(s) Heron knows by name. %d have every part they need, "
             "%d are waiting on something nobody has built."
             % (len(found["skills"]), len(ready), len(waiting)), ""]

    if ready:
        lines.append("Every part provided:")
        for s in ready:
            said = s["utterances"][0] if s["utterances"] else ""
            lines.append('  %-34s e.g. "%s"' % (s["name"], said))

    if waiting:
        lines.append("")
        lines.append("Waiting on a missing piece:")
        for s in waiting:
            lines.append("  %-34s needs %s"
                         % (s["name"], ", ".join(s["missing"])))

    lines.append("")
    lines.append("%d capability(ies) with a provider; %d wanted and unprovided."
                 % (len(found["capabilities"]), len(found["gaps"])))

    if found["problems"]:
        lines.append("")
        for line in found["problems"]:
            lines.append("  PROBLEM  %s" % line)

    lines.append("")
    lines.append('"Every part provided" means each piece has something on disk '
                 "that claims to do it. It does not mean Heron can run it. "
                 + _CANNOT_RUN)
    lines.append(_NOT_PROVEN)
    return "\n".join(lines)


@server.tool()
def heron_resolve(capability: str) -> str:
    """
    Ask who can do one named capability - for example FILTER_ELEMENTS_BY_CATEGORY.

    Use after heron_capabilities or heron_lookup has named one, to see what
    would carry it out, at what risk, and on which Revit releases. Ask for the
    capability, never for a fragment id: which fragment serves it is Heron's
    to decide and can change without any plan changing.
    """
    revit, how = _revit_version()

    try:
        found = brain.resolve(capability, revit=revit)
    except brain.BrainUnavailable as why:
        return str(why)

    where = ("" if revit is None
             else " on Revit %s (%s)" % (revit, how))

    if not found["providers"]:
        if found.get("blocked_by_version"):
            # The distinction the whole wall exists for. "Nobody can do that"
            # and "nobody can do that HERE" send a user in opposite directions.
            return ("Heron knows %s, and nothing that provides it works%s.\n"
                    "Those providers exist - they are declared for other Revit "
                    "releases, and Heron will not offer one outside the "
                    "releases it declares. A fragment applied to the wrong "
                    "release does not announce itself: it runs, it half-works, "
                    "and it is found later by somebody measuring something."
                    % (capability, where))
        return ("Nothing provides %s.\n"
                "That absence IS the gap - Heron keeps no second list of "
                "missing things, because one that had to be kept in step "
                "would eventually disagree with this answer.\n"
                "Run heron_capabilities to see which jobs are waiting on it."
                % capability)

    lines = ["%s%s" % (capability, where),
             "  risk       %s   (the highest any provider carries, worked out "
             "rather than declared)" % found["risk"],
             "  area       %s" % found["domain"],
             "  Revit      %s   (releases EVERY provider supports, not any)"
             % (", ".join(found["revit"]) or "none in common"),
             "  best state %s" % found["status"],
             "",
             "  %d provider(s), most trusted first:" % len(found["providers"])]
    for p in found["providers"]:
        lines.append("    %-14s %-11s %s" % (p["id"], p["status"], p["kind"]))

    if revit is None:
        lines.append("")
        lines.append("No Revit is connected, so the version filter did not "
                     "run and this is every provider rather than the ones "
                     "that would work on your release.")

    lines.append("")
    lines.append(_CANNOT_RUN)
    lines.append(_NOT_PROVEN)
    return "\n".join(lines)


@server.tool()
def heron_lookup(request: str) -> str:
    """
    Work out which capability would serve a request written in the user's own
    words - "select all the ducts", "how many air terminals".

    Use when the user asks for Revit work and you need to know whether Heron
    has a way of doing it. It answers with the capability, so a plan never
    depends on which fragment happens to serve it today. Touches nothing.
    """
    revit, how = _revit_version()

    try:
        found = brain.lookup(request, revit=revit)
    except brain.BrainUnavailable as why:
        return str(why)

    if not found["capability"]:
        lines = ["Heron has no way of doing that."]
        blocked = [e for e in found["excluded"] if "Revit" in e["reason"]]
        if blocked:
            lines.append("%d fragment(s) were excluded because they are not "
                         "declared for Revit %s. They exist - they are just "
                         "not for this release." % (len(blocked), revit))
        lines.append("Run heron_capabilities to see what it does know.")
        return "\n".join(lines)

    # THE CAPABILITY IS THE ANSWER. The provider is underneath it as evidence,
    # so a plan is built on the capability and stays true when the fragment
    # behind it is replaced, split or retired.
    lines = ['"%s"' % request,
             "  would need   %s" % found["capability"],
             "  matched by   %s" % found["route"],
             "  provided by  %s" % found["provider"],
             "",
             "  %s" % found["note"]]

    if len(found["candidates"]) > 1:
        lines.append("")
        lines.append("  Other capabilities that came close:")
        seen = set()
        for c in found["candidates"][1:]:
            if c["capability"] in seen or c["capability"] == found["capability"]:
                continue
            seen.add(c["capability"])
            lines.append("    %-30s %s" % (c["capability"], c["why"]))

    if revit is None:
        lines.append("")
        lines.append("No Revit is connected, so the version filter did not run.")
    else:
        lines.append("")
        lines.append("Filtered to Revit %s (%s)." % (revit, how))

    lines.append("")
    lines.append(_CANNOT_RUN)
    lines.append(_NOT_PROVEN)
    return "\n".join(lines)


if __name__ == "__main__":
    if os.name != "nt":
        # The bridge is a Windows named pipe, and Revit is Windows-only.
        sys.stderr.write("Heron's bridge uses Windows named pipes. Revit is Windows-only.\n")
        sys.exit(2)
    server.run()
