# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The network a Revit sprinkler system gives for a hydraulic calculation -
docs/46 section 4.

Reads REPORT_SPRINKLER_NETWORK's JSON (format 1, defined once in
docs/work-notes/plans/sprinkler-panel-2026-10-04.md), checks it (gate 1), and
turns it into the nodes and pipes heron_fire's `hydraulic` solves. It adds no
physics: connectors Revit says are joined become one node, a pipe becomes an
edge, and a fitting's equivalent length - the modeller's, from the table they
confirm - is put on the smallest pipe that meets it.

Nothing is defaulted. A pipe with no inside diameter, a sprinkler type with no
confirmed K-factor, a network in two pieces: each is a FAIL, named, and the
solve waits. A K-factor is never taken from Revit's stored value - the unit it
is stored in is not one Heron can rely on, so the modeller confirms each
type's K once (docs/46 section 3.1).

Every number carries its unit in its key name. Standard library and
heron_fire's own tables only.
"""

import hashlib
import json

import heron_fire as FIRE

FORMAT = 1
#: The solver's own limits (heron_fire.calc_hydraulic) - checked here first, so
#: the modeller is told before anything is asked.
MAX_NODES = 400
MAX_PIPES = 600
#: Element kinds that are one node each, and which names a merged node first.
NODE_KINDS = ("sprinkler", "equipment", "accessory", "fitting", "other")
#: Kinds a fitting row is never made for - a cap closes a dead end, no water
#: flows through it.
NO_ROW = ("cap",)
TEE = "tee or cross, flow turned 90 degrees"


class NetworkError(ValueError):
    """The network cannot be read at all - wrong format, or not JSON."""


class Network(object):
    """One system's network, as read - nothing in it is repaired."""

    def __init__(self, d):
        self.document = d.get("document")
        self.systems = list(d.get("systems") or [])
        self.system = d.get("system")
        self.elements = list(d.get("elements") or [])
        self.findings = list(d.get("findings") or [])
        self.by_id = dict((str(e.get("id")), e) for e in self.elements)
        self.raw = d


def read(raw):
    """The network, whole - or NetworkError. Never half of one."""
    if isinstance(raw, Network):
        return raw
    if isinstance(raw, str):
        try:
            raw = json.loads(raw)
        except ValueError as why:
            raise NetworkError("the network is not JSON: %s" % why)
    if not isinstance(raw, dict) or raw.get("format") != FORMAT:
        raise NetworkError("the network is not format %d - read the model again with "
                           "REPORT_SPRINKLER_NETWORK" % FORMAT)
    for key in ("systems", "system", "elements"):
        if key not in raw:
            raise NetworkError("the network has no %r" % key)
    for e in raw.get("elements") or []:
        if not isinstance(e, dict) or e.get("id") is None:
            raise NetworkError("an element of the network has no id")
    return Network(raw)


def _label(e):
    names = [x for x in (e.get("family"), e.get("type")) if x]
    return "%s (%s)" % (": ".join(names) or e.get("category") or e.get("kind"), e.get("id"))


def nominal_label(metres):
    """
    A Revit nominal diameter as its standard size - "DN25" - snapped to the
    nearest standard nominal within 1 mm, read as DN or as inches x 25.4, so a
    model held in inches (1 in = 25.4 mm) reads the same as one held in DN.
    A size that is neither is "DN<mm> (not a standard size)"; None when the
    model holds none.
    """
    if metres is None:
        return None
    mm = float(metres) * 1000.0
    best = None
    for _label_in, dn, inches in FIRE.NOMINALS:
        for target in (float(dn), inches * 25.4):
            gap = abs(mm - target)
            if gap <= 1.0 and (best is None or gap < best[0]):
                best = (gap, dn)
    if best is None:
        return "DN%d (not a standard size)" % int(round(mm))
    return "DN%d" % best[1]


def _size_key(e):
    return None if e is None else nominal_label(e.get("nominal_diameter_m"))


# --- sprinkler types ---------------------------------------------------------

