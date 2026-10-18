#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Does the secret gate see what it says it sees, and nothing it says it does not?

    python tests/test_check_secrets.py

`tools/check-secrets.py` is a gate a pull request has to pass (D-139). It
reads the CONTENTS of every tracked file against heron_secrets.PATTERNS and
the NAMES of every tracked file against the Secrets block of .gitignore.

EVERY CASE BUILDS ITS OWN REPOSITORY, in a temp folder, and points the tool at
it. A suite that scanned this checkout would be asserting today's tree, which
is what CI already does and a different question from whether the gate works.

Every fake credential here is built from pieces in heron_secrets
(FAKE_FORGE_TOKEN, FAKE_PRIVATE_KEY), so this file carries no literal of any
shape the gate looks for - the gate runs over this file too.

Exits 3, NOT RUN, when git is not on this machine: the tool cannot run without
it, so nothing here could prove anything either way.
"""

import importlib.util
import io
import os
import shutil
import stat
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-secrets.py")
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_secrets as SECRETS                               # noqa: E402

FAILURES = []

GITIGNORE = """# ====================
#  Secrets
# ====================
.env
.env.*
!.env.example
*.key
credentials.*

# ====================
#  Build output
# ====================
bin/
"""

NO_BLOCK = """# ====================
#  Build output
# ====================
bin/
"""


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has hyphens in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_secrets", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


def have_git():
    try:
        return subprocess.run(["git", "--version"], stdout=subprocess.PIPE,
                              stderr=subprocess.PIPE).returncode == 0
    except OSError:
        return False


def _writable(func, path, _info):
    os.chmod(path, stat.S_IWRITE)
    func(path)


MADE = []


def repo(files, gitignore=GITIGNORE, force=(), init=True):
    """A temp folder holding these files, staged in a fresh repository."""
    root = tempfile.mkdtemp(prefix="heron-check-secrets-")
    MADE.append(root)
    with io.open(os.path.join(root, ".gitignore"), "w", encoding="utf-8") as f:
        f.write(gitignore)
    for name, content in files.items():
        path = os.path.join(root, *name.split("/"))
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "wb") as f:
            f.write(content if isinstance(content, bytes)
                    else content.encode("utf-8"))
    if init:
        # A git hook or an outer repository must not redirect these, and a
        # step that failed must stop the case: a repository staged wrong
        # would let every assertion below pass for the wrong reason.
        env = {key: value for key, value in os.environ.items()
               if key not in ("GIT_DIR", "GIT_INDEX_FILE", "GIT_WORK_TREE")}
        steps = [["git", "init", "-q", root], ["git", "-C", root, "add", "."]]
        steps += [["git", "-C", root, "add", "-f", name] for name in force]
        for step in steps:
            done = subprocess.run(step, stdout=subprocess.PIPE,
                                  stderr=subprocess.PIPE, env=env)
            if done.returncode != 0:
                raise RuntimeError("could not build the case: %s" % step)
    return root


def scan(tool, root):
    out = io.StringIO()
    code = tool.run(root=root, out=out)
    return code, out.getvalue()


def main():
    if not have_git():
        print("NOT RUN  git is not on this machine, and the gate cannot run "
              "without it")
        return 3
    tool = load()
    if tool is None:
        print("FAILED  tools/check-secrets.py could not be loaded")
        return 1
    # No temp folder may find a repository above it and answer for it.
    os.environ["GIT_CEILING_DIRECTORIES"] = tempfile.gettempdir()
    token = SECRETS.FAKE_FORGE_TOKEN

    print("1. A clean tree passes")
    code, out = scan(tool, repo({"README.md": "hello\n",
                                 ".env.example": "TOKEN=put-yours-here\n"}))
    check(code == 0 and "PASS" in out, "exit 0 and PASS")
    check("3 tracked file(s); 3 read as text" in out,
          "and all three tracked files were read - the pass is not vacuous")

    print()
    print("2. A token in a tracked file fails, and its value is not printed")
    code, out = scan(tool, repo({"src/app.py": "x = 1\nTOKEN = '%s'\n"
                                               % token}))
    check(code == 1, "exit 1")
    check("src/app.py:2:10 - github token" in out,
          "the finding names the file, line, column and shape")
    check(token not in out, "the token itself appears nowhere in the report")

    print()
    print("3. A private key's BEGIN line fails")
    code, out = scan(tool, repo({"keys/deploy.txt": "notes\n%s\n"
                                                    % SECRETS.FAKE_PRIVATE_KEY}))
    check(code == 1 and "keys/deploy.txt:2:1 - private key" in out,
          "exit 1, naming the private key at line 2")

    print()
    print("4. A force-added .env fails by its name; .env.example does not")
    code, out = scan(tool, repo({".env": "A=1\n", ".env.example": "A=\n"},
                                force=(".env",)))
    check(code == 1 and "FAIL  .env - tracked" in out,
          "the tracked .env fails although it holds no token at all")
    check("FAIL  .env.example" not in out,
          "the block's '!' line is honoured, as git honours it")

    print()
    print("5. An ignored file is not read, because it is not published")
    code, out = scan(tool, repo({".env": "TOKEN=%s\n" % token,
                                 "README.md": "hello\n"}))
    check(code == 0, "a real .env, ignored as it should be, passes")

    print()
    print("6. A file that is not text is counted, not read")
    code, out = scan(tool, repo({"model.bin": b"\0\0" + token.encode("ascii"),
                                 "README.md": "hello\n"}))
    check(code == 0, "exit 0 - it was not read, so it found nothing")
    check("1 not text and not read" in out,
          "and the report says one file was not read")

    print()
    print("7. It refuses to run rather than guess")
    code, out = scan(tool, repo({"README.md": "hello\n"}, gitignore=NO_BLOCK))
    check(code == 2 and "could not run" in out,
          "no Secrets block in .gitignore: exit 2, not a pass")
    code, out = scan(tool, repo({"README.md": "hello\n"}, init=False))
    check(code == 2 and "not a git checkout" in out,
          "not a checkout: exit 2")
    tool.GIT = "heron-no-such-git"
    try:
        code, out = scan(tool, repo({"README.md": "hello\n"}))
    finally:
        tool.GIT = "git"
    check(code == 2 and "git was not found" in out, "no git: exit 2")

    print()
    print("8. A name is matched in any folder and in any case")
    code, out = scan(tool, repo({"config/.env": "A=1\n",
                                 "certs/DEPLOY.KEY": "x\n"},
                                force=("config/.env", "certs/DEPLOY.KEY")))
    check("FAIL  config/.env - tracked" in out,
          "a .env two folders down is refused like one at the top")
    check("FAIL  certs/DEPLOY.KEY - tracked" in out,
          "an upper-case name is refused on every system, CI's included")
    named = token + ".key"
    code, out = scan(tool, repo({named: "x\n"}, force=(named,)))
    check(code == 1 and token not in out,
          "a file NAMED like a token is refused, and the name is redacted")

    print()
    print("9. UTF-16 text is read, not skipped as binary")
    utf16 = ("TOKEN=%s\r\n" % token).encode("utf-16")
    code, out = scan(tool, repo({"logs/run.txt": utf16}))
    check(code == 1 and "logs/run.txt:1:7 - github token" in out,
          "what PowerShell 5 writes with '>' is scanned like any text")

    print()
    print("10. An empty Secrets block is not read from the next block")
    empty = GITIGNORE.replace(".env\n.env.*\n!.env.example\n*.key\n"
                              "credentials.*\n", "# nothing yet\n")
    path = os.path.join(repo({}, gitignore=empty, init=False), ".gitignore")
    check(tool.secrets_block(path) == [],
          "a block holding only a comment is empty - not bin/ from below it")
    code, out = scan(tool, repo({"README.md": "hello\n"}, gitignore=empty))
    check(code == 2, "and the run refuses rather than guessing")

    print()
    print("11. Both lists come from the one place that owns them")
    check(tool.SECRETS.PATTERNS is SECRETS.PATTERNS,
          "the shapes are the redactor's own PATTERNS, not a copy")
    block = tool.secrets_block(os.path.join(ROOT, ".gitignore"))
    check(".env" in block and "!.env.example" in block
          and "credentials.*" in block,
          "this repository's .gitignore Secrets block is found and read")

    for root in MADE:
        shutil.rmtree(root, onerror=_writable)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the gate reads what it says it reads, and prints no value")
    return 0


if __name__ == "__main__":
    sys.exit(main())
