# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-FRG-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.3.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

r"""
The Revit tool catalogue for a BIM modeller - generated from the cards.

    python tools/generate-tool-catalogue.py            write the pages
    python tools/generate-tool-catalogue.py --check    exit 1 if a page is out of date

WHAT IT WRITES
--------------
    docs/guide/revit-tool-catalogue.md        the front page: how a tool is
                                              reached, the ribbon as the add-in
                                              builds it, the job groups
    docs/guide/tool-catalogue/<job>.md        one page per job, every live tool
                                              in it

WHY IT IS COMMITTED WHEN fragment-catalog.html IS NOT
-----------------------------------------------------
generate-fragment-catalog.py draws the library for whoever is CHANGING Heron,
and is gitignored: re-running it is the only way to read it. A modeller reads
Heron on GitHub, never by running a script, so this page has to be in the
repository - and a committed page can go stale. `--check` is what stops it:
tests/test_tool_catalogue.py runs it, so a card whose words, status or proof
moved without the pages being written again fails that test, naming this
command. Nothing in these pages is typed by a person - every count, status,
release and phrase comes off the cards and the add-in's ribbon code at the
moment this runs. The owner asked for exactly that on 2026-11-22: "no count or
status is ever typed by hand".

WHAT EACH STATUS IS CALLED, AND WHERE THAT COMES FROM
-----------------------------------------------------
The same rules a chat meets, asked of the code that applies them, never
re-decided here:

  - a card is USABLE IN A CHAT when its status is PROVEN or PRODUCTION and its
    proof still matches its code (`Fragment.proof_is_stale`, the D-30
    fingerprint revit_change refuses on - heron_brain.proof_status);
  - a DRAFT card, or a PROVEN one whose proof went stale, that CHANGES the
    model is refused by revit_change until it is proved (D-117,
    heron_bridge_client.unproven_refusal);
  - a DRAFT card that only reads, or only changes what is SHOWN (select, zoom,
    isolate - EXECUTE and below), still runs: revit_read and revit_show run it
    and the answer says DRAFT (heron_mcp_server, revit_show's note on D-117).
    The page says so rather than calling it unusable, because that would be
    untrue;
  - DEPRECATED and ARCHIVED cards are retired: retrieval never offers them,
    so they are counted on the front page and not listed.

The releases are two different claims and are printed as two: BUILT FOR is the
card's `revit:` list - it compiles there - and PROVED ON is
heron_matrix.proven_releases, the releases a proof that still stands names.

NOTHING NAMES A MODEL. A proof's `model:` can be a real client's job; the page
gives the proof's date and Revit release only.

THE OUTPUT IS THE SAME ON EVERY MACHINE: sorted, "\n" line ends, no date and no
path, so `--check` on Windows and on Linux agree.

Exit 0 = written, or current. 1 = `--check` found a page out of date (it names
them). 2 = the library could not be read.
"""

import argparse
import io
import json
import os
import posixpath
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as HF                                   # noqa: E402
import heron_matrix as MATRIX                                 # noqa: E402

FRONT = os.path.join("docs", "guide", "revit-tool-catalogue.md")
PAGES = os.path.join("docs", "guide", "tool-catalogue")
MANIFEST = os.path.join("platform", "heron-products.json")

#: The add-ins whose ribbon code is read, by project folder under revit/.
RIBBON_SOURCES = (
    ("Heron.Revit.Addin", "HeronApplication.cs"),
    ("Heron.Tools", "HeronToolsApplication.cs"),
    ("Heron.Doc", "HeronDocApplication.cs"),
)

LIVE = ("PROVEN", "PRODUCTION")
RETIRED = ("DEPRECATED", "ARCHIVED")

#: The seven risk levels, lowest first - mcp/server/heron_runtime.RISK_ORDER,
#: which tests/test_tool_catalogue.py holds this to. What each needs from the
#: modeller, in the words of Heron's Settings window (D-106, D-166).
RISK_WORDS = {
    "READ":    ("Answers a question", None),
    "ANALYZE": ("Answers a question", None),
    "SUGGEST": ("Answers a question", None),
    "EXECUTE": ("Changes only what Revit shows - a selection, a zoom, a view", None),
    "MODIFY":  ("Changes the model", "Changes"),
    "PUBLISH": ("Changes the model and writes files outside it", "Changes and Publish"),
    "ADMIN":   ("Changes project-wide settings", "Changes and Admin"),
}
SHOW_CEILING = "EXECUTE"

