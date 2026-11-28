#!/usr/bin/env python3
# Heron-Agent:  HERON-INS-PKG-012
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Build Heron's chat side as a Claude Code plugin folder. It publishes nothing.

    python tools/build-plugin.py                  writes dist/heron-plugin/
    python tools/build-plugin.py --out <folder>   writes <folder>/heron-plugin/
    python tools/build-plugin.py --list           says what would go in, writes nothing

WHY THIS EXISTS
---------------
The owner said yes on 2026-11-28 to Heron's chat side - the MCP server, the
brain, its skills - arriving as a versioned Claude Code plugin (D-210, and
docs/work-notes/plans/claude-code-plugin-2026-11-27.md). Claude Code copies
each version of a plugin into a folder of its own, and every path a plugin
names must stay inside that folder. So the plugin is BUILT as one folder,
never pointed at this repository. This is part 1 of that build: the folder.
No marketplace file, no release workflow and no Setup change are made here -
those are later parts, each its own pull request.

WHAT GOES IN, AND WHERE EACH PART COMES FROM
-------------------------------------------
    the files heron-project.zip ships   tools/build-release-assets.py's
                                        workspace_files() - its WORKSPACE_KEEP
                                        list, WORKSPACE_NEVER and the R-87 and
                                        D-186 refusals. ONE list and one walk,
                                        so the download and the plugin cannot
                                        drift apart. Entries under a folder in
                                        LEFT_OUT are not copied (below).
    .claude-plugin/plugin.json          platform/Heron.Plugin's, with `version`
                                        written here from heron-products.json's
                                        `manifestVersion` - one number, typed in
                                        one place. check-products.py fails a
                                        built folder whose version differs.
    .mcp.json                           platform/Heron.Plugin's, which starts
                                        the server from ${CLAUDE_PLUGIN_ROOT}.
                                        It takes the place of the repository's
                                        own .mcp.json, which starts it from a
                                        relative path. It pins the same client
                                        id as the repository's .mcp.json, and
                                        the build refuses one that differs -
                                        today's value, until Q-100 (#1488) is
                                        answered.
    agents/<name>.md                    .claude/agents/<name>.md for each agent
                                        in MODELLER_AGENTS, with every Heron
                                        tool name rewritten to the name Claude
                                        Code gives a plugin's tool, and every
                                        relative link re-aimed from its new
                                        folder. A link to anything the plugin
                                        does not carry stops the build.

LEFT OUT, AND WHY
-----------------
`revit/`, `platform/`, `tests/`, `tools/` and `.claude/` - the add-in comes
from Setup, and the rest is for developing Heron (section 3 of the note).
WORKSPACE_KEEP names five files under `tools/` and two under `platform/` for
the download's installers; those stay in heron-project.zip and are not
copied here. No knowledge store is ever copied (R-87, unchanged).

WHAT THIS CANNOT TELL YOU
-------------------------
Whether Claude Code installs it, starts the server or shows the tools on a
modeller's PC. `claude plugin validate dist/heron-plugin` checks the manifest,
the paths and each MCP entry; the PC checks are section 8 of the note. A
built folder is not an install and must never be reported as one.

