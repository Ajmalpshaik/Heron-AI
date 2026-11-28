#!/usr/bin/env python3
# Heron-Agent:  HERON-INS-PKG-012
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.3.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Put a built Heron plugin on the plugin-release branch: one commit per release,
never a force-push. Run by .github/workflows/plugin-release.yml.

    python tools/publish-plugin.py --check
        hold .claude-plugin/marketplace.json to the plugin template. Writes nothing.

    python tools/publish-plugin.py --plugin <built folder> --stage <new folder>
        lay out in <new folder> the branch as it would be (plugin-release/) and
        a marketplace beside it that points there, for `claude plugin validate`.
        No git.

    python tools/publish-plugin.py --plugin <built folder> [--tag v1.2.3] [--push]
        make the release's commit for plugin-release. With --push it is sent to
        the remote, never forced; without it, nothing leaves this machine.

WHY THIS EXISTS
---------------
Part 8 of the plugin plan (D-210, D-212). A modeller's Claude Code reads the
marketplace from .claude-plugin/marketplace.json on `main`, and that file sends
it to the plugin-release branch of this same repository for the plugin itself.
That branch holds the built plugin folder at its root and nothing else, and it
moves only when a Heron release is published - so a modeller's Heron changes
on a release, never on a push to `main`.

THE RULES, AND WHY EACH IS HERE RATHER THAN IN THE WORKFLOW
-----------------------------------------------------------
A rule written in YAML is a rule no test can reach (release.yml says the same),
so the workflow only calls this, and tests/test_plugin_marketplace.py runs it
against a throwaway repository.

    the marketplace      named `heron`, one plugin named as plugin.json names
                         it, fetched from this repository at ref
                         `plugin-release`, and no version of its own - the
                         plugin's one number is plugin.json's (D-210 item 3).
    the branch           an ORPHAN: its first commit has no parent, so it
                         shares no history with `main`. Every later commit has
                         the branch's newest commit as its only parent - one
                         commit per release, in order.
    never forced         the push is `<commit>:refs/heads/plugin-release`, with
                         no `+` and no --force. If the branch moved after it was
                         read, the remote refuses, and so does this.
    written only here    the branch's newest commit must be one of these
                         ("Heron plugin ..."); anything else is a hand-made
                         commit, and nothing is written on top of it.
    one number           the release's tag is `v` + the plugin's version, and
                         the version is new: Claude Code moves a modeller only
                         when the number changes, so the same number with other
                         files would reach nobody, and a lower one would move
                         them back.

The commit is made with git's own plumbing - a temporary index, `write-tree`,
`commit-tree` - so no branch is checked out, no working folder is switched and
no local branch is created in the checkout it runs in.

WHAT THIS CANNOT TELL YOU
-------------------------
Whether Claude Code installs what the branch holds on a modeller's PC, or how
much it downloads. That is section 8 of
docs/work-notes/plans/claude-code-plugin-2026-11-27.md, on a real Windows PC.