#: The job groups, by the card's `domain:`. A domain missing here still gets a
#: page, named from its own word - a new domain never drops its tools.
JOBS = (
    ("revit.mep", "mep", "MEP - ducts, pipes, systems and equipment"),
    ("revit.views", "views", "Views and visibility"),
    ("revit.sheets", "sheets", "Sheets and title blocks"),
    ("revit.schedules", "schedules", "Schedules"),
    ("revit.annotation", "annotation", "Annotation - tags, dimensions and text"),
    ("revit.families", "families", "Families"),
    ("revit.elements", "elements", "Model elements and types"),
    ("revit.geometry", "geometry", "Geometry and placement"),
    ("revit.selection", "selection", "Finding and selecting"),
    ("revit.parameters", "parameters", "Parameters"),
    ("revit.rooms", "rooms", "Rooms, spaces and areas"),
    ("revit.datums", "datums", "Levels and grids"),
    ("revit.phases", "phases", "Phases"),
    ("revit.qa", "model-checks", "Model checks"),
    ("revit.reporting", "reports", "Reports"),
    ("revit.document", "project", "The project and its settings"),
    ("revit.export", "export", "Export and print"),
)

#: Words kept in capitals when a capability name is turned into a title.
ACRONYMS = {"MEP", "DWG", "DXF", "DGN", "PDF", "IFC", "HVAC", "CAD", "ID", "IDS",
            "BIM", "XYZ", "CSV", "QA", "BCF", "VG", "NWC", "FBX", "UV", "MCP",
            "RVT", "RFA", "UI", "BOQ", "FCU", "AHU", "VAV", "BQ", "2D", "3D"}

ELEMENT_LIST = re.compile(r"^(IList|ICollection|IEnumerable)<(Element|ElementId)>$")


# ---------------------------------------------------------------------------
# Reading the cards
# ---------------------------------------------------------------------------

def title_of(capability):
    """LIST_EXPORT_SETUPS -> "List export setups"."""
    words = [w for w in str(capability or "").split("_") if w]
    out = []
    for i, word in enumerate(words):
        if word in ACRONYMS:
            out.append(word)
        elif i == 0:
            out.append(word.capitalize())
        else:
            out.append(word.lower())
    return " ".join(out) or "(no capability)"


def anchor_of(capability):
    return str(capability or "").lower().replace("_", "-")


def words_of(name):
    """setupCount -> "setup count"; tag_at -> "tag at"."""
    text = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", str(name or ""))
    return text.replace("_", " ").lower().strip()


def one_line(text):
    return " ".join(str(text or "").split())


def paragraphs(text, card=None):
    """The card's `purpose:` as its own paragraphs, each on one line.

    A link a card writes is relative to the card's own folder; it is re-pointed
    from the page, which sits somewhere else (`card` is the card folder,
    relative to the checkout, "/" separated)."""
    parts = re.split(r"\n\s*\n", str(text or "").strip())
    out = [one_line(p) for p in parts if p.strip()]
    if card:
        out = [rebase_links(p, card) for p in out]
    return out


LINK = re.compile(r"\]\(([^)\s]+)\)")


def rebase_links(text, card):
    """Re-point every relative markdown link from `card`'s folder to PAGES."""
    def moved(match):
        target = match.group(1)
        if re.match(r"^([a-z]+:|#|/)", target):
            return match.group(0)
        path, _sep, frag = target.partition("#")
        real = posixpath.normpath(posixpath.join(card, path))
        new = posixpath.relpath(real, PAGES.replace(os.sep, "/"))
        return "](%s%s)" % (new, "#" + frag if frag else "")
    return LINK.sub(moved, text)


def plain_type(need):
    """What to type for one need, as a modeller reads it."""
    declared = str(need.get("type") or "")
    hint = HF.how_to_type(declared, need.get("name"))
    base = {
        "string": "text",
        "int": "a whole number",
        "double": "a number",
        "bool": "yes or no",
        "View": "a view, by name",
        "Level": "a level, by name",
        "XYZ": "a point",
        "IList<string>": "a list",
        "IList<XYZ>": "points",
        "IList<double>": "numbers",
        "IList<int>": "whole numbers",
    }.get(declared, "")
    if base and hint:
        return "%s - %s" % (base, one_line(hint))
    return base or one_line(hint) or "a value"