Exit 0 = built (or listed). Exit 1 = refused, and it says why; nothing is left
in the place of the plugin folder when it refuses.
"""

import argparse
import importlib.util
import io
import json
import os
import re
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

TEMPLATE = os.path.join("platform", "Heron.Plugin")
MANIFEST = os.path.join("platform", "heron-products.json")
REPO_MCP = ".mcp.json"
AGENTS_SRC = os.path.join(".claude", "agents")

FOLDER = "heron-plugin"
PLUGIN_JSON = os.path.join(".claude-plugin", "plugin.json")
PLUGIN_MCP = ".mcp.json"

# The plugin root as Claude Code names it inside .mcp.json. Every path the
# server's entry names starts with it, so nothing reaches outside the folder.
PLUGIN_ROOT_VAR = "${CLAUDE_PLUGIN_ROOT}"

# Top-level folders the plugin never carries - section 3 of the note.
LEFT_OUT = ("revit", "platform", "tests", "tools", ".claude")

# The agents a MODELLER uses. The rest of .claude/agents is for developing
# Heron - writing fragments, recording defects - and stays out.
MODELLER_AGENTS = ("heron-model-auditor",)

# How Heron's tools are named when the server is registered in .mcp.json, and
# the prefix Claude Code gives a plugin's server instead:
# mcp__plugin_<plugin>_<server>__ (code.claude.com/docs/en/mcp, read 2026-11-28).
REPO_TOOL_PREFIX = "mcp__heron__"

VERSION_SHAPE = re.compile(r"^\d+\.\d+\.\d+([-+][0-9A-Za-z.-]+)?\Z")
MD_LINK = re.compile(r"\]\(([^)\s]+)\)")


class Refused(Exception):
    """The build stops. Its text says why, and what to do."""


def _load_release_assets():
    """tools/build-release-assets.py as a module - its name has a hyphen."""
    path = os.path.join(ROOT, "tools", "build-release-assets.py")
    spec = importlib.util.spec_from_file_location("heron_build_release_assets", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _read_json(rel):
    full = os.path.join(ROOT, rel)
    try:
        with io.open(full, encoding="utf-8-sig") as handle:
            return json.load(handle)
    except (IOError, OSError) as exc:
        raise Refused("cannot read %s: %s" % (rel, exc))
    except ValueError as exc:
        raise Refused("%s is not valid JSON: %s" % (rel, exc))


def manifest_version():
    """heron-products.json's `manifestVersion` - the plugin's one version."""
    version = _read_json(MANIFEST).get("manifestVersion")
    if not isinstance(version, str) or not VERSION_SHAPE.match(version):
        raise Refused("%s has no manifestVersion of the shape 1.2.3 (found %r), "
                      "so the plugin has no version to carry" % (MANIFEST, version))
    return version


def plugin_manifest(version):
    """The template's plugin.json with `version` written in, second."""
    template = _read_json(os.path.join(TEMPLATE, PLUGIN_JSON))
    if "version" in template:
        raise Refused(
            "%s carries a version of its own (%r). The build writes it from %s's "
            "manifestVersion, so a typed copy is only a number that goes stale - "
            "take it out." % (os.path.join(TEMPLATE, PLUGIN_JSON),
                              template["version"], MANIFEST))
    name = template.get("name")
    if not isinstance(name, str) or not re.match(r"^[a-z0-9][a-z0-9-]*\Z", name):
        raise Refused("%s names no plugin in kebab-case (found %r)"
                      % (os.path.join(TEMPLATE, PLUGIN_JSON), name))
    out = {"name": name, "version": version}
    for key, value in template.items():
        if key != "name":
            out[key] = value
    return out


def mcp_entry():
    """The template's .mcp.json, its `env` held to the repository's own."""
    template = _read_json(os.path.join(TEMPLATE, PLUGIN_MCP))
    servers = template.get("mcpServers")
    if not isinstance(servers, dict) or len(servers) != 1:
        raise Refused("%s must name exactly one server"
                      % os.path.join(TEMPLATE, PLUGIN_MCP))
    (server, entry), = servers.items()
    for arg in entry.get("args") or []:
        if not isinstance(arg, str) or not arg.startswith(PLUGIN_ROOT_VAR + "/") \
                or ".." in arg.replace("\\", "/").split("/"):
            raise Refused("%s: the argument %r does not start at %s, or climbs out "
                          "of it - every path a plugin names stays inside its folder"
                          % (os.path.join(TEMPLATE, PLUGIN_MCP), arg, PLUGIN_ROOT_VAR))
    # ONE CLIENT ID PER PERSON, PINNED BY EVERY DOOR THAT STARTS THE SERVER
    # (D-22, row 5b-51) - tests/test_config_and_health.py finds every such
    # file and holds them to the same value, so the template pins it too. A
    # template whose env has drifted from the repository's is refused here
    # rather than shipped: until Q-100 (#1488) is answered both carry today's.
    repo = _read_json(REPO_MCP).get("mcpServers", {}).get(server)
    if not isinstance(repo, dict):
        raise Refused("%s has no '%s' server to compare the env with" % (REPO_MCP, server))
    if entry.get("env") != repo.get("env"):
        raise Refused("%s's env %r is not %s's %r - the client id is one value, "
                      "set the same in every file that starts the server"
                      % (os.path.join(TEMPLATE, PLUGIN_MCP), entry.get("env"),
                         REPO_MCP, repo.get("env")))
    return server, {"mcpServers": {server: dict(entry)}}


