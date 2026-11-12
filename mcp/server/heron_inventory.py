#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-SRV-001
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
The inventory reads: what a model holds, each from ONE fixed operation in the
add-in - phases, levels, worksets, views, a picture of a view, sheets, rooms,
schedules, families, the export check, imports, annotation, systems,
parameters and groups.

    python mcp/server/heron_tools.py        # every tool, and what it may do

NOT RUN. heron_mcp_server.py is the file a host starts, and it imports this
one LAST, after its own tools - so the list a host is served is in the order
it always was, these at the end. Each tool here sends one named operation,
never a fragment, and answers from that one reply; heron_tools.TOOLS declares
each READ, and the picture ANALYZE - none a change to the model.

WHY THEY ARE HERE. They were the foot of heron_mcp_server.py until
2026-11-11, moved whole and unchanged - the first tools to leave that file
(FRAGMENT-ISSUES row 5b-1555). They were first because they need the least:
the server object, the binding and the pin, and two helpers that say how a
call ended, all from heron_server_core. Nothing here imports the entry file,
which would run it a second time.

A file joins the server's source by joining heron_tools.SERVER_FILES, and
tests/test_tool_registry.py fails on one that registers a tool and is not on
that list.
"""

import base64
import binascii

from heron_server_core import (NEWLINE, _Image, _ended, _ended_by_reply,   # noqa: E402
                               binding, pinned, server)
from heron_session import NotBound                          # noqa: E402
# heron_server_core has loaded heron_brain, which put brain/ on the path.
from heron_install import install_line                      # noqa: E402


@server.tool()
def revit_phases() -> str:
    """
    List the phases and design options in the open Revit model, with how many
    elements sit in each.

    Use whenever a count looks wrong and links have already been ruled out,
    and whenever the user mentions a phase, existing versus new work,
    demolition, a design option, or an alternative layout. On a real job
    "412 ducts" is a fact about a model AND a phase AND a design option: the
    Existing phase holds the survey, New Construction holds the work, and an
    option set called something like "Riser Layout" can hold two complete
    alternative arrangements of the same shafts.

    Every number it gives was counted by asking each element what it carries,
    not worked out from how a filter is believed to behave. Compare them with
    a count you were given elsewhere and you can see for yourself what that
    count included.

    It reads only. No view's phase is changed and no design option is made
    active — both of those change what everybody else on the job sees.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_phases")
    session.close()
    _ended_by_reply("revit_phases", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    phases = reply.get("phases") or []
    options = reply.get("designOptions") or []
    placed = reply.get("placedElements", 0)

    if len(phases) <= 1 and not options:
        return ("%s has one phase and no design options, so a count of this "
                "model is a count of the whole model. %s placed elements."
                % (where, "{:,}".format(placed)))

    lines = ["%s: %d phase(s), %d design option(s), %s placed elements."
             % (where, len(phases), len(options), "{:,}".format(placed)), ""]

    for phase in phases:
        gone = phase.get("demolished", 0)
        lines.append("  phase   %-26s %s built%s"
                     % (phase.get("name"),
                        "{:,}".format(phase.get("created", 0)),
                        "" if not gone else ", %s demolished" % "{:,}".format(gone)))

    if options:
        lines.append("")
        for option in options:
            lines.append("  option  %-26s %s elements   [%s%s]"
                         % (option.get("name"),
                            "{:,}".format(option.get("elements", 0)),
                            option.get("set") or "no set named",
                            ", primary" if option.get("primary") else ""))
        lines.append("")
        lines.append("%s elements are in the main model, carrying no option at all."
                     % "{:,}".format(reply.get("inMainModel", 0)))

    view, phase, filtered = (reply.get("activeView"), reply.get("viewPhase"),
                             reply.get("viewPhaseFilter"))
    if view:
        lines.append("")
        lines.append("The active view %r is on phase %s with phase filter %s — which is "
                     "what a number read off that view has been through."
                     % (view, phase or "not set", filtered or "none"))

    no_phase = reply.get("withNoPhase", 0)
    if no_phase:
        lines.append("%s elements name no phase at all. Levels, grids and views are not "
                     "built in one; that is normal and is not a count of nothing."
                     % "{:,}".format(no_phase))

    return "\n".join(lines)


@server.tool()
def revit_levels() -> str:
    """
    List the levels and grids in the open Revit model, and what sits on each
    level - with the three states that are hard to see in Revit and costly to
    miss: two levels at the SAME elevation, a level with NOTHING on it, and
    names that sort out of order.

    Every elevation is as Revit prints it, in the project's own units - never
    a raw number. It reads only: no level is renamed and no elevation moved.
    """
    # Levels are the hosts almost everything else depends on: a duct is at a
    # level, a room is bounded between two, a view is cut at one. A raw number is
    # never given because Revit stores lengths in decimal feet whatever the
    # project is set to, and an unlabelled 9.84 gets read as millimetres by the
    # next person who sees it. Moving a level drags every element hosted on it,
    # which is why this tool never does. Trimmed 2026-10-17 (docs/04 s8.8).
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_levels")
    session.close()
    _ended_by_reply("revit_levels", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    levels = reply.get("levels") or []
    grids = reply.get("grids") or []

    if not levels and not grids:
        return ("%s has no levels and no grids. That is normal for a family or "
                "a detail file and unusual for a project." % where)

    lines = ["%s: %d level(s), %d grid(s)." % (where, len(levels), len(grids)), ""]

    for level in levels:
        flag = "   <-- shares this elevation" if level.get("sharesElevation") else ""
        on = level.get("elements", 0)
        lines.append("  %-28s %-14s %s%s"
                     % (level.get("name"),
                        level.get("elevation") or "elevation not given",
                        ("nothing on it" if not on
                         else "%s element(s)" % "{:,}".format(on)),
                        flag))

    if grids:
        lines.append("")
        shown = grids[:12]
        for grid in shown:
            lines.append("  grid    %-20s %s"
                         % (grid.get("name"), grid.get("extent") or ""))
        if len(grids) > len(shown):
            lines.append("  ... and %d more grid(s)." % (len(grids) - len(shown)))

    findings = []
    if reply.get("levelsSharingAnElevation"):
        findings.append(
            "%d level(s) sit at the same elevation as another. Legal, sometimes "
            "deliberate, and the usual reason something modelled on one cannot "
            "be found on the other — the Project Browser sorts by NAME, so the "
            "two are nowhere near each other in it."
            % reply["levelsSharingAnElevation"])
    if reply.get("levelsWithNothingOnThem"):
        findings.append(
            "%d level(s) have nothing on them — either set-out nobody cleared "
            "up, or work that went onto a different level than intended."
            % reply["levelsWithNothingOnThem"])
    if reply.get("repeatedGridNames"):
        findings.append(
            "%d grid name(s) are used more than once. A dimension to \"grid B\" "
            "stops being an instruction anybody can follow."
            % reply["repeatedGridNames"])
    if reply.get("namesSortOutOfOrder"):
        findings.append(
            "Level names do not sort in elevation order — \"Level 10\" sorts "
            "before \"Level 2\" in every browser and schedule, because both sort "
            "as text. An observation, not a fault.")

    if findings:
        lines.append("")
        for finding in findings:
            lines.append("  ! " + finding)

    on_no_level = reply.get("onNoLevel", 0)
    if on_no_level:
        lines.append("")
        lines.append("%s placed element(s) name no level at all. Grids, views and "
                     "some annotation do not sit on one; that is normal."
                     % "{:,}".format(on_no_level))

    lines.append("")
    lines.append("Elevations are as Revit prints them, in the project's own units. "
                 "Nothing was changed.")
    return "\n".join(lines)


@server.tool()
def revit_worksets() -> str:
    """
    List the worksets in the open Revit model, what is on each, and a sampled
    reading of who owns what.

    Somebody else owning most of the model is the system working, not a
    problem — on a workshared job that is the normal state, and it is reported
    in the same voice as anything else.

    Ownership is a SAMPLE, never a survey, and the answer always says how big
    the sample was. Asking the central model who owns an element costs one
    round trip per element on Revit's own thread, so sweeping a real job would
    freeze the model for whoever is working in it.

    If the model is not workshared it says so, which is a different answer
    from an empty list.

    It reads only. No workset is created, opened or closed, nothing is
    borrowed, and nothing is relinquished — relinquishing somebody's borrowed
    element loses their unsynchronised work.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_worksets")
    session.close()
    _ended_by_reply("revit_worksets", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)

    if not reply.get("workshared"):
        return ("%s is not workshared, so worksets, ownership and checkout do "
                "not apply to it. That is a different answer from an empty "
                "list. Families, detail files and single-person jobs are "
                "normally like this." % where)

    worksets = reply.get("worksets") or []
    lines = ["%s: %d workset(s), %s placed element(s)."
             % (where, len(worksets),
                "{:,}".format(reply.get("placedElements", 0))), ""]

    for workset in worksets:
        lines.append("  %-32s %12s   %s"
                     % (workset.get("name"),
                        "{:,}".format(workset.get("elements", 0)),
                        "open" if workset.get("open") else "CLOSED in this session"))

    # THE LIST HAS TO ACCOUNT FOR THE TOTAL, or it teaches the reader to
    # distrust it. Proved against Project1 work_ajmal.al on 2026-09-17, where
    # this printed `Workset1 181` and `Shared Levels and Grids 2` against
    # 3,513 placed elements - 183 named, 3,330 unexplained, and nothing in the
    # answer said where they went. The add-in had computed `onNoWorkset` all
    # along; this tool simply never read it.
    #
    # Most of a model genuinely sits on no workset - a view, a level, a family
    # symbol - so the number is large and normal. Large and normal and MISSING
    # is what makes a reader stop trusting the rows above it.
    #
    # THERE ARE TWO WAYS TO BE ABSENT AND THEY ARE NOT THE SAME THING, which
    # the first version of this fix got wrong and only a real model showed.
    #
    # `onNoWorkset` is an element naming NO workset. On a real model that is
    # usually ZERO - so this printed nothing, and looked like the fix had not
    # worked at all.
    #
    # `onOtherWorksets` is the actual gap: elements on a VIEW, FAMILY or
    # STANDARD workset. The list shows USER worksets, because those are the
    # ones a modeller made - but every view, family symbol and settings element
    # sits on one of the others. On Project1 work_ajmal.al that was 3,338 of
    # 3,542, and the answer named 204 of them.
    #
    # Reported as ONE line rather than listed: a row per view is noise.
    on_other = reply.get("onOtherWorksets", 0)
    if on_other:
        lines.append("  %-32s %12s   view / family / standard worksets"
                     % ("(not listed above)", "{:,}".format(on_other)))
    on_none = reply.get("onNoWorkset", 0)
    if on_none:
        lines.append("  %-32s %12s   not on any workset"
                     % ("(none named)", "{:,}".format(on_none)))

    sampled = reply.get("ownershipSampled", 0)
    of_total = reply.get("ownershipOf", 0)
    lines.append("")
    lines.append("Ownership, sampled over %s of %s element(s):"
                 % ("{:,}".format(sampled), "{:,}".format(of_total)))
    lines.append("  free                %s" % "{:,}".format(reply.get("freeInSample", 0)))
    lines.append("  owned by you        %s" % "{:,}".format(reply.get("ownedByMeInSample", 0)))
    lines.append("  owned by others     %s" % "{:,}".format(reply.get("ownedByOthersInSample", 0)))
    not_known = reply.get("ownershipNotKnownInSample", 0)
    if not_known:
        lines.append("  not known           %s   (different from free, and only "
                     "one of the two is safe to act on)" % "{:,}".format(not_known))

    owners = reply.get("ownersInSample") or []
    if owners:
        lines.append("")
        for owner in owners[:10]:
            lines.append("  %-24s %s in sample"
                         % (owner.get("name"), "{:,}".format(owner.get("inSample", 0))))
        if len(owners) > 10:
            lines.append("  ... and %d more." % (len(owners) - 10))

    closed = reply.get("closedWorksets", 0)
    if closed:
        lines.append("")
        lines.append("%d workset(s) are CLOSED in this session. Elements on a closed "
                     "workset are not in the model you are looking at, so any count "
                     "taken here has left them out." % closed)

    lines.append("")
    lines.append("Someone else owning part of the model is normal and is not a "
                 "finding. Nothing was changed.")
    return "\n".join(lines)


@server.tool()
def revit_views() -> str:
    """
    List the views in the open Revit model, what governs each of them, and
    what the active view is showing through.

    A count taken in a view is a count THROUGH that view - its template,
    discipline, detail level, filters and crop each remove things before you
    ever see them. So "412 ducts" is a fact about a view at least as much as
    about a model.

    It reports three states that are hard to see and expensive to miss: a view
    with NO template (nothing controls what it shows, and it drifts from every
    other view of the same thing), a view on NO sheet, and a template nothing
    uses. None of the three is an error.

    Sheets themselves belong to revit_sheets. This says only whether a view is
    placed, which is a fact about the view.

    It reads only. No template is applied and no visibility is changed.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_views")
    session.close()
    _ended_by_reply("revit_views", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    count = reply.get("viewCount", 0)
    if not count:
        return ("%s has no views at all, which is unusual for anything but a "
                "container file." % where)

    lines = ["%s: %s view(s), %d view template(s)."
             % (where, "{:,}".format(count), reply.get("templateCount", 0)), ""]

    for kind in sorted(reply.get("viewsByType") or [],
                       key=lambda row: -row.get("views", 0)):
        lines.append("  %-26s %s" % (kind.get("type"),
                                     "{:,}".format(kind.get("views", 0))))

    active = reply.get("activeView")
    if active:
        lines.append("")
        lines.append("Active view %r (%s)" % (active, reply.get("activeViewType")))
        lines.append("  template      %s" % (reply.get("activeViewTemplate") or "none"))
        lines.append("  discipline    %s" % (reply.get("activeViewDiscipline") or "not given"))
        lines.append("  detail level  %s" % (reply.get("activeViewDetailLevel") or "not given"))
        lines.append("  filters       %d" % reply.get("activeViewFilters", 0))
        lines.append("  crop          %s"
                     % ("ON - things outside it are not counted"
                        if reply.get("activeViewCropped") else "off"))
        lines.append("")
        lines.append("Any count you take in that view has been through all of the "
                     "above before you see it.")

    findings = []
    if reply.get("viewsWithNoTemplate"):
        findings.append(
            "%s view(s) have NO view template, so nothing controls what they "
            "show. They drift from every other view of the same thing - the "
            "usual reason two plans of one floor disagree."
            % "{:,}".format(reply["viewsWithNoTemplate"]))
    if reply.get("viewsOnNoSheet"):
        findings.append(
            "%s view(s) are on no sheet. Not a fault - working views are "
            "supposed to exist - but they cost file size and a browser nobody "
            "can find anything in."
            % "{:,}".format(reply["viewsOnNoSheet"]))
    if reply.get("templatesNothingUses"):
        findings.append(
            "%d view template(s) are applied to nothing. Set up once, never "
            "used, and quietly believed to be in force."
            % reply["templatesNothingUses"])

    if findings:
        lines.append("")
        for finding in findings:
            lines.append("  ! " + finding)

    lines.append("")
    lines.append("A view counts as placed if it is on a sheet as a viewport OR as a "
                 "schedule instance, so schedules are not reported as unplaced. "
                 "Nothing was changed.")
    return "\n".join(lines)


# THE VIEWS A PICTURE CAN BE LAID OVER THE PLAN FROM - Revit's ViewType names
# for a ViewPlan the add-in sends a crop for (row 5b-1481). A CEILING PLAN IS
# LEFT OUT until NEEDS-CHECKING MX19's reflected-ceiling step has run: it looks
# up, and nothing yet shows its picture is laid out as its crop box says.
_PLAN_TYPES = ("FloorPlan", "EngineeringPlan", "AreaPlan")

# How far, in pixels, the picture's height may sit from the one the crop's
# shape gives at its width - Revit rounds the height to a whole pixel. Beyond
# it the picture's edges are not the crop's, and no mapping is given.
_SHAPE_SLACK_PX = 2.0

# How far from square the crop's corners may be - the cosine of the angle
# between its top edge and its left edge. 0.001 is under a tenth of a degree.
_SQUARE_SLACK = 0.001

# The fields an add-in that knows about a plan's frame sends - all of them
# absent means the add-in is older than this server.
_FRAME_FIELDS = ("planFrame", "viewScale", "cropActive", "annotationCropActive",
                 "cropShape", "cropCornersMm", "cropCornersWhyNot")


def _png_size(picture):
    """(width, height) in pixels from a PNG's own header, or None."""
    if len(picture) < 24 or picture[12:16] != b"IHDR":
        return None
    width = int.from_bytes(picture[16:20], "big")
    height = int.from_bytes(picture[20:24], "big")
    return (width, height) if width and height else None


def _crop_corners(text):
    """The add-in's cropCornersMm - "x,y; x,y; x,y; x,y", top-left first and
    clockwise as the picture is seen - as four (x, y) in mm, or None."""
    corners = []
    for pair in (text or "").split(";"):
        parts = pair.split(",")
        if len(parts) != 2:
            return None
        try:
            corners.append((float(parts[0]), float(parts[1])))
        except ValueError:
            return None
    return corners if len(corners) == 4 else None


def _mm_text(value, places):
    """A number with at most PLACES decimals and no trailing zeros."""
    said = ("%." + str(places) + "f") % value
    said = said.rstrip("0").rstrip(".") if "." in said else said
    return "0" if said in ("-0", "") else said


def _picture_to_model(reply, picture):
    """
    The ONE line revit_view_image adds about where the picture sits in the
    model (row 5b-1481): how to turn a place on the picture into model mm, or
    why it cannot.

    Given only for a floor, engineering or area PLAN whose crop is ON, a plain
    rectangle, and whose ANNOTATION crop is OFF - the one case where the
    picture's edges can be the crop's - only when the add-in sent the crop's
    corners, they are square and seen from above (not mirrored), and the
    picture's own shape, read from its PNG header, is the crop's shape.
    Anything else is said in one line with no numbers: a position guessed
    from a picture would size the wrong duct.

    IN FRACTIONS OF THE PICTURE, NEVER PIXELS (review of ed899b5): the chat
    does not see the PNG's own pixels - Claude scales a large image down
    before reading it - so a tag is placed by how far across and down the
    picture it sits, 0 to 1, which scaling does not change.

    Millimetres from Revit's internal origin - the numbers EXTRACT_CAD_CURVES
    and DRAW_MEP_FROM_CAD's runsFound give, and its size@x,y hints take.
    """
    lead = "No model position from this picture: "
    if reply.get("mode") == "elements":
        return lead + "it is a temporary 3D view of elements."
    view_type = reply.get("viewType") or "view"
    if view_type == "CeilingPlan":
        return (lead + "it is a ceiling plan, and the mapping is not yet measured for "
                "ceiling plans (NEEDS-CHECKING MX19).")
    if view_type not in _PLAN_TYPES:
        return lead + "it is a %s, not a plan." % view_type
    if not any(field in reply for field in _FRAME_FIELDS):
        return (lead + "the add-in in Revit sent no crop for the plan - it is older than "
                "this server.")
    if reply.get("planFrame") == "unavailable":
        return (lead + "Revit did not give the crop for this picture (%s)."
                % (reply.get("planFrameError") or "no reason given"))
    if not reply.get("cropActive"):
        return lead + "the plan's crop is off, so its edges are not a known place."
    if reply.get("annotationCropActive") is not False:
        return (lead + "the plan's annotation crop is on (or not said), so the picture "
                "may reach past the crop.")
    shape = reply.get("cropShape") or "not said"
    if shape != "rectangle":
        return lead + "the plan's crop is %s, not a plain rectangle." % shape
    if reply.get("cropCornersWhyNot"):
        return lead + "%s." % reply.get("cropCornersWhyNot")
    corners = _crop_corners(reply.get("cropCornersMm"))
    size = _png_size(picture)
    if corners is None or size is None:
        return lead + "the crop's corners or the picture's size could not be read."

    (x0, y0), (x1, y1), (x2, y2), (x3, y3) = corners
    right = (x1 - x0, y1 - y0)            # top-left to top-right
    down = (x3 - x0, y3 - y0)             # top-left to bottom-left
    across = (right[0] ** 2 + right[1] ** 2) ** 0.5
    tall = (down[0] ** 2 + down[1] ** 2) ** 0.5
    if across <= 0 or tall <= 0:
        return lead + "the crop has no size."
    if abs(x1 + x3 - x0 - x2) > 1.0 or abs(y1 + y3 - y0 - y2) > 1.0:
        return lead + "the crop's corners do not make a rectangle."
    if abs(right[0] * down[0] + right[1] * down[1]) / (across * tall) > _SQUARE_SLACK:
        return lead + "the crop's corners are not at a right angle."
    # SEEN FROM ABOVE, the picture's rightward edge turned clockwise is its
    # downward edge: the z of (right x down) is negative. Positive is a
    # mirror image - the picture would put every tag on the wrong side.
    if right[0] * down[1] - right[1] * down[0] >= 0:
        return lead + "the crop's corners are mirrored."
    width, height = size
    if abs(height - width * tall / across) > _SHAPE_SLACK_PX:
        return (lead + "the picture is %d x %d px and the crop %s x %s mm - not the same "
                "shape, so the picture's edges are not the crop's."
                % (width, height, _mm_text(across, 0), _mm_text(tall, 0)))

    def formula(start, along_u, along_v):
        said = _mm_text(start, 1)
        for step, name in ((along_u, "u"), (along_v, "v")):
            if abs(step) < 0.05:
                continue
            said += " %s %s*%s" % ("-" if step < 0 else "+", _mm_text(abs(step), 1), name)
        return said

    scale = reply.get("viewScale")
    return ("Model position from this picture (%splan, crop on, annotation crop off): its "
            "corners are, in mm from Revit's internal origin, top-left %s,%s, top-right "
            "%s,%s, bottom-right %s,%s, bottom-left %s,%s. Measure the tag's position as a "
            "fraction of the picture's width and height - u across from the left edge and "
            "v down from the top edge, each 0 to 1 - and it is at x = %s, y = %s mm. "
            "Exact only if Revit's picture fills the crop edge to edge - not yet measured "
            "in Revit (NEEDS-CHECKING MX19); a tag just outside the crop may be missing "
            "from the picture."
            % ("1:%s " % scale if scale else "",
               _mm_text(x0, 1), _mm_text(y0, 1), _mm_text(x1, 1), _mm_text(y1, 1),
               _mm_text(x2, 1), _mm_text(y2, 1), _mm_text(x3, 1), _mm_text(y3, 1),
               formula(x0, right[0], down[0]), formula(y0, right[1], down[1])))


@server.tool()
def revit_view_image(view: str = "", elements: str = "", width: int = 1280) -> list:
    """
    Take a picture of a Revit view and LOOK at it - to check what you just
    did, instead of trusting a count.

    Say what to picture in one of two ways:

    - `view`: a view or sheet name as the Project Browser shows it. Empty
      means the view in front. It is pictured exactly as it stands - its
      template, crop and filters - and nothing about it is changed. If two
      views share the name, the answer lists each as "ViewType: Name" to
      copy. A cropped plan, no annotation crop, maps to model mm (not yet
      measured, MX19).
    - `elements`: element ids, comma separated, as Heron's answers print them
      - Revit's number, or a form's long id from CREATE_FAMILY_FORMS or
      REPORT_FAMILY_FORMS; the two may be mixed. Heron
      frames just those in a temporary isometric 3D view - a section box round
      them with a margin, model categories only, levels, grids and links left
      out - and takes the view away again. In a project, floors,
      ceilings and roofs over them are hidden, and named. In a family its
      forms are framed too, by their extents; its connectors are hidden
      unless asked for. An id
      not in the model or family in front refuses the whole picture, naming
      it.

    `width`: the picture's width, 320 to 2400 px (default 1280).

    IT CHANGES NOTHING. The temporary view is made inside a group Revit always
    rolls back - there is no way to keep it - and the picture is written to
    Heron's own folder, read back and deleted. Nothing is exported
    anywhere another person reads. revit_views gives a view's name.
    """
    if _Image is None:
        return _ended("refused",
                      "This MCP SDK has no image type, so Heron cannot hand a picture to the "
                      "chat. Update it, in the Heron folder, with:  %s - then restart the "
                      "chat. Nothing has been sent to Revit."
                      % install_line("pip install --user --upgrade mcp"))

    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    # THE PIN TRAVELS WITH THE REQUEST, as for a selection: given element ids
    # the add-in makes a view before it answers, so the guard has to stand in
    # front of that, not behind it. Empty when this chat has pinned nothing.
    reply = session.request("capture_view",
                            op_args={"view": (view or "").strip(),
                                     "elementIds": (elements or "").strip(),
                                     "pixelWidth": width,
                                     "expectProject": pinned.project_information_id or ""})
    session.close()
    _ended_by_reply("revit_view_image", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    try:
        picture = base64.b64decode(reply.get("png") or "", validate=True)
    except (TypeError, ValueError, binascii.Error):
        picture = b""
    if not picture.startswith(b"\x89PNG"):
        _ended("failed")
        return ("%s answered without a picture Heron can read, so there is nothing to "
                "look at. Ask again; if it repeats, the add-in may be older than this "
                "server." % where)

    if reply.get("mode") == "elements":
        lines = ["A picture of %d element(s) in %s, in a temporary 3D view, %s px wide."
                 % (reply.get("shown", 0), where, reply.get("pixelWidth"))]
        if reply.get("leftOut"):
            lines.append("Left out of the frame: %s." % reply.get("leftOut"))
        # IN THE FRAME, HIDDEN FROM THE PICTURE (row 5b-1328): what lies over
        # the elements, named so the chat never reads it as not there.
        if reply.get("hiddenAbove"):
            lines.append("Hidden from the picture, as they lie over them: %s. They are "
                         "still in the model." % reply.get("hiddenAbove"))
    else:
        lines = ["A picture of %r (%s) in %s, %s px wide."
                 % (reply.get("view"), reply.get("viewType"), where, reply.get("pixelWidth"))]
    if reply.get("widthClamped"):
        lines.append("The width asked for was outside 320 to 2400 px, so %s was used."
                     % reply.get("pixelWidth"))
    # WHERE IT SITS IN THE MODEL, or why that is not said (row 5b-1481).
    lines.append(_picture_to_model(reply, picture))

    # THE TWO PROMISES, SAID ONLY WHEN THE ADD-IN SAYS THEY WERE KEPT.
    #
    # AND "NOTHING WAS CHANGED" NEVER OVER A LOSS: the capture's rollback is
    # checked like every other (RevitFragment.RollBackAndCheck), and when it
    # took elements the last kept job made, `keptLost` counts them and
    # `rollbackNote` is the add-in's sentence for it, said in its place.
    note = reply.get("rollbackNote")
    if reply.get("rolledBack") is False:
        lines.append("! Revit did NOT confirm the temporary view was taken away. Look under "
                     "3D Views in the Project Browser, and at Undo, before going on.")
    elif reply.get("keptLost"):
        lines.append("! %s" % note)
    elif reply.get("mode") == "elements":
        lines.append("The temporary view is gone - Revit rolled it back. Nothing was changed.")
    else:
        lines.append("Nothing was changed.")
    if note and not (reply.get("keptLost") and reply.get("rolledBack") is not False):
        lines.append(note)
    if reply.get("fileDeleted") is False:
        lines.append("! The picture's file could not be deleted from Heron's captures "
                     "folder on this PC. That folder is safe to empty.")

    return [NEWLINE.join(lines), _Image(data=picture, format="png")]


def _schedules_counted(sheets):
    """Did the add-in that answered count schedules on sheets at all?

    An add-in built before 2026-09-27 sends `views` and no `schedules`, and
    its "nothing on it" means only "no views" - a sheet carrying just a
    schedule is among them (FRAGMENT-ISSUES 5b-238). This server and the
    add-in go live at different restarts, so the one cannot assume the other.
    """
    return any("schedules" in sheet for sheet in sheets)


def _sheet_line(sheet):
    """One sheet's line in revit_sheets: views and schedules counted apart.

    A sheet is NOTHING ON IT only when it has no view AND no schedule. The
    titleblock's revision schedule is never in the add-in's count, so it
    cannot make an empty sheet look full. When the add-in sent no schedule
    count, an empty-looking sheet is said to have no VIEWS - never nothing.
    """
    views = sheet.get("views") or 0
    schedules = sheet.get("schedules")
    note = ""
    if sheet.get("placeholder"):
        note = "   placeholder"
    elif not views and schedules is None:
        note = "   <-- no views; schedules not counted by this add-in"
    elif not views and not schedules:
        note = "   <-- NOTHING ON IT"
    elif not sheet.get("titleblock"):
        note = "   <-- no titleblock"
    counts = "%2d view(s)" % views
    if schedules is not None:
        counts += ", %d schedule(s)" % schedules
    return ("  %-14s %-38s %s%s"
            % (sheet.get("number"), (sheet.get("name") or "")[:38], counts, note))


def _empty_sheets_finding(count, schedules_counted):
    """The finding line for sheets with nothing on them, honest about what counted."""
    if not schedules_counted:
        return ("%d sheet(s) have no views placed on them. Schedules were NOT "
                "counted - the Heron add-in in this Revit is older than this "
                "check - so a sheet carrying only a schedule is among them. "
                "Restart Revit after the add-in is updated for the real count."
                % count)
    return ("%d sheet(s) have nothing placed on them - no view and no schedule. "
            "The Project Browser shows a sheet with one view and a sheet with "
            "none identically, so this does not show up until it is printed."
            % count)


@server.tool()
def revit_sheets() -> str:
    """
    List the sheets in the open Revit model - numbering, titleblocks and
    revisions.

    The sheets are the deliverable. This reports the states that turn up at
    issue, which is the worst moment to find them: a sheet with NOTHING on it
    (invisible in the Project Browser, which shows an empty sheet and a full
    one identically), a sheet with NO titleblock, more than one titleblock
    family across the set, and placeholder sheets.

    Views and schedules are counted apart. A sheet carrying only a schedule is
    not empty; the titleblock's own revision schedule is not counted, because
    every sheet has one.

    Placeholders are counted separately rather than left out - a reserved
    number must never read as a sheet that is ready.

    Sheet numbers are listed as they stand and never checked against a
    standard: Revit already refuses a duplicate, and what your numbering ought
    to look like is not something this can know.

    It reads only. No sheet is renumbered - renumbering breaks every drawing
    reference pointing at it, across the set and every consultant's copy.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_sheets")
    session.close()
    _ended_by_reply("revit_sheets", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    sheets = reply.get("sheets") or []
    if not sheets:
        return ("%s has no sheets. Normal for a working model or a family, and "
                "worth knowing before anybody asks for a drawing set from it."
                % where)

    lines = ["%s: %d sheet(s), %d revision(s), %d issued."
             % (where, reply.get("sheetCount", 0),
                reply.get("revisionCount", 0), reply.get("revisionsIssued", 0)), ""]

    shown = sheets[:30]
    for sheet in shown:
        lines.append(_sheet_line(sheet))
    if len(sheets) > len(shown):
        lines.append("  ... and %d more sheet(s)." % (len(sheets) - len(shown)))

    titleblocks = reply.get("titleblocks") or []
    if titleblocks:
        lines.append("")
        for block in titleblocks:
            lines.append("  titleblock  %-40s %d sheet(s)"
                         % (block.get("name"), block.get("sheets", 0)))

    findings = []
    if reply.get("sheetsWithNothingOnThem"):
        findings.append(_empty_sheets_finding(reply["sheetsWithNothingOnThem"],
                                              _schedules_counted(sheets)))
    if reply.get("sheetsWithNoTitleblock"):
        findings.append(
            "%d sheet(s) have no titleblock. They print with no border, no "
            "number, no revision and no signature block."
            % reply["sheetsWithNoTitleblock"])
    if reply.get("titleblockFamiliesInUse", 0) > 1:
        findings.append(
            "%d different titleblock families are in use across the set. Usually "
            "a sheet started from the wrong template, and it is not noticed "
            "until two borders appear on paper."
            % reply["titleblockFamiliesInUse"])
    if reply.get("placeholderSheets"):
        findings.append(
            "%d are PLACEHOLDER sheets - a number reserved for a drawing that "
            "does not exist yet. Counted separately, never as ready."
            % reply["placeholderSheets"])

    if findings:
        lines.append("")
        for finding in findings:
            lines.append("  ! " + finding)

    revisions = reply.get("revisions") or []
    if revisions:
        lines.append("")
        for revision in revisions[:12]:
            lines.append("  rev %-8s %-44s %s"
                         % (revision.get("number") or "?",
                            (revision.get("description") or "")[:44],
                            "issued" if revision.get("issued") else "not issued"))
        if len(revisions) > 12:
            lines.append("  ... and %d more revision(s)." % (len(revisions) - 12))
        lines.append("")
        lines.append("Revisions are reported, never interpreted - whether a sheet "
                     "SHOULD carry one is a question about your issue process.")

    lines.append("")
    lines.append("Nothing was changed.")
    return "\n".join(lines)


@server.tool()
def revit_rooms() -> str:
    """
    List the rooms, MEP spaces and areas in the open Revit model, and the two
    states that cost real time.

    Rooms are the architect's and SPACES are the engineer's. They are separate
    elements and they go out of step whenever a partition moves and nobody
    presses the button, so both are reported side by side and the difference
    is stated.

    UNPLACED and NOT ENCLOSED are counted separately because they look
    identical in a schedule - both give zero area - and they are completely
    different jobs to fix. An unplaced room sits nowhere in the model and still
    appears in every total; a not-enclosed one is placed and its boundary
    leaks, which one millimetre of gap will do.

    REDUNDANT rooms - two in one enclosure - are NOT checked. Revit reports
    those through its Warnings list, and matching a warning by its text would
    work in English and silently report zero in every other language. The
    answer says so, so a silence is not read as a clean bill.

    Areas are as Revit prints them, in the project's own units.

    It reads only. Nothing is placed, deleted or re-bounded - all three change
    somebody's area schedule, which on most jobs is a contractual document.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_rooms")
    session.close()
    _ended_by_reply("revit_rooms", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)
    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    rooms = reply.get("rooms", 0)
    spaces = reply.get("spaces", 0)
    areas = reply.get("areas", 0)

    if not rooms and not spaces and not areas:
        return ("%s has no rooms, no MEP spaces and no areas. Not a fault - "
                "plenty of models never get any - but any question about areas, "
                "occupancy or MEP loads has nothing here to answer from." % where)

    lines = ["%s: %d room(s), %d MEP space(s), %d area(s)."
             % (where, rooms, spaces, areas), ""]

    def state(label, total, unplaced, unbounded):
        if not total:
            return
        lines.append("  %-12s %4d total   %4d unplaced   %4d not enclosed"
                     % (label, total, unplaced, unbounded))

    state("rooms", rooms, reply.get("roomsUnplaced", 0),
          reply.get("roomsNotEnclosed", 0))
    state("MEP spaces", spaces, reply.get("spacesUnplaced", 0),
          reply.get("spacesNotEnclosed", 0))
    if areas:
        lines.append("  %-12s %4d total   %4d unplaced"
                     % ("areas", areas, reply.get("areasUnplaced", 0)))

    if rooms and spaces and rooms != spaces:
        lines.append("")
        lines.append("There are %d room(s) and %d space(s) - a difference of %d. Rooms "
                     "are the architect's and spaces the engineer's, and they drift "
                     "apart whenever a partition moves. That gap is the first thing to "
                     "look at." % (rooms, spaces, abs(rooms - spaces)))

    examples = reply.get("examples") or []
    if examples:
        lines.append("")
        for one in examples:
            lines.append("  %-10s %-28s %s"
                         % (one.get("number") or "-", one.get("name") or "",
                            one.get("state")))

    lines.append("")
    lines.append("REDUNDANT rooms are not checked - Revit reports those in its "
                 "Warnings list. Areas are as Revit prints them, in the project's "
                 "own units. Nothing was changed.")
    return "\n".join(lines)


@server.tool()
def revit_schedules() -> str:
    """
    List the schedules in the open Revit model and what governs each of them.

    A schedule is a FILTERED VIEW. Its category, its phase and its filters each
    remove rows silently, so "48 doors" off a door schedule is not the number
    of doors in the model - it is the number of that category, on that phase,
    that passed that filter.

    So every schedule is reported with how many filters it carries and how many
    elements the model holds in its category. The two numbers side by side are
    the answer; neither on its own is.

    It does NOT read the rows. Exporting a schedule's contents belongs to the
    export agent at PUBLISH risk, and is a different job from saying which
    schedules exist.

    It reads only. No field is added and no filter is changed - both change
    what everybody downstream is pricing from.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_schedules")
    session.close()
    _ended_by_reply("revit_schedules", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)
    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    schedules = reply.get("schedules") or []
    if not schedules:
        return ("%s has no schedules. Nothing is extracting data out of it yet, "
                "which is worth knowing before anybody asks for a quantity."
                % where)

    lines = ["%s: %d schedule(s), %d with filters, %d on no sheet."
             % (where, reply.get("scheduleCount", 0),
                reply.get("schedulesWithFilters", 0),
                reply.get("schedulesOnNoSheet", 0)), ""]

    for one in schedules[:30]:
        held = one.get("elementsInCategory", 0)
        marks = []
        if one.get("filters"):
            marks.append("%d filter(s)" % one["filters"])
        if one.get("materialTakeoff"):
            marks.append("takeoff")
        if not one.get("onASheet"):
            marks.append("not on a sheet")
        lines.append("  %-34s %-18s model holds %-8s %s"
                     % ((one.get("name") or "")[:34],
                        (one.get("category") or "no model category")[:18],
                        "{:,}".format(held) if held else "-",
                        ", ".join(marks)))
    if len(schedules) > 30:
        lines.append("  ... and %d more schedule(s)." % (len(schedules) - 30))

    lines.append("")
    lines.append("'model holds' is what the category contains BEFORE that schedule's "
                 "phase and filters have had their say. Where it differs from the "
                 "schedule's own row count, the difference is what is being left out.")
    lines.append("")
    lines.append("The rows themselves are not read here - that is the export agent's "
                 "job, at PUBLISH risk. Nothing was changed.")
    return "\n".join(lines)


@server.tool()
def revit_families() -> str:
    """
    List the families and types in the open Revit model, and how many of each
    are actually placed.

    TYPES and INSTANCES are reported separately, because "how many of these are
    there" has two answers in Revit and they differ by a lot.

    It reports three states that cost real money: a type placed NOWHERE (
    carried in the file for ever, and in every type selector somebody then
    picks the wrong one from), an IN-PLACE family (modelled into this project,
    reusable nowhere, has to be remade next job), and a family carrying many
    types with almost none placed.

    System families - walls, ducts, pipes, floors - are included and flagged
    rather than filtered out, so the totals agree with the Project Browser.

    NOTHING IS LOADED, and that is deliberate rather than incidental. A family
    carries its own materials and loading one overwrites the project's: six
    families once reset the pipe colour on a whole job silently, with no count
    moving and nothing warning.

    It reads only.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_families")
    session.close()
    _ended_by_reply("revit_families", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)
    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    families = reply.get("families") or []
    if not families:
        return "%s reports no families or types at all." % where

    lines = ["%s: %d family/families, %s type(s), %s placed instance(s)."
             % (where, reply.get("familyCount", 0),
                "{:,}".format(reply.get("typeCount", 0)),
                "{:,}".format(reply.get("placedInstances", 0))), ""]

    ordered = sorted(families, key=lambda row: -(row.get("typesNothingUses") or 0))
    for one in ordered[:25]:
        marks = []
        if one.get("inPlace"):
            marks.append("IN-PLACE")
        elif one.get("systemFamily"):
            marks.append("system")
        if one.get("typesNothingUses"):
            marks.append("%d type(s) unused" % one["typesNothingUses"])
        lines.append("  %-34s %-16s %3d type(s)  %8s placed  %s"
                     % ((one.get("name") or "")[:34],
                        (one.get("category") or "")[:16],
                        one.get("types", 0),
                        "{:,}".format(one.get("placed", 0)),
                        ", ".join(marks)))
    if len(ordered) > 25:
        lines.append("  ... and %d more family/families." % (len(ordered) - 25))

    findings = []
    if reply.get("typesNothingUses"):
        findings.append(
            "%s type(s) are placed nowhere. Each is carried in the file for ever "
            "and appears in every type selector."
            % "{:,}".format(reply["typesNothingUses"]))
    if reply.get("inPlaceFamilies"):
        findings.append(
            "%d family/families are IN-PLACE - reusable nowhere, cannot be "
            "swapped, have to be remade on the next job."
            % reply["inPlaceFamilies"])
    if reply.get("familiesWithManyTypesAndFewPlacements"):
        findings.append(
            "%d family/families carry %d or more types with %d or fewer placed. "
            "A rule of thumb, stated so you can disagree with it."
            % (reply["familiesWithManyTypesAndFewPlacements"],
               reply.get("manyTypesThreshold", 10),
               reply.get("fewPlacementsThreshold", 2)))

    if findings:
        lines.append("")
        for finding in findings:
            lines.append("  ! " + finding)

    lines.append("")
    lines.append("NOTHING WAS LOADED. A family carries its own materials and loading "
                 "one overwrites the project's, silently. Nothing was changed.")
    return "\n".join(lines)


def _export_blank_line(reply):
    """The "would print blank" line of revit_export_check, honest about what counted.

    An add-in built before 2026-09-27 counts a sheet blank when it has no
    VIEWS, so a sheet carrying only a schedule is among its number
    (FRAGMENT-ISSUES 5b-239). One that counts schedules says so with
    `schedulesCounted`. This server and the add-in go live at different
    restarts, so the one cannot assume the other.
    """
    blank = reply.get("sheetsThatWouldPrintBlank", 0)
    line = "  would print blank         %d" % blank
    if blank and not reply.get("schedulesCounted"):
        line += "   <-- views only; schedules not counted by this add-in"
    return line


def _export_blank_caveat(reply):
    """What to say when an older add-in's blank count may hold schedule sheets, or None."""
    blank = reply.get("sheetsThatWouldPrintBlank", 0)
    if not blank or reply.get("schedulesCounted"):
        return None
    return ("The Heron add-in in this Revit is older than this check and counted "
            "only VIEWS, so a sheet carrying only a schedule is among the %d it "
            "calls blank. Restart Revit after the add-in is updated for the real "
            "count." % blank)


@server.tool()
def revit_export_check() -> str:
    """
    Say whether an export of the open Revit model would be worth sending -
    WITHOUT exporting anything.

    It looks for what has turned up in issued sets and cannot be seen before
    export: nothing to export at all, sheets that would print BLANK (no view
    and no schedule; the titleblock's own revision schedule is not counted),
    links NOT LOADED (they export as nothing), and rooms with no area feeding
    area schedules. It reports and does not judge - a coordination model with
    links unloaded on purpose is a legitimate thing to export.

    NOTHING LEAVES THE MODEL: no file is written, nothing printed or sent. The
    export itself is a separate job at PUBLISH risk.
    """
    # Trimmed 2026-10-17 (docs/04 s8.8). Once a file has left there is no undo:
    # the recipient has it, and on a real job somebody may already be building
    # from it - which is why this answers beforehand the question nothing in
    # Revit answers until the file is written. The export itself needs a
    # destination, an overwrite decision and a person who meant it.
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("check_export")
    session.close()
    _ended_by_reply("revit_export_check", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)
    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    lines = ["%s - export readiness." % where, ""]
    lines.append("  sheets                    %d" % reply.get("sheets", 0))
    if reply.get("placeholderSheets"):
        lines.append("  of which placeholders     %d" % reply["placeholderSheets"])
    lines.append(_export_blank_line(reply))
    lines.append("  links not loaded          %d of %d"
                 % (reply.get("linksNotLoaded", 0), reply.get("links", 0)))
    lines.append("  rooms with no area        %d of %d"
                 % (reply.get("roomsWithNoArea", 0), reply.get("rooms", 0)))

    lines.append("")
    if reply.get("worthSending"):
        lines.append("Nothing here says an export would be wrong.")
    else:
        lines.append("There is at least one thing to look at before sending this.")

    caveat = _export_blank_caveat(reply)
    if caveat:
        lines.append("")
        lines.append(caveat)

    reads = reply.get("reads")
    if reads:
        lines.append("")
        lines.append(reads)
    return "\n".join(lines)


@server.tool()
def revit_imports() -> str:
    """
    List what has been brought into the open Revit model from outside, with
    LINKED and IMPORTED told apart.

    That distinction is the whole point. A LINKED CAD file stays outside the
    model and comes out cleanly. An IMPORTED one is copied INTO the model and
    never leaves: its layers, line patterns, text styles and fonts are in the
    project permanently and appear in every dialog from then on, and DELETING
    THE IMPORT DOES NOT REMOVE THEM.

    An imported DWG and a linked one look identical in the drawing area. Only
    Manage Links and the Import category tell them apart, and nobody opens
    those until the file is already slow.

    The count is of UNEXPLODED imports only, and says so. An exploded import is
    loose model lines and text, indistinguishable from work somebody drew,
    while its line patterns and text styles remain - nothing can count those
    after the fact.

    It reads only. Nothing is imported, linked, reloaded or removed.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_imports")
    session.close()
    _ended_by_reply("revit_imports", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)
    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    imported = reply.get("imported") or []
    linked = reply.get("linkedCad") or []

    lines = ["%s: %d imported CAD, %d linked CAD, %d Revit link(s)."
             % (where, reply.get("importedCount", 0),
                reply.get("linkedCadCount", 0), reply.get("revitLinks", 0)), ""]

    if imported:
        lines.append("  IMPORTED - copied in, and here permanently:")
        for one in imported[:15]:
            view = one.get("view")
            lines.append("    %-40s %s"
                         % ((one.get("name") or "")[:40],
                            ("only in view %r" % view) if view else "model-wide"))
        if len(imported) > 15:
            lines.append("    ... and %d more." % (len(imported) - 15))

    if linked:
        if imported:
            lines.append("")
        lines.append("  LINKED - outside the model, reloadable, removable:")
        for one in linked[:15]:
            lines.append("    %s" % (one.get("name") or ""))
        if len(linked) > 15:
            lines.append("    ... and %d more." % (len(linked) - 15))

    reads = reply.get("reads")
    if reads:
        lines.append("")
        lines.append(reads)
    return "\n".join(lines)


@server.tool()
def revit_annotation() -> str:
    """
    List the dimensions, tags, text and keynotes in the open Revit model, and
    find the ones that lie.

    The thing this exists for is an OVERRIDDEN DIMENSION - one typed over with
    text instead of reporting what it measures. On the drawing it looks exactly
    like a real dimension. It says 2400 on a wall that is 2100, it survives
    every model change because nothing recomputes it, it is checked by nobody
    because it looks correct, and it gets built. Nothing in Revit lists them.

    It also finds tags that have lost what they were tagging. Revit warns once,
    at the moment of deletion, and never again - so the warning gets dismissed
    and the empty tag stays on the sheet.

    Values are Revit's own formatted strings, taken as they are. Nothing here
    parses a dimension back into a number or compares it against a measurement.

    It reads only. No dimension is created, no override cleared and no tag
    deleted - all three change a drawing somebody may already have checked.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_annotation")
    session.close()
    _ended_by_reply("revit_annotation", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)
    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    lines = ["%s: %s dimension(s), %s tag(s), %s text note(s), %s keynote(s)."
             % (where,
                "{:,}".format(reply.get("dimensions", 0)),
                "{:,}".format(reply.get("tags", 0)),
                "{:,}".format(reply.get("textNotes", 0)),
                "{:,}".format(reply.get("keynotes", 0))), ""]

    overridden = reply.get("dimensionsOverridden", 0)
    if overridden:
        lines.append("  ! %s dimension(s) are OVERRIDDEN - typed over, not measured."
                     % "{:,}".format(overridden))
        for one in reply.get("overriddenExamples") or []:
            lines.append("      shows %-22s in view %s"
                         % (repr(one.get("shows")), one.get("view") or "(not in a view)"))
        lines.append("")

    orphans = reply.get("tagsWithNoHost", 0)
    if orphans:
        lines.append("  ! %s tag(s) have lost what they were tagging."
                     % "{:,}".format(orphans))
        lines.append("")

    reads = reply.get("reads")
    if reads:
        lines.append(reads)
    return "\n".join(lines)


def _systems_answer(reply, where):
    """The words for one `list_systems` reply.

    Lifted out of revit_systems so it can be read without the MCP SDK -
    tests/test_systems_answer.py checks the wording against what the add-in
    actually counts, which is where it went wrong before.

    UNTIL 2026-09-22 a system's count was its TERMINALS ONLY, and every duct,
    pipe and fitting was reported on no system. The add-in now sends the run
    (`network`) and the terminals and base equipment (`components`) apart, and
    both are printed, so each can be held against Revit on its own.
    """
    systems = reply.get("systems") or []
    loose = reply.get("connectedToNothing") or []
    examined = reply.get("mepElementsExamined", 0)

    if not systems and not examined:
        return ("%s has no duct or pipe systems and no MEP elements at all. "
                "That is a statement about this model, not an empty answer — "
                "an architectural or structural file has neither." % where)

    lines = ["%s: %d system(s), %s MEP element(s) examined."
             % (where, len(systems), "{:,}".format(examined)), ""]

    # AN ADD-IN OLDER THAN THE FIX SENDS NO `network`, and what it counts is
    # terminals only. Printing that zero as if it were the answer IS the
    # defect, so an old add-in is named rather than believed.
    split = bool(systems) and all("network" in s for s in systems)

    for system in systems:
        count = "%s element(s)" % "{:,}".format(system.get("elements", 0))
        if split:
            count += " — %s in the run, %s terminal(s) and equipment" % (
                "{:,}".format(system.get("network", 0)),
                "{:,}".format(system.get("components", 0)))
        lines.append("  %-4s  %-30s %s%s"
                     % (system.get("kind") or "?",
                        system.get("name"),
                        count,
                        "" if not system.get("systemType")
                        else "   [%s]" % system.get("systemType")))

    if systems and not split:
        lines.append("")
        lines.append("THIS REVIT IS RUNNING A HERON ADD-IN OLDER THAN 2026-09-22. Each count "
                     "above is that system's TERMINALS ONLY, and every duct, pipe and fitting "
                     "reads as on no system below. Deploy the current add-in and restart "
                     "Revit before trusting either number.")

    off_system = reply.get("onNoSystem", 0)
    if off_system:
        lines.append("")
        lines.append("%s MEP element(s) are on no duct or pipe system — no system holds "
                     "them in its run, among its terminals or as its base equipment. "
                     "Electrical circuits are not read here, so anything electrical is "
                     "counted in this number." % "{:,}".format(off_system))
    elif split and examined:
        lines.append("")
        lines.append("Every one of the %s MEP element(s) examined is held by a duct or "
                     "pipe system." % "{:,}".format(examined))

    open_ended = reply.get("withAnOpenConnector", 0)
    if open_ended:
        lines.append("")
        lines.append("%s element(s) have at least one open connector. That is NORMAL — "
                     "the end of every run is one — and is a count rather than a list "
                     "for that reason." % "{:,}".format(open_ended))

    if loose:
        lines.append("")
        lines.append("%d element(s) are CONNECTED TO NOTHING — no joined connector in any "
                     "direction, so nothing is attached to them at all. Whether each one "
                     "is on a system is a separate count, above:" % len(loose))
        for one in loose[:20]:
            lines.append("    %-22s %-26s %s"
                         % (one.get("category") or "?",
                            one.get("name"),
                            one.get("level") or "no level"))
        if len(loose) > 20:
            lines.append("    ... and %d more." % (len(loose) - 20))
    else:
        lines.append("")
        lines.append("Every MEP element that reports a connector has at least one joined. "
                     "Nothing is floating unattached.")

    return "\n".join(lines)


@server.tool()
def revit_systems() -> str:
    """
    List the duct and pipe systems in the open Revit model - each with its
    run (ducts or pipes and their fittings) and its terminals and equipment -
    the MEP elements no system holds, and the ones connected to nothing.

    Use whenever the user mentions a system, a riser, flow, sizing, a system
    browser, or says that something "is not on a system" or that a schedule or
    a calculation is coming up short. Also use it before trusting any MEP
    total: a duct that LOOKS joined on screen and is not connected carries no
    flow, is not on the system it looks part of, and is missing from every
    number downstream without anything saying so.

    AN OPEN CONNECTOR IS NOT A FAULT and this does not report it as one — the
    end of every run is open, and a stub waiting for coordination is open on
    purpose. What it singles out is the element with NO joined connector in
    any direction — the group worth a person's eye. Whether an element is ON a
    system is counted separately, from what each system holds, and is never
    inferred from whether it is joined.

    It reads only. Nothing is connected, renamed or put on a system.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_systems")
    session.close()
    _ended_by_reply("revit_systems", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    return _systems_answer(reply, where)


@server.tool()
def revit_parameters(category: str = "ducts", parameter: str = "") -> str:
    """
    Read the parameters of every element of one category in the open Revit
    model - either which parameters are filled in across the whole category,
    or what one named parameter says element by element.

    Use when the user asks whether something is filled in, complete, missing,
    empty or blank; before a schedule, an IFC export, a COBie drop or a QA
    hand-over; or whenever they name a parameter and want to know what it
    says. Leave `parameter` empty for the coverage answer - "which of these
    are filled in" - and name one for the values.

    IT READS THE TYPE AS WELL AS THE INSTANCE. Fire Rating, Assembly Code and
    most classification data sit on the TYPE, and a check that reads only
    instances reports a confident, formatted zero on data that is actually
    there. Every row says which one answered.

    A VALUE COMES BACK IN THE PROJECT'S UNITS, formatted the way Revit itself
    would print it in a schedule. Revit holds lengths internally in decimal
    feet whatever the project is set to, so a raw number never appears on its
    own - where one is given it is labelled unconverted.

    It reads only. No parameter is written, and nothing is created or deleted.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    # THE PIN TRAVELS WITH THE REQUEST, for the reason revit_select_by_category
    # gives at length: a guard checked on the reply is checked too late. This
    # one writes nothing, so a late check would cost no model - but it would
    # still hand back a completeness report for the wrong building, and a
    # report from the wrong model reads exactly like one from the right model.
    reply = session.request("read_parameters",
                            op_args={"category": category,
                                     "parameter": parameter or "",
                                     "expectProject": pinned.project_information_id or ""})
    session.close()
    _ended_by_reply("revit_parameters", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)
    kind = reply.get("category")

    if reply.get("parameter"):
        return _parameter_values(reply, where, kind)
    return _parameter_coverage(reply, where, kind)


def _parameter_coverage(reply, where, kind):
    """Which parameters these elements carry, and how many are filled in."""
    rows = reply.get("parameters") or []
    elements = reply.get("elements", 0)

    if not elements:
        return ("%s has no %s at all, so there are no parameters to report. "
                "That is a statement about this model rather than an empty "
                "answer." % (where, kind))

    lines = ["%s: %s %s, %d distinct parameter(s)."
             % (where, "{:,}".format(elements), kind,
                reply.get("distinctParameters", 0)),
             "",
             "  %-34s %-9s %8s %8s %8s %8s"
             % ("PARAMETER", "WHERE", "ON", "FILLED", "NOT SET", "BLANK")]

    for row in rows:
        lines.append("  %-34s %-9s %8s %8s %8s %8s%s"
                     % (_clip(row.get("name"), 34),
                        row.get("where"),
                        "{:,}".format(row.get("onElements", 0)),
                        "{:,}".format(row.get("withAValue", 0)),
                        "{:,}".format(row.get("noValue", 0)),
                        "{:,}".format(row.get("blank", 0)),
                        "" if row.get("sameNameOnOneElement", 1) <= 1
                        else "   <- NAME USED TWICE"))

    left = reply.get("notListed", 0)
    if left:
        # WHICH KIND WENT. The add-in lists type rows first precisely so a
        # truncation cannot remove a whole kind, and saying which was cut
        # is what lets a reader tell a long answer from a misleading one.
        kinds = []
        if reply.get("notListedType"):
            kinds.append("%s on the type" % "{:,}".format(reply["notListedType"]))
        if reply.get("notListedInstance"):
            kinds.append("%s on the instance"
                         % "{:,}".format(reply["notListedInstance"]))
        lines.append("  ... and %s more parameter(s)%s."
                     % ("{:,}".format(left),
                        "" if not kinds else " — " + ", ".join(kinds)))

    lines.append("")
    lines.append("WHERE says instance or type. A `type` row counts TYPES, not "
                 "elements - 4 against 900 doors means four door types.")
    lines.append("NOT SET and BLANK both print blank in a schedule and are not "
                 "the same fault: NOT SET is data nobody entered, BLANK is "
                 "usually a space somebody typed.")

    # COUNT THE NAMES, NOT THE ROWS. A name ambiguous on the instance AND on
    # the type is two rows and one name - the add-in's own `reads` note says
    # "the same name can appear twice, once as each" - so counting rows said
    # "2 parameter name(s)" beside a list of one, and the reader goes looking
    # for a second name that was never there. And a list that stops at five
    # says so, for the reason the notListed block above states out loud: a
    # truncated answer that does not say what it dropped is the one a reader
    # trusts by mistake.
    clashes = sorted(set(row.get("name") for row in rows
                         if row.get("sameNameOnOneElement", 1) > 1))
    if clashes:
        rest = len(clashes) - len(clashes[:5])
        lines.append("")
        lines.append("%d parameter name(s) answer to TWO different parameters on a "
                     "single element here — %s%s. Asking by those names would hit "
                     "whichever Revit returned first, so they are not safe to ask "
                     "by on this model."
                     % (len(clashes), ", ".join(clashes[:5]),
                        "" if not rest else ", and %d more" % rest))

    return "\n".join(lines)


def _parameter_values(reply, where, kind):
    """What one named parameter says, element by element."""
    name = reply.get("parameter")
    examined = reply.get("examined", 0)
    matched = reply.get("matched", 0)
    absent = reply.get("withoutTheParameter", 0)

    if not examined:
        return ("%s has no %s at all, so there is nothing to read \"%s\" from."
                % (where, kind, name))

    if not matched:
        return ("%s: none of the %s %s carry a parameter called \"%s\" — not on "
                "the instance and not on the type. That usually means a "
                "different family, or a project parameter that was never bound "
                "to this category, rather than data nobody filled in."
                % (where, "{:,}".format(examined), kind, name))

    lines = ["%s: \"%s\" on %s of %s %s."
             % (where, name, "{:,}".format(matched),
                "{:,}".format(examined), kind),
             "",
             "  %s filled in, %s not set, %s blank."
             % ("{:,}".format(reply.get("withAValue", 0)),
                "{:,}".format(reply.get("noValue", 0)),
                "{:,}".format(reply.get("blank", 0)))]

    if absent:
        lines.append("  %s do not carry it at all." % "{:,}".format(absent))

    lines.append("")
    rows = reply.get("elements") or []
    for row in rows[:30]:
        shown = row.get("matches") or [{}]
        value = shown[0].get("value")
        lines.append("  %-28s %-8s %-22s %s"
                     % (_clip(row.get("element"), 28),
                        row.get("where"),
                        _clip(row.get("level") or "no level", 22),
                        value if value else "(%s)" % row.get("state")))

    # THE REMAINDER IS WORKED OUT HERE AND NOT READ OUT OF THE REPLY.
    # There are TWO caps and they are different sizes: the add-in stops
    # at 500 rows and `notListed` describes only that, while this list
    # stops at 30. Printing the add-in's figure after this cap told a
    # reader "and 100 more" on an answer where 570 were unshown - a count
    # that is wrong in the direction that makes somebody stop looking.
    left = matched - min(len(rows), 30)
    if left > 0:
        lines.append("  ... and %s more." % "{:,}".format(left))

    ambiguous = reply.get("ambiguousElements", 0)
    if ambiguous:
        lines.append("")
        lines.append("%s element(s) carry MORE THAN ONE parameter called \"%s\" — a "
                     "built-in and a shared one, say. Writing by this name would "
                     "hit whichever Revit returned first, so it is not safe to "
                     "write by here." % ("{:,}".format(ambiguous), name))

    lines.append("")
    lines.append("Values are in the project's units, formatted the way Revit "
                 "prints them. WHERE says whether the instance or the type "
                 "answered.")

    return "\n".join(lines)


def _clip(text, width):
    """A column that stays a column, however long a parameter name is."""
    text = "" if text is None else str(text)
    return text if len(text) <= width else text[:width - 1] + "\u2026"


@server.tool()
def revit_groups(category: str = "") -> str:
    """
    List the groups and assemblies in the open Revit model, or - with a
    category - show which of those elements sit inside one and how many other
    placements an edit would reach.

    ASK THIS BEFORE ANY CHANGE to elements that might be grouped. Editing one
    member of a group edits it in every place that group is put, which is the
    point of groups and a nasty surprise when you did not know the element was
    in one. Revit raises NO error for this: a move of a group member returns
    cleanly and shifts nothing at all.

    Use when the user asks about groups, assemblies, why an edit did not take,
    why something changed in more than one place, or before approving a change
    to a category on a model that uses groups. Leave `category` empty for the
    inventory of what groups exist; name one for the pre-flight.

    A GROUP MEMBER IS NOT PINNED, so `pinned` is reported separately - two
    different reasons an edit will not land, fixed two different ways.

    It reads only. Nothing is grouped, ungrouped or edited.
    """
    try:
        session = binding.resolve()
    except NotBound as unbound:
        return _ended("refused", str(unbound))

    reply = session.request("list_groups",
                            op_args={"category": category or "",
                                     "expectProject": pinned.project_information_id or ""})
    session.close()
    _ended_by_reply("revit_groups", reply)

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        return reply.get("message") or reply.get("error") or "The request was refused."

    wrong_model = pinned.check(reply)
    if wrong_model is not None:
        return _ended("refused", wrong_model)

    where = "%s (Revit %s, session %s)" % (reply.get("document"),
                                           session.revit_version, session.pid)

    if reply.get("category"):
        return _groups_in_category(reply, where)
    return _groups_inventory(reply, where)


def _groups_inventory(reply, where):
    """What groups and assemblies this model has."""
    rows = reply.get("groupTypes") or []
    assemblies = reply.get("assemblies") or []
    total = reply.get("groupTypeCount", 0)

    if not total and not assemblies:
        return ("%s has no groups and no assemblies. That is a statement about "
                "this model, not an empty answer \u2014 and it means an edit to "
                "any element here reaches exactly one place." % where)

    lines = ["%s: %d group type(s), %d assembly(ies)."
             % (where, total, reply.get("assemblyCount", 0)),
             "",
             "  %-38s %-16s %10s %9s" % ("GROUP TYPE", "KIND", "PLACEMENTS", "MEMBERS")]

    for row in rows:
        members = row.get("members")
        lines.append("  %-38s %-16s %10s %9s"
                     % (_clip(row.get("name"), 38),
                        _clip(row.get("kind") or "?", 16),
                        "{:,}".format(row.get("placements", 0)),
                        "-" if members is None else "{:,}".format(members)))

    left = reply.get("notListed", 0)
    if left:
        lines.append("  ... and %s more." % "{:,}".format(left))

    if assemblies:
        lines.append("")
        lines.append("  ASSEMBLIES")
        for one in assemblies[:20]:
            lines.append("  %-38s %s member(s)"
                         % (_clip(one.get("name"), 38),
                            "{:,}".format(one.get("members", 0))))
        # Off the TOTAL, not off this already-capped list - the add-in
        # caps `assemblies` at 500 and this loop caps it again at 20.
        more = reply.get("assemblyCount", len(assemblies)) - min(len(assemblies), 20)
        if more > 0:
            lines.append("  ... and %s more." % "{:,}".format(more))

    lines.append("")
    lines.append("PLACEMENTS is the number that matters: edit one member of a "
                 "group placed 12 times and you have edited 12 places.")

    unplaced = reply.get("unplacedGroupTypes", 0)
    if unplaced:
        lines.append("%d group definition(s) have nothing placed. Not a fault \u2014 "
                     "that is what a purge would remove." % unplaced)

    if assemblies:
        lines.append("Assemblies are listed and not explained: whether an edit "
                     "inside one travels to another of the same type has not "
                     "been checked against a real model.")

    return "\n".join(lines)


def _groups_in_category(reply, where):
    """Which of these elements an edit would multiply."""
    kind = reply.get("category")
    examined = reply.get("examined", 0)
    grouped = reply.get("inAGroup", 0)
    assembled = reply.get("inAnAssembly", 0)
    worst = reply.get("mostPlacements", 0)

    if not examined:
        return ("%s has no %s at all, so there is nothing to check for groups. "
                "That is a statement about this model rather than an empty "
                "answer." % (where, kind))

    if not grouped and not assembled:
        return ("%s: none of the %s %s are in a group or an assembly. An edit "
                "to any of them reaches exactly one place, which is what you "
                "expected \u2014 and it is worth having been told rather than "
                "assumed."
                % (where, "{:,}".format(examined), kind))

    lines = ["%s: %s of %s %s are inside a group."
             % (where, "{:,}".format(grouped), "{:,}".format(examined), kind)]

    if worst > 1:
        lines.append("")
        lines.append("!! THE WORST CASE IS %s PLACEMENTS. Editing that element "
                     "changes %s places in this model, and Revit will not warn "
                     "you \u2014 a move of a group member returns cleanly and "
                     "shifts nothing at all."
                     % ("{:,}".format(worst), "{:,}".format(worst)))

    lines.append("")
    lines.append("  %-30s %-20s %10s %7s"
                 % ("ELEMENT", "GROUP", "PLACEMENTS", "PINNED"))

    for row in (reply.get("elements") or [])[:30]:
        chain = row.get("chain") or []
        first = chain[0] if chain else {}
        name = first.get("groupType") or first.get("group") or row.get("assembly") or "-"
        lines.append("  %-30s %-20s %10s %7s%s"
                     % (_clip(row.get("element"), 30),
                        _clip(name, 20),
                        "{:,}".format(row.get("placementsOfItsGroup", 0)),
                        "yes" if row.get("pinned") else "no",
                        "   NESTED" if row.get("nested") else ""))

    # THE REMAINDER COMES OFF `matched`, NOT off grouped + assembled. An
    # element can be in a group AND an assembly, so adding the two counts
    # it twice - and there are two caps besides: the add-in stops at 500
    # rows and this list stops at 30. The add-in reports `matched` as the
    # number of rows it would have produced, which is the only figure both
    # caps can be subtracted from honestly.
    shown = min(len(reply.get("elements") or []), 30)
    left = reply.get("matched", shown) - shown
    if left > 0:
        lines.append("  ... and %s more." % "{:,}".format(left))

    lines.append("")
    if reply.get("inNoGroup"):
        lines.append("%s are in no group at all \u2014 an edit reaches those once."
                     % "{:,}".format(reply["inNoGroup"]))

    if reply.get("pinned"):
        lines.append("%s are PINNED, which is a different problem: a group "
                     "member is not pinned, so unpinning will not free one and "
                     "ungrouping will not free the other."
                     % "{:,}".format(reply["pinned"]))

    if reply.get("nested"):
        lines.append("%s sit in NESTED groups. Each level's own placement count "
                     "is reported and they are NOT multiplied \u2014 whether a "
                     "nested group's count already includes the copies inside "
                     "its parent has not been checked against a real model, and "
                     "a wrong multiplier here would read exactly like a right "
                     "one." % "{:,}".format(reply["nested"]))

    return "\n".join(lines)
