# Heron-Agent:  HERON-SKL-VAL-004, HERON-RAG-RNK-006
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Do a SKILL's own words reach a SKILL's own capabilities?

    python tools/check-skill-routing.py
    python tools/check-skill-routing.py --revit 2024
    python tools/check-skill-routing.py --list

THE QUESTION NOTHING ELSE IN THIS REPOSITORY ASKS
--------------------------------------------------
A skill declares two halves and nothing has ever compared them:

    utterances:  the words Ajmal actually says
    needs:       the capabilities it requires

`check-routing.py` asks each FRAGMENT its own declared utterances. That is a
different question with a different subject, and a library can pass it while
every skill in it is unreachable - because a skill's utterances are **not
indexed at all**. `heron_search`, `heron_retrieve` and `heron_embed` contain no
mention of a skill; only `brain/fragments/*/fragment.yaml` is indexed. So the
words a skill claims can only ever land on a FRAGMENT, and whether that
fragment provides something the skill asked for is the thing nobody checked.

`check-risk-crossings.py` asks a WRITTEN-DOWN list of ordinary sentences. That
list is deliberately not derived, and this one deliberately is: these are the
42 sentences the library itself claims somebody will say.

WHAT IT REPORTS, IN SEPARATE LISTS BECAUSE THEY ARE DIFFERENT CLAIMS
---------------------------------------------------------------------
**REACHES** - the utterance resolved to a capability the skill declares in
`needs`. This is a LOWER BOUND exactly as `check-routing.py` is one: a skill's
own phrasing shares vocabulary with the fragment it wants, so passing proves
little. Failing proves something real.

**MISSES** - it resolved to a capability this skill never asked for. Not
automatically a defect: a skill composes several capabilities and `lookup`
returns ONE, so a miss says the single best answer sits outside the plan. The
reader decides whether the plan is short a capability or the words are wrong.

**CROSSINGS** - it resolved to something that CHANGES THE MODEL when the skill
that claims the sentence does not. That is FRAGMENT-ISSUES rows 109, 113 and
116 arriving at the skill layer, and it is the list to read first.

**ESCALATIONS** - the skill was not asking a question, so "a question answered
by a write" is the wrong sentence, but its words still reached HIGHER UP THE
LADDER than the skill owns. Kept apart from the crossings because it is a
different claim, and reported at all because the first run of this tool buried
one: `highlight them` belongs to `select-elements` at EXECUTE and resolves to
`HIGHLIGHT_VS_REST`, a MODIFY that overrides graphics in the view. In a list of
21 misses a risk escalation reads exactly like a wrong-but-harmless answer.

THE DISCRIMINATOR IS THE SKILL'S OWN RISK, NOT A GUESS ABOUT THE WORDS
----------------------------------------------------------------------
`check-risk-crossings.py` has to ask "was a READ beaten", because a written
down sentence carries no declaration of what it deserves. Here the sentence
belongs to a skill, and **the skill has already declared its risk**. So a
crossing is decided by comparing two things both of which are written down:

    a skill at READ or ANALYZE whose words reach MODIFY, PUBLISH or ADMIN

No intent parsing, no judgement about phrasing. `mep-grayout` is declared
MODIFY, so its words reaching a MODIFY is CORRECT and is not reported. That is
the same principle as the sibling tool's "an imperative is not a question",
obtained from a declaration instead of from a guess.

**AN EXECUTE IS REPORTED SEPARATELY** and is not counted a crossing, matching
`check-risk-crossings.py`: a view change is real and visible and undone by
Reset Temporary Hide/Isolate, and `select-elements` is declared EXECUTE
precisely because selecting is not reading.

WHAT IT CANNOT SEE, AND ONE OF THEM IS A TRAP
----------------------------------------------
It reads the store, so a skill or fragment edited but not re-indexed is judged
as the search actually sees it. And **a count from this tool is a sample, not a
measurement** - FRAGMENT-ISSUES row 116 measured 7, 9 and 8 crossings from
three runs of the sibling sweep minutes apart, two of them with byte-identical
inputs, and the cause is in the INDEX rather than the ranker. So this prints
the same index fingerprint that tool prints, for the same reason: two runs are
worth comparing only when the block above the numbers matches. **Read the
NAMES, not the number.**