Exit 0 = done, or nothing to do. Exit 1 = refused, and it says why; when it
refuses, nothing was sent.
"""

import argparse
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

MARKETPLACE = os.path.join(".claude-plugin", "marketplace.json")
TEMPLATE_JSON = os.path.join("platform", "Heron.Plugin", ".claude-plugin", "plugin.json")
PLUGIN_JSON = os.path.join(".claude-plugin", "plugin.json")

# The marketplace's name is what a modeller types after the @ in
# `claude plugin install heron@heron`. Checked against the reserved names in
# Claude Code's marketplace reference on 2026-11-28 (D-212); `claude plugin
# marketplace add` refuses a reserved one, so a rename is a re-check.
MARKETPLACE_NAME = "heron"
BRANCH = "plugin-release"
COMMIT_PREFIX = "Heron plugin "

VERSION_SHAPE = re.compile(r"^(\d+)\.(\d+)\.(\d+)([-+][0-9A-Za-z.-]+)?\Z")
GITHUB_REPO = re.compile(r"^https://github\.com/([A-Za-z0-9-]+/[A-Za-z0-9._-]+?)(?:\.git)?/?\Z")


class Refused(Exception):
    """Nothing is sent. The text says why, and what to do."""


def read_json(path):
    try:
        with io.open(path, encoding="utf-8-sig") as handle:
            return json.load(handle)
    except (IOError, OSError) as exc:
        raise Refused("cannot read %s: %s" % (path, exc))
    except ValueError as exc:
        raise Refused("%s is not valid JSON: %s" % (path, exc))


def template():
    return read_json(os.path.join(ROOT, TEMPLATE_JSON))


def repository(tmpl):
    """`owner/repo` from the template's `repository` - typed in one place."""
    match = GITHUB_REPO.match(str(tmpl.get("repository", "")))
    if not match:
        raise Refused("%s's repository %r is not a https://github.com/<owner>/<repo> "
                      "address, so there is no repository to fetch the plugin from"
                      % (TEMPLATE_JSON, tmpl.get("repository")))
    return match.group(1)


def expected_source(tmpl):
    return {"source": "github", "repo": repository(tmpl), "ref": BRANCH}


def check_marketplace(market, tmpl):
    """Every way `market` (a parsed marketplace.json) is wrong. [] when right."""
    if not isinstance(market, dict):
        return ["the marketplace is not a JSON object"]
    problems = []
    if market.get("name") != MARKETPLACE_NAME:
        problems.append("the marketplace is named %r, not %r - the name a modeller "
                        "types after the @, checked against Claude Code's reserved "
                        "names (D-212)" % (market.get("name"), MARKETPLACE_NAME))
    owner = market.get("owner")
    if not isinstance(owner, dict) or not str(owner.get("name", "")).strip():
        problems.append("the marketplace has no owner with a name - Claude Code "
                        "requires one")
    if not str(market.get("description", "")).strip():
        problems.append("the marketplace has no description - `claude plugin "
                        "validate` warns, and a modeller adding it sees none")
    plugins = market.get("plugins")
    if not isinstance(plugins, list) or len(plugins) != 1 \
            or not isinstance(plugins[0], dict):
        problems.append("the marketplace must list exactly one plugin, Heron's "
                        "(found %r)" % (plugins,))
        return problems
    entry = plugins[0]
    if entry.get("name") != tmpl.get("name"):
        problems.append("the marketplace lists the plugin as %r and plugin.json "
                        "names it %r - install by one name finds nothing under the "
                        "other" % (entry.get("name"), tmpl.get("name")))
    if "version" in entry:
        problems.append("the marketplace's entry types version %r - the plugin's "
                        "one number is plugin.json's, written by the build from "
                        "heron-products.json (D-210 item 3); take it out"
                        % (entry["version"],))
    want = expected_source(tmpl)
    source = entry.get("source")
    if not isinstance(source, dict):
        problems.append("the plugin's source is %r, not this repository at ref %s"
                        % (source, BRANCH))
    else:
        for key in ("source", "repo", "ref"):
            if source.get(key) != want[key]:
                problems.append("the plugin's source has %s %r, not %r"
                                % (key, source.get(key), want[key]))
        extra = sorted(set(source) - set(want))
        if extra:
            problems.append("the plugin's source carries %s - a `sha` pins one "
                            "commit and the plugin would never move; the branch "
                            "is the pin" % ", ".join(extra))
    return problems


