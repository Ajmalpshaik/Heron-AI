#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Heron's MCP server over real stdio, in front of a Revit that takes its time.

    python tests/support/slow_revit_server.py      # what test_mcp_stdio.py starts

Not a suite - test_mcp_stdio.py starts it as a child, the way a host starts
the real server. It imports heron_mcp_server as a module, so the off-Windows
refusal under its `__main__` never runs, and serves it unchanged over stdio.

ONE THING IS A STAND-IN: the Revit. Every session this chat binds is a
SlowRevit - the client's own Bridge with its pipe swapped for one that answers
after HERON_TEST_SLOW_REVIT_S seconds (90 unless set). Everything above the
pipe is the real code: Bridge.request, the join loop in _read_line that calls
`on_wait`, the tool, and the SDK that turns each call into a progress
notification. So what the host reads is what a host would read from a Revit
that was slow for any reason - a slow fragment, a family build, a modeller
answering a prompt in Revit.

No named pipe is opened and no discovery folder is read; test_mcp_stdio.py
installs tests/support/live_revit_guard.py in this child as well, and fails
the run if either happened.
"""

import json
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_bridge_client as bridge                 # noqa: E402
import heron_mcp_server as SERVER                    # noqa: E402

SECONDS = float(os.environ.get("HERON_TEST_SLOW_REVIT_S") or 90)

# A good answer to any fragment run: the model it ran in, and nothing left.
REPLY = {"ok": True, "document": "Heron slow stand-in", "projectKey": "heron-slow-stand-in",
         "provides": {}}


class SlowPipe(object):
    """The client's end of a pipe whose Revit answers after SECONDS."""

    def __init__(self):
        self.sent = []

    def write(self, data):
        self.sent.append(data)
        return len(data)

    def read(self, _size):
        time.sleep(SECONDS)
        return (json.dumps(REPLY) + "\n").encode("utf-8")

    def close(self):
        pass


class SlowRevit(bridge.Bridge):
    """The client's own Bridge, with only the pipe replaced."""

    def __init__(self):
        super(SlowRevit, self).__init__(
            {"pid": 4242, "pipeName": "heron-slow-stand-in", "revitVersion": "2024",
             "protocolVersion": bridge.PROTOCOL_VERSION, "token": "stand-in"}, None)

    def _connect(self, timeout):
        return SlowPipe()


SERVER.binding.resolve = SlowRevit

if __name__ == "__main__":
    SERVER.server.run()
