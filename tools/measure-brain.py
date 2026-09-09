# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
How long the brain takes, stage by stage. The half nothing was measuring.

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

The incoming master architecture document puts it plainly and it is right:
"without a baseline, improvement cannot be proven."

WHAT IT DELIBERATELY DOES NOT CLAIM
-----------------------------------
`Heron-Agent: none`, and that is a decision rather than an omission.

docs/28 defines HERON-OPS-OBS-011, the Observability Agent, as "latency, token
usage, model calls per request, cost per request". D-01 put every model call in
the HOST. Heron makes none - heron_embed runs a local model, no tokens, no
account, no cost (D-24, D-26). So three of those four fields describe something
Heron cannot see from here, and a file claiming that agent id would make
check-metadata.py report the Observability Agent as BUILT while three quarters
of its declared job stayed impossible.

This is the latency quarter, named as such. The registry row wants correcting;
that is a decision for the owner, not something a tool should paper over by
claiming the id anyway.

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

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")

# How many requests to time. Small enough to run in seconds, large enough that
# a median means something. Every request is a real declared utterance.
DEFAULT_REQUESTS = 30


def load_yaml():
    try:
        import yaml
    except ImportError:
        sys.stderr.write("This needs PyYAML: pip install --user pyyaml\n")
        raise SystemExit(2)
    return yaml


def library():
    """(fragment ids, utterances) from disk. The ids, because a count is not
    an identity - see check-routing.py."""
    yaml = load_yaml()
    ids = set()
    said = []
    for name in sorted(os.listdir(FRAGMENTS)):
        path = os.path.join(FRAGMENTS, name, "fragment.yaml")
        if not os.path.exists(path):
            continue
        with open(path, encoding="utf-8") as fh:
            doc = yaml.safe_load(fh)
        if not doc or not doc.get("id"):
            continue
        ids.add(doc["id"])
        for phrase in doc.get("utterances") or []:
            said.append(phrase)
    return ids, said


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


class Timer(object):
    """Milliseconds per stage, kept as every reading rather than a running sum.

    The worst case is the interesting one here - D-49's failure was a single
    call taking thirty minutes while the average stayed respectable - and an
    average cannot be un-averaged after the fact.
    """

    def __init__(self):
        self.readings = {}
        self.order = []

    def time(self, stage, fn):
        started = time.perf_counter()
        result = fn()
        elapsed = (time.perf_counter() - started) * 1000.0
        if stage not in self.readings:
            self.readings[stage] = []
            self.order.append(stage)
        self.readings[stage].append(elapsed)
        return result

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

    disk_ids, phrases = library()
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
