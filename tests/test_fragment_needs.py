#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The compile gate and the executor build a fragment's SCOPE from the same
contract. Runs without Revit.

THIS IS THE `needs` HALF OF test_fragment_imports.py, AND THE GAP IT GUARDS WAS
REAL RATHER THAN HYPOTHETICAL. That test exists because a namespace the gate
compiles against and the executor does not supply is "343 fragments pass a green
gate and one throws at the machine". Exactly the same drift existed one field
over, in the open, and nothing looked at it:

    tools/check-fragments-compile.py wraps each snippet in a method whose
    PARAMETERS ARE ITS DECLARED needs - all of them, whatever they are called.

    RevitFragment.cs supplied THREE NAMES: doc, uidoc and app.

So 196 of 348 fragments declared a host-sourced need the executor had no way to
provide, compiled green against a scope the machine could not reproduce, and
were counted as ready. The library reported 348 built and 20 runnable, and
nothing connected those two numbers to each other.

The executor now generates a typed local per declared need, from the same
contract the gate reads. That makes the two agree BY CONSTRUCTION rather than by
two lists somebody keeps level - which is the fix the imports test could not
have, because namespaces have no contract to be generated from.

WHAT IS LEFT FOR THIS TEST TO DO, then, is the part generation cannot check: a
contract is a file on somebody's disk, and the executor writes its name and type
straight into generated C#. A need called `class`, or typed `IList<Element`,
compiles nowhere and fails at the PC in front of the owner - so it is caught
here, where finding it costs a second.

    python tests/test_fragment_needs.py
