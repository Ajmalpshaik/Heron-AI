# Heron-Agent:  HERON-SKL-VAL-004
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
A skill: what the user can ASK FOR, in their own words.

    python brain/heron_skill.py            validate them, and list the gaps

Step 14 of docs/27-build-order.md.

SKILL, FRAGMENT, RECIPE (D-29)
------------------------------
  Skill     what the user asks for, in BIM language. "Select all the ducts."
  Fragment  a composable piece of the how, with a declared contract
  Recipe    a bespoke multi-stage job that genuinely cannot be composed

A SKILL NAMES CAPABILITIES, NEVER FRAGMENTS
-------------------------------------------
This is Step 12's whole purpose reaching its customer. A skill says it needs
FILTER_ELEMENTS_BY_CATEGORY; it never says FRG-ELE-001. So a fragment can be
improved, replaced, split into three or retired, and not one skill is edited.

Which means a skill can be written BEFORE the fragment that will serve it -
and that is not a loophole, it is the point. A skill whose capability nobody
provides is exactly the capability gap docs/18 wants visible, and writing the
skills first is how the gaps get FOUND rather than guessed at.

WHAT A SKILL HERE IS NOT
------------------------
It is not proven because it is written. Every skill starts DRAFT and stays
there until something has watched it work against a real model - the same bar
D-30 sets for fragments, for the same reason. `python tools/check-gaps.py`
counts them honestly.
"""

import copy
import hashlib
import io
import os
import re
import sys

try:
    import yaml
except ImportError:                                          # pragma: no cover
    import heron_install as INSTALL
    sys.stderr.write("Heron's brain needs PyYAML - in the Heron folder: %s\n"
                     % INSTALL.install_line("pip install --user pyyaml"))
    raise

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as FRAG                                 # noqa: E402
import heron_capability as CAP                               # noqa: E402
import heron_relpath as RELPATH                              # noqa: E402

SKILLS_DIR = os.path.join(ROOT, "brain", "skills")

REQUIRED = ("heron-status", "heron-step", "heron-since", "heron-layer",
            "id", "name", "domain", "purpose", "utterances", "needs",
            "preconditions", "risk", "revit")

# docs/09 section 2's [NOTE] asked for preconditions alongside utterances:
# "preconditions let Heron fail early with a useful message instead of failing
# deep inside a transaction". Required for the same reason utterances are - a
# skill that cannot say what must be true before it runs will discover it the
# expensive way.


class Skill(object):
    def __init__(self, data, path):
        self.data = data
        self.path = path

    @property
    def id(self):
        return self.data.get("id")

    @property
    def name(self):
        return self.data.get("name")

    @property
    def status(self):
        return self.data.get("heron-status")

    def utterances(self):
        return list(self.data.get("utterances", []) or [])

    def needs(self):
        """The capabilities this skill requires. Never fragment ids."""
        return list(self.data.get("needs", []) or [])

    def not_offered_by(self):
        """Needs this method USES but is never OFFERED through - see
        methods_for. Every one is in `needs` too (validate() refuses one that
        is not), so it is still served beside the method with its status;
        it only stops a request that reached it from naming this method.
        [] for a skill that says nothing, which is offered through every
        need, as before the field existed (row 5b-2090)."""
        return list(self.data.get("not-offered-by", []) or [])

    def offered_by(self):
        """The needs a request may link this method through - `needs` less
        `not-offered-by`, in the skill's own order."""
        held = set(self.not_offered_by())
        return [c for c in self.needs() if c not in held]

    @property
    def purpose(self):
        """The method, in the skill's own words - for a family, every step in
        the order it has to happen."""
        return self.data.get("purpose") or ""

    @property
    def risk(self):
        return self.data.get("risk")

    def preconditions(self):
        return list(self.data.get("preconditions", []) or [])

    def parts(self):
        """The purpose cut where the skill's `stages` says - a list of
        {"stage", "starts", "text"} in the purpose's own order, whose texts
        joined are the purpose exactly. [] for a skill that declares no
        stages, which is served whole as it always was.

        WHY A METHOD IS CUT AT ALL (the owner's choice, 2026-11-28, "One
        stage at a time"). family-creation's purpose grew to about 72,000
        characters on his family standard, and heron_method served it in one
        reply of about 82,000 - near what a host takes from one tool before
        it cuts the end off, and the end is the behaviour rules and the
        report. So a method this long is served as a summary first and one
        stage at a time after (heron_brain.method).

        THE CUTS ARE DECLARED, NEVER GUESSED FROM PROSE. Each entry of
        `stages` names the purpose line its part starts at, exactly as
        written (stripped of its indent); the part runs to the next entry's
        line. An entry with a `stage` is a stage, served on its own when
        asked by that id; one without goes back into the summary, as does
        everything before the first entry. A line that is missing, written
        twice or out of order is a ValueError - validate() reports it - so a
        heading reworded in the purpose cannot quietly fold one stage into
        its neighbour."""
        declared = self.data.get("stages")
        if not declared:
            return []
        if not isinstance(declared, list):
            raise ValueError("stages must be a list, not %r" % (declared,))
        purpose = self.purpose
        lines = purpose.splitlines(True)
        cuts, seen, previous = [], set(), -1
        for entry in declared:
            if not isinstance(entry, dict) or not isinstance(
                    entry.get("starts"), str) or not entry["starts"].strip():
                raise ValueError("every entry of stages names the purpose line "
                                 "it starts at as `starts`, not %r" % (entry,))
            starts = entry["starts"].strip()
            stage = entry.get("stage")
            stage = None if stage is None else str(stage).strip()
            if stage == "":
                raise ValueError("stage %r has an empty id" % starts)
            if stage is not None and stage.lower() in seen:
                raise ValueError("stage %s is declared twice" % stage)
            if stage is not None:
                seen.add(stage.lower())
            at = [i for i, line in enumerate(lines) if line.strip() == starts]
            if len(at) != 1:
                raise ValueError("stages: %r is %s of the purpose, so where its "
                                 "part starts cannot be known"
                                 % (starts, "not a line" if not at
                                    else "%d lines" % len(at)))
            if at[0] <= previous:
                raise ValueError("stages: %r comes before the line declared "
                                 "ahead of it - list them in the purpose's "
                                 "order" % starts)
            previous = at[0]
            cuts.append((at[0], stage, starts))
        if not seen:
            raise ValueError("stages declares no entry with a `stage`")
        found = []
        if cuts[0][0] > 0:
            found.append({"stage": None, "starts": None,
                          "text": "".join(lines[:cuts[0][0]])})
        for n, (at, stage, starts) in enumerate(cuts):
            end = cuts[n + 1][0] if n + 1 < len(cuts) else len(lines)
            found.append({"stage": stage, "starts": starts,
                          "text": "".join(lines[at:end])})
        return found

    def __repr__(self):
        return "<Skill %s %s>" % (self.id, self.status)


