# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Replace a MACHINE's name in a recorded proof with a person's.

    python tools/resign-machine-proofs.py --list
    python tools/resign-machine-proofs.py --by "Ajmal PS"

WHY THIS EXISTS, AND WHY IT IS NOT `accept`. Sixteen fragments carry a proof
signed `"Claude Opus 5, at Ajmal PS's PC"` and no date. D-30 says the machine
gathers evidence and a PERSON signs, so that is not a signature - it is the
thing the rule exists to forbid, written into the field meant to prevent it.

`heron_validate.py accept` cannot fix them: it reads a draft from
brain/proof-drafts/ and none of the sixteen has one. The proof is already
written into fragment.yaml. So this tool changes the one field that is wrong
and leaves every other line exactly as recorded.

IT DOES NOT JUDGE THE EVIDENCE, and must not be read as endorsing it. The
evidence was checked separately and is good - list-levels records eleven levels
with negative elevations read correctly, a negative case on a second document,
and a second route that agreed. What was missing was a person's name.

RUNNING IT IS THE SIGNATURE. Whoever types --by is saying they have read the
proofs and stand behind them, which is what D-30 asks for and what a machine
cannot do on their behalf.
"""

import argparse
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
MACHINE = re.compile(r"(claude|opus|gpt|sonnet|assistant)", re.IGNORECASE)


def machine_signed():
    """Every fragment whose recorded proof is signed by something that is not a person."""
    found = []
    for slug in sorted(os.listdir(FRAGMENTS)):
        path = os.path.join(FRAGMENTS, slug, "fragment.yaml")
        if not os.path.isfile(path):
            continue
        text = io.open(path, "r", encoding="utf-8").read()
        block = re.search(r"^proof:\n(.*?)(?=\n\w|\Z)", text, re.S | re.M)
        if not block:
            continue
        by = re.search(r"^  by:\s*(.+)$", block.group(1), re.M)
        if by and MACHINE.search(by.group(1)):
            found.append((slug, path, by.group(1).strip()))
    return found


def main(argv):
    parser = argparse.ArgumentParser()
    parser.add_argument("--by", help="the person signing, exactly as they write their name")
    parser.add_argument("--list", action="store_true", help="show them and change nothing")
    parser.add_argument("--date", default=None, help="the date to record (default: today)")
    args = parser.parse_args(argv[1:])

    found = machine_signed()
    if not found:
        print("No proof is signed by a machine. Nothing to do.")
        return 0

    if args.list or not args.by:
        print("%d proof(s) signed by a machine:" % len(found))
        for slug, _path, by in found:
            print("  %-32s %s" % (slug, by))
        if not args.by:
            print("")
            print("Read them first - tools/resign-machine-proofs.py explains why - then:")
            print('  python tools/resign-machine-proofs.py --by "Your Name"')
        return 0

    if args.date is None:
        import datetime
        args.date = datetime.date.today().isoformat()

    for slug, path, _by in found:
        text = io.open(path, "r", encoding="utf-8").read()
        block = re.search(r"^proof:\n(.*?)(?=\n\w|\Z)", text, re.S | re.M)
        before = block.group(1)
        after = re.sub(r"^  by:.*$", "  by: %s" % args.by.strip(), before, count=1, flags=re.M)
        # A DATE IS PART OF A SIGNATURE. Every one of these had none, and a
        # proof nobody can date is a proof nobody can question.
        if re.search(r"^  date:", after, re.M):
            after = re.sub(r"^  date:.*$", "  date: '%s'" % args.date, after, count=1, flags=re.M)
        else:
            after = "  date: '%s'\n" % args.date + after
        text = text[:block.start(1)] + after + text[block.end(1):]
        io.open(path, "w", encoding="utf-8", newline="").write(text)
        print("  signed  %s" % slug)

    print("")
    print("%d proof(s) now signed by %s on %s." % (len(found), args.by.strip(), args.date))
    print("Their status was NOT touched - promoting is a separate act, as it always was.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