def needs_of(frag):
    """(you_say, works_on, earlier): what the modeller types, what must be
    selected, and what an earlier step in the same request hands over."""
    you_say, works_on, earlier = [], [], []
    for need in frag.needs():
        if not isinstance(need, dict):
            continue
        source = HF.need_source(need)
        if source == "ambient":
            continue
        name = need.get("name")
        if source == "request":
            absent = HF.need_absent_text(need)
            optional = absent is not None or str(need.get("optional")).strip().lower() == "true"
            line = "**%s** - %s" % (words_of(name), plain_type(need))
            if optional:
                line += "; optional" + (", left out means *%s*" % absent if absent else "")
            you_say.append(line)
        elif ELEMENT_LIST.match(str(need.get("type") or "")):
            works_on.append(words_of(name))
        else:
            earlier.append(words_of(name))
    return you_say, works_on, earlier


def results_of(frag):
    out = []
    for given in frag.provides():
        if isinstance(given, dict) and HF.provide_role(given) == "result":
            out.append(words_of(given.get("name")))
    return out


def standing(frag):
    """(word, usable, proved_on, proof) for one live card.

    word is PROVEN, STALE or DRAFT; usable says whether a chat runs it."""
    risk = str(frag.data.get("risk") or "").upper()
    shows_only = risk in RISK_WORDS and \
        list(RISK_WORDS).index(risk) <= list(RISK_WORDS).index(SHOW_CEILING)
    proof = frag.proof if isinstance(frag.proof, dict) else {}
    if frag.status in LIVE and proof and not frag.proof_is_stale():
        found, _stale = MATRIX.proven_releases(frag)
        return "PROVEN", True, sorted(found), proof
    word = "STALE" if frag.status in LIVE else "DRAFT"
    return word, shows_only, [], proof


def entry_of(frag):
    word, usable, proved_on, proof = standing(frag)
    you_say, works_on, earlier = needs_of(frag)
    risk = str(frag.data.get("risk") or "").upper()
    return {
        "capability": str(frag.data.get("capability") or ""),
        "title": title_of(frag.data.get("capability")),
        "anchor": anchor_of(frag.data.get("capability")),
        "identity": one_line(frag.semantic_identity),
        "purpose": paragraphs(frag.data.get("purpose"),
                              "brain/fragments/%s" % os.path.basename(str(frag.folder))
                              if getattr(frag, "folder", None) else None),
        "risk": risk,
        "word": word,
        "usable": usable,
        "built_for": sorted(str(r) for r in (frag.supported or [])),
        "proved_on": proved_on,
        "proof_date": str(proof.get("date") or "") if word == "PROVEN" else "",
        "held": one_line(frag.data.get("proof-held")),
        "phrases": [one_line(u) for u in frag.utterances() if one_line(u)][:3],
        "you_say": you_say,
        "works_on": works_on,
        "earlier": earlier,
        "results": results_of(frag),
        "domain": str(frag.data.get("domain") or ""),
    }


def job_of(domain):
    for key, slug, name in JOBS:
        if key == domain:
            return slug, name
    word = domain.split(".")[-1] if domain else "other"
    return re.sub(r"[^a-z0-9]+", "-", word.lower()).strip("-") or "other", \
        word.replace("-", " ").capitalize() or "Other"


def collect(fragments):
    """{slug: (name, [entries sorted by title])}, and the retired count."""
    groups, retired = {}, 0
    for frag in fragments.values():
        if frag.status in RETIRED:
            retired += 1
            continue
        entry = entry_of(frag)
        slug, name = job_of(entry["domain"])
        groups.setdefault(slug, (name, []))[1].append(entry)
    for slug in groups:
        groups[slug][1].sort(key=lambda e: (e["title"].lower(), e["capability"]))
    return groups, retired


# ---------------------------------------------------------------------------
# Reading the ribbon out of the add-ins' own code
# ---------------------------------------------------------------------------