def type_key(e):
    """A sprinkler type's key: its type id, else family|type."""
    if e.get("type_id"):
        return str(e["type_id"])
    return "%s|%s" % (e.get("family") or "", e.get("type") or "")


def sprinkler_types(n):
    """{type_key: {"name", "heads", "connector", "parameter"}} - what Revit holds, as text."""
    out = {}
    for e in read(n).elements:
        if e.get("kind") != "sprinkler":
            continue
        k = e.get("k") or {}
        row = out.setdefault(type_key(e), {
            "name": ": ".join(x for x in (e.get("family"), e.get("type")) if x) or "(unnamed)",
            "heads": [], "connector": k.get("connector"), "parameter": k.get("parameter")})
        row["heads"].append(str(e["id"]))
    return out


# --- joining connectors into nodes -------------------------------------------

def _kind_rank(kind):
    return NODE_KINDS.index(kind) if kind in NODE_KINDS else len(NODE_KINDS)


def _point(c):
    p = c.get("point")
    if isinstance(p, (list, tuple)) and len(p) == 3:
        return [float(x) for x in p]
    return None


def _dist(a, b):
    return sum((a[i] - b[i]) ** 2 for i in range(3)) ** 0.5


class _Graph(object):
    """The network joined: nodes, pipe segments and what was noticed on the way."""

    def __init__(self, n):
        self.n = n
        self.node_of = {}        # "eid:cid" -> node id
        self.records = {}        # node id -> {"id", "elevation_m", "at", "elements", "open"}
        self.segments = []       # {"id", "e", "a", "b", "length_m", "added_m"}
        self.notes = []          # sentences, said on the sheet
        self.outside = []        # (element id, connector id, the element it names)
        self._build()

    def _build(self):
        n = self.n
        parent = {}

        def top(i):
            while parent[i] != i:
                parent[i] = parent[parent[i]]
                i = parent[i]
            return i

        def join(a, b):
            ra, rb = top(a), top(b)
            if ra != rb:
                parent[max(ra, rb)] = min(ra, rb)

        points = {}
        for e in n.elements:
            for c in e.get("connectors") or []:
                key = "%s:%s" % (e["id"], c.get("id"))
                parent[key] = key
                points[key] = _point(c)
        for e in n.elements:
            conns = e.get("connectors") or []
            keys = ["%s:%s" % (e["id"], c.get("id")) for c in conns]
            if e.get("kind") != "pipe":
                for k in keys[1:]:
                    join(keys[0], k)
            for c, key in zip(conns, keys):
                for other in c.get("to") or []:
                    if not isinstance(other, (list, tuple)) or len(other) != 2:
                        continue
                    okey = "%s:%s" % (other[0], other[1])
                    if okey in parent:
                        join(key, okey)
                    elif str(other[0]) not in n.by_id:
                        self.outside.append((str(e["id"]), c.get("id"), str(other[0])))
        groups = {}
        for key in parent:
            groups.setdefault(top(key), []).append(key)
        for keys in groups.values():
            owners = sorted(set(k.split(":")[0] for k in keys))
            solid = [o for o in owners if (n.by_id.get(o) or {}).get("kind") != "pipe"]
            # The merged node's id: a sprinkler, then equipment, then the rest -
            # then the smallest id, so the same model always names it the same.
            solid.sort(key=lambda o: (_kind_rank(n.by_id[o].get("kind")), o))
            if solid:
                nid = solid[0]
            elif len(keys) == 1:
                pipe, cid = keys[0].split(":")
                nid = "e%s.%s" % (pipe, cid)
            else:
                first = sorted(keys)[0].split(":")
                nid = "j%s.%s" % (first[0], first[1])
            got = [points[k] for k in keys if points.get(k)]
            xyz = [round(sum(p[i] for p in got) / len(got), 3) for i in range(3)] if got else None
            self.records[nid] = {"id": nid, "elevation_m": xyz[2] if xyz else None, "at": xyz,
                                 "elements": solid, "open": not solid and len(keys) == 1}
            for k in keys:
                self.node_of[k] = nid
        seg_of = {}
        for e in n.elements:
            if e.get("kind") == "pipe":
                self._pipe(e, points, seg_of)
        self._centres(points, seg_of)

    def _pipe(self, e, points, seg_of):
        """A pipe is one segment between its two ends - or, with a tap or a spud joined
        part-way along it (a Curve connector), one segment between each point in turn,
        its length shared by where each point sits."""
        conns = e.get("connectors") or []
        ends = [c for c in conns if (c.get("type") or "end") == "end"]
        along = [c for c in conns if c.get("type") == "curve"]
        if len(ends) != 2:
            self.notes.append("pipe %s has %d end connectors, not 2 - it is left out"
                              % (e["id"], len(ends)))
            return
        a, b = _point(ends[0]), _point(ends[1])
        chain = [ends[0]]
        if along:
            if a is None or b is None or _dist(a, b) <= 1e-9:
                self.notes.append("pipe %s has a tap along it but its ends could not be placed "
                                  "- it is left out" % e["id"])
                return
            span = _dist(a, b)
            axis = [(b[i] - a[i]) / span for i in range(3)]
            placed = [c for c in along if _point(c)]
            placed.sort(key=lambda c: sum((_point(c)[i] - a[i]) * axis[i] for i in range(3)))
            chain += placed
        chain.append(ends[1])
        total = e.get("length_m")
        straight = sum(_dist(_point(x), _point(y)) for x, y in zip(chain, chain[1:])
                       if _point(x) and _point(y)) if len(chain) > 2 else None
        for i, (x, y) in enumerate(zip(chain, chain[1:])):
            na = self.node_of["%s:%s" % (e["id"], x.get("id"))]
            nb = self.node_of["%s:%s" % (e["id"], y.get("id"))]
            if na == nb:
                if len(chain) == 2:
                    self.notes.append("pipe %s starts and ends at the same point - it is left out"
                                      % e["id"])
                continue
            if total is None:
                length = None
            elif straight:
                length = float(total) * _dist(_point(x), _point(y)) / straight
            else:
                length = float(total)
            seg = {"id": str(e["id"]) if len(chain) == 2 else "%s/%d" % (e["id"], i + 1),
                   "e": e, "a": na, "b": nb, "length_m": length, "added_m": 0.0}
            self.segments.append(seg)
            for c in (x, y):
                seg_of["%s:%s" % (e["id"], c.get("id"))] = seg

    def _centres(self, points, seg_of):
        """
        A pipe's length is cut to cut: it stops at the fitting's connector. The
        fitting's own centre-to-end length is added to the pipe that meets it
        there, from the element's location point - leaving it out would err
        small (docs/46 s4).
        """
        for e in self.n.elements:
            if e.get("kind") == "pipe" or not isinstance(e.get("at"), (list, tuple)):
                continue
            centre = [float(x) for x in e["at"]]
            for c in e.get("connectors") or []:
                here = _point(c)
                if here is None:
                    continue
                for other in c.get("to") or []:
                    seg = seg_of.get("%s:%s" % (other[0], other[1])) \
                        if isinstance(other, (list, tuple)) and len(other) == 2 else None
                    if seg is not None and seg["length_m"] is not None:
                        seg["added_m"] += _dist(here, centre)

    def meeting(self):
        """{node id: [segment, ...]} - the pipe segments that meet each node."""
        out = {}
        for s in self.segments:
            out.setdefault(s["a"], []).append(s)
            out.setdefault(s["b"], []).append(s)
        return out

    def node_of_element(self, e):
        conns = e.get("connectors") or []
        return self.node_of.get("%s:%s" % (e["id"], conns[0].get("id"))) if conns else None


