# Heron-Agent:  HERON-SKL-VAL-004
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 14 - skills, and the capability gaps writing them reveals.

    python tests/test_skills.py

WHAT IT PROVES
  1. Every skill is well-formed, and names CAPABILITIES rather than fragments -
     which is what lets a fragment be replaced without editing a skill.
  2. A skill's capabilities resolve to real providers, or the shortfall is a
     reported GAP rather than a silent failure at run time.
  3. Writing skills first PRODUCES the build queue: what to build next, ordered
     by how many real jobs want it, rather than by guess.

WHAT IT DOES NOT PROVE
  Nothing about whether a skill works. All ten are DRAFT and not one has met a
  model. Phase 2's definition of done is "ten real skills work" and the word
  that is not satisfied here is WORK.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    home = tempfile.mkdtemp(prefix="heron-skills-")
    os.environ["HERON_KNOWLEDGE"] = home

    import heron_skill as SKILL
    import heron_fragment as FRAG
    import heron_scope as SCOPE
    import heron_capability as CAP
    import heron_graph as GRAPH

    try:
        print("1. Every skill is well-formed")
        skills, problems = SKILL.load_all()
        check(not problems, "no load problems%s"
              % ("" if not problems else ": " + problems[0]))
        check(len(skills) >= 10,
              "at least ten skills, which is Phase 2's own target (%d)"
              % len(skills))
        for skill in sorted(skills.values(), key=lambda s: s.id):
            broken = SKILL.validate(skill)
            check(not broken, "%s is well-formed%s"
                  % (skill.id, "" if not broken else ": " + broken[0]))

        # EVERY PRECONDITION AND UTTERANCE IS TEXT (FRAGMENT-ISSUES 5b-1584). A
        # line with a colon and a space in it is a mapping to YAML, not a
        # sentence: sheets-from-excel's title block line was served to the chat
        # as a Python dict, and nothing failed. Read from each file as YAML
        # reads it, so the check needs nothing from the loader.
        import glob
        import yaml
        for path in sorted(glob.glob(os.path.join(ROOT, "brain", "skills", "*.yaml"))):
            with io.open(path, encoding="utf-8") as fh:
                data = yaml.safe_load(fh) or {}
            for key in ("preconditions", "utterances"):
                items = data.get(key) if isinstance(data, dict) else None
                odd = [item for item in (items or []) if not isinstance(item, str)]
                check(not odd, "%s: every one of its %s is text%s"
                      % (os.path.basename(path), key,
                         "" if not odd else " - not %r" % (odd[0],)))

        # AND THE LOADER'S OWN validate() REFUSES IT, so a method written after
        # this suite ran is refused too - planted here, as YAML reads it.
        first = sorted(skills.values(), key=lambda s: s.id)[0]
        for key in ("preconditions", "utterances"):
            planted = dict(first.data)
            planted[key] = list(first.data.get(key) or ["a sentence", "another"]) + [
                {"a title block named like": "Family: Type"}]
            refused = SKILL.validate(SKILL.Skill(planted, first.path))
            check(any(key in p and "must be text" in p for p in refused),
                  "validate() refuses a %s line YAML read as a mapping (5b-1584)" % key)

        print()
        print("2. A skill names CAPABILITIES, never fragments")
        fragments, _ = FRAG.load_all()
        ids = set(fragments)
        for skill in skills.values():
            named = [c for c in skill.needs() if c in ids]
            check(not named,
                  "%s names no fragment id%s"
                  % (skill.id, "" if not named else ": " + ", ".join(named)))
        check(all(c.isupper() for s in skills.values() for c in s.needs()),
              "every requirement is a CAPABILITY name")

        print()
        print("3. Capabilities resolve, or the shortfall is a reported GAP")
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            CAP.rebuild(store)
            for skill in skills.values():
                for capability in skill.needs():
                    GRAPH.skill_needs(store, skill.id, capability)
                    CAP.want(store, capability,
                             "needed by skill %s" % skill.id)

            gaps = dict(CAP.gaps(store))
            covered = [s for s in skills.values()
                       if not any(c in gaps for c in s.needs())]
            check(covered,
                  "%d skill(s) have every capability provided: %s"
                  % (len(covered), ", ".join(sorted(s.id for s in covered))))
            # EVERY capability the ten skills need is now provided - the seven
            # that were missing were written on 2026-08-29. So the real library
            # can no longer demonstrate a gap, and asserting that it has one
            # would be asserting that the work is unfinished.
            #
            # What must still be proven is the MECHANISM: that a capability
            # nobody provides is REPORTED rather than discovered at run time.
            # Tested with a name nothing will ever provide, so this check keeps
            # meaning whatever the library does next.
            check(len(covered) == len(skills),
                  "ALL %d skill(s) have every capability provided" % len(skills))
            CAP.want(store, "NO_SUCH_CAPABILITY_EXISTS",
                     "asked for by this test, and provided by nothing")
            invented = dict(CAP.gaps(store))
            check("NO_SUCH_CAPABILITY_EXISTS" in invented,
                  "a capability nobody provides is REPORTED as a gap - the "
                  "absence IS the finding, with no second list to keep in step")

            # A RETIRED CARD PROVIDES NOTHING. When a card is merged into
            # another and DEPRECATED, a method still naming its capability
            # must show as a gap - not stay green while it quietly runs the
            # retired card (found 2026-11-01).
            store.execute(
                "INSERT OR REPLACE INTO fragments (id, capability, "
                "semantic_identity, kind, status, domain, risk, folder, revit) "
                "VALUES ('FRG-QA-998','ONLY_A_RETIRED_CARD_DOES_THIS',"
                "'a job only a retired card does','action','DEPRECATED','test',"
                "'READ','x','2024')")
            store.db.commit()
            CAP.want(store, "ONLY_A_RETIRED_CARD_DOES_THIS",
                     "asked for by this test, provided only by a DEPRECATED card")
            retired_only = dict(CAP.gaps(store))
            check("ONLY_A_RETIRED_CARD_DOES_THIS" in retired_only,
                  "a capability only a DEPRECATED card provides is a GAP - a "
                  "method needing it must be repointed to the card it merged into")
            store.execute("DELETE FROM fragments WHERE id = 'FRG-QA-998'")
            store.db.commit()

            for skill in covered:
                for capability in skill.needs():
                    provider = CAP.best_provider(store, capability)
                    check(provider is not None,
                          "%s -> %s resolves to %s"
                          % (skill.id, capability, provider))

            print()
            print("4. Writing skills first PRODUCES the build queue")
            # The queue is EMPTY now, and that is the queue having been worked
            # rather than the mechanism being broken. What is still worth
            # proving is that it would be ORDERED BY REAL DEMAND if it were not
            # empty - so it is built over every capability the skills ask for,
            # which is the same arithmetic the gap queue used.
            demand = {}
            for skill in skills.values():
                for capability in skill.needs():
                    demand.setdefault(capability, []).append(skill.id)
            ordered = sorted(demand.items(), key=lambda kv: (-len(kv[1]), kv[0]))
            outstanding = [(c, w) for c, w in ordered if c in gaps]
            check(not outstanding,
                  "nothing is outstanding - every capability the skills ask "
                  "for has a provider")
            check(ordered and len(ordered[0][1]) >= 2,
                  "and demand is still counted per capability - %s is wanted "
                  "by %d skills, which is what ordered the queue while there "
                  "was one" % (ordered[0][0], len(ordered[0][1])))
            for capability, wanters in ordered[:4]:
                print("        %-28s %d skill(s)" % (capability, len(wanters)))

            print()
            print("5. The graph reaches skills through capabilities")
            impact = GRAPH.impact(store, "FRG-ELE-001")
            affected = [s for s, _c in impact["skills_affected"]]
            check(len(affected) >= 3,
                  "changing the category filter touches %d skill(s), none of "
                  "which named it: %s" % (len(affected),
                                          ", ".join(sorted(set(affected))[:4])))
        finally:
            store.close()

        print()
        print("6. Every skill is DRAFT, and that is not an oversight")
        check(all(s.status == "DRAFT" for s in skills.values()),
              "not one is claimed as proven - none has met a model")
    finally:
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    print()
    print("7. D-48 - one unreadable skill costs ONE skill, never the library")
    room = tempfile.mkdtemp(prefix="heron-skills-d48-")
    try:
        io.open(os.path.join(room, "aaa-good.yaml"), "w",
                encoding="utf-8").write("id: SKL-ISO-001\nname: first good\n")
        # A DIRECTORY carrying the .yaml extension. load_all filtered on the
        # extension and never asked whether the entry was a file, so io.open
        # raised PermissionError straight out of the loader.
        os.makedirs(os.path.join(room, "bbb-a-directory.yaml"))
        io.open(os.path.join(room, "ccc-broken.yaml"), "w",
                encoding="utf-8").write("id: SKL-ISO-002\nname: [unclosed\n  x: : :\n")
        io.open(os.path.join(room, "ddd-good.yaml"), "w",
                encoding="utf-8").write("id: SKL-ISO-003\nname: second good\n")

        raised = None
        try:
            found_d, problems_d = SKILL.load_all(room)
        except Exception as exc:                              # noqa: BLE001
            raised, found_d, problems_d = exc, {}, []

        check(raised is None,
              "load_all survives both a directory named .yaml and a bad parse")
        check(sorted(found_d) == ["SKL-ISO-001", "SKL-ISO-003"],
              "both good skills load, including the one AFTER the two failures")
        check(any("not a file" in p for p in problems_d),
              "the directory is named as a problem rather than swallowed")
        check(any("could not be parsed" in p for p in problems_d),
              "the malformed one is named too")
    finally:
        shutil.rmtree(room, ignore_errors=True)

    print()
    print("8. A FRAGMENT ID IS REFUSED AS A CAPABILITY NAME")
    # ROW 5b-106. The module states its central rule in capitals - "A SKILL
    # NAMES CAPABILITIES, NEVER FRAGMENTS ... So a fragment can be improved,
    # replaced, split into three or retired, and not one skill is edited" -
    # and validate() carried the matching refusal word for word. The test
    # behind that sentence was `capability.isupper()`, and a fragment id
    # passes it: 'FRG-ELE-001'.isupper() is True.
    #
    # SECTION 2 ABOVE COULD NOT CATCH IT EITHER, and that is the same shape as
    # row 5b-102: every case ever put through the check is a real capability
    # out of the real library, so nothing ever handed it the thing it exists
    # to refuse. This hands it one.
    check("FRG-ELE-001".isupper(),
          "a fragment id IS upper case - which is why isupper() could never "
          "have been the test, and why this section exists")

    import copy as _copy
    _real = sorted(SKILL.load_all()[0].values(), key=lambda s_: s_.id)[0]

    def _needing(what):
        data = _copy.deepcopy(_real.data)
        data["needs"] = [what]
        return SKILL.validate(SKILL.Skill(data, _real.path))

    def _refused(what):
        return any("needs must be" in p for p in _needing(what))

    check(_refused("FRG-ELE-001"),
          "a fragment id in `needs` is refused - it validated CLEAN, and "
          "main() then listed it under CAPABILITY GAPS, which that tool calls "
          "'what to build next, in the order real work asks for it - not a "
          "guess'. So naming a fragment produced an instruction to go and "
          "build a capability called FRG-ELE-001")
    check(_refused("frg-ele-001") and _refused(123),
          "and the two it already caught - a lowercase name and a non-string "
          "- are caught still")

    # AND NOTHING THAT IS A REAL CAPABILITY IS REFUSED. Measured before the
    # rule was tightened: 0 of the fragment ids match ^[A-Z][A-Z0-9_]*$
    # because every one carries hyphens, and every capability does match.
    _frags, _ = FRAG.load_all()
    _caps = sorted(set(f.data.get("capability") for f in _frags.values()
                       if f.data.get("capability")))
    _wrongly = [c for c in _caps if _refused(c)]
    check(not _wrongly,
          "while every capability the library actually provides is accepted - "
          "checked against all %d of them, not against an example%s"
          % (len(_caps),
             "" if not _wrongly else ": " + ", ".join(_wrongly[:5])))

    _declared = sorted(set(c for s_ in SKILL.load_all()[0].values()
                           for c in s_.needs()))
    _broken = [c for c in _declared if _refused(c)]
    check(not _broken,
          "and so is every requirement the %d skill(s) already declare - %d "
          "distinct, and the rule refuses none of them%s"
          % (len(SKILL.load_all()[0]), len(_declared),
             "" if not _broken else ": " + ", ".join(_broken[:5])))

    print()
    print("9. A METHOD NEVER CALLS A CARD PROVEN THAT IS NOT")
    # ROW 5b-1957. heron_method serves a method's purpose and preconditions
    # whole, and a step written "LIST_LINKED_MODELS (PROVEN)" is read as the
    # card's status. Four methods said it of seven cards that were DRAFT -
    # sent back when their C# moved, or never proved - so a host was told a
    # change would go through revit_change, which refuses a DRAFT (D-117).
    # Only this direction is held: a card sent back to DRAFT turns this red
    # and names the method that still calls it PROVEN, while a card that is
    # signed never does - a step that undersells a card is stale, not unsafe.
    import re as _re9
    _proven9 = set(f.data.get("capability") for f in _frags.values()
                   if f.data.get("heron-status") == "PROVEN")
    _said9 = _re9.compile(r"\b([A-Z][A-Z0-9]+(?:_[A-Z0-9]+)+)\s*\(\s*PROVEN\b")
    _methods9 = SKILL.load_all()[0]
    for _s9 in sorted(_methods9.values(), key=lambda s_: s_.id):
        _text9 = _re9.sub(r"\s+", " ", "%s %s" % (
            _s9.purpose, " ".join(str(p) for p in _s9.preconditions())))
        _claims9 = set(_said9.findall(_text9))
        _wrong9 = sorted(c for c in _claims9 if c not in _proven9)
        if _claims9:
            check(not _wrong9,
                  "%s calls PROVEN only cards that are (%d named)%s"
                  % (_s9.id, len(_claims9),
                     "" if not _wrong9 else " - not: " + ", ".join(_wrong9)))
    # AND IT CAN FAIL: a method planted to call a DRAFT card PROVEN.
    _draft9 = sorted(set(f.data.get("capability") for f in _frags.values()
                         if f.data.get("heron-status") == "DRAFT"
                         and f.data.get("capability")) - _proven9)
    check(_draft9 and not set(_said9.findall(
        "0. %s (PROVEN, version 2): a step" % _draft9[0])) <= _proven9,
          "a step calling a DRAFT card PROVEN is caught (%s planted)"
          % (_draft9[0] if _draft9 else "no DRAFT card to plant"))

    print()
    print("10. A METHOD NEVER SERVES A GAP OR A WAIT A CARD HAS CLOSED")
    # ROW 5b-1981. The other direction to section 9, held sentence by
    # sentence rather than by a rule, so that signing a card never turns
    # this red. Each sentence below was served by heron_method after the
    # card beside it had done the job: replace-in-place-family said nothing
    # writes a value by GUID while SET_PARAMETER_VALUES_BY_ID had taken
    # "Name {GUID}" since its version 6 (row 5b-1344, 2026-10-28), and told
    # the chat not to try; room-setup listed numbering the rooms as a gap
    # "until RENUMBER_SEQUENTIAL is proved" after its proof of 2026-11-14;
    # reinforce-concrete called CHECK_REBAR_AGAINST_SPEC a DRAFT read after
    # its proof of 2026-11-17; drawing-set's step 5 began a status sentence
    # it never finished. Each check also asks that the card is still in the
    # method's needs, so its real status is served beside the steps.
    _closed10 = (
        ("replace-in-place-family", "GAP - A GUID WRITE",
         "SET_PARAMETER_VALUES_BY_ID"),
        ("replace-in-place-family", "ONE THING THIS JOB NEEDS DOES NOT EXIST",
         "SET_PARAMETER_VALUES_BY_ID"),
        ("room-setup", "a gap until RENUMBER_SEQUENTIAL is proved",
         "RENUMBER_SEQUENTIAL"),
        ("reinforce-concrete", "A DRAFT READ, CHECK_REBAR_AGAINST_SPEC",
         "CHECK_REBAR_AGAINST_SPEC"),
        ("drawing-set", "Its status: TAG_ROOMS", "TAG_ELEMENTS_IN_VIEW"),
    )
    _methods10 = SKILL.load_all()[0]

    def _served10(sid):
        s_ = _methods10.get(sid)
        if s_ is None:
            return None, ()
        return (" ".join(("%s %s" % (s_.purpose, " ".join(
            str(p) for p in s_.preconditions()))).split()), s_.needs())

    def _clean10(text, needs, stale, cap):
        return text is not None and stale not in text and cap in needs

    for _sid10, _stale10, _cap10 in _closed10:
        _text10, _needs10 = _served10(_sid10)
        check(_clean10(_text10, _needs10, _stale10, _cap10),
              "%s no longer serves \"%s\", and needs %s"
              % (_sid10, _stale10, _cap10))
    # The GUID write is named where the job needs it: step 7 writes a value
    # by "Name {GUID}" through the card that takes that form. Stage 7 is the
    # old step 7 since the method was put in stages (2026-11-28).
    import re as _re10
    _replace10, _ = _served10("replace-in-place-family")
    _step7 = (_replace10 or "").partition(" STAGE 7 - ")[2].partition(
        " STAGE 8 - ")[0]
    check("SET_PARAMETER_VALUES_BY_ID" in _step7
          and _re10.search(r"\{[^}]*GUID[^}]*\}", _step7) is not None,
          "replace-in-place-family Stage 7 writes a value by \"Name {GUID}\" "
          "through SET_PARAMETER_VALUES_BY_ID")
    # AND IT CAN FAIL: the stale sentence planted back is caught.
    _sid10, _stale10, _cap10 = _closed10[0]
    _text10, _needs10 = _served10(_sid10)
    check(not _clean10("%s 7. %s 8." % (_text10, _stale10), _needs10,
                       _stale10, _cap10)
          and not _clean10(_text10, [n for n in _needs10 if n != _cap10],
                           _stale10, _cap10),
          "a closed gap planted back, or its card taken out of needs, is "
          "caught (%s)" % _sid10)

    print()
    print("11. A METHOD SERVED IN STAGES CUTS WHERE IT SAYS, AND LOSES NOTHING")
    # The owner's choice, 2026-11-28 ("One stage at a time"): a method too
    # long for one reply declares `stages`, and heron_method serves a summary
    # and then one stage at a time (heron_skill.Skill.parts). Held here: the
    # parts joined ARE the purpose, byte for byte - so the summary and the
    # stages together can lose nothing - and every capability the method
    # needs is named in some stage, so its live status is served with the
    # stage that uses it. And a declaration that no longer fits its purpose
    # is refused, never served as one stage swallowing the next.
    import copy as _copy11
    import re as _re11
    _methods11 = SKILL.load_all()[0]
    _staged11 = sorted(s_.id for s_ in _methods11.values() if s_.data.get("stages"))
    check("family-creation" in _staged11,
          "family-creation declares its stages (%s)" % ", ".join(_staged11))
    _shaped11 = _re11.compile(r"\b[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)+\b")
    for _sid11 in _staged11:
        _s11 = _methods11[_sid11]
        _parts11 = _s11.parts()
        check("".join(p_["text"] for p_ in _parts11) == _s11.purpose,
              "%s: its %d parts joined are its purpose, byte for byte"
              % (_sid11, len(_parts11)))
        _stages11 = [p_ for p_ in _parts11 if p_["stage"] is not None]
        check(len(_stages11) >= 2 and all(
            p_["text"].lstrip().startswith(p_["starts"]) for p_ in _stages11),
              "%s: %d stages, each beginning at the line it declares"
              % (_sid11, len(_stages11)))
        _named11 = set()
        for p_ in _stages11:
            _named11.update(_shaped11.findall(p_["text"]))
        _unnamed11 = [c for c in _s11.needs() if c not in _named11]
        check(not _unnamed11,
              "%s: every one of its %d needs is named in a stage%s"
              % (_sid11, len(_s11.needs()),
                 "" if not _unnamed11 else " - NOT: " + ", ".join(_unnamed11)))
    # A method with no stages has no parts, and is served whole.
    _plain11 = [s_ for s_ in _methods11.values() if not s_.data.get("stages")]
    check(_plain11 and all(s_.parts() == [] for s_ in _plain11),
          "the %d methods that declare no stages have no parts - served whole"
          % len(_plain11))
    # AND IT CAN FAIL: a heading that is not a line, one written twice, two
    # out of order, and an id used twice are each refused by validate().
    if "family-creation" in _methods11:
        _real11 = _methods11["family-creation"]

        def _with11(stages, purpose=None):
            data = _copy11.deepcopy(_real11.data)
            data["stages"] = stages
            if purpose is not None:
                data["purpose"] = purpose
            return SKILL.validate(SKILL.Skill(data, _real11.path))

        _say11 = [p_ for p_ in _real11.parts() if p_["stage"] is not None]
        _a11, _b11 = _say11[0]["starts"], _say11[1]["starts"]
        check(not _with11(_real11.data["stages"]),
              "family-creation's own stages validate clean")
        for _why11, _bad11, _purpose11 in (
                ("a heading that is no line of the purpose",
                 [{"stage": "x", "starts": _a11 + " - REWORDED"}], None),
                ("a heading written twice in the purpose",
                 [{"stage": "x", "starts": _a11}],
                 _real11.purpose + "\n" + _a11 + "\n"),
                ("two headings out of the purpose's order",
                 [{"stage": "x", "starts": _b11}, {"stage": "y", "starts": _a11}],
                 None),
                ("one id given to two stages",
                 [{"stage": "x", "starts": _a11}, {"stage": "X", "starts": _b11}],
                 None),
                ("no entry that is a stage",
                 [{"starts": _a11}], None)):
            check(any("stage" in p_ for p_ in _with11(_bad11, _purpose11)),
                  "validate() refuses %s" % _why11)

    print()
    print("The HVAC skills name heron_hvac calculations that exist (PROPOSALS F42-3)")
    import re as _re
    import heron_hvac as _HVAC
    _skills = SKILL.load_all()[0]
    for _sid in ("space-airflow", "terminal-layout", "duct-layout"):
        _purpose = str(_skills[_sid].data.get("purpose", "")) if _sid in _skills else ""
        _named = _re.findall(r"heron_hvac's ([a-z_]+)", _purpose) + \
            _re.findall(r"and ([a-z_]+) where the modeller asks", _purpose)
        if _sid == "space-airflow":
            continue    # it reaches the engine through revit_building_loads
        check(_named and all(n in _HVAC.CALCULATIONS for n in _named),
              "%s names heron_hvac calculations that exist: %s"
              % (_sid, ", ".join(_named) or "none named"))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - ten skills, none of which names a fragment, and a build")
    print("queue produced by real jobs rather than by guessing - and one")
    print("unreadable skill costs one skill rather than all ten (D-48).")
    print()
    print("Phase 2's definition of done is 'ten real skills WORK'. Ten are")
    print("written and every one is DRAFT. That last word needs a Revit.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