Exit 0 whatever it finds, exactly like both siblings. A crossing is a FINDING
that a person judges; a tool that failed a build over one would teach people to
weaken a skill's utterances to buy a number, which is the one response
brain/retrieval-history.md rules out.

THE CLASSIFIER IS SHARED, AND `prove-skill.py` IS WHAT IT IS SHARED WITH
------------------------------------------------------------------------
`classify()` below decides which of the five lists an answer belongs in, and it
lives outside `main()` because `tools/prove-skill.py` - which asks this same
seam the same sentences as one half of a skill's proof - imports it. Two copies
of *what a crossing is* would start disagreeing about the same skill on the
same day, and the whole argument of this file is that a disagreement between
two declarations is the thing worth finding.

`words_moved()` is here for the same reason and answers the other half of it:
whether a saved measurement is still ABOUT the sentences a skill says. Both
`tools/prove-skill.py`, which writes the recordings, and
`tools/generate-skill-catalog.py`, which draws them, read it from here.

**It compares the PHRASES and not `_index_fingerprint()` above.** The store is
one file every worktree writes, so its md5 differs between two runs for reasons
that change nothing, and a rule hung on it would cry stale every time - noise a
reader learns to ignore. Utterances live in `brain/skills/*.yaml`, under version
control, and can be rewritten without the store changing at all. The fingerprint
answers *which library was asked*; the phrases answer *which sentences* - and it
was the second question that went unasked for a week (register row 152).
"""

import argparse
import hashlib
import os
import pathlib
import sqlite3
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

SKILLS_DIR = os.path.join(ROOT, "brain", "skills")

# The ladder from the Constitution, lowest first (docs/12 s2). ANALYZE has side
# effects "none" - it computes over what was read - which is why a skill at
# ANALYZE is treated as a question-asker here alongside READ.
LADDER = ["READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH", "ADMIN"]

CHANGES_THE_MODEL = ("MODIFY", "PUBLISH", "ADMIN")
CHANGES_A_VIEW = ("EXECUTE",)

# A skill at one of these is ASKING something. Its words reaching a write is
# the crossing this tool exists to find.
ASKS_A_QUESTION = ("READ", "ANALYZE")


def _index_fingerprint():
    """{path, md5, counts, built_from, other_tree} for the store read here.

    The block `check-risk-crossings.py` prints too - it IMPORTS this rather
    than keeping its own, which it did until 2026-09-19 and which is how two
    copies of one rule start drifting. The reason is row 116's: two runs of a
    sweep over this store are not comparable unless the index they ran against
    is known to be the same one. A failure to read it is REPORTED and never
    returned as a zero (D-52).

    `built_from` is the fragment folder the store was last rebuilt from, and
    `other_tree` is True when that is not the tree running now. The store is
    ONE file for every checkout on the machine, so this names the session
    whose opinion is currently in it - row 131, where a verified fragment edit
    was ABSENT again minutes later because two other worktrees were up. It is
    None on a store written before `heron_search.index` recorded it, and an
    absent answer is not the same as agreement.
    """
    import heron_scope as SCOPE

    out = {}
    try:
        # One spelling - scope_path joins with os.sep, HERON_KNOWLEDGE arrives
        # however the caller typed it, and the same file printed twice must not
        # read as two.
        out["path"] = SCOPE.scope_path(SCOPE.GLOBAL).replace(os.sep, "/")
    except ValueError as why:
        return {"error": str(why)}

    try:
        with open(out["path"], "rb") as handle:
            out["md5"] = hashlib.md5(handle.read()).hexdigest()
    except (IOError, OSError) as why:
        out["md5"] = "unreadable (%s)" % why
        return out

    counts = {}
    try:
        uri = pathlib.Path(out["path"]).as_uri() + "?mode=ro"
        db = sqlite3.connect(uri, uri=True)
        try:
            for table in ("identities", "fragments", "vectors", "utterances"):
                counts[table] = db.execute(
                    "SELECT COUNT(*) FROM %s" % table).fetchone()[0]
            # WHOSE FRAGMENTS ARE IN IT. Read in the same read-only
            # connection, and a store too old to carry the row answers None
            # rather than naming a tree it does not know.
            try:
                found = db.execute(
                    "SELECT digest FROM index_state WHERE name = 'search_root'"
                ).fetchone()
                out["built_from"] = found[0] if found else None
            except sqlite3.OperationalError as exc:
                # Only the table not being there yet - a store written
                # before `heron_search.index` recorded this. Any other
                # fault is a broken store and must not read as "no tree".
                if "no such table" not in str(exc):
                    raise
                out["built_from"] = None
        finally:
            db.close()
    except Exception as why:                                   # noqa: BLE001
        out["counts_error"] = "%s: %s" % (type(why).__name__, why)
        return out

    out["counts"] = counts
    if out.get("built_from"):
        import heron_fragment as FRAG
        here = os.path.abspath(FRAG.FRAGMENTS_DIR).replace(os.sep, "/")
        out["other_tree"] = out["built_from"] != here
        out["running_in"] = here
    return out


def words_moved(rows, utterances):
    """(gone, fresh) - the phrases a recording and a skill no longer share.

    A recording holds the sentence it measured. A skill's utterances live in
    git and get edited without anyone re-taking one, so `words 1 of 4 reach`
    outlives the four it counted and the card goes on showing a number about
    sentences the skill no longer says. That is D-30's staleness, and the
    catalogue held the fingerprint for it without ever comparing anything.

    THIS COMPARES THE PHRASES AND NOT THE STORE, which is the whole design.
    `global.db` is one file every worktree writes (row 116), so its md5
    differs between two renders for reasons that change nothing, and a rule
    hung on it would cry STALE on every page - noise a reader learns to
    ignore, which is worse than silence. The phrases are under version
    control, and a difference in them is a real one.

    `gone` was measured and is no longer said. `fresh` is said today and was
    never measured. Either one makes the count a statement about a different
    set of sentences.
    """
    measured = [str(row[0]) for row in rows if row]
    says = [str(one) for one in utterances]
    gone = [one for one in measured if one not in says]
    fresh = [one for one in says if one not in measured]
    return gone, fresh


# WHAT THE RETRIEVER ALREADY SAID ABOUT HOW LITTLE IT FOUND, AND EVERY SWEEP
# THREW AWAY. `heron_retrieve.Contest.sentence()` writes these clauses, and
# `heron_brain.lookup` carries them across in `note` - so the evidence has been
# at this seam all along and no sweep has ever read it. FRAGMENT-ISSUES row 137
# is the cost: a question inside a MODIFY skill reaching an unrelated MODIFY is
# invisible to the risk comparison BY CONSTRUCTION, and the retriever was
# saying "no route preferred this" in plain words the whole time.
#
# THE PHRASES ARE MATCHED AND NOT RE-DERIVED. A second implementation of "was
# this contested" would be a second opinion about the retriever's own
# measurement. `tests/test_skill_proving.py` pins each phrase against
# `Contest.sentence`'s source, so a reword there fails loudly and names it
# rather than quietly turning this section off.
NOTHING_FOUND = (
    ("a coin toss - no route preferred the winner", "A COIN TOSS"),
    ("the words route ranked the library rather than selecting from it",
     "ranked the library rather than selecting from it"),
    ("one candidate, so nothing was contested",
     "a shortlist of one is not a ranking"),
    ("the pool is not yet evidence, so 'both agree' means nothing",
     "'both agree' means nothing here yet"),
)


def unsettled(note):
    """The retriever's own sentences about how little it found, as short tags.

    An empty tuple means it said none of them, which is NOT the same as
    saying the answer is good - it is the absence of a complaint (D-52). A
    note that could not be read is empty for the same reason.
    """
    said = note or ""
    return tuple(tag for tag, marker in NOTHING_FOUND if marker in said)


def fingerprint_lines(index, lead="  "):
    """The fingerprint block as lines, so four sweeps print ONE of it.

    `check-risk-crossings.py`, `prove-skill.py` (twice) and this file each
    had their own copy of these six prints, which is four places to edit the
    day the block learns something new - and it has just learned `built_from`.
    Returning lines rather than printing keeps it usable from a tool that
    wraps its output and from a test that reads it.

    A STORE BUILT BY ANOTHER TREE IS SAID LOUDLY AND IS NOT AN ERROR. It is
    the normal state on a machine running several worktrees, and the only
    thing wrong with it was that nothing said so (row 131).
    """
    out = []
    if index.get("error"):
        return [lead + "no store: %s" % index["error"]]
    out.append(lead + "store    %s" % index.get("path", "?"))
    out.append(lead + "md5      %s" % index.get("md5", "?"))
    if index.get("counts_error"):
        out.append(lead + "counts   COULD NOT BE READ - %s"
                   % index["counts_error"])
        out.append(lead + "         Not reported as zero on purpose: a store")
        out.append(lead + "         that cannot be read is not an empty one "
                          "(D-52).")
    elif index.get("counts"):
        out.append(lead + "rows     %s" % ", ".join(
            "%s %d" % (name, index["counts"][name])
            for name in sorted(index["counts"])))
    if "built_from" in index:
        out.append(lead + "built by %s" % (index["built_from"]
                                           or "NOT RECORDED - a store written "
                                              "before anyone asked"))
        if index.get("other_tree"):
            out.append(lead + "         ^ NOT THE TREE RUNNING HERE, which is")
            out.append(lead + "           %s" % index.get("running_in", "?"))
            out.append(lead + "           The store is one file for every "
                              "checkout; this")
            out.append(lead + "           names whose fragments are in it "
                              "(row 131).")
    return out


def _md5(path):
    """The store's hash again, for the did-this-run-write-it check."""
    try:
        with open(path, "rb") as handle:
            return hashlib.md5(handle.read()).hexdigest()
    except (IOError, OSError):
        return None