def _graph(n):
    return _Graph(read(n))


# --- fittings and valves -----------------------------------------------------

def _accessory_kind(e):
    text = " ".join(str(x) for x in (e.get("family"), e.get("type")) if x)
    for raw in (e.get("type"), e.get("family"), text):
        got = FIRE.fitting_key(raw)
        if got:
            return got
    low = " %s " % " ".join(text.lower().replace("-", " ").replace("_", " ").split())
    best = None
    for key, words in FIRE._FITTINGS.items():
        for w in words:
            if " %s " % w in low and (best is None or len(w) > len(best[1])):
                best = (key, w)
    if best:
        return best[0]
    return "valve or device: %s" % (text or e.get("category") or "unnamed")


#: Revit's PartType names (by ToString) that turn the flow 90 degrees into a branch.
BRANCHES = ("tee", "cross", "lateraltee", "lateralcross", "wye", "pants")


def fitting_kind(e):
    """What a fitting or accessory counts as, in the chart's words where it has them."""
    if e.get("kind") == "accessory":
        return _accessory_kind(e)
    raw = (e.get("part_type") or "").strip()
    part = raw.lower()
    if part == "elbow":
        angle = e.get("angle_deg")
        if angle is not None and abs(float(angle) - 90.0) <= 5.0:
            return "90 degree standard elbow"
        if angle is not None and abs(float(angle) - 45.0) <= 5.0:
            return "45 degree elbow"
        return "elbow, %s" % ("%g degrees" % round(float(angle)) if angle is not None
                              else "angle not read")
    if part in BRANCHES or part.startswith("tap") or part.startswith("spud"):
        return TEE
    if part in ("cap", "endcap"):
        return "cap"
    return "fitting: %s" % (raw or e.get("type") or "unnamed")


