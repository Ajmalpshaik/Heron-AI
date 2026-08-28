#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Settings and platform health. Runs without Revit.

TWO REAL BUGS ARE PINNED HERE, both found by writing these agents rather than
by reasoning about them, and both invisible to a compiler:

1. `write.enabled` was READ by HeronPermissions but never DECLARED in
   HeronConfig.Defaults - and Load() adopts a file value only for a declared
   key. So setting it to true in the config would have been silently ignored,
   and writing could never have been turned on, while the refusal told the user
   to set the very thing they had just set. The check below is the general form:
   every key the C# reads must be declared.

2. The client's response deadline was the constant 90.0 while the add-in's
   operationTimeoutSeconds was configurable at 60. Correct only by coincidence.
   Raise the add-in's to 120 - reasonable on a slow model - and the client gave
   up first, replacing "Revit started it and is still working" with "no
   answer". The check below asserts the ordering holds at every value, not just
   the default.

    python tests/test_config_and_health.py
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_config as cfg                                   # noqa: E402
import heron_health as hp                                    # noqa: E402

CONFIG_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronConfig.cs")
CS_DIRS = [os.path.join(ROOT, "platform"), os.path.join(ROOT, "revit")]

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def csharp_declared():
    """The keys HeronConfig.Defaults declares."""
    text = io.open(CONFIG_CS, encoding="utf-8").read()
    block = text[text.index("Defaults ="):]
    block = block[:block.index("};")]
    return dict(re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]*)"\s*\}', block))


def csharp_keys_read():
    """
    Every config key the C# actually reads.

    Covers both the literal form - config.GetBool("write.enabled", false) - and
    the constant form, which is what HeronPermissions uses and what a
    literal-only scan would have missed entirely.
    """
    keys = set()
    for root in CS_DIRS:
        for dirpath, _, filenames in os.walk(root):
            for name in filenames:
                if not name.endswith(".cs"):
                    continue
                text = io.open(os.path.join(dirpath, name), encoding="utf-8").read()
                keys.update(re.findall(r'\.Get(?:Int|Bool)?\(\s*"([^"]+)"', text))
                keys.update(re.findall(
                    r'public\s+const\s+string\s+\w*Key\w*\s*=\s*"([^"]+)"', text))
    return keys


def main():
    declared = csharp_declared()

    print("The two settings tables agree")
    check(set(cfg.DEFAULTS) == set(declared),
          "Python DEFAULTS and C# Defaults declare the same keys"
          + ("" if set(cfg.DEFAULTS) == set(declared) else
             "  <- only in Python: %s ; only in C#: %s"
             % (sorted(set(cfg.DEFAULTS) - set(declared)),
                sorted(set(declared) - set(cfg.DEFAULTS)))))

    for key in sorted(set(cfg.DEFAULTS) & set(declared)):
        check(cfg.DEFAULTS[key] == declared[key],
              "%s defaults to %r on both sides" % (key, declared[key]))

    print()
    print("BUG 1 - every key the C# reads is declared, or it is silently ignored")
    read = csharp_keys_read()
    check(len(read) > 0, "found %d config key(s) being read in the C#" % len(read))
    for key in sorted(read):
        check(key in declared,
              "'%s' is read AND declared - an undeclared key is dropped by Load()" % key)

    print()
    print("BUG 2 - the client's deadline is always beyond the add-in's")
    for operation in (1, 10, 30, 60, 90, 120, 600, 3000):
        values = dict(cfg.DEFAULTS)
        values["revit.operationTimeoutSeconds"] = str(operation)
        client = cfg.response_timeout(values)
        server = cfg.operation_timeout(values)
        check(client > server,
              "operationTimeout=%s -> add-in %g, client %g (client must be higher)"
              % (operation, server, client))

    print()
    print("And the default is unchanged, so nothing proven in Steps 1-5 moves")
    check(cfg.response_timeout(dict(cfg.DEFAULTS)) == 90.0,
          "with defaults the client deadline is still exactly 90.0")

    print()
    print("Settings are a closed set - a typo must not become a setting")
    tmp = os.path.join(os.environ.get("TMPDIR", "/tmp"), "heron-test.config")
    io.open(tmp, "w", encoding="utf-8").write(
        u"# a comment\n"
        u"write.enabled = true\n"
        u"revit.operationTimeoutSeconds=45\n"
        u"write.enabledd = true\n"          # typo - must be ignored
        u"nonsense\n"                        # no '=' - must be ignored
        u"totally.made.up = 99\n")
    loaded = cfg.load(tmp)
    check(cfg.writing_enabled(loaded), "a declared key is read from the file")
    check(cfg.operation_timeout(loaded) == 45.0, "and so is a numeric one")
    check("write.enabledd" not in loaded, "a typo'd key is NOT adopted")
    check("totally.made.up" not in loaded, "an undeclared key is NOT adopted")
    os.remove(tmp)

    print()
    print("A missing or unreadable file is the defaults, never an error")
    check(cfg.load("/no/such/path/heron.config") == cfg.DEFAULTS,
          "a missing file gives exactly the defaults")

    print()
    print("The add-in's own idea of true")
    for raw, expected in (("true", True), ("TRUE", True), ("1", True), ("yes", True),
                          ("false", False), ("0", False), ("", False), ("maybe", False)):
        values = dict(cfg.DEFAULTS)
        values["write.enabled"] = raw
        check(cfg.writing_enabled(values) is expected,
              "write.enabled=%r is %s" % (raw, expected))

    print()
    print("A busy timeout longer than the operation timeout is reported")
    values = dict(cfg.DEFAULTS)
    values["revit.busyTimeoutSeconds"] = "120"
    found = cfg.problems(values)
    check(len(found) == 1 and "busy" in found[0].lower(),
          "waiting longer to START than to FINISH is flagged, not silently allowed")
    check(cfg.problems(dict(cfg.DEFAULTS)) == [],
          "and the defaults report no problems at all")

    print()
    print("Health: worst state wins")
    check(hp.assess(live=[1]).state == hp.HEALTHY,
          "one Revit answering and nothing amiss -> HEALTHY, all five components green")
    check(hp.assess(live=[1], starting=[2]).state == hp.DEGRADED,
          "one answering and one not yet -> DEGRADED, because something usable is not")
    check(hp.assess(live=[1], discovery_readable=False).state == hp.FAILED,
          "an unreadable discovery folder is FAILED - Heron cannot find any Revit")
    check(hp.assess(live=[1], versions_agree=False).state == hp.DEGRADED,
          "two halves on different versions is DEGRADED")

    print()
    print("Health: no Revit connected is NOT a failure")
    idle = hp.assess()
    check(idle.state == hp.WARNING,
          "it is Heron's designed resting state, so it must not read as broken")
    check("invisible to Heron by design" in idle.describe(),
          "and the reason is said, so the word FAILED keeps its meaning for real faults")

    print()
    print("Health: writing being on is surfaced loudly")
    hot = hp.assess(live=[1], writing=True)
    gate = [c for c in hot.components if c.name == "write gate"][0]
    check(gate.state == hp.WARNING, "an open write gate is a WARNING, never HEALTHY")
    check("never been proven" in gate.detail,
          "and it says why - the path has not met a real Revit")

    print()
    print("Health: a healthy system says so briefly")
    fine = hp.assess(live=[1], writing=False)
    fine.components = [c for c in fine.components if c.state == hp.HEALTHY]
    check("Every component healthy." in fine.describe(),
          "nothing to report reads as one line, not a wall of green")

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("PASSED - settings agree across both halves, and health means something.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