def skills():
    """Every skill as (id, risk, [utterances], {needs}), loaded through the
    brain's own loader rather than by re-reading YAML.

    Using `heron_skill.load_all` and not a private parse is deliberate: a skill
    this tool could read but Heron could not would be judged as reachable when
    it is not there at all.
    """
    import heron_skill as SKILL

    # load_all returns a DICT keyed by id, not a list. Iterating it directly
    # yields the ids as strings and every attribute read then fails - which is
    # how this was found, and is worth a line rather than a silent `.values()`.
    found, problems = SKILL.load_all()
    out = []
    for skill in found.values():
        out.append((
            skill.data.get("id") or os.path.basename(skill.path),
            (skill.data.get("risk") or "").upper(),
            list(skill.data.get("utterances") or []),
            set(skill.needs() or []),
        ))
    return out, problems


def declared_by_fragments():
    """{phrase: [(fragment id, capability, risk)]} for every fragment utterance.

    Read from DISK and not from the store, because this half of the report is
    deliberately store-independent: it is the part a rebuild cannot move, and
    row 116 is the reason that distinction is worth having.
    """
    try:
        import yaml
    except ImportError:
        sys.stderr.write("This needs PyYAML: pip install --user pyyaml\n")
        raise SystemExit(2)

    fragments = os.path.join(ROOT, "brain", "fragments")
    out = {}
    if not os.path.isdir(fragments):
        return out
    for name in sorted(os.listdir(fragments)):
        path = os.path.join(fragments, name, "fragment.yaml")
        if not os.path.exists(path):
            continue
        with open(path, encoding="utf-8") as handle:
            doc = yaml.safe_load(handle)
        if not doc:
            continue
        for said in doc.get("utterances") or []:
            out.setdefault(said.strip().lower(), []).append(
                (doc.get("id"), doc.get("capability"), doc.get("risk")))
    return out