def built_version(folder, tmpl):
    """The version a built plugin folder carries. Refuses anything else."""
    manifest = os.path.join(folder, PLUGIN_JSON)
    if not os.path.isfile(manifest):
        raise Refused("%s holds no %s - it is not a folder tools/build-plugin.py "
                      "built" % (folder, PLUGIN_JSON))
    doc = read_json(manifest)
    if doc.get("name") != tmpl.get("name"):
        raise Refused("%s names the plugin %r, not %r"
                      % (manifest, doc.get("name"), tmpl.get("name")))
    version = doc.get("version")
    if not isinstance(version, str) or not VERSION_SHAPE.match(version):
        raise Refused("%s has no version of the shape 1.2.3 (found %r)"
                      % (manifest, version))
    if os.path.exists(os.path.join(folder, ".git")):
        raise Refused("%s holds a .git - the branch holds the plugin's files only"
                      % folder)
    for base, dirs, names in os.walk(folder):
        for name in dirs + names:
            if os.path.islink(os.path.join(base, name)):
                raise Refused("%s is a symbolic link; the branch holds files only"
                              % os.path.join(base, name))
    return version


def files_in(folder):
    """Every file under `folder`, as git spells the path."""
    found = set()
    for base, _dirs, names in os.walk(folder):
        for name in names:
            rel = os.path.relpath(os.path.join(base, name), folder)
            found.add(rel.replace(os.sep, "/"))
    return found


def _triple(version):
    match = VERSION_SHAPE.match(version or "")
    return tuple(int(part) for part in match.groups()[:3]) if match else None


# ------------------------------------------------------------------ stage

def stage(plugin, out, market, tmpl):
    """<out>/plugin-release/ as the branch would hold it, and beside it a
    marketplace whose one plugin is that folder - for `claude plugin validate`."""
    if os.path.exists(out) and os.listdir(out):
        raise Refused("%s is not empty - stage into a new folder" % out)
    tree = os.path.join(out, BRANCH)
    shutil.copytree(plugin, tree, symlinks=False)
    rehearsal = json.loads(json.dumps(market))
    rehearsal["plugins"][0]["source"] = "./" + BRANCH
    os.makedirs(os.path.join(out, ".claude-plugin"))
    with io.open(os.path.join(out, MARKETPLACE), "w", encoding="utf-8",
                 newline="\n") as handle:
        handle.write(json.dumps(rehearsal, indent=2, ensure_ascii=False) + "\n")
    return tree


# -------------------------------------------------------------------- git

def git(repo, args, env=None, cwd=None):
    """Run git; its output, or Refused naming the command and what git said."""
    command = ["git"] + list(args)
    if cwd is None:
        command = ["git", "-C", repo] + list(args)
    try:
        done = subprocess.run(command, cwd=cwd, env=env, stdout=subprocess.PIPE,
                              stderr=subprocess.PIPE, universal_newlines=True,
                              encoding="utf-8", errors="replace")
    except OSError as exc:
        raise Refused("cannot run git: %s" % exc)
    if done.returncode != 0:
        raise Refused("`git %s` failed: %s" % (" ".join(args),
                                               (done.stderr or done.stdout).strip()))
    return done.stdout


def remote_tip(repo, remote):
    """The newest commit of plugin-release on `remote`, or None when it has none."""
    out = git(repo, ["ls-remote", "--heads", remote, "refs/heads/" + BRANCH])
    for line in out.splitlines():
        parts = line.split("\t")
        if len(parts) == 2 and parts[1] == "refs/heads/" + BRANCH:
            return parts[0]
    return None


def fetch_tip(repo, remote):
    """Fetch the remote's newest plugin-release commit; its sha. A shallow
    checkout - CI's - fetches one commit; a whole one is never made shallow."""
    args = ["fetch", "--no-tags", "--quiet"]
    if git(repo, ["rev-parse", "--is-shallow-repository"]).strip() == "true":
        args.append("--depth=1")
    git(repo, args + [remote, "refs/heads/" + BRANCH])
    return git(repo, ["rev-parse", "FETCH_HEAD"]).strip()


