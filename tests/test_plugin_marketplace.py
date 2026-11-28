#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.3.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Heron's marketplace on `main`, and the plugin-release branch it points at -
the rules of tools/publish-plugin.py, run on a throwaway repository.

    python tests/test_plugin_marketplace.py

WHAT THIS PROVES
----------------
    .claude-plugin/marketplace.json is valid JSON, is named `heron`, lists one
      plugin named as plugin.json names it, fetched from this repository at
      ref `plugin-release`, and types no version of its own
    a marketplace wrong in any of those ways is refused, each with its reason
    the plugin tools/build-plugin.py builds carries heron-products.json's
      version, check-products.py still holds it, and its name is the
      marketplace's plugin's
    the branch: its first commit is an ORPHAN, holding exactly the built
      folder at its root and nothing of `main`; every later commit has the
      branch's newest commit as its one parent, one per release; a re-run of
      the same release adds nothing
    refused, and nothing sent: --push with no tag, a tag that is not `v` and
      the version, the same version with other files, a lower version, and a
      branch whose newest commit this tool did not make
    NEVER FORCED: a branch that moved after it was read is refused by the push
    the checkout it runs in keeps its files, its index and its branches, and a
      whole clone is never made shallow; a shallow one, CI's shape, works
    --stage lays the branch out with a marketplace pointing at it
    every document that names the install line says it works only after the
      first release
    the workflow publishes on a published release, and only its publish job
      pushes - never with --force

WHAT IT CANNOT PROVE
--------------------
That GitHub lets the workflow's token push the branch, or that a modeller's
Claude Code installs it from github.com - that is the first real release, and
section 8 of docs/work-notes/plans/claude-code-plugin-2026-11-27.md on a real
Windows PC. `claude plugin validate` is the workflow's to run: this suite
needs only Python and git.

Exit 0 = every check passed. Exit 1 = at least one failed.
Exit 3 = could not run: no git on this machine. NOT a pass.
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
TOOL = os.path.join(ROOT, "tools", "publish-plugin.py")
BUILD = os.path.join(ROOT, "tools", "build-plugin.py")
PRODUCTS = os.path.join(ROOT, "tools", "check-products.py")
MARKET = os.path.join(ROOT, ".claude-plugin", "marketplace.json")
TEMPLATE = os.path.join(ROOT, "platform", "Heron.Plugin", ".claude-plugin", "plugin.json")
MANIFEST = os.path.join(ROOT, "platform", "heron-products.json")
WORKFLOW = os.path.join(ROOT, ".github", "workflows", "plugin-release.yml")

INSTALL_LINE = "marketplace add Ajmalpshaik/Heron-AI"

FAILURES = []


def check(ok, what):
    print("  %-5s %s" % ("ok" if ok else "FAIL", what))
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


def run(args, cwd=None):
    done = subprocess.run(args, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                          universal_newlines=True, encoding="utf-8", errors="replace")
    return done.returncode, done.stdout


def git(repo, *args):
    code, out = run(["git", "-C", repo] + list(args))
    if code != 0:
        raise RuntimeError("git %s: %s" % (" ".join(args), out))
    return out.strip()


def fake_plugin(parent, version, extra="one"):
    """A small folder shaped as a built plugin - the branch's rules are about
    the folder's shape, not its size."""
    folder = os.path.join(parent, "plugin-%s-%s" % (version, extra))
    os.makedirs(os.path.join(folder, ".claude-plugin"))
    os.makedirs(os.path.join(folder, "brain"))
    with io.open(os.path.join(folder, ".claude-plugin", "plugin.json"), "w",
                 encoding="utf-8") as handle:
        handle.write(json.dumps({"name": read_json(TEMPLATE)["name"],
                                 "version": version}) + "\n")
    with io.open(os.path.join(folder, "brain", "card.txt"), "w", encoding="utf-8") as handle:
        handle.write("content %s\n" % extra)
    with io.open(os.path.join(folder, "README.md"), "w", encoding="utf-8") as handle:
        handle.write("Heron\n")
    return folder