def load(path):
    """Read one skill file. Every failure leaves as a ValueError (D-48).

    The reason is load_all's, not this function's: it catches so that one
    unreadable skill costs one skill. An IOError escaping from here walked
    straight out of load_all and took the whole library with it - measured on
    2026-09-06 with a DIRECTORY named `x.yaml`, which load_all happily passed
    to io.open because it filtered on the extension and never asked whether the
    entry was a file. Two skills beside it, and it returned neither.

    THE PARSE IS KEPT WHILE THE BYTES ARE THE SAME, as heron_fragment.load()
    keeps a card's: heron_lookup asks which skills a request belongs to on
    every call, and parsing every skill each time is pure-Python YAML for
    nothing. The mark is heron_fragment.file_mark()'s, so an unchanged file is
    not even opened; a hit is a deep copy, so no caller changes the next read.
    """
    try:
        mark, raw = FRAG.file_mark(path)
        held = _PARSED.get(path)
        if held is not None and held[0] == mark:
            return Skill(copy.deepcopy(held[1]), path)
        if raw is None:
            with io.open(path, "rb") as handle:
                raw = handle.read()
            mark = hashlib.blake2b(raw, digest_size=16).digest()
        data = yaml.safe_load(raw.decode("utf-8"))
    except yaml.YAMLError as exc:
        raise ValueError("could not be parsed - %s" % " ".join(str(exc).split()))
    except UnicodeDecodeError as exc:
        raise ValueError("could not be read - %s" % " ".join(str(exc).split()))
    except (IOError, OSError) as exc:
        raise ValueError("could not be read - %s" % " ".join(str(exc).split()))
    if not isinstance(data, dict):
        raise ValueError("%s is not a mapping" % path)
    _PARSED[path] = (mark, copy.deepcopy(data))
    return Skill(data, path)


