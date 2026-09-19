# Heron-Agent:  HERON-DEV-RVT-013
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 17 - `tools/prove-tracking.py`, D-53 tracking for a FRAGMENT.

    python tests/test_prove_tracking.py

WHAT IT PROVES, AND IT IS THE HALF THAT NEEDS NO REVIT
--------------------------------------------------------
`brain/heron_validate.py` has judged a fragment's tracking set since D-53 was
written and NOTHING has ever produced one - the judging half was built and the
producing half was not. This suite covers the producing half's ARRANGEMENT,
which is every check that happens before a model is opened:

  1. the bar is the one heron_validate enforces, read from it rather than
     typed again - and DISTINCT values, because three copies of one input
     cannot show an answer following it
  2. every reason a run is refused, each on a fragment or an argument built to
     have exactly that fault, and all of them from disk
  3. `judge()` answers the same two questions heron_validate will ask of the
     record, so a run that cannot pass is never written out looking like
     evidence
  4. the three fragments docs/NEEDS-CHECKING Group W names as standing between
     the skills and three more proofs can each be ARRANGED

WHAT IT DOES NOT PROVE
  `live_runner`, which is the one function that needs a Revit. Everything
  around it is covered, including the run LOOP - `run_rows` takes its runner
  as an argument and this suite passes a fake. Nothing here has been in front
  of Revit, and a tracking set is not a proof until a person signs one
  (`brain/proof-drafts/README.md`).
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class FakeFrag(object):
    """A fragment built in memory, never on disk.

    Built rather than written into brain/fragments/ because these are shapes
    the library must not be given: a contract with two results and no guidance,
    a need nobody can type. A fixture that created one is a defect somebody
    later finds and "fixes".
    """

    def __init__(self, slug, status="DRAFT", needs=(), provides=(), risk="READ"):
        self.slug = slug
        self.status = status
        self.data = {"capability": slug.upper().replace("-", "_"),
                     "risk": risk}
        self._needs = list(needs)
        self._provides = list(provides)

    def needs(self):
        return list(self._needs)

    def provides(self):
        return list(self._provides)