def publish(tool, repo, plugin, *flags):
    """tools/publish-plugin.py's main(), its output captured. (exit, text)"""
    out = io.StringIO()
    keep = sys.stdout
    sys.stdout = out
    try:
        code = tool.main(["--repo", repo, "--plugin", plugin] + list(flags))
    finally:
        sys.stdout = keep
    return code, out.getvalue()


def branch_log(remote):
    """[(sha, [parents], subject)] of plugin-release, newest first."""
    code, out = run(["git", "-C", remote, "log", "--format=%H|%P|%s", "plugin-release"])
    if code != 0:
        return []
    rows = []
    for line in out.splitlines():
        sha, parents, subject = line.split("|", 2)
        rows.append((sha, parents.split(), subject))
    return rows


# ---------------------------------------------------------------- sections

def section_marketplace(tool):
    print("\n1. The marketplace on main")
    market = read_json(MARKET)
    tmpl = read_json(TEMPLATE)
    check(tool.check_marketplace(market, tmpl) == [],
          "%s passes every rule: %s" % (os.path.relpath(MARKET, ROOT),
                                        tool.check_marketplace(market, tmpl) or "none broken"))
    check(market.get("name") == "heron", "the marketplace is named heron")
    plugins = market.get("plugins") or [{}]
    check(len(plugins) == 1 and plugins[0].get("name") == tmpl.get("name"),
          "its one plugin is named as plugin.json names it (%r)" % tmpl.get("name"))
    source = plugins[0].get("source") or {}
    check(isinstance(source, dict) and source.get("ref") == "plugin-release",
          "its source ref is plugin-release")
    check(isinstance(source, dict) and source.get("source") == "github"
          and source.get("repo") == "Ajmalpshaik/Heron-AI",
          "its source is this repository on GitHub, read from plugin.json's repository")
    check("version" not in plugins[0], "its entry types no version of its own")
    code, out = run([sys.executable, TOOL, "--check"])
    check(code == 0, "publish-plugin.py --check exits 0 (%s)" % out.strip().splitlines()[-1])

    print("\n2. A wrong marketplace is refused, with its reason")

    def broken(change, word, what):
        doc = json.loads(json.dumps(market))
        change(doc)
        found = tool.check_marketplace(doc, tmpl)
        check(any(word in p for p in found), "%s - %r" % (what, found[:1]))

    broken(lambda d: d.update(name="claude-plugins-official"), "named",
           "a marketplace named other than heron")
    broken(lambda d: d["plugins"][0].update(version="9.9.9"), "version",
           "an entry that types a version")
    broken(lambda d: d["plugins"][0]["source"].update(ref="main"), "ref",
           "a source at ref main")
    broken(lambda d: d["plugins"][0]["source"].update(repo="someone/else"), "repo",
           "a source in another repository")
    broken(lambda d: d["plugins"][0]["source"].update(sha="a" * 40), "sha",
           "a source pinned to one commit")
    broken(lambda d: d["plugins"][0].update(source="./plugin"), "source",
           "a relative source on main")
    broken(lambda d: d["plugins"][0].update(name="heron-ai"), "names it",
           "an entry named other than plugin.json")
    broken(lambda d: d["plugins"].append({"name": "x", "source": "./x"}), "exactly one",
           "two plugins")
    broken(lambda d: d.pop("owner"), "owner", "no owner")
    broken(lambda d: d.pop("description"), "description", "no description")


def section_build(tool, scratch):
    print("\n3. The built plugin")
    code, out = run([sys.executable, BUILD, "--out", scratch])
    built = os.path.join(scratch, "heron-plugin")
    check(code == 0, "tools/build-plugin.py builds it (exit %d)" % code)
    if code != 0:
        print(out)
        return None
    made = read_json(os.path.join(built, ".claude-plugin", "plugin.json"))
    check(made.get("version") == read_json(MANIFEST).get("manifestVersion"),
          "its version %r is heron-products.json's manifestVersion" % made.get("version"))
    check(made.get("name") == read_json(MARKET)["plugins"][0]["name"],
          "its name is the marketplace's plugin's")
    code, out = run([sys.executable, PRODUCTS, "--plugin", built])
    check(code == 0, "check-products.py --plugin still holds the version (exit %d)" % code)
    return built


