# Heron-Agent:  HERON-RAG-RET-003
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The modeller's short forms - read before the ranked search, never guessed.

    python tests/test_short_forms.py

Offline, nothing installed beyond PyYAML. Every list here is planted in a
scratch HERON_DATA, so the owner's own file is never read or written.

WHAT IT PROVES
  1. With no list of his own, nothing is added - and the example that ships
     beside the code is never read (D-34: Heron ships no list).
  2. A recorded short form adds its long form AFTER his words, which all stay.
  3. A word nobody recorded is left exactly as typed - no plural rule, no
     part-word match.
  4. An entry with no person or date, one recorded by a process, and a short
     form given two meanings are not used.
  5. Through heron_retrieve.find: his own words reach an exact declared phrase
     first; when they miss, the ranked search sees the long form and the
     answer's note says which short form it read.
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

scratch_audit.plant()

FAILURES = []

LIST = u"""
short-forms:
  - forms: [VCD, VCDs]
    means: volume control damper
    by: A Modeller
    at: "2026-11-26"
  - forms: [XYZ]
    means: no person said this
    by: inferred
    at: "2026-11-26"
  - forms: [ABC]
    means: nobody dated this
    by: A Modeller
  - forms: [DUO]
    means: first meaning
    by: A Modeller
    at: "2026-11-26"
  - forms: [DUO]
    means: second meaning
    by: Another Modeller
    at: "2026-11-26"
"""


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def add(store, fid, capability, identity):
    store.execute(
        "INSERT OR REPLACE INTO fragments (id, capability, semantic_identity, "
        "kind, status, domain, risk, folder, revit) VALUES (?,?,?,?,?,?,?,?,?)",
        (fid, capability, identity, "filter", "DRAFT", "test", "READ", "x",
         "2020,2024"))
    store.db.commit()


def main():
    data = tempfile.mkdtemp(prefix="heron-short-forms-data-")
    knowledge = tempfile.mkdtemp(prefix="heron-short-forms-kb-")
    os.environ["HERON_DATA"] = data
    os.environ["HERON_KNOWLEDGE"] = knowledge
    try:
        import heron_short_forms as SHORT

        print("1. NO LIST OF HIS OWN - NOTHING IS ADDED")
        check(SHORT.where() == os.path.join(data, "short-forms.yaml"),
              "the list is looked for in Heron's home, which is his data")
        check(os.path.isfile(SHORT.EXAMPLE),
              "the example ships beside the code, to be copied")
        said = "isolate the VCDs"
        check(SHORT.expand(said) == (said, []),
              "with no file of his own the example's VCD is not used - the "
              "product ships no list")

        with io.open(SHORT.where(), "w", encoding="utf-8") as handle:
            handle.write(LIST)
        entries, problems = SHORT.recorded()

        print("2. A RECORDED SHORT FORM ADDS ITS LONG FORM, AND REMOVES NOTHING")
        searched, used = SHORT.expand("isolate the VCDs in this view")
        check(searched == "isolate the VCDs in this view volume control damper",
              "his words first, in his order, the long form after (%r)" % searched)
        check([e["forms"] for e in used] == [["VCD", "VCDs"]],
              "and it says which entry it used")
        check(SHORT.expand("hide every vcd")[0].endswith("volume control damper"),
              "any case - a short form is the same letters typed small")
        check(SHORT.expand("VCD volume control damper")[1] == [],
              "a long form already in the request is not added twice")
        note = SHORT.sentence(used)
        check("VCD" in note and "volume control damper" in note
              and "A Modeller" in note and "2026-11-26" in note,
              "the note names the form, its meaning, who and when (%r)" % note)

        print("3. A WORD NOBODY RECORDED IS LEFT AS TYPED")
        for words in ("isolate all the PRVs", "count the VCDX tags",
                      "select the VCDS2 family", "move the FCUs"):
            check(SHORT.expand(words) == (words, []),
                  "%r is untouched - no part word, no guessed plural" % words)

        print("4. WHAT IS NOT A RECORD IS NOT USED")
        forms = set(f for e in entries for f in e["forms"])
        check("XYZ" not in forms and any("not a person" in p for p in problems),
              "an entry recorded by a process is refused, and said so")
        check("ABC" not in forms and any("carries no at" in p for p in problems),
              "an entry with no date is not a record")
        check("DUO" not in forms and any("two meanings" in p and "duo" in p
                                         for p in problems),
              "a short form given two meanings is read as neither - which one "
              "is a question for the modeller")
        check(SHORT.expand("show the DUO")[1] == [],
              "so it is left as typed")

        print("5. THROUGH THE LOOKUP")
        import heron_scope as SCOPE
        import heron_search as SEARCH
        import heron_embed as EMBED
        import heron_retrieve as R
        store = SCOPE.open_scope(SCOPE.EXPERIMENTAL)
        try:
            SEARCH.ensure_tables(store)
            add(store, "FRG-SF-901", "DAMPER_FIND", "find every volume control damper")
            add(store, "FRG-SF-902", "FITTING_FIND", "find every pipe fitting in a view")
            add(store, "FRG-SF-903", "OWN_WORDS", "list each VCD")
            SEARCH.index(store, force=True)
            EMBED.index(store, force=True)

            exact = R.find(store, "list each VCD", revit="2024")
            check(exact.route == "identity" and exact.fragment_id == "FRG-SF-903",
                  "his own words reach their exact declared phrase first (%s %s)"
                  % (exact.route, exact.fragment_id))

            ranked = R.find(store, "find every VCD", revit="2024")
            check(ranked.route == "hybrid" and ranked.fragment_id == "FRG-SF-901",
                  "when they miss, the ranked search sees the long form (%s)"
                  % ranked.fragment_id)
            check("VCD" in ranked.note and "volume control damper" in ranked.note,
                  "and the answer says it read VCD as volume control damper")

            os.remove(SHORT.where())
            plain = R.find(store, "find every VCD", revit="2024")
            check("short forms" not in plain.note,
                  "with the list gone, nothing is read as anything (%r)" % plain.note)
        finally:
            store.close()
    finally:
        shutil.rmtree(data, ignore_errors=True)
        shutil.rmtree(knowledge, ignore_errors=True)

    print()
    if FAILURES:
        print("%d FAILED" % len(FAILURES))
        return 1
    print("all passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
