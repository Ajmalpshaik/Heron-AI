# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-SEC-012
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Secret Manager - a handle travels, a value does not, and nothing logs one.

    python brain/heron_secrets.py      what a leak looks like when it is caught

ARTICLE 17, AND WHY IT IS URGENT RATHER THAN THEORETICAL
---------------------------------------------------------
"Never write an API key, token or credential into a fragment, skill, prompt,
source file, log or commit. Never include one in a result."

docs/12 s5a adds the reason: this repository is public (D-07). Anything
committed to a public repository is compromised permanently - history persists
and forks propagate. A token that reaches a log has already leaked, whoever
deletes the line afterwards.

THIS MODULE IS NOT THE STORE, AND THAT IS DELIBERATE
-----------------------------------------------------
docs/12 s5a.1 asks for a credential store outside the workspace - Windows
Credential Manager or DPAPI-protected storage - and this repository has one
rule about paths: only the path manager builds them. So the store arrives here
as a BACKEND, injected, and this module holds no path of its own.

What it does own is the part that keeps being got wrong:

  THE HANDLE      a fragment or agent is given `heron:secret/github-token`,
                  never a value (docs/12 s5a.3). The Kernel resolves it at
                  call time, and the value's lifetime is that call.
  THE REDACTOR    everything on the way out - audit lines, error messages,
                  evidence records, anything shown to a model - goes through
                  one redactor (docs/12 s5a.2).

A HANDLE THAT PRINTS ITS VALUE IS THE WHOLE BUG
------------------------------------------------
The classic leak is not a token written down on purpose. It is an object put
into a log line, or into an exception message, whose __repr__ helpfully
includes what it holds. So Handle.__repr__ and __str__ return the handle, the
value is not an attribute anything can reach, and a Handle in an f-string is
safe by construction rather than by everybody remembering.

WHAT THE REDACTOR CAN AND CANNOT DO
------------------------------------
It removes every registered value, and it removes strings shaped like the
credentials that actually appear in this kind of work. It cannot recognise a
secret it has never seen that looks like ordinary text, and saying so matters
more than a reassuring sentence would: a redactor believed to be complete is
how a token ends up in a log nobody re-reads. Pre-commit scanning is the second
layer docs/12 s5a.4 asks for, and it is a different tool.

