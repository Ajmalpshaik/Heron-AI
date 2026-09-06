#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The client's hand-rolled needs reader agrees with a real YAML parser, on every
fragment in the library. Runs without Revit.

WHY A HAND-ROLLED READER EXISTS AT ALL. mcp/client/heron_bridge_client.py is
stdlib only and says so at the top - it must run on a machine where nothing is
installed, which is the whole reason `doctor` exists. It now has to send each
fragment's `contract.needs` along with its source, because the executor builds
the snippet's scope from that contract and Revit has never seen fragment.yaml.
So one small block of YAML has to be read without PyYAML.

WHY THAT IS DANGEROUS, AND WHAT THIS TEST IS FOR. A needs list that comes back
SHORT is the worst failure on this path. The executor binds what it was told
about; the snippet then names something nobody put in scope; and the error
arrives at the PC as a compile failure inside generated code, blamed on a
fragment that is perfectly correct. Nothing on the client side would look wrong.

So the reader refuses anything it does not recognise rather than returning a
partial answer - and this test is the evidence that "does not recognise" is rare
enough to be useful: it reads all 348 with PyYAML and with the reader, and the
two must agree entry for entry.

THE ONE NORMALISATION, AND WHY IT IS NOT A FUDGE. PyYAML resolves `optional:
true` to a Python bool; the reader returns the token it read. Both saw the same
four characters. Scalars are compared as text on both sides so that the
comparison is about STRUCTURE - which entries, in which order, with which fields
- rather than about type coercion the executor never sees, since it reads only
`name`, `type` and `source`, all of them strings.

    python tests/test_fragment_needs_reader.py
"""

import io
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))


def as_text(value):
    """A scalar as the characters that were in the file, on both sides."""
    if isinstance(value, bool):
        return "true" if value else "false"
    return str(value)


def normalise(needs):
    return [dict((k, as_text(v)) for k, v in entry.items())
            for entry in needs if isinstance(entry, dict)]


def run():
    try:
        import yaml
    except ImportError:
        print("Fragment needs reader - client against PyYAML")
        print("  SKIP  PyYAML is not installed, so there is nothing to compare")
        print("        the reader WITH. pip install --user pyyaml")
        return 3

    try:
        import heron_bridge_client as client
    except ImportError as failure:
        print("Fragment needs reader - client against PyYAML")
        print("  FAIL  the client could not be imported: %s" % failure)
        return 1

    if not hasattr(client, "fragment_needs"):
        print("Fragment needs reader - client against PyYAML")
        print("  FAIL  the client has no fragment_needs() any more. If the contract")
        print("        is now read some other way, this test is the thing to update -")
        print("        but the reader must not simply be gone while the executor")
        print("        still expects a contract with every source.")
        return 1

    agreed = 0
    refused = []
    disagreed = []
    read = 0

    for folder in sorted(os.listdir(FRAGMENTS) if os.path.isdir(FRAGMENTS) else []):
        path = os.path.join(FRAGMENTS, folder, "fragment.yaml")
        if not os.path.isfile(path):
            continue
        read += 1

        try:
            with io.open(path, encoding="utf-8") as handle:
                loaded = yaml.safe_load(handle)
        except Exception as failure:                   # noqa: BLE001
            disagreed.append((folder, "PyYAML itself could not read it: %s"
                              % str(failure)[:80]))
            continue

        contract = (loaded or {}).get("contract") or {}
        truth = normalise(contract.get("needs") or [])

        mine = client.fragment_needs(path)
        if mine is None:
            refused.append(folder)
            continue

        if normalise(mine) != truth:
            disagreed.append((folder, "%d entry(ies) against PyYAML's %d"
                              % (len(mine), len(truth))))
            continue

        agreed += 1

    print("Fragment needs reader - client against PyYAML")
    print("  %d fragment.yaml read" % read)
    print("  %d agree exactly" % agreed)
    print("  %d refused (read nothing rather than guessing)" % len(refused))
    print("  %d DISAGREE" % len(disagreed))

    failures = []

    # PROVE THE SWEEP SAW SOMETHING. Two empty lists compare equal, and a run
    # over no fragments would pass this file perfectly while checking nothing.
    if read < 50:
        failures.append(
            "only %d fragment.yaml were found. The library is several hundred - a "
            "comparison over almost none of it proves nothing" % read)

    for folder, why in disagreed:
        failures.append("%s: %s" % (folder, why))

    # A refusal is SAFE - the client prints why and sends nothing - but it is
    # still a fragment that cannot be run, so it is a failure here rather than
    # a note. The fix is the needs block, and the reader names the file.
    for folder in refused:
        failures.append(
            "%s: the reader refused its needs block, so this fragment cannot be "
            "sent at all. Safe, and still unrunnable" % folder)

    if failures:
        print()
        for line in failures[:20]:
            print("  FAIL  " + line)
        if len(failures) > 20:
            print("  ... and %d more" % (len(failures) - 20))
        return 1

    print()
    print("  PASS  the stdlib reader and PyYAML see the same contract everywhere.")
    print()
    print("This is about READING the contract, not about honouring it. Whether the")
    print("executor then binds those needs correctly is D-30, and needs a model.")
    return 0


if __name__ == "__main__":
    sys.exit(run())
