#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Claude Code plugin folder tools/build-plugin.py makes - built into a
temporary folder and read back.

    python tests/test_build_plugin.py

WHAT THIS PROVES
----------------
    plugin.json names the plugin, and carries heron-products.json's
      manifestVersion, a description, an author, a licence and a homepage
    .mcp.json is valid JSON and starts the server from ${CLAUDE_PLUGIN_ROOT},
      at a file the folder really holds, with the repository's client id
    no file is a link, and no path climbs out of the folder - in the files,
      in plugin.json, in .mcp.json, or in a link inside the agent
    none of revit/, platform/, tests/, tools/, .claude/ and no knowledge store
    every WORKSPACE_KEEP entry it does not leave out on purpose is there -
      one list with heron-project.zip, not a second one
    the modeller's agent names its tools the way Claude Code names a
      plugin's, and none the old way
    README.md (40 words or more), LICENSE and NOTICE are there
    check-products.py passes the build, fails one whose version differs, and
      fails a template that types a version of its own
    the build refuses a template with a version, an agent linking something
      the plugin does not carry, and a folder in its place it did not write
    the longest path, under Claude Code's cache folder for a 20-character
      Windows user name, stays under Windows' 260-character limit - PRINTED

WHAT IT CANNOT PROVE
--------------------
That Claude Code installs the folder, starts the server or lists the tools.
`claude plugin validate` and a real Windows PC answer that - section 8 of
docs/work-notes/plans/claude-code-plugin-2026-11-27.md. A green run here is a
statement about the folder, not about an install.

