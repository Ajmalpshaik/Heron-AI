# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
What is left to do, in one place.

    python tools/balance-of-work.py             # print it
    python tools/balance-of-work.py --write     # regenerate the note

Always exits 0. This reports; it does not gate.

WHY THIS EXISTS
---------------
Four tools already answer part of "what is left", and none of them answers all
of it. check-gaps.py knows what is unfinished versus what is only waiting for a
machine. owner-queue.py knows what needs the owner personally. open-defects.py
knows Heron's own defects. agent-count.py knows the register. A person wanting
to decide what to do next had to run four things and hold the answers in their
head, and the fragment library - the largest single body of work left - was in
none of them except as a footnote.

THIS TOOL TYPES NO NUMBER OF ITS OWN. Every figure below is read back out of the
tool that owns it, and every row prints the command that derives it, so a number
here can always be checked against its source in one line. Where a figure could
not be derived, the row says so rather than guessing - a blank is honest and a
plausible zero is not.

FOR-THE-OWNER.md holds no list for exactly this reason: three typed lists went
stale on 2026-09-12, all three on the same day. The difference here is that
nothing is typed. If this file and its source ever disagree, the source wins and
this file is stale - which is what the generated stamp at its top is for.
"""

import io
import os
import re
import subprocess
import sys
from datetime import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "work-notes", "BALANCE-OF-WORK.md")

# The repo is on D: and the temp directory is on C: on the owner's machine.
# os.path.relpath RAISES across drives on Windows, so paths here are joined from
# ROOT and never relativised against the working directory.


def run(args, timeout=300):
    """Run one of our own tools and hand back its stdout.

    stdin is closed deliberately. A tool that stops to ask a question with
    nobody at the keyboard hangs the whole run, and that has happened here.
    """
    try:
        p = subprocess.run(
            [sys.executable] + args,
            cwd=ROOT,
            stdin=subprocess.DEVNULL,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=timeout,
        )
        return p.stdout.decode("utf-8", "replace")
    except Exception as exc:                       # a missing tool is a fact, not a crash
        return "TOOL DID NOT RUN: %s" % exc


def read(rel):
    p = os.path.join(ROOT, rel)
    if not os.path.exists(p):
        return ""
    return io.open(p, encoding="utf-8", errors="replace").read()


def grab(text, pattern, group=1):
    """Pull one figure out of a tool's own printed summary, or None.

    None means 'that tool did not say', and every caller prints that rather
    than substituting a zero.
    """
    m = re.search(pattern, text)
    return m.group(group).strip() if m else None


# --------------------------------------------------------------------------
# the sources
# --------------------------------------------------------------------------

def statuses(folder, pattern="fragment.yaml"):
    """Count heron-status: across a library, the same way FOR-THE-OWNER says to."""
    out = {}
    base = os.path.join(ROOT, *folder.split("/"))
    if not os.path.isdir(base):
        return out
    if pattern == "fragment.yaml":
        paths = [os.path.join(base, d, pattern) for d in os.listdir(base)]
    else:
        paths = [os.path.join(base, f) for f in os.listdir(base) if f.endswith(".yaml")]
    for p in paths:
        if not os.path.exists(p):
            continue
        for line in io.open(p, encoding="utf-8", errors="replace"):
            if line.startswith("heron-status:"):
                s = line.split(":", 1)[1].strip()
                out[s] = out.get(s, 0) + 1
                break
    return out


def register_rows():
    """NEEDS-CHECKING rows, with the regex check-gaps.py and owner-queue.py share.

    Struck ID means done. If you strike anything but the ID it is a comment,
    which is a mistake this register has already recorded against itself.
    """
    text = read("docs/NEEDS-CHECKING.md")
    if not text:
        return None, None
    rows = re.findall(r"^\| (~~)?\*\*([A-Z]\d+[a-z]?)\*\*(~~)?\s*\|", text, re.M)
    done = sum(1 for r in rows if r[0] and r[2])
    return len(rows), done


def proposals_open():
    text = read("docs/PROPOSALS.md")
    if not text:
        return None, None
    items = re.findall(u"^### ([\U0001F534\U0001F7E0\U0001F7E1\U0001F535✅])\\s*(F\\d+)\\.", text, re.M)
    openish = [i[1] for i in items if i[0] != u"✅"]
    return len(items), openish


def live_work_notes():
    """Notes still open, read off the work-notes index rather than typed here.

    A linked row is live; a struck row has been retired. The index ships its
    own completeness check, so this is a derived list, not a remembered one.
    """
    text = read("docs/work-notes/README.md")
    live = re.findall(r"^\| \[`([^`]+)`\]\([^)]+\) \| \*\*([^*]+)\*\*", text, re.M)
    # This page is a work note listed in that same index. Listing itself as
    # outstanding work would be true and useless.
    return [(n, s.rstrip(".").strip()) for n, s in live
            if os.path.basename(n) != "BALANCE-OF-WORK.md"]


def collect():
    d = {}

    frag = statuses("brain/fragments")
    d["frag"] = frag
    d["skills"] = statuses("brain/skills", "*.yaml")

    drafts_dir = os.path.join(ROOT, "brain", "agent-proof-drafts")
    d["agent_drafts"] = sorted(
        f[:-5] for f in os.listdir(drafts_dir) if f.endswith(".yaml")
    ) if os.path.isdir(drafts_dir) else []

    d["rows"], d["rows_done"] = register_rows()
    d["prop_total"], d["prop_open"] = proposals_open()
    d["notes"] = live_work_notes()

    defects = run(["tools/open-defects.py"])
    d["defect_open"] = grab(defects, r"still OPEN\s*:\s*(\d+)")
    d["defect_rows"] = grab(defects, r"rows in the section\s*:\s*(\d+)")
    d["defect_ids"] = grab(defects, r"ids\s*:\s*([0-9, ]+)")

    agents = run(["tools/agent-count.py"])
    d["agents_left"] = grab(agents, r"Register reconciles:.*?(\d+) left")
    d["agents_total"] = grab(agents, r"Register reconciles:\s*(\d+) agents")

    owner = run(["tools/owner-queue.py"])
    d["owner_items"] = grab(owner, r"(\d+) item\(s\) waiting on the owner")

    jobs = run(["tools/generate-jobs.py"])
    # generate-jobs.py emits a YAML comment block, so its prose is wrapped and
    # every line carries a leading '#'. Strip the markers BEFORE flattening, or
    # a sentence that spans two lines reads as "35 of # them" and matches nothing.
    flat = re.sub(r"\s+", " ", re.sub(r"(?m)^#[ \t]?", "", jobs))
    d["jobs_ready"] = grab(flat, r"(\d+) of them can be arranged as written")
    d["jobs_blocked"] = grab(flat, r"(\d+) cannot, and are listed")
    # How many DRAFTs have never been RUN HERE, which is not how many are DRAFT.
    # brain/proof-drafts/runs/ is gitignored (.gitignore:193) and therefore
    # per-worktree: main carried 309 records on 2026-09-19 and a fresh worktree
    # carried 5. Rows 1a and 1b are a fact about THIS CHECKOUT, not the project,
    # and the first version of this page shipped a worktree's 35/44 where the
    # owner's own tree said 14/26.
    d["jobs_norun"] = grab(flat, r"(\d+) fragment\(s\) are still DRAFT with no run record")

    docs = run(["tools/check-docs.py"])
    d["q_open"] = grab(docs, r"ACTUAL:\s*\d+ answered,\s*(\d+) open")
    d["q_ids"] = grab(docs, r"still open:\s*(.+)")
    d["stale_sigs"] = grab(docs, r"STALE - signed, then the code changed under it \((\d+)\)")
    # check-signatures.py prints that heading ONLY when something is stale,
    # and "No signature is waiting" when nothing is. Without this second read
    # the row could never say zero - it said "not derived", which this tool
    # tells the reader does NOT mean zero. A clean board read as an unchecked
    # one on 2026-09-19 and 2026-09-20.
    if d["stale_sigs"] is None and "No signature is waiting" in docs:
        d["stale_sigs"] = "0"

    return d


# --------------------------------------------------------------------------
# the note
# --------------------------------------------------------------------------

def n(v):
    """A figure a tool did not give back is not a zero."""
    return "**not derived**" if v in (None, "") else "**%s**" % v


def render(d):
    stamp = datetime.now().strftime("%Y-%m-%d %H:%M")
    frag = d["frag"]
    draft = frag.get("DRAFT")
    proven = frag.get("PROVEN")
    total = sum(frag.values()) if frag else None
    skills_draft = d["skills"].get("DRAFT")
    skills_total = sum(d["skills"].values()) if d["skills"] else None
    rows_left = (d["rows"] - d["rows_done"]) if d["rows"] is not None else None

    L = []
    w = L.append

    w("<!-- Heron-Agent:  none -->")
    w("<!-- Heron-Step:   18 -->")
    w("<!-- Heron-Status: DRAFT -->")
    w("<!-- Heron-Since:  0.1.0 -->")
    w("<!-- Heron-Layer:  docs -->")
    w("<!-- See docs/29-metadata-standard.md -->")
    w("")
    w("# The balance of work")
    w("")
    w("> **GENERATED %s by `python tools/balance-of-work.py --write`.**" % stamp)
    w("> **If that stamp is not today, this page is history and not a work list.** Every figure")
    w("> below was read back out of the tool that owns it, and each row names the command that")
    w("> derives it. Nothing here is typed by hand, so where this page and a register disagree,")
    w("> the register is right and this page is out of date. Run it again.")
    w("")
    w("**It is a work note, and it is meant to be deleted** — which is why it lives in")
    w("`work-notes/` and not in `docs/`. When a row below reaches zero, that row has no more")
    w("business here. When every row does, delete the file and the tool with it.")
    w("")
    w("---")
    w("")
    w("## What is left")
    w("")
    w("| # | What is left | Count | Derive it with |")
    w("|---|---|---|---|")
    w("| 1 | **Fragments that have never met a model** — the largest single body of work left | %s of %s | `grep -h '^heron-status:' brain/fragments/*/fragment.yaml \\| sort \\| uniq -c` |"
      % (n(draft), n(total)))
    w("| 1a | — of those, never run **in this checkout** — see the warning below | %s | `python tools/generate-jobs.py` |"
      % n(d["jobs_norun"]))
    w("| 1b | — of those, **ready to prove right now**, needing only a Revit session | %s | `python tools/generate-jobs.py` |"
      % n(d["jobs_ready"]))
    w("| 1c | — of those, **structurally blocked**, each with a reason printed | %s | `python tools/generate-jobs.py` |"
      % n(d["jobs_blocked"]))
    w("| 2 | **Skills never proved** | %s of %s | `grep -h '^heron-status:' brain/skills/*.yaml \\| sort \\| uniq -c` |"
      % (n(skills_draft), n(skills_total)))
    w("| 3 | **Agents left to build** | %s of %s | `python tools/agent-count.py` |"
      % (n(d["agents_left"]), n(d["agents_total"])))
    w("| 4 | **Agent proofs drafted but unsigned** | %s | `ls brain/agent-proof-drafts/*.yaml` |"
      % n(len(d["agent_drafts"]) if d["agent_drafts"] else None))
    w("| 5 | **Proving-register rows still open** | %s of %s | `python tools/owner-queue.py` |"
      % (n(rows_left), n(d["rows"])))
    w("| 6 | **Heron's own defects still open** | %s of %s | `python tools/open-defects.py` |"
      % (n(d["defect_open"]), n(d["defect_rows"])))
    w("| 7 | **Questions unanswered** | %s | `python tools/check-docs.py` |" % n(d["q_open"]))
    w("| 8 | **Proposals awaiting the owner** | %s of %s | `python tools/owner-queue.py` |"
      % (n(len(d["prop_open"]) if d["prop_open"] is not None else None), n(d["prop_total"])))
    w("| 9 | **Signatures gone stale — proved, then the code moved under them** | %s | `python tools/check-signatures.py` |"
      % n(d["stale_sigs"]))
    w("| 10 | **Everything waiting on the owner personally**, across three registers | %s | `python tools/owner-queue.py` |"
      % n(d["owner_items"]))
    w("")
    w("> **ROWS 1a TO 1c ARE ABOUT THIS CHECKOUT, NOT ABOUT THE PROJECT.** They count what has never")
    w("> been run *here*, and `brain/proof-drafts/runs/` is gitignored, so every worktree starts almost")
    w("> empty. On 2026-09-19 the main checkout held **309** run records and a fresh worktree held")
    w("> **5** — the same library read as *14 ready* from one and *35 ready* from the other. **Regenerate")
    w("> this page from the checkout you will actually prove in**, and treat a figure generated")
    w("> anywhere else as meaningless. Row 1 itself is committed data and does not have this problem.")
    w("")
    w("**Rows 1 and 5 are not the same work and neither contains the other.** A fragment is proved")
    w("against a model; a register row is a thing nothing has checked. A session that clears one can")
    w("leave the other untouched.")
    w("")

    w("## Read the ids, not the count")
    w("")
    w("Every drift this repository has caught was visible in a list and invisible in a total.")
    w("")
    if d["q_ids"]:
        w("**Questions still open:** %s" % d["q_ids"])
        w("")
    if d["defect_ids"]:
        w("**Defects still open** (`docs/FRAGMENT-ISSUES.md` section 5): %s" % d["defect_ids"])
        w("")
        w("> A row can still say OPEN after a later row has closed it — four did on 2026-09-16, and no")
        w("> pattern finds them. Reading beats grepping here.")
        w("")
    if d["prop_open"]:
        w("**Proposals still open** (`docs/PROPOSALS.md`): %s" % ", ".join(d["prop_open"]))
        w("")
    if d["agent_drafts"]:
        w("**Agent proofs drafted and unsigned:** %s" % ", ".join(d["agent_drafts"]))
        w("")
        w("> These are not waiting on a writer. They are waiting on a model — imports needs a **saved**")
        w("> CAD link, and levels, sheets and annotation need one real project model rather than an")
        w("> arrangement invented for the proof.")
        w("")

    w("## Work notes still open, and what each is waiting for")
    w("")
    w("A note is not finished when its work is finished. It is finished when the durable part of it")
    w("has moved somewhere permanent and the note itself has gone. These have not reached that point,")
    w("and the reason is the second column — read it before deleting one.")
    w("")
    if d["notes"]:
        w("| Note | Why it is still here |")
        w("|---|---|")
        for name, state in d["notes"]:
            w("| `%s` | %s |" % (name, state))
    else:
        w("*None — `docs/work-notes/README.md` lists no live note.*")
    w("")
    w("---")
    w("")
    w("## What this page deliberately does not cover")
    w("")
    w("- **Whether anything above is a good idea.** It counts what is open, not what is worth doing.")
    w("- **Anything with no register.** If a piece of work is in nobody's list, it is in no row here")
    w("  either, and that is a gap in the registers rather than in this page.")
    w("- **The archive.** `docs/handover-archive/` is finished work kept on purpose and is never a")
    w("  balance.")
    w("")
    return "\n".join(L) + "\n"


def to_console(d):
    frag = d["frag"]
    rows_left = (d["rows"] - d["rows_done"]) if d["rows"] is not None else None
    line = "=" * 62
    out = [line, "THE BALANCE OF WORK   %s" % datetime.now().strftime("%Y-%m-%d %H:%M"), line, ""]
    pairs = [
        ("fragments never in front of a model", frag.get("DRAFT"), "of %s" % sum(frag.values()) if frag else ""),
        ("  never run IN THIS CHECKOUT", d["jobs_norun"], "<- worktree-local"),
        ("  of those, arrangeable right now", d["jobs_ready"], ""),
        ("  of those, structurally blocked", d["jobs_blocked"], ""),
        ("skills never proved", d["skills"].get("DRAFT"), "of %s" % sum(d["skills"].values()) if d["skills"] else ""),
        ("agents left to build", d["agents_left"], "of %s" % d["agents_total"] if d["agents_total"] else ""),
        ("agent proofs unsigned", len(d["agent_drafts"]), ""),
        ("proving-register rows open", rows_left, "of %s" % d["rows"] if d["rows"] else ""),
        ("Heron's own defects open", d["defect_open"], "of %s" % d["defect_rows"] if d["defect_rows"] else ""),
        ("questions unanswered", d["q_open"], ""),
        ("proposals awaiting the owner", len(d["prop_open"]) if d["prop_open"] is not None else None, ""),
        ("signatures gone stale", d["stale_sigs"], ""),
        ("waiting on the owner in total", d["owner_items"], ""),
    ]
    for label, value, tail in pairs:
        shown = "not derived" if value in (None, "") else str(value)
        out.append("  %-38s %8s %s" % (label, shown, tail))
    out += ["", "  A figure that says 'not derived' means its tool did not answer.",
            "  It does not mean zero.", ""]
    return "\n".join(out)


def main(argv):
    d = collect()
    if "--write" in argv:
        io.open(OUT, "w", encoding="utf-8", newline="").write(render(d))
        sys.stdout.write("Written: docs/work-notes/BALANCE-OF-WORK.md\n")
    sys.stdout.write(to_console(d) + "\n")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