def without_the_store(loaded):
    """What is true about these skills before anything is ranked.

    THIS IS THE HALF THAT CANNOT WOBBLE, and it is printed first for that
    reason. Row 116 measured 7, 9 and 8 crossings from three runs of the
    sibling sweep minutes apart; everything below is read from the files on
    disk, so two runs of it disagree only if somebody edited something.

    It makes no judgement about PHRASING. Whether "how many sprinklers" is a
    question or a design request is exactly the intent-parsing that
    check-risk-crossings.py's docstring rules out - "a tool that tried to parse
    intent would be a second, worse retriever". The facts here are mechanical:
    who declared what, and whether two declarations agree.
    """
    by_fragment = declared_by_fragments()

    identity, ranked, collisions = [], [], []
    seen = {}
    for sid, srisk, said, needs in loaded:
        for phrase in said:
            key = phrase.strip().lower()
            seen.setdefault(key, []).append((sid, srisk))
            if key in by_fragment:
                for fid, capability, frisk in by_fragment[key]:
                    identity.append((sid, srisk, phrase, fid, capability,
                                     frisk, capability in needs))
            else:
                ranked.append((sid, srisk, phrase))

    for key, claimants in sorted(seen.items()):
        if len(claimants) > 1:
            collisions.append((key, claimants))

    return identity, ranked, collisions


