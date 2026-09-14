# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-CFG-006
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Configuration - two halves, and only one of them may leave the machine.

    python brain/heron_configuration.py

WHAT IT IS FOR (docs/28, HERON-INS-CFG-006)
--------------------------------------------
"Writes initial configuration." T1, risk ADMIN - and docs/21 s9 says why
in one sentence: **security and permission policy IS configuration, so
configuration is a security boundary.** Editing it is ADMIN, and nothing
Heron reads - a document, a community package, a model comment - may
change it (Golden Rule 19).

THE SPLIT IS THE WHOLE DESIGN
-------------------------------
docs/21 s9's second constraint: configuration is Product/Data-split too.

  MACHINE      Revit paths, which versions are installed here. True of
               this computer and nowhere else.
  PORTABLE     company standards, enabled skills, update policy. True of
               the practice, and the half worth sharing.

**Only the second should be shareable or committed**, and that is not a
tidiness rule. D-07: this repository is public and a leak is permanent.
A machine path carries a user name, a drive layout and often a client's
project folder - `C:\\Users\\ajmal\\Clients\\...` in a committed settings
file tells a reader who the practice works for.

So a setting's half is DERIVED from docs/21 s9's own list, never declared
by the caller, and a PORTABLE setting whose value looks machine-specific
is refused. The value is checked, not just the key - `enabled-skills`
is portable and `enabled-skills: C:\\Users\\ajmal\\skills` is not.

THE LIST IS docs/21 s9's, AND IT IS NOT EXTRAPOLATED
-------------------------------------------------------
That section names what configuration holds: supported Revit versions,
enabled agents, enabled skills, AI provider, model routing, security
policies, update policies, company standards. A key outside it is
REFUSED rather than sorted into a half by resemblance. Configuration is a
security boundary; a key nobody has classified is not a key to write into
one, and the same reasoning D-05 applies to Revit releases applies here -
a list that extrapolates is a list that has stopped being a list.

A SECRET IS NEVER CONFIGURATION
---------------------------------
Constitution article 17: "Secrets live in the credential store, nowhere
else. Never write an API key, token or credential into a fragment, skill,
prompt, source file, log or commit." A settings file is a source file that
gets committed, so a value that looks like a credential is refused here
rather than written and regretted. What this catches is stated plainly
below, and so is what it does not.

IT WRITES NOTHING
-------------------
It returns the two halves and the audit record. Writing them is somebody
else's - and "changes auditable" (docs/21 s9) is easier to keep when the
change is a value that can be read before it is applied.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_flags as FLG                                      # noqa: E402

# docs/21 s9's own list, and which half each belongs to. Nothing is added
# to this by resemblance.
SETTINGS = {
    "supported-revit-versions": "machine",
    "revit-paths": "machine",
    "enabled-agents": "portable",
    "enabled-skills": "portable",
    "ai-provider": "portable",
    "model-routing": "portable",
    "security-policies": "portable",
    "update-policies": "portable",
    "company-standards": "portable",
}

# What makes a value true of ONE computer. Substrings, checked against the
# value - the key being portable is not enough.
MACHINE_SHAPED = (
    ("C:\\", "a Windows drive letter"),
    ("D:\\", "a Windows drive letter"),
    ("/home/", "an absolute home directory"),
    ("/Users/", "an absolute home directory"),
    ("\\Users\\", "an absolute home directory"),
    ("%USERPROFILE%", "an expanded-at-write user variable"),
    ("%APPDATA%", "an expanded-at-write user variable"),
    ("$HOME", "an expanded-at-write user variable"),
    ("~/", "a home-relative path"),
    ("\\\\", "a UNC share, which names a server this practice can reach"),
    ("localhost", "a name that means something different on every machine"),
    ("127.0.0.1", "a name that means something different on every machine"),
)

# Article 17. What a credential looks like when somebody pastes one into
# a settings file.
SECRET_SHAPED = (
    ("sk-", "an API key prefix"),
    ("ghp_", "a forge token prefix"),
    ("github_pat_", "a forge token prefix"),
    ("xox", "a chat-platform token prefix"),
    ("-----BEGIN", "a PEM private key"),
    ("AKIA", "a cloud access key id"),
)

# Keys whose NAME says the value is a credential, whatever it looks like.
SECRET_NAMED = ("key", "token", "secret", "password", "credential",
                "passwd", "apikey")