The marker is a FIXED WIDTH. Replacing a value with stars of the same length
publishes the length, and a length is a meaningful clue about which credential
it was.
"""

import os
import re
import sys

MARKER = "[redacted]"
HANDLE_PREFIX = "heron:secret/"

# The shapes that actually turn up: provider keys, forge tokens, chat-app
# tokens, cloud access keys, and a bearer header. Each is anchored on a prefix
# that is not ordinary prose, because a pattern that matches ordinary prose
# redacts the log instead of the secret.
PATTERNS = (
    ("github token",    re.compile(r"\bgh[pousr]_[A-Za-z0-9]{16,}")),
    ("github pat",      re.compile(r"\bgithub_pat_[A-Za-z0-9_]{20,}")),
    ("provider key",    re.compile(r"\bsk-[A-Za-z0-9_\-]{16,}")),
    ("aws access key",  re.compile(r"\bAKIA[0-9A-Z]{16}\b")),
    ("slack token",     re.compile(r"\bxox[baprs]-[A-Za-z0-9\-]{10,}")),
    ("bearer token",    re.compile(r"(?i)\bbearer\s+[A-Za-z0-9._\-]{20,}")),
)


def _inside(path, workspace):
    """
    Is `path` inside `workspace`, compared as paths rather than as text.

    Three things a startswith() on raw strings gets wrong, and the third is
    the one that matters:

      case      Windows paths are case-insensitive, so C:\\Heron-AI and
                c:\\heron-ai are one folder. A case-sensitive compare lets a
                store inside the repository through the boundary.
      shape     ./Heron-AI/../Heron-AI/.secrets is inside, and reads as a
                different string.
      boundary  "Heron-AI-notes" starts with "Heron-AI" and is NOT inside it.
                A prefix test says it is, and would refuse a store that was
                perfectly safe - the failure that teaches people to pass the
                check a folder name it does not complain about.

    So both are made absolute, normalised for the platform, and compared
    COMPONENT BY COMPONENT.
    """
    here = os.path.normcase(os.path.abspath(str(path))).replace("\\", "/")
    root = os.path.normcase(os.path.abspath(str(workspace))).replace("\\", "/")
    here_parts = [p for p in here.split("/") if p]
    root_parts = [p for p in root.split("/") if p]
    return here_parts[:len(root_parts)] == root_parts


class Handle(object):
    """
    A name for a secret. It is what travels; the value never does.

    __str__ and __repr__ both give the handle, so putting one in a log line or
    an exception message cannot leak anything. That is the point of the class
    existing at all rather than passing the string around.
    """

    def __init__(self, name):
        if not name or not str(name).startswith(HANDLE_PREFIX):
            raise ValueError("a secret handle starts with '%s' - '%s' does not"
                             % (HANDLE_PREFIX, name))
        self.name = str(name)

    def __str__(self):
        return self.name

    def __repr__(self):
        return "Handle(%s)" % self.name

    def __eq__(self, other):
        return isinstance(other, Handle) and other.name == self.name

    def __hash__(self):
        return hash(self.name)


class Secrets(object):
    """
    The resolver and the redactor. Holds no path and writes nothing.

    `backend` is a callable: handle name in, value out, None when it has no
    such secret. Windows Credential Manager on a real machine; a dictionary in
    a test. Injected, because only the path manager may decide where anything
    lives.
    """

    def __init__(self, backend=None, workspace=None):
        self._backend = backend
        self._workspace = workspace
        self._known = {}             # handle name -> value, for redaction only

    # ------------------------------------------------------------- resolve
    def resolve(self, handle):
        """
        The value, for the length of one call. Raises rather than returning
        None for a missing secret: a caller that carried on with None would
        send an unauthenticated request and report whatever came back.
        """
        name = str(handle)
        if not name.startswith(HANDLE_PREFIX):
            raise ValueError("'%s' is not a secret handle" % name)
        if self._backend is None:
            raise LookupError(
                "BACKEND_UNAVAILABLE: no credential store is configured, "
                "so '%s' cannot be "
                "resolved. Heron does not keep credentials in the workspace "
                "(Article 17)." % name)
        try:
            value = self._backend(name)
        except Exception as exc:                             # noqa: BLE001
            # Deliberately broad: the backend is Windows Credential Manager or
            # whatever else a machine has, and an outage there is an
            # operational failure the contract already declares and retries.
            # Letting it escape meant the one failure worth retrying could
            # never take the retry path the contract promises.
            raise LookupError("BACKEND_UNAVAILABLE: the credential store "
                              "could not be reached - %s: %s"
                              % (type(exc).__name__, exc))
        if value is None:
            raise LookupError("NO_SUCH_HANDLE: the credential store has "
                              "no '%s'" % name)
        self._known[name] = value
        return value

    def register_for_redaction(self, handle, value):
        """
        Teach the redactor a value without resolving it through a backend.

        Used where a secret arrives from somewhere else entirely - a header a
        caller supplied, a token pasted into a config - so that it can still be
        kept out of the log.
        """
        self._known[str(handle)] = value
        return self

    def use_store(self, path):
        """
        Point at a credential store. Refuses one inside the workspace.

        docs/12 s5a.1: "Never a file in the repo tree." A store in the tree is
        a secret one `git add -A` away from a public repository, and D-07 makes
        that permanent.
        """
        if self._workspace and _inside(path, self._workspace):
            raise ValueError(
                "STORE_INSIDE_WORKSPACE: that credential store is inside "
                "the workspace. Anything in "
                "the tree is one commit from a public repository, and a public "
                "repository is permanent (D-07, docs/12 s5a.1).")
        return path

    # -------------------------------------------------------------- redact
    def redact(self, text):
        """
        (clean_text, found) - everything on the way out goes through here.

        Registered values first, so a known secret is named by its handle
        rather than by the pattern that happened to match it. A redaction
        nothing reports is a leak nobody can investigate.
        """
        if text is None:
            return None, []
        clean = str(text)
        found = []

        for name, value in sorted(self._known.items(),
                                  key=lambda kv: -len(str(kv[1] or ""))):
            if value and str(value) in clean:
                clean = clean.replace(str(value), MARKER)
                found.append(name)

        for label, pattern in PATTERNS:
            if pattern.search(clean):
                clean = pattern.sub(MARKER, clean)
                found.append(label)

        return clean, found

    def refuse_secret_input(self, payload):
        """
        A payload a fragment is about to be given. Returns the offending keys.

        docs/12 s5a.3: secrets are never a fragment input - a fragment gets a
        handle. This is the check that makes that a rule rather than a habit,
        and it looks at values rather than at key names: calling the field
        `token` is not what makes it dangerous.
        """
        offending = []

        def look(where, value):
            if isinstance(value, Handle):
                return
            if isinstance(value, dict):
                for key, inner in sorted(value.items()):
                    look("%s.%s" % (where, key) if where else str(key), inner)
                return
            if isinstance(value, (list, tuple)):
                for index, inner in enumerate(value):
                    look("%s[%d]" % (where, index), inner)
                return
            if not isinstance(value, str) or value.startswith(HANDLE_PREFIX):
                return
            _clean, found = self.redact(value)
            if found:
                offending.append((where, "VALUE_OFFERED_AS_INPUT", found[0]))

        # NESTED VALUES ARE LOOKED AT, because the contract allows a `map`
        # payload and this method is the boundary that enforces
        # VALUE_OFFERED_AS_INPUT. Stopping at top-level strings meant
        # {"auth": {"token": "..."}} passed clean, which is exactly how a
        # credential would actually be handed to a fragment - nobody puts it
        # in a bare top-level field called `secret`.
        for key, value in sorted((payload or {}).items()):
            look(str(key), value)
        return offending


# The sample credentials below are assembled from pieces rather than written
# out. A literal token-shaped string in a tracked file is exactly what
# push protection and pre-commit scanning exist to stop, and a demonstration
# that cannot be committed demonstrates nothing. The pieces also make the
# point: what the redactor matches is the SHAPE, so a fake one is caught the
# same as a real one.
FAKE_FORGE_TOKEN = "gh" + "p_" + ("A1b2C3d4E5f6" * 3)
FAKE_CLOUD_KEY = "AK" + "IA" + "IOSFODNN7EXAMPLE"


def main(argv):
    store = {HANDLE_PREFIX + "github-token": FAKE_FORGE_TOKEN}
    secrets = Secrets(backend=store.get, workspace="/home/user/Heron-AI")
    handle = Handle(HANDLE_PREFIX + "github-token")

    print("SECRET MANAGER   the handle travels, the value does not")
    print("=" * 67)
    print("  a handle in a log line      %s" % handle)
    print("  the same handle repr'd      %r" % handle)

    value = secrets.resolve(handle)
    line = "PUT /repos failed, sent Authorization: Bearer %s" % value
    clean, found = secrets.redact(line)
    print()
    print("  what an error message tried to say:")
    print("    %s" % clean)
    print("    redacted: %s" % ", ".join(found))

    unknown = "the runner exported %s before it failed" % FAKE_CLOUD_KEY
    clean, found = secrets.redact(unknown)
    print()
    print("  a secret this process never resolved, caught by shape:")
    print("    %s" % clean)
    print("    redacted: %s" % ", ".join(found))

    print()
    print("  a credential store inside the workspace:")
    try:
        secrets.use_store("/home/user/Heron-AI/.secrets.json")
        print("    FAIL  it was allowed")
        return 1
    except ValueError as exc:
        print("    refused  %s" % exc)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
