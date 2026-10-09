#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/heron-ready.py - a new PC is made ready, or told exactly what it lacks.

    python tests/test_heron_ready.py

A new laptop's first "create a family" took over seven minutes where the
office PC took one or two (2026-10-08): the knowledge store and the search
model were left for the first chat to build, and a missing package left Claude
reading Heron's files instead of calling its tools. The tool exists so a new
PC starts where the old one is. This holds the three things it promises:

  1. A missing REQUIRED package exits 1 and names the command that installs
     it - and builds nothing it cannot build properly.
  2. Its exit code follows its own FIX lines: 0 exactly when none is printed.
  3. Without --check, the knowledge store is BUILT, so the first chat finds
     it built. With --check, nothing is.
  4. --snapshot writes what a PC has as facts - the Heron tools Claude Code
     lets run, how much it remembers, as counts - and NO secret: no memory
     text, no key, no address, no chat, no unrelated permission.

Every run points HERON_KNOWLEDGE at a scratch folder, so the shared store on
the machine running this is never touched (row 5b-233).

WHAT IT CANNOT DO: say anything about a real Windows PC - the Microsoft Store
shortcut, %APPDATA%, the add-in folders and Claude Code's own approvals are
not here to look at.
"""

import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "heron-ready.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def run(knowledge, *args, **env):
    """(exit code, output) of the tool with its store in `knowledge`."""
    environ = dict(os.environ, HERON_KNOWLEDGE=knowledge, PYTHONUTF8="1")
    environ.update(env)
    done = subprocess.run([sys.executable, TOOL] + list(args), cwd=ROOT,
                          env=environ, stdout=subprocess.PIPE,
                          stderr=subprocess.STDOUT, timeout=600)
    return done.returncode, done.stdout.decode("utf-8", "replace")


def main():
    if not os.path.exists(TOOL):
        check(False, "tools/heron-ready.py exists")
        return 1
    work = tempfile.mkdtemp(prefix="heron-ready-")
    try:
        print("1. A missing required package is named, with its command")
        shadow = os.path.join(work, "shadow")
        os.makedirs(shadow)
        with open(os.path.join(shadow, "yaml.py"), "w") as handle:
            handle.write("raise ImportError('PyYAML is not installed here')\n")
        store = os.path.join(work, "no-yaml")
        os.makedirs(store)
        code, out = run(store, PYTHONPATH=shadow)
        check(code == 1, "with no PyYAML it exits 1 (got %d)" % code)
        check("pip install --user -r requirements.txt" in out,
              "and names the command that installs it")
        check(os.listdir(store) == [],
              "and builds no knowledge store without it")

        print()
        print("2. The exit code follows the FIX lines")
        store = os.path.join(work, "check")
        os.makedirs(store)
        code, out = run(store, "--check")
        fixes = [line for line in out.splitlines()
                 if line.strip().startswith("FIX ")]
        check((code == 0) == (not fixes),
              "exit %d with %d FIX line(s)" % (code, len(fixes)))
        check(os.listdir(store) == [],
              "--check builds nothing")

        print()
        print("3. Without --check the store is built before any chat asks")
        store = os.path.join(work, "full")
        os.makedirs(store)
        code, out = run(store)
        check("tool cards ready" in out,
              "it reports the tool cards ready")
        check(any(name.endswith(".db") for name in os.listdir(store)),
              "and the store is on disk in the folder it was given: %s"
              % ", ".join(sorted(os.listdir(store))))

        print()
        print("4. A snapshot says what a PC has - and carries no secret")
        claude = os.path.join(work, "claude")
        memory = os.path.join(claude, "projects", "D--Work-Heron-Ai", "memory")
        os.makedirs(memory)
        with open(os.path.join(memory, "MEMORY.md"), "w") as handle:
            handle.write("SECRET MEMORY TEXT\nsecond line\n")
        with open(os.path.join(claude, "settings.json"), "w") as handle:
            json.dump({"permissions": {"allow": ["mcp__heron__heron_lookup",
                                                 "Bash(git push:*)"]},
                       "env": {"ANTHROPIC_API_KEY": "sk-not-for-sharing"}},
                      handle)
        with open(os.path.join(claude, ".claude.json"), "w") as handle:
            json.dump({"primaryApiKey": "sk-not-for-sharing",
                       "oauthAccount": {"emailAddress": "someone@example.com"},
                       "projects": {"D:/Work/Heron-Ai": {
                           "hasTrustDialogAccepted": True,
                           "enabledMcpjsonServers": ["heron"],
                           "allowedTools": ["mcp__heron__revit_read"],
                           "history": [{"display": "SECRET CHAT TEXT"}]}}},
                      handle)
        out = os.path.join(work, "pc.json")
        code, said = run(os.path.join(work, "full"), "--snapshot", out,
                         CLAUDE_CONFIG_DIR=claude)
        check(code == 0 and os.path.exists(out),
              "it writes the file (exit %d)" % code)
        text = open(out).read() if os.path.exists(out) else ""
        got = json.loads(text) if text else {}
        side = got.get("claude_code") or {}
        check(side.get("user_settings", {}).get("heron_allow")
              == ["mcp__heron__heron_lookup"],
              "it names the Heron tools Claude Code lets run unasked")
        check([p.get("memory_md_lines") for p in side.get("projects", [])] == [2],
              "and how much Claude remembers of Heron, as a line count")
        check([a.get("heron_allowed_tools") for a in side.get("approvals", [])]
              == [["mcp__heron__revit_read"]],
              "and the approvals kept for the Heron folder")
        leaked = [s for s in ("SECRET MEMORY TEXT", "sk-not-for-sharing",
                              "someone@example.com", "SECRET CHAT TEXT",
                              "git push") if s in text]
        check(not leaked, "and nothing else - no memory text, key, address, "
                          "chat or other permission%s"
              % ("" if not leaked else " - LEAKED: " + ", ".join(leaked)))
        check(isinstance(got.get("lookup_seconds"), list),
              "it times three lookups where the store is its own: %s"
              % got.get("lookup_seconds"))
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASSED - a missing package is named with its command, the exit code")
    print("follows what is printed, and the store is built before a chat asks.")
    print("It says nothing about a real Windows PC.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