def section_branch(tool, scratch, built):
    print("\n4. The plugin-release branch")
    os.makedirs(scratch)
    remote = os.path.join(scratch, "remote.git")
    work = os.path.join(scratch, "work")
    run(["git", "init", "-q", "--bare", remote])
    run(["git", "clone", "-q", remote, work])
    with io.open(os.path.join(work, "main-only.txt"), "w", encoding="utf-8") as handle:
        handle.write("a file of main's\n")
    git(work, "add", "main-only.txt")
    git(work, "commit", "-q", "-m", "main")
    git(work, "push", "-q", "origin", "HEAD:main")
    status_before = git(work, "status", "--porcelain")
    head_before = git(work, "rev-parse", "HEAD")

    v1 = built or fake_plugin(scratch, "0.3.0")
    version = read_json(os.path.join(v1, ".claude-plugin", "plugin.json"))["version"]
    code, out = publish(tool, work, v1, "--tag", "v" + version, "--push")
    log = branch_log(remote)
    check(code == 0 and len(log) == 1, "the first release makes one commit (exit %d)" % code)
    check(bool(log) and log[0][1] == [], "that commit is an orphan - no parent, no history of main")
    files = set(git(remote, "ls-tree", "-r", "--name-only", "plugin-release").splitlines())
    check(files == tool.files_in(v1),
          "the branch holds exactly the built folder's %d files, at its root" % len(files))
    check("main-only.txt" not in files and ".claude-plugin/marketplace.json" not in files,
          "and nothing of main's - no main file, no marketplace")
    check(bool(log) and log[0][2] == "Heron plugin %s - release v%s" % (version, version),
          "its commit is named with the version: %r" % (log[0][2] if log else None))

    code, out = publish(tool, work, v1, "--tag", "v" + version, "--push")
    check(code == 0 and len(branch_log(remote)) == 1,
          "the same release again adds nothing (exit %d)" % code)

    tip = branch_log(remote)[0][0]

    def refused(args, word, what, plugin=None):
        code, out = publish(tool, work, plugin or v1, *args)
        check(code == 1 and word in out and branch_log(remote)[0][0] == tip,
              "%s is refused and nothing is sent - %s"
              % (what, out.strip().splitlines()[-1][:110] if out.strip() else "no output"))

    refused(["--push"], "--tag", "--push with no tag")
    refused(["--tag", "v0.0.1", "--push"], "tagged", "a tag that is not v + the version")
    same = fake_plugin(scratch, version, "other files")
    refused(["--tag", "v" + version, "--push"], "already holds", "the same version with other files",
            plugin=same)
    code, out = publish(tool, work, same)
    check(code == 0 and "WOULD BE REFUSED" in out and branch_log(remote)[0][0] == tip,
          "a rehearsal of it says it would be refused, and sends nothing")

    major, minor, patch = (int(x) for x in version.split("-")[0].split("+")[0].split("."))
    newer = "%d.%d.%d" % (major, minor, patch + 1)
    v2 = fake_plugin(scratch, newer)
    code, out = publish(tool, work, v2, "--tag", "v" + newer, "--push")
    log = branch_log(remote)
    check(code == 0 and len(log) == 2 and log[0][1] == [tip],
          "release %s is one commit whose one parent is the last release's (exit %d)"
          % (newer, code))
    tip = log[0][0]
    refused(["--tag", "v" + version, "--push"], "lower", "a lower version after %s" % newer,
            plugin=v1)

    check(git(work, "status", "--porcelain") == status_before
          and git(work, "rev-parse", "HEAD") == head_before,
          "the checkout's files, index and HEAD are as they were")
    check(git(work, "branch", "--list", "plugin-release") == "",
          "no local plugin-release branch was made - nothing was checked out")
    check(git(work, "rev-parse", "--is-shallow-repository") == "false",
          "a whole clone is not made shallow by the fetch")

    print("\n5. Never forced")
    newest = "%d.%d.%d" % (major, minor, patch + 2)
    v3 = fake_plugin(scratch, newest)
    tmpl = read_json(TEMPLATE)
    done = tool.prepare(work, v3, "v" + newest, "origin", tmpl, strict=True)
    # Another run moves the branch after this one read it.
    other = os.path.join(scratch, "other")
    run(["git", "clone", "-q", "--branch", "main", remote, other])
    v3b = fake_plugin(scratch, newest, "raced")
    code, out = publish(tool, other, v3b, "--tag", "v" + newest, "--push")
    raced = branch_log(remote)[0][0]
    try:
        tool.push(work, "origin", done)
        pushed = True
    except tool.Refused:
        pushed = False
    check(code == 0 and not pushed and branch_log(remote)[0][0] == raced,
          "a branch that moved after it was read is refused by the push, and kept "
          "(the other run exit %d, this push %s)" % (code, "SENT" if pushed else "refused"))

    git(other, "fetch", "-q", "origin", "plugin-release")
    git(other, "checkout", "-q", "-b", "by-hand", "FETCH_HEAD")
    with io.open(os.path.join(other, "README.md"), "a", encoding="utf-8") as handle:
        handle.write("edited by hand\n")
    git(other, "commit", "-q", "-am", "fix a typo")
    git(other, "push", "-q", "origin", "HEAD:plugin-release")
    tip = branch_log(remote)[0][0]
    v4 = fake_plugin(scratch, "%d.%d.%d" % (major, minor, patch + 3))
    refused(["--tag", "v%d.%d.%d" % (major, minor, patch + 3), "--push"], "not a commit this tool made",
            "a branch whose newest commit was made by hand", plugin=v4)

    print("\n6. A shallow checkout - CI's shape")
    remote2 = os.path.join(scratch, "remote2.git")
    full = os.path.join(scratch, "full")
    run(["git", "init", "-q", "--bare", remote2])
    run(["git", "clone", "-q", remote2, full])
    with io.open(os.path.join(full, "main-only.txt"), "w", encoding="utf-8") as handle:
        handle.write("a file of main's\n")
    git(full, "add", "main-only.txt")
    git(full, "commit", "-q", "-m", "main")
    git(full, "push", "-q", "origin", "HEAD:main")
    first = fake_plugin(scratch, "1.0.0", "shallow")
    publish(tool, full, first, "--tag", "v1.0.0", "--push")
    tip = branch_log(remote2)[0][0] if branch_log(remote2) else None
    shallow = os.path.join(scratch, "shallow")
    run(["git", "clone", "-q", "--depth", "1", "--branch", "main", "file://" + remote2, shallow])
    check(git(shallow, "rev-parse", "--is-shallow-repository") == "true",
          "the clone is shallow, as actions/checkout leaves it")
    second = fake_plugin(scratch, "1.0.1", "shallow")
    code, out = publish(tool, shallow, second, "--tag", "v1.0.1", "--push")
    log = branch_log(remote2)
    check(code == 0 and tip is not None and len(log) == 2 and log[0][1] == [tip],
          "from it the release is still one commit on the last (exit %d)" % code)