Exit 0 = every check passed. Exit 1 = at least one failed.
"""

import importlib.util
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
import heron_tools as TOOLS                                  # noqa: E402

TOOL = os.path.join(ROOT, "tools", "build-plugin.py")
# The server's entry file, asked of heron_tools rather than typed (row 5b-2039).
ENTRY = os.path.relpath(TOOLS.server_files()[-1], ROOT).replace(os.sep, "/")
CHECK = os.path.join(ROOT, "tools", "check-products.py")

FAILURES = []

# Claude Code keeps each installed version of a plugin under
# ~/.claude/plugins/cache/<marketplace>/<plugin>/<version>/ (the plugin
# documentation, read 2026-11-28). The marketplace's own name is a later
# part's choice; "heron" is assumed here, and a longer one is a re-measure.
USER_NAME_CHARS = 20
MARKETPLACE = "heron"
# MAX_PATH is 260 INCLUDING the terminating NUL, so 259 is the longest path
# an ordinary Windows call opens.
WINDOWS_LIMIT = 260


def check(ok, what):
    print("  %s  %s" % ("PASS" if ok else "FAIL", what))
    if not ok:
        FAILURES.append(what)


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def read_json(path):
    with io.open(path, encoding="utf-8-sig") as handle:
        return json.load(handle)


def strings(value):
    """Every string inside a JSON value."""
    if isinstance(value, str):
        yield value
    elif isinstance(value, dict):
        for one in value.values():
            for s in strings(one):
                yield s
    elif isinstance(value, list):
        for one in value:
            for s in strings(one):
                yield s


def main():
    BP = load(TOOL, "heron_build_plugin")
    CP = load(CHECK, "heron_check_products")
    BRA = BP._load_release_assets()

    products = read_json(os.path.join(ROOT, "platform", "heron-products.json"))
    want_version = products.get("manifestVersion")
    repo_mcp = read_json(os.path.join(ROOT, ".mcp.json"))

    work = tempfile.mkdtemp(prefix="heron-plugin-test-")
    try:
        print("\n1. It builds, into a folder of its own")
        # A REFUSAL HERE IS ONE FAILURE, NOT A TRACEBACK - every section after
        # it reads the folder, so none can run without one.
        try:
            target, version, count = BP.build(work)
        except (BP.Refused, IOError, OSError) as exc:
            check(False, "the build ran: %s" % exc)
            print("\n%d FAILED - nothing after the build can be read" % len(FAILURES))
            return 1
        check(os.path.isdir(target) and os.path.basename(target) == "heron-plugin",
              "the folder is <out>/heron-plugin")
        check(count > 0, "it holds files (%d)" % count)
        check(not os.path.exists(os.path.join(work, ".heron-plugin.building")),
              "and no half-built folder is left beside it")

        print("\n2. plugin.json")
        manifest_path = os.path.join(target, ".claude-plugin", "plugin.json")
        manifest = read_json(manifest_path) if os.path.isfile(manifest_path) else {}
        check(manifest.get("name") == "heron", "its name is heron")
        check(manifest.get("version") == want_version and version == want_version,
              "its version is heron-products.json's manifestVersion (%s)" % want_version)
        check(isinstance(manifest.get("description"), str)
              and len(manifest["description"].split()) >= 10,
              "it has a description a modeller can read")
        author = manifest.get("author")
        check(isinstance(author, dict) and bool(author.get("name")),
              "it names an author")
        licence = io.open(os.path.join(ROOT, "LICENSE"), encoding="utf-8").read()
        check(manifest.get("license") == "Apache-2.0" and "Apache License" in licence,
              "its licence is the one LICENSE carries (Apache-2.0)")
        check(str(manifest.get("homepage", "")).startswith("https://"),
              "it has a homepage")

        print("\n3. .mcp.json")
        try:
            mcp = read_json(os.path.join(target, ".mcp.json"))
            parsed = True
        except (IOError, OSError, ValueError):
            mcp, parsed = {}, False
        check(parsed, ".mcp.json is valid JSON")
        servers = mcp.get("mcpServers") or {}
        check(list(servers) == ["heron"], "it names one server, heron")
        entry = servers.get("heron") or {}
        args = entry.get("args") or []
        check(entry.get("command") == "python", "it starts python, as Setup's entry does")
        check(len(args) == 1 and args[0] == "${CLAUDE_PLUGIN_ROOT}/" + ENTRY,
              "from ${CLAUDE_PLUGIN_ROOT}, at the server's own file (%s)" % ENTRY)
        if args:
            inside = args[0].replace("${CLAUDE_PLUGIN_ROOT}/", "", 1)
            check(os.path.isfile(os.path.join(target, *inside.split("/"))),
                  "and the folder holds that file")
        check(bool((entry.get("env") or {}).get("HERON_CLIENT_ID"))
              and entry.get("env") == repo_mcp["mcpServers"]["heron"].get("env"),
              "it pins the repository's .mcp.json's client id - one value (Q-100 open)")

        print("\n4. Nothing reaches outside the folder")
        real_root = os.path.realpath(target)
        links, outside = [], []
        for base, dirs, names in os.walk(target):
            for name in dirs + names:
                full = os.path.join(base, name)
                if os.path.islink(full):
                    links.append(full)
                rel = os.path.relpath(full, target)
                if ".." in rel.split(os.sep) or not os.path.realpath(full).startswith(real_root + os.sep):
                    outside.append(rel)
        check(not links, "no file or folder is a link%s" % (" - %s" % links[:3] if links else ""))
        check(not outside, "no path climbs out of the folder%s"
              % (" - %s" % outside[:3] if outside else ""))
        climbs = [s for s in list(strings(manifest)) + list(strings(mcp))
                  if ".." in s.replace("\\", "/").split("/")]
        check(not climbs, "plugin.json and .mcp.json name no path with ..%s"
              % (" - %s" % climbs if climbs else ""))

        print("\n5. What it never carries")
        top = set(os.listdir(target))
        for never in ("revit", "platform", "tests", "tools", ".claude", ".git"):
            check(never not in top, "no %s/ at its top" % never)
        stores = []
        for base, dirs, names in os.walk(target):
            for name in names:
                if name.lower().endswith(BRA.STORE_SHAPES + BRA.DOCUMENT_SHAPES) \
                        or name.endswith(".pyc"):
                    stores.append(os.path.relpath(os.path.join(base, name), target))
            for name in dirs:
                if name in BRA.WORKSPACE_NEVER:
                    stores.append(os.path.relpath(os.path.join(base, name), target))
        check(not stores, "no knowledge store, document, .pyc or build folder (R-87, D-186)%s"
              % (" - %s" % stores[:3] if stores else ""))

        print("\n6. One list with heron-project.zip")
        missing = [keep for keep in BRA.WORKSPACE_KEEP
                   if not BP.left_out(keep) and keep.replace(os.sep, "/") != ".mcp.json"
                   and not os.path.exists(os.path.join(target, keep))]
        check(not missing, "every WORKSPACE_KEEP entry it does not leave out is there%s"
              % (" - missing %s" % missing if missing else ""))
        check(getattr(BRA, "workspace_files", None) is not None
              and "workspace_files()" in io.open(TOOL, encoding="utf-8").read(),
              "and it reads them through build-release-assets' own walk")

        print("\n7. The modeller's agent")
        agent_path = os.path.join(target, "agents", "heron-model-auditor.md")
        agent = io.open(agent_path, encoding="utf-8").read() if os.path.isfile(agent_path) else ""
        check(bool(agent), "agents/heron-model-auditor.md is there")
        found = re.search(r"^tools:\s*(.+)$", agent, re.M)
        tools = [t.strip() for t in found.group(1).split(",")] if found else []
        check(bool(tools) and all(t.startswith("mcp__plugin_heron_heron__") for t in tools),
              "every tool it names is a plugin tool name (%d)" % len(tools))
        check("mcp__heron__" not in agent, "and none is named the old way")
        broken = []
        for link in re.findall(r"\]\(([^)\s]+)\)", agent):
            if re.match(r"^[a-z][a-z0-9+.-]*:", link) or link.startswith("#"):
                continue
            path = os.path.normpath(os.path.join(os.path.dirname(agent_path),
                                                 link.split("#", 1)[0]))
            if not os.path.realpath(path).startswith(real_root + os.sep) \
                    or not os.path.isfile(path):
                broken.append(link)
        check(not broken, "every link in it opens a file inside the folder%s"
              % (" - %s" % broken if broken else ""))

        print("\n8. README, LICENSE, NOTICE")
        readme = os.path.join(target, "README.md")
        words = len(io.open(readme, encoding="utf-8").read().split()) if os.path.isfile(readme) else 0
        check(words >= 40, "README.md has 40 words or more (%d)" % words)
        check(os.path.isfile(os.path.join(target, "LICENSE")), "LICENSE is there")
        check(os.path.isfile(os.path.join(target, "NOTICE")), "NOTICE is there")

        print("\n9. The version gate")
        def run_check(folder):
            proc = subprocess.run([sys.executable, CHECK, "--plugin", folder],
                                  stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            return proc.returncode, proc.stdout.decode("utf-8", "replace")
        code, said = run_check(target)
        check(code == 0, "check-products --plugin passes this build (exit %d)" % code)
        stale = dict(manifest, version="0.0.1")
        with io.open(manifest_path, "w", encoding="utf-8") as handle:
            handle.write(json.dumps(stale))
        code, said = run_check(target)
        check(code == 1 and "'0.0.1'" in said,
              "and FAILS a build whose version differs, naming it (exit %d)" % code)

        template = tempfile.mkdtemp(prefix="heron-plugin-template-")
        try:
            os.makedirs(os.path.join(template, ".claude-plugin"))
            typed = read_json(os.path.join(ROOT, BP.TEMPLATE, ".claude-plugin", "plugin.json"))
            typed["version"] = want_version
            with io.open(os.path.join(template, ".claude-plugin", "plugin.json"), "w",
                         encoding="utf-8") as handle:
                handle.write(json.dumps(typed))
            shutil.copyfile(os.path.join(ROOT, BP.TEMPLATE, ".mcp.json"),
                            os.path.join(template, ".mcp.json"))
            was_cp = CP.PLUGIN_TEMPLATE
            CP.PLUGIN_TEMPLATE = os.path.join(template, ".claude-plugin", "plugin.json")
            problems = []
            CP.check_plugin(products, target, True, problems)
            CP.PLUGIN_TEMPLATE = was_cp
            check(any("carries version" in p for p in problems),
                  "check-products fails a template that types a version of its own")
            was_bp = BP.TEMPLATE
            BP.TEMPLATE = template
            refused = None
            try:
                BP.build(work)
            except BP.Refused as exc:
                refused = str(exc)
            finally:
                BP.TEMPLATE = was_bp
            check(refused is not None and "version of its own" in refused,
                  "and the build refuses one - one number, typed in one place")
        finally:
            shutil.rmtree(template, ignore_errors=True)

        print("\n10. What the build refuses")
        agents = tempfile.mkdtemp(prefix="heron-plugin-agents-")
        try:
            with io.open(os.path.join(agents, "heron-model-auditor.md"), "w",
                         encoding="utf-8") as handle:
                handle.write(u"---\nname: heron-model-auditor\ntools: mcp__heron__revit_health\n"
                             u"---\nSee [a tool](../../tools/check-docs.py).\n")
            was = BP.AGENTS_SRC
            BP.AGENTS_SRC = agents
            refused = None
            try:
                BP.build(work)
            except BP.Refused as exc:
                refused = str(exc)
            finally:
                BP.AGENTS_SRC = was
            check(refused is not None and "does not carry" in refused,
                  "an agent linking something the plugin does not carry stops the build")
        finally:
            shutil.rmtree(agents, ignore_errors=True)

        drift = tempfile.mkdtemp(prefix="heron-plugin-drift-")
        try:
            os.makedirs(os.path.join(drift, ".claude-plugin"))
            shutil.copyfile(os.path.join(ROOT, BP.TEMPLATE, ".claude-plugin", "plugin.json"),
                            os.path.join(drift, ".claude-plugin", "plugin.json"))
            moved = read_json(os.path.join(ROOT, BP.TEMPLATE, ".mcp.json"))
            moved["mcpServers"]["heron"]["env"] = {"HERON_CLIENT_ID": "some-other-id"}
            with io.open(os.path.join(drift, ".mcp.json"), "w", encoding="utf-8") as handle:
                handle.write(json.dumps(moved))
            was = BP.TEMPLATE
            BP.TEMPLATE = drift
            refused = None
            try:
                BP.build(work)
            except BP.Refused as exc:
                refused = str(exc)
            finally:
                BP.TEMPLATE = was
            check(refused is not None and "some-other-id" in refused,
                  "a template whose client id is not the repository's stops the build")
        finally:
            shutil.rmtree(drift, ignore_errors=True)

        other = tempfile.mkdtemp(prefix="heron-plugin-other-")
        try:
            os.makedirs(os.path.join(other, "heron-plugin"))
            keep = os.path.join(other, "heron-plugin", "mine.txt")
            with io.open(keep, "w", encoding="utf-8") as handle:
                handle.write(u"somebody's file")
            refused = None
            try:
                BP.build(other)
            except BP.Refused as exc:
                refused = str(exc)
            check(refused is not None and os.path.isfile(keep),
                  "a folder in its place that it did not write is refused, and left alone")
        finally:
            shutil.rmtree(other, ignore_errors=True)

        again, _v, _n = BP.build(work)
        check(read_json(os.path.join(again, ".claude-plugin", "plugin.json")).get("version")
              == want_version, "a build of its own is replaced by the next build")

        print("\n11. The longest path on a modeller's PC")
        length, rel = BP.longest_path(again)
        prefix = "C:\\Users\\%s\\.claude\\plugins\\cache\\%s\\heron\\%s\\" % (
            "x" * USER_NAME_CHARS, MARKETPLACE, want_version)
        total = len(prefix) + length
        print("      longest relative path: %d characters - %s" % (length, rel))
        print("      cache prefix:          %d characters - %s" % (len(prefix), prefix))
        print("      together:              %d of Windows' %d (the longest usable is %d)"
              % (total, WINDOWS_LIMIT, WINDOWS_LIMIT - 1))
        check(total < WINDOWS_LIMIT,
              "under Windows' %d-character limit, for a %d-character user name"
              % (WINDOWS_LIMIT, USER_NAME_CHARS))
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    if FAILURES:
        print("%d FAILED" % len(FAILURES))
        return 1
    print("ALL PASS. A statement about the folder, not about an install: whether")
    print("Claude Code installs it on a modeller's PC is section 8 of the note.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