"""

import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
EXECUTOR = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")
GLOBALS = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronFragmentGlobals.cs")

# What the globals object carries as real fields. Everything else a fragment
# names has to be generated, and that is the whole point of this file.
FIELDS = ("doc", "uidoc", "app")

# A NEED'S NAME BECOMES A LOCAL VARIABLE, so a C# keyword is not a usable name
# however good it looks in a contract. `params`, `object`, `base` and `event`
# are the ones a Revit contract would plausibly reach for.
KEYWORDS = {
    "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
    "checked", "class", "const", "continue", "decimal", "default", "delegate",
    "do", "double", "else", "enum", "event", "explicit", "extern", "false",
    "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
    "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
    "new", "null", "object", "operator", "out", "override", "params", "private",
    "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
    "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
    "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
    "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
}

IDENTIFIER = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")

# Letters, digits, and the punctuation a generic type name needs. Deliberately
# the same set RevitFragment.IsTypeName accepts - if these two ever disagree,
# the disagreement is a fragment that passes here and is refused at the machine.
TYPE_NAME = re.compile(r"^[A-Za-z_][A-Za-z0-9_.<>,\[\] ]*$")


def contracts():
    """(folder, needs) for every fragment, or None if the library cannot be read."""
    try:
        import yaml
    except ImportError:
        return None

    found = []
    if not os.path.isdir(FRAGMENTS):
        return found

    for folder in sorted(os.listdir(FRAGMENTS)):
        path = os.path.join(FRAGMENTS, folder, "fragment.yaml")
        if not os.path.isfile(path):
            continue
        try:
            with io.open(path, encoding="utf-8") as handle:
                loaded = yaml.safe_load(handle)
        except Exception as failure:                  # noqa: BLE001 - reported, not raised
            found.append((folder, None, str(failure)[:120]))
            continue
        if not isinstance(loaded, dict):
            found.append((folder, None, "fragment.yaml is not a mapping"))
            continue
        contract = loaded.get("contract") or {}
        needs = [n for n in (contract.get("needs") or []) if isinstance(n, dict)]
        found.append((folder, needs, None))
    return found


def executor_generates_scope():
    """
    Does the executor still build scope FROM THE CONTRACT?

    A regex, and therefore a weak check - but the thing it is watching for is
    somebody replacing generation with a fixed list again, which is a large and
    obvious edit rather than a subtle one.
    """
    if not os.path.isfile(EXECUTOR):
        return None
    text = io.open(EXECUTOR, encoding="utf-8").read()
    return {
        "reads_needs": 'ReadObjectArray(request, "needs")' in text,
        "writes_prologue": "__heron[" in text,
        "binds": "BindNeeds(" in text,
    }


def run():
    failures = []
    notes = []

    library = contracts()
    if library is None:
        print("Fragment needs - executor against contracts")
        print("  SKIP  PyYAML is not installed, so the contracts cannot be read.")
        print("        pip install --user pyyaml")
        return 3                                       # the suite's "could not run"

    # PROVE THE PATTERN CAN SEE WHAT IS THERE. A sweep that silently matched
    # nothing passes perfectly and proves nothing, and this repository has been
    # caught by exactly that more than once.
    if len(library) < 50:
        failures.append(
            "only %d fragment(s) were read out of brain/fragments. The library is "
            "several hundred - a check over almost none of it passes and means "
            "nothing" % len(library))

    unreadable = [(f, why) for f, needs, why in library if needs is None]
    for folder, why in unreadable:
        failures.append("%s: contract could not be read - %s" % (folder, why))

    readable = [(f, n) for f, n, why in library if n is not None]

    total_needs = 0
    host_needs = 0
    request_needs = 0
    generated = 0
    runnable_now = 0
    needs_request_values = 0

    for folder, needs in readable:
        extra_host = 0
        wants_request = False

        for need in needs:
            name = need.get("name")
            declared = need.get("type")
            source = need.get("source") or "host"
            total_needs += 1

            if not name or not isinstance(name, str):
                failures.append("%s: a need has no name" % folder)
                continue
            if not declared or not isinstance(declared, str):
                failures.append("%s: need '%s' has no type" % (folder, name))
                continue

            if source == "request":
                request_needs += 1
                wants_request = True
            else:
                host_needs += 1

            if name in FIELDS:
                continue

            generated += 1

            # From here down: this name and this type get WRITTEN INTO C#.
            if not IDENTIFIER.match(name):
                failures.append(
                    "%s: need '%s' is not a C# identifier, and the executor makes "
                    "a local variable of it" % (folder, name))
            elif name in KEYWORDS:
                failures.append(
                    "%s: need '%s' is a C# KEYWORD. It reads perfectly in a "
                    "contract and cannot be a variable name - the fragment would "
                    "be refused at the machine" % (folder, name))

            if not TYPE_NAME.match(declared):
                failures.append(
                    "%s: need '%s' has type '%s', which the executor will not write "
                    "into generated code" % (folder, name, declared))

            if name.startswith("__"):
                failures.append(
                    "%s: need '%s' starts with the prefix the executor reserves for "
                    "its own bag, and would collide with it" % (folder, name))

            if source != "request":
                extra_host += 1

        if wants_request:
            needs_request_values += 1
        elif extra_host == 0:
            runnable_now += 1

    scope = executor_generates_scope()
    if scope is None:
        failures.append("RevitFragment.cs is missing - there is no executor to check")
    else:
        for what, present in sorted(scope.items()):
            if not present:
                failures.append(
                    "the executor no longer looks like it generates scope from the "
                    "contract (%s). If that is deliberate, this test is now wrong; "
                    "if it is not, 196 fragments just became green and unrunnable "
                    "again" % what)

    print("Fragment needs - executor against contracts")
    print("  %d fragment(s), %d declared need(s)" % (len(readable), total_needs))
    print("    %d host-sourced, %d from the caller's request" % (host_needs, request_needs))
    print("    %d name(s) the executor must GENERATE (everything but doc/uidoc/app)"
          % generated)
    print()
    print("  %d fragment(s) need nothing the host cannot bind" % runnable_now)
    print("  %d fragment(s) also need a value from the caller, which has no route yet"
          % needs_request_values)

    if failures:
        print()
        for line in failures[:20]:
            print("  FAIL  " + line)
        if len(failures) > 20:
            print("  ... and %d more" % (len(failures) - 20))
        print("\nFAILED - a contract the gate compiles that the executor cannot run "
              "is a green library and a failure at the PC.")
        return 1

    print()
    print("  PASS  every declared need can be written into generated code, and the")
    print("        executor still builds its scope from the contract itself.")
    print()
    print("This says the two sides agree on the SHAPE of a need. It does not say")
    print("any fragment does the right thing - that is D-30, and it needs a model.")
    return 0


if __name__ == "__main__":
    sys.exit(run())