def section_stage(tool, scratch, built):
    print("\n7. --stage, for claude plugin validate")
    os.makedirs(scratch)
    plugin = built or fake_plugin(scratch, "0.3.0", "stage")
    out_dir = os.path.join(scratch, "stage")
    code, out = run([sys.executable, TOOL, "--plugin", plugin, "--stage", out_dir])
    check(code == 0, "it lays the branch out (exit %d)" % code)
    tree = os.path.join(out_dir, "plugin-release")
    check(os.path.isdir(tree) and tool.files_in(tree) == tool.files_in(plugin),
          "plugin-release/ holds exactly the built folder")
    rehearsal = read_json(os.path.join(out_dir, ".claude-plugin", "marketplace.json"))
    real = read_json(MARKET)
    check(rehearsal["plugins"][0]["source"] == "./plugin-release",
          "its marketplace points at that folder")
    rehearsal["plugins"][0]["source"] = real["plugins"][0]["source"]
    check(rehearsal == real, "and is otherwise the marketplace on main")
    code, out = run([sys.executable, TOOL, "--plugin", plugin, "--stage", out_dir])
    check(code == 1 and "not empty" in out, "a folder that is not empty is refused")


def section_docs():
    print("\n8. Every document naming the install line says when it works")
    # Tracked and new: a document this change adds is read before it is committed.
    code, out = run(["git", "-C", ROOT, "ls-files", "--cached", "--others",
                     "--exclude-standard", "*.md"])
    named = []
    for rel in out.splitlines():
        path = os.path.join(ROOT, rel)
        try:
            lines = io.open(path, encoding="utf-8").read().splitlines()
        except (IOError, OSError, UnicodeDecodeError):
            continue
        for i, line in enumerate(lines):
            if INSTALL_LINE not in line:
                continue
            # Near: the ten lines either side, so a command in a fenced block
            # is read with the sentences around it.
            block = lines[max(0, i - 10):i + 11]
            says = re.search(r"first (real )?release", " ".join(block), re.I) is not None
            named.append(rel)
            check(says, "%s:%d says it works only after the first release" % (rel, i + 1))
    check(bool(named), "at least one document names the install line (%d found)" % len(named))