def _smallest(segments):
    """The smallest-bore segment of those meeting a node; ties to the larger element id."""
    ok = [s for s in segments if s["e"].get("inner_diameter_m")]
    if not ok:
        return None

    def order(s):
        eid = str(s["e"]["id"])
        return (float(s["e"]["inner_diameter_m"]), -int(eid) if eid.isdigit() else 0, s["id"])
    return sorted(ok, key=order)[0]


def fitting_rows(n):
    """
    One row per fitting kind and size: {key: {"kind", "size", "count", "elements",
    "chart"}}. The size is the smallest pipe meeting it. "chart" is the chart's
    row (kind, inch label, ft, m) where heron_fire holds one - for the runner
    to offer, adjusted to the pipe; never applied.
    """
    g = _graph(n)
    meeting = g.meeting()
    out = {}
    for e in g.n.elements:
        if e.get("kind") not in ("fitting", "accessory"):
            continue
        kind = fitting_kind(e)
        if kind in NO_ROW:
            continue
        nid = g.node_of_element(e)
        small = _smallest(meeting.get(nid, [])) if nid else None
        size = _size_key(small["e"]) if small else None
        key = "%s|%s" % (kind, size or "size not read")
        row = out.setdefault(key, {"kind": kind, "size": size, "count": 0, "elements": [],
                                   "bore_mm": round(float(small["e"]["inner_diameter_m"])
                                                    * 1000.0, 3) if small else None,
                                   "chart": chart_row(kind, size)})
        row["count"] += 1
        row["elements"].append(str(e["id"]))
    return out


def chart_row(kind, size):
    """The chart's row for this kind and size - (kind, inch label, ft, m) - or None."""
    if not size or "not a standard" in size:
        return None
    label = FIRE.nominal_key(size)
    for row in FIRE.REFERENCES["equivalent_lengths"]["rows"]:
        if row[0] == kind and row[1] == label:
            return row
    return None


# --- the solver's network ----------------------------------------------------

