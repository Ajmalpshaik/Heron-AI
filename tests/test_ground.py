# Heron-Agent:  HERON-RAG-CIT-014
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 3 - a claim is checked against the clause it cites.

    python tests/test_ground.py

CHECK 1 IS THE UNDERSTATING RULE, AND IT IS FIRST BECAUSE THE PLAN SAYS SO.
A modeller who writes "the duct needs insulation" about a clause saying
"ducts shall be insulated to 25mm" has said something TRUE AND LESS SPECIFIC.
A checker that calls that a fabrication will be switched off within a week,
and then every other row on this page is worth nothing.

WHAT ELSE IT PROVES
  2. A FABRICATED SENTENCE IS FLAGGED WITH ITS RATIO. The thing the whole
     stage is for.
  3. BOTH SIDES ARE NORMALISED IDENTICALLY (R-48) - 150mm / 150 mm, DN150 /
     150Ø, §21.3.2 / 21.3.2, Revit 2024 / 2024. A SPELLING DIFFERENCE MUST
     NEVER READ AS AN INVENTION.
  4. ONLY CHECKABLE SENTENCES ARE CHECKED (R-50). A sentence with no fact in
     it cannot fabricate one, and flagging it teaches people to ignore flags.
  5. A FACT WITH NO CITATION IS A BUG (R-65), not a low-confidence answer.
  6. THE THRESHOLD IS PER KIND OF CLAIM (R-49), from a named table.
  7. IT FLAGS AND NEVER REWRITES (R-53), and there is no function here that
     could.
  8. IT CALLS NO MODEL AND TOUCHES NO NETWORK (R-47). difflib and re.
  9. THE REPORT CARRIES ITS DENOMINATOR AND ITS THRESHOLDS (R-52) - a flag
     count with no denominator is not a measurement.
"""

import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


DOCUMENT = """Heron Test Standard 2026

Section 9 Thermal Insulation

9.1 Ductwork

9.1.1 Thickness

Ducts shall be insulated to 25mm, except where installed within a conditioned
space and serving a terminal within 3m.

9.1.2 Materials

Insulation shall be mineral wool of density 48 kg/m3.

Section 12 Sanitary Drainage

12.1 Gradients