def section_workflow():
    print("\n9. The workflow")
    text = io.open(WORKFLOW, encoding="utf-8").read()
    body = "\n".join(l for l in text.splitlines() if not l.lstrip().startswith("#"))
    check(re.search(r"release:\s*\n\s*types:\s*\[published\]", body) is not None,
          "it publishes when a release is published")
    check("--force" not in body and "+refs" not in body and "push -f" not in body,
          "no force anywhere in it")
    check(body.count("--push") == 1, "one step pushes, and only one")
    publish_job = body.split("\n  publish:", 1)[-1]
    check("--push" in publish_job and "contents: write" in publish_job,
          "the push is in the publish job, the one job that may write")
    rehearse_job = body.split("\n  rehearse:", 1)[-1].split("\n  publish:", 1)[0]
    check("--push" not in rehearse_job and "contents: write" not in rehearse_job,
          "the rehearsal pushes nothing and may write nothing")
    check("claude plugin validate --strict" in rehearse_job,
          "the rehearsal runs claude plugin validate --strict")


def main():
    print("Heron's marketplace and the plugin-release branch\n")
    if shutil.which("git") is None:
        print("COULD NOT RUN: no git on this machine.")
        return 3
    tool = load(TOOL, "heron_publish_plugin")

    scratch = tempfile.mkdtemp(prefix="heron-marketplace-")
    keep_env = dict(os.environ)
    os.environ.update({
        "GIT_AUTHOR_NAME": "heron-test", "GIT_AUTHOR_EMAIL": "test@example.invalid",
        "GIT_COMMITTER_NAME": "heron-test", "GIT_COMMITTER_EMAIL": "test@example.invalid",
        # Hermetic: no signing, no hooks, no default branch name from this PC.
        "GIT_CONFIG_GLOBAL": os.path.join(scratch, "no-global-config"),
        "GIT_CONFIG_NOSYSTEM": "1",
    })
    try:
        section_marketplace(tool)
        built = section_build(tool, os.path.join(scratch, "build"))
        section_branch(tool, os.path.join(scratch, "branch"), built)
        section_stage(tool, os.path.join(scratch, "stage-run"), built)
        section_docs()
        section_workflow()
    finally:
        os.environ.clear()
        os.environ.update(keep_env)
        shutil.rmtree(scratch, ignore_errors=True)

    print()
    if FAILURES:
        print("%d check(s) FAILED:" % len(FAILURES))
        for what in FAILURES:
            print("  - " + what)
        return 1
    print("Every check passed. A green run is a statement about the files and a")
    print("throwaway repository - not about GitHub, and not about a modeller's PC.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
