# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-API-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The API Documentation Agent - every MCP tool, from its own signature.

    python tools/generate-api-docs.py
    HERON_API_DOCS_OUT=somewhere.html python tools/generate-api-docs.py

docs/28: `HERON-DOC-API-001` API Documentation Agent - "Generated from
tool schemas." A Heron tool's schema is not a JSON file anywhere: it is
the decorated function's SIGNATURE, which the MCP SDK turns into one at
run time. So that is what this reads.

IT PARSES, IT DOES NOT IMPORT
-------------------------------
Importing `heron_mcp_server` needs the MCP SDK installed and defines
eighteen tools as a side effect of asking what they are. A documentation
tool that only works on a machine which can already run the server is
useless exactly where documentation is wanted - on a reviewer's laptop,
in CI, on a checkout with no dependencies. `ast` needs nothing.

THE CONCLUSION IT CARRIES: A PARAMETER NOTHING EXPLAINS
---------------------------------------------------------
A tool's docstring can describe the tool beautifully and never mention
its arguments. A caller then reads `depth: int = 0` off the schema and
has to guess. That gap is invisible in the source, invisible in the
registry, and it is the whole thing an API reference exists to close -
so every parameter is checked against its own tool's docstring, on a
word boundary, and the ones nothing explains are named.

Two were unexplained the first time this ran, against eighteen tools
that all have docstrings.