#: path -> (blake2b of the file's bytes, parsed mapping). See load().
_PARSED = {}


def load_all(root=None):
    root = root or SKILLS_DIR
    found, problems = {}, []
    if not os.path.isdir(root):
        return found, problems

    for name in sorted(os.listdir(root)):
        if not name.endswith((".yaml", ".yml")):
            continue
        path = os.path.join(root, name)
        if not os.path.isfile(path):
            # A directory named `x.yaml` is not a skill. This is checked rather
            # than assumed because the extension filter above cannot tell.
            problems.append("%s: not a file, so it is not a skill" % name)
            continue
        try:
            skill = load(path)
        except (ValueError, yaml.YAMLError) as exc:
            problems.append("%s: %s" % (name, exc))
            continue
        except Exception as exc:                     # noqa: BLE001 - deliberate
            # D-48: one broken skill costs one skill. A bare except is normally
            # a smell; here it IS the guarantee, and it is commented so it is
            # not tidied away by someone who reads it as laziness.
            problems.append("%s: unreadable - %s: %s"
                            % (name, type(exc).__name__,
                               " ".join(str(exc).split())))
            continue
        if not skill.id:
            problems.append("%s: no id" % name)
            continue
        if skill.id in found:
            problems.append("%s: id %s already used" % (name, skill.id))
            continue
        found[skill.id] = skill
    return found, problems


def validate(skill):
    problems = []
    where = os.path.basename(skill.path)

    for field in REQUIRED:
        if skill.data.get(field) in (None, "", [], {}):
            problems.append("%s: missing %s" % (where, field))

    if skill.status and skill.status not in FRAG.STATUSES:
        problems.append("%s: heron-status %r is not a lifecycle state"
                        % (where, skill.status))

    # heron_capability's ladder, not a typed copy (PROPOSALS F22).
    if skill.data.get("risk") and skill.data["risk"] not in CAP.RISK_ORDER:
        problems.append("%s: risk %r is not a permission level"
                        % (where, skill.data["risk"]))

    said = skill.data.get("utterances")
    if said is not None and (not isinstance(said, list) or len(said) < 2):
        problems.append(
            "%s: a skill needs at least TWO utterances. One is a name; two is "
            "the beginning of knowing how somebody actually asks" % where)

    # EVERY PRECONDITION AND UTTERANCE IS TEXT (FRAGMENT-ISSUES 5b-1584). A line
    # with a colon and a space in it is a MAPPING to YAML, not a sentence:
    # sheets-from-excel's "Family: Type" title block line was served to the
    # chat as a dictionary, and nothing refused it. tests/test_skills.py read
    # the files for it; the loader's own check is this, so a method written
    # after that suite ran is refused too. Quote such a line.
    for field in ("preconditions", "utterances"):
        items = skill.data.get(field)
        if not isinstance(items, list):
            continue
        for item in items:
            if not isinstance(item, str):
                problems.append(
                    "%s: every one of its %s must be text, not %r - a line with "
                    "a colon and a space in it is read as a mapping; quote it"
                    % (where, field, item))
                break

    # THE RULE THIS FILE STATES IN CAPITALS, AND `isupper()` WAS NOT IT.
    #
    # "A SKILL NAMES CAPABILITIES, NEVER FRAGMENTS" is the whole reason a
    # fragment can be improved, replaced, split into three or retired with no
    # skill edited - and the test behind the refusal below was
    # `capability.isupper()`, which `FRG-ELE-001` passes. So `needs:
    # [FRG-ELE-001]` validated CLEAN, and `main()` then listed it under
    # CAPABILITY GAPS - the list it calls "what to build next, in the order
    # real work asks for it - not a guess". Naming a fragment produced an
    # instruction to go and build a capability called FRG-ELE-001.
    # FRAGMENT-ISSUES row 5b-106.
    #
    # THE PATTERN IS THE FRAGMENT SIDE'S OWN, imported rather than written
    # again here, so the two halves cannot come to disagree about what a
    # capability name looks like - which is how a rule ends up enforced in one
    # place and not the other. Measured before it was tightened: it accepts
    # 396 of 396 capabilities in the library and 0 of 396 fragment ids, every
    # one of which carries hyphens, and it refuses none of the requirements
    # the ten skills already declare.
    for capability in skill.needs():
        if (not isinstance(capability, str)
                or not FRAG.CAPABILITY_PATTERN.match(capability)):
            problems.append(
                "%s: needs must be CAPABILITY names in SCREAMING_SNAKE, not %r. "
                "A skill that names a fragment defeats the registry"
                % (where, capability))

    # `not-offered-by` NAMES NEEDS, AND ONLY NEEDS (row 5b-2090). An entry
    # that is not in `needs` holds nothing back - a misspelt one would leave
    # the method offered beside the very requests it was written to stay off,
    # and nothing would say so. Every need held back is still served.
    held = skill.data.get("not-offered-by")
    if held is not None:
        if not isinstance(held, list):
            problems.append("%s: not-offered-by must be a list of needs, not %r"
                            % (where, held))
        else:
            needs = set(skill.needs())
            for capability in held:
                if not isinstance(capability, str) or capability not in needs:
                    problems.append(
                        "%s: not-offered-by names %r, which is not one of its "
                        "needs - it can only hold back a capability the method "
                        "uses, so it holds back nothing" % (where, capability))
            if needs and needs <= set(c for c in held if isinstance(c, str)):
                problems.append(
                    "%s: not-offered-by holds back every need, so no request "
                    "could ever be offered this method" % where)

    # A METHOD SERVED IN STAGES MUST CUT WHERE IT SAYS (Skill.parts): a
    # heading reworded in the purpose and not in `stages` is refused here,
    # not discovered as a stage that swallowed the next one.
    try:
        skill.parts()
    except ValueError as exc:
        problems.append("%s: %s" % (where, exc))

    unknown = [v for v in (skill.data.get("revit") or [])
               if str(v) not in FRAG.REVIT_VERSIONS]
    if unknown:
        problems.append("%s: revit names releases Heron does not know: %s"
                        % (where, ", ".join(str(u) for u in unknown)))
    return problems


