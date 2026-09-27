#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
What Revit's Talk button writes, read by what the chat reads it with (D-104).

    python tests/test_talk_contract.py

THE FORMAT HAS TWO HALVES IN TWO LANGUAGES. HeronTalkMailbox.cs writes each
message and selection; mcp/server/heron_talk.py reads them. Either can be
right on its own and the pair still wrong - a field renamed on one side, a
quote escaped differently, a number the other side does not count. This suite
builds tests/Heron.Talk.TestHost, which links the add-in's own
HeronTalkMailbox.cs by source, drives it one step at a time, and reads every
step back with heron_talk. Nothing here is a copy of either half.

WHAT IT PROVES
  1. Both halves name the SAME talk folder - on this system, where both can
     be pointed at a temporary one.
  2. The Talk button sees the Python listener's heartbeat, and sees nothing
     before it.
  3. A message and its selection survive the crossing whole: a quote and a
     backslash in the model's name, a name that is not ASCII, an element with
     no category, one that could not be read.
  4. Nothing selected, and more selected than was saved, both read back as
     what they are.
  5. Numbers are never reused while a Revit runs, even after a message is
     claimed; the short memory keeps the last 20; and closing Revit forgets it.

IT EXITS 3 WITHOUT .NET, which is NOT a pass - the same rule as
tests/test_binding_note.py, whose way of choosing a framework this copies.
"""

import io
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HOST = os.path.join(ROOT, "tests", "Heron.Talk.TestHost")
COULD_NOT_RUN = 3
PID = 424242                 # Program.Pid in the host
FAILURES = []

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_bridge_client as bridge           # noqa: E402
import heron_talk as talk                      # noqa: E402


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def _asked(what):
    """`dotnet <what>`, or None when dotnet cannot answer at all."""
    try:
        out = subprocess.check_output(["dotnet", what], stderr=subprocess.STDOUT)
    except Exception:                                      # noqa: BLE001
        return None
    return out.decode("utf-8", "replace")


def _majors(said, prefix=None):
    """The MAJOR version of every line dotnet listed, highest last."""
    out = []
    for line in (said or "").splitlines():
        parts = line.split()
        if prefix:
            if not line.startswith(prefix):
                continue
            parts = parts[1:]
        if not parts:
            continue
        try:
            out.append(int(parts[0].split(".")[0]))
        except ValueError:
            continue
    return sorted(set(out))


def _tfm():
    """The newest framework an installed SDK can build AND a runtime can run.

    Why both, and why asking either alone picks a target that cannot be
    built, is written at length in tests/test_binding_note.py's copy.
    """
    runtimes = _majors(_asked("--list-runtimes"), "Microsoft.NETCore.App")
    sdks = _majors(_asked("--list-sdks"))
    if not runtimes or not sdks:
        return None
    usable = [m for m in runtimes if m <= max(sdks)]
    return "net%d.0" % max(usable) if usable else None


class Host(object):
    """The built test host, run one step at a time."""

    def __init__(self, dll, env, root):
        self.dll, self.env, self.root = dll, env, root

    def step(self, *args):
        cmd = ["dotnet", self.dll] + list(args)
        if self.root is not None and args and args[0] not in ("root", "describe"):
            cmd += ["--root", self.root]
        ran = subprocess.run(cmd, env=self.env, stdout=subprocess.PIPE,
                             stderr=subprocess.STDOUT)
        said = (ran.stdout or b"").decode("utf-8", "replace")
        answer = {}
        for line in said.splitlines():
            if "=" in line:
                key, _, value = line.partition("=")
                answer[key.strip()] = value
        answer["_code"] = ran.returncode
        answer["_said"] = said
        return answer


def main():
    tfm = _tfm()
    if tfm is None:
        print("COULD NOT RUN - no framework here that an installed SDK can build AND an")
        print("  installed runtime can run. This suite builds its host and then runs it.")
        print("  Linux:   apt-get install -y dotnet-sdk-10.0")
        print("  This is exit 3, which is NOT a pass: nothing was checked.")
        return COULD_NOT_RUN

    out_dir = "bin/x64/Debug-%s/" % tfm
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024",
         "-p:HeronTfm=%s" % tfm, "-p:OutputPath=%s" % out_dir],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        print("FAILED - the test host did not build for %s." % tfm)
        said = (built.stdout or b"").decode("utf-8", "replace").strip()
        for line in said.splitlines()[-25:]:
            print("    %s" % line)
        return 1

    dll = os.path.join(HOST, out_dir, "Heron.Talk.TestHost.dll")
    if not os.path.exists(dll):
        print("FAILED - built, and %s is not there." % dll)
        return 1

    work = tempfile.mkdtemp(prefix="heron-talk-contract-")
    try:
        return run(dll, work)
    finally:
        shutil.rmtree(work, ignore_errors=True)


def run(dll, work):
    env = dict(os.environ)
    posix = os.name != "nt"
    if posix:
        # BOTH HALVES ASKED THE SAME QUESTION. On this system .NET reads the
        # local-data folder from XDG_DATA_HOME, and so does
        # heron_bridge_client.local_app_data - so pointing that one variable
        # at a temporary folder moves HeronPaths.Talk and TALK_DIR together.
        env["XDG_DATA_HOME"] = work
        root = os.path.join(bridge.local_app_data(env), "Heron", "talk")
        host = Host(dll, env, None)
    else:
        # Windows asks the shell for the folder, not a variable, so the only
        # way to keep this off the modeller's real talk folder is to name one.
        root = os.path.join(work, "talk")
        host = Host(dll, env, root)

    print("1. Both halves name the same folder")
    if posix:
        said = host.step("root").get("root")
        check(said == root, "HeronPaths.Talk is the folder heron_talk reads: %s" % said)
    else:
        print("  --    NOT RUN on Windows: the folder cannot be moved there without a real one")

    print()
    print("2. The Talk button sees the chat's heartbeat")
    check(host.step("listening").get("listening") == "false",
          "with no chat listening, the button sees none")
    talk.heartbeat(root, 31337)
    check(host.step("listening").get("listening") == "true",
          "with the Python listener's heartbeat written, the button sees it")
    stale = talk.heartbeat(root, 31338)
    talk.stop_heartbeat(root, 31337)
    old = os.path.getmtime(stale) - 60
    os.utime(stale, (old, old))
    check(host.step("listening").get("listening") == "false",
          "a heartbeat a minute old is a chat that has closed, and does not count")
    talk.stop_heartbeat(root, 31338)
    check(host.step("listening").get("listening") == "false",
          "and nothing is seen once the listener has gone")

    print()
    print("3. A message and its selection cross whole")
    sent = host.step("send", "--text", "resize these to 400x300")
    check(sent.get("message") == "1", "the first message is number 1: %s" % sent.get("message"))
    waiting = talk.pending(root)
    check([(p, n) for p, n, _ in waiting] == [(PID, 1)],
          "heron_talk finds it waiting from that Revit: %s" % [(p, n) for p, n, _ in waiting])
    message = talk.claim(waiting[0][2]) if waiting else {}
    check(message.get("text") == "resize these to 400x300", "the words arrive unchanged")
    check(message.get("document") == 'Tower "A" \\ MEP'
          and message.get("documentPath") == 'C:\\Projects\\Tower "A"\\MEP.rvt',
          "a quote and a backslash in the model's name survive: %r" % message.get("document"))
    check(message.get("view") == "Level 1 - Mech" and message.get("revitVersion") == "2024"
          and message.get("revitPid") == PID, "the view, the release and the Revit arrive")
    check(message.get("selection") == 1 and message.get("selected") == 6,
          "the selection's number and the true count arrive - not the list")
    check(message.get("categories") == [{"name": "Ducts", "count": 3},
                                         {"name": "(no category)", "count": 1},
                                         {"name": "Duct Fittings", "count": 1}],
          "the categories arrive most first: %s" % message.get("categories"))

    content, meta = talk.channel_event(message)
    check(content.startswith("resize these to 400x300")
          and 'model "Tower "A" \\ MEP"' in content
          and "Selection 1 saved: 6 elements" in content,
          "and read as the channel event the chat will see")
    check(meta.get("revit") == str(PID) and meta.get("selection") == "1", "routed by meta: %s" % meta)

    listed = talk.selection_answer(1, folder_root=root)
    check("1491300 | u-4 | Duct Fittings | Elbow \u00D8200 \u2013 90\u00B0" in listed,
          "a name that is not ASCII survives in the saved list")
    check("77 | u-5 | (no category) |" in listed, "an element with no category is listed as such")
    check("1 element could not be read" in listed and "Only the first" not in listed,
          "one that could not be read is counted, and is not mistaken for a list cut short")

    print()
    print("4. Nothing selected, and more than was saved")
    host.step("send", "--text", "and this", "--empty")
    quiet = next((m for m in [talk.claim(p) for _, n, p in talk.pending(root) if n == 2]), None) or {}
    check(quiet.get("selection") is None and quiet.get("selected") == 0,
          "nothing selected reads back as no selection: %s / %s"
          % (quiet.get("selection"), quiet.get("selected")))
    check("(Nothing was selected.)" in talk.channel_event(quiet)[0], "and the chat is told so")
    check(not os.path.exists(os.path.join(root, str(PID), "selection-2.json")),
          "and no empty selection file is left for it")

    host.step("send", "--text", "all of these", "--truncated")
    cut = talk.selection_answer(3, folder_root=root)
    check("Only the first 5 were saved" in cut and "8 selected" in cut,
          "more selected than saved reads back as a list cut short")

    print()
    print("5. Numbers, the short memory, and forgetting")
    fourth = host.step("send", "--text", "again")
    check(fourth.get("message") == "4",
          "a claimed message still holds its number - the next is 4: %s" % fourth.get("message"))
    last = None
    for _ in range(20):
        last = host.step("send", "--text", "more")
    check(last.get("message") == "24", "twenty more take it to 24: %s" % last.get("message"))
    folder = os.path.join(root, str(PID))
    names = sorted(os.listdir(folder))
    numbers = sorted(set(int(n.split("-")[1].split(".")[0]) for n in names
                         if n.startswith(("message-", "selection-"))))
    check(numbers == list(range(5, 25)),
          "the last 20 are kept and the older ones forgotten: %s..%s"
          % (numbers[0] if numbers else None, numbers[-1] if numbers else None))

    described = host.step("describe")
    check(described.get("describe") == 'Tower "A" \\ MEP  \u00B7  Level 1 - Mech  \u00B7  6 selected: '
                                       'Ducts 3, (no category) 1, Duct Fittings 1',
          "the Talk window's line names the model, view and selection: %s" % described.get("describe"))
    check(described.get("empty") == "Project1  \u00B7  Level 1  \u00B7  nothing selected",
          "and says so when nothing is selected")

    if posix:
        gone = host.step("forget")
        check(gone.get("exists") == "false" and not os.path.exists(folder),
              "closing Revit forgets everything it said")
    else:
        print("  --    NOT RUN on Windows: forgetting is refused outside Heron's own folder, by design")

    print()
    if FAILURES:
        print("FAILED")
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("PASSED - what the Talk button writes is what the chat reads.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
