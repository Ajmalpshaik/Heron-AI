# Heron-Agent:  HERON-DEV-PRF-015
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
How long the brain takes, stage by stage. The half nothing was measuring.

THIS FILE OWNS HERON-DEV-PRF-015, DECIDED BY THE OWNER ON 2026-09-17
----------------------------------------------------------------------
The row is "execution time AND RESOURCE COST", and that conjunction is the
whole of the decision. This measures both; brain/heron_devperf.py measures
time only. D-75 had already noticed as much - "the row's own words point at
measure-brain.py" - but declined to rule, because picking on an inference is
exactly the guess that produced PROPOSALS F27 in the first place.

So the claim waited for a person, and a person made it. F27 stays as the
record of why it could not be settled by reading.

heron_devperf.py keeps `Heron-Agent: none`. It is not lesser and it is not
dead code: it times the test SUITES against check-gaps' bound, which is a real
job and the reason it was written. It simply is not this row.

    python tools/measure-brain.py
    python tools/measure-brain.py --revit 2024 --requests 40

WHY THIS EXISTS
---------------
docs/32 s4.2. The Revit side has been measured since Step 4: HeronAudit writes
a duration per request and heron_gaps.py reports median and worst milliseconds
per fragment and per operation. The BRAIN has no clock in it at all - a grep
for perf_counter or monotonic over heron_search, heron_embed and heron_retrieve
returns nothing, and none of them writes to the audit trail.

That is the half where the only latency disaster this project has actually had
lived. D-49: closing register row A7 installed a trained embedding backend, its
import ran on the asyncio event loop, and a real Claude Code tool call sat on
heron_capabilities for THIRTY MINUTES. An import costing 1.0 s in a fresh
process was still running at 40 s there. It was found with faulthandler by
somebody who noticed a hang - not by anything that measures.

TWO LINES FOR THAT, NOT ONE, AND THE SECOND IS THE ONE THAT MATTERS.
`import heron_embed` is the module and is cheap everywhere. `backend()` calls
`_load_model()`, which is where the TRAINED ENCODER is imported - the 1.0 s
that became 40 and then thirty minutes. The first version of this tool timed
only the first and called it the D-49 measurement; the cost happened on the
next line, untimed. On this container it is invisible either way because
model2vec is not installed and _load_model() returns None at once. On the
machine where it matters it would have hidden the whole thing, in the tool
written to catch exactly that.

RESOURCE COST, AND WHY IT IS TWO NUMBERS THAT MEASURE DIFFERENT THINGS
-----------------------------------------------------------------------
docs/28 gives HERON-DEV-PRF-015 "execution time AND RESOURCE COST", and
until 2026-09-15 this file measured only the first half.

The obvious tool is tracemalloc, and for THIS failure it is the wrong one:
it counts Python allocations, and D-49's thirty minutes were a NATIVE model
import. The tool written to catch that would have reported a few megabytes
and missed it.

So the cost reported here is PEAK RSS, through resource.getrusage, which
counts native memory because it counts the process. It buys that at a price
worth stating: ru_maxrss is a HIGH-WATER MARK for the whole process and
never falls, so a stage can be credited with the growth that happened
across it and NEVER with its own peak. A stage that allocates 2 GB and
frees it shows zero, correctly and uselessly.

`resource` does not exist on Windows. There the cost is reported as NOT
MEASURED rather than as zero - the two are different, and only one of them
is true.

The incoming master architecture document puts it plainly and it is right:
"without a baseline, improvement cannot be proven."

WHAT IT DELIBERATELY DOES NOT CLAIM
-----------------------------------
`Heron-Agent: none`, and that is a decision rather than an omission.

It used to argue that against HERON-OPS-OBS-011, whose row then read "latency,
token usage, model calls per request, cost per request" - three of which Heron
cannot see. THAT ARGUMENT IS SPENT: D-58 corrected the row on 2026-09-09 to
the two things Heron can see, HERON-OPS-OBS-011 is built in
brain/heron_observability.py, and the corrected row NAMES THIS FILE as what
measures the latency half for the brain. So this is a tool that row relies on,
not a candidate for its id.