def product_states(root):
    """{assembly file name: (product name, state)} from the manifest."""
    with io.open(os.path.join(root, MANIFEST), encoding="utf-8-sig") as fh:
        data = json.load(fh)
    found = {}
    for product in data.get("products", []):
        if product.get("assembly"):
            found[product["assembly"]] = (product.get("name") or product["id"],
                                          product.get("state") or "")
    return found


def ribbon_of(path):
    """One add-in's ribbon as its code builds it:
    {"tab", "panel", "items": [{"label", "tip", "menu": [{"label","tip"}]}]}."""
    with io.open(path, encoding="utf-8-sig") as fh:
        text = fh.read()
    const = dict(re.findall(r'const string (\w+) = "([^"]*)";', text))
    buttons = {}
    for var, label in re.findall(
            r'var (\w+) = new PushButtonData\(\s*"[^"]*",\s*"([^"]*)"', text):
        tip = re.search(r'\b%s\.ToolTip = "([^"]*)";' % re.escape(var), text)
        buttons[var] = {"label": label.replace("\\n", " "),
                        "tip": tip.group(1) if tip else ""}
    splits = {}
    for var, label in re.findall(
            r'var (\w+) = \w+\.AddItem\(\s*new SplitButtonData\("[^"]*",\s*"([^"]*)"\)', text):
        splits[var] = {"label": label, "tip": "", "menu": []}
    items, seen = [], set()
    for match in re.finditer(r'(\w+)\.(AddItem|AddPushButton)\((\w+)\)', text):
        owner, call, var = match.groups()
        if var not in buttons:
            continue
        if call == "AddPushButton" and owner in splits:
            if owner not in seen:
                items.append(splits[owner])
                seen.add(owner)
            splits[owner]["menu"].append(dict(buttons[var]))
        elif call == "AddItem":
            items.append(dict(buttons[var], menu=[]))
    return {"tab": const.get("TabName", ""), "panel": const.get("PanelName", ""),
            "items": items}


def ribbons(root):
    states = product_states(root)
    out = []
    for folder, source in RIBBON_SOURCES:
        path = os.path.join(root, "revit", folder, source)
        if not os.path.isfile(path):
            continue
        name, state = states.get(folder + ".dll", (folder, "not in the manifest"))
        out.append(dict(ribbon_of(path), product=name, state=state,
                        source="revit/%s/%s" % (folder, source)))
    return out


# ---------------------------------------------------------------------------
# Writing the pages
# ---------------------------------------------------------------------------

def releases_text(values):
    return ", ".join(values) if values else "none"


def status_line(e):
    if e["word"] == "PROVEN":
        when = " on %s" % e["proof_date"] if e["proof_date"] else ""
        where = (" (Revit %s)" % ", ".join(e["proved_on"])) if e["proved_on"] else ""
        return "**Usable in a chat.** Proved on a real model%s%s." % (when, where)
    if e["usable"]:
        why = "its proof no longer matches its code" if e["word"] == "STALE" \
            else "it has not been proved on a real model yet"
        return ("**%s - runs in a chat, and its answer says so.** It changes nothing in "
                "the model, so it is not refused, but %s." % (e["word"], why))
    lead = "DRAFT" if e["word"] == "DRAFT" else "PROOF OUT OF DATE"
    line = "**%s - not yet usable in a chat - refused until proved (D-117).**" % lead
    if e["held"]:
        line += " Held on purpose: %s" % e["held"]
    return line


def steps_of(e):
    what, switch = RISK_WORDS.get(e["risk"], ("Does something in Revit", "Changes"))
    steps = ["Open the model in Revit and connect Heron: press **Heron** on the Heron "
             "tab, so its heron turns orange."]
    if e["works_on"]:
        steps.append("Select what it works on in Revit (%s) - or name it in the same "
                     "request, and Heron finds it first." % ", ".join(e["works_on"]))
    if switch:
        steps.append("Turn **%s** on in Heron's **Settings** (Heron tab)." % switch)
    ask = "In a Claude chat, ask in your own words"
    if e["phrases"]:
        ask += ", for example *\"%s\"*" % e["phrases"][0]
    if e["you_say"]:
        ask += ". Say the values under **You say** in the same sentence"
    steps.append(ask + ".")
    if switch:
        steps.append("Heron makes the change as **one entry in Revit's Undo list**. With "
                     "**Ask to Keep** on, Revit asks **Keep or Discard** first; Ctrl+Z "
                     "takes it back.")
    elif e["risk"] == SHOW_CEILING:
        steps.append("Revit shows the result. Nothing in the model changes.")
    else:
        steps.append("Heron answers in the chat. Nothing in the model changes.")
    return steps