RISK AND OPERATION ARE ASKED FOR, NOT COPIED
----------------------------------------------
`mcp/server/heron_tools.py` owns what a tool may do, and it raises
rather than defaulting for a tool nobody declared. This page calls it.
A tool decorated but not declared, or declared but not decorated, is
REPORTED rather than dropped - a page quietly missing a tool is worse
than a page saying it cannot classify one.
"""

import ast
import datetime
import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_tools as TOOLS                                   # noqa: E402

OUT = os.environ.get("HERON_API_DOCS_OUT", "api-reference.html")
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")


def decorated(tree):
    """Every `@server.tool()` function, in the order the file declares them."""
    found = []
    for node in tree.body:
        if not isinstance(node, ast.FunctionDef):
            continue
        for mark in node.decorator_list:
            call = mark.func if isinstance(mark, ast.Call) else mark
            if getattr(call, "attr", None) == "tool":
                found.append(node)
                break
    return found


def explains(doc, name):
    """
    Does this docstring mention this parameter?

    ON A WORD BOUNDARY. `full` is not explained by the word "fully", and
    `path` is not explained by "pathological" - a substring match would
    call both of those documented and the gap would stay invisible,
    which is the one thing this page exists to prevent.

    Strictly, which means a docstring saying "requests are queued" does
    NOT explain a parameter called `request`. That is a false positive,
    and it is the right direction to be wrong in: naming a parameter
    that is arguably documented costs a reader one glance, and missing
    one that is not hides exactly what this page is for.
    """
    return bool(re.search(r"\b%s\b" % re.escape(name), doc or ""))


def signature(node):
    """The parameters, with their annotation and default if they have one."""
    args = node.args.args
    fills = list(node.args.defaults)
    first = len(args) - len(fills)
    out = []
    for index, arg in enumerate(args):
        default = None
        if index >= first:
            try:
                default = ast.unparse(fills[index - first])
            except Exception:                            # pragma: no cover
                default = "?"
        out.append({
            "name": arg.arg,
            "type": ast.unparse(arg.annotation) if arg.annotation else "",
            "default": default})
    return out


def collect():
    tree = ast.parse(io.open(SERVER, encoding="utf-8").read())
    served = decorated(tree)
    rows, notes = [], []

    for node in served:
        doc = ast.get_docstring(node) or ""
        params = signature(node)
        for one in params:
            one["explained"] = explains(doc, one["name"])

        try:
            risk = TOOLS.NAMES[TOOLS.risk_of(node.name)]
            operation = TOOLS.operation_of(node.name)
        except TOOLS.NotDeclared:
            # REPORTED, NOT DROPPED. A page quietly missing a tool is
            # worse than a page saying it cannot classify one.
            risk, operation = None, None
            notes.append("%s is served but not declared in heron_tools.TOOLS, "
                         "so nothing here knows what it may do" % node.name)

        rows.append({
            "name": node.name,
            "risk": risk,
            "operation": operation,
            "changes_model": bool(risk) and TOOLS.writes(node.name),
            "returns": ast.unparse(node.returns) if node.returns else "",
            "summary": " ".join(doc.strip().split("\n\n")[0].split()),
            "doc": doc.strip(),
            "params": params,
            "silent": [one["name"] for one in params if not one["explained"]],
            "line": node.lineno,
        })

    absent = sorted(set(TOOLS.TOOLS) - set(one["name"] for one in rows))
    for name in absent:
        notes.append("%s is declared in heron_tools.TOOLS but no function in "
                     "the server carries @server.tool() for it" % name)
    return rows, notes


HTML = u"""<title>Heron MCP API Reference</title>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<style>
:root{
  --ink:#16232B; --muted:#66767C; --ground:#EFF2F1; --card:#FAFBFA;
  --rule:#C6D0D1; --accent:#1D5C68; --warn:#A9660F; --bad:#9E3B33;
  --safe:#3D7A55;
}
@media (prefers-color-scheme:dark){:root{
  --ink:#DFE7E8; --muted:#84969B; --ground:#0F1719; --card:#161F22;
  --rule:#2B3A3E; --accent:#63BAC5; --warn:#DFA246; --bad:#E0796F;
  --safe:#71B98A;}}
*{box-sizing:border-box}
body{margin:0;background:var(--ground);color:var(--ink);
     font:14px/1.55 -apple-system,"Segoe UI",system-ui,sans-serif}
header{padding:18px 20px;border-bottom:1px solid var(--rule);background:var(--card)}
h1{margin:0 0 4px;font-size:19px;letter-spacing:-.01em}
.sub{color:var(--muted);font-size:13px}
.bar{display:flex;flex-wrap:wrap;gap:8px;padding:12px 20px;
     border-bottom:1px solid var(--rule);background:var(--card);
     position:sticky;top:0;z-index:5}
input,select{font:inherit;padding:6px 9px;border:1px solid var(--rule);
     border-radius:3px;background:var(--ground);color:var(--ink)}
input{flex:1;min-width:200px}
.notes{margin:0;padding:12px 20px;border-bottom:1px solid var(--rule);
       background:var(--card);color:var(--bad);font-size:13px}
.notes ul{margin:4px 0 0;padding-left:18px}
main{padding:14px 20px 60px;display:grid;gap:10px;
     grid-template-columns:repeat(auto-fill,minmax(360px,1fr))}
.t{border:1px solid var(--rule);border-radius:4px;background:var(--card);
   padding:11px 13px}
.t h2{margin:0;font:13px ui-monospace,Consolas,monospace;letter-spacing:-.005em}
.says{color:var(--muted);font-size:13px;margin:6px 0 8px}
.tags{display:flex;flex-wrap:wrap;gap:5px;margin:4px 0 7px}
.tag{font:10px ui-monospace,Consolas,monospace;letter-spacing:.06em;
   text-transform:uppercase;border:1px solid currentColor;border-radius:2px;
   padding:1px 5px;color:var(--muted)}
.tag.safe{color:var(--safe)} .tag.bad{color:var(--bad)} .tag.warn{color:var(--warn)}
.tag.read{color:var(--accent)}
table{border-collapse:collapse;width:100%;font-size:12.5px;margin-top:4px}
th{text-align:left;font:10px ui-monospace,Consolas,monospace;
   letter-spacing:.06em;text-transform:uppercase;color:var(--muted);
   padding:3px 8px 3px 0;font-weight:400}
td{padding:2px 8px 2px 0;vertical-align:top;border-top:1px solid var(--rule)}
td code{font:11px ui-monospace,Consolas,monospace;color:var(--accent)}
.gap{color:var(--warn)}
details{margin-top:7px;font-size:12.5px}
summary{cursor:pointer;color:var(--muted)}
pre{white-space:pre-wrap;margin:6px 0 0;font:12px ui-monospace,Consolas,monospace;
    color:var(--muted)}
footer{padding:16px 20px;border-top:1px solid var(--rule);color:var(--muted);
       font-size:12.5px;background:var(--card)}
.none{color:var(--muted);padding:24px 20px}
</style>
<header>
  <h1>Heron MCP API Reference</h1>
  <div class="sub" id="sub">loading</div>
</header>
<div class="bar">
  <input id="q" placeholder="search a tool, a parameter, a bridge operation">
  <select id="f">
    <option value="">every tool</option>
    <option value="write">can change the model</option>
    <option value="gap">has a parameter nothing explains</option>
  </select>
</div>
<div class="notes" id="notes" hidden></div>
<main id="out"></main>
<footer id="foot"></footer>
<script>
const DATA = __DATA__;
const rows = DATA.rows;
document.getElementById("sub").textContent = DATA.subtitle;
document.getElementById("foot").textContent = DATA.footer;

function esc(s){ const e = document.createElement("i");
  e.textContent = s == null ? "" : String(s); return e.innerHTML; }

if (DATA.notes.length){
  const n = document.getElementById("notes");
  n.hidden = false;
  n.innerHTML = "<b>Could not be classified:</b><ul>" +
    DATA.notes.map(t => `<li>${esc(t)}</li>`).join("") + "</ul>";
}

function card(r){
  const params = r.params.length ? `<table>
    <tr><th>parameter</th><th>type</th><th>default</th><th>explained</th></tr>
    ${r.params.map(p => `<tr>
      <td><code>${esc(p.name)}</code></td>
      <td>${esc(p.type) || "&mdash;"}</td>
      <td>${p.default === null ? "<i>required</i>" : esc(p.default)}</td>
      <td>${p.explained ? "yes"
            : `<span class="gap">nothing explains it</span>`}</td>
    </tr>`).join("")}</table>` : `<div class="says">Takes nothing.</div>`;
  return `<div class="t">
    <h2>${esc(r.name)}(${r.params.map(p => esc(p.name)).join(", ")})${
      r.returns ? " &rarr; " + esc(r.returns) : ""}</h2>
    <div class="tags">
      <span class="tag ${r.changes_model ? "bad" : "read"}">${
        esc(r.risk || "not declared")}</span>
      ${r.changes_model ? `<span class="tag bad">changes the model</span>`
                        : `<span class="tag safe">cannot change the model</span>`}
      ${r.operation ? `<span class="tag">bridge: ${esc(r.operation)}</span>`
                    : `<span class="tag">reaches no model</span>`}
      ${r.silent.length ? `<span class="tag warn">${r.silent.length} parameter${
        r.silent.length > 1 ? "s" : ""} unexplained</span>` : ""}
    </div>
    <p class="says">${esc(r.summary)}</p>
    ${params}
    <details><summary>the whole docstring</summary><pre>${esc(r.doc)}</pre></details>
  </div>`;
}

function draw(){
  const q = document.getElementById("q").value.toLowerCase().trim();
  const want = document.getElementById("f").value;
  const shown = rows.filter(r => {
    if (want === "write" && !r.changes_model) return false;
    if (want === "gap" && !r.silent.length) return false;
    if (!q) return true;
    return (r.name + " " + r.summary + " " + (r.operation || "") + " " +
            r.params.map(p => p.name).join(" ")).toLowerCase().includes(q);
  });
  document.getElementById("out").innerHTML = shown.length
    ? shown.map(card).join("") : `<div class="none">Nothing matches.</div>`;
}
["q", "f"].forEach(id =>
  document.getElementById(id).addEventListener("input", draw));
draw();
</script>
"""


def main():
    rows, notes = collect()
    writers = [one for one in rows if one["changes_model"]]
    gaps = [one for one in rows if one["silent"]]
    params = sum(len(one["params"]) for one in rows)
    silent = sum(len(one["silent"]) for one in rows)

    payload = {
        "rows": rows, "notes": notes,
        "subtitle": (
            "%d tool(s) and %d parameter(s), parsed from "
            "mcp/server/heron_mcp_server.py at %s. %d can change the model. "
            "%d parameter(s) are named by no docstring."
            % (len(rows), params,
               datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
               len(writers), silent)),
        "footer": (
            "Generated by tools/generate-api-docs.py. The schema is read "
            "from each decorated function's SIGNATURE - parsed, never "
            "imported, so this works on a checkout with no MCP SDK "
            "installed. Risk and bridge operation are asked of "
            "mcp/server/heron_tools.py rather than copied. A parameter is "
            "'explained' only when its own tool's docstring names it on a "
            "word boundary. Re-run rather than edit."),
    }

    text = HTML.replace("__DATA__", json.dumps(payload, sort_keys=True))
    io.open(OUT, "w", encoding="utf-8").write(text)

    def w(line):
        sys.stdout.write(line.encode("ascii", "replace").decode("ascii"))

    w("%d tool(s), %d parameter(s), %d that change the model\n"
      % (len(rows), params, len(writers)))
    for one in gaps:
        w("  %s: nothing explains %s\n"
          % (one["name"], ", ".join(one["silent"])))
    for line in notes:
        w("  UNCLASSIFIED: %s\n" % line)
    w("wrote %s (%d KB)\n" % (OUT, len(text) // 1024))
    return 0


if __name__ == "__main__":
    sys.exit(main())
