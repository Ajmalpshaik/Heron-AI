# Heron-Agent:  HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Read the shortlist again, properly, and only the shortlist.

    python brain/heron_rerank.py              what it would cost, and what is here

Stage 7 of docs/work-notes/plans/rag/00-structure.md s6. Closes R-41 and R-42.

WHAT A RE-RANKER IS, AND WHY IT IS NOT ANOTHER ROUTE
---------------------------------------------------
The two routes in heron_retrieve.py score a question and a passage SEPARATELY
and then compare the two scores. A cross-encoder reads them TOGETHER - one
model call per (question, passage) pair - so it can notice that a clause about
"the hose reel cabinet" answers "where do I put the fire cabinet" and a clause
about "cabinet finishes" does not. That is the single biggest quality jump
available to this stack, and it is the slowest thing in it.

So it runs on the top ~20 only, which is what docs/05 s4.4 already specifies.
Twenty pairs is a fixed cost per question. The whole library is not, and a
re-ranker let loose on it would turn one question into a scan.

THE ABSENT CASE IS THE NORMAL CASE, AND IT IS THE ONE THAT IS TESTED (R-41)
---------------------------------------------------------------------------
heron_embed.py set this contract and this file keeps it: two states, and
`backend()` says out loud which one answered. With no re-ranker installed the
shortlist comes back in EXACTLY the order fusion produced - not a worse order,
the same one - and the caller is told the re-ranker did not run.

    ABSENT IS SLOWER TO BE RIGHT. IT IS NEVER BROKEN.

`scores()` returns None rather than raising, for every reason it can fail:
nothing installed, weights not downloadable, a model that loads and then throws
on a long passage. A retrieval stack that stops answering because an optional
quality step is missing is worse than one that never had the step.

WHAT THIS COSTS TO INSTALL, SAID BEFORE ANYTHING IS DOWNLOADED (R-77)
---------------------------------------------------------------------
`announcement()` is the whole reason this module has a command line. A
cross-encoder is the largest optional piece in this plan after a document
parser, and docs/.../01-requirements.md R-79 records the field reading as
**500 MB to 2 GB** - a torch build, not the weights, is most of it. Somebody on
a metered connection gets to read that and say no BEFORE a download starts,
which is the only moment at which saying no is any use.

There is no cheap tier here and pretending otherwise would be the useful lie.
The encoder has one: model2vec's potion-base-8M is a few tens of megabytes,
which is why `heron_embed` can offer meaning to almost any machine. Nothing
equivalent exists for cross-encoding - reading a pair together is what costs
the parameters. So this module offers ONE backend and states its size, rather
than a ladder whose bottom rung does not exist.

D-01 STILL BINDS IT: per-user, no administrator rights (R-42). The install line
in `announcement()` is `--user` for that reason, and model2vec already proved
a trained model can be installed that way on a locked-down machine.

WHAT HAS NOT BEEN MEASURED, AND WHY THE NUMBER IS ABSENT RATHER THAN ESTIMATED
-----------------------------------------------------------------------------
**Stage 7 asks for a before and an after, at the same corpus size, and only the
BEFORE exists.** The before is in brain/retrieval-history.md, taken 2026-09-11
at 360 fragments and 62 chunks: the spreads, the gaps, and the twelve-question
run that found no floor.

The after has NOT been taken. This container's network refuses huggingface.co
(`CONNECT tunnel failed, response 403`, re-checked 2026-09-11), so no
cross-encoder weights can be fetched, so there is nothing to measure. That is a
fact about ONE CONTAINER, recorded with its environment attached, exactly as
heron_embed.py recorded the same block on 2026-08-28.

**No number here was estimated to fill the gap.** A re-ranker's whole claim is
that it improves an order, and an improvement nobody measured is a feeling.
docs/.../03-working-note.md carries the open clause as W-9; NEEDS-CHECKING.md
`A10` is the run on a machine that can reach a model. (`A7` is the ENCODER's
version of the same row, closed 2026-09-06 on the owner's PC - which is the
evidence that a trained model can be installed per-user at all.)

A TEST MAY INJECT A SCORER. A MEASUREMENT MAY NOT.
--------------------------------------------------
tests/test_rerank.py installs a stub backend to prove the plumbing: that the
order changes, that it is reported, that at most twenty pairs are ever scored,
that a backend which throws is absorbed. That is a test of THIS FILE and it is
legitimate.