def make_tree(repo, folder):
    """Write `folder`'s files as a git tree in `repo`'s objects; its sha.
    A temporary index, so the checkout's own index and files are untouched."""
    gitdir = git(repo, ["rev-parse", "--absolute-git-dir"]).strip()
    scratch = tempfile.mkdtemp(prefix="heron-plugin-index-")
    try:
        env = dict(os.environ, GIT_INDEX_FILE=os.path.join(scratch, "index"))
        base = ["--git-dir", gitdir, "--work-tree", os.path.abspath(folder),
                "-c", "core.autocrlf=false", "-c", "core.safecrlf=false"]
        # --force: what the build wrote is what ships, whatever an ignore
        # file says; the check below proves the tree holds every file.
        git(repo, base + ["add", "--all", "--force", "--", "."], env=env,
            cwd=os.path.abspath(folder))
        return git(repo, base + ["write-tree"], env=env, cwd=os.path.abspath(folder)).strip()
    finally:
        shutil.rmtree(scratch, ignore_errors=True)


def tree_files(repo, tree):
    out = git(repo, ["ls-tree", "-r", "--name-only", "-z", tree])
    return set(name for name in out.split("\0") if name)


def prepare(repo, plugin, tag, remote, tmpl, strict):
    """
    Make the release's commit; send nothing. Returns a dict saying what it
    did. `strict` enforces the rules that belong to the moment of release -
    the tag, and a version new to the branch; a rehearsal reports them.
    """
    version = built_version(plugin, tmpl)
    notes = []

    def rule(text):
        if strict:
            raise Refused(text + " Nothing was sent.")
        notes.append("AT A RELEASE THIS WOULD BE REFUSED: " + text)

    if tag is None:
        if strict:
            raise Refused("--push needs --tag, the release's tag")
        tag = "v" + version
    elif tag != "v" + version:
        rule("The release is tagged %s, but the plugin built from it says %s - "
             "the tag must be `v` followed by manifestVersion in "
             "platform/heron-products.json." % (tag, version))

    tip = remote_tip(repo, remote)
    parent, old_version = None, None
    if tip is not None:
        parent = fetch_tip(repo, remote)
        subject = git(repo, ["log", "-1", "--format=%s", parent]).strip()
        if not subject.startswith(COMMIT_PREFIX):
            raise Refused(
                "%s's newest commit %s says %r - not a commit this tool made. The "
                "branch is written only by the release workflow; find out who "
                "wrote it before anything goes on top. Nothing was sent."
                % (BRANCH, parent[:12], subject))
        try:
            old = json.loads(git(repo, ["show", "%s:%s" % (parent, ".claude-plugin/plugin.json")]))
            old_version = old.get("version")
        except (Refused, ValueError):
            raise Refused("%s's newest commit %s holds no readable "
                          ".claude-plugin/plugin.json at its root. Nothing was sent."
                          % (BRANCH, parent[:12]))

    tree = make_tree(repo, plugin)
    missing = files_in(plugin) - tree_files(repo, tree)
    extra = tree_files(repo, tree) - files_in(plugin)
    if missing or extra:
        raise Refused("the tree git wrote is not the built folder (missing %s, "
                      "extra %s). Nothing was sent."
                      % (sorted(missing)[:5], sorted(extra)[:5]))

    if parent is not None:
        if git(repo, ["rev-parse", parent + "^{tree}"]).strip() == tree:
            return {"version": version, "tag": tag, "commit": parent,
                    "parent": parent, "new": False, "notes": notes,
                    "old_version": old_version}
        if old_version == version:
            rule("%s already holds %s with other files. Claude Code moves a "
                 "modeller only when the number changes, so these would reach "
                 "nobody - raise manifestVersion in platform/heron-products.json "
                 "and release again." % (BRANCH, version))
        elif _triple(old_version) and _triple(version) < _triple(old_version):
            rule("%s holds %s, and %s is lower - it would move modellers back."
                 % (BRANCH, old_version, version))

    source = git(repo, ["rev-parse", "HEAD"]).strip()
    message = "%s%s - release %s\n\nBuilt by tools/build-plugin.py from %s.\n" % (
        COMMIT_PREFIX, version, tag, source)
    args = ["commit-tree", tree, "-m", message]
    if parent is not None:
        args[2:2] = ["-p", parent]
    commit = git(repo, args).strip()
    return {"version": version, "tag": tag, "commit": commit, "parent": parent,
            "new": True, "notes": notes, "old_version": old_version}