def margin(answer):
    """How the winner beat the runner-up, in the terms the seam actually gives.

    NOT A NUMBER, AND THAT IS A FINDING RATHER THAN A SHORTCUT. `heron_retrieve
    .find` puts `score`, `words_score`, `nearness_score` and `rerank_score` on
    every candidate, and `heron_brain.lookup` copies FIVE keys across -
    capability, provider, status, risk, why - and drops all four scores. So a
    caller at the seam a host uses cannot tell a landslide from a photo-finish,
    and FRAGMENT-ISSUES row 109's defining measurement - *"2.4 ranks clear of
    the runner-up"* - is not reproducible here at all. It was taken by hand.

    What IS available is `why()`, a string of the form "words #1 + nearness #7",
    and that is what row 109 actually quotes when it explains itself: the two
    routes' ranks, which say WHICH route carried the win. Reporting that is
    more use than a scalar anyway - it names the disagreement rather than
    measuring it.

    Reading this tool's own seam and not a deeper one is deliberate: the
    sibling sweep's docstring calls it "the same seam a host uses, so a result
    here is what a user would really have got". Reaching past it for a number
    would make this tool's answer better than the host's, which is the one
    thing it must not be.
    """
    candidates = answer.get("candidates") or []
    winner = (candidates[0].get("why") if candidates else None) or "?"
    runner = candidates[1].get("why") if len(candidates) > 1 else None
    return winner, runner


def rung(level):
    """Where a risk level sits on the ladder; -1 for anything unrecognised."""
    return LADDER.index(level) if level in LADDER else -1