def render_entry(e):
    out = ['<a id="%s"></a>' % e["anchor"], "", "### %s" % e["title"], ""]
    out.append(status_line(e))
    out.append("")
    if e["identity"]:
        out.append("**What it does:** %s." % e["identity"].rstrip("."))
        out.append("")
    for para in e["purpose"]:
        out.append(para)
        out.append("")
    what, _switch = RISK_WORDS.get(e["risk"], ("Does something in Revit", None))
    out.append("| | |")
    out.append("|---|---|")
    out.append("| **Kind** | %s (`%s`) |" % (what, e["risk"] or "risk not declared"))
    out.append("| **Reached by** | asking in a Claude chat |")
    if e["works_on"]:
        out.append("| **Works on** | what is selected in Revit: %s |" % ", ".join(e["works_on"]))
    if e["earlier"]:
        out.append("| **Handed on by an earlier step** | %s |" % ", ".join(e["earlier"]))
    if e["you_say"]:
        out.append("| **You say** | %s |" % "<br>".join(s.replace("|", "/") for s in e["you_say"]))
    if e["results"]:
        out.append("| **You get** | %s |" % ", ".join(e["results"]))
    out.append("| **Built for Revit** | %s |" % releases_text(e["built_for"]))
    out.append("| **Proved on Revit** | %s |" % releases_text(e["proved_on"]))
    out.append("| **Capability** | `%s` |" % e["capability"])
    out.append("")
    if e["usable"]:
        out.append("**How to use it**")
        out.append("")
        for i, step in enumerate(steps_of(e), 1):
            out.append("%d. %s" % (i, step))
        out.append("")
        if e["phrases"][1:]:
            out.append("Other ways to ask: %s"
                       % " · ".join("*\"%s\"*" % p for p in e["phrases"][1:]))
            out.append("")
    elif e["phrases"]:
        # Refused today, so no steps - a list of them would be a way to try it.
        # The words are still shown: they are what it will answer once proved.
        out.append("Once it is proved, you will ask: %s"
                   % " · ".join("*\"%s\"*" % p for p in e["phrases"]))
        out.append("")
    return out


def counts(entries):
    proven = sum(1 for e in entries if e["word"] == "PROVEN")
    runs_draft = sum(1 for e in entries if e["word"] != "PROVEN" and e["usable"])
    refused = sum(1 for e in entries if not e["usable"])
    return proven, runs_draft, refused


def render_group(slug, name, entries):
    proven, runs_draft, refused = counts(entries)
    out = [
        "[Heron AI](../../../README.md) › [Documentation](../../README.md) › "
        "[Revit tool catalogue](../revit-tool-catalogue.md) › **%s**" % name,
        "",
        "# %s" % name,
        "",
        "<!-- GENERATED by tools/generate-tool-catalogue.py from the cards in "
        "brain/fragments. Do not edit: run the tool. -->",
        "",
        "%d tools: **%d usable in a chat**, %d not proved yet that still run because "
        "they change nothing, and %d refused until proved. How the statuses and "
        "steps below are decided: [the catalogue's front page](../revit-tool-catalogue.md)."
        % (len(entries), proven, runs_draft, refused),
        "",
        "| Tool | Status | Kind |",
        "|---|---|---|",
    ]
    for e in entries:
        word = {"PROVEN": "usable"}.get(e["word"], e["word"].lower() if e["usable"]
                                         else "refused until proved")
        if e["word"] != "PROVEN" and e["usable"]:
            word = "%s, runs" % e["word"].lower()
        out.append("| [%s](#%s) | %s | %s |" % (e["title"], e["anchor"], word, e["risk"]))
    out.append("")
    out.append("---")
    out.append("")
    for e in entries:
        out.extend(render_entry(e))
    return "\n".join(out).rstrip() + "\n"


