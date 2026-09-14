# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-SEC-012
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The secret manager - a handle in a log line leaks nothing.

    python tests/test_secrets.py

WHAT IT PROVES
  1. A HANDLE NEVER PRINTS ITS VALUE - not in str(), not in repr(), not in a
     format string, not inside an exception message. This is the leak that
     actually happens: nobody writes a token into a log on purpose, an object
     is put in one and its __repr__ is helpful.

  2. A RESOLVED VALUE IS REDACTED EVERYWHERE AFTERWARDS, and it is named by
     its handle rather than by the pattern that matched it.

  3. A SECRET THIS PROCESS NEVER RESOLVED IS CAUGHT BY SHAPE - forge tokens,
     provider keys, cloud access keys, chat-app tokens, a bearer header.

  4. THE MARKER IS A FIXED WIDTH. Stars of the same length publish the length,
     and a length narrows down which credential it was.

  5. REDACTION IS REPORTED. A redaction nothing reports is a leak nobody can
     investigate, and redaction is idempotent - running it twice changes
     nothing further.

  6. A CREDENTIAL STORE INSIDE THE WORKSPACE IS REFUSED. docs/12 s5a.1, and
     D-07 makes a leak into this repository permanent.

  7. A MISSING SECRET RAISES RATHER THAN RETURNING None. A caller carrying on
     with None sends an unauthenticated request and reports whatever comes
     back.

  8. A SECRET OFFERED AS A FRAGMENT INPUT IS REFUSED, by its value's shape and
     not by the field's name - calling a field 'token' is not what makes it
     dangerous (docs/12 s5a.3).

  9. NO TOKEN-SHAPED LITERAL IS WRITTEN IN EITHER FILE. A demonstration that
     push protection blocks demonstrates nothing.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_secrets as SECRETS                               # noqa: E402

FAILURES = []

# Assembled, never written out - the rule this file also asserts at claim 9.
FORGE = "gh" + "p_" + ("Z9y8X7w6V5u4" * 3)
PROVIDER = "sk" + "-" + ("aBcDeFgHiJkLmNoP" * 2)
CLOUD = "AK" + "IA" + "IOSFODNN7EXAMPLE"
CHAT = "xo" + "xb-" + "1234567890-abcdefghij"
NAME = SECRETS.HANDLE_PREFIX + "forge-token"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    print("1. A handle never prints its value")
    handle = SECRETS.Handle(NAME)
    secrets = SECRETS.Secrets(backend={NAME: FORGE}.get, workspace=ROOT)
    check(str(handle) == NAME and FORGE not in str(handle),
          "str() gives the handle")
    check(FORGE not in repr(handle), "repr() gives the handle")
    check(FORGE not in "a line about %s and %r" % (handle, handle),
          "and neither reaches a format string")
    try:
        raise RuntimeError("failed using %s" % handle)
    except RuntimeError as exc:
        check(FORGE not in str(exc),
              "nor an exception message built from the handle")
    check(FORGE not in str(vars(handle)),
          "the object holds no value to find")

    print()
    print("2. A resolved value is redacted afterwards, by handle")
    value = secrets.resolve(handle)
    check(value == FORGE, "resolve() returns the value for the call itself")
    clean, found = secrets.redact("sent Authorization: Bearer %s" % value)
    check(FORGE not in clean, "the value is gone from the line")
    check(found and found[0] == NAME,
          "and it is reported by its handle, not by a pattern")

    print()
    print("3. A secret nobody resolved is caught by its shape")
    fresh = SECRETS.Secrets()
    for label, sample in (("forge token", FORGE), ("provider key", PROVIDER),
                          ("cloud access key", CLOUD), ("chat token", CHAT)):
        clean, found = fresh.redact("the log said %s and stopped" % sample)
        check(sample not in clean and bool(found),
              "an unregistered %s is redacted anyway" % label)
    clean, found = fresh.redact("Authorization: Bearer %s" % ("q" * 40))
    check("q" * 40 not in clean, "a bearer header is redacted by shape")

    print()
    print("4 and 5. Fixed width, reported, and idempotent")
    clean, _found = fresh.redact("x %s y" % FORGE)
    check(SECRETS.MARKER in clean and len(SECRETS.MARKER) < len(FORGE),
          "the marker is a fixed width, not stars of the value's length")
    again, found_again = fresh.redact(clean)
    check(again == clean and found_again == [],
          "redacting an already clean line changes nothing and reports nothing")
    check(fresh.redact(None) == (None, []),
          "None goes through without an exception")

    print()
    print("6. A credential store in the tree is refused")
    refused = False
    try:
        secrets.use_store(os.path.join(ROOT, ".secrets.json"))
    except ValueError as exc:
        refused = "public repository is permanent" in str(exc)
    check(refused, "a store inside the workspace is refused, with the reason")
    outside = secrets.use_store("/somewhere/else/heron.credentials")
    check(outside.endswith("heron.credentials"),
          "a store outside it is allowed")

    print()
    print("7. A missing secret raises rather than returning None")
    for call, what in (
            (lambda: secrets.resolve(SECRETS.Handle(
                SECRETS.HANDLE_PREFIX + "not-there")),
             "a handle the store does not have"),
            (lambda: SECRETS.Secrets().resolve(handle),
             "a resolve with no store configured at all")):
        raised = False
        try:
            call()
        except LookupError:
            raised = True
        check(raised, "%s raises" % what)
    bad = False
    try:
        SECRETS.Handle("github-token")
    except ValueError:
        bad = True
    check(bad, "a handle without the prefix is refused when it is made")

    print()
    print("8. A secret offered as a fragment input is refused")
    offending = secrets.refuse_secret_input({
        "category": "OST_DuctCurves",
        "notes": "use %s to authenticate" % CLOUD,
        "credential": handle,
        "other": SECRETS.HANDLE_PREFIX + "another",
    })
    keys = [key for key, _code, _why in offending]
    check(keys == ["notes"],
          "the field carrying a secret VALUE is named, and only that one")
    check("credential" not in keys and "other" not in keys,
          "a Handle and a handle string are both fine to pass")

    print()
    print("9. Neither file contains a token-shaped literal")
    shapes = re.compile(r"gh[pousr]_[A-Za-z0-9]{16,}|AKIA[0-9A-Z]{16}"
                        r"|\bsk-[A-Za-z0-9]{16,}|xox[baprs]-[A-Za-z0-9\-]{10,}")
    for name in ("brain/heron_secrets.py", "tests/test_secrets.py"):
        text = io.open(os.path.join(ROOT, name), encoding="utf-8").read()
        check(not shapes.search(text),
              "%s has no token-shaped literal in it" % name)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the handle travels, the value does not, and nothing logs one")
    return 0


if __name__ == "__main__":
    sys.exit(main())