def main():
    import tempfile
    os.environ.setdefault("HERON_KNOWLEDGE",
                          tempfile.mkdtemp(prefix="heron-tracking-"))

    import importlib.util
    spec = importlib.util.spec_from_file_location(
        "prove_tracking", os.path.join(ROOT, "tools", "prove-tracking.py"))
    T = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(T)

    import heron_validate as VALIDATE

    print("1. the bar is the one heron_validate already enforces")
    source = io.open(os.path.join(ROOT, "brain", "heron_validate.py"),
                     encoding="utf-8").read()
    check("the tracking set has only %d row(s)" in source,
          "heron_validate refuses a short tracking set in its own words")
    check("if len(tracking) < 3:" in source,
          "and the number it enforces is 3")
    check(T.MIN_TRACKING_ROWS == 3,
          "this tool reads the same bar (%d)" % T.MIN_TRACKING_ROWS)
    check("tracking" not in io.open(
        os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py"),
        encoding="utf-8").read(),
        "and NOTHING in the client produces one - the gap this tool fills")

    print()
    print("2. every refusal, on a fault built to trigger it, all from disk")
    receivable = (lambda kind, name:
                  (False, "made up") if kind == "Reference" else (True, None))
    plain = [{"name": "categoryName", "type": "string", "source": "request"}]

    ok = T.refusals(FakeFrag("f", needs=plain), "categoryName",
                    ["a", "b", "c"], receivable)
    check(ok == [], "a sound arrangement is refused for nothing")

    why = T.refusals(FakeFrag("f", status="PROVEN", needs=plain),
                     "categoryName", ["a", "b", "c"], receivable)
    check(any("already been in front of a model" in w for w in why),
          "a PROVEN fragment is refused - re-proving spends the owner twice")

    why = T.refusals(FakeFrag("f", needs=plain), "nope", ["a", "b", "c"],
                     receivable)
    check(any("declares no need called" in w for w in why),
          "a need the fragment does not declare is named, with what it takes")

    chained = [{"name": "elements", "type": "IList<Element>"}]
    why = T.refusals(FakeFrag("f", needs=chained), "elements",
                     ["a", "b", "c"], receivable)
    check(any("source: fragment" in w for w in why),
          "a need filled by an earlier fragment is not a value to vary")

    facey = [{"name": "faces", "type": "Reference", "source": "request"}]
    why = T.refusals(FakeFrag("f", needs=facey), "faces", ["a", "b", "c"],
                     receivable)
    check(any("cannot be typed in" in w for w in why),
          "a shape Revit cannot receive is refused by D-54's own list")

    why = T.refusals(FakeFrag("f", needs=plain), "categoryName", ["a", "b"],
                     receivable)
    check(any("at least 3" in w for w in why),
          "two values is refused before a session is spent on it")

    why = T.refusals(FakeFrag("f", needs=plain), "categoryName",
                     ["a", "a", "a"], receivable)
    check(any("only 1 distinct" in w for w in why),
          "and so is three copies of one value - the bar is the VARIATION")

    two = plain + [{"name": "tolerance", "type": "double", "source": "request"}]
    why = T.refusals(FakeFrag("f", needs=two), "categoryName",
                     ["a", "b", "c"], receivable)
    check(any("Hold it still with --set tolerance" in w for w in why),
          "another caller value must be HELD STILL - D-53 is ONE input moving")
    check(T.refusals(FakeFrag("f", needs=two), "categoryName",
                     ["a", "b", "c"], receivable, {"tolerance": "25"}) == [],
          "and holding it satisfies that")

    why = T.refusals(FakeFrag("f", needs=plain), "categoryName",
                     ["a", "b", "c"], receivable, {"widthMm": "1"})
    check(any("dropped in silence" in w for w in why),
          "a --set the fragment does not declare is named, not dropped "
          "(row 71's rule, applied before the run)")

    # ONE PARTICULAR ELEMENT IS HAND WORK, NOT AN IMPOSSIBILITY. The refusal
    # itself names the word that works, and a tracking run has a person at the
    # keyboard - which a generated job file does not.
    one = plain + [{"name": "start", "type": "Element", "source": "request"}]
    hand = lambda kind, name: ((True, None) if kind != "Element"
                               else (False, "PICK IT"))
    why = T.refusals(FakeFrag("f", needs=one), "categoryName",
                     ["a", "b", "c"], hand, None, "PICK IT")
    check(any("--set start=selected" in w for w in why),
          "a singular Element asks to be held still as `selected`")
    check(T.refusals(FakeFrag("f", needs=one), "categoryName",
                     ["a", "b", "c"], hand, {"start": "selected"},
                     "PICK IT") == [],
          "and once held, it is not a blocker")

    print()
    print("3. judge() asks what heron_validate will ask of the record")
    rows = [{"input": "a", "field": "count", "value": 1},
            {"input": "b", "field": "count", "value": 2},
            {"input": "c", "field": "count", "value": 3}]
    good, why = T.judge(rows)
    check(good, "three rows that move are a tracking set: %s" % why)

    good, why = T.judge(rows[:2])
    check(not good and "only 2 row(s)" in why,
          "two rows are refused, in heron_validate's own terms")

    same = [dict(r, value=7) for r in rows]
    good, why = T.judge(same)
    check(not good and "ignoring its input" in why,
          "three identical rows are refused - that IS what ignoring the input "
          "looks like")

    # `_as_count` IS IMPORTED, NOT RE-IMPLEMENTED. The executor sends strings:
    # "0", "0 item(s)" and 0 are one answer, and reading them as three is the
    # bug that flagged every negative case until 2026-09-07.
    built = T.track_rows([("a", {"count": "3 item(s)"}),
                          ("b", {"count": "0"}),
                          ("c", {"count": 7})], "count")
    check([r["value"] for r in built] == [3, 0, 7],
          "a row's value comes through _as_count, so the executor's strings "
          "are read as the numbers they stand for")
    check(all(r["field"] == "count" and r["input"] for r in built),
          "and every row carries the shape heron_validate reads back")
    check(VALIDATE._as_count("0 item(s)") == 0,
          "which is heron_validate's own function, not a copy")

    print()
    print("3b. the run loop, on a FAKE runner - no Revit anywhere")
    # `runner` is injected precisely so this can be proved here. The only line
    # left unproved is the bridge call itself, which is the same call
    # cmd_prove already makes.
    sent = []

    def fake(values):
        sent.append(dict(values))
        return {"ok": True, "provides": {"count": len(values["categoryName"])}}

    rows, refused = T.run_rows(fake, "categoryName", ["a", "bb", "ccc"],
                               {"held": "still"}, "count")
    check([r["value"] for r in rows] == [1, 2, 3],
          "every value is run and the declared result becomes the row")
    check([r["input"] for r in rows]
          == ["categoryName=a", "categoryName=bb", "categoryName=ccc"],
          "and each row names the input that produced it")
    check(all(s.get("held") == "still" for s in sent),
          "the held values go with EVERY run - one input moves, not two")
    check(refused == [], "nothing was refused")

    def picky(values):
        if values["categoryName"] == "bb":
            return {"ok": False, "error": "bad_request_value",
                    "message": "no category called bb"}
        return {"ok": True, "provides": {"count": 1}}

    rows, refused = T.run_rows(picky, "categoryName", ["a", "bb", "ccc"],
                               None, "count")
    check(len(rows) == 2 and len(refused) == 1,
          "A REFUSED VALUE IS NOT A TRACKING ROW - it shows the fragment "
          "never looked, not that it looked and found nothing")
    check(refused[0][0] == "bb" and "bad_request_value" in refused[0][1],
          "and the refusal is carried with its reason")
    good, why = T.judge(rows)
    check(not good and "only 2 row(s)" in why,
          "so the set is then short, and says so rather than passing")

    # A reply without a `provides` envelope is read directly - the executor
    # has used both shapes and a row built from neither would be empty.
    rows, _ = T.run_rows(lambda v: {"ok": True, "count": 4},
                         "categoryName", ["a", "b", "c"], None, "count")
    check([r["value"] for r in rows] == [4, 4, 4],
          "a flat reply is read too")
    good, why = T.judge(rows)
    check(not good, "and three identical answers are still refused")

    print()
    print("3c. a write is not tracked down the read path")
    ladder = {"READ": 0, "ANALYZE": 1, "SUGGEST": 2, "EXECUTE": 3, "MODIFY": 4}
    check(T.write_path_refusal(FakeFrag("f", risk="READ"), 4, ladder) is None,
          "a READ is sent")
    check(T.write_path_refusal(FakeFrag("f", risk="SUGGEST"), 4, ladder) is None,
          "and so is a SUGGEST - it is below the write threshold")
    why = T.write_path_refusal(FakeFrag("f", risk="MODIFY"), 4, ladder)
    check(why is not None and "WRITE path" in why,
          "a MODIFY is refused - a write varied three ways changes the model "
          "three times, and defect row 6 is what sending it down the read "
          "path costs")
    why = T.write_path_refusal(FakeFrag("f", risk="NONSENSE"), 4, ladder)
    check(why is not None and "not a level HeronRisk names" in why,
          "and a risk nobody can establish is refused rather than assumed safe")
    # THE THRESHOLD IS READ FROM THE REGISTRY, NOT TYPED - Golden Rule 19.
    GJ_ = T._sibling("generate-jobs.py")
    name, ordinal, real = GJ_.write_threshold()
    check(real.get(name) == ordinal,
          "and the real threshold comes from HeronOperationRegistry.cs (%s)"
          % name)

    print()
    print("4. Group W's three fragments can each be ARRANGED")
    by_slug, unreadable = T.library()
    check(not unreadable, "the real library reads clean")
    GJ = T._sibling("generate-jobs.py")

    wanted = {
        "trace-connectivity": ("tolerance", ["1", "10", "100"],
                               {"start": "selected"}),
        "describe-blank-parameters": ("parameterName",
                                      ["Mark", "Comments", "Width"], {}),
        "report-findings": ("whatWasChecked", ["ducts", "pipes", "walls"],
                            {"checkedCount": "10"}),
    }
    for slug, (need, values, held) in sorted(wanted.items()):
        frag = by_slug.get(slug)
        check(frag is not None, "%s is in the library" % slug)
        if frag is None:
            continue
        why = T.refusals(frag, need, values, GJ.receivable, held,
                         GJ.ELEMENT_INSTANCE_REASON)
        check(why == [], "%s varying `%s` is arrangeable%s"
              % (slug, need, "" if not why else ": " + why[0]))
        results, _roles = T.result_names(frag)
        check(bool(results),
              "%s declares something a tracking row can carry: %s"
              % (slug, ", ".join(results)))

    print()
    print("5. it never signs, and it says what it cannot do")
    tool = io.open(os.path.join(ROOT, "tools", "prove-tracking.py"),
                   encoding="utf-8").read()
    check("never signs" in tool and "never promotes" in tool,
          "the tool says in its own words that it never signs and never "
          "promotes")
    check("IS THE ONE FUNCTION NOTHING HERE HAS EXERCISED" in tool,
          "and names the one function no test here can reach - live_runner, "
          "which needs a Revit")
    check("A run that cannot pass `judge()` **writes nothing**" in tool,
          "and says that a set which cannot pass is not written out looking "
          "like evidence")
    check("cmd_prove" in tool,
          "and says whose call it sends, rather than inventing a second "
          "opinion about talking to the executor")
    check("does not replace D-30" in tool,
          "and that tracking is not an easier route past a negative case")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the arrangement half of D-53 tracking for a fragment,")
    print("which is every check that can be made without a Revit. The three")
    print("fragments standing between the skills and three more proofs can")
    print("each be arranged; running them needs the machine that has a model.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