Drainage carrying soil shall fall at not less than 1:100.
"""


class FakePart(object):
    """A packet part, as heron_context builds one. Only what check() reads."""

    def __init__(self, citation, body):
        self.citation = citation
        self.body = body


class FakePacket(object):
    def __init__(self, parts):
        self.parts = parts


def main():
    import heron_ground as G

    source = ("Ducts shall be insulated to 25mm, except where installed "
              "within a conditioned space and serving a terminal within 3m.")
    packet = FakePacket([FakePart(
        {"chunk": "doc:0003", "document": "Heron Test Standard 2026",
         "locator": "9.1.1", "heading_path": "... 9.1.1 Thickness"}, source)])

    print("1. R-51 - A CLAIM THAT SAYS LESS THAN ITS SOURCE PASSES")
    report = G.check("The duct needs insulation to 25mm [9.1.1].", packet)
    verdicts = [c.verdict for c in report.claims if c.verdict != G.SKIPPED]
    check(all(v in G.PASSES for v in verdicts),
          "'the duct needs insulation to 25mm' passes against a clause that "
          "also states an exception and a distance (%s)" % ", ".join(verdicts))
    check(report.ok, "and the report says nothing is wrong with it")

    report = G.check("Ducts shall be insulated [9.1.1].", packet)
    check(report.ok,
          "and so does a claim with no number at all - it states less, and "
          "understating is not fabricating")
    print()

    print("2. A FABRICATED SENTENCE IS FLAGGED, WITH ITS RATIO")
    report = G.check("Ducts shall be insulated to 50mm [9.1.1].", packet)
    check(len(report.flagged) == 1,
          "a thickness the clause does not state is flagged")
    if report.flagged:
        flag = report.flagged[0]
        check(flag.ratio is not None,
              "and it carries its similarity ratio, %.2f" % flag.ratio)
        check(flag.threshold is None,
              "with NO threshold, because it was flagged on an ADDED FACT "
              "rather than on a score - the ratio was measured against six "
              "true paraphrases and six wrong claims and could not separate "
              "them, so it is reported and never enforced here")
        check("50mm" in " ".join(flag.added),
              "and names the fact the source does not carry: %s"
              % ", ".join(flag.added))
    report = G.check("Insulation shall be mineral wool of density 96 kg/m3 "
                     "[9.1.1].", packet)
    check(len(report.flagged) == 1,
          "a density from a different clause, cited to this one, is flagged")
    print()

    print("3. R-48 - both sides normalised identically, so spelling is not a lie")
    pairs = [("150mm", "150 mm"), ("DN150", "150Ø"), ("DN150", "Ø150"),
             ("§21.3.2", "21.3.2"), ("Revit 2024", "2024"),
             ("1,500mm", "1500 mm"),
             # A drainage fall is written three ways on three drawings, and a
             # percentage two ways. Both were destroyed by the punctuation
             # strip before the selector ever saw them - so a fall was not a
             # checkable fact at all and could be invented freely.
             ("1:100", "1 in 100"), ("1:100", "1/100"),
             ("50%", "50 per cent")]
    for left, right in pairs:
        check(G.normalise(left) == G.normalise(right),
              "%-12s and %-10s normalise to the same thing (%r)"
              % (left, right, G.normalise(left)))

    spaced = FakePacket([FakePart(
        {"chunk": "d:1", "locator": "9.1.1", "document": "S"},
        "Ducts shall be insulated to 25 mm.")])
    report = G.check("Ducts shall be insulated to 25mm [9.1.1].", spaced)
    check(report.ok,
          "and '25mm' against a source saying '25 mm' is NOT a fabrication - "
          "the row that would have made this checker useless")
    print()

    print("4. R-50 - only sentences with a fact in them are checked")
    report = G.check("This clause is worth reviewing with the MEP lead.",
                     packet)
    check([c.verdict for c in report.claims] == [G.SKIPPED],
          "a sentence with no fact in it is skipped, not flagged")
    check(report.checked == 0 and len(report.claims) == 1,
          "and the report counts 0 checked out of 1 sentence - the "
          "denominator is what makes two runs comparable")
    check(G.facts("drainage shall fall at 1:100") == ["1in100"],
          "a gradient is a fact - it was not, until the punctuation strip was "
          "found to be eating the colon before the selector looked")
    check(G.facts("allow 50% spare capacity") == ["50pct"],
          "and so is a percentage")
    check(G.facts("this is worth reviewing") == [],
          "the selector finds no fact in it")
    check("25mm" in G.facts("insulated to 25mm"),
          "and does find one in 'insulated to 25mm'")
    print()

    print("5. R-65 - a fact with no citation is a BUG, not a weak answer")
    report = G.check("Ducts shall be insulated to 25mm as per the "
                     "specification.", packet)
    check(len(report.uncited) == 1,
          "'as per the specification' is not a citation")
    check(not report.ok,
          "and the report does not pass - R-21 calls an uncited standards "
          "answer a bug rather than a low-confidence answer")
    report = G.check("Ducts shall be insulated to 25mm [doc:0003].", packet)
    check(report.ok, "while a chunk id cites it just as well as a locator")
    print()

    print("6. R-49 - the threshold is per KIND of claim, from a named table")
    check(sorted(G.THRESHOLDS) == sorted([G.QUOTE, G.REFERENCE, G.NUMERIC,
                                          G.PARAPHRASE]),
          "four kinds, four thresholds")
    check(G.THRESHOLDS[G.QUOTE] is not None
          and G.THRESHOLDS[G.PARAPHRASE] is None
          and G.THRESHOLDS[G.NUMERIC] is None
          and G.THRESHOLDS[G.REFERENCE] is None,
          "and only a QUOTE gets a gate (%.2f coverage). The other three are "
          "carried by the added-fact rule, because the similarity ratio was "
          "MEASURED against six true paraphrases and six wrong claims and "
          "their ranges overlap - a true paraphrase scored 0.222, below a "
          "wrong claim at 0.236" % G.THRESHOLDS[G.QUOTE])

    src = ("Ducts shall be insulated to 25mm, except where installed within "
           "a conditioned space and serving a terminal within 3m.")
    quoted = FakePacket([FakePart(
        {"chunk": "d:1", "locator": "9.1.1", "document": "S"}, src)])
    report = G.check('It states "ducts shall be insulated to 25mm" [9.1.1].',
                     quoted)
    check(report.ok,
          "a REAL partial quotation passes - it is IN the source, which is "
          "what a quotation claims")
    report = G.check('It states "all ductwork requires 25mm insulation '
                     'everywhere" [9.1.1].', quoted)
    check(not report.ok,
          "and a misquotation does not. Containment separated these cleanly "
          "(1.00 against 0.17) where similarity did not")
    check(G.kind_of('He said "ducts shall be insulated to 25mm"') == G.QUOTE,
          "a sentence in quotation marks is a QUOTE")
    check(G.kind_of("Clause 9.1.1 applies to ductwork") == G.REFERENCE,
          "one naming a clause number is a REFERENCE")
    check(G.kind_of("Insulate to 25mm") == G.NUMERIC,
          "one carrying a dimension is NUMERIC")
    check(G.kind_of("Ducts shall be insulated") == G.PARAPHRASE,
          "and prose is a PARAPHRASE")
    check(G.kind_of("Insulate to 25mm [9.1.1].") == G.NUMERIC,
          "and the CITATION MARKER does not decide the kind - every cited "
          "sentence was being called a REFERENCE claim because of the marker "
          "saying where it came from, not because of anything it said")
    print()

    print("7. R-53 - it FLAGS and it cannot rewrite")
    check(not hasattr(G, "repair") and not hasattr(G, "rewrite")
          and not hasattr(G, "correct") and not hasattr(G, "fix"),
          "there is no repair, rewrite, correct or fix in this module - a "
          "checker that quietly fixes its own findings is how a wrong answer "
          "becomes an invisible one")
    report = G.check("Ducts shall be insulated to 50mm [9.1.1].", packet)
    check(not hasattr(report, "corrected") and not hasattr(report, "answer"),
          "and the report carries no corrected text either")
    check("REPORT" in "\n".join(report.lines()),
          "it says so in its own output")
    print()

    print("8. R-47 - no model, no network, no keys")
    import heron_ground
    text = open(heron_ground.__file__, encoding="utf-8").read()
    for forbidden in ("import requests", "urllib.request", "http://",
                      "https://", "openai", "anthropic", "model2vec",
                      "api_key", "API_KEY"):
        check(forbidden not in text,
              "the module contains no %r" % forbidden)
    check("import difflib" in text and "import re" in text,
          "difflib and re are the whole comparison")
    print()

    print("8b. The three holes a review found, each of which said 'ok'")
    # Every one of these produced a PASSING report on a false claim. They are
    # kept together because they share a shape: a sentence the checker never
    # looked at is indistinguishable, from the outside, from a sentence it
    # looked at and approved.
    check(G.facts("Every duct shall carry OST_DuctCurves.") == ["ost_ductcurves"],
          "a CATEGORY is a fact. The pattern was written case-sensitive and "
          "facts() lowercases before running it, so OST_ anything matched "
          "NOTHING and the sentence was skipped as factless")
    check(G.facts("Set BuiltInParameter.RBS_DUCT_BOTTOM_ELEVATION.")
          == ["builtinparameter.rbs_duct_bottom_elevation"],
          "and so is a PARAMETER, for the same reason")

    report = G.check('The clause says "Ducts shall be painted red" [9.1.1].',
                     packet)
    check(not report.ok,
          "a fabricated QUOTATION carrying no number is flagged. It was "
          "skipped as factless before the quote gate could run - so the one "
          "gate that survived measurement never ran on the only kind of claim "
          "it was built for")
    report = G.check('The clause says "insulated to 25mm" [9.1.1].', packet)
    check(report.ok, "while a real partial quotation still passes")

    two = FakePacket([
        FakePart({"chunk": "a:1", "locator": "4.1", "document": "Company"},
                 "Ducts shall be insulated to 30mm."),
        FakePart({"chunk": "b:1", "locator": "4.1", "document": "Project"},
                 "Ducts shall be insulated to 40mm.")])
    report = G.check("Ducts shall be insulated to 30mm [4.1].", two)
    check(report.ambiguous and not report.ok,
          "a locator that names TWO documents is AMBIGUOUS, not resolved to "
          "whichever was added last. That silently checked a TRUE claim "
          "against the wrong document and flagged it - the false alarm R-51 "
          "exists to prevent, arriving through the citation")
    report = G.check("Ducts shall be insulated to 30mm [a:1].", two)
    check(report.ok,
          "and citing the chunk id resolves it, which is what the ambiguous "
          "report asks for")
    print()

    print("9. R-52 - the report carries its thresholds and its denominator")
    report = G.check("Ducts shall be insulated to 50mm [9.1.1]. This is worth "
                     "a look. Drainage falls at 1:100 [9.1.1].", packet)
    printed = "\n".join(report.lines())
    check("Thresholds used" in printed, "it states the thresholds it used")
    check("of 3 sentence(s)" in printed,
          "and how many sentences it saw, not only how many it flagged")
    check(report.checked == 2,
          "two of the three carried a fact and were checked")
    check(any("%.2f" % c.ratio in printed for c in report.flagged if c.ratio),
          "and every flag is printed with its ratio")
    print()

    print("10. It runs against a real packet, end to end")
    home = tempfile.mkdtemp(prefix="heron-ground-")
    papers = tempfile.mkdtemp(prefix="heron-ground-papers-")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        import heron_scope as SCOPE
        import heron_search as SEARCH
        import heron_embed as EMBED
        import heron_ingest as I
        import heron_context as CTX

        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            path = os.path.join(papers, "standard.md")
            with open(path, "w", encoding="utf-8") as handle:
                handle.write(DOCUMENT)
            I.ingest(store, path, added_by="tests")
            SEARCH.index(store)
            EMBED.index(store)
            SEARCH.index_chunks(store)
            EMBED.index_chunks(store)

            real = CTX.assemble(store, "how thick should duct insulation be",
                                path=CTX.STANDARDS)
            cited = [p for p in real.parts if getattr(p, "citation", None)]
            check(cited,
                  "the STANDARDS packet carries %d part(s) with a citation "
                  "bound to an exact chunk" % len(cited))
            check(all(c.citation.get("chunk") for c in cited),
                  "and every one names the CHUNK, not just the document")

            report = G.check("Ducts shall be insulated to 25mm [9.1.1].", real)
            check(report.ok,
                  "a true claim against the real packet passes")
            report = G.check("Ducts shall be insulated to 75mm [9.1.1].", real)
            check(not report.ok,
                  "and an invented thickness against the same packet does not")

            print()
            print("11. R-45 - the refusal NARROWED, and where it has not")
            empty_home = tempfile.mkdtemp(prefix="heron-ground-empty-")
            os.environ["HERON_KNOWLEDGE"] = empty_home
            try:
                bare = SCOPE.open_scope(SCOPE.GLOBAL)
                try:
                    refused = None
                    try:
                        CTX.assemble(bare, "how thick should insulation be",
                                     path=CTX.STANDARDS)
                    except CTX.SourceMissing as why:
                        refused = str(why)
                    check(refused and "NO DOCUMENT IS INDEXED" in refused,
                          "with an empty store the path still REFUSES, and "
                          "now names the real cause rather than saying no "
                          "clause store exists at all")
                finally:
                    bare.close()
            finally:
                shutil.rmtree(empty_home, ignore_errors=True)
                os.environ["HERON_KNOWLEDGE"] = home

            # AND THE PART R-45 HAS NOT REACHED, ASSERTED SO IT CANNOT BE
            # QUIETLY FORGOTTEN. Asked about cats, this path returns clauses -
            # correctly cited, and about ducts. The refusal "nothing indexed
            # covers this" needs a floor, R-60 says a floor comes from a
            # measurement, and W-8 records that no measurement on this backend
            # separates a real question from an unreal one.
            #
            # So the packet carries the measurement instead of acting on it,
            # and this asserts that it does. When the floor exists this check
            # is what should change.
            cats = CTX.assemble(store, "what is the best food for a cat",
                                path=CTX.STANDARDS)
            honest = [p for p in cats.parts
                      if p.name.startswith("how contested")]
            check(honest,
                  "a question with no BIM content still returns clauses - and "
                  "the packet carries how contested they were rather than "
                  "presenting them as settled")
            check(honest and "DOES NOT MEAN IT ANSWERS THE QUESTION"
                  in honest[0].body,
                  "and says plainly that being cited is not being right")
        finally:
            store.close()
    finally:
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(papers, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)
    print()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a claim that says less than its source passes, a claim")
    print("that states a fact the source does not carry is flagged with its")
    print("ratio, a spelling difference is never read as an invention, and")
    print("nothing here can rewrite an answer.")
    print()
    print("It proves nothing about whether the ANSWER is good. It proves the")
    print("answer did not invent the numbers in it. Those are different")
    print("claims, and only the second is testable without a person.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