It is not a measurement of re-ranking, and it is not written up as one. The
difference is that a stub's opinion about which clause answers a question is
this session's opinion wearing a model's clothes.
"""

import os
import sys
import threading

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

# How many pairs are ever scored. docs/05 s4.4's "top ~20", and it is a CEILING
# rather than a target: a shortlist of five costs five pairs, not twenty.
SHORTLIST = 20

ABSENT = "absent"
CROSS_ENCODER = "cross-encoder"

# The default model, overridable the way heron_embed's is, so a machine with a
# different one already cached does not have to fetch this one.
DEFAULT_MODEL = "cross-encoder/ms-marco-MiniLM-L-6-v2"
MODEL_ENV = "HERON_RERANK_MODEL"

# The size R-79 records from the field reading on 2026-09-10. NOT measured here
# and not presented as though it were - this container cannot reach the host
# that would answer. `pip download --no-deps sentence-transformers torch` on a
# machine that can reach it is the command that confirms it.
SIZE = "500 MB to 2 GB"

# THE VARIABLES THAT STOP A DOWNLOAD STARTING BY ITSELF.
#
# R-77 says the size is announced BEFORE the download, and the first version
# of this module announced it only from the command line while warm() - which
# the MCP server calls at startup - went straight to CrossEncoder(), which
# FETCHES THE WEIGHTS when they are not cached. So on a machine that had
# `sentence-transformers` installed, production start would have pulled
# several hundred megabytes in the background with nobody told. The
# announcement was real and it was on the wrong path. Found by a review
# 2026-09-11.
#
# These are huggingface_hub's own switches, set around the load and put back
# afterwards: a model already on disk loads, and a model that is not simply
# fails, which this module already treats as "no re-ranker here".
_OFFLINE = ("HF_HUB_OFFLINE", "TRANSFORMERS_OFFLINE")

_CACHE = []                      # [scorer] or [None] once it has been settled
_WARMING = threading.Event()
_WARM_THREAD = [None]


def announcement():
    """What would be installed, what for, and how big - before any download.

    R-77 asks for the announcement BEFORE the download rather than a progress
    bar during it, and the distinction is the whole requirement: a person on a
    site connection can decline 2 GB, and cannot un-download it.
    """
    return "\n".join([
        "THE RE-RANKER IS OPTIONAL AND IT IS THE LARGEST OPTIONAL PIECE.",
        "",
        "  package   sentence-transformers (which pulls in torch)",
        "  model     %s" % (os.environ.get(MODEL_ENV) or DEFAULT_MODEL),
        "  size      %s - mostly torch, not the weights" % SIZE,
        "  for       reading the top %d (question, passage) pairs together, so"
        % SHORTLIST,
        "            a shortlist can be re-ordered by what actually answers",
        "  needs     huggingface.co reachable, once, to fetch the weights",
        "",
        "  install   pip install --user sentence-transformers",
        "",
        "Per-user, no administrator rights (D-01, R-42).",
        "",
        "WITHOUT IT HERON STILL ANSWERS. The shortlist comes back in the order",
        "fusion produced and every answer says the re-ranker did not run.",
        "Slower to be right, never broken.",
    ])


def warm():
    """Import the cross-encoder on a BACKGROUND thread, and return at once.

    NOT a precaution - a repeat. `A8` in NEEDS-CHECKING.md is thirty minutes of
    a real Claude Code tool call waiting on `import model2vec` inside an MCP
    handler, on the asyncio event loop, because a 1.0 s import was the first
    thing a request touched. Installing the encoder to prove search understood
    meaning is what made the server stop replying.

    A torch import is HEAVIER than that one. So the same rule, from the start
    this time rather than after the stack dump: a heavy optional import happens
    here, once, off the request path, and until it finishes every caller is told
    the re-ranker did not run.
    """
    if _CACHE or _WARMING.is_set():
        return
    _WARMING.set()

    def run():
        try:
            _load()
        finally:
            _WARMING.clear()

    t = threading.Thread(target=run, name="heron-rerank-warm", daemon=True)
    _WARM_THREAD[0] = t
    t.start()


def _load():
    """The cross-encoder this machine has, or None. Never raises.

    None is a NORMAL return. Nothing installed, no network for the weights, a
    hardened build - all of them mean the same thing to a caller, which is that
    fusion's order stands.
    """
    if _CACHE:
        return _CACHE[0]

    if _WARMING.is_set() and threading.current_thread() is not _WARM_THREAD[0]:
        # A warm-up is already importing torch on its own thread. Do NOT import
        # it here as well: this call may be on an event loop, which is the hang
        # warm() exists to prevent. The thread check is not decoration - without
        # it the warm-up thread trips its own guard, loads nothing, clears the
        # flag, and the next request imports on the loop exactly as before.
        # That was the encoder's first version of this fix.
        return None

    try:
        model = _construct(offline=True)
        _CACHE.append(lambda pairs: [float(s) for s in model.predict(pairs)])
        return _CACHE[0]
    except Exception:
        # Deliberately every exception, not ImportError alone. The ways this
        # fails on a real machine are an unreachable weights host, a refused
        # cache directory and a torch build that will not load - none of which
        # is an ImportError, and all of which mean "no re-ranker here".
        pass

    _CACHE.append(None)
    return None


def _construct(offline):
    """Build the cross-encoder. `offline=True` cannot start a download.

    THE ONLY PLACE THE WEIGHTS ARE EVER FETCHED IS offline=False, and the only
    caller that passes it is `fetch()`, which prints announcement() first and
    will not proceed without a yes. Every automatic path - warm(), backend(),
    scores() - comes through here with offline=True and therefore either finds
    the model already on disk or reports `absent`.
    """
    from sentence_transformers import CrossEncoder
    name = os.environ.get(MODEL_ENV) or DEFAULT_MODEL
    if not offline:
        return CrossEncoder(name)

    was = dict((key, os.environ.get(key)) for key in _OFFLINE)
    for key in _OFFLINE:
        os.environ[key] = "1"
    try:
        return CrossEncoder(name)
    finally:
        # PUT BACK, including where it was UNSET. Leaving these on would make
        # every other huggingface user in this process offline too, which is a
        # side effect nobody asked this module for.
        for key, value in was.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value


def fetch(confirmed=False):
    """Download the weights, AFTER the size has been shown. Returns a message.

    R-77's actual shape: the announcement comes first, a person reads it, and
    only then does anything start. `confirmed` is the yes - without it this
    prints what it would cost and downloads nothing, which is the whole
    difference between announcing BEFORE and announcing DURING.
    """
    if not confirmed:
        return (announcement()
                + "\n\nNothing was downloaded. To go ahead:"
                  "\n  python brain/heron_rerank.py --fetch --yes")
    _CACHE[:] = []
    try:
        model = _construct(offline=False)
    except ImportError:
        return ("sentence-transformers is not installed, so there is nothing "
                "to fetch weights for yet:\n  pip install --user "
                "sentence-transformers")
    except Exception as problem:
        return "The weights could not be fetched: %s" % problem
    _CACHE.append(lambda pairs: [float(s) for s in model.predict(pairs)])
    return ("Fetched. %s is on this machine now, and retrieval will use it."
            % (os.environ.get(MODEL_ENV) or DEFAULT_MODEL))


def backend():
    """Which backend answered, and what that means. Reported, never assumed."""
    if _load() is not None:
        # INSTALLED, not "it ran". Whether it ran on a PARTICULAR question is a
        # different fact and it is reported per candidate, by Candidate.why():
        # an identity short circuit answers without any shortlist to re-read.
        return CROSS_ENCODER, ("installed - it reads each (question, passage) "
                               "pair together on any shortlist that reaches it")
    return ABSENT, ("no re-ranker installed - the shortlist keeps the order "
                    "fusion gave it, which is slower to be right and not broken")


def scores(question, passages):
    """A score per passage, higher is better - or None if nothing re-ranked.

    None means "fusion's order stands", and it is the answer on a machine with
    nothing installed, which is most machines. It is never an exception: R-41
    says the absence makes Heron slower to be right, never broken, and a caller
    that has to wrap this in a try block has a re-ranker that can break it.

    At most SHORTLIST pairs are scored. A caller that hands over more gets the
    first SHORTLIST scored and the rest returned as None scores, so the caller
    can tell "the re-ranker had no opinion about this one" from "the re-ranker
    did not run at all" - which are different facts and would otherwise both
    arrive as a missing number.
    """
    if not passages:
        return None

    model = _load()
    if model is None:
        return None

    head = list(passages)[:SHORTLIST]
    try:
        got = model([(question, text or "") for text in head])
    except Exception:
        # A model that loads and then throws - a passage longer than its window
        # is the usual one - is still an absent re-ranker as far as the answer
        # is concerned. It must not become an absent ANSWER.
        return None

    if len(got) != len(head):
        # A backend that returns the wrong number of scores cannot be aligned
        # to the passages it was given, and guessing the alignment would
        # silently re-order the shortlist by nothing at all.
        return None

    return list(got) + [None] * (len(passages) - len(head))


def main(argv):
    if "--fetch" in argv:
        print(fetch(confirmed="--yes" in argv))
        return 0

    print(announcement())
    print("")
    name, why = backend()
    print("Backend: %s" % name)
    print("  %s" % why)
    if name == ABSENT:
        print("")
        print("NOT MEASURED: Stage 7 asks for a before and an after at the "
              "same corpus")
        print("  size. brain/retrieval-history.md has the before. The after "
              "needs a")
        print("  machine that can reach huggingface.co - see A10 in "
              "docs/NEEDS-CHECKING.md.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