def network(n, k_by_type, eq_by_row, operating, source):
    """
    (nodes, pipes, notes) in heron_fire `hydraulic`'s own input shape - nodes
    {id, elevation_m[, k_lpm_bar]}, pipes {from, to, length_m, bore_mm,
    equivalent_length_m, element}. Only the heads in `operating` carry a K;
    every other node is a plain junction and flows nothing. Raises
    NetworkError for what qa() calls FAIL - the caller checks qa() first.
    """
    g = _graph(n)
    operating = set(str(x) for x in operating or [])
    k_by_type = dict((str(k), v) for k, v in (k_by_type or {}).items())
    eq_by_row = dict(eq_by_row or {})
    meeting = g.meeting()
    notes = list(g.notes)
    extra = {}
    for key, row in fitting_rows(g.n).items():
        length = eq_by_row.get(key)
        if length is None:
            raise NetworkError("no equivalent length for %s at %s" % (
                row["kind"], row["size"] or "a size not read"))
        if not float(length):
            continue
        for eid in row["elements"]:
            e = g.n.by_id[eid]
            nid = g.node_of_element(e)
            small = _smallest(meeting.get(nid, [])) if nid else None
            if small is None:
                notes.append("%s meets no pipe with a bore, so its equivalent length is not "
                             "counted" % _label(e))
                continue
            extra[small["id"]] = extra.get(small["id"], 0.0) + float(length)
    nodes = []
    for nid, rec in sorted(g.records.items()):
        if rec["elevation_m"] is None:
            raise NetworkError("node %s has no position" % nid)
        node = {"id": nid, "elevation_m": rec["elevation_m"]}
        heads = [x for x in rec["elements"] if (g.n.by_id.get(x) or {}).get("kind")
                 == "sprinkler"]
        if len(heads) > 1:
            raise NetworkError("sprinklers %s meet at one point" % ", ".join(heads))
        if heads and heads[0] in operating:
            k = k_by_type.get(type_key(g.n.by_id[heads[0]]))
            if k is None:
                raise NetworkError("sprinkler %s has no confirmed K-factor" % heads[0])
            node["k_lpm_bar"] = float(k)
        nodes.append(node)
    out = []
    added = 0.0
    for s in g.segments:
        e = s["e"]
        if not e.get("inner_diameter_m") or not s["length_m"]:
            raise NetworkError("pipe %s has no inside diameter or length" % e["id"])
        added += s["added_m"]
        out.append({"from": s["a"], "to": s["b"],
                    "length_m": round(s["length_m"] + s["added_m"], 4),
                    "bore_mm": round(float(e["inner_diameter_m"]) * 1000.0, 3),
                    "equivalent_length_m": round(extra.get(s["id"], 0.0), 4),
                    "element": str(e["id"]), "segment": s["id"]})
    if added:
        notes.append("%.2f m of fitting length, centre to end, is added to the pipes - Revit "
                     "measures a pipe cut to cut" % added)
    return nodes, out, notes


def head_nodes(n):
    """{sprinkler element id: its node id}."""
    g = _graph(n)
    return dict((str(e["id"]), g.node_of_element(e)) for e in g.n.elements
                if e.get("kind") == "sprinkler" and e.get("connectors"))


def graph(n):
    """(node records, segments) - for the view and the remote-area ranking. Each segment
    {"id", "e" (the pipe element), "a", "b", "length_m", "added_m"}."""
    g = _graph(n)
    return g.records, g.segments


def source_candidates(n):
    """Where the demand may be reported: the system's base equipment, and every open pipe end."""
    g = _graph(n)
    out = []
    base = str((g.n.system or {}).get("base_equipment") or "")
    if base and base in g.n.by_id:
        nid = g.node_of_element(g.n.by_id[base])
        if nid:
            out.append({"id": nid, "what": "the system's base equipment, %s"
                        % _label(g.n.by_id[base]), "at": g.records[nid]["at"]})
    for nid, rec in sorted(g.records.items()):
        if rec["open"]:
            pipe = g.n.by_id.get(nid[1:].split(".")[0]) or {}
            out.append({"id": nid, "what": "an open end of pipe %s, %s" % (
                pipe.get("id"), _size_key(pipe) or "size not read"), "at": rec["at"]})
    return out