def push(repo, remote, done):
    """Send the commit. No `+`, no --force: a branch that moved is refused."""
    git(repo, ["push", "--quiet", remote,
               "%s:refs/heads/%s" % (done["commit"], BRANCH)])
    now = remote_tip(repo, remote)
    if now != done["commit"]:
        raise Refused("pushed, but %s on %s is %s, not %s"
                      % (BRANCH, remote, now, done["commit"]))


# ------------------------------------------------------------------- main

def main(argv=None):
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")

    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--check", action="store_true",
                        help="hold .claude-plugin/marketplace.json to the template")
    parser.add_argument("--plugin", help="a folder tools/build-plugin.py built")
    parser.add_argument("--stage", help="lay the branch out in this new folder; no git")
    parser.add_argument("--tag", help="the release's tag, v + the plugin's version")
    parser.add_argument("--push", action="store_true",
                        help="send the commit to the remote (never forced)")
    parser.add_argument("--remote", default="origin")
    parser.add_argument("--repo", default=ROOT,
                        help="the checkout whose remote is written (default: this one)")
    args = parser.parse_args(argv)

    try:
        tmpl = template()
        market = read_json(os.path.join(ROOT, MARKETPLACE))
        problems = check_marketplace(market, tmpl)
        if problems:
            raise Refused("%s is wrong:\n  - %s" % (MARKETPLACE, "\n  - ".join(problems)))
        if args.check:
            print("%s is right: marketplace %s, plugin %s from %s at ref %s, no "
                  "version of its own." % (MARKETPLACE.replace(os.sep, "/"),
                                           MARKETPLACE_NAME, tmpl["name"],
                                           repository(tmpl), BRANCH))
            return 0
        if not args.plugin:
            raise Refused("name the built folder with --plugin (or use --check)")
        plugin = os.path.abspath(args.plugin)

        if args.stage:
            version = built_version(plugin, tmpl)
            tree = stage(plugin, os.path.abspath(args.stage), market, tmpl)
            print("Laid out plugin %s as %s would hold it, in %s" % (version, BRANCH, tree))
            print("Check both with:")
            print("  claude plugin validate --strict %s" % os.path.abspath(args.stage))
            print("  claude plugin validate --strict %s" % tree)
            print("NOTHING WAS COMMITTED OR SENT.")
            return 0

        done = prepare(os.path.abspath(args.repo), plugin, args.tag, args.remote,
                       tmpl, strict=args.push)
        for note in done["notes"]:
            print(note)
        if not done["new"]:
            print("%s already holds exactly this plugin %s (commit %s). Nothing to "
                  "send." % (BRANCH, done["version"], done["commit"][:12]))
            return 0
        print("Made %s's commit for plugin %s: %s, %s."
              % (BRANCH, done["version"], done["commit"][:12],
                 "on top of %s (%s)" % (done["parent"][:12], done["old_version"])
                 if done["parent"] else "the branch's first, an orphan"))
        if not args.push:
            print("NOTHING WAS SENT - a rehearsal. --push sends it.")
            return 0
        push(os.path.abspath(args.repo), args.remote, done)
        print("Sent: %s on %s is now %s. Modellers move to %s when Claude Code next "
              "updates the marketplace." % (BRANCH, args.remote, done["commit"][:12],
                                            done["version"]))
        return 0
    except Refused as exc:
        print("REFUSED: %s" % exc)
        return 1


if __name__ == "__main__":
    sys.exit(main())