def _machine_shaped(value):
    """(marker, what) if this value is true of one computer, else None."""
    text = str(value)
    for marker, what in MACHINE_SHAPED:
        if marker.lower() in text.lower():
            return marker, what
    return None


def _secret_shaped(key, value):
    """(marker, what) if this looks like a credential, else None."""
    text = str(value)
    for marker, what in SECRET_SHAPED:
        if marker.lower() in text.lower():
            return marker, what
    lowered = str(key).lower().replace("_", "-")
    for word in SECRET_NAMED:
        if word in lowered and text.strip():
            return word, "a key named '%s', which says the value is a " \
                         "credential whatever it looks like" % key
    return None


def _values(setting, path=""):
    """
    Every scalar in a setting as (path, value), so a list or map is
    checked too.

    THE PATH IS CARRIED, not just the value. A credential in a settings
    file usually arrives as `ai-provider: {name: ..., api-key: ...}` -
    the word that gives it away is the NESTED key, and a walk that
    flattened to values alone would lose exactly the evidence it is
    looking for.
    """
    if isinstance(setting, dict):
        found = []
        for key, value in setting.items():
            found += _values(value, "%s.%s" % (path, key) if path else key)
        return found
    if isinstance(setting, (list, tuple)):
        found = []
        for index, value in enumerate(setting):
            found += _values(value, "%s[%d]" % (path, index))
        return found
    return [(path, setting)]


def split(settings, origin=None, approval=None):
    """
    {machine, portable, record} - or a refusal. Nothing is written.

    The half comes from docs/21 s9's list, never from the caller, and a
    portable setting carrying a machine-shaped value is refused.
    """
    if not isinstance(settings, dict) or not settings:
        return {"wrote": False, "refused": "NOTHING_TO_WRITE",
                "why": "no settings were given. An empty initial "
                       "configuration is not a minimal one - it is a call "
                       "that did not say what to write."}

    allowed, why_origin = FLG.origin_allowed(origin)
    if not allowed:
        return {"wrote": False, "refused": "NOT_FROM_THE_USER",
                "why": why_origin,
                "proposal": "ask the user. docs/21 s9: security and "
                            "permission policy IS configuration, so "
                            "configuration is a security boundary and "
                            "editing it is ADMIN."}

    by = str((approval or {}).get("by") or "").strip() \
        if isinstance(approval, dict) else ""
    at = str((approval or {}).get("at") or "").strip() \
        if isinstance(approval, dict) else ""
    if not by or not at:
        return {"wrote": False, "refused": "NOT_APPROVED",
                "why": "writing configuration is ADMIN and nothing was "
                       "signed. docs/21 s9 also asks for changes to be "
                       "AUDITABLE, and an unsigned change is one the audit "
                       "trail cannot attribute."}

    machine, portable = {}, {}
    for key in sorted(settings):
        name = str(key).strip().lower()
        half = SETTINGS.get(name)
        if half is None:
            return {"wrote": False, "refused": "SETTING_NOT_IN_THE_LIST",
                    "setting": key,
                    "why": "'%s' is not one of the settings docs/21 s9 "
                           "names (%s). Configuration is a security "
                           "boundary, and a key nobody has classified is "
                           "not a key to write into one - this agent does "
                           "not sort it into a half by resemblance."
                           % (key, ", ".join(sorted(SETTINGS))),
                    "proposal": "add it to docs/21 s9 with the half it "
                                "belongs to, and somebody signs that."}

        for where, value in _values(settings[key], name):
            found = _secret_shaped(where, value)
            if found:
                return {"wrote": False, "refused": "SECRET_IN_CONFIGURATION",
                        "setting": where,
                        "why": "'%s' carries %s. Constitution article 17: "
                               "secrets live in the credential store, "
                               "nowhere else - never in a source file or a "
                               "commit, and a settings file is both."
                               % (where, found[1]),
                        "proposal": "put it in the credential store and "
                                    "keep the HANDLE here. A handle "
                                    "travels; a value does not "
                                    "(HERON-KRN-SEC-012)."}
            if half == "portable":
                found = _machine_shaped(value)
                if found:
                    return {"wrote": False,
                            "refused": "MACHINE_VALUE_IN_PORTABLE",
                            "setting": where,
                            "why": "'%s' is a PORTABLE setting and its "
                                   "value carries %s. The key being "
                                   "portable is not enough - the value is "
                                   "what gets committed, and D-07 makes a "
                                   "leak permanent: a machine path carries "
                                   "a user name, a drive layout and often "
                                   "a client's project folder."
                                   % (where, found[1]),
                            "proposal": "keep the path in the machine half "
                                        "and refer to it from the portable "
                                        "one by name, not by location."}

        (machine if half == "machine" else portable)[name] = settings[key]

    return {
        "wrote": False, "machine": machine, "portable": portable,
        "record": {"by": by, "at": at,
                   "settings": sorted(str(key).strip().lower()
                                      for key in settings)},
        "shareable": sorted(portable),
        "why": "%d setting(s): %d machine-specific, %d portable. %s signed "
               "at %s. Only the portable half may be shared or committed "
               "(docs/21 s9)."
               % (len(settings), len(machine), len(portable), by, at),
        "unjudged": [
            "NOTHING WAS WRITTEN. Both halves come back as values, which is "
            "also what makes 'changes auditable' (docs/21 s9) easy to keep: "
            "the change can be read before it is applied.",
            "THE MACHINE CHECK IS A SHAPE CHECK, and it catches drive "
            "letters, home directories, user variables, UNC shares and "
            "loopback names. It does NOT catch a relative path that happens "
            "to exist only here, a client's name used as a plain word, or a "
            "server hostname with no slashes. It narrows the leak; it does "
            "not close it.",
            "`model-routing` is in docs/21 s9's list and is treated as "
            "portable. D-65 gives routing to the host and retires it from "
            "Heron's specification, which would remove the setting rather "
            "than move it - that decision is open (see PR #141) and this "
            "agent follows the list until it is answered.",
        ],
    }