def _pieces(records, segments):
    """The node ids of each connected piece, largest first."""
    touching = dict((nid, []) for nid in records)
    for s in segments:
        touching[s["a"]].append(s["b"])
        touching[s["b"]].append(s["a"])
    seen = set()
    out = []
    for start in sorted(records):
        if start in seen:
            continue
        piece, todo = [], [start]
        seen.add(start)
        while todo:
            here = todo.pop()
            piece.append(here)
            for other in touching[here]:
                if other not in seen:
                    seen.add(other)
                    todo.append(other)
        out.append(piece)
    out.sort(key=len, reverse=True)
    return out


def prune(nodes, pipes, keep):
    """
    (nodes, pipes, removed) - the dead-end branches taken away, leaf by leaf: a
    node met by one pipe that is not in `keep` (the source and the operating
    heads), and that pipe. EXACT, not an approximation: a dead end with no open
    head carries no flow, so it changes no pressure anywhere. It is what lets a
    whole floor fit the solver's limit.
    """
    keep = set(keep)
    alive_nodes = dict((x["id"], x) for x in nodes)
    alive = list(range(len(pipes)))
    degree = dict((nid, 0) for nid in alive_nodes)
    for i in alive:
        degree[pipes[i]["from"]] += 1
        degree[pipes[i]["to"]] += 1
    removed = 0
    changed = True
    while changed:
        changed = False
        still = []
        for i in alive:
            p = pipes[i]
            leaf = None
            for end in (p["from"], p["to"]):
                if degree[end] == 1 and end not in keep:
                    leaf = end
            if leaf is None:
                still.append(i)
                continue
            degree[p["from"]] -= 1
            degree[p["to"]] -= 1
            alive_nodes.pop(leaf, None)
            removed += 1
            changed = True
        alive = still
    for nid in [k for k, d in degree.items() if d == 0 and k not in keep]:
        alive_nodes.pop(nid, None)
    return ([x for x in nodes if x["id"] in alive_nodes], [pipes[i] for i in alive], removed)