The open one is HERON-DEV-PRF-015, "execution time and resource cost", which is
word for word what this file now measures - and claiming it would still be a
guess, because the question is WHOSE cost that row means. Its department is the
build pipeline, and the one Development row already claimed by a tool is
HERON-DEV-RVT-013, on tools/batch-prove.py, which measures FRAGMENTS rather
than Heron. Under that reading PRF-015 measures the artefact being built and
this file measures the builder, and the two are not the same agent.

Not resolved here. PROPOSALS F27.

IT IS NOT A GATE AND EXITS 0
----------------------------
A timing is not a pass or a fail. There is no threshold here to breach, because
there is no agreed budget yet - docs/19 s2 proposes one and nothing implements
it. Setting a limit from the first run would make this machine's speed the
standard, which is exactly backwards.

THE QUESTIONS ARE THE FRAGMENTS' OWN WORDS, WHICH FLATTERS ONE STAGE
-------------------------------------------------------------------
Asking a fragment its own declared utterance is the case Step 9's identity
short circuit was built for: one lookup, no search, no fusion. So find() comes
back in about a millisecond and that figure is the BEST case, not the typical
one. The run says how many requests took that route so the number cannot be
quoted on its own. retrieve() is timed separately and unconditionally for
exactly this reason - it is what a request phrased in somebody else's words
actually costs.

A MEASUREMENT WITHOUT A MACHINE IS NOT A MEASUREMENT
----------------------------------------------------
So every run prints the machine, the Python, the embedding backend and the
fragment count with it. A number from a Linux container and a number from the
owner's PC are not comparable, and a table that did not say which is which
would be used as though they were.