def render_ribbon(rows):
    out = []
    shipped = [r for r in rows if r["state"] == "SHIPPED"]
    other = [r for r in rows if r["state"] != "SHIPPED"]
    for r in shipped:
        out.append("**%s tab › %s panel** - the %s (`%s`):" % (r["tab"], r["panel"],
                                                              r["product"], r["source"]))
        out.append("")
        out.append("| Button | What it does |")
        out.append("|---|---|")
        for item in r["items"]:
            if item["menu"]:
                out.append("| **%s** (a button with a drop-down arrow) | %s |" % (
                    item["label"], item["menu"][0]["tip"]))
                for row in item["menu"][1:]:
                    out.append("| › %s (in its drop-down) | %s |" % (row["label"], row["tip"]))
            else:
                out.append("| **%s** | %s |" % (item["label"], item["tip"]))
        out.append("")
    if other:
        out.append("Not on a modeller's ribbon: %s. Each is a `%s` product in "
                   "`platform/heron-products.json` - a shape proof with one button "
                   "that does nothing - and the installer does not offer it." % (
                       "; ".join("the %s product's **%s** tab, **%s** panel (`%s`)"
                                 % (r["product"], r["tab"], r["panel"], r["source"])
                                 for r in other),
                       "` or `".join(sorted(set(r["state"] for r in other)))))
        out.append("")
    return out


def render_front(groups, retired, rows):
    every = [e for _n, entries in groups.values() for e in entries]
    proven, runs_draft, refused = counts(every)
    out = [
        "[Heron AI](../../README.md) › [Documentation](../README.md) › **Revit tool catalogue**",
        "",
        "# Revit tool catalogue",
        "",
        "<!-- GENERATED by tools/generate-tool-catalogue.py from the cards in "
        "brain/fragments and the ribbon code in revit/. Do not edit: run the tool. -->",
        "",
        "**Every tool Heron has for Revit, by job** - what each one does, how you reach "
        "it, what it needs, what you get, and on which Revit releases it is built and "
        "proved. It is written by a script from Heron's own tool cards, so no count or "
        "status on these pages is typed by a person.",
        "",
        "Heron holds **%d live tools**: **%d are usable in a chat**, %d are not proved "
        "yet but run because they change nothing in the model, and %d are refused "
        "until they are proved. %d retired tools are left out - each one's card names "
        "the tool that replaced it." % (len(every), proven, runs_draft, refused, retired),
        "",
        "## How you reach a tool",
        "",
        "**Almost every tool is reached by asking in a Claude chat, not by a button.** "
        "Like typing a command in Revit instead of hunting for it on the ribbon: you "
        "say what you want - *\"which views have no template?\"*, *\"select all ducts "
        "on Level 2\"* - and Heron picks the tool whose words match, runs it inside "
        "Revit and reports back. The phrases on each page are the ones the tool's "
        "card declares; your own words work too.",
        "",
        "Before the first request: Revit open with a model, and Heron connected - "
        "press **Heron** on the Heron tab until its heron turns orange. Installing "
        "Heron: [the installation guide](install.md).",
        "",
        "**A request you repeat** can be saved and run again from the **Commands** "
        "panel (Heron button's drop-down › Commands) - [Using Heron](using-heron.md).",
        "",
        "## The Heron tab in Revit",
        "",
        "Read out of the add-in's own ribbon code, so it is what Revit shows:",
        "",
    ]
    out.extend(render_ribbon(rows))
    out.extend([
        "## What the status words mean",
        "",
        "| Status | What it means for you |",
        "|---|---|",
        "| **Usable in a chat** | Proved on a real Revit model, and its proof still "
        "matches its code (D-30). A chat runs it. |",
        "| **DRAFT, runs** / **STALE, runs** | Not proved, or its proof is out of "
        "date - but it only reads the model or changes what is shown (a selection, a "
        "zoom), so a chat still runs it and the answer says DRAFT. Nothing in the "
        "model changes. |",
        "| **Refused until proved** | It would change the model and is not proved, or "
        "its proof is out of date. **Not yet usable in a chat - refused until proved "
        "(D-117).** |",
        "",
        "And what each tool needs switched on:",
        "",
        "| Kind | Needs, in Heron's Settings |",
        "|---|---|",
        "| Answers a question (`READ`, `ANALYZE`, `SUGGEST`) | nothing - works with "
        "Changes off |",
        "| Changes only what Revit shows (`EXECUTE`) | nothing - works with Changes off |",
        "| Changes the model (`MODIFY`) | **Changes** on |",
        "| Writes files outside the model (`PUBLISH`) | **Changes** and **Publish** on |",
        "| Changes project-wide settings (`ADMIN`) | **Changes** and **Admin** on |",
        "",
        "Heron starts with every switch off, so it only reads until you turn "
        "**Changes** on. A change is one entry in Revit's Undo list; with **Ask to "
        "Keep** on, Revit asks **Keep or Discard** before anything is kept. More: "
        "[What keeps the model safe](safety.md).",
        "",
        "**Built for** is the Revit releases a tool is compiled for. **Proved on** is "
        "the releases a real run was recorded on. A release in the first list and not "
        "the second is expected to work and has not been watched working.",
        "",
        "## Tools by job",
        "",
        "| Job | Tools | Usable in a chat | Not proved, still run | Refused until proved |",
        "|---|---|---|---|---|",
    ])
    for slug in sorted(groups, key=lambda s: groups[s][0].lower()):
        name, entries = groups[slug]
        p, r, x = counts(entries)
        out.append("| [%s](tool-catalogue/%s.md) | %d | %d | %d | %d |"
                   % (name, slug, len(entries), p, r, x))
    out.extend([
        "",
        "## Keeping this page true",
        "",
        "It is written by `python tools/generate-tool-catalogue.py` from "
        "`brain/fragments/*/fragment.yaml` and the ribbon code in `revit/`, and "
        "`python tests/test_tool_catalogue.py` fails when a card changed and the "
        "pages were not written again. Change a card, then run the tool.",
    ])
    return "\n".join(out).rstrip() + "\n"


