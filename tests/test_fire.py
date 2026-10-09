# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The fire protection design engine - its arithmetic against first principles
and an independent solver, its tables against their own units, and its refusal
to supply a design value nobody gave it.

    python tests/test_fire.py

WHAT IT PROVES
  1. THE PHYSICS MATCHES ITS SOURCES. NFPA 13's Hazen-Williams form, 4.52 in
     US units and 6.05 x 10^5 in SI, derived here from the general
     Hazen-Williams equation and from each other; 0.433 psi per foot as
     water's own weight; the K-factor conversion against NFPA's nominal K80;
     the supply curve through its own test point.

  2. THE SOLVER IS RIGHT. A network solved by Newton's method agrees with a
     single path walked by hand, with a two-branch tree solved here by an
     independent shooting method, and - for a looped grid - satisfies every
     equation it was built from: every pipe on Hazen-Williams, every node
     balanced, the governing sprinkler exactly at its need.

  3. THE WORKED EXAMPLES COME OUT. A design area, a room's layout, a drawn
     layout checked, pipe schedules (above and below a ceiling too), the beam
     and three-times rules, a pump, standpipes, extinguishers, detectors,
     storage and coverage - each against a sum done here.

  4. THE TABLES AGREE WITH THEMSELVES. Every SI figure is its US figure
     converted, every bore is the outside diameter less two walls, and the
     schedule 40 bores are the HVAC engine's.

  5. HERON SUPPLIES NO DESIGN VALUE (D-33), AND HOLDS NONE IT COULD NOT CHECK.
     Every calculation handed nothing computes nothing and asks; a figure is
     offered beside the question and never applied; the hazard class is never
     inferred; a factor that enlarges a demand is asked; a cell left out of a
     table refuses rather than guesses.

  6. IT NEVER UNDERSIZES. Across every count each schedule holds, the size
     chosen carries the count and the size below does not; every layout meets
     its limits and none with fewer devices does.

  7. WHAT IT CANNOT USE IS REFUSED, and what it does not read is named.

  8. IT REACHES NOTHING, and a project's standards are kept as D-111 says -
     asked once, in that project's own FIRE record, never its HVAC one.

  9. ONE STANDARD AT A TIME. EN 12845, FM Global and the UAE's authority are
     read however they are written; a class is read only in the project's own
     standard and never across; each standard's figures are offered beside
     its questions; adjustments are added on the area first selected, as
     NFPA 13's own example; and the round-two calculations - temperature
     ratings, fittings, hose reels, a detector radius, BS 5306-8's
     extinguishers, coverage by two devices - come out against sums done
     here, with their tables agreeing with their own units.

WHAT IT DOES NOT PROVE
  That any answer is right for a building, or that a held figure is NFPA's
  current text - docs/42 s10 says how far each was checked. Nothing here has
  met a real project; docs/42 s14 says what that would take.
