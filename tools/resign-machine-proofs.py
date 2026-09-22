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

A NAME IS NOT YAML. The name and the date are rendered by PyYAML rather than
pasted in, and the whole fragment is re-read BEFORE anything is written: if
the name that comes back is not the name that was typed, nothing is written
at all. Measured 2026-09-22 on the version without it - `--by "Ajmal: PS"`
left a fragment that no longer parsed, `--by "Ajmal #2"` left one signed
"Ajmal", and both printed `signed` and exited 0. `heron_validate.py accept`
writes the same field and then re-reads the file it wrote, restoring the
original if anything moved; this is that guard one step earlier, so a run
that cannot be finished leaves every file as it found it.
"""

import argparse
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
MACHINE = re.compile(r"(claude|opus|gpt|sonnet|assistant)", re.IGNORECASE)


def _yaml():
    """PyYAML, or a sentence saying what to install. It is REQUIRED, not
    optional - tools/check-dependencies.py and requirements.txt both say so."""
    try:
        import yaml
    except ImportError:
        sys.stderr.write("This needs PyYAML: pip install --user pyyaml\n")
        raise
    return yaml


def _field(name, value, indent="  "):
    """One YAML field, written the way YAML itself would write it.

    Pasting the value in raw is what went wrong: a colon in a name leaves a
    fragment that no longer parses, and a hash leaves one that parses and
    says something shorter than what was typed. safe_dump quotes whatever
    has to be quoted, and a value that spans lines comes back indented under
    the key rather than landing at the top level of the file.
    """
    rendered = _yaml().safe_dump({name: value}, allow_unicode=True,
                                 default_flow_style=False,
                                 width=10 ** 6).rstrip("\n")
    return "\n".join(indent + line for line in rendered.split("\n"))


def _unwritable(text, by, date):
    """Why this text must NOT be written, or None if it is safe.

    GOLDEN RULE 4 - a record is never destroyed. brain/fragments/ is that
    record, and the only thing standing between a typed name and a proof
    nobody can read is this function.
    """
    yaml = _yaml()
    try:
        book = yaml.safe_load(text)
    except yaml.YAMLError as why:
        return "it would no longer read as YAML - %s" % type(why).__name__
    if not isinstance(book, dict) or not isinstance(book.get("proof"), dict):
        return "it would no longer carry a proof block"
    proof = book["proof"]
    if proof.get("by") != by:
        return ("the name would come back as %r rather than %r"
                % (proof.get("by"), by))
    if str(proof.get("date")) != str(date):
        return ("the date would come back as %r rather than %r"
                % (proof.get("date"), date))
    return None


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

    # heron_validate.accept refuses this in one line - "accept needs a
    # person's name" - and the same act must not have two answers. Without
    # it, --by "   " wrote an EMPTY by: field and said it had signed.
    if args.by is not None and not args.by.strip():
        print("a name that is only spaces is not a signature.")
        print('  python tools/resign-machine-proofs.py --by "Your Name"')
        return 2

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

    # BUILT FIRST, WRITTEN SECOND. Sixteen fragments were the case this tool
    # was made for; a run that fails on the ninth must not leave eight
    # rewritten and eight not.
    name = args.by.strip()
    signed = _field("by", name)
    dated = _field("date", args.date)
    ready = []
    for slug, path, _by in found:
        text = io.open(path, "r", encoding="utf-8").read()
        block = re.search(r"^proof:\n(.*?)(?=\n\w|\Z)", text, re.S | re.M)
        before = block.group(1)
        # A LAMBDA, NOT A STRING. re.sub reads a backslash in the replacement
        # as an escape, and a name is not a regular expression.
        after = re.sub(r"^  by:.*$", lambda _m: signed, before, count=1,
                       flags=re.M)
        # A DATE IS PART OF A SIGNATURE. Every one of these had none, and a
        # proof nobody can date is a proof nobody can question.
        if re.search(r"^  date:", after, re.M):
            after = re.sub(r"^  date:.*$", lambda _m: dated, after, count=1,
                           flags=re.M)
        else:
            after = dated + "\n" + after
        candidate = text[:block.start(1)] + after + text[block.end(1):]

        problem = _unwritable(candidate, name, args.date)
        if problem:
            print("NOTHING WAS WRITTEN. %s would be damaged: %s" % (slug, problem))
            print("Every fragment is exactly as it was.")
            return 1
        ready.append((slug, path, candidate))

    for slug, path, candidate in ready:
        io.open(path, "w", encoding="utf-8", newline="").write(candidate)
        print("  signed  %s" % slug)

    print("")
    print("%d proof(s) now signed by %s on %s." % (len(found), args.by.strip(), args.date))
    print("Their status was NOT touched - promoting is a separate act, as it always was.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