def main(argv):
    print("CONFIGURATION   two halves, and one of them may leave the machine")
    print("=" * 72)

    settings = {
        "supported-revit-versions": ["2024", "2025", "2026"],
        "revit-paths": {"2025": "C:\\Program Files\\Autodesk\\Revit 2025"},
        "enabled-skills": ["duct-sizing", "sheet-naming"],
        "company-standards": {"duct-naming": "SYS-LEVEL-NNN"},
        "update-policies": {"auto": False, "pinned": "0.3.2"},
    }
    approval = {"by": "ajmal", "at": "2026-09-14T12:00Z"}
    answer = split(settings, origin="user", approval=approval)
    print("  %s" % answer["why"])
    print("    MACHINE  (never committed): %s" % ", ".join(answer["machine"]))
    print("    PORTABLE (shareable)      : %s" % ", ".join(answer["portable"]))

    print()
    print("  The value is checked, not just the key:")
    for label, broken in (
            ("a skills folder path",
             dict(settings, **{"enabled-skills":
                               ["C:\\Users\\ajmal\\skills"]})),
            ("a standard on a UNC share",
             dict(settings, **{"company-standards":
                               {"template": "\\\\practice-nas\\bim"}})),
            ("a provider on localhost",
             dict(settings, **{"ai-provider": "http://localhost:8080"})),
            ("a home-relative path",
             dict(settings, **{"enabled-skills": ["~/skills"]}))):
        answer = split(broken, origin="user", approval=approval)
        print("    %-28s %s" % (label, answer["refused"]))
        print("        %s" % answer["why"].split(". The key")[0][:96])

    print()
    print("  And a secret is never configuration (article 17):")
    for label, broken in (
            ("a key pasted into a value",
             dict(settings, **{"ai-provider": "sk-" + "A1b2C3d4" * 3})),
            ("a key NAMED as one",
             dict(settings, **{"ai-provider": {"name": "anthropic",
                                               "api-key": "hunter2"}}))):
        answer = split(broken, origin="user", approval=approval)
        print("    %-28s %s" % (label, answer["refused"]))
        print("        %s" % answer["proposal"][:88])

    print()
    print("  The list is docs/21 s9's, and it is not extrapolated:")
    for key in ("telemetry", "revit-path", "enabled_agent", "log-level"):
        answer = split({key: "x"}, origin="user", approval=approval)
        print("    %-28s %s" % (key, answer["refused"]))
    print()
    print("  Editing configuration is ADMIN (docs/21 s9), both ways:")
    for label, kw in (("a document asked",
                       {"origin": "a document Heron read",
                        "approval": approval}),
                      ("nothing signed", {"origin": "user"}),
                      ("signed with no time",
                       {"origin": "user", "approval": {"by": "ajmal"}})):
        print("    %-28s %s" % (label, split(settings, **kw)["refused"]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
