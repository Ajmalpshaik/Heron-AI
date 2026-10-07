#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The add-in and the brain name the SAME ambient needs. Runs without Revit.

An ambient need is an object the add-in hands a fragment because a fragment
cannot build it - D-112's `editScopeFailures`, D-114's `familyLoadOptions`.
Each name lives in two places: a constant in RevitFragment.cs, which binds it,
and AMBIENT in brain/heron_fragment.py, which tells every Python tool the need
is the host's and not a value to ask the caller for. Spelt differently, the
fragment compiles green - the gate makes every need a parameter - and at the
machine the add-in reports the need as never supplied.

It also checks LOAD_FAMILY keeps Article 7's confirmation: `reload` is a switch
that is OFF unless typed, never a value that defaults to on.

    python tests/test_fragment_ambient.py
"""

import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "brain"))

EXECUTOR = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")
LOAD_FAMILY = os.path.join(ROOT, "brain", "fragments", "load-family")

# The add-in's constants for the ambient needs it binds by name. `doc`, `uidoc`
# and `app` are fields on the globals object and need no constant.
CONSTANTS = ("EditScopeNeed", "FamilyLoadNeed")


def executor_names():
    text = io.open(EXECUTOR, encoding="utf-8").read()
    found = {}
    for constant in CONSTANTS:
        match = re.search(r'const string %s = "([^"]+)";' % constant, text)
        found[constant] = match.group(1) if match else None
    return found, text


def run():
    import heron_fragment
    import yaml

    failures = []
    names, text = executor_names()

    for constant, value in names.items():
        if value is None:
            failures.append("could not read %s out of RevitFragment.cs - the pattern "
                            "found nothing, which is not the same as no constant" % constant)
        elif value not in heron_fragment.AMBIENT:
            failures.append("RevitFragment.cs binds the ambient need '%s' (%s) and "
                            "heron_fragment.AMBIENT does not know it" % (value, constant))

    # The other direction: every AMBIENT name beyond the three globals fields
    # must be one the add-in binds.
    for name in heron_fragment.AMBIENT:
        if name in ("doc", "uidoc", "app"):
            continue
        if name not in names.values():
            failures.append("heron_fragment.AMBIENT lists '%s' and RevitFragment.cs has "
                            "no constant binding it" % name)

    # The type the add-in writes into the prologue is the type AMBIENT declares.
    family = names.get("FamilyLoadNeed")
    if family and family in heron_fragment.AMBIENT:
        declared = heron_fragment.AMBIENT[family]
        if ('lines.Append("%s ")' % declared) not in text:
            failures.append("the add-in's prologue for '%s' is not typed %s, which is "
                            "what AMBIENT declares" % (family, declared))

    manifest = yaml.safe_load(io.open(os.path.join(LOAD_FAMILY, "fragment.yaml"),
                                      encoding="utf-8").read())
    needs = {n["name"]: n for n in manifest["contract"]["needs"]}
    load_need = needs.get("familyLoadOptions")
    if load_need is None:
        failures.append("load-family no longer declares familyLoadOptions")
    elif heron_fragment.need_source(load_need) != "ambient":
        failures.append("load-family's familyLoadOptions is not read as ambient")
    elif load_need.get("type") != heron_fragment.AMBIENT.get("familyLoadOptions"):
        failures.append("load-family declares familyLoadOptions as %r, AMBIENT as %r"
                        % (load_need.get("type"), heron_fragment.AMBIENT.get("familyLoadOptions")))

    # ARTICLE 7. A reload is only ever asked for: an optional bool binds false
    # when absent (HeronBindingNote.AbsentValue), and nothing else does.
    for switch in ("reload", "overwriteValues"):
        need = needs.get(switch)
        if need is None:
            failures.append("load-family has no '%s' need" % switch)
        elif not heron_fragment.need_may_be_absent(need):
            failures.append("load-family's '%s' is not an optional bool, so it is either "
                            "required on every load or could default to something other "
                            "than off" % switch)

    source = io.open(os.path.join(LOAD_FAMILY, "impl", "any", "fragment.cs"),
                     encoding="utf-8").read()
    if "existing != null && !reload" not in source:
        failures.append("load-family no longer refuses a loaded family when reload is off")

    print("Fragment ambient needs - add-in against brain")
    for constant, value in sorted(names.items()):
        print("  %-15s %s" % (constant, value))
    if failures:
        for line in failures:
            print("  FAIL  " + line)
        return 1
    print("  PASS  both sides name the same ambient needs; LOAD_FAMILY reloads only when asked")
    return 0


if __name__ == "__main__":
    try:
        import yaml  # noqa: F401
    except ImportError:
        print("NOT RUN - this needs PyYAML (pip install --user pyyaml).")
        sys.exit(3)
    sys.exit(run())