"""

from __future__ import print_function

import ast
import math
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_fire as F                                        # noqa: E402
import heron_hvac as H                                        # noqa: E402

FAILURES = []
STD = {"sprinkler_standard": "NFPA 13-2022", "fire_authority": "QCDD"}


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def near(value, expected, tolerance):
    """Within a RELATIVE tolerance."""
    return value is not None and abs(value - expected) <= abs(expected) * tolerance


def result(answer, label):
    for name, text in answer["results"]:
        if name == label:
            return text
    return None


def first_number(text):
    return float((text or "nan").split()[0].replace(",", ""))


def checks(answer, level=None):
    return [t for lv, t in answer["checks"] if level is None or lv == level]


def run(name, inputs, recorded=None):
    return F.run(name, dict(inputs), recorded=recorded)


# ---------------------------------------------------------------------------

def physics():
    print("1. the physics against its sources")

    def general(q_gpm, d_in, c):
        # The general Hazen-Williams equation, V = 1.318 C R^0.63 S^0.54 (ft, s),
        # for a full round pipe, as psi per foot of water at 62.4 lb/ft3.
        q = q_gpm * 231.0 / 1728.0 / 60.0
        d = d_in / 12.0
        v = q / (math.pi * d * d / 4.0)
        s = (v / (1.318 * c * (d / 4.0) ** 0.63)) ** (1.0 / 0.54)
        return s * 62.4 / 144.0

    worst = 0.0
    for q in (15, 30, 60, 100, 250, 500, 1000):
        for d in (1.049, 1.38, 1.61, 2.067, 2.469, 3.068, 4.026, 6.065):
            for c in (100, 120, 150):
                nfpa = F.HW_US * q ** 1.85 / (c ** 1.85 * d ** 4.87)
                worst = max(worst, abs(nfpa / general(q, d, c) - 1.0))
    check(worst < 0.01, "NFPA 13's 4.52 form agrees with the general Hazen-Williams "
          "equation within %.2f %% over sprinkler flows and bores - its exponents are "
          "the general one's, rounded" % (100 * worst))
    si = F.HW_US / F.PSI_PER_BAR / F.M_PER_FT * F.MM_PER_IN ** 4.87 / F.LPM_PER_GPM ** 1.85
    check(near(si, F.HW_SI, 0.001),
          "and 4.52 in US units is %.0f in SI - NFPA's printed 6.05 x 10^5" % si)
    us = F.hw_friction(100 * F.LPM_PER_GPM, 2.067 * 25.4, 120) * F.PSI_PER_BAR * F.M_PER_FT
    check(near(us, F.HW_US * 100 ** 1.85 / (120 ** 1.85 * 2.067 ** 4.87), 0.001),
          "the SI form used gives the US form's psi per foot for 100 gpm in a 2 in pipe")
    check(near(F.BAR_PER_M * F.PSI_PER_BAR * F.M_PER_FT, 62.35 / 144.0, 0.0005),
          "0.433 psi per foot is water's own weight, 62.35 lb/ft3 over 144 in2 - %.5f bar/m"
          % F.BAR_PER_M)
    k80 = 5.6 * F.K_METRIC_PER_US
    check(abs(k80 - 80.7) < 0.05, "K 5.6 gpm/psi^0.5 is K %.2f L/min.bar^0.5 - NFPA's "
          "nominal metric K80 is its rounding" % k80)
    check(near(F.head_flow(80.0, 0.5), 80.0 * math.sqrt(0.5), 1e-12)
          and near(F.head_pressure(80.0, 56.5685), 0.5, 1e-5),
          "q = K sqrt(p) and p = (q/K)^2 are each other's inverse")
    check(near(F.supply_pressure(6.0, 4.0, 3000.0, 3000.0), 4.0, 1e-12)
          and near(F.supply_pressure(6.0, 4.0, 3000.0, 0.0), 6.0, 1e-12)
          and near(F.supply_flow(6.0, 4.0, 3000.0, 4.0), 3000.0, 1e-9),
          "the supply curve passes through the static pressure and the test point, and "
          "its inverse returns the test flow")
    one = run("convert", {"value": 100, "from": "gpm", "to": "l/min"})
    check(first_number(one["results"][0][1]) == 378.541,
          "100 US gpm converts to 378.541 L/min, the exact gallon")
    dens = run("convert", {"value": 0.1, "from": "gpm/ft2", "to": "mm/min"})
    check(abs(first_number(dens["results"][0][1]) - 4.07458) < 1e-4,
          "0.10 gpm/ft2 is 4.0746 mm/min - NFPA's SI prints 4.1")
    print()


# ---------------------------------------------------------------------------

def _branch(k, p_end, r, n, dz=0.0):
    """Walk one branch line back from its end sprinkler: (flow, pressure at its root)."""
    q, p = k * math.sqrt(p_end), p_end
    for i in range(1, n):
        p = p + r * q ** 1.85 + dz
        q += k * math.sqrt(p)
    p = p + r * q ** 1.85 + dz          # the arm from the first sprinkler to the root
    return q, p


def _bisect(f, lo, hi):
    for _ in range(200):
        mid = 0.5 * (lo + hi)
        if f(mid):
            hi = mid
        else:
            lo = mid
    return 0.5 * (lo + hi)


def solver():
    print("2. the network solver against hand sums and an independent method")
    r1 = F.hw_friction(1.0, 26.64, 120) * 3.0
    nodes = {"S": {"z": 0.0}, "A1": {"z": 0.0, "k": 80.0, "p_req": 0.5},
             "A2": {"z": 0.0, "k": 80.0, "p_req": 0.5}, "A3": {"z": 0.0, "k": 80.0, "p_req": 0.5}}
    pipes = [{"a": "S", "b": "A1", "r": r1}, {"a": "A1", "b": "A2", "r": r1},
             {"a": "A2", "b": "A3", "r": r1}]
    p, q, gov = F.solve_network(nodes, pipes, "S")
    hq, hp = _branch(80.0, 0.5, r1, 3)
    check(gov == "A3" and near(p["S"], hp, 1e-9) and near(q[0], hq, 1e-9),
          "a branch of three sprinklers: the end one governs, and the source needs %.4f bar "
          "for %.2f L/min - the same as walking it back by hand" % (p["S"], q[0]))
    nodes2 = dict((n, dict(v)) for n, v in nodes.items())
    for n in ("A1", "A2", "A3"):
        nodes2[n]["z"] = 4.0
    p2, _q2, _g2 = F.solve_network(nodes2, pipes, "S")
    check(near(p2["S"] - p["S"], 4.0 * F.BAR_PER_M, 1e-9),
          "lift the branch 4 m and the source needs 4 x 0.0979 bar more, exactly")

    # Two branches at one junction J, the riser S-J: the independent method walks
    # each branch from its end, the governing branch at its need, the other
    # solved by bisection for the same junction pressure.
    ra = F.hw_friction(1.0, 26.64, 120) * 3.2
    rb = F.hw_friction(1.0, 35.08, 120) * 4.1
    rr = F.hw_friction(1.0, 52.48, 120) * 12.0
    tree = {"S": {"z": 0.0}, "J": {"z": 3.0}}
    tp = [{"a": "S", "b": "J", "r": rr}]
    for line, r, k, n, need in (("A", ra, 80.0, 4, 0.6), ("B", rb, 115.0, 3, 0.9)):
        prev = "J"
        for i in range(1, n + 1):
            tree["%s%d" % (line, i)] = {"z": 3.0, "k": k, "p_req": need}
            tp.append({"a": prev, "b": "%s%d" % (line, i), "r": r})
            prev = "%s%d" % (line, i)
    pt, qt, gt = F.solve_network(tree, tp, "S")
    qa, pa = _branch(80.0, 0.6, ra, 4)
    qb, pb = _branch(115.0, 0.9, rb, 3)
    if pa >= pb:
        pj = pa
        end = _bisect(lambda e: _branch(115.0, e, rb, 3)[1] >= pj, 0.9, 50.0)
        qb = _branch(115.0, end, rb, 3)[0]
    else:
        pj = pb
        end = _bisect(lambda e: _branch(80.0, e, ra, 4)[1] >= pj, 0.6, 50.0)
        qa = _branch(80.0, end, ra, 4)[0]
    ps = pj + rr * (qa + qb) ** 1.85 + 3.0 * F.BAR_PER_M
    check(near(pt["J"], pj, 1e-7) and near(pt["S"], ps, 1e-7) and near(qt[0], qa + qb, 1e-7),
          "a two-branch tree: junction %.4f bar, source %.4f bar for %.2f L/min - the "
          "independent shooting method agrees" % (pt["J"], pt["S"], qt[0]))
    check(gt in ("A4", "B3"), "and the governing sprinkler is the end of a branch - %s" % gt)

    # A looped grid: two cross mains joined by three branch lines, sprinklers on
    # the branches. Checked by the equations themselves.
    g_nodes = {"S": {"z": 0.0}}
    g_pipes = [{"a": "S", "b": "L0", "r": F.hw_friction(1.0, 77.92, 120) * 8.0},
               {"a": "S", "b": "R0", "r": F.hw_friction(1.0, 77.92, 120) * 20.0}]
    for row in range(3):
        g_nodes["L%d" % row] = {"z": 3.5}
        g_nodes["R%d" % row] = {"z": 3.5}
        for col in range(4):
            g_nodes["H%d%d" % (row, col)] = {"z": 3.5, "k": 80.0, "p_req": 0.7}
        line = ["L%d" % row] + ["H%d%d" % (row, c) for c in range(4)] + ["R%d" % row]
        for a_, b_ in zip(line, line[1:]):
            g_pipes.append({"a": a_, "b": b_, "r": F.hw_friction(1.0, 35.08, 120) * 3.0})
        if row:
            for side in "LR":
                g_pipes.append({"a": "%s%d" % (side, row - 1), "b": "%s%d" % (side, row),
                                "r": F.hw_friction(1.0, 62.68, 120) * 3.0})
    gp, gq, gg = F.solve_network(g_nodes, g_pipes, "S")
    worst = 0.0
    for name, node in g_nodes.items():
        if name == "S":
            continue
        inflow = 0.0
        for pipe, q in zip(g_pipes, gq):
            if pipe["b"] == name:
                inflow += q
            elif pipe["a"] == name:
                inflow -= q
        out = F.head_flow(node["k"], gp[name]) if node.get("k") else 0.0
        worst = max(worst, abs(inflow - out))
    hw_ok = all(abs((gp[p["a"]] - gp[p["b"]] + F.BAR_PER_M * (g_nodes[p["a"]]["z"]
                                                             - g_nodes[p["b"]]["z"]))
                    - math.copysign(p["r"] * abs(q) ** 1.85, q)) < 1e-9
                for p, q in zip(g_pipes, gq) if abs(q) > 0.05)
    heads = [n for n in g_nodes if g_nodes[n].get("k")]
    check(worst < 1e-6, "a looped grid of 12 sprinklers: every node balances to %.1e L/min"
          % worst)
    check(hw_ok, "every pipe in it carries the flow Hazen-Williams gives for its head")
    check(abs(gp[gg] - 0.7) < 1e-9 and min(gp[h] for h in heads) >= 0.7 - 1e-9,
          "the governing sprinkler %s runs at exactly 0.7 bar and none at less" % gg)
    try:
        F.solve_network({"S": {"z": 0}, "A": {"z": 0, "k": 80, "p_req": 0.5},
                         "X": {"z": 0}}, [{"a": "S", "b": "A", "r": r1}], "S")
        cut = False
    except F.Unsolved as why:
        cut = "not connected" in str(why) and "X" in str(why)
    check(cut, "a node connected to nothing is named, not solved round")
    print()


# ---------------------------------------------------------------------------

def worked_examples():
    print("3. the worked examples come out")
    area = run("design_area", dict(STD, hazard="OH1", density_mm_min=6.1, design_area_m2=140,
                                   area_per_sprinkler_m2=12, spacing_along_branch_m=3.4,
                                   k_lpm_bar=80, min_pressure_bar=0.5,
                                   hose_allowance_lpm=946.4, duration_min=60))
    check(area["status"] == "ok" and result(area, "Sprinklers in it").startswith("12 ")
          and result(area, "Along a branch line").startswith("5 ")
          and result(area, "Branch lines") == "3",
          "140 m2 at 12 m2 each holds 12 sprinklers; 1.2 sqrt(140) = 14.2 m over 3.4 m is 5 "
          "along a branch line; 3 branch lines")
    check(abs(first_number(result(area, "Least flow per sprinkler")) - 73.2) < 0.05
          and abs(first_number(result(area, "End sprinkler pressure")) - (73.2 / 80) ** 2) < 0.001,
          "each gives 6.1 x 12 = 73.2 L/min, which a K80 needs (73.2/80)^2 = 0.837 bar for")
    dry = run("design_area", dict(STD, density_mm_min=6.1, design_area_m2=140,
                                  area_per_sprinkler_m2=12, spacing_along_branch_m=3.4,
                                  system_type="dry", dry_area_increase_pct=30))
    check(first_number(result(dry, "Design area")) == 182.0
          and result(dry, "Sprinklers in it").startswith("16 "),
          "a dry system at the +30 % the modeller gave works 182 m2 - 16 sprinklers")
    combined = run("design_area", dict(STD, density_mm_min=6.1, design_area_m2=140,
                                       area_per_sprinkler_m2=12, spacing_along_branch_m=3.4,
                                       above_ceiling={"density_mm_min": 6.1,
                                                      "design_area_m2": 140,
                                                      "area_per_sprinkler_m2": 9},
                                       combine_above_and_below=True))
    apart = run("design_area", dict(STD, density_mm_min=6.1, design_area_m2=140,
                                    area_per_sprinkler_m2=12, spacing_along_branch_m=3.4,
                                    above_ceiling={"density_mm_min": 6.1,
                                                   "design_area_m2": 140,
                                                   "area_per_sprinkler_m2": 9},
                                    combine_above_and_below=False))
    below_q, above_q = 12 * 6.1 * 12, 16 * 6.1 * 9
    check(abs(first_number(result(combined, "Combined")) - (below_q + above_q)) < 0.1
          and abs(first_number(result(apart, "Governing")) - max(below_q, above_q)) < 0.1,
          "sprinklers above and below a ceiling: %.0f + %.0f L/min combined, the larger "
          "alone when the modeller says they are calculated apart" % (below_q, above_q))

    layout = run("sprinkler_layout", dict(STD, room_length_m=20, room_width_m=12,
                                          max_spacing_m=4.6, max_area_m2=12.1,
                                          max_wall_distance_m=2.3, min_spacing_m=1.8,
                                          min_wall_distance_m=0.1))
    check(layout["status"] == "ok" and result(layout, "Sprinklers").startswith("20 ")
          and "4.000 m along x, 3.000 m along y" in result(layout, "Spacing"),
          "a 20 x 12 m room at 4.6 m and 12.1 m2: 20 sprinklers, 4.0 x 3.0 m")

    room = [[0, 0], [12000, 0], [12000, 9000], [0, 9000]]
    grid_pts = [[x, y] for y in (1500, 4500, 7500) for x in (1500, 4500, 7500, 10500)]
    limits = dict(STD, max_spacing_m=4.6, max_area_m2=12.1, max_wall_distance_m=2.3,
                  min_spacing_m=1.8, min_wall_distance_m=0.1, branch_axis="x",
                  outline_mm=room)
    good = run("sprinkler_spacing", dict(limits, sprinklers=grid_pts))
    check(good["status"] == "ok" and not checks(good, "FAIL"),
          "a 3 x 3 m grid in a 12 x 9 m room passes every limit")
    moved = [list(p) for p in grid_pts]
    moved[3] = [9500, 1500]
    bad = run("sprinkler_spacing", dict(limits, sprinklers=moved))
    fails = " ".join(checks(bad, "FAIL"))
    check("4 (S 5.00 m" in fails and "area 15.00 m2" in fails and "wall 2.50 m away" in fails,
          "move the end sprinkler 1 m away from its wall and it fails, named: S = twice the "
          "2.5 m to the wall = 5.0 m, an area of 15 m2, and the wall too far")
    tight = [list(p) for p in grid_pts]
    tight[0] = [50, 1500]
    close = run("sprinkler_spacing", dict(limits, sprinklers=tight))
    check("wall 0.05 m close" in " ".join(checks(close, "FAIL")),
          "a sprinkler 50 mm from a wall fails the 100 mm minimum given")
    ell = [[0, 0], [12000, 0], [12000, 6000], [6000, 6000], [6000, 12000], [0, 12000]]
    pts = [[1500, 1500], [4500, 1500], [7500, 1500], [10500, 1500],
           [1500, 4500], [4500, 4500], [7500, 4500], [10500, 4500],
           [1500, 7500], [4500, 7500], [1500, 10500], [4500, 10500]]
    lshape = run("sprinkler_spacing", dict(limits, outline_mm=ell, sprinklers=pts))
    check(lshape["status"] == "ok" and not checks(lshape, "FAIL"),
          "an L-shaped room laid out on the same grid passes, the inside corner seen as a "
          "wall")
    # Row 5b-339: a head STANDING ON A WALL passed. A 10 x 10 m L (less a 5 x 5 m
    # corner) with a centred 3 x 3 grid puts heads on the re-entrant walls and
    # the corner, and with no least wall distance given nothing caught it -
    # inside() counts an edge as inside, ray_to_wall ignores a wall at 0.
    ell10 = [[0, 0], [10000, 0], [10000, 5000], [5000, 5000], [5000, 10000], [0, 10000]]
    third = 10000.0 / 3.0
    centred = [[x, y] for y in (third / 2, 5000.0, 10000 - third / 2)
               for x in (third / 2, 5000.0, 10000 - third / 2)
               if not (x > 5000.0 and y > 5000.0)]
    no_minimum = dict(STD, max_spacing_m=4.6, max_area_m2=12.1, max_wall_distance_m=2.3,
                      branch_axis="x", outline_mm=ell10)
    walled = run("sprinkler_spacing", dict(no_minimum, sprinklers=centred))
    said = " ".join(checks(walled, "FAIL"))
    check(said.count("(on a wall)") == 3 and "3 of 8 sprinklers" in said,
          "the three heads standing on the re-entrant walls and corner FAIL 'on a wall', "
          "with no least wall distance given (%r)" % said)

    rows = [("light", "steel", 10, "2"), ("light", "steel", 11, "2 1/2"),
            ("light", "copper", 12, "2"), ("OH1", "steel", 21, "3"),
            ("OH2", "copper", 115, "4"), ("OH2", "copper", 116, "5"),
            ("OH1", "steel", 275, "6")]
    for hz, mat, n, want in rows:
        got = run("pipe_schedule", dict(STD, hazard=hz, material=mat, sprinklers=n))
        check(got["status"] == "ok" and result(got, "Size") == "%s %s" % (F.nominal_text(want), mat),
              "%s hazard, %d sprinklers on %s: %s" % (hz, n, mat, F.nominal_text(want)))
    ab = run("pipe_schedule", dict(STD, hazard="light", material="steel",
                                   above_and_below_ceiling=True, above=8, below=8))
    check(result(ab, "Size") == "%s steel" % F.nominal_text("2 1/2"),
          "8 above and 8 below a light hazard ceiling: 16 counted, past 2 in's 15 - 2 1/2 in")
    many = run("pipe_schedule", dict(STD, hazard="light", material="steel",
                                     above_and_below_ceiling=True,
                                     pipes=[{"id": "CM1", "above": 40, "below": 45}]))
    check(result(many, "Size") == "%s steel" % F.nominal_text("3"),
          "40 above + 45 below = 85, past the 50 the table allows 2 1/2 in: 3 in, and the "
          "light schedule for the larger level, 45, also gives 3 in")
    bigger = run("pipe_schedule", dict(STD, hazard="light", material="copper",
                                       above_and_below_ceiling=True, above=70, below=90))
    check(result(bigger, "Size") == "%s copper" % F.nominal_text("3 1/2"),
          "70 above + 90 below in copper: the larger level, 90, needs 3 1/2 in by the light "
          "schedule - more than the 3 in step")
    wide = run("pipe_schedule", dict(STD, hazard="OH1", material="steel", sprinklers=18,
                                     spacing_over_3_7m=True))
    check(result(wide, "Size") == "%s steel" % F.nominal_text("3"),
          "18 ordinary hazard sprinklers more than 3.7 m apart: 3 in, where 2 1/2 in carries "
          "20 at the usual spacing")
    branch = run("pipe_schedule", dict(STD, hazard="light", material="steel", sprinklers=9,
                                       most_on_a_branch=9))
    check(any("9 sprinklers on a branch line" in t for t in checks(branch, "FAIL")),
          "9 sprinklers on one side of a cross main fails the 8 the schedule allows")

    beam = [(200, 0.0), (304.8, 63.5), (350, 63.5), (700, 139.7), (1100, 304.8)]
    for dist, allowed in beam:
        got = run("obstruction", dict(STD, sprinkler_type="standard-upright-pendent",
                                      distance_to_obstruction_mm=dist))
        text = result(got, "Beam rule at A = %s mm" % F._g(float(dist))) or ""
        check(("at most %s mm" % F._g(allowed)) in text,
              "the beam rule at A = %s mm allows %s mm" % (F._g(float(dist)), F._g(allowed)))
    need = run("obstruction", dict(STD, sprinkler_type="standard-upright-pendent",
                                   deflector_above_bottom_mm=100))
    check("at least 609.6 mm" in (result(need, "Beam rule") or ""),
          "a deflector 100 mm above a beam's bottom needs the sprinkler 609.6 mm (2 ft) away")
    three = run("obstruction", dict(STD, sprinkler_type="standard-upright-pendent",
                                    obstruction_width_mm=300, clear_distance_mm=600))
    check(result(three, "Three-times rule").startswith("609.6 mm")
          and checks(three, "FAIL"),
          "a 300 mm column needs 900 mm by three times, capped at 24 in (609.6 mm) - 600 mm "
          "fails")
    duct = run("obstruction", dict(STD, sprinkler_type="standard-upright-pendent",
                                   obstruction_width_mm=1500))
    check(any("wider than 1219.2 mm" in t for t in checks(duct, "WARN")),
          "a 1500 mm duct is wider than 4 ft - sprinklers are asked for under it")

    pump = run("fire_pump", dict(STD, rated_flow_gpm=750, rated_pressure_bar=8,
                                 churn_pressure_bar=10, pressure_at_150_bar=5.5,
                                 demand_flow_lpm=3000, demand_pressure_bar=7.5,
                                 suction_pressure_bar=0))
    check(pump["status"] == "ok" and not checks(pump, "FAIL")
          and any("125.0 %" in t for t in checks(pump, "OK")),
          "a pump at 125 % churn and 69 % at 150 % flow passes NFPA 20's curve rules and "
          "meets 3000 L/min at 7.5 bar")
    steep = run("fire_pump", dict(STD, rated_flow_lpm=2000, rated_pressure_bar=8,
                                  churn_pressure_bar=11.5, pressure_at_150_bar=5.0))
    check(len(checks(steep, "FAIL")) == 2,
          "churn at 144 % and 62 % at 150 % flow - both rules fail")

    stand = run("standpipe", dict(STD, standpipe_class="i", standpipes=5, first_flow_lpm=1893,
                                  additional_flow_lpm=946, max_total_flow_lpm=3785,
                                  outlet_pressure_bar=6.9, height_m=40,
                                  friction_loss_bar=0.6))
    check(result(stand, "Flow").startswith("3785.0 L/min")
          and abs(first_number(result(stand, "Pressure at the source"))
                  - (6.9 + 40 * F.BAR_PER_M + 0.6)) < 0.001,
          "five Class I standpipes ask 1893 + 4 x 946, held to the 3785 L/min given; the "
          "source needs 6.9 bar + 40 m of rise + 0.6 bar of friction")
    ext = run("extinguishers", dict(STD, floor_area_m2=2000, rating_a=2, area_per_a_m2=139.4,
                                    max_area_m2=1045.2, max_travel_m=22.9))
    check(result(ext, "Extinguishers").startswith("8 at 2-A"),
          "2000 m2 of ordinary hazard at 2-A x 139.4 m2: 8 extinguishers, at least")
    det = run("detector_layout", dict(STD, room_length_m=20, room_width_m=15,
                                      detector_type="smoke", spacing_m=9.1,
                                      max_wall_distance_m=4.55))
    check(result(det, "Detectors").startswith("6 "),
          "a 20 x 15 m room at 9.1 m spacing: 6 smoke detectors")
    heat = run("detector_layout", dict(STD, room_length_m=20, room_width_m=15,
                                       detector_type="heat", spacing_m=15, height_factor=0.64,
                                       max_wall_distance_m=7.5))
    check(heat["status"] == "ok" and "x 0.64" in result(heat, "Spacing"),
          "a heat detector's listed 15 m at a 0.64 height factor is laid out at 9.6 m")
    tank = run("water_storage", dict(STD, sprinkler_flow_lpm=1500, duration_min=60,
                                     hose_allowance_lpm=946.4))
    check(abs(first_number(result(tank, "Effective volume")) - 146.78) < 0.01,
          "(1500 + 946.4) L/min for 60 min is 146.78 m3")
    cover = run("coverage_check", {"outline_mm": [[0, 0], [20000, 0], [20000, 10000],
                                                  [0, 10000]],
                                   "points_mm": [[5000, 5000], [15000, 5000]],
                                   "radius_m": 7})
    check(any("7.07" in t for t in checks(cover, "FAIL")),
          "two devices 10 m apart in a 20 x 10 m room leave the corners 7.07 m away")
    flow = run("sprinkler_flow", {"k_lpm_bar": 80, "density_mm_min": 4.1,
                                  "area_per_sprinkler_m2": 9, "min_pressure_bar": 0.5})
    check(first_number(result(flow, "Pressure needed")) == 0.5
          and "minimum pressure governs" in result(flow, "Flow at that pressure"),
          "4.1 mm/min over 9 m2 asks 36.9 L/min, which a K80 gives at 0.21 bar - the 0.5 bar "
          "minimum governs and it discharges more")
    print()


# ---------------------------------------------------------------------------

def tables_agree():
    print("4. the tables agree with themselves")
    crit = F.REFERENCES["design_criteria"]
    c = crit["columns"]
    worst = 0.0
    for row in crit["rows"]:
        for si, us in (("density mm/min", "density gpm/ft2"),
                       ("second density mm/min", "second density gpm/ft2")):
            worst = max(worst, abs(row[c.index(si)] - row[c.index(us)] * F.MM_MIN_PER_GPM_FT2))
    check(worst <= 0.05 + 1e-9, "every density's mm/min is its gpm/ft2 converted, to the "
          "0.1 NFPA prints (worst %.3f)" % worst)
    areas = all(abs(r[c.index("design area m2")] - r[c.index("design area ft2")]
                    * F.M2_PER_FT2) <= 5.0 for r in crit["rows"])
    hose = all(abs(r[c.index("hose allowance L/min")] - r[c.index("hose allowance gpm")]
                   * F.LPM_PER_GPM) < 0.05 for r in crit["rows"])
    check(areas and hose, "every design area is its ft2 to the 10 m2 the 2022 SI rounds to, "
          "and every hose allowance its gpm converted exactly")
    sp = F.REFERENCES["spacing_standard_spray"]
    cs = sp["columns"]
    ok = all(abs(r[cs.index("max area m2")] - r[cs.index("max area ft2")] * F.M2_PER_FT2) < 0.05
             and (r[cs.index("max spacing m")] == F.NOT_HELD
                  or abs(r[cs.index("max spacing m")] - r[cs.index("max spacing ft")]
                         * F.M_PER_FT) < 0.05) for r in sp["rows"])
    check(ok, "every protection area and spacing is its US figure converted, to 0.1")
    beam = all(abs(r[3] - r[4] * 25.4) < 1e-9 for r in F.REFERENCES["beam_rule_standard"]["rows"])
    steps = F.REFERENCES["beam_rule_standard"]["rows"]
    joined = all(abs(a_[2] - b_[1]) < 1e-9 for a_, b_ in zip(steps, steps[1:]))
    check(beam and joined, "the beam rule's millimetres are its inches x 25.4, and each row "
          "starts where the last one stops")
    bores = F.REFERENCES["steel_pipe_bores"]["rows"]
    check(all(abs(r[3] - 2 * r[4] - r[5]) < 0.011 for r in bores),
          "every steel bore is the outside diameter less two walls")
    hv = dict((r[1].split(" (")[1].rstrip(")").replace(" in", ""), r[2])
              for r in H.REFERENCES["pipe_sizes"]["rows"])
    same = [abs(hv[r[1]] - r[5]) < 1e-9 for r in bores if r[0] == "40" and r[1] in hv]
    check(same and all(same), "schedule 40's bores are the HVAC engine's, row for row (%d)"
          % len(same))
    ext = F.REFERENCES["extinguisher_rules"]
    ce = ext["columns"]
    check(all(abs(r[ce.index("area per unit of A m2")] - r[ce.index("area per unit of A ft2")]
                  * F.M2_PER_FT2) < 0.05
              and abs(r[ce.index("most travel m")] - r[ce.index("most travel ft")]
                      * F.M_PER_FT) < 0.05 for r in ext["rows"]),
          "the extinguisher areas and travel are their US figures converted")
    hh = F.REFERENCES["heat_detector_height"]["rows"]
    check(all(abs(r[1] - (10 + 2 * i) * F.M_PER_FT) < 0.005 for i, r in enumerate(hh))
          and [r[2] for r in hh] == sorted([r[2] for r in hh], reverse=True),
          "the heat detector heights are 10 to 30 ft in metres, and the factor only falls")
    pumps = F.REFERENCES["fire_pump_ratings"]["rows"]
    check(all(abs(r[1] - r[0] * F.LPM_PER_GPM) < 0.05 for r in pumps),
          "NFPA 20's rated capacities in L/min are their gpm converted")
    for table in ("pipe_schedule_light", "pipe_schedule_light_above_below",
                  "pipe_schedule_ordinary", "pipe_schedule_ordinary_wide",
                  "pipe_schedule_ordinary_above_below"):
        rows = F.REFERENCES[table]["rows"]
        for col in (1, 2):
            held = [r[col] for r in rows if r[col] not in (None, F.NOT_HELD)]
            check(held == sorted(held) and len(set(held)) == len(held),
                  "%s's %s column only rises" % (table, ("steel", "copper")[col - 1]))
    print()


# ---------------------------------------------------------------------------

def no_design_value():
    print("5. Heron supplies no design value (D-33), and holds none it could not check")
    for name in F.CALCULATIONS:
        if name == "reference":
            continue
        got = F.run(name, {})
        check(got["status"] == "missing" and not got["results"] and not got["tables"],
              "%s handed nothing computes nothing and asks for %d input(s)"
              % (name, len(got["missing"])))
    asked = run("design_area", dict(STD, hazard="OH1", area_per_sprinkler_m2=12,
                                    spacing_along_branch_m=3.4))
    dens = [m for m in asked["missing"] if m["input"] == "density_mm_min"]
    check(asked["status"] == "missing" and dens and "6.1" in dens[0].get("reference", "")
          and "do not assume" in dens[0]["reference"],
          "naming the hazard does NOT fill in its density - 6.1 mm/min is offered beside the "
          "question, never applied")
    second = run("design_area", dict(STD, hazard="light", concealed_space="unsprinklered-combustible",
                                     area_per_sprinkler_m2=12, spacing_along_branch_m=3.4))
    dens2 = [m for m in second["missing"] if m["input"] == "density_mm_min"]
    check(dens2 and "2.9 mm/min over 280 m2" in dens2[0].get("reference", ""),
          "with combustible concealed spaces unsprinklered, the offer is NFPA's second point "
          "- 2.9 mm/min over 280 m2")
    lay = run("sprinkler_layout", dict(STD, hazard="OH2", room_length_m=10, room_width_m=8))
    spacing = [m for m in lay["missing"] if m["input"] == "max_spacing_m"]
    check(lay["status"] == "missing" and spacing and "12.1 m2" in spacing[0]["reference"]
          and "4.6 m" in spacing[0]["reference"],
          "a layout's limits are asked, with ordinary hazard's 12.1 m2 and 4.6 m offered")
    dry = run("design_area", dict(STD, density_mm_min=6.1, design_area_m2=140,
                                  area_per_sprinkler_m2=12, spacing_along_branch_m=3.4,
                                  system_type="dry"))
    check(dry["status"] == "missing"
          and any(m["input"] == "dry_area_increase_pct" for m in dry["missing"]),
          "a dry system's larger area is ASKED, not skipped - leaving it out errs small")
    plain = run("design_area", dict(STD, density_mm_min=6.1, design_area_m2=140,
                                    area_per_sprinkler_m2=12, spacing_along_branch_m=3.4))
    check(first_number(result(plain, "Design area")) == 140.0
          and "no system type given" in " ".join(plain["assumed"]),
          "no quick-response reduction is applied unasked, and an unnamed system is said")
    cls = run("hazard_class", dict(STD, occupancy="kitchen"))
    check(cls["status"] == "ok" and not cls["results"]
          and any("does not classify" in t for t in checks(cls, "WARN"))
          and any("2 classes" in t for t in checks(cls, "WARN")),
          "a kitchen is shown under both the classes its lists hold - and NOT classified")
    copper = run("pipe_schedule", dict(STD, hazard="OH1", material="copper", sprinklers=30))
    check(copper["status"] == "refused" and "not read" in copper["refused"][0],
          "30 ordinary hazard sprinklers on copper reach a cell that could not be checked - "
          "refused, not guessed")
    steel = run("pipe_schedule", dict(STD, hazard="light", material="steel", sprinklers=61))
    check(steel["status"] == "refused" and "not read" in steel["refused"][0],
          "61 light hazard sprinklers on steel reach the 3 1/2 in cell left out - refused")
    ohab = run("pipe_schedule", dict(STD, hazard="OH1", material="steel",
                                     above_and_below_ceiling=True, above=16, below=16))
    check(ohab["status"] == "refused" and "not read" in ohab["refused"][0],
          "32 above and below an ordinary hazard ceiling reach the 3 in row left out - refused")
    near_ok = run("obstruction", dict(STD, sprinkler_type="standard-upright-pendent",
                                      distance_to_obstruction_mm=1500,
                                      deflector_above_bottom_mm=400))
    near_bad = run("obstruction", dict(STD, sprinkler_type="standard-upright-pendent",
                                       distance_to_obstruction_mm=1500,
                                       deflector_above_bottom_mm=420))
    check(checks(near_ok, "OK") and checks(near_bad, "FAIL")
          and "419.1" in (result(near_ok, "Beam rule at A = 1500 mm") or ""),
          "a beam 1.5 m away reads the 4 ft 6 in row - 16 1/2 in, 419.1 mm: 400 passes, "
          "420 fails")
    far = run("obstruction", dict(STD, sprinkler_type="standard-upright-pendent",
                                  distance_to_obstruction_mm=1800,
                                  deflector_above_bottom_mm=500))
    check("disagree" in (result(far, "Beam rule at A = 1800 mm") or "")
          and any("NOT CHECKED" in t for t in checks(far, "WARN")),
          "a beam 1.8 m away is past the rows held, where the sources disagree - said, and "
          "the height NOT CHECKED")
    ask_c = run("hydraulic", dict(STD, source="S",
                                  nodes=[{"id": "S", "elevation_m": 0},
                                         {"id": "A", "elevation_m": 3, "k_lpm_bar": 80,
                                          "min_flow_lpm": 60}],
                                  pipes=[{"from": "S", "to": "A", "length_m": 10,
                                          "bore_mm": 26.64}]))
    asked_c = [m for m in ask_c["missing"] if m["input"] == "c_factor"]
    asked_p = [m for m in ask_c["missing"] if m["input"] == "min_pressure_bar"]
    check(ask_c["status"] == "missing" and asked_c and asked_p
          and "wet or deluge 120" in asked_c[0].get("reference", "")
          and "7 psi (0.5 bar)" in asked_p[0].get("reference", ""),
          "a hydraulic calculation asks for C and the minimum pressure, NFPA 13's figures "
          "offered beside each - neither is filled in")
    office = F.REFERENCES["office_firefighting"]
    clear = F.lookup("sprinkler_rules", "clearance below a deflector to storage")
    check(clear and "457 mm" in clear[1] and "450 mm" in clear[1]
          and "owner's file" in clear[2] and len(office["rows"]) >= 10,
          "the owner's own figures are held beside NFPA's and labelled, never applied - the "
          "storage clearance carries both, each with where it came from")
    text = F.describe(run("pipe_schedule", dict(STD, hazard="light", material="steel",
                                                 sprinklers=10)))
    check("QCDD" in text and "fire consultant" in text and "DRAFT" in text,
          "every answer ends saying the fire consultant and QCDD approve it")
    print()


# ---------------------------------------------------------------------------

def never_undersizes():
    print("6. it never undersizes")
    bad = []
    for table in ("pipe_schedule_light", "pipe_schedule_ordinary",
                  "pipe_schedule_ordinary_wide"):
        rows = F.REFERENCES[table]["rows"]
        for col, material in ((1, "steel"), (2, "copper")):
            held = [r for r in rows if r[col] not in (None, F.NOT_HELD)]
            for n in range(1, held[-1][col] + 1):
                size, _why, _kind = F.schedule_size(table, material, n)
                if size is None:
                    continue
                i = [r[0] for r in rows].index(size)
                carries = rows[i][col]
                below = rows[i - 1][col] if i else 0
                if carries < n or (below not in (None, F.NOT_HELD) and below >= n):
                    bad.append((table, material, n, size))
    check(not bad, "across every count the schedules hold, the size chosen carries it and "
          "the size below does not%s" % ("" if not bad else ": %s" % bad[:3]))
    worst = []
    for length, width, smax, amax in ((20, 12, 4.6, 12.1), (7.3, 5.1, 4.6, 20.9),
                                      (31, 4, 4.6, 12.1), (9, 9, 3.7, 9.3), (50, 33, 4.6, 12.1)):
        rows_cols = F.grid(length, width, smax=smax, amax=amax, wmax=smax / 2.0)
        rows, cols = rows_cols
        sx, sy = length / cols, width / rows
        meets = sx <= smax + 1e-9 and sy <= smax + 1e-9 and sx * sy <= amax + 1e-9
        fewer = [(r, c) for r in range(1, rows * cols) for c in range(1, rows * cols)
                 if r * c < rows * cols and length / c <= smax + 1e-9
                 and width / r <= smax + 1e-9 and (length / c) * (width / r) <= amax + 1e-9]
        if not meets or fewer:
            worst.append((length, width, rows_cols, fewer[:1]))
    check(not worst, "five rooms: every grid meets its spacing and area, and no grid of "
          "fewer sprinklers does%s" % ("" if not worst else ": %s" % worst))
    print()


# ---------------------------------------------------------------------------

def refusals():
    print("7. what it cannot use is refused, and what it does not read is named")
    neg = run("friction_loss", {"flow_lpm": -10, "bore_mm": 52.48, "c_factor": 120})
    check(neg["status"] == "refused", "a negative flow is refused")
    twice = run("friction_loss", {"flow_lpm": 300, "flow_gpm": 80, "bore_mm": 52.48,
                                  "c_factor": 120})
    check(twice["status"] == "refused" and "once" in twice["refused"][0],
          "a flow given in two units at once is refused, not reconciled")
    word = run("friction_loss", {"flow_lpm": "lots", "bore_mm": 52.48, "c_factor": 120})
    check(word["status"] == "refused", "a flow that is not a number is refused")
    unknown = F.run("sprinkler_magic", {})
    check(unknown["status"] == "unknown" and "pipe_schedule" in unknown["known"],
          "an unknown calculation is unknown, and the known ones are listed")
    typo = run("friction_loss", {"flow_lpm": 300, "bore_mm": 52.48, "c_factor": 120,
                                 "lenght_m": 10})
    check(typo["status"] == "ok" and "lenght_m" in typo["ignored"],
          "a mistyped key is named IGNORED, never quietly dropped")
    eh = run("pipe_schedule", dict(STD, hazard="EH1", material="steel", sprinklers=10))
    check(eh["status"] == "refused" and "hydraulic" in eh["refused"][0],
          "extra hazard is refused the pipe schedule and sent to the hydraulic calculation")
    guess = run("pipe_schedule", dict(STD, hazard="ordinary", material="steel", sprinklers=4))
    check(guess["status"] == "refused",
          "'ordinary' is not a class - OH1 or OH2 is the engineer's to say")
    src = run("hydraulic", dict(STD, source="A", c_factor=120, min_pressure_bar=0.5,
                                nodes=[{"id": "S", "elevation_m": 0},
                                       {"id": "A", "elevation_m": 3, "k_lpm_bar": 80,
                                        "min_flow_lpm": 60}],
                                pipes=[{"from": "S", "to": "A", "length_m": 10,
                                        "bore_mm": 26.64}]))
    check(src["status"] == "refused", "a sprinkler named as the source is refused")
    lost = run("hydraulic", dict(STD, source="S", c_factor=120, min_pressure_bar=0.5,
                                 nodes=[{"id": "S", "elevation_m": 0},
                                        {"id": "A", "elevation_m": 3, "k_lpm_bar": 80,
                                         "min_flow_lpm": 60},
                                        {"id": "B", "elevation_m": 3, "k_lpm_bar": 80,
                                         "min_flow_lpm": 60}],
                                 pipes=[{"from": "S", "to": "A", "length_m": 10,
                                         "bore_mm": 26.64}]))
    check(lost["status"] == "refused" and "B" in lost["refused"][0],
          "a sprinkler connected to nothing is refused by name")
    out = run("sprinkler_spacing", dict(STD, outline_mm=[[0, 0], [5000, 0], [5000, 5000],
                                                         [0, 5000]],
                                        sprinklers=[[2500, 2500], [9000, 2500]],
                                        branch_axis="x", max_spacing_m=4.6, max_area_m2=12.1,
                                        max_wall_distance_m=2.3))
    check(out["status"] == "refused" and "outside" in out["refused"][0],
          "a sprinkler outside the room's outline is refused, not measured")
    pump = run("fire_pump", dict(STD, rated_flow_lpm=2000, rated_pressure_bar=8,
                                 churn_pressure_bar=7, pressure_at_150_bar=5))
    check(pump["status"] == "refused", "a pump curve that rises with flow is refused")
    supply = run("water_supply", {"supply_static_bar": 4, "supply_residual_bar": 5,
                                  "supply_test_flow_lpm": 2000, "demand_flow_lpm": 1000,
                                  "demand_pressure_bar": 3})
    check(supply["status"] == "refused", "a residual above the static is refused")
    qr = run("design_area", dict(STD, density_mm_min=6.1, design_area_m2=140,
                                 area_per_sprinkler_m2=12, spacing_along_branch_m=3.4,
                                 system_type="dry", dry_area_increase_pct=30,
                                 qr_reduction_pct=25))
    check(qr["status"] == "refused", "a quick-response reduction on a dry system is refused")
    smoke = run("detector_layout", dict(STD, room_length_m=10, room_width_m=10,
                                        detector_type="smoke", spacing_m=9.1,
                                        max_wall_distance_m=4.55, height_factor=0.8))
    check(smoke["status"] == "refused", "a height factor on a smoke detector is refused")
    print()


# ---------------------------------------------------------------------------

def reaches_nothing_and_keeps_once():
    print("8. it reaches nothing, and a project's standards are kept once (D-111)")
    tree = ast.parse(open(os.path.join(ROOT, "brain", "heron_fire.py")).read())
    names = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            names.update(a.name.split(".")[0] for a in node.names)
        elif isinstance(node, ast.ImportFrom):
            names.add((node.module or "").split(".")[0])
    check(names <= {"collections", "math", "os", "sys", "heron_hvac"},
          "the engine imports the standard library and the HVAC engine only: %s"
          % ", ".join(sorted(names)))
    import heron_tools as TOOLS
    check(TOOLS.TOOLS.get("heron_fire") == (TOOLS.READ, None),
          "the MCP registry declares heron_fire READ, with no bridge operation")

    # Every Revit tool an INTO REVIT line names is a capability the library has,
    # read from the source so a line no test reaches is checked too - the HVAC
    # suite's own guard, kept for this engine.
    capabilities = set()
    fragments = os.path.join(ROOT, "brain", "fragments")
    for folder in os.listdir(fragments):
        path = os.path.join(fragments, folder, "fragment.yaml")
        if os.path.isfile(path):
            for line in open(path):
                if line.startswith("capability:"):
                    capabilities.add(line.split(":", 1)[1].strip().strip("'\""))
    named = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Call) and getattr(node.func, "attr", None) == "into_revit":
            for part in ast.walk(node):
                if isinstance(part, ast.Constant) and isinstance(part.value, str):
                    named.update(re.findall(r"\b[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)+\b", part.value))
    unknown = sorted(named - capabilities)
    check(len(named) >= 8 and not unknown,
          "each of the %d Revit tools the answers name is a capability in brain/fragments%s"
          % (len(named), "" if not unknown else " - not found: %s" % ", ".join(unknown)))

    plain = {"hazard": "light", "material": "steel", "sprinklers": 10}
    first = F.run("pipe_schedule", plain)
    asks = [q["input"] for q in first["ask_once"]]
    check(first["status"] == "ok" and asks == ["sprinkler_standard", "fire_authority"]
          and "ASK ONCE FOR THIS PROJECT" in F.describe(first),
          "with the project's standards unknown it still answers, and asks the two once")
    recorded = dict((k, {"value": v, "recorded": "2026-10-02T00:00:00Z"})
                    for k, v in STD.items())
    kept = F.run("pipe_schedule", plain, recorded=recorded)
    check(kept["ask_once"] == [] and kept["standards"]["fire_authority"]["from"] == "record"
          and any("QCDD approves" in t for t in checks(kept, "WARN")),
          "with the record handed in, nothing is asked and QCDD's precedence is said")
    told = F.run("pipe_schedule", dict(plain, sprinkler_standard="NFPA 13 2019"),
                 recorded=recorded)
    check(told["standards"]["sprinkler_standard"] == {"value": "NFPA 13-2019",
                                                      "from": "request"}
          and any("follows NFPA 13-2019" in t for t in checks(told, "WARN")),
          "an edition said in the request wins, and one Heron's tables are not from is said")
    other = F.run("pipe_schedule", dict(plain, sprinkler_standard="other"))
    check(any("a comparison" in t for t in checks(other, "WARN")),
          "a project not governed by NFPA 13 is told the figures are a comparison")
    bad = F.run("pipe_schedule", dict(plain, sprinkler_standard="NFPA 13"))
    check(bad["status"] == "refused", "an edition with no year is refused, not guessed")
    odd = dict(recorded, fire_authority={"value": "the council", "recorded": "x"})
    again = F.run("pipe_schedule", plain, recorded=odd)
    check([q["input"] for q in again["ask_once"]] == ["fire_authority"],
          "a recorded answer the engine cannot read is ASKED AGAIN, never repaired")
    pump = F.run("fire_pump", {"rated_flow_lpm": 2000, "rated_pressure_bar": 8,
                               "churn_pressure_bar": 10, "pressure_at_150_bar": 5.5,
                               "sprinkler_standard": "NFPA 13-2022"})
    check("sprinkler_standard" in pump["ignored"]
          and [q["input"] for q in pump["ask_once"]] == ["fire_authority"],
          "a pump question asks only who approves the design, and names an NFPA 13 edition "
          "given to it IGNORED")

    import heron_designbasis as KEEP
    import heron_brain as BRAIN
    home = tempfile.mkdtemp()
    was = dict((k, os.environ.get(k)) for k in ("HERON_KNOWLEDGE", "HERON_AUDIT"))
    os.environ["HERON_KNOWLEDGE"] = home
    os.environ["HERON_AUDIT"] = os.path.join(home, "audit")
    try:
        check(KEEP.path_for("P-1", "fire").endswith(".fire.json")
              and KEEP.path_for("P-1", "fire") != KEEP.path_for("P-1", "hvac"),
              "a project's fire record is its own file, beside its HVAC one")
        try:
            KEEP.path_for("P-1", "plumbing")
            named = True
        except ValueError:
            named = False
        check(not named, "a discipline nothing keeps a basis for is refused, not filed")
        got = BRAIN.fire("pipe_schedule", dict(plain, **STD), project="P-2",
                         project_name="Tower 2")
        check("kept for this project: fire_authority = QCDD" in got["text"]
              and got["ask_once"] == [],
              "through the seam, the two answers given once are kept for the open project")
        later = BRAIN.fire("pipe_schedule", plain, project="P-2")
        check(later["ask_once"] == []
              and later["standards"]["sprinkler_standard"]["from"] == "record"
              and "recorded for this project" in later["text"],
              "and the next fire answer there asks nothing and says where each came from")
        hvac_after = KEEP.read("P-2", "hvac")[0]
        check(hvac_after == {} and not os.path.exists(KEEP.path_for("P-2", "hvac")),
              "the fire answers never reach the project's HVAC record")
        elsewhere = BRAIN.fire("pipe_schedule", plain, project="P-3")
        check([q["input"] for q in elsewhere["ask_once"]] == ["sprinkler_standard",
                                                              "fire_authority"],
              "another project is asked afresh - nothing crosses between projects")
        nowhere = BRAIN.fire("pipe_schedule", dict(plain, **STD), project=None)
        check("NOT KEPT" in nowhere["text"]
              and not os.path.exists(os.path.join(home, "projects", "None.fire.json")),
              "with no project known, nothing is kept and the answer says why")
        listing = BRAIN.fire("", "")
        check(listing["status"] == "catalogue" and "pipe_schedule" in listing["text"]
              and "needs:" in listing["text"],
              "an empty calculation lists them all and what each needs")
    finally:
        for key, value in was.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
        shutil.rmtree(home, ignore_errors=True)
    print()


def beyond_nfpa():
    print("9. beyond NFPA 13 - one standard at a time, and the round-two figures")
    sv = F.standard_value
    check(sv("sprinkler_standard", "BS EN 12845:2015+A1:2019") == "EN 12845:2015"
          and sv("sprinkler_standard", "EN12845") == "EN 12845"
          and sv("sprinkler_standard", "fm global") == "FM Global"
          and sv("fire_authority", "Dubai Civil Defence") == F.UAE
          and sv("fire_authority", "UAE") == F.UAE,
          "EN 12845, FM Global and the UAE's authority are read however they are written")
    try:
        sv("sprinkler_standard", "NFPA 13R 2019")
        residential = None
    except F.Refused as why:
        residential = str(why)
    check(residential is not None and "residential" in residential,
          "NFPA 13R is refused as a project standard, saying why - its figures are reference "
          "rows only")

    en = dict(STD, sprinkler_standard="EN 12845:2015")
    fm = dict(STD, sprinkler_standard="FM Global", fire_authority="other")
    oh3 = run("design_area", dict(STD, hazard="OH3"))
    check(oh3["status"] == "refused" and "BS EN 12845 class" in oh3["refused"][0]
          and "NFPA 13-2022" in oh3["refused"][0],
          "OH3 under an NFPA 13 project is refused - an EN 12845 class is never read across")
    eh1 = run("design_area", dict(en, hazard="EH1"))
    check(eh1["status"] == "refused" and "NFPA 13 class" in eh1["refused"][0],
          "EH1 under an EN 12845 project is refused the same way")
    hc = run("design_area", dict(STD, sprinkler_standard=None, hazard="HC-2"))
    check(hc["status"] == "refused" and "FM Global class" in hc["refused"][0]
          and "not known" in hc["refused"][0],
          "HC-2 with the standard unknown is refused, saying which standard it belongs to")

    en_oh2 = run("design_area", dict(en, hazard="OH2", area_per_sprinkler_m2=12,
                                     system_type="dry"))
    offers = dict((m["input"], m.get("reference", "")) for m in en_oh2["missing"])
    check(en_oh2["status"] == "missing" and "5 mm/min for OH2" in offers["density_mm_min"]
          and "144 m2 for OH2" in offers["design_area_m2"]
          and "180 m2 dry against 144 m2 wet - 25 %" in offers["dry_area_increase_pct"]
          and "spacing_along_branch_m" not in offers,
          "EN 12845 OH2: 5 mm/min, 144 m2 and its dry 180 m2 (25 %) OFFERED - and NFPA's "
          "1.2 sqrt A spacing not asked")
    fm_hc3 = run("design_area", dict(fm, hazard="HC-3", area_per_sprinkler_m2=9))
    fm_offers = dict((m["input"], m.get("reference", "")) for m in fm_hc3["missing"])
    check("12.22 mm/min for HC-3" in fm_offers.get("density_mm_min", "")
          and "232.3 m2 for HC-3" in fm_offers.get("design_area_m2", "")
          and "not read in this work for HC-3, a ceiling up to 60 ft" in
          fm_offers.get("density_mm_min", ""),
          "FM Global HC-3: 0.30 gpm/ft2 = 12.22 mm/min over 2500 ft2 = 232.3 m2 offered, its "
          "30-60 ft row said not read")
    nf_dry = run("design_area", dict(STD, hazard="OH1", area_per_sprinkler_m2=12,
                                     spacing_along_branch_m=3.4, system_type="dry"))
    dry_offer = [m for m in nf_dry["missing"] if m["input"] == "dry_area_increase_pct"]
    check(dry_offer and "dry pipe - the design area increased 30 %" in dry_offer[0]["reference"],
          "NFPA 13's 30 % for a dry system is OFFERED beside the question, never applied")
    lh_dry = run("design_area", dict(en, hazard="LH", density_mm_min=2.25, design_area_m2=84,
                                     area_per_sprinkler_m2=21, system_type="dry",
                                     dry_area_increase_pct=25))
    check(lh_dry["status"] == "ok" and result(lh_dry, "Along a branch line") is None
          and any("does not allow a dry system at LH" in t for t in checks(lh_dry, "FAIL")),
          "a dry LH system under EN 12845 FAILS - the standard designs it as OH1 - and no "
          "NFPA branch count is given")

    # Adjustments are taken on the area FIRST selected and added: NFPA 13's own
    # example, 2500 + 30 % - 25 % = 2625 ft2. In turn it would be 2437.5.
    ft2 = F.M2_PER_FT2
    adj = run("design_area", dict(STD, hazard="EH1", density_mm_min=12.2,
                                  design_area_m2=2500 * ft2, area_per_sprinkler_m2=9.3,
                                  spacing_along_branch_m=3.0, system_type="dry",
                                  dry_area_increase_pct=30, high_temp_reduction_pct=25))
    check(near(first_number(result(adj, "Design area")), 2625 * ft2, 1e-4),
          "a dry system with high-temperature sprinklers: 2500 + 750 - 625 = 2625 ft2, each "
          "on the area first selected - not 2437.5 in turn")
    floor = run("design_area", dict(STD, hazard="EH2", density_mm_min=16.3,
                                    design_area_m2=2500 * ft2, area_per_sprinkler_m2=9.3,
                                    spacing_along_branch_m=3.0, high_temp_reduction_pct=25))
    check(any("2000 ft2" in t for t in checks(floor, "FAIL")),
          "the high-temperature reduction below 2000 ft2 FAILS")
    few = run("design_area", dict(STD, hazard="light", density_mm_min=4.1,
                                  design_area_m2=50, area_per_sprinkler_m2=12,
                                  spacing_along_branch_m=3.4, system_type="wet",
                                  qr_reduction_pct=40))
    check(any("never takes the design area below five" in t for t in checks(few, "FAIL")),
          "a quick-response reduction leaving fewer than five sprinklers FAILS")
    hot_lh = run("design_area", dict(STD, hazard="light", density_mm_min=4.1,
                                     design_area_m2=140, area_per_sprinkler_m2=12,
                                     spacing_along_branch_m=3.4, high_temp_reduction_pct=25))
    check(hot_lh["status"] == "refused", "the high-temperature reduction at light hazard is "
          "refused - it is extra hazard's")
    after = run("design_area", dict(STD, hazard="light", density_mm_min=2.9,
                                    design_area_m2=280, area_per_sprinkler_m2=12,
                                    spacing_along_branch_m=3.4, system_type="wet",
                                    concealed_space="unsprinklered-combustible",
                                    qr_reduction_pct=20))
    check(any("after its adjustments, is below" in t for t in checks(after, "WARN")),
          "the unsprinklered concealed space rule is checked AFTER the other adjustments")

    for name, inputs in (("pipe_schedule", dict(en, hazard="OH1", material="steel",
                                                 sprinklers=10)),
                         ("obstruction", {"sprinkler_standard": "FM Global",
                                          "sprinkler_type": "standard-upright-pendent",
                                          "distance_to_obstruction_mm": 600})):
        got = run(name, inputs)
        check(got["status"] == "refused" and not got["ignored"],
              "%s under %s is refused, its own inputs not called ignored"
              % (name, inputs["sprinkler_standard"]))

    t_n = run("temperature_rating", dict(STD, ceiling_temperature_c=38))
    t_o = run("temperature_rating", dict(STD, ceiling_temperature_c=37))
    t_v = run("temperature_rating", dict(STD, ceiling_temperature_f=302))
    check((result(t_n, "Lowest allowed") or "").startswith("intermediate")
          and (result(t_o, "Lowest allowed") or "").startswith("ordinary")
          and (result(t_v, "Lowest allowed") or "").startswith("very extra high"),
          "38 C is past ordinary's 100 F (37.8 C) - intermediate; 37 C ordinary; 302 F past "
          "extra high's 300 F")
    t_en = run("temperature_rating", dict(en, ceiling_temperature_c=45))
    t_hot = run("temperature_rating", dict(en, ceiling_temperature_c=115))
    t_fm = run("temperature_rating", dict(fm, ceiling_temperature_c=45))
    check("79 C, yellow" in (result(t_en, "Nearest bulb") or "")
          and "none of the bulbs" in (result(t_hot, "Bulbs") or "")
          and t_fm["status"] == "refused",
          "EN 12845: 45 C + 30 C asks 75 C - the 79 C yellow bulb; 145 C reaches no bulb held; "
          "FM Global's guidance not read - refused")

    eq = run("equivalent_length", {"fittings": [{"fitting": "tee", "nominal": "2", "count": 2},
                                                {"fitting": "gate valve", "nominal": "DN150"}],
                                   "c_factor": 120, "schedule": "40"})
    check(near(first_number(result(eq, "Equivalent length")), 23 * F.M_PER_FT, 1e-3),
          "two 2 in tees and a 6 in gate valve, schedule 40 at C 120: 10 + 10 + 3 = 23 ft")
    eq10 = run("equivalent_length", {"fittings": [{"fitting": "tee", "nominal": "2"}],
                                     "c_factor": 150, "schedule": "10"})
    bore10 = F.lookup("steel_pipe_bores", "10")
    factor = (150 / 120.0) ** 1.85 * (54.76 / 52.48) ** 4.87
    check(bore10 is not None
          and near(first_number(result(eq10, "Equivalent length")), 10 * F.M_PER_FT * factor,
                   1e-3),
          "a 2 in tee in schedule 10 at C 150: 10 ft x (150/120)^1.85 x (54.76/52.48)^4.87")
    check(abs((100 / 120.0) ** 1.85 - 0.714) < 0.001,
          "(C/120)^1.85 at C 100 is 0.714 - the 0.713 multiplier remembered beside the chart")
    e45 = run("equivalent_length", {"fittings": [{"fitting": "45 elbow", "nominal": "2"}],
                                    "c_factor": 120, "schedule": "40"})
    check(e45["status"] == "refused" and "not read" in e45["refused"][0],
          "a 45 degree elbow, not read in the chart, is refused - never remembered")

    reels = run("hose_reels", {"reels_operating": 2, "reel_flow_lpm": 24,
                               "reel_pressure_bar": 2, "height_m": 30, "duration_min": 15,
                               "fire_authority": "QCDD"})
    check(near(first_number(result(reels, "Flow")), 48.0, 1e-9)
          and near(first_number(result(reels, "Pressure at the source")),
                   2 + 30 * F.BAR_PER_M, 1e-3)
          and any("TWO hoses" in t for t in checks(reels, "WARN")),
          "two reels at 24 L/min: 48 L/min, 2 bar + 30 m of rise at the source, and QCDD's "
          "two hoses said")

    room = [[0, 0], [10000, 0], [10000, 10000], [0, 10000]]
    pts = [{"id": "A", "x": 0, "y": 0}, {"id": "B", "x": 10000, "y": 10000}]
    one = run("coverage_check", {"outline_mm": room, "points_mm": pts, "radius_m": 12})
    two = run("coverage_check", {"outline_mm": room, "points_mm": pts, "radius_m": 12,
                                 "reached_by": 2})
    many = run("coverage_check", {"outline_mm": room, "points_mm": pts, "radius_m": 12,
                                  "reached_by": 3})
    check(checks(one, "OK") and not checks(one, "FAIL") and checks(two, "FAIL")
          and many["status"] == "refused",
          "two opposite corners reach every point within 12 m once, not twice - the far "
          "corner is 14.1 m from its second; three asked of two devices is refused")

    det = run("detector_layout", {"room_length_m": 20, "room_width_m": 12,
                                  "detector_type": "smoke", "radius_m": 7.5,
                                  "min_wall_distance_m": 0.5, "ceiling_height_m": 11,
                                  "max_ceiling_height_m": 10.5, "fire_authority": "QCDD"})
    check(first_number(result(det, "Detectors")) == 3
          and first_number(result(det, "Farthest point")) <= 7.5
          and any("ceiling against" in t for t in checks(det, "FAIL")),
          "BS 5839-1's 7.5 m radius covers a 20 x 12 m ceiling with 3 smoke detectors, and an "
          "11 m ceiling against 10.5 m FAILS")
    both = run("detector_layout", {"room_length_m": 20, "room_width_m": 12,
                                   "detector_type": "smoke", "radius_m": 7.5, "spacing_m": 9.1,
                                   "max_wall_distance_m": 4.55})
    check(both["status"] == "refused", "a radius AND a spacing is refused - one decides")
    worst = []
    for length, width, r in ((20, 12, 7.5), (31, 9, 5.3), (7, 7, 6.4), (45, 30, 7.5)):
        rows, cols = F.grid(length, width, rmax=r)
        fewer = [(a_, b_) for a_ in range(1, rows * cols) for b_ in range(1, rows * cols)
                 if a_ * b_ < rows * cols
                 and math.hypot(length / b_, width / a_) / 2.0 <= r + 1e-9]
        worst.append(math.hypot(length / cols, width / rows) / 2.0 <= r + 1e-9 and not fewer)
    check(all(worst), "every radius layout keeps the farthest point within the radius, and no "
          "grid with fewer detectors does (%d rooms)" % len(worst))

    bs = run("extinguishers", {"floor_area_m2": 1000, "rating_a": 13, "rating_per_m2": 0.065,
                               "min_total_rating": 26, "min_count": 2, "fire_authority": "none"})
    small = run("extinguishers", {"floor_area_m2": 40, "rating_a": 13, "rating_per_m2": 0.065,
                                  "min_total_rating": 26, "min_count": 1,
                                  "fire_authority": "none"})
    mixed = run("extinguishers", {"floor_area_m2": 40, "rating_a": 13, "rating_per_m2": 0.065,
                                  "area_per_a_m2": 100, "max_area_m2": 1000})
    check(first_number(result(bs, "Extinguishers")) == 5
          and first_number(result(small, "Extinguishers")) == 2
          and mixed["status"] == "refused",
          "BS 5306-8: 1000 m2 x 0.065 = 65A, five 13A; 40 m2 still 26A, two; the two methods "
          "mixed are refused - their ratings are different scales")

    store = run("water_storage", {"sprinkler_flow_lpm": 1500, "duration_min": 60,
                                  "fire_authority": "QCDD", "sprinkler_standard": "NFPA 13-2022"})
    said = " ".join(checks(store, "WARN"))
    check("two compartments of 45.00 m3" in said and "250.0 L/min" in said,
          "QCDD's tank: 90 m3 as two compartments of 45 m3, refilled in 6 h at 250 L/min - "
          "said, as reported")

    fmt = F.REFERENCES["fm_criteria"]
    cf = fmt["columns"]
    fm_ok = all((r[cf.index("density gpm/ft2")] == F.NOT_HELD
                 or abs(r[cf.index("density mm/min")]
                        - r[cf.index("density gpm/ft2")] * F.MM_MIN_PER_GPM_FT2) < 0.005)
                and (r[cf.index("area wet ft2")] == F.NOT_HELD
                     or abs(r[cf.index("area wet m2")] - r[cf.index("area wet ft2")]
                            * F.M2_PER_FT2) < 0.05)
                and abs(r[cf.index("hose L/min")] - r[cf.index("hose gpm")]
                        * F.LPM_PER_GPM) < 0.05 for r in fmt["rows"])
    check(fm_ok, "FM Global's SI is its US figures converted, every row")
    enc = F.REFERENCES["en12845_criteria"]
    ce = enc["columns"]
    en_ok = all(abs(r[ce.index("area dry m2")] - 1.25 * r[ce.index("area wet m2")]) < 1e-9
                for r in enc["rows"] if isinstance(r[ce.index("area dry m2")], (int, float)))
    check(en_ok, "every EN 12845 dry area held is its wet area x 1.25 - the table read, not a "
          "rule applied")
    eqt = F.REFERENCES["equivalent_lengths"]["rows"]
    check(all(abs(r[3] - r[2] * F.M_PER_FT) < 0.0005 for r in eqt),
          "every equivalent length in metres is its feet converted")
    temps = F.REFERENCES["temperature_ratings"]["rows"]
    check(all(abs(r[2] - (r[1] - 32) * 5 / 9.0) < 0.05 for r in temps)
          and [r[1] for r in temps] == sorted(r[1] for r in temps),
          "every most ceiling temperature in C is its F converted, and they only rise")
    cvals = [r[1] for t in ("c_factors_nfpa", "c_factors_en12845")
             for r in F.REFERENCES[t]["rows"] if r[1] != F.NOT_HELD]
    check(cvals and all(100 <= c <= 150 for c in cvals),
          "every C held is between 100 and 150, and the conflicting ones are not held")
    print()


def numbers_as_data():
    print("10. the hydraulic answer carries its own numbers as data (docs/46)")
    tree = run("hydraulic", dict(STD, source="S", c_factor=120, min_pressure_bar=0.5,
                                 density_mm_min=4.1,
                                 nodes=[{"id": "S", "elevation_m": 0},
                                        {"id": "T", "elevation_m": 3},
                                        {"id": "A", "elevation_m": 3, "k_lpm_bar": 80,
                                         "area_per_sprinkler_m2": 12},
                                        {"id": "B", "elevation_m": 3, "k_lpm_bar": 80,
                                         "area_per_sprinkler_m2": 12}],
                                 pipes=[{"from": "S", "to": "T", "length_m": 3,
                                         "bore_mm": 52.48},
                                        {"from": "T", "to": "A", "length_m": 4,
                                         "bore_mm": 26.64},
                                        {"from": "T", "to": "B", "length_m": 7,
                                         "bore_mm": 26.64}]))
    d = tree["data"]
    check(tree["status"] == "ok" and d["governing"] == "B" and d["source"] == "S",
          "the governing head and the source are in data - the farther head governs")
    check(near(d["demand_lpm"], first_number(result(tree, "Sprinkler demand")), 1e-3)
          and near(d["source_bar"], first_number(result(tree, "Pressure at S")), 1e-3),
          "data's demand and source pressure are the numbers the results print")
    into_t = sum(p["q_lpm"] for p in d["pipes"] if p["to"] == "T")
    out_t = sum(p["q_lpm"] for p in d["pipes"] if p["from"] == "T")
    check(near(into_t, out_t, 1e-6) and near(out_t, d["demand_lpm"], 1e-6),
          "pipe flows in data balance at the tee and equal the demand")
    b = d["heads"]["B"]
    check(near(b["p_bar"], b["p_req_bar"], 1e-6) and b["q_lpm"] >= b["q_req_lpm"] - 1e-9
          and near(d["pressures"]["A"], d["heads"]["A"]["p_bar"], 1e-12),
          "the governing head runs at exactly its need; every head's pressure is in data")
    print()


def water_and_spacing_as_data():
    print("11. the fire water and spacing answers carry their numbers; offers and fields by name")
    QC = dict(STD, fire_authority="QCDD")
    sp = run("standpipe", dict(QC, standpipe_class="i", standpipes=1, first_flow_lpm=1893,
                               outlet_pressure_bar=6.9, height_m=30))
    check(sp["status"] == "ok" and near(sp["data"]["flow_lpm"], 1893, 1e-12)
          and near(sp["data"]["source_bar"], first_number(result(sp, "Pressure at the source")),
                   1e-3),
          "standpipe: data's flow and source pressure are the numbers its results print")
    hr = run("hose_reels", dict(QC, reels_operating=2, reel_flow_lpm=30, reel_pressure_bar=2,
                                height_m=10, duration_min=60))
    check(hr["status"] == "ok" and hr["data"]["flow_lpm"] == 60
          and near(hr["data"]["volume_m3"], 3.6, 1e-12),
          "hose reels: flow, source pressure and water for the duration are in data")
    st = run("water_storage", dict(QC, sprinkler_flow_lpm=1000, duration_min=60,
                                   hose_allowance_lpm=950, unusable_volume_m3=5,
                                   other_demands=[{"name": "hose reels", "flow_lpm": 60,
                                                   "duration_min": 60}]))
    check(st["status"] == "ok" and near(st["data"]["effective_m3"], 120.6, 1e-12)
          and near(st["data"]["total_m3"], 125.6, 1e-12),
          "storage: the effective volume is every demand's flow over its own duration, plus "
          "the unusable volume in total")
    pump = run("fire_pump", dict(QC, rated_flow_lpm=2000, rated_pressure_bar=8,
                                 churn_pressure_bar=10, pressure_at_150_bar=6,
                                 demand_flow_lpm=2500, demand_pressure_bar=9,
                                 suction_pressure_bar=0))
    check(pump["status"] == "ok" and pump["data"]["ok"] is False
          and pump["data"]["at_demand_bar"] is not None and pump["data"]["at_demand_bar"] < 9,
          "pump: a pump short of the demand says so in data as well as in its FAIL")
    ws = run("water_supply", {"supply_static_bar": 6, "supply_residual_bar": 4,
                              "supply_test_flow_lpm": 3000, "demand_flow_lpm": 1500,
                              "demand_pressure_bar": 4})
    check(ws["status"] == "ok" and near(ws["data"]["margin_bar"],
                                        ws["data"]["at_demand_bar"] - 4, 1e-12),
          "water supply: the pressure the test gives at the demand, and the margin, in data")
    room = run("sprinkler_spacing", dict(STD, hazard="light", branch_axis="x",
                                         outline_mm=[[0, 0], [6000, 0], [6000, 4000], [0, 4000]],
                                         sprinklers=[{"id": "a", "x": 1500, "y": 1000},
                                                     {"id": "b", "x": 4500, "y": 1000},
                                                     {"id": "c", "x": 1500, "y": 3000},
                                                     {"id": "d", "x": 4500, "y": 3000}],
                                         max_spacing_m=4.6, max_area_m2=20.9,
                                         max_wall_distance_m=2.3))
    heads = room["data"]["heads"]
    check(room["status"] == "ok" and sorted(heads) == ["a", "b", "c", "d"]
          and all(h["s_m"] == 3.0 and h["l_m"] == 2.0 and not h["fails"] for h in heads.values()),
          "spacing: each head's S, L and its failures are in data - 3 m by 2 m, none failing")
    offers = F.spacing_offers("OH1")
    check("ordinary hazard" in (offers["max_spacing_m"] or "").lower()
          and "light hazard" not in (offers["max_spacing_m"] or "").lower(),
          "the spacing offers are the class's own figures")
    both = run("combined_demand", {"demands": [
        {"name": "sprinklers", "flow_lpm": 1000, "pressure_bar": 4},
        {"name": "standpipes", "flow_lpm": 1893, "pressure_bar": 9.8}]})
    check(both["status"] == "ok" and both["data"]["flow_lpm"] == 2893
          and both["data"]["pressure_bar"] == 9.8 and both["data"]["governing"] == "standpipes"
          and any("not solved together" in t for t in checks(both, "WARN")),
          "combined demand: the flows add, the highest pressure governs, and it says the "
          "paths are not solved together")
    off = F.fields("hose_reels")
    check(any(f["input"] == "duration_min" and f["offer"] for f in off),
          "fields() carries the offer of an optional input too")
    check(F.family_said("BS EN 12845") == "en" and F.family_said("NFPA 13-2022") == "nfpa",
          "a standard as a person says it is read through standard_value before its family")
    pump_fields = dict((f["input"], f["required"]) for f in F.fields("fire_pump"))
    check(pump_fields.get("rated_flow_lpm") is True and pump_fields.get("demand_flow_lpm") is False,
          "fields() derives what a calculation reads, required or not, by running it on nothing")
    print()


def layout_any_shape():
    print("12. a layout for a room of any shape (docs/47 s4)")
    lim = dict(STD, max_spacing_m=4.6, max_area_m2=21, max_wall_distance_m=2.3,
               min_wall_distance_m=0.1, branch_angle_deg=0)
    rect = [[0, 0], [6000, 0], [6000, 4000], [0, 4000]]
    got = run("sprinkler_layout_room", dict(lim, outline_mm=rect))
    same = run("sprinkler_layout", dict(STD, room_length_m=6, room_width_m=4, max_spacing_m=4.6,
                                        max_area_m2=21, max_wall_distance_m=2.3))
    check(got["status"] == "ok" and got["data"]["passed"]
          and len(got["data"]["points"]) == first_number(result(same, "Sprinklers")),
          "a 6 x 4 m rectangle gets as many heads as `sprinkler_layout` gives it")
    ell = [[0, 0], [10000, 0], [10000, 4000], [6000, 4000], [6000, 10000], [0, 10000]]
    got = run("sprinkler_layout_room", dict(lim, outline_mm=ell))
    pts = [(p["x_mm"], p["y_mm"]) for p in got["data"]["points"]]
    check(got["data"]["passed"] and pts
          and all(F.inside(ell, x, y) and F.wall_distance(ell, x, y) >= 100 - 1e-6
                  for x, y in pts),
          "an L (10 x 10 m less 4 x 6 m) passes with every head inside and off every wall")
    spaced = run("sprinkler_spacing", dict(STD, outline_mm=ell, branch_axis="x",
                                           sprinklers=[[x, y] for x, y in pts],
                                           max_spacing_m=4.6, max_area_m2=21,
                                           max_wall_distance_m=2.3, min_wall_distance_m=0.1))
    check(not checks(spaced, "FAIL"),
          "the layout's heads, checked again by `sprinkler_spacing` itself, fail nothing")
    r = math.radians(30)
    turned = [[x * math.cos(r) - y * math.sin(r) + 500, x * math.sin(r) + y * math.cos(r) + 700]
              for x, y in ell]
    got30 = run("sprinkler_layout_room", dict(lim, outline_mm=turned, branch_angle_deg=30))
    check(got30["data"]["passed"] and len(got30["data"]["points"]) == len(pts)
          and all(F.inside(turned, p["x_mm"], p["y_mm"]) for p in got30["data"]["points"]),
          "the same L turned 30 degrees, with the angle given, gets the same count, in the "
          "model's own coordinates")
    square = [[0, 0], [6000, 0], [6000, 6000], [0, 6000]]
    column = [[2500, 2500], [3500, 2500], [3500, 3500], [2500, 3500]]
    got = run("sprinkler_layout_room", dict(lim, outline_mm=square, holes_mm=[column]))
    check(got["data"]["passed"]
          and not any(F.inside(column, p["x_mm"], p["y_mm"]) for p in got["data"]["points"]),
          "no head is put in a column")
    got = run("sprinkler_layout_room", dict(lim, outline_mm=[[0, 0], [20000, 0], [20000, 1200],
                                                             [0, 1200]]))
    check(got["data"]["passed"] and got["data"]["lines"] == 1,
          "a 1.2 x 20 m corridor passes on one branch line")
    narrow = run("sprinkler_layout_room", dict(lim, outline_mm=[[0, 0], [20000, 0],
                                                                [20000, 1000], [0, 1000]],
                                               max_wall_distance_m=0.5, min_spacing_m=0.3))
    check(narrow["data"]["passed"] and len(narrow["data"]["points"]) == 20,
          "a tight wall limit is searched far enough: a 20 x 1 m room at 0.5 m from the wall "
          "passes on 20 heads (the Codex review of #418)")
    got = run("sprinkler_layout_room", dict(lim, outline_mm=rect, max_wall_distance_m=0.5,
                                            min_wall_distance_m=0.6))
    check(not got["data"]["passed"] and checks(got, "FAIL")
          and "NOT TO BE PLACED" in " ".join(checks(got, "FAIL")),
          "limits nothing can meet: the closest layout is shown failing and never offered for "
          "placing")
    asked = run("sprinkler_layout_room", dict(STD, outline_mm=ell))
    angle = [m for m in asked["missing"] if m["input"] == "branch_angle_deg"]
    wall = [m for m in asked["missing"] if m["input"] == "min_wall_distance_m"]
    check(asked["status"] == "missing" and angle and "offered, never assumed"
          in angle[0]["reference"] and wall,
          "the branch angle is asked with the longest wall's offered, and the least wall "
          "distance is required")
    print()


def main():
    physics()
    solver()
    worked_examples()
    tables_agree()
    no_design_value()
    never_undersizes()
    refusals()
    reaches_nothing_and_keeps_once()
    beyond_nfpa()
    numbers_as_data()
    water_and_spacing_as_data()
    layout_any_shape()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1
    print("PASSED - the fire protection engine's arithmetic agrees with first principles,")
    print("an independent solver and its own tables; it undersizes nothing, holds no")
    print("figure it could not check, and supplies no design value it was not given.")
    print()
    print("It proves nothing about a building. Every design value is the fire")
    print("consultant's, and no answer here has been checked against a real project.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