# ---------------------------------------------------------------------------
# Which jobs a request is one step of
# ---------------------------------------------------------------------------

# Words every request has and no job is told apart by. Asking to MAKE or
# CREATE something is the request, not the subject of it.
_PLAIN = frozenset((
    "a an the this that these those it its my our your me i we you to of for "
    "in on at with from and or all please can could would will be is are do "
    "does make create build new add put get one two three four five six "
    "seven eight nine ten").split())


def _words(text):
    """The words of `text` that can tell one job from another, lower case and
    singular - "families" is "family", "ducts" is "duct", "glass" stays."""
    found = []
    for word in re.findall(r"[a-z0-9]+", (text or "").lower()):
        # A PLAIN WORD IS DROPPED BEFORE IT IS MADE SINGULAR (row 5b-650):
        # "this" became "thi" and "does" "doe", which no list held, so every
        # request saying "this" shared a word with every method saying it.
        if word in _PLAIN:
            continue
        if len(word) > 4 and word.endswith("ies"):
            word = word[:-3] + "y"
        elif len(word) > 3 and word.endswith("s") and not word.endswith("ss"):
            word = word[:-1]
        if word not in _PLAIN:
            found.append(word)
    return found


def methods_for(skills, request, capabilities, limit=2, ceiling=None):
    """The skills `request` is one step of, best first - at most `limit`.

    WHY THIS EXISTS. A family is not one capability, it is a method of fifteen
    steps in an order that matters, and that method lives in a skill's
    `purpose`. heron_lookup answered "create a family" with ONE capability -
    CREATE_FAMILY_SWEPT_BLEND, a ranked guess - and nothing a tool returned
    named the method. A chat with no memory of Heron then read the skill file
    and docs/43 off disk to work it out: minutes on a new PC, where the old one
    remembered.

    WHAT LINKS A SKILL TO A REQUEST IS DATA, NOT A GUESS ABOUT MEANING. A
    skill is offered only when it NEEDS one of `capabilities` - what retrieval
    already returned, and not one its `not-offered-by` holds back - and shares at least one word with the request in its
    name or its utterances. The words only ORDER what that link allows: the
    most shared words first, then how often they recur in the skill's own
    phrasing - which puts the general family method ahead of the special ones
    it sends to - then the most of the request's capabilities it uses. That
    last is the weakest sign, because a ranked guess's runners-up are where
    retrieval is noisiest. The host still decides what the user meant (D-01);
    this names the methods so it does not have to read them off disk.

    A NEED A METHOD ONLY USES ALONG THE WAY DOES NOT OFFER IT (row 5b-2090).
    One shared need is enough to link, so a method that uses many tools was
    reachable from a request for any of them: family-creation cuts a section
    to check the family, draws a duct and makes a schedule in its project
    test, deletes an imported underlay and remakes a connector - and with
    those in `needs`, "delete this duct", "make a schedule of duct
    sizes", "create a section through these ducts" and "draw a duct from here
    to there" each named the family method, through the one word "duct" its
    air-terminal utterance says. Its builder had left the five out of `needs`
    to stop it, so the method served a shorter list of what it uses than its
    stages name. A skill's `not-offered-by` lists those needs: still in
    `needs`, still served with their status, never a link. A skill without it
    links through every need, as before. Measured 2026-11-28 against general
    rules that need no field: linking only through the capability the lookup
    ranked first changed the methods named for hundreds of requests, left
    dozens of methods unnamed for their own utterances, and still named the
    family method beside three of the four - its link there IS the first
    capability; counting only a word the method says twice, or only a word of
    its name or id, did no better. What a method merely uses along the way is a
    fact about the method, so it is the method's to say.

    `ceiling` IS D-128's, AND A METHOD IS HELD TO IT AS A CAPABILITY IS (row
    5b-650). A request the host calls a question is answered under the
    highest risk a question may be - `heron_brain.question_ceiling()` - and
    every write was already left out of its candidates; the methods were
    not, so "how many sprinklers are in this room", asked as a question, was
    named sprinkler-layout and terminal-layout, both MODIFY. A method whose
    declared risk is above `ceiling` is left out; one with no risk a
    permission level knows is left out too, because nothing says it is
    below. None - not a question - leaves every method in, as before.
    """
    if ceiling is not None:
        top = CAP.RISK_ORDER.index(ceiling)
        skills = [s for s in skills if s.risk in CAP.RISK_ORDER
                  and CAP.RISK_ORDER.index(s.risk) <= top]
    asked = set(_words(request))
    wanted = [c for c in capabilities if c]
    if not asked or not wanted:
        return []
    ranked = []
    for skill in skills:
        # Through the needs it may be offered by, never `not-offered-by`
        # (row 5b-2090) - a skill without that field links through them all.
        needs = set(skill.offered_by())
        linked = len([c for c in set(wanted) if c in needs])
        if not linked:
            continue
        said = _words(" ".join([skill.name or "", (skill.id or "").replace(
            "-", " ")] + [str(u) for u in skill.utterances()]))
        shared = asked.intersection(said)
        if not shared:
            continue
        recur = len([w for w in said if w in shared])
        ranked.append(((-len(shared), -recur, -linked, skill.id or ""), skill))
    ranked.sort(key=lambda pair: pair[0])
    return [skill for _key, skill in ranked[:limit]]