def qa(n, k_by_type=None, source=None):
    """Gate 1 - what is wrong, or worth saying, before anything is solved. FAIL first."""
    n = read(n)
    found = []

    def add(level, text, element=None):
        found.append({"level": level, "element": element, "text": text})

    if n.system is None:
        names = ", ".join(s.get("name") or "?" for s in n.systems)
        add("FAIL", "no sprinkler system was chosen - %s" % (
            "say which: %s" % names if names else "the model has no fire protection piping "
            "system"))
        return found
    if not n.elements:
        add("FAIL", "the system %s holds no elements" % (n.system.get("name") or ""))
        return found
    g = _Graph(n)
    k_by_type = dict((str(k), v) for k, v in (k_by_type or {}).items())
    for e in n.elements:
        kind = e.get("kind")
        if kind == "pipe":
            if not e.get("inner_diameter_m"):
                add("FAIL", "pipe %s has no inside diameter in the model - check its pipe "
                    "type's segment and size" % _label(e), e["id"])
            if not e.get("length_m"):
                add("FAIL", "pipe %s has no length" % _label(e), e["id"])
            size = _size_key(e)
            if size and "not a standard" in size:
                add("WARN", "pipe %s is %s - its fittings' chart rows cannot be matched"
                    % (_label(e), size), e["id"])
        if kind == "sprinkler":
            conns = e.get("connectors") or []
            if not conns or not any(c.get("to") for c in conns):
                add("WARN", "sprinkler %s is connected to no pipe - it is not part of the "
                    "water network" % _label(e), e["id"])
        if kind == "equipment" and len(e.get("connectors") or []) > 1:
            add("WARN", "%s has %d connectors in this system - water through it is counted "
                "with no loss; a pump's own rise is not in this calculation"
                % (_label(e), len(e["connectors"])), e["id"])
        if kind == "other":
            add("WARN", "%s is in the system but is not a pipe, fitting, valve, sprinkler or "
                "equipment - it is counted as a joint" % _label(e), e["id"])
    for nid, rec in g.records.items():
        heads = [x for x in rec["elements"] if (n.by_id.get(x) or {}).get("kind") == "sprinkler"]
        if len(heads) > 1:
            add("FAIL", "sprinklers %s meet at one point with no pipe between - each head "
                "needs its own outlet" % ", ".join(heads))
    meeting = g.meeting()
    for e in n.elements:
        if e.get("kind") in ("fitting", "accessory") and not meeting.get(g.node_of_element(e)):
            add("WARN", "%s meets no pipe - its equivalent length is not counted" % _label(e),
                e["id"])
    for eid, cid, other in g.outside:
        add("WARN", "%s is joined to element %s, which is not in this system - that end is "
            "treated as open" % (_label(n.by_id[eid]), other), eid)
    for key, row in sprinkler_types(n).items():
        if k_by_type.get(key) is None:
            add("FAIL", "sprinkler type %s (%d heads) has no confirmed K-factor - confirm it "
                "on the panel" % (row["name"], len(row["heads"])))
    for text in g.notes:
        add("WARN", text)
    for row in fitting_rows(n).values():
        if row["kind"].startswith(("valve or device:", "fitting:", "elbow, ")):
            add("WARN", "%d x %s is not a kind the chart names - its equivalent length is "
                "asked" % (row["count"], row["kind"]))
    opens = [nid for nid, rec in g.records.items() if rec["open"] and nid != source]
    if opens:
        add("WARN", "%d open pipe end(s) - a break, or an end with no cap - %s" % (
            len(opens), ", ".join(sorted(opens)[:8])))
    if source is None and not (n.system or {}).get("base_equipment"):
        add("FAIL", "the system has no base equipment, so where the demand is reported is "
            "not known - choose the source on the panel")
    elif source is not None and source not in g.records:
        add("FAIL", "the source %s is not a point of this network" % source)
    pieces = _pieces(g.records, g.segments)
    if len(pieces) > 1:
        add("FAIL", "the network is in %d pieces that do not meet - %d point(s) are not "
            "joined to the main one; connect them, or leave them out of the system"
            % (len(pieces), sum(len(p) for p in pieces[1:])))
    if len(g.records) > MAX_NODES or len(g.segments) > MAX_PIPES:
        add("INFO", "%d points and %d pipes - more than the %d and %d Heron solves at once; "
            "the branches with no head in the remote area are left out of the solve (they "
            "carry no water), and only if it is still too big is it refused"
            % (len(g.records), len(g.segments), MAX_NODES, MAX_PIPES))
    counts = {}
    for e in n.elements:
        counts[e.get("kind")] = counts.get(e.get("kind"), 0) + 1
    add("INFO", "read: %s; %d points once joined" % (
        ", ".join("%d %s" % (v, k) for k, v in sorted(counts.items())), len(g.records)))
    by_size = {}
    for e in n.elements:
        if e.get("kind") == "pipe" and e.get("length_m"):
            size = _size_key(e) or "size not read"
            by_size[size] = by_size.get(size, 0.0) + float(e["length_m"])
    if by_size:
        add("INFO", "pipe length by size: %s" % ", ".join(
            "%s %.1f m" % (k, v) for k, v in sorted(by_size.items())))
    for text in n.findings:
        add("WARN", text)
    order = {"FAIL": 0, "WARN": 1, "INFO": 2}
    found.sort(key=lambda f: order.get(f["level"], 3))
    return found


#: What a confirmation does NOT depend on - names that change without a pipe moving.
NAMES = ("level", "space", "family", "type")


def fingerprint(n):
    """The network's own fingerprint - geometry, sizes and connections, never names: a
    confirmation holds for exactly these pipes and heads, and survives a renamed Space."""
    n = read(n)
    elements = [dict((k, v) for k, v in e.items() if k not in NAMES)
                for e in n.raw.get("elements") or []]
    raw = {"system": (n.raw.get("system") or {}).get("id"), "elements": elements}
    text = json.dumps(raw, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(text.encode("utf-8")).hexdigest()[:16]
