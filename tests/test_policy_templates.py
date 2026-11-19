#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Group Policy templates for Heron's three ceilings (D-166, layer 3).

    python tests/test_policy_templates.py

The add-in reads HKLM\\SOFTWARE\\Policies\\Heron - AllowChanges, AllowAdmin,
AllowPublish - as a ceiling an AI running as the modeller cannot lift
(HeronMachinePolicy in revit/Heron.Revit.Addin/HeronApplication.cs). These
templates let an office's IT set it from Group Policy or Intune, as it sets
everything else. WHAT IT HOLDS:

  1. Both files are well-formed XML in the Group Policy namespace.
  2. THE NAMES ARE THE ADD-IN'S: every value the add-in reads has exactly one
     policy, under the add-in's own key, read from the C# rather than typed
     here - so a value renamed in the add-in fails this test, not a PC.
  3. Enabled writes 1 (the modeller may turn it on), Disabled writes 0 (it
     cannot be turned on), each a DWORD, machine-wide.
  4. Every string the template names is in the language file, and the
     language file says the policy never turns a switch on.
"""

from __future__ import print_function

import io
import os
import re
import sys
from xml.dom import minidom

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ADMX = os.path.join(ROOT, "platform", "policy", "Heron.admx")
ADML = os.path.join(ROOT, "platform", "policy", "en-US", "Heron.adml")
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronApplication.cs")
NS = "http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions"
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def addin_names():
    """The key and every value name HeronMachinePolicy reads."""
    text = io.open(ADDIN, encoding="utf-8").read()
    block = text[text.find("class HeronMachinePolicy"):]
    block = block[:block.find("\n    }\n")]
    key = re.search(r'KeyPath\s*=\s*@"([^"]+)"', block)
    return (key.group(1) if key else ""), sorted(set(re.findall(r'Allowed\(key,\s*"(\w+)"\)', block)))


def main():
    print("Heron's Group Policy templates")
    for path in (ADMX, ADML):
        check(os.path.isfile(path), "%s is there" % os.path.relpath(path, ROOT))
    if FAILURES:
        return 1
    admx = minidom.parse(ADMX).documentElement
    adml = minidom.parse(ADML).documentElement
    check(admx.tagName == "policyDefinitions" and admx.getAttribute("xmlns") == NS,
          "the template is a policyDefinitions file in the Group Policy namespace")
    check(adml.tagName == "policyDefinitionResources" and adml.getAttribute("xmlns") == NS,
          "the language file is a policyDefinitionResources file in the same namespace")

    key, names = addin_names()
    check(key.lower() == r"software\policies\heron" and names == ["AllowAdmin", "AllowChanges", "AllowPublish"],
          "read from the add-in: the key %s and the values %s" % (key, names))
    policies = dict((p.getAttribute("valueName"), p) for p in admx.getElementsByTagName("policy"))
    check(sorted(policies) == names, "one policy for each value the add-in reads - found %s" % sorted(policies))
    for name in names:
        policy = policies.get(name)
        if policy is None:
            continue
        check(policy.getAttribute("class") == "Machine", "%s is machine-wide" % name)
        check(policy.getAttribute("key").lower() == key.lower(), "%s writes under the add-in's own key" % name)

        def value(tag):
            found = policy.getElementsByTagName(tag)
            dec = found[0].getElementsByTagName("decimal") if found else []
            return dec[0].getAttribute("value") if dec else None
        check(value("enabledValue") == "1" and value("disabledValue") == "0",
              "%s: Enabled writes the DWORD 1, Disabled writes 0" % name)

    strings = dict((s.getAttribute("id"), "".join(n.data for n in s.childNodes if n.nodeType == n.TEXT_NODE))
                   for s in adml.getElementsByTagName("string"))
    wanted = set(re.findall(r"\$\(string\.(\w+)\)", io.open(ADMX, encoding="utf-8").read()))
    check(wanted and wanted <= set(strings), "every string the template names is in the language file"
          + ("" if wanted <= set(strings) else " - missing %s" % sorted(wanted - set(strings))))
    check(all(strings.get(w, "").strip() for w in wanted), "and none of them is empty")
    helps = " ".join(strings.get(n + "_Help", "") for n in names)
    check(helps.count("never turns") == len(names),
          "each policy's help says it is a ceiling and never turns the switch on")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for what in FAILURES:
            print("  - %s" % what)
        return 1
    print("PASS    Group Policy templates name exactly the ceilings the add-in reads (D-166)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