def tool_prefix(plugin, server):
    """The prefix Claude Code gives a plugin's MCP tools."""
    return "mcp__plugin_%s_%s__" % (plugin, server)


def left_out(rel):
    """True when `rel` (a path inside the workspace) is under LEFT_OUT."""
    first = rel.replace("\\", "/").split("/", 1)[0]
    return first in LEFT_OUT


def _inside(rel):
    """A relative path that stays inside its root: not absolute, no `..`."""
    parts = rel.replace("\\", "/").split("/")
    return not os.path.isabs(rel) and ".." not in parts and rel not in ("", ".")


def _agent_text(name, prefix, shipped):
    """One agent's file as the plugin carries it."""
    src = os.path.join(ROOT, AGENTS_SRC, name + ".md")
    try:
        text = io.open(src, encoding="utf-8").read()
    except (IOError, OSError) as exc:
        raise Refused("cannot read the agent %s: %s" % (src, exc))

    text = text.replace(REPO_TOOL_PREFIX, prefix)

    here = os.path.join(ROOT, AGENTS_SRC)

    def aim(match):
        target = match.group(1)
        if re.match(r"^[a-z][a-z0-9+.-]*:", target) or target.startswith("#"):
            return match.group(0)
        path, _, anchor = target.partition("#")
        repo_rel = os.path.relpath(os.path.normpath(os.path.join(here, path)), ROOT)
        repo_rel = repo_rel.replace(os.sep, "/")
        if repo_rel not in shipped:
            raise Refused(
                "the agent %s links %s, which the plugin does not carry. Link "
                "something that ships with it, or take the link out."
                % (name, target))
        new = os.path.relpath(repo_rel, "agents").replace(os.sep, "/")
        return "](%s%s)" % (new, ("#" + anchor) if anchor else "")

    return MD_LINK.sub(aim, text)


def plan():
    """
    (files, dropped): every (source, path in the plugin) to copy, and every
    WORKSPACE_KEEP path left out with why. Raises Refused, or the IOError
    workspace_files() raises for a store or a document (R-87, D-186).
    """
    assets = _load_release_assets()
    template_files = set([PLUGIN_MCP.replace(os.sep, "/")])
    files, dropped = [], []
    for source, rel in assets.workspace_files():
        norm = rel.replace(os.sep, "/")
        if left_out(norm):
            dropped.append((norm, "under %s/, which the plugin never carries"
                            % norm.split("/", 1)[0]))
            continue
        if norm in template_files:
            dropped.append((norm, "replaced by %s's own"
                            % TEMPLATE.replace(os.sep, "/")))
            continue
        if not _inside(norm):
            raise Refused("%s is not a path inside the plugin folder" % rel)
        if os.path.islink(source):
            raise Refused("%s is a symbolic link; the plugin carries files only" % rel)
        files.append((source, norm))
    return files, dropped


def _previous_build(path):
    """True when `path` is a folder this tool wrote - safe to replace."""
    marker = os.path.join(path, PLUGIN_JSON)
    if not os.path.isfile(marker):
        return False
    try:
        with io.open(marker, encoding="utf-8") as handle:
            return json.load(handle).get("name") == _read_json(
                os.path.join(TEMPLATE, PLUGIN_JSON)).get("name")
    except (IOError, OSError, ValueError):
        return False


def _write_json(path, doc):
    with io.open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(doc, indent=2, ensure_ascii=False) + "\n")