def main():
    found, problems = load_all()
    # SKILLS_DIR is a folder a suite repoints at scratch, which on Windows can
    # sit on another drive than ROOT - os.path.relpath raises there; this
    # display line answers with the whole path instead (row 5b-2086).
    print("Skills in %s" % RELPATH.relpath(SKILLS_DIR, ROOT))
    print()

    if not found and not problems:
        print("None yet.")
        return 0

    fragments, _ = FRAG.load_all()
    # A retired card provides nothing a method may rely on - heron_capability.gaps.
    retired = FRAG.STATUSES[FRAG.STATUSES.index("DEPRECATED"):]
    provided = set(f.data.get("capability") for f in fragments.values()
                   if f.status not in retired)

    wanted = {}
    for skill in sorted(found.values(), key=lambda s: s.id):
        broken = validate(skill)
        problems.extend(broken)
        missing = [c for c in skill.needs() if c not in provided]
        mark = "FAIL" if broken else ("gap" if missing else "ok")
        print("  %-5s %-26s %-46s %s"
              % (mark, skill.id, skill.name,
                 "needs %d capability(ies)" % len(skill.needs())))
        for capability in missing:
            wanted.setdefault(capability, []).append(skill.id)

    print()
    print("  %d skill(s), all DRAFT until something watches them work" % len(found))

    if wanted:
        print()
        print("  CAPABILITY GAPS - what these skills need and nothing provides:")
        for capability in sorted(wanted):
            print("    %-34s wanted by %s"
                  % (capability, ", ".join(sorted(wanted[capability]))))
        print()
        print("  That list is the point of writing skills first. It is what to")
        print("  build next, in the order real work asks for it - not a guess.")

    if problems:
        print()
        for line in problems:
            print("  %s" % line)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