def build(root=None, fragments=None):
    """{relative path: text} for every page."""
    root = root or ROOT
    if fragments is None:
        fragments, problems = HF.load_all()
        if problems and not fragments:
            raise ValueError("; ".join(problems))
    groups, retired = collect(fragments)
    pages = {FRONT.replace(os.sep, "/"): render_front(groups, retired, ribbons(root))}
    for slug, (name, entries) in groups.items():
        pages["%s/%s.md" % (PAGES.replace(os.sep, "/"), slug)] = \
            render_group(slug, name, entries)
    return pages


def stale_pages(pages, root=None):
    """Paths whose file differs from `pages`, and catalogue pages no card wants."""
    root = root or ROOT
    wrong = []
    for rel, text in sorted(pages.items()):
        path = os.path.join(root, rel)
        try:
            with io.open(path, encoding="utf-8", newline="") as fh:
                held = fh.read().replace("\r\n", "\n")
        except (IOError, OSError):
            held = None
        if held != text:
            wrong.append(rel)
    folder = os.path.join(root, PAGES)
    if os.path.isdir(folder):
        for name in sorted(os.listdir(folder)):
            rel = "%s/%s" % (PAGES.replace(os.sep, "/"), name)
            if name.endswith(".md") and rel not in pages:
                wrong.append(rel + " (no job has this page any more)")
    return wrong


def write(pages, root=None):
    root = root or ROOT
    folder = os.path.join(root, PAGES)
    os.makedirs(folder, exist_ok=True)
    for name in os.listdir(folder):
        rel = "%s/%s" % (PAGES.replace(os.sep, "/"), name)
        if name.endswith(".md") and rel not in pages:
            os.remove(os.path.join(folder, name))
    for rel, text in pages.items():
        with io.open(os.path.join(root, rel), "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[1].strip())
    parser.add_argument("--check", action="store_true",
                        help="exit 1 if a page is out of date, writing nothing")
    args = parser.parse_args(argv)
    try:
        pages = build()
    except ValueError as exc:
        print("The library could not be read: %s" % exc)
        return 2
    if args.check:
        wrong = stale_pages(pages)
        if wrong:
            print("The Revit tool catalogue is out of date:")
            for rel in wrong:
                print("  %s" % rel)
            print("Run: python tools/generate-tool-catalogue.py")
            return 1
        print("The Revit tool catalogue is current (%d pages)." % len(pages))
        return 0
    write(pages)
    print("Wrote %d pages: %s and %s/." % (len(pages), FRONT, PAGES))
    return 0


if __name__ == "__main__":
    sys.exit(main())
