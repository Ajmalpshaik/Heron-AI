# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-MCP-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
MCP registration - the triangle agrees, or nothing is registered.

    python mcp/server/heron_register.py

WHAT IT IS FOR (docs/28, HERON-INS-MCP-004)
--------------------------------------------
"Registers the MCP server with the host." T1, risk ADMIN.

REGISTERING A SERVER THAT CANNOT WORK IS WORSE THAN NOT REGISTERING ONE
-------------------------------------------------------------------------
docs/04 s5 draws the version compatibility triangle: three independently
versioned pieces must agree - the Heron MCP server, the Heron Revit
add-in, and Revit itself - with the MCP protocol version beside them.

And it names the failure mode rather than leaving it to be discovered:

  "A user will end up with a new MCP server and an old add-in - this is
  the most common real-world failure mode, because the add-in requires a
  Revit restart to update and the server does not."

HERON-OPS-UPD-010 is the other half of that sentence: rule 3 there says an
add-in update has not taken effect until Revit restarts, so the add-in is
always the piece that lags. This agent is where that lag is caught, at the
moment somebody would otherwise wire a mismatched pair into a host.

NEVER SILENTLY DEGRADE
------------------------
docs/04 s5, in its own words: "Never silently degrade. A half-updated pair
that mostly works is worse than a clean refusal." So there is no partial
registration here - no "registered, read-only", no "registered with
warnings". The server registers for the Revit releases whose add-ins
agree, and every release whose add-in does not is REFUSED by name, with
the message that section specifies: which release, which version is
there, which is needed, and that Revit must restart.

THE COMPATIBLE RANGE IS DECLARED, NOT INFERRED
------------------------------------------------
"The server refuses to operate outside a DECLARED compatible range."
Declared - so a server that names no range is refused rather than assumed
to work with whatever it meets. Inferring the range from the version it
happens to have is how a range silently widens by one release each time
somebody ships.

THE ADD-IN ADVERTISES ITS VERSION ON CONNECT
----------------------------------------------
Also docs/04 s5, and it decides the shape of this agent's input: the
add-in's version is something that ARRIVES, not something configuration
states. A release nothing advertised is not a release with a working
add-in - it is one nobody has heard from, and those are different only in
the direction that matters.