def classify(skill_risk, resolved_risk, capability, needs):
    """Which of the five lists this answer belongs in. ONE implementation.

    Returns "crossing", "view", "escalation", "reach" or "miss".

    EXTRACTED RATHER THAN COPIED, and the reason is this file's own subject.
    `tools/prove-skill.py` asks the same question of the same seam and has to
    get the same answer, and the only way two copies of a judgement stay in
    step is by not being two copies - the argument `batch-prove.py` makes about
    importing `looks_empty` from the drafter rather than re-writing it.

    Every branch below kept its comment, because each one was a correction.
    """
    # A skill that asks a question, answered by something that changes
    # the model. Reported whether or not the capability is in `needs`:
    # a skill that DECLARED the write is worse, not better, and the
    # lists below keep the two apart.
    if skill_risk in ASKS_A_QUESTION and resolved_risk in CHANGES_THE_MODEL:
        return "crossing"
    if skill_risk in ASKS_A_QUESTION and resolved_risk in CHANGES_A_VIEW:
        return "view"
    if (resolved_risk in CHANGES_THE_MODEL or resolved_risk in CHANGES_A_VIEW) \
            and rung(resolved_risk) > rung(skill_risk) >= 0:
        # HIGHER ON THE LADDER THAN THE SKILL DECLARES, from a skill
        # that was not asking a question. A DIFFERENT CLAIM from a
        # crossing and kept apart from it on purpose: nobody asked
        # anything, so "a question answered by a write" is the wrong
        # sentence - but the request still reached further up the
        # ladder than the skill that owns the words.
        #
        # THE RISK MUST ACTUALLY DO SOMETHING, and testing the rung
        # alone was not enough. The first version of this rule was
        # `rung(risk) > rung(srisk)`, and it reported
        # `find the blank parameters` - READ, resolving to
        # DESCRIBE_BLANK_PARAMETERS at ANALYZE - as an escalation.
        # docs/12 s2 gives ANALYZE side effects **none**, and row 116
        # names that capability as the RIGHT owner for that sentence,
        # so the rule had flagged the one case the register calls
        # correct. Same over-reporting the sibling tool records fixing
        # ("it flagged anything that was not READ"), reproduced here
        # by reading position on the ladder as if it meant harm.
        #
        # THIS LIST EXISTS BECAUSE THE FIRST RUN HID ONE. `highlight
        # them` belongs to select-elements, declared EXECUTE, and
        # resolves to HIGHLIGHT_VS_REST - a MODIFY that overrides
        # graphics in the view. It was filed under "reached something
        # the skill never asked for", in a list of 21, where a risk
        # escalation reads exactly like a wrong-but-harmless answer.
        return "escalation"
    if capability in needs:
        return "reach"
    return "miss"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--revit", default="2020")
    parser.add_argument("--list", action="store_true",
                        help="print the skills and their words, run nothing")
    args = parser.parse_args()

    loaded, problems = skills()
    if problems:
        print("SKILLS THAT WOULD NOT LOAD (%d) - these are not routed below:"
              % len(problems))
        for why in problems:
            print("  %s" % why)
        print("")

    if not loaded:
        print("No skill loaded from %s - nothing to route."
              % os.path.relpath(SKILLS_DIR, ROOT))
        return 0

    if args.list:
        for sid, risk, said, needs in loaded:
            print("%s  (%s)" % (sid, risk or "no risk declared"))
            for phrase in said:
                print("    %s" % phrase)
            print("    needs: %s" % ", ".join(sorted(needs)))
        return 0

    # THE STORE-INDEPENDENT HALF, FIRST AND ON PURPOSE. A reader who stops
    # after this section has the findings that do not move between runs.
    identity, ranked, collisions = without_the_store(loaded)
    total_said = sum(len(said) for _sid, _r, said, _n in loaded)

    print("WHAT IS TRUE BEFORE ANYTHING IS RANKED - read from disk, so this")
    print("half does not move between runs the way the half below does:")
    print("")
    print("  A SKILL'S WORDS A FRAGMENT ALSO DECLARES (%d of %d) - these resolve"
          % (len(identity), total_said))
    print("  by IDENTITY, so the answer is fixed and not a ranked guess:")
    if not identity:
        print("    none")
    for sid, srisk, phrase, fid, capability, frisk, declared in identity:
        print("    %-5s %-18s %-7s %-38s -> %s %s %s"
              % ("ok" if declared else "OUT", sid, srisk, phrase[:38],
                 fid, capability, frisk))
    if any(not d for *_x, d in identity):
        print("")
        print("    An OUT is a GUARANTEED disagreement, not a probable one: the")
        print("    skill declared one plan and the library answers that sentence")
        print("    with a capability outside it, every time, by the route that")
        print("    does not rank.")

    print("")
    print("  A SKILL'S WORDS NO FRAGMENT DECLARES (%d of %d) - identity cannot"
          % (len(ranked), total_said))
    print("  fire, so RANKING decides, and ranking is the thing that answers a")
    print("  question with a write (FRAGMENT-ISSUES rows 113 and 116):")
    for sid, srisk, phrase in ranked:
        print("    %-18s %-7s %s" % (sid, srisk, phrase))

    print("")
    print("  ONE SENTENCE CLAIMED BY TWO SKILLS (%d):" % len(collisions))
    if not collisions:
        print("    none")
    for key, claimants in collisions:
        print("    %-44s %s" % (key[:44], ", ".join(
            "%s (%s)" % (sid, risk) for sid, risk in claimants)))
    print("")

    import heron_brain as brain

    # READ IT BEFORE ASKING ANYTHING. The sibling tool measured that a sweep
    # moves the store it is measuring, so a fingerprint taken afterwards would
    # describe an index this run had already perturbed.
    index = _index_fingerprint()

    reaches, misses, crossings = [], [], []
    view_changes, escalations, unresolved = [], [], []
    # WHAT THE RETRIEVER SAID ABOUT ITS OWN CONFIDENCE, collected beside the
    # five lists rather than folded into them: an unsettled answer can be in
    # ANY of them, and a sixth bucket would make a reader choose.
    nothing_found = []
    asked = 0

    for sid, srisk, said, needs in loaded:
        for phrase in said:
            asked += 1
            try:
                answer = brain.lookup(phrase, revit=args.revit)
            except Exception as why:                           # noqa: BLE001
                unresolved.append((sid, phrase,
                                   "%s: %s" % (type(why).__name__, why)))
                continue

            capability = answer.get("capability")
            if not capability:
                unresolved.append((sid, phrase, "no capability"))
                continue

            risk = (answer.get("risk") or "").upper()
            route = answer.get("route") or "?"
            gap = margin(answer)
            row = (sid, srisk, phrase, capability, risk, route, gap, needs)

            # THE CROSSING TEST, AND IT IS DECIDED BY TWO DECLARATIONS.
            # Both are written down - the skill's own risk and the resolved
            # capability's - so `classify` makes no judgement about phrasing.
            # It lives above this loop because `tools/prove-skill.py` asks the
            # same question and must not answer it differently.
            where = classify(srisk, risk, capability, needs)
            {"crossing": crossings, "view": view_changes,
             "escalation": escalations, "reach": reaches,
             "miss": misses}[where].append(row)

            # AND WHAT THE RETRIEVER ITSELF SAID ABOUT IT. Read from the
            # `note` it already returns, never re-derived (row 137).
            told = unsettled(answer.get("note"))
            if told:
                nothing_found.append((sid, phrase, capability, risk, where,
                                      told))

    print("Revit %s   skills: %d   utterances asked: %d"
          % (args.revit, len(loaded), asked))
    print("")
    print("THE INDEX THIS RAN AGAINST - compare it before comparing counts:")
    for line in fingerprint_lines(index):
        print(line)
    print("")

    def show(row):
        sid, srisk, phrase, capability, risk, route, gap, needs = row
        winner, runner = gap
        said = ("  %-18s %-7s %-40s -> %-26s %-7s %s"
                % (sid, srisk, phrase[:40], capability, risk, route))
        # The margin, in the only terms this seam gives - see margin().
        said += "\n  %-18s won on %s" % ("", winner)
        if runner:
            said += "; runner-up %s" % runner
        return said

    print("A SKILL THAT ASKS, ANSWERED BY SOMETHING THAT WRITES (%d):"
          % len(crossings))
    if not crossings:
        print("  none")
    for row in crossings:
        print(show(row))
        if row[3] in row[7]:
            print("  %-18s and the skill DECLARED it in `needs` - so this is "
                  "the skill's own plan," % "")
            print("  %-18s not a retrieval accident. The declaration is the "
                  "defect." % "")

    if crossings:
        print("")
        print("  These are FRAGMENT-ISSUES rows 109, 113 and 116 at the skill")
        print("  layer. A caller acting on one does not get a poor answer to")
        print("  its question; it CHANGES THE MODEL in reply to one.")

    print("")
    print("A SKILL THAT ASKS, ANSWERED BY A VIEW CHANGE (%d) - read them, "
          "not failures:" % len(view_changes))
    if not view_changes:
        print("  none")
    for row in view_changes:
        print(show(row))

    print("")
    print("REACHED HIGHER UP THE LADDER THAN THE SKILL DECLARES (%d) - nobody"
          % len(escalations))
    print("asked a question, so these are not crossings; the request still")
    print("escalated past the risk the skill owns:")
    if not escalations:
        print("  none")
    for row in escalations:
        print(show(row))

    print("")
    print("REACHED A CAPABILITY THE SKILL DECLARED (%d):" % len(reaches))
    if not reaches:
        print("  none")
    for row in reaches:
        print(show(row))

    print("")
    print("REACHED SOMETHING THE SKILL NEVER ASKED FOR (%d) - a judgement:"
          % len(misses))
    if not misses:
        print("  none")
    for row in misses:
        print(show(row))

    print("")
    print("THE RETRIEVER SAID IT FOUND NOTHING, IN ITS OWN WORDS (%d) - and"
          % len(nothing_found))
    print("this cuts ACROSS the five lists above, which is the point. A")
    print("crossing is decided by comparing the sentence's reach against the")
    print("SKILL's declared risk, so a question sitting inside a MODIFY skill")
    print("can never register as one (row 137). This does not ask about risk")
    print("at all: it reports what `heron_retrieve.Contest` already measured")
    print("and `lookup` already carried back in `note`, which every sweep")
    print("including this one used to throw away.")
    if not nothing_found:
        print("  none - and that is the ABSENCE of a complaint, not a clean")
        print("  sweep (D-52). The retriever says these things when it can;")
        print("  silence is not a claim that the answers are good.")
    for sid, phrase, capability, risk, where, told in nothing_found:
        print("  %-18s %-40s -> %-26s %-7s (%s)"
              % (sid, phrase[:40], capability, risk, where))
        for tag in told:
            print("  %-18s %s" % ("", tag))

    if unresolved:
        print("")
        print("NOT RESOLVED (%d):" % len(unresolved))
        for sid, phrase, why in unresolved:
            print("  %-18s %-44s %s" % (sid, phrase[:44], why))

    print("")
    print("BY SKILL - the answer to 'which skills understand correctly':")
    for sid, srisk, said, needs in loaded:
        mine = [r for r in reaches if r[0] == sid]
        bad = [r for r in crossings if r[0] == sid]
        other = [r for r in misses if r[0] == sid]
        up = [r for r in escalations if r[0] == sid]
        view = [r for r in view_changes if r[0] == sid]
        lost = [u for u in unresolved if u[0] == sid]
        print("  %-18s %-7s %d/%d reach declared needs%s"
              % (sid, srisk, len(mine), len(said),
                 ("   CROSSINGS: %d" % len(bad)) if bad else ""))
        if other or view or lost or up:
            print("  %-18s          %s" % ("", "; ".join(filter(None, [
                "%d elsewhere" % len(other) if other else "",
                "%d ESCALATED" % len(up) if up else "",
                "%d to a view change" % len(view) if view else "",
                "%d unresolved" % len(lost) if lost else "",
            ]))))

    # DID ASKING CHANGE THE THING ASKED? Row 116 measured that running the
    # sibling sweep moved the store's md5 with no other writer. Saying so every
    # run is cheaper than somebody re-discovering it.
    if index.get("path") and index.get("md5"):
        after = _md5(index["path"])
        if after and after != index["md5"]:
            print("")
            print("THE STORE CHANGED WHILE THIS RAN:")
            print("  before   %s" % index["md5"])
            print("  after    %s" % after)
            print("  Asking moved the index. That is this tool, another")
            print("  session, or both - global.db is ONE file for every")
            print("  worktree on the machine. It is why a count from here is")
            print("  a sample rather than a measurement (row 116).")

    print("")
    print("Exit 0 whatever this finds, the same rule check-routing.py and")
    print("check-risk-crossings.py set. A crossing is a judgement a person")
    print("makes - and NOTHING here says a skill works. Understanding is half")
    print("a proof; the other half is a model (D-30).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