def build(out_dir):
    """Build <out_dir>/heron-plugin. Returns (path, version, file count)."""
    version = manifest_version()
    manifest = plugin_manifest(version)
    server, mcp = mcp_entry()
    prefix = tool_prefix(manifest["name"], server)
    files, _dropped = plan()
    shipped = set(rel for _source, rel in files)
    agents = [(name, _agent_text(name, prefix, shipped)) for name in MODELLER_AGENTS]
    for name, text in agents:
        if REPO_TOOL_PREFIX in text:
            raise Refused("the agent %s still names a tool %s... after the rewrite"
                          % (name, REPO_TOOL_PREFIX))

    target = os.path.join(out_dir, FOLDER)
    if os.path.exists(target) and not _previous_build(target):
        raise Refused("%s exists and is not a plugin folder this tool built - move "
                      "it, and build again. Nothing was written." % target)

    stage = os.path.join(out_dir, "." + FOLDER + ".building")
    if os.path.exists(stage):
        shutil.rmtree(stage)
    os.makedirs(stage)
    try:
        for source, rel in files:
            dest = os.path.join(stage, *rel.split("/"))
            parent = os.path.dirname(dest)
            if not os.path.isdir(parent):
                os.makedirs(parent)
            shutil.copyfile(source, dest)

        os.makedirs(os.path.join(stage, ".claude-plugin"))
        _write_json(os.path.join(stage, PLUGIN_JSON), manifest)
        _write_json(os.path.join(stage, PLUGIN_MCP), mcp)

        os.makedirs(os.path.join(stage, "agents"))
        for name, text in agents:
            with io.open(os.path.join(stage, "agents", name + ".md"), "w",
                         encoding="utf-8", newline="\n") as handle:
                handle.write(text)

        if os.path.exists(target):
            shutil.rmtree(target)
        os.rename(stage, target)
    except BaseException:
        shutil.rmtree(stage, ignore_errors=True)
        raise

    count = sum(len(names) for _base, _dirs, names in os.walk(target))
    return target, version, count


def longest_path(root):
    """(length, relative path) of the longest file path under `root`, spelt
    with backslashes as Windows spells it."""
    best = (0, "")
    for base, _dirs, names in os.walk(root):
        for name in names:
            rel = os.path.relpath(os.path.join(base, name), root).replace(os.sep, "\\")
            if len(rel) > best[0]:
                best = (len(rel), rel)
    return best


def main():
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")

    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", default="dist",
                        help="the folder heron-plugin/ is written into (default: dist)")
    parser.add_argument("--list", action="store_true",
                        help="say what would go in and what is left out; write nothing")
    args = parser.parse_args()

    try:
        if args.list:
            version = manifest_version()
            files, dropped = plan()
            print("Heron plugin %s - what would go in (nothing written)\n" % version)
            print("  %d files from WORKSPACE_KEEP (tools/build-release-assets.py)" % len(files))
            print("  %s, with version %s" % (PLUGIN_JSON.replace(os.sep, "/"), version))
            print("  %s, from %s" % (PLUGIN_MCP, TEMPLATE.replace(os.sep, "/")))
            for name in MODELLER_AGENTS:
                print("  agents/%s.md, from %s" % (name, AGENTS_SRC.replace(os.sep, "/")))
            print("\nLeft out:")
            for rel, why in dropped:
                print("  %-36s %s" % (rel, why))
            return 0

        out = args.out if os.path.isabs(args.out) else os.path.join(ROOT, args.out)
        if not os.path.isdir(out):
            os.makedirs(out)
        target, version, count = build(out)
    except (Refused, IOError, OSError) as exc:
        print("REFUSED: %s" % exc)
        return 1

    length, rel = longest_path(target)
    print("Built the Heron plugin %s in %s - %d files." % (version, target, count))
    print("Longest path inside it: %d characters (%s)." % (length, rel))
    print("Check it with:  claude plugin validate %s" % target)
    print("\nNOTHING WAS PUBLISHED. This is a folder on this machine; installing it on")
    print("a modeller's PC is section 8 of docs/work-notes/plans/claude-code-plugin-2026-11-27.md.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