IT REGISTERS NOTHING
----------------------
It returns the entry a host would be given. Writing it into a host's
configuration is somebody else's, and an agent that edited a host's
config file would be editing configuration (docs/21 s9) from inside the
thing being configured.
"""

import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(HERE)),
                                "brain"))

ROOT = os.path.dirname(os.path.dirname(HERE))

import heron_flags as FLG                                      # noqa: E402

# D-05's table, as heron_fragment.py and heron_packages.py carry it. An
# unlisted release is an error, never a guess.
REVIT_VERSIONS = ("2020", "2021", "2022", "2023", "2024", "2025", "2026",
                  "2027")


def _parts(version):
    """(major, minor) as integers, or None for anything else."""
    pieces = str(version or "").strip().split(".")
    if len(pieces) < 2:
        return None
    try:
        return int(pieces[0]), int(pieces[1])
    except ValueError:
        return None


def _agrees(addin, wants):
    """
    (verdict, why) for one add-in against the server's declared range.

    "0.4.x" means major and minor must match. Older and newer are
    reported SEPARATELY because the remedy differs: an older add-in
    needs a Revit restart, a newer one needs the server updated, and
    telling somebody to restart Revit when the server is behind wastes
    the one action they were willing to take.
    """
    found, needed = _parts(addin), _parts(wants)
    if needed is None:
        return "NO_COMPATIBLE_RANGE", (
            "the server declares %r as the add-in version it needs, which "
            "is not major.minor. docs/04 s5 asks for a DECLARED compatible "
            "range, and a range nobody can read is not declared." % wants)
    if found is None:
        return "NOTHING_TO_TALK_TO", (
            "nothing advertised an add-in version (%r). docs/04 s5: the "
            "add-in advertises its version on connect, so a release nothing "
            "advertised is not one with a working add-in - it is one nobody "
            "has heard from." % addin)
    if found == needed:
        return None, "version %s, matching the declared %s" % (addin, wants)
    # THE WORDING IS docs/04 s5's OWN: "Heron add-in in Revit 2024 is
    # version 0.3.1, this Heron needs 0.4.x. Restart Revit to finish
    # updating." The caller supplies the first half, so this supplies the
    # rest of that exact sentence.
    if found < needed:
        return "ADDIN_IS_OLDER", (
            "version %s, this Heron needs %s.x. Restart Revit to finish "
            "updating - the add-in requires a restart to update and the "
            "server does not, which is why this pair is the most common "
            "real-world failure (docs/04 s5)."
            % (addin, "%d.%d" % needed))
    return "ADDIN_IS_NEWER", (
        "version %s, this Heron is %s.x - the SERVER is behind, not Revit. "
        "Restarting Revit would change nothing; the server is what needs "
        "updating." % (addin, "%d.%d" % needed))


def register(server, addins=None, origin=None, approval=None):
    """
    {register, releases, refused_releases, entry} - or a refusal.

    Nothing is written. The entry a host would be given comes back as a
    value, because an agent that edited a host's configuration file would
    be editing configuration from inside the thing being configured.
    """
    if not isinstance(server, dict) or not str(
            server.get("version") or "").strip():
        return {"register": False, "refused": "NOTHING_TO_REGISTER",
                "why": "no server described itself. A registration entry "
                       "names a version, a command and the add-in range it "
                       "needs, and none of those can be guessed."}

    wants = str(server.get("needs_addin") or "").strip()
    if not wants:
        return {"register": False, "refused": "NO_COMPATIBLE_RANGE",
                "why": "the server does not declare which add-in versions "
                       "it works with. docs/04 s5: the server refuses to "
                       "operate outside a DECLARED compatible range - "
                       "inferring it from the version the server happens to "
                       "have is how a range silently widens by one release "
                       "each time somebody ships.",
                "proposal": "declare `needs_addin` as major.minor, and "
                            "somebody signs the day it changes."}

    allowed, why_origin = FLG.origin_allowed(origin, "registering with a host")
    if not allowed:
        return {"register": False, "refused": "NOT_FROM_THE_USER",
                "why": why_origin,
                "proposal": "ask the user. Registering a server with a host "
                            "is what makes Heron's tools callable at all."}

    by = str((approval or {}).get("by") or "").strip() \
        if isinstance(approval, dict) else ""
    if not by:
        return {"register": False, "refused": "NOT_APPROVED",
                "why": "registering with a host is ADMIN and nobody signed. "
                       "It is the act that makes Heron's tools reachable, "
                       "which is a permission decision before it is a "
                       "configuration one."}

    if not isinstance(addins, dict) or not addins:
        return {"register": False, "refused": "NOTHING_TO_TALK_TO",
                "why": "no add-in advertised a version for any release. "
                       "There is nothing for this server to be compatible "
                       "WITH, and registering it anyway would put a working "
                       "entry in a host's configuration for a Heron that "
                       "cannot reach Revit."}

    good, refused = [], []
    for release in sorted(addins):
        if str(release).strip() not in REVIT_VERSIONS:
            return {"register": False, "refused": "REVIT_NOT_SUPPORTED",
                    "release": release,
                    "why": "Revit %s is not in the supported table (%s-%s). "
                           "D-05: an unlisted release is an error, never a "
                           "guess, and the table is never extrapolated "
                           "forward." % (release, REVIT_VERSIONS[0],
                                         REVIT_VERSIONS[-1])}
        verdict, why = _agrees(addins[release], wants)
        if verdict == "NO_COMPATIBLE_RANGE":
            return {"register": False, "refused": verdict, "why": why}
        if verdict:
            refused.append({"release": release, "refused": verdict,
                            "why": "Heron add-in in Revit %s is %s"
                                   % (release, why)})
        else:
            good.append({"release": release, "addin": addins[release],
                         "why": why})

    if not good:
        return {"register": False, "refused": "NOTHING_TO_TALK_TO",
                "refused_releases": refused,
                "why": "not one of the %d release(s) has an add-in this "
                       "server can work with. docs/04 s5: never silently "
                       "degrade - a half-updated pair that mostly works is "
                       "worse than a clean refusal, and a pair that does "
                       "not work at all is not a registration."
                       % len(refused)}

    return {
        "register": True,
        "entry": {"name": str(server.get("name") or "heron"),
                  "command": server.get("command"),
                  "version": str(server["version"]).strip()},
        "releases": good, "refused_releases": refused,
        "why": "%s %s registers for Revit %s. %s"
               % (server.get("name") or "heron", server["version"],
                  ", ".join(entry["release"] for entry in good),
                  "Every release has an add-in that agrees."
                  if not refused else
                  "%d release(s) refused by name: %s."
                  % (len(refused), ", ".join("%s (%s)" % (entry["release"],
                                                          entry["refused"])
                                             for entry in refused))),
        "unjudged": [
            "NOTHING WAS REGISTERED. The entry comes back as a value; "
            "writing it into a host's configuration is somebody else's, "
            "and an agent that edited a host's config file would be "
            "editing configuration (docs/21 s9) from inside the thing "
            "being configured.",
            "THERE IS NO PARTIAL REGISTRATION. A refused release is refused, "
            "not registered read-only or registered with a warning - docs/04 "
            "s5: never silently degrade, a half-updated pair that mostly "
            "works is worse than a clean refusal.",
            "the add-in versions were ADVERTISED, not read. This agent did "
            "not connect to anything; it compared what it was handed "
            "against the range the server declared.",
            "the MCP protocol version sits on the same triangle (docs/04 "
            "s5) and is not checked here - nothing in this repository "
            "declares which protocol versions Heron speaks, so a check "
            "would be comparing against a number this agent invented.",
        ],
    }


def main(argv):
    print("MCP REGISTRATION   the triangle agrees, or nothing is registered")
    print("=" * 72)

    server = {"name": "heron", "version": "0.4.0", "needs_addin": "0.4",
              "command": "python mcp/server/heron_server.py"}
    signed = {"by": "ajmal", "at": "2026-09-14T12:00Z"}

    answer = register(server, addins={"2024": "0.4.0", "2025": "0.4.1"},
                      origin="user", approval=signed)
    print("  %s" % answer["why"])

    print()
    print("  The most common real-world failure, caught before it is wired:")
    answer = register(server, addins={"2024": "0.3.1", "2025": "0.4.0"},
                      origin="user", approval=signed)
    print("  %s" % answer["why"])
    for entry in answer["refused_releases"]:
        print("    %s" % entry["why"][:118])

    print()
    print("  And the other direction, which has a different remedy:")
    answer = register(server, addins={"2025": "0.5.0"}, origin="user",
                      approval=signed)
    print("    %s - %s" % (answer["refused"],
                           answer["refused_releases"][0]["why"][:96]))

    print()
    print("  Never silently degrade - no release working is not a partial:")
    answer = register(server, addins={"2024": "0.3.1"}, origin="user",
                      approval=signed)
    print("    %s" % answer["refused"])
    print("      %s" % answer["why"][:104])

    print()
    print("  The range is DECLARED, not inferred:")
    for label, what in (("no range at all", dict(server, needs_addin="")),
                        ("a range that is a word",
                         dict(server, needs_addin="latest")),
                        ("a bare major", dict(server, needs_addin="0"))):
        answer = register(what, addins={"2025": "0.4.0"}, origin="user",
                          approval=signed)
        print("    %-24s %s" % (label, answer["refused"]))

    print()
    print("  And it is ADMIN, from the user:")
    for label, kw in (("a document asked",
                       {"origin": "a document Heron read",
                        "approval": signed}),
                      ("nothing signed", {"origin": "user"}),
                      ("nothing advertised", {"origin": "user",
                                              "approval": signed,
                                              "addins": {}}),
                      ("a release nobody supports",
                       {"origin": "user", "approval": signed,
                        "addins": {"2019": "0.4.0"}})):
        kw.setdefault("addins", {"2025": "0.4.0"})
        print("    %-24s %s" % (label, register(server, **kw)["refused"]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