THE STORE MUST HOLD THIS WORKING TREE, AND IT IS THE IDS THAT SAY SO
--------------------------------------------------------------------
Same rule and same reason as check-routing.py, which learned it the hard way:
one store at %APPDATA%\\Heron\\knowledge serves every checkout on the machine,
so a COUNT can match while the store holds another session's library. Timings
over the wrong library look completely normal.
"""

import os
import platform
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as FRAG                      # noqa: E402

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")

# How many requests to time. Small enough to run in seconds, large enough that
# a median means something. Every request is a real declared utterance.
DEFAULT_REQUESTS = 30


def library():
    """(fragment ids, declared utterances) - through the canonical loader.

    THE PARSE IS heron_fragment.load_all()'s. The first version read and
    parsed each fragment.yaml here with its own `except Exception: continue`,
    which skipped a malformed one in silence - so the "library" line would have
    been short by one and nothing would have said why. D-48 settled that one
    broken part costs one part and that the part is NAMED; load_all() returns
    its problems and main() prints them.

    THE IDS AND NOT THE COUNT - a count is not an identity, which
    check-routing.py learned when three sessions each had exactly 226
    fragments and the store held another session's library.
    """
    found, problems = FRAG.load_all(FRAGMENTS)
    ids = set(found)
    said = []
    for frag in found.values():
        for phrase in (frag.data.get("utterances") or []):
            said.append(phrase)
    return ids, said, problems


def sample(phrases, count):
    """A spread across the library, taken deterministically.

    Every run must ask the same questions or two runs cannot be compared, so
    this strides through the sorted list rather than shuffling. Sorting first
    also stops the sample being an accident of directory order.
    """
    phrases = sorted(set(phrases))
    if not phrases or count >= len(phrases):
        return phrases
    stride = len(phrases) / float(count)
    return [phrases[int(i * stride)] for i in range(count)]


def peak_bytes():
    """Peak RSS for this process, or None where it cannot be read.

    NOT ZERO WHERE IT CANNOT BE READ. `resource` is absent on Windows, and
    reporting 0 MB there would be a measurement nobody took wearing the
    clothes of one that was.

    ru_maxrss is kilobytes on Linux and bytes on macOS - a unit difference
    that silently makes one of them look a thousand times better than the
    other if it is not converted.
    """
    try:
        import resource
    except ImportError:
        return None
    raw = resource.getrusage(resource.RUSAGE_SELF).ru_maxrss
    return raw if sys.platform == "darwin" else raw * 1024


class Timer(object):
    """Milliseconds per stage, kept as every reading rather than a running sum.

    The worst case is the interesting one here - D-49's failure was a single
    call taking thirty minutes while the average stayed respectable - and an
    average cannot be un-averaged after the fact.

    Peak RSS is kept the same way, as GROWTH ACROSS each stage. See the
    module docstring for why that is the honest reading of a high-water
    mark and not a stage's own peak.
    """

    def __init__(self):
        self.readings = {}
        self.grew = {}
        self.order = []

    def time(self, stage, fn):
        before = peak_bytes()
        started = time.perf_counter()
        result = fn()
        elapsed = (time.perf_counter() - started) * 1000.0
        after = peak_bytes()
        if stage not in self.readings:
            self.readings[stage] = []
            self.grew[stage] = []
            self.order.append(stage)
        self.readings[stage].append(elapsed)
        self.grew[stage].append(None if before is None or after is None
                                else after - before)
        return result

    def cost(self, stage):
        """(measured, total growth in bytes) across every run of a stage."""
        values = self.grew.get(stage) or []
        real = [v for v in values if v is not None]
        if not real:
            return False, None
        return True, sum(real)

    def stat(self, stage):
        """(runs, median, worst) in milliseconds."""
        values = sorted(self.readings.get(stage) or [])
        if not values:
            return 0, None, None
        middle = len(values) // 2
        if len(values) % 2:
            median = values[middle]
        else:
            median = (values[middle - 1] + values[middle]) / 2.0
        return len(values), median, values[-1]


def mb(size):
    """Bytes as megabytes, or the reason there is no number."""
    if size is None:
        return "not measured"
    return "%.1f MB" % (size / (1024.0 * 1024.0))


def fmt(ms):
    if ms is None:
        return "-"
    if ms >= 100:
        return "%.0f" % ms
    if ms >= 10:
        return "%.1f" % ms
    return "%.2f" % ms


def main(argv):
    revit = None
    if "--revit" in argv:
        revit = argv[argv.index("--revit") + 1]
    requests = DEFAULT_REQUESTS
    if "--requests" in argv:
        requests = int(argv[argv.index("--requests") + 1])

    disk_ids, phrases, problems = library()
    if problems:
        print("FRAGMENTS THAT COULD NOT BE READ  (%d) - D-48: named, never "
              "skipped in silence" % len(problems))
        for line in problems:
            print("  %s" % line)
        print("")
    if not phrases:
        print("No fragment declares an utterance, so there is nothing to ask.")
        return 0

    # The embedding backend import is timed FIRST and on its own, because it is
    # the one cost D-49 proved can dwarf everything else and it happens once
    # per process. Rolling it into the first request would hide it inside a
    # figure that then looks like a slow search.
    clock = Timer()
    clock.time("import heron_embed", lambda: __import__("heron_embed"))

    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_embed as EMBED
    import heron_retrieve as RETRIEVE

    # TIMED, AND IT WAS NOT UNTIL 2026-09-09. `backend()` calls `_load_model()`,
    # which is where the trained encoder is imported - the 1.0 s that became 40
    # and then thirty minutes on an event loop (D-49). The line above times only
    # the MODULE import, which is cheap on every machine.
    #
    # So the first version of this tool reported "import the embedding backend:
    # 6.6 ms" and called it the D-49 measurement, while the cost D-49 is about
    # happened on the next line, untimed. On this container it is invisible
    # because model2vec is not installed and _load_model() returns None at once.
    # On the machine where it matters it would have hidden the whole thing -
    # in the tool written to catch exactly that.
    backend, _why = clock.time("load the trained encoder", EMBED.backend)

    store = clock.time("open the scope", lambda: SCOPE.open_scope(SCOPE.GLOBAL))
    store_ids = set(row["id"] for row in store.fragments())
    if store_ids != disk_ids:
        store.close()
        built, problems = SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        store_ids = set(row["id"] for row in store.fragments())
        print("  (store did not match this working tree; rebuilt %d%s)"
              % (built, "; %d problem(s)" % len(problems) if problems else ""))
        if store_ids != disk_ids:
            print("  the store STILL does not match this working tree. These")
            print("  timings would be over a library that is not on disk, which")
            print("  is not a measurement. Run `python brain/heron_fragment.py`.")
            store.close()
            return 2

    try:
        clock.time("index for keywords", lambda: SEARCH.index(store))
        clock.time("index for nearness", lambda: EMBED.index(store))

        asked = sample(phrases, requests)
        short_circuited = 0
        for text in asked:
            clock.time("filter: eligible()",
                       lambda: RETRIEVE.eligible(store, revit))
            hit, _status = clock.time("route: short circuit",
                                      lambda: SEARCH.short_circuit(store, text))
            if hit:
                short_circuited += 1
            clock.time("route: keywords",
                       lambda: list(SEARCH.keywords(store, text, limit=60)))
            clock.time("route: nearness",
                       lambda: list(EMBED.nearest(store, text, limit=60)))
            clock.time("the stack: retrieve()",
                       lambda: RETRIEVE.retrieve(store, text, revit=revit))
            clock.time("the whole lookup: find()",
                       lambda: RETRIEVE.find(store, text, revit=revit))
    finally:
        store.close()

    print("BRAIN LATENCY BASELINE")
    print("=" * 70)
    print("machine   %s %s, Python %s"
          % (platform.system(), platform.machine(), platform.python_version()))
    print("backend   %s" % backend)
    print("library   %d fragments, %d declared utterances"
          % (len(disk_ids), len(phrases)))
    print("asked     %d request(s)%s"
          % (len(asked), ", filtered to Revit %s" % revit if revit else ""))
    print("")
    print("  %-30s %6s  %10s  %10s" % ("stage", "runs", "median ms", "worst ms"))
    print("  " + "-" * 62)
    for stage in clock.order:
        runs, median, worst = clock.stat(stage)
        print("  %-30s %6d  %10s  %10s"
              % (stage, runs, fmt(median), fmt(worst)))
    print("")
    # RESOURCE COST, the other half of docs/28's "execution time and resource
    # cost". Peak RSS through resource.getrusage, because it counts NATIVE
    # memory and D-49's cost was a native model import - tracemalloc would
    # have reported a few megabytes and missed the whole thing.
    total = peak_bytes()
    if total is None:
        print("PEAK MEMORY   not measured on %s - `resource` is a Unix module, "
              "and 0 MB\n              would be a measurement nobody took."
              % platform.system())
    else:
        print("PEAK MEMORY   %s for the whole process, at its high-water mark"
              % mb(total))
        print("")
        print("  %-30s %14s" % ("stage", "grew by"))
        print("  " + "-" * 46)
        for stage in clock.order:
            measured, grew = clock.cost(stage)
            print("  %-30s %14s"
                  % (stage, mb(grew) if measured else "not measured"))
        print("")
        print("  GREW BY IS NOT A STAGE'S OWN PEAK. ru_maxrss is a high-water")
        print("  mark for the process and never falls, so a stage that")
        print("  allocated two gigabytes and freed them shows 0.0 MB -")
        print("  correctly, and uselessly. Only growth is attributable.")
    print("")
    print("Read the WORST column first. D-49's thirty-minute hang was one call,")
    print("and a median would have called that afternoon healthy.")
    print("")
    if backend == "lexical":
        print("  `load the trained encoder` MEANS NOTHING ON THIS RUN. The backend")
        print("  is `lexical`, so _load_model() returns None immediately and there")
        print("  is no encoder to load. That line is the D-49 measurement and it")
        print("  only measures anything where model2vec is installed - which is")
        print("  the machine that matters and is not this one.")
    else:
        print("  `load the trained encoder` IS the D-49 measurement. It is the")
        print("  import that reached thirty minutes on an asyncio event loop, and")
        print("  heron_brain.warm() exists to keep it off a request thread.")
    print("")
    if short_circuited:
        print("  find() IS FLATTERED HERE AND THE NUMBER MUST NOT BE QUOTED ALONE.")
        print("  %d of %d requests took the identity short circuit, because the"
              % (short_circuited, len(asked)))
        print("  questions asked are fragments' OWN declared utterances. That is")
        print("  the best case by construction - one lookup, no search - and it is")
        print("  Step 9 working exactly as designed. A request phrased in somebody")
        print("  else's words costs what retrieve() costs, not what find() shows.")
    else:
        print("  No request took the identity short circuit, so find() here is")
        print("  the full stack rather than the best case.")
    print("")
    print("This is one machine on one day. It is a baseline to compare against,")
    print("not a budget - docs/19 s2 proposes budgets and nothing implements one,")
    print("and taking this run as the standard would make this container's speed")
    print("the thing Heron is held to.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
