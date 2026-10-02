# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
HVAC design, worked out before it is modelled - loads, air, ventilation, ducts,
diffusers, fans and chilled water - from the modeller's own design criteria.

    python brain/heron_hvac.py                      every calculation, and what each needs
    python brain/heron_hvac.py duct_size '{"flow_ls": 500, ...}'
    python brain/heron_hvac.py reference ventilation

docs/41-hvac-design.md is the reading of HAP, ASHRAE, SMACNA and the rest that
this file was written from, and says what each calculation is and is not.

WHY IT EXISTS
-------------
Heron could already MODEL ductwork - draw a run, fit its joints, size it from a
velocity, set a terminal's flow - and could not say what any of those numbers
should be. The fragments say so of themselves: SET_AIR_TERMINAL_FLOW "does not
work out what the flow should be", AUTO_SIZE_MEP "does not calculate load". The
owner, 2026-10-01: *"you have capability on the ducting ... but you are not
aware of the designing part"*. This is that part.

IT SUPPLIES NO DESIGN VALUE - D-33, AND THE SHAPE OF EVERY ANSWER FOLLOWS
-------------------------------------------------------------------------
A design criterion - a friction rate, a velocity limit, a supply temperature,
an outdoor-air rate, a people count - is an INPUT, every time. A calculation
missing one computes nothing and says what to ask for. Where a published
standard holds a figure for it, that figure is SHOWN beside the question with
its table and edition - offered, never applied. "ASHRAE 62.1 lists 2.5 L/s per
person for office space - confirm it" is a question; using it unasked would be
the plausible default D-33 calls the most dangerous thing Heron could offer.

What IS built in is what no designer chooses: constants of nature and
definitions (heron_psychro.py), the exact unit conversions, and the constants
that belong to a published METHOD - Colebrook's 3.7 and 2.51, Huebscher's 1.30,
the 0.25 m/s that defines a T50 throw. Choosing a method is a technical choice,
which D-33's own boundary leaves to Heron provided it says what it chose; every
answer carries a METHOD and a SOURCES section for exactly that.

A factor that would REDUCE a load - a lighting use factor, a diversity, a
safety margin taken off - is never invented either. Not given means not
applied, and the answer says so, so an omission errs toward the larger number.

TWO THINGS THE OWNER HAS ANSWERED, AND BOTH ARE SAID OUT LOUD
--------------------------------------------------------------
A supply diffuser's neck in an NC/RC 30 room is held to the owner's own
2.5 m/s, not the 2.2 ASHRAE's table prints (D-110): a figure the owner gave
once, applied only where its condition holds, named every time it is, and
given way to by any figure the modeller states. And a project's governing
standards - its 62.1 and 90.1 editions, its QCS edition, CIBSE beside ASHRAE -
are asked ONCE per project and kept for it (D-111). They never block a
calculation; until they are known, the checks that need them say so. The
caller keeps them and hands them back as `recorded`, so this file still reads
no store.

IT READS NO MODEL AND CHANGES NOTHING
-------------------------------------
Pure arithmetic over what it is handed: no Revit, no network, no file, no
package beyond the standard library, so it answers on a laptop on a plane. What
puts a result INTO the model is a fragment - SET_AIR_TERMINAL_FLOW,
SET_MEP_SIZE, PLACE_FAMILY_INSTANCES - and an answer names which, in an INTO
REVIT section. Resolving a number is not changing a model.

IT IS NOT HAP, AND EVERY LOAD ANSWER SAYS SO
--------------------------------------------
Carrier's HAP and ASHRAE's Radiant Time Series work hour by hour, delaying the
radiant part of each heat gain through the building's mass. The cooling load
here is a PEAK COMPONENT ESTIMATE: each gain at its own peak, summed, with no
storage and no time lag. That is larger than an hourly method's peak, never
smaller for the same inputs, and it is the right tool for checking a figure
and the wrong one for selecting a chiller. docs/41 says why at length.

EVERY ANSWER IS THE SAME SHAPE
------------------------------
run(name, inputs) returns a dict with a `status` of ok, missing, refused or
unknown; describe() turns it into the text a modeller reads. A key nobody
reads is named IGNORED rather than dropped - a mistyped `flow_lps` must not
look like an answer to `flow_ls`.
"""

import collections
import json
import math
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_psychro as PSY                                   # noqa: E402


# ---------------------------------------------------------------------------
# Exact conversions. Definitions, not measurements - never "improve" them.
# ---------------------------------------------------------------------------

LS_PER_CFM = 0.4719474432          # 1 ft3/min: 28.316846592 L / 60 s
W_PER_BTUH = 0.29307107017         # IT Btu: 1055.05585262 J / 3600 s
W_PER_TR = 12000 * W_PER_BTUH      # ton of refrigeration, 12 000 Btu/h
PA_PER_INWG = 248.84               # inch of water at 60 F, ASHRAE's HVAC convention
MS_PER_FPM = 0.00508               # 1 ft/min
LS_PER_USGPM = 3.785411784 / 60.0  # US gallon per minute
M_PER_FT = 0.3048
M2_PER_FT2 = M_PER_FT * M_PER_FT
W_M2K_PER_IP_U = W_PER_BTUH / (M2_PER_FT2 * 5.0 / 9.0)

DISCLAIMER = ("A design aid from published methods, by Heron's HVAC engine - "
              "DRAFT, and no engineer has signed it off. The engineer of record "
              "approves every design value (DISCLAIMER.md).")

NOT_HAP = ("PEAK COMPONENT ESTIMATE, NOT AN HOURLY SIMULATION: each gain is "
           "taken at its own peak and summed, with no thermal storage or time "
           "lag. An hourly method (ASHRAE RTS or heat balance, as in Carrier "
           "HAP, Trane TRACE or IES) gives a lower, time-aligned peak - use one "
           "to select plant.")


class Refused(ValueError):
    """An input that cannot be a design figure. Carries the sentence that says why."""


# ---------------------------------------------------------------------------
# Reading inputs. The ONLY way a calculation reads one.
# ---------------------------------------------------------------------------

class Inputs(object):
    """
    One dict of inputs - the top level, or one record of a list - and every
    name read from it.

    Every accessor records the name it read. That record is what lets an
    unread key be named IGNORED, and what lets catalogue() derive what each
    calculation needs by running it on nothing: the list a caller is shown is
    the list the code reads, with no second copy to drift.
    """

    def __init__(self, answer, prefix, data):
        self.answer = answer
        self.prefix = prefix
        self.data = data if isinstance(data, dict) else {}
        self.seen = set()
        answer.views.append(self)

    def name(self, key):
        return "%s.%s" % (self.prefix, key) if self.prefix else key

    def raw(self, key):
        self.seen.add(key)
        return self.data.get(key)

    def has(self, key):
        value = self.raw(key)
        return value is not None and value != "" and value != []

    def unread(self):
        return [self.name(k) for k in sorted(self.data) if k not in self.seen]

    def text(self, key):
        """An optional label - a zone's name, an element id. Never parsed, never required."""
        raw = self.raw(key)
        if raw is None or str(raw).strip() == "":
            return None
        return str(raw).strip()

    def _absent(self, key, unit, why, required, reference):
        if required:
            self.answer.need(self.name(key), unit, why, reference)
        elif not self.prefix:
            self.answer.optional_input(key, unit, why)
        return None

    def number(self, key, unit, why, low=None, high=None, required=True,
               reference=None, positive=False):
        """A number, or None having recorded why it is missing or unusable."""
        raw = self.raw(key)
        if raw is None or raw == "":
            return self._absent(key, unit, why, required, reference)
        try:
            value = _to_number(raw, self.name(key))
            _within(value, self.name(key), unit, low, high, positive)
        except Refused as why_not:
            self.answer.refuse(str(why_not))
            return None
        return value

    def integer(self, key, why, low=0, high=100000, required=True, reference=None):
        value = self.number(key, "count", why, low, high, required, reference)
        if value is None:
            return None
        if abs(value - round(value)) > 1e-9:
            self.answer.refuse("%s must be a whole number, got %s" % (self.name(key), _g(value)))
            return None
        return int(round(value))

    def word(self, key, why, required=True):
        raw = self.raw(key)
        if raw is None or str(raw).strip() == "":
            return self._absent(key, "text", why, required, None)
        return str(raw).strip()

    def choice(self, key, options, why, required=True, reference=None):
        raw = self.raw(key)
        if raw is None or raw == "":
            return self._absent(key, "one of: " + ", ".join(options), why, required, reference)
        word = str(raw).strip().lower().replace("_", "-").replace(" ", "-")
        if word not in options:
            self.answer.refuse("%s must be one of %s - got %r"
                               % (self.name(key), ", ".join(options), raw))
            return None
        return word

    def flag(self, key, why, required=False):
        raw = self.raw(key)
        if raw is None or raw == "":
            return self._absent(key, "true or false", why, required, None)
        if isinstance(raw, bool):
            return raw
        word = str(raw).strip().lower()
        if word in ("true", "yes", "y", "1"):
            return True
        if word in ("false", "no", "n", "0"):
            return False
        self.answer.refuse("%s must be true or false - got %r" % (self.name(key), raw))
        return None

    def numbers(self, key, unit, why, low=None, high=None, required=True,
                reference=None, positive=True):
        """A list of numbers - sizes, weights. Never a single number pretending."""
        raw = self.raw(key)
        if raw is None or raw == "" or raw == []:
            return self._absent(key, "list of " + unit, why, required, reference)
        if isinstance(raw, str):
            raw = [part for part in raw.replace(";", ",").split(",") if part.strip()]
        if not isinstance(raw, (list, tuple)):
            self.answer.refuse("%s must be a list of %s" % (self.name(key), unit))
            return None
        out = []
        for i, item in enumerate(raw):
            label = "%s[%d]" % (self.name(key), i)
            try:
                value = _to_number(item, label)
                _within(value, label, unit, low, high, positive)
            except Refused as why_not:
                self.answer.refuse(str(why_not))
                return None
            out.append(value)
        return out

    def words(self, key, why, required=True):
        """A list of labels - element ids, names. Kept as text, never parsed."""
        raw = self.raw(key)
        if raw is None or raw == "" or raw == []:
            return self._absent(key, "list of ids", why, required, None)
        if isinstance(raw, str):
            raw = [part for part in raw.replace(";", ",").split(",")]
        if not isinstance(raw, (list, tuple)):
            self.answer.refuse("%s must be a list" % self.name(key))
            return None
        out = [str(item).strip() for item in raw]
        if any(not item for item in out):
            self.answer.refuse("%s holds an empty entry" % self.name(key))
            return None
        if len(set(out)) != len(out):
            self.answer.refuse("%s names the same id twice" % self.name(key))
            return None
        return out

    def records(self, key, why, required=True):
        """A list of objects - zones, sections, walls - each read as Inputs of its own."""
        raw = self.raw(key)
        if raw is None or raw == "" or raw == []:
            return self._absent(key, "list of objects", why, required, None)
        if isinstance(raw, dict):
            raw = [raw]
        if not isinstance(raw, (list, tuple)) or not all(isinstance(r, dict) for r in raw):
            self.answer.refuse("%s must be a list of objects, each with named fields"
                               % self.name(key))
            return None
        return [Inputs(self.answer, "%s[%d]" % (self.name(key), i), r)
                for i, r in enumerate(raw)]

    def record(self, key, why, required=True):
        """One object - the people in a room, its lighting."""
        raw = self.raw(key)
        if raw is None or raw == "" or raw == {}:
            return self._absent(key, "object", why, required, None)
        if not isinstance(raw, dict):
            self.answer.refuse("%s must be an object with named fields" % self.name(key))
            return None
        return Inputs(self.answer, self.name(key), raw)

    def flow(self, why, prefix="flow", required=True, reference=None, water=False):
        """A flow in L/s, given in L/s, m3/h, m3/s, cfm (or US gpm for water) - exactly once."""
        units = _WATER_UNITS if water else _FLOW_UNITS
        given = []
        for suffix, factor, label in units:
            key = "%s_%s" % (prefix, suffix)
            raw = self.raw(key)
            if raw is not None and raw != "":
                given.append((key, raw, factor, label))
        if not given:
            alternatives = ", ".join("%s_%s" % (prefix, u[0]) for u in units[1:])
            return self._absent("%s_ls" % prefix, "L/s (or %s)" % alternatives,
                                why, required, reference)
        if len(given) > 1:
            self.answer.refuse("give %s once - %s were all given, and preferring one "
                               "would hide which was meant"
                               % (self.name(prefix), ", ".join(self.name(g[0]) for g in given)))
            return None
        key, raw, factor, label = given[0]
        try:
            value = _to_number(raw, self.name(key))
            _within(value, self.name(key), label, None, None, True)
        except Refused as why_not:
            self.answer.refuse(str(why_not))
            return None
        ls = value * factor
        if ls > 1.0e6:
            self.answer.refuse("%s is %s L/s - more than any building system carries; "
                               "check the unit" % (self.name(key), _g(ls)))
            return None
        return ls

    def power_w(self, why, prefix="load", required=True, reference=None, signed=False):
        """A heat flow in W, given in W, kW, TR or Btu/h - exactly once."""
        given = []
        for suffix, factor in (("w", 1.0), ("kw", 1000.0), ("tr", W_PER_TR),
                               ("btuh", W_PER_BTUH)):
            key = "%s_%s" % (prefix, suffix)
            raw = self.raw(key)
            if raw is not None and raw != "":
                given.append((key, raw, factor))
        if not given:
            return self._absent("%s_w" % prefix, "W (or %s_kw, %s_tr, %s_btuh)"
                                % (prefix, prefix, prefix), why, required, reference)
        if len(given) > 1:
            self.answer.refuse("give %s once - %s were all given"
                               % (self.name(prefix), ", ".join(self.name(g[0]) for g in given)))
            return None
        key, raw, factor = given[0]
        try:
            value = _to_number(raw, self.name(key))
            if not signed:
                _within(value, self.name(key), "", 0, None, False)
        except Refused as why_not:
            self.answer.refuse(str(why_not))
            return None
        return value * factor

    def humidity(self, prefix, why, required=True):
        """
        Exactly one humidity reading for one air state, as keyword arguments
        for heron_psychro.state(): <prefix>rh_pct, <prefix>wet_bulb_c,
        <prefix>dew_point_c or <prefix>humidity_ratio_g_kg.
        """
        options = (("rh_pct", "rh_pct", 1.0, 0, 100),
                   ("wet_bulb_c", "twb_c", 1.0, -60, 90),
                   ("dew_point_c", "tdp_c", 1.0, -90, 90),
                   ("humidity_ratio_g_kg", "w", 0.001, 0, 200))
        given = [(key, arg, scale, low, high) for key, arg, scale, low, high in options
                 if self.has(prefix + key)]
        if not given:
            if required:
                self.answer.need(self.name(prefix + "rh_pct"),
                                 "%% (or %swet_bulb_c, %sdew_point_c, %shumidity_ratio_g_kg)"
                                 % (prefix, prefix, prefix), why)
            elif not self.prefix:
                self.answer.optional_input(prefix + "rh_pct", "%", why)
            return None
        if len(given) > 1:
            self.answer.refuse("give one humidity reading for %s - %s were all given, "
                               "and two readings usually disagree"
                               % (self.name(prefix.rstrip("_") or "the air"),
                                  ", ".join(self.name(prefix + g[0]) for g in given)))
            return None
        key, arg, scale, low, high = given[0]
        value = self.number(prefix + key, key.split("_")[-1], why, low, high)
        if value is None:
            return None
        return {arg: value * scale}

    def pressure_kpa(self, why):
        """Barometric pressure from altitude_m or pressure_kpa - never assumed."""
        has_z, has_p = self.has("altitude_m"), self.has("pressure_kpa")
        if has_z and has_p:
            self.answer.refuse("give altitude_m or pressure_kpa, not both - they would "
                               "usually disagree, and preferring one would hide it")
            return None
        if not has_z and not has_p:
            self.answer.need(self.name("altitude_m"), "m above sea level (or pressure_kpa)", why,
                             "0 for sea level - say so rather than leave it out; air at "
                             "1000 m is about 11 % less dense than at sea level")
            return None
        if has_z:
            z = self.number("altitude_m", "m", why, -500, 6000)
            return None if z is None else PSY.pressure_at_altitude(z)
        return self.number("pressure_kpa", "kPa", why, 50, 110)


def _view(thing):
    """An Inputs view, whether handed one or handed the Answer whose top level it is."""
    return thing.top if isinstance(thing, Answer) else thing


def _to_number(raw, name):
    if isinstance(raw, bool):
        raise Refused("%s must be a number, got %r" % (name, raw))
    if isinstance(raw, (int, float)):
        value = float(raw)
    else:
        try:
            value = float(str(raw).strip())
        except ValueError:
            raise Refused("%s must be a number, got %r" % (name, raw))
    if value != value or value in (float("inf"), float("-inf")):
        raise Refused("%s is not a finite number" % name)
    return value


def _within(value, name, unit, low, high, positive):
    if positive and value <= 0:
        raise Refused("%s must be more than zero, got %s %s" % (name, _g(value), unit))
    if low is not None and value < low:
        raise Refused("%s %s %s is below %s %s - not a design figure Heron will "
                      "calculate from" % (name, _g(value), unit, _g(low), unit))
    if high is not None and value > high:
        raise Refused("%s %s %s is above %s %s - not a design figure Heron will "
                      "calculate from" % (name, _g(value), unit, _g(high), unit))


_FLOW_UNITS = (("ls", 1.0, "L/s"), ("m3h", 1.0 / 3.6, "m3/h"),
               ("m3s", 1000.0, "m3/s"), ("cfm", LS_PER_CFM, "cfm"))
_WATER_UNITS = (("ls", 1.0, "L/s"), ("m3h", 1.0 / 3.6, "m3/h"),
                ("usgpm", LS_PER_USGPM, "US gpm"))


# ---------------------------------------------------------------------------
# The answer every calculation fills in
# ---------------------------------------------------------------------------

class Answer(object):
    """One calculation's answer, built while it runs."""

    def __init__(self, name, inputs, recorded=None):
        self.name = name
        self.views = []
        self.top = Inputs(self, "", inputs if isinstance(inputs, dict) else {})
        # The project's governing standards as RECORDED for it, handed in by
        # the caller (D-111) - this file reads no store. What this answer used
        # goes in `standards`, with where each came from; what is still not
        # known goes in `ask_once`, which is a question and never a blocker.
        self.recorded = recorded if isinstance(recorded, dict) else {}
        self.standards = collections.OrderedDict()
        self.ask_once = []       # dicts: input, unit, why
        self.missing = []        # dicts: input, unit, why, reference
        self.refused = []        # sentences
        self.results = []        # (label, text)
        self.tables = []         # dicts: title, columns, rows
        self.checks = []         # (level, text) - OK / WARN / FAIL
        self.method = []
        self.sources = []
        self.assumed = []
        self.next = []
        self.optional = []       # (input, unit, why) - for the catalogue
        self.csv = None

    def __getattr__(self, name):
        # The top-level accessors, so a calculation reads a.number(...).
        if name in ("number", "integer", "word", "choice", "flag", "numbers",
                    "words", "records", "record", "flow", "power_w",
                    "humidity", "pressure_kpa", "has", "raw"):
            return getattr(self.top, name)
        raise AttributeError(name)

    def incomplete(self):
        return bool(self.missing or self.refused)

    def need(self, name, unit, why, reference=None):
        if any(m["input"] == name for m in self.missing):
            return
        entry = {"input": name, "unit": unit, "why": why}
        if reference:
            entry["reference"] = reference
        self.missing.append(entry)

    def optional_input(self, name, unit, why):
        if not any(o[0] == name for o in self.optional):
            self.optional.append((name, unit, why))

    def refuse(self, sentence):
        if sentence not in self.refused:
            self.refused.append(sentence)

    def result(self, label, text):
        self.results.append((label, text))

    def table(self, title, columns, rows):
        self.tables.append({"title": title, "columns": list(columns),
                            "rows": [list(r) for r in rows]})

    def check(self, level, text):
        self.checks.append((level, text))

    def cite(self, *sources):
        for s in sources:
            if s not in self.sources:
                self.sources.append(s)

    def uses(self, *lines):
        for line in lines:
            if line not in self.method:
                self.method.append(line)

    def assume(self, line):
        if line not in self.assumed:
            self.assumed.append(line)

    def into_revit(self, line):
        if line not in self.next:
            self.next.append(line)

    def ignored(self):
        out = []
        for view in self.views:
            out.extend(view.unread())
        return out


# ---------------------------------------------------------------------------
# Number formatting - what a modeller reads. ASCII only: a Windows console
# that cannot print a degree sign must still print the answer.
# ---------------------------------------------------------------------------

def _g(value):
    return ("%.6g" % value) if isinstance(value, float) else str(value)


def _f(value, places=1):
    if value is None:
        return "-"
    text = "%.*f" % (places, value)
    if text.startswith("-") and float(text) == 0:
        text = text[1:]
    if abs(value) >= 10000:
        whole, _, frac = text.partition(".")
        sign = "-" if whole.startswith("-") else ""
        whole = whole.lstrip("-")
        groups = []
        while len(whole) > 3:
            groups.insert(0, whole[-3:])
            whole = whole[:-3]
        groups.insert(0, whole)
        text = sign + ",".join(groups) + ("." + frac if frac else "")
    return text


def flow_text(ls):
    return "%s L/s  (%s m3/h, %s cfm)" % (_f(ls, 1), _f(ls * 3.6, 0), _f(ls / LS_PER_CFM, 0))


def power_text(w):
    return "%s W  (%s kW, %s TR, %s Btu/h)" % (_f(w, 0), _f(w / 1000.0, 2),
                                              _f(w / W_PER_TR, 2), _f(w / W_PER_BTUH, 0))


def pressure_text(pa):
    return "%s Pa  (%s in.wg)" % (_f(pa, 1), _f(pa / PA_PER_INWG, 3))


def friction_text(pa_m):
    return "%s Pa/m  (%s in.wg/100 ft)" % (_f(pa_m, 2), _f(pa_m * 100 * M_PER_FT / PA_PER_INWG, 3))


def velocity_text(ms):
    return "%s m/s  (%s fpm)" % (_f(ms, 2), _f(ms / MS_PER_FPM, 0))


def temp_text(c):
    return "%s C  (%s F)" % (_f(c, 1), _f(c * 9.0 / 5.0 + 32.0, 1))


def size_text(shape, d=None, w=None, h=None):
    if shape == "round":
        return "%s mm round" % _f(d, 0)
    if shape == "oval":
        return "%s x %s mm flat oval" % (_f(w, 0), _f(h, 0))
    return "%s x %s mm" % (_f(w, 0), _f(h, 0))


# ---------------------------------------------------------------------------
# Sources. Cited by document, edition and section - never quoted at length.
# ---------------------------------------------------------------------------

SRC_PSYCHRO = ("ASHRAE Handbook - Fundamentals (SI), Ch. 1 Psychrometrics - "
               "Hyland & Wexler saturation pressure (eqs. 5, 6), standard "
               "atmosphere (eq. 3), humidity ratio, enthalpy, specific volume "
               "and wet bulb (eqs. 20-35)")
SRC_LOADS = ("ASHRAE Handbook - Fundamentals (SI), Ch. 18 Nonresidential Cooling "
             "and Heating Load Calculations - heat gain by component, and the "
             "air-side relations q = m.cp.dt, q = m.dW.hfg, q = m.dh")
SRC_SOLAIR = ("ASHRAE Handbook - Fundamentals (SI), Ch. 18 - sol-air temperature "
              "te = to + (a/ho).Et - e.dR/ho")
SRC_STANDARD_AIR = ("ASHRAE Handbook - Fundamentals (SI), Ch. 18 - standard-air "
                    "coefficients qs = 1.23 Q dt, ql = 3010 Q dW, qt = 1.20 Q dh "
                    "(W, L/s)")
SRC_DUCT = ("ASHRAE Handbook - Fundamentals (SI), Ch. 21 Duct Design - "
            "Darcy-Weisbach friction, Colebrook friction factor, velocity "
            "pressure rho.V^2/2, Huebscher equivalent diameter for rectangular "
            "duct and Heyt & Diaz for flat oval")
SRC_6221 = ("ANSI/ASHRAE Standard 62.1 Ventilation and Acceptable Indoor Air "
            "Quality - Ventilation Rate Procedure, Section 6.2")
SRC_DIFFUSION = ("ASHRAE Handbook - Fundamentals (SI), Ch. 20 Space Air "
                 "Diffusion - throw, characteristic length and ADPI")
SRC_FAN = ("Fan power from first principles: air power = volume flow x fan "
           "total pressure; shaft power = air power / fan efficiency; input "
           "power = shaft power / (motor x drive efficiency)")
SRC_WATER = ("Water properties: IAPWS-95 (density, specific heat) and IAPWS "
             "2008 (viscosity) at 101.325 kPa; pipe friction by Darcy-Weisbach "
             "with the Colebrook friction factor (ASHRAE Handbook - "
             "Fundamentals Ch. 22 Pipe Sizing)")
SRC_UNITS = ("Exact conversion factors: international foot 0.3048 m, IT Btu "
             "1055.05585262 J, US gallon 3.785411784 L; inch of water at 60 F "
             "taken as 248.84 Pa")


# ---------------------------------------------------------------------------
# Air for a calculation that turns airflow into heat or into pressure
# ---------------------------------------------------------------------------

STANDARD_AIR_DENSITY = 1.204       # kg/m3 - dry air at 20 C, 101.325 kPa
# Viscosity of standard air, chosen so the Reynolds number is ASHRAE's own
# Re = 66.4 Dh V (Dh mm, V m/s): nu = 1 / 66 400 m2/s, mu = rho.nu.
STANDARD_AIR_VISCOSITY = STANDARD_AIR_DENSITY / 66400.0
STANDARD_SENSIBLE = 1.23           # W per (L/s . K)
STANDARD_LATENT = 3010.0           # W per (L/s . kg/kg)
STANDARD_TOTAL = 1.20              # W per (L/s . kJ/kg)


class Air(object):
    """The air one calculation's flow is made of, and where its numbers came from."""

    def __init__(self, density, viscosity, label, standard, dry_density=None, cp=None):
        self.density = density              # kg of moist air per m3
        self.viscosity = viscosity          # Pa.s
        self.label = label
        self.standard = standard
        self.dry_density = dry_density if dry_density is not None else density
        self.cp = cp                        # kJ/(kg dry air . K)

    def sensible_w_per_ls_k(self):
        """W per (L/s . K): ASHRAE's 1.23 for standard air, else rho_dry x cp."""
        if self.standard:
            return STANDARD_SENSIBLE
        return self.dry_density * self.cp


def read_air(view, why, temp_key="air_temp_c"):
    """
    ASHRAE standard air, or the caller's temperature at the caller's altitude.
    Never assumed: one of the two is asked for.
    """
    view = _view(view)
    standard = view.flag("standard_air", "true for ASHRAE standard air (20 C, "
                         "101.325 kPa) instead of a temperature and an altitude")
    if standard:
        if view.has(temp_key) or view.has("altitude_m") or view.has("pressure_kpa"):
            view.answer.refuse("standard_air was asked for AND a temperature or an "
                               "altitude was given - say which is meant")
            return None
        return Air(STANDARD_AIR_DENSITY, STANDARD_AIR_VISCOSITY,
                   "ASHRAE standard air - 1.204 kg/m3, Re = 66.4 Dh V (20 C, 101.325 kPa)",
                   True, cp=STANDARD_SENSIBLE / STANDARD_AIR_DENSITY)
    t = view.number(temp_key, "C", why + " (or standard_air: true)", -40, 80)
    p = view.pressure_kpa(why)
    g = view.number("air_humidity_ratio_g_kg", "g/kg", "humidity of that air - "
                    "dry air is used if it is not given", 0, 60, required=False)
    if t is None or p is None or view.answer.refused:
        return None
    w = 0.0 if g is None else g / 1000.0
    if g is None:
        view.answer.assume("no air humidity given, so dry air - a few tenths of a "
                           "percent on density, and on the side of more airflow in "
                           "a heat balance")
    try:
        v = PSY.specific_volume(t, w, p)
    except PSY.PsychroRangeError as why_not:
        view.answer.refuse(str(why_not))
        return None
    label = "%s at %s kPa%s" % (temp_text(t), _f(p, 2),
                                "" if g is None else ", %s g/kg" % _f(g, 2))
    return Air((1.0 + w) / v, PSY.air_viscosity(t), label, False,
               dry_density=1.0 / v, cp=PSY.humid_specific_heat(w))


# ---------------------------------------------------------------------------
# Friction - ASHRAE Fundamentals Ch. 21 (air) and Ch. 22 (water)
# ---------------------------------------------------------------------------

def friction_factor(re, rel_roughness):
    """
    Darcy friction factor. Colebrook solved by fixed-point iteration on
    1/sqrt(f), started from the Altshul-Tsal estimate ASHRAE Ch. 21 gives;
    laminar below Re 2300, where f = 64/Re.
    """
    if re <= 0:
        raise Refused("the Reynolds number must be positive")
    if re < 2300:
        return 64.0 / re
    f_prime = 0.11 * (rel_roughness + 68.0 / re) ** 0.25
    f = f_prime if f_prime >= 0.018 else 0.85 * f_prime + 0.0028
    x = 1.0 / math.sqrt(f)
    for _ in range(100):
        nxt = -2.0 * math.log10(rel_roughness / 3.7 + 2.51 * x / re)
        if abs(nxt - x) < 1e-13:
            x = nxt
            break
        x = nxt
    return 1.0 / (x * x)


def huebscher(width_mm, height_mm):
    """Equivalent round diameter of a rectangular duct - same flow, same friction."""
    a, b = float(width_mm), float(height_mm)
    return 1.30 * (a * b) ** 0.625 / (a + b) ** 0.25


def flat_oval_area_perimeter(major_mm, minor_mm):
    a, b = float(major_mm), float(minor_mm)
    return math.pi * b * b / 4.0 + b * (a - b), math.pi * b + 2.0 * (a - b)


def flat_oval_equivalent(major_mm, minor_mm):
    """Heyt & Diaz equivalent diameter of a flat oval duct: 1.55 A^0.625 / P^0.25."""
    area, perimeter = flat_oval_area_perimeter(major_mm, minor_mm)
    return 1.55 * area ** 0.625 / perimeter ** 0.25


def pipe_friction(flow_ls, diameter_mm, density, viscosity, roughness_mm):
    """
    Friction in a round conduit at a flow: velocity m/s, velocity pressure Pa,
    Reynolds number, Darcy friction factor, and Pa per metre.
    """
    d = diameter_mm / 1000.0
    area = math.pi * d * d / 4.0
    v = flow_ls / 1000.0 / area
    pv = density * v * v / 2.0
    re = density * v * d / viscosity
    f = friction_factor(re, roughness_mm / diameter_mm)
    return v, pv, re, f, f / d * pv


def duct_friction_at(flow_ls, shape, air, roughness_mm, d=None, w=None, h=None):
    """
    Friction for a duct of any shape at a flow. A rectangular or oval duct is
    computed as its equivalent round at the same flow - which is what the
    equivalent diameter is DEFINED to mean - while its velocity is its own,
    flow over its own area.
    """
    if shape == "round":
        de, area = d, math.pi * (d / 1000.0) ** 2 / 4.0
    elif shape == "oval":
        de = flat_oval_equivalent(w, h)
        area = flat_oval_area_perimeter(w, h)[0] / 1e6
    else:
        de, area = huebscher(w, h), w * h / 1e6
    _v_eq, _pv_eq, re, f, pa_m = pipe_friction(flow_ls, de, air.density, air.viscosity,
                                               roughness_mm)
    v = flow_ls / 1000.0 / area
    return {"de": de, "area": area, "velocity": v, "pv": air.density * v * v / 2.0,
            "re": re, "f": f, "pa_m": pa_m}


def read_roughness(view, why, water=False):
    """Absolute roughness in mm: roughness_mm, or a material named from the cited table."""
    view = _view(view)
    has_r, has_m = view.has("roughness_mm"), view.has("material")
    table = "pipe_roughness" if water else "duct_roughness"
    names = [row[0] for row in REFERENCES.get(table, {}).get("rows", [])]
    if has_r and has_m:
        view.answer.refuse("give roughness_mm or material, not both")
        return None, None
    if has_r:
        r = view.number("roughness_mm", "mm", why, 0, 20)
        return r, None if r is None else "%s mm, as given" % _g(r)
    if has_m:
        name = view.word("material", why)
        row = reference_lookup(table, name)
        if row is None:
            view.answer.refuse("material %r is not in the %s table - give roughness_mm, "
                               "or one of: %s" % (name, table, "; ".join(names)))
            return None, None
        return float(row[1]), "%s, %s mm (%s)" % (row[0], _g(float(row[1])),
                                                 REFERENCES[table]["source"])
    view.answer.need(view.name("roughness_mm"), "mm (or material)", why,
                     "name the material instead - %s" % "; ".join(names) if names else None)
    return None, None


def _bisect_up(f, low, high, steps=200):
    """Smallest x in [low, high] with f(x) True, for f False below it and True above."""
    if not f(high):
        return None
    for _ in range(steps):
        mid = 0.5 * (low + high)
        if f(mid):
            high = mid
        else:
            low = mid
        if high - low < 1e-7:
            break
    return high


# ---------------------------------------------------------------------------
# Reference tables - SHOWN when asked, OFFERED beside a missing input, never
# applied. Each table carries its citation; docs/41 records how every value
# was checked, and a value nobody could check is not here.
# ---------------------------------------------------------------------------

REFERENCES = collections.OrderedDict()


def _ref(table, title, source, columns, rows, note=None):
    REFERENCES[table] = {"title": title, "source": source,
                         "columns": list(columns), "rows": [list(r) for r in rows],
                         "note": note}


def reference_lookup(table, key):
    """The row of a reference table whose first column is `key`, or None."""
    data = REFERENCES.get(table)
    if not data or key is None:
        return None
    wanted = " ".join(str(key).strip().lower().replace("_", " ").replace("-", " ").split())
    for row in data["rows"]:
        name = " ".join(str(row[0]).strip().lower().replace("_", " ").replace("-", " ").split())
        if name == wanted:
            return row
    return None


def offer(table, key, column, unit):
    """The sentence offering ONE reference value beside a missing input, or None."""
    data = REFERENCES.get(table)
    row = reference_lookup(table, key)
    if not data or row is None or column not in data["columns"]:
        return None
    value = row[data["columns"].index(column)]
    if value in (None, ""):
        return None
    return ("%s gives %s %s for %s - offer it to the modeller, do not assume it"
            % (data["source"], _g(value), unit, row[0]))


def offer_table(table):
    """The sentence pointing at a whole reference table, or None if Heron holds none."""
    data = REFERENCES.get(table)
    if not data:
        return None
    return ("see `reference %s` - %s (%s); offer a row, do not assume one"
            % (table, data["title"], data["source"]))


def reference_rows(table, key):
    """Every row of a reference table whose first column is `key` - one per load, say."""
    data = REFERENCES.get(table)
    if not data or key is None:
        return []
    wanted = " ".join(str(key).strip().lower().replace("_", " ").replace("-", " ").split())
    return [row for row in data["rows"]
            if " ".join(str(row[0]).strip().lower().replace("_", " ").replace("-", " ")
                        .split()) == wanted]


# --- the reference data ------------------------------------------------------
#
# docs/41 s9 records, table by table, what each value was checked against. In
# short: the 62.1 rates and the 55 limits were read in the 2022/2023 text and
# cross-checked against independent datasets; the ADPI rows were read through
# manufacturers' reproductions of ASHRAE's table and only the rows whose
# numbers came back from a search that did not contain them are here. A row
# nobody could check is left out rather than marked.

_ref("duct_roughness", "Absolute roughness of duct material",
     "ASHRAE Handbook - Fundamentals, Ch. 21 Duct Design, Table 1",
     ("material", "roughness mm", "category"),
     [("galvanized steel", 0.09, "medium smooth - the basis of ASHRAE's friction chart")],
     note="only the row checked from two sources is held. Flexible duct fully extended is "
          "1.0 to 4.6 mm, which is a range to choose from, not a value - give roughness_mm "
          "for it, and for anything else.")

_ref("duct_design_guidance", "Maximum duct velocity for a room's acoustic criterion",
     "ASHRAE Handbook - HVAC Applications, Noise and Vibration Control, "
     "maximum recommended duct airflow velocities",
     ("main duct location", "design RC(N)", "rectangular m/s", "circular m/s"),
     [("in a shaft or above a drywall ceiling", 45, 17.8, 25.4),
      ("in a shaft or above a drywall ceiling", 35, 12.7, 17.8),
      ("in a shaft or above a drywall ceiling", 25, 8.6, 12.7),
      ("above a suspended acoustic ceiling", 45, 12.7, 22.9),
      ("above a suspended acoustic ceiling", 35, 8.9, 15.2),
      ("above a suspended acoustic ceiling", 25, 6.1, 10.2),
      ("within the occupied space", 45, 10.2, 19.8),
      ("within the occupied space", 35, 7.4, 13.2),
      ("within the occupied space", 25, 4.8, 8.6)],
     note="MAIN ducts; branches about 80 % of these and final runouts to outlets 50 % or "
          "less. The 25 row is RC 25 and below. For an equal-friction rate there is no "
          "ASHRAE figure held here: 0.08 to 0.10 in.wg per 100 ft (0.65 to 0.82 Pa/m) is "
          "commonly quoted for low-pressure ductwork, from secondary sources only.")

_ref("smacna_pressure_classes", "Duct construction pressure classes",
     "ANSI/SMACNA HVAC Duct Construction Standards - Metal and Flexible, 3rd ed.",
     ("class", "Pa", "in.wg"),
     [("+/-125 Pa (1/2 in.wg)", 125, 0.5), ("+/-250 Pa (1 in.wg)", 250, 1),
      ("+/-500 Pa (2 in.wg)", 500, 2), ("+/-750 Pa (3 in.wg)", 750, 3),
      ("+/-1000 Pa (4 in.wg)", 1000, 4), ("+/-1500 Pa (6 in.wg)", 1500, 6),
      ("+/-2500 Pa (10 in.wg)", 2500, 10)],
     note="a class is chosen from the static pressure the duct runs at. ASHRAE 90.1's "
          "interpretation IC 90.1-2013-14 requires Seal Class A at every pressure class, "
          "and 90.1 sets leakage class 4 for ducts tested above 3 in.wg (750 Pa).")

_ref("round_duct_sizes", "Round duct nominal (inside) diameters, mm", "EN 1506:2007",
     ("series", "diameters mm"),
     [("recommended", "63, 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000, 1250"),
      ("additional", "150, 300, 355, 450, 560, 710, 900, 1120")],
     note="read in two independent transcriptions, not the standard itself. A project's "
          "own size table - the one in Revit's Mechanical Settings - is the list to size "
          "to, or Revit snaps the size to its nearest.")


_ref("duct_insulation", "Minimum duct insulation, as installed",
     "ANSI/ASHRAE/IES 90.1-2022 Table 6.8.2",
     ("duct and climate zone", "exterior", "unconditioned space", "indirectly conditioned space"),
     [("supply and return, heating and cooling, zones 0 to 4", "R-8 (RSI 1.41)", "R-6 (RSI 1.06)",
       "R-1.9 (RSI 0.33)"),
      ("supply and return, cooling only, zones 0 to 6", "R-8 (RSI 1.41)", "R-6 (RSI 1.06)",
       "R-1.9 (RSI 0.33)"),
      ("supply and return, heating and cooling, zones 5 to 8", "R-12 (RSI 2.11)",
       "R-6 (RSI 1.06)", "R-1.9 (RSI 0.33)")],
     note="unconditioned space includes attics above insulated ceilings, parking garages and "
          "crawl spaces; indirectly conditioned includes return plenums, where a RETURN duct "
          "needs none. Doha works out as climate zone 0B from ASHRAE 169's definitions - "
          "derived, not looked up in the station list. R-values are the insulation alone.")

_ref("fan_power_limits", "Fan power and specific fan power limits",
     "ASHRAE 90.1-2022 Table 6.5.3.1-1; UK Approved Document L 2021 Vol 2 Table 6.9",
     ("basis", "limit"),
     [("90.1 Option 1, constant volume", "motor nameplate hp <= 0.0011 x cfm "
       "(about 1.74 W of nameplate per L/s)"),
      ("90.1 Option 1, variable volume", "hp <= 0.0015 x cfm (about 2.37 W per L/s)"),
      ("90.1 Option 2, constant volume", "fan bhp <= 0.00094 x cfm + A "
       "(about 1.49 W shaft per L/s, plus A)"),
      ("90.1 Option 2, variable volume", "fan bhp <= 0.0013 x cfm + A "
       "(about 2.05 W shaft per L/s, plus A)"),
      ("Part L, central balanced with heating and cooling, new", "SFP 2.0 W per L/s"),
      ("Part L, central balanced with heating only, new", "SFP 1.9 W per L/s"),
      ("Part L, all other central balanced, new", "SFP 1.5 W per L/s"),
      ("Part L, zonal supply, fan remote from the zone, new", "SFP 1.1 W per L/s"),
      ("Part L, fan coil unit, rating-weighted average", "SFP 0.4 W per L/s"),
      ("Part L, kitchen extract, fan remote, with grease filter", "SFP 1.0 W per L/s")],
     note="90.1 applies to a system above 5 hp; A is the sum of each pressure-drop credit "
          "(Table 6.5.3.1-2) x its cfm / 4131. Part L's SFP is the fans' electrical input "
          "over the larger of supply and extract airflow; add 1.0 for a HEPA filter. Which "
          "of these binds a Qatari project is not settled by anything read here.")

_ref("pipe_roughness", "Absolute roughness of pipe material",
     "Moody (1944), as reproduced in pipe-flow references",
     ("material", "roughness mm", "note"),
     [("commercial steel", 0.045, "new; an old closed loop is nearer 0.1 to 0.2 mm"),
      ("copper", 0.0015, "drawn tube, commercially smooth")],
     note="read in secondary sources, consistent with the Handbook of Hydraulic Resistance's "
          "ranges. No figure for PVC or PP-R could be checked: give roughness_mm for those.")

_ref("pipe_sizes", "Steel pipe inside diameters", "ASME B36.10M (Schedule 40)",
     ("series", "nominal", "inside diameter mm"),
     [("steel sch 40", "DN15 (1/2 in)", 15.76), ("steel sch 40", "DN20 (3/4 in)", 20.96),
      ("steel sch 40", "DN25 (1 in)", 26.64), ("steel sch 40", "DN32 (1 1/4 in)", 35.08),
      ("steel sch 40", "DN40 (1 1/2 in)", 40.94), ("steel sch 40", "DN50 (2 in)", 52.48),
      ("steel sch 40", "DN65 (2 1/2 in)", 62.68), ("steel sch 40", "DN80 (3 in)", 77.92),
      ("steel sch 40", "DN90 (3 1/2 in)", 90.12), ("steel sch 40", "DN100 (4 in)", 102.26),
      ("steel sch 40", "DN125 (5 in)", 128.20), ("steel sch 40", "DN150 (6 in)", 154.08),
      ("steel sch 40", "DN200 (8 in)", 202.74), ("steel sch 40", "DN250 (10 in)", 254.46),
      ("steel sch 40", "DN300 (12 in)", 303.18)],
     note="inside diameter = outside diameter - 2 x wall, checked row by row. At DN300 "
          "schedule 40 is thicker than STD (304.74 mm bore).")

_ref("pipe_design_guidance", "Chilled and condenser water: most flow per pipe size, US gpm",
     "ANSI/ASHRAE/IES 90.1-2022 Table 6.5.4.6",
     ("pipe", "<=2000 h", "<=2000 h variable", "2000-4400 h", "2000-4400 h variable",
      ">4400 h", ">4400 h variable"),
     [("2 1/2 in", 120, 180, 85, 130, 68, 110), ("3 in", 180, 270, 140, 210, 110, 170),
      ("4 in", 350, 530, 260, 400, 210, 320), ("5 in", 410, 620, 310, 470, 250, 370),
      ("6 in", 740, 1100, 570, 860, 440, 680), ("8 in", 1200, 1800, 900, 1400, 700, 1100),
      ("10 in", 1800, 2700, 1300, 2000, 1000, 1600),
      ("12 in", 2500, 3800, 1900, 2900, 1500, 2300)],
     note="hours are the system's annual operating hours - a Qatari plant usually runs "
          "more than 4400. 'variable' is variable flow with variable-speed pumps. 1 US gpm "
          "is 0.0631 L/s. A code maximum, not a design target; no ASHRAE friction-rate "
          "figure for pipe could be checked here.")

_ref("chilled_water_practice", "Chilled-water coil selection",
     "ANSI/ASHRAE/IES 90.1-2022 Section 6.5.4.7",
     ("requirement", "value"),
     [("water temperature rise across the coil", "at least 8.33 K (15 F)"),
      ("leaving water temperature", "at least 13.89 C (57 F)")],
     note="exceptions include coils with high air-side pressure drop, fan units up to "
          "2360 L/s, constant-volume systems, entering water at or above 10 C and entering "
          "air at or below 18.3 C. A district-cooling plant's supply and return are the "
          "utility's to state - no Qatar Cool or Kahramaa figure could be checked here.")


# What QCS 2014 Section 22 Part 1 is REPORTED to set - read only in search
# summaries, never in the QCS text. One home, used by the table below and by
# the check a QCS 2014 project gets (D-111).
QCS_2014_REPORTED = ("46 C DB / 30 C WB outdoors and 23 +/- 1 C, 50 +/- 5 % indoors")

_ref("design_weather", "Design weather - Doha International (WMO 411700)",
     "ASHRAE Handbook - Fundamentals 2017, climatic design data, as ASHRAE's own "
     "2017 load-calculation workbook carries it",
     ("month", "0.4 % DB C", "mean coincident WB C", "daily DB range K",
      "coincident WB range K", "tau_b", "tau_d"),
     [("jan", 26.5, 17.1, 7.8, 3.8, 0.503, 1.892), ("feb", 29.9, 17.4, 9.1, 3.9, 0.551, 1.782),
      ("mar", 34.1, 18.1, 10.9, 4.5, 0.617, 1.644), ("apr", 39.2, 20.1, 11.8, 5.0, 0.664, 1.593),
      ("may", 44.1, 21.1, 12.2, 5.8, 0.654, 1.595), ("jun", 45.8, 21.5, 12.4, 6.2, 0.630, 1.636),
      ("jul", 45.7, 22.2, 11.7, 6.6, 0.686, 1.592), ("aug", 44.9, 23.2, 10.7, 6.4, 0.647, 1.673),
      ("sep", 42.2, 22.5, 10.3, 5.8, 0.569, 1.800), ("oct", 39.6, 21.5, 10.4, 5.6, 0.511, 1.916),
      ("nov", 34.2, 20.3, 8.6, 4.2, 0.505, 1.936), ("dec", 28.9, 18.9, 8.1, 3.7, 0.483, 1.955)],
     note="MONTHLY 0.4 % values, not the annual 0.4/1/2 % ones, which could not be "
          "checked. The two ranges are the workbook's own, which it lists beside the "
          "5 % dry bulb and ASHRAE's example uses with it; monthly_load applies them to "
          "these 0.4 % values and says so. Heating: 11.8 C at 99.6 %, 13.0 C at 99 %. "
          "Site 25.261 N, 51.565 E, "
          "10.7 m, UTC+3. The peak dry bulb is DRY air - July's 45.7/22.2 C holds 7.1 g/kg, "
          "less than a 23 C 50 % room - so latent and coil loads need the "
          "dehumidification design condition. QCS 2014 Section 22 Part 1 is reported to "
          "specify " + QCS_2014_REPORTED + " - read only in search summaries; confirm "
          "against the QCS text.")

_ref("sol_air", "Absorptance over outside surface coefficient, for sol-air temperature",
     "ASHRAE Handbook - Fundamentals Ch. 18, as ASHRAE's own 2017 worked example uses it",
     ("surface", "absorptance_over_ho m2.K/W", "absorptance", "ho W/m2.K"),
     [("light surface", 0.026, 0.45, 17.0), ("dark surface", 0.053, 0.90, 17.0)],
     note="the long-wave term for a roof facing the sky, 3.7 K, is part of the method and "
          "applied by Heron; a wall's is 0.")

_ref("people_heat_gain", "Heat gain from people", "ASHRAE Handbook - Fundamentals Ch. 18 "
     "Table 1 (SI)",
     ("activity", "sensible W", "latent W"),
     [("moderately active office work", 75, 55), ("seated, very light work", 70, 45)],
     note="only these two rows were checked - against ASHRAE's own 2017 worked example "
          "(250 and 200 Btu/h a person) and an open implementation of the method. The rest "
          "of Table 1 was not reachable; take other activities from the Handbook.")


_ref("ventilation_rates", "Minimum outdoor air in the breathing zone",
     "ANSI/ASHRAE 62.1-2022 Table 6-1",
     ("occupancy", "group", "Rp L/s per person", "Ra L/s per m2",
      "default occupants per 100 m2", "air class"),
     [("office space", "office buildings", 2.5, 0.3, 5, 1),
      ("reception areas", "office buildings", 2.5, 0.3, 30, 1),
      ("main entry lobbies", "office buildings", 2.5, 0.3, 10, 1),
      ("breakrooms (office buildings)", "office buildings", 2.5, 0.6, 50, 1),
      ("telephone/data entry", "office buildings", 2.5, 0.3, 60, 1),
      ("conference/meeting", "general", 2.5, 0.3, 50, 1),
      ("corridors", "general", None, 0.3, None, 1),
      ("break rooms (general)", "general", 2.5, 0.3, 25, 1),
      ("classrooms (ages 5 to 8)", "educational", 5, 0.6, 25, 1),
      ("classrooms (age 9 plus)", "educational", 5, 0.6, 35, 1),
      ("lecture classroom", "educational", 3.8, 0.3, 65, 1),
      ("lecture hall (fixed seats)", "educational", 3.8, 0.3, 150, 1),
      ("computer lab", "educational", 5, 0.6, 25, 1),
      ("science laboratories", "educational", 5, 0.9, 25, 2),
      ("libraries", "public assembly", 2.5, 0.6, 10, 1),
      ("auditorium seating area", "public assembly", 2.5, 0.3, 150, 1),
      ("places of religious worship", "public assembly", 2.5, 0.3, 120, 1),
      ("lobbies (public assembly)", "public assembly", 2.5, 0.3, 150, 1),
      ("museums/galleries", "public assembly", 3.8, 0.3, 40, 1),
      ("sales", "retail", 3.8, 0.6, 15, 2),
      ("mall common areas", "retail", 3.8, 0.3, 40, 1),
      ("supermarket", "retail", 3.8, 0.3, 8, 1),
      ("restaurant dining rooms", "food and beverage", 3.8, 0.9, 70, 2),
      ("cafeteria/fast-food dining", "food and beverage", 3.8, 0.9, 100, 2),
      ("bars, cocktail lounges", "food and beverage", 3.8, 0.9, 100, 2),
      ("kitchen (cooking)", "food and beverage", 3.8, 0.6, 20, 2),
      ("hotel bedroom/living room", "hotels", 2.5, 0.3, 10, 1),
      ("hotel lobbies/prefunction", "hotels", 3.8, 0.3, 30, 1),
      ("hotel multipurpose assembly", "hotels", 2.5, 0.3, 120, 1),
      ("gym, sports arena (play area)", "sports", 10, 0.9, 7, 2),
      ("health club/aerobics room", "sports", 10, 0.3, 40, 2),
      ("health club/weight rooms", "sports", 10, 0.3, 10, 2),
      ("spectator areas", "sports", 3.8, 0.3, 150, 1),
      ("swimming (pool and deck)", "sports", None, 2.4, None, 2),
      ("warehouses", "miscellaneous", 5, 0.3, None, 2),
      ("pharmacy (prep. area)", "miscellaneous", 2.5, 0.9, 10, 2),
      ("transportation waiting", "miscellaneous", 3.8, 0.3, 100, 1),
      ("banks or bank lobbies", "miscellaneous", 3.8, 0.3, 15, 1),
      ("residential common corridors", "residential", None, 0.3, None, 1)],
     note="the SI column as 62.1 prints it - 7.5 cfm per person is printed 3.8 L/s, not "
          "converted to 3.54 - so do not mix it with the I-P column. Apartments are "
          "outside 62.1-2022's scope (ASHRAE 62.2). Check the edition your authority "
          "adopted: no Qatari source read here names one.")

_ref("zone_air_distribution", "Zone air distribution effectiveness Ez",
     "ANSI/ASHRAE 62.1-2022 Table 6-4",
     ("configuration", "Ez"),
     [("ceiling supply of cool air", 1.0),
      ("ceiling supply of warm air and floor return", 1.0),
      ("ceiling supply of warm air 8 C or more above space temperature and ceiling return", 0.8),
      ("ceiling supply of warm air less than 8 C above space, jet below 0.8 m/s within "
       "1.4 m of the floor, ceiling return", 0.8),
      ("ceiling supply of warm air less than 8 C above space, jet at or above 0.8 m/s "
       "within 1.4 m of the floor, ceiling return", 1.0),
      ("floor supply of warm air and floor return", 1.0),
      ("floor supply of warm air and ceiling return", 0.7),
      ("makeup supply outlet more than half the space's length from the exhaust or return", 0.8),
      ("makeup supply outlet less than half the space's length from the exhaust or return", 0.5),
      ("stratified: floor supply of cool air, vertical throw at or above 0.25 m/s at 1.4 m, "
       "ceiling return at or below 5.5 m", 1.05),
      ("stratified: floor supply of cool air, vertical throw below 0.25 m/s at 1.4 m, "
       "ceiling return at or below 5.5 m", 1.2),
      ("stratified: floor supply of cool air, vertical throw below 0.25 m/s at 1.4 m, "
       "ceiling return above 5.5 m", 1.5)],
     note="ceiling supply and ceiling return mean more than 1.4 m above the floor. Where "
          "a zone is both cooled and heated, design to the worse of the two Ez.")

_ref("exhaust_rates", "Minimum exhaust rates", "ANSI/ASHRAE 62.1-2022 Table 6-2",
     ("space", "L/s per fixture", "L/s per m2", "basis"),
     [("toilets - public", "25 / 35", None, "per WC or urinal; the higher rate where "
       "periods of heavy use are expected"),
      ("toilets - private", "12.5 / 25", None, "one person at a time; the lower rate for "
       "continuous operation during hours of use"),
      ("shower rooms", "10 / 25", None, "per showerhead; the lower rate for continuous "
       "operation"),
      ("kitchens - commercial", None, 3.5, ""),
      ("kitchenettes", None, 1.5, ""),
      ("janitor closets, trash rooms, recycling", None, 5.0, ""),
      ("locker rooms - athletic, industrial, health care", None, 2.5, ""),
      ("locker rooms - all others", None, 1.25, ""),
      ("parking garages", None, 3.7, "not required where two or more sides are at least "
       "50 % open to outside"),
      ("copy, printing rooms", None, 2.5, ""),
      ("soiled laundry storage rooms", None, 5.0, ""),
      ("storage rooms, chemical", None, 7.5, ""),
      ("art classrooms", None, 3.5, ""),
      ("educational science laboratories", None, 5.0, ""),
      ("barber shops", None, 2.5, ""),
      ("beauty and nail salons", None, 3.0, "")],
     note="62.1 Section 6.5.1.2 keeps these spaces' exhaust larger than their supply. "
          "An IMC-based code ties the higher toilet rate to intermittent fan operation "
          "instead of heavy use - the project's adopted code decides.")

_ref("adpi", "ADPI throw ratios - the classic ASHRAE selection guide",
     "ASHRAE ADPI selection guide, classic table as reproduced in manufacturers' "
     "engineering guides",
     ("terminal", "room load W/m2", "throw", "T/L for best ADPI", "best ADPI",
      "ADPI above", "T/L range low", "T/L range high"),
     [("high sidewall grille", 189, "T50", 1.8, 72, 70, 1.5, 2.2),
      ("high sidewall grille", 126, "T50", 1.6, 78, 70, 1.2, 2.3),
      ("high sidewall grille", 63, "T50", 1.5, 85, 80, 1.0, 1.9),
      ("circular ceiling diffuser", 189, "T50", 0.8, 83, 80, 0.7, 1.2),
      ("circular ceiling diffuser", 126, "T50", 0.8, 88, 80, 0.5, 1.5),
      ("circular ceiling diffuser", 63, "T50", 0.8, 93, 90, 0.7, 1.3),
      ("ceiling slot diffuser", 252, "T100", 0.3, 85, 80, 0.3, 0.7),
      ("ceiling slot diffuser", 189, "T100", 0.3, 88, 80, 0.3, 0.8),
      ("ceiling slot diffuser", 126, "T100", 0.3, 91, 80, 0.3, 1.1),
      ("ceiling slot diffuser", 63, "T100", 0.3, 92, 80, 0.3, 1.5)],
     note="ADPI predicted from T/L, not measured comfort. The classic table dates from "
          "1960s products at 63-252 W/m2; ASHRAE's 2019 HVAC Applications carries the "
          "RP-1546 update for today's lower loads. L for a ceiling diffuser is the "
          "distance to the nearest wall or to where neighbouring jets meet; for a "
          "sidewall grille, to the wall it blows at. Read through search summaries of "
          "the reproductions, not the Handbook page - confirm before relying on it.")

_ref("air_terminal_guidance", "Neck velocity for a room criterion, when no product "
     "sound data is held", "ASHRAE Handbook - HVAC Applications (2019) Ch. 49, Table 9",
     ("room criterion", "supply outlet neck m/s", "return opening m/s"),
     [("RC/NC 45", 3.2, 3.8), ("RC/NC 40", 2.8, 3.4), ("RC/NC 35", 2.5, 3.0),
      ("RC/NC 30", 2.2, 2.5), ("RC/NC 25", 1.8, 2.2)],
     note="a screening limit for ONE outlet with no damper in its neck; the product's own "
          "catalogue NC governs, and NC at the same neck velocity ranges widely between "
          "products. Read from a transcription that cites the Handbook page. For a SUPPLY "
          "neck at NC/RC 30 the owner's own figure, 2.5 m/s, is used instead of this "
          "table's 2.2 (D-110); the table is kept as ASHRAE prints it.")

# The office's own figures. Each is the OWNER's answer, recorded as a decision -
# not a standard's figure and not Heron's. Applied only where its condition
# holds, said out loud every time it is, and given way to by any figure the
# modeller states. It is the owner's, so it goes with the owner to every
# project; the day a second office uses Heron it is theirs to replace (D-110,
# as D-83 reserves for the same day).
OFFICE_SUPPLY_NECK_NC = 30         # the room criterion the figure is for, NC/RC
OFFICE_SUPPLY_NECK_MS = 2.5        # supply diffuser neck velocity at it, m/s - D-110

_ref("comfort_air_speed", "Average air speed limits without occupant control",
     "ANSI/ASHRAE 55-2023 Section 5.3.4.2",
     ("operative temperature", "average air speed limit"),
     [("above 25.5 C", "0.8 m/s"),
      ("23.0 to 25.5 C", "Va = 50.49 - 4.4047 to + 0.096425 to^2 m/s"),
      ("below 23.0 C", "0.2 m/s (unless clothing above 0.7 clo or activity above 1.3 met)")],
     note="ankle draft (Section 5.3.5.3): air speed 0.1 m above the floor below "
          "0.35 TS + 0.39 m/s, TS being the whole-body thermal sensation.")


# ---------------------------------------------------------------------------
# The project's governing standards - asked ONCE per project and kept for it
# (D-111). None of the four could be established from any source this engine
# was built from, and each changes what an answer may check. They never block
# a calculation: one that depends on them still calculates, puts the questions
# at the top of its answer, and says which checks it could not run. The caller
# keeps the answers (heron_designbasis.py, through the brain seam) and hands
# them back as `recorded` - this file still reads no store.
# ---------------------------------------------------------------------------

PROJECT_STANDARDS = collections.OrderedDict([
    ("ventilation_standard",
     ("62.1-<year>, or other",
      "which edition of ASHRAE 62.1 governs this project's outdoor air - other if "
      "something else does")),
    ("energy_standard",
     ("90.1-<year>, other or none",
      "whether ASHRAE 90.1 applies to this project, and which edition - other for "
      "another energy code, none for none")),
    ("qcs_edition",
     ("QCS <year>, or none",
      "which edition of the Qatar Construction Specifications governs - none "
      "outside Qatar")),
    ("cibse_beside_ashrae",
     ("true or false", "whether CIBSE guidance is used beside ASHRAE on this project")),
])

# The editions this engine's tables and clauses were taken from. A project that
# follows another edition is told so beside every figure and check it touches.
HELD_62_1 = "62.1-2022"
HELD_90_1 = "90.1-2022"
NO_STANDARD = "none"
OTHER_STANDARD = "other"

# Each standard as (the publishers' words a person may put before it, the
# number that names it, how an edition of it is written).
_EDITIONS = {
    "ventilation_standard": (("ansi", "ashrae", "ies"), "62.1", "62.1-%d"),
    "energy_standard": (("ansi", "ashrae", "ies"), "90.1", "90.1-%d"),
    "qcs_edition": (("qcs",), "", "QCS %d"),
}
_WORDS = {"ventilation_standard": (OTHER_STANDARD,),
          "energy_standard": (OTHER_STANDARD, NO_STANDARD),
          "qcs_edition": (NO_STANDARD,)}


def _edition_year(text, publishers, number):
    """The year in "ASHRAE 62.1-2022", "62.1 2022" or "QCS 2014", or None."""
    words = [w for w in text.replace("/", " ").replace("-", " ").split()
             if w not in publishers]
    rest = "".join(words)
    if not rest.startswith(number):
        return None
    rest = rest[len(number):]
    if len(rest) == 4 and rest.isdigit() and 1980 <= int(rest) <= 2099:
        return int(rest)
    return None


def standard_value(name, raw):
    """One project standard as said, normalised - or Refused saying what is accepted."""
    unit = PROJECT_STANDARDS[name][0]
    if name == "cibse_beside_ashrae":
        if isinstance(raw, bool):
            return raw
        word = str(raw).strip().lower()
        if word in ("true", "yes", "y", "1"):
            return True
        if word in ("false", "no", "n", "0"):
            return False
        raise Refused("%s must be true or false - got %r" % (name, raw))
    text = " ".join(str(raw).strip().lower().replace("_", " ").split())
    if text in _WORDS[name]:
        return text
    publishers, number, form = _EDITIONS[name]
    year = _edition_year(text, publishers, number)
    if year is not None:
        return form % year
    raise Refused("%s must be %s - got %r" % (name, unit, raw))


def standard_text(value):
    """A project standard as a person reads it."""
    if value is True:
        return "yes"
    if value is False:
        return "no"
    return str(value)


def project_standards(a):
    """
    The four project standards for this answer - each from this request, else
    from the project's record, else None, and every None put to the modeller
    as an ask-once question. A value given here wins over the record, and the
    caller keeps it in the record's place, the old one in its history.
    """
    out = collections.OrderedDict()
    for name, (unit, why) in PROJECT_STANDARDS.items():
        raw = a.raw(name)
        if raw is not None and raw != "":
            try:
                out[name] = standard_value(name, raw)
            except Refused as why_not:
                a.refuse(str(why_not))
                out[name] = None
                continue
            a.standards[name] = {"value": out[name], "from": "request"}
            continue
        kept = a.recorded.get(name)
        value = None
        if isinstance(kept, dict) and "value" in kept:
            try:
                value = standard_value(name, kept["value"])
            except Refused:
                # A record this engine cannot read is ASKED AGAIN, never
                # repaired into the nearest thing it might have meant.
                value = None
        if value is None:
            a.ask_once.append({"input": name, "unit": unit, "why": why})
        else:
            a.standards[name] = {"value": value, "from": "record",
                                 "recorded": kept.get("recorded")}
        out[name] = value
    if out.get("cibse_beside_ashrae"):
        a.check("WARN", "this project uses CIBSE beside ASHRAE and Heron holds no CIBSE "
                        "data - where CIBSE sets a figure, take it from the CIBSE guide")
    return out


def _applies_90_1(energy):
    """True if ASHRAE 90.1 applies to the project, False if not, None if not known yet."""
    return None if energy is None else energy.startswith("90.1-")


def _edition_said(edition, held):
    """'' when the project follows the edition Heron holds, else the clause saying it does not."""
    if edition is None or edition == held:
        return ""
    return "; this project follows %s, so confirm it in that edition" % edition


def _for_project(sentence, edition, held, family):
    """An offered figure, with the project's own edition beside it when that differs."""
    if not sentence or edition is None or edition == held:
        return sentence
    if edition in (OTHER_STANDARD, NO_STANDARD):
        return "%s; %s does not govern this project" % (sentence, family)
    return "%s; this project follows %s - read that edition's figure" % (sentence, edition)


def _check_62_1(a, edition, what):
    """The answer's line on which standard governs the outdoor air it was worked to."""
    if edition is None:
        a.check("WARN", "not checked: which standard governs this project's outdoor air is "
                        "not known yet - %s ASHRAE %s's" % (what, HELD_62_1))
    elif edition == OTHER_STANDARD:
        a.check("WARN", "this project's outdoor air is governed by something other than "
                        "ASHRAE 62.1 - this is 62.1's procedure, a comparison and not the "
                        "project's requirement")
    elif edition != HELD_62_1:
        a.check("WARN", "this project follows ASHRAE %s and %s ASHRAE %s's - confirm each "
                        "figure in %s" % (edition, what, HELD_62_1, edition))
    else:
        a.check("OK", "ASHRAE %s governs this project's outdoor air, and %s that edition's"
                % (edition, what))


# ---------------------------------------------------------------------------
# The calculations. Each is registered with a name, a title and a group; its
# docstring's first paragraph is the one-line purpose the catalogue shows.
# ---------------------------------------------------------------------------

CALCULATIONS = collections.OrderedDict()


def calculation(name, title, group):
    def register(fn):
        doc = (fn.__doc__ or "").strip().split("\n\n")[0]
        CALCULATIONS[name] = {"title": title, "group": group, "run": fn,
                              "purpose": " ".join(doc.split())}
        return fn
    return register


# --- units -----------------------------------------------------------------

_CONVERT = collections.OrderedDict([
    ("airflow", [("l/s", 1e-3), ("m3/s", 1.0), ("m3/h", 1.0 / 3600.0),
                 ("cfm", LS_PER_CFM / 1000.0), ("l/min", 1.0 / 60000.0),
                 ("us gpm", LS_PER_USGPM / 1000.0)]),
    ("pressure", [("pa", 1.0), ("kpa", 1000.0), ("in.wg", PA_PER_INWG),
                  ("ft.wg", PA_PER_INWG * 12.0), ("mm.wg", 9.80665),
                  ("psi", 6894.757293168361), ("bar", 1.0e5)]),
    ("power", [("w", 1.0), ("kw", 1000.0), ("btu/h", W_PER_BTUH),
               ("mbh", 1000.0 * W_PER_BTUH), ("tr", W_PER_TR),
               ("hp", 745.69987158227022)]),
    ("velocity", [("m/s", 1.0), ("fpm", MS_PER_FPM), ("ft/s", M_PER_FT),
                  ("km/h", 1.0 / 3.6)]),
    ("length", [("mm", 1e-3), ("m", 1.0), ("in", 0.0254), ("ft", M_PER_FT)]),
    ("area", [("m2", 1.0), ("ft2", M2_PER_FT2), ("mm2", 1e-6), ("in2", 0.00064516)]),
    ("volume", [("m3", 1.0), ("l", 1e-3), ("ft3", 0.028316846592),
                ("us gal", 0.003785411784)]),
    ("friction rate", [("pa/m", 1.0), ("in.wg/100ft", PA_PER_INWG / (100.0 * M_PER_FT))]),
    ("heat flux", [("w/m2", 1.0), ("btu/h.ft2", W_PER_BTUH / M2_PER_FT2)]),
    ("u-value", [("w/m2k", 1.0), ("btu/h.ft2.f", W_M2K_PER_IP_U)]),
    ("airflow per area", [("l/s.m2", 1.0), ("cfm/ft2", LS_PER_CFM / M2_PER_FT2)]),
    ("specific fan power", [("w/(l/s)", 1.0), ("w/cfm", 1.0 / LS_PER_CFM)]),
    ("temperature difference", [("delta k", 1.0), ("delta f", 5.0 / 9.0)]),
])

_ALIASES = {
    "lps": "l/s", "l/sec": "l/s", "litres/s": "l/s", "liters/s": "l/s",
    "m3/hr": "m3/h", "cmh": "m3/h", "m3h": "m3/h", "m3s": "m3/s", "cms": "m3/s",
    "ft3/min": "cfm", "gpm": "us gpm", "usgpm": "us gpm", "lpm": "l/min",
    "inwg": "in.wg", "in.w.g.": "in.wg", "inh2o": "in.wg", "in.h2o": "in.wg",
    "\"wg": "in.wg", "ftwg": "ft.wg", "fth2o": "ft.wg", "fthead": "ft.wg",
    "mmwg": "mm.wg", "mmh2o": "mm.wg", "mmwc": "mm.wg",
    "watt": "w", "watts": "w", "kilowatt": "kw", "btuh": "btu/h", "btu/hr": "btu/h",
    "kbtu/h": "mbh", "ton": "tr", "tons": "tr", "rt": "tr", "bhp": "hp",
    "mps": "m/s", "ft/min": "fpm", "fps": "ft/s", "kph": "km/h",
    "inch": "in", "inches": "in", "feet": "ft", "foot": "ft",
    "sqm": "m2", "sq.m": "m2", "sqft": "ft2", "sq.ft": "ft2", "sqin": "in2",
    "litre": "l", "liter": "l", "litres": "l", "liters": "l", "cuft": "ft3",
    "gal": "us gal", "usgal": "us gal", "gallon": "us gal",
    "inwg/100ft": "in.wg/100ft", "in/100ft": "in.wg/100ft",
    "btuh/ft2": "btu/h.ft2", "btu/hft2": "btu/h.ft2",
    "w/m2.k": "w/m2k", "w/(m2.k)": "w/m2k", "w/m2/k": "w/m2k",
    "btu/hft2f": "btu/h.ft2.f", "btuh/ft2f": "btu/h.ft2.f", "btu/h.ft2.f": "btu/h.ft2.f",
    "l/s/m2": "l/s.m2", "l/sm2": "l/s.m2", "cfm/sqft": "cfm/ft2",
    "w/l/s": "w/(l/s)", "w/lps": "w/(l/s)",
    "deltak": "delta k", "dk": "delta k", "deltac": "delta k", "deltaf": "delta f",
    "df": "delta f",
    "c": "c", "degc": "c", "celsius": "c", "f": "f", "degf": "f",
    "fahrenheit": "f", "k": "k", "kelvin": "k",
}


def _unit_key(text):
    t = str(text).strip().lower().replace("°", "").replace("^", "")
    t = t.replace("³", "3").replace("²", "2")
    squashed = t.replace(" ", "")
    for kind, units in _CONVERT.items():
        for name, _factor in units:
            if squashed == name.replace(" ", ""):
                return kind, name
    alias = _ALIASES.get(squashed)
    if alias in ("c", "f", "k"):
        return "temperature", alias
    if alias:
        for kind, units in _CONVERT.items():
            for name, _factor in units:
                if alias == name:
                    return kind, name
    return None, None


def _to_kelvin(value, unit):
    return {"c": value + 273.15, "f": (value - 32.0) * 5.0 / 9.0 + 273.15, "k": value}[unit]


def _from_kelvin(kelvin, unit):
    return {"c": kelvin - 273.15, "f": (kelvin - 273.15) * 9.0 / 5.0 + 32.0, "k": kelvin}[unit]


@calculation("convert", "Unit conversion", "units")
def calc_convert(a):
    """Converts a value between the units HVAC work mixes - L/s, m3/h and cfm; Pa and in.wg; W, kW, TR and Btu/h; m/s and fpm; temperatures; and the rest.

    Without `to`, every unit of the same kind is shown.
    """
    value = a.number("value", "number", "the number to convert")
    source = a.word("from", "the unit the value is in - cfm, Pa, TR, F, in.wg/100ft ...")
    target = a.word("to", "the unit wanted; left out, every unit of that kind is shown",
                    required=False)
    if a.incomplete():
        return
    kind, unit = _unit_key(source)
    if kind is None:
        a.refuse("'%s' is not a unit Heron converts. It knows: %s, and C, F, K"
                 % (source, "; ".join("%s (%s)" % (k, ", ".join(n for n, _f in u))
                                      for k, u in _CONVERT.items())))
        return
    wanted = [None]
    if target:
        to_kind, to_unit = _unit_key(target)
        if to_kind is None:
            a.refuse("'%s' is not a unit Heron converts" % target)
            return
        if to_kind != kind:
            a.refuse("%s is a %s and %s is a %s - they do not convert into each other"
                     % (source, kind, target, to_kind))
            return
        wanted = [to_unit]
    a.cite(SRC_UNITS)
    if kind == "temperature":
        if unit == "k" and value < 0 or _to_kelvin(value, unit) < 0:
            a.refuse("%s %s is below absolute zero" % (_g(value), source))
            return
        kelvin = _to_kelvin(value, unit)
        for name in (wanted if target else ["c", "f", "k"]):
            a.result("%s %s" % (_g(value), unit.upper()),
                     "%.6g %s" % (_from_kelvin(kelvin, name), name.upper()))
        a.uses("temperatures convert with their offsets; a temperature DIFFERENCE "
               "does not - use delta k / delta f for one")
        return
    factors = dict(_CONVERT[kind])
    base = value * factors[unit]
    for name in (wanted if target else [n for n, _f in _CONVERT[kind]]):
        a.result("%s %s" % (_g(value), unit), "%.6g %s" % (base / factors[name], name))


# --- moist air -------------------------------------------------------------

def _state_result(a, s, prefix=""):
    a.result(prefix + "Dry bulb", temp_text(s["t_c"]))
    a.result(prefix + "Relative humidity", "%s %%" % _f(s["rh_pct"], 1))
    a.result(prefix + "Wet bulb", temp_text(s["twb_c"]))
    a.result(prefix + "Dew point", temp_text(s["tdp_c"]) if s["tdp_c"] is not None else "-")
    a.result(prefix + "Humidity ratio", "%s g/kg dry air  (%s gr/lb)"
             % (_f(s["w"] * 1000.0, 2), _f(s["w"] * 7000.0, 1)))
    a.result(prefix + "Enthalpy", "%s kJ/kg dry air" % _f(s["h_kj_kg"], 2))


def _state(a, t, p, hum):
    try:
        return PSY.state(t, p, **hum)
    except PSY.PsychroRangeError as why:
        a.refuse(str(why))
        return None


@calculation("psychrometrics", "Moist-air state point", "air")
def calc_psychrometrics(a):
    """Every property of one air state - humidity ratio, relative humidity, wet bulb, dew point, enthalpy, specific volume and density - from the dry bulb and one humidity reading."""
    t = a.number("dry_bulb_c", "C", "dry-bulb temperature", -60, 90)
    hum = a.humidity("", "the air's humidity - relative humidity, wet bulb, dew point "
                         "or humidity ratio")
    p = a.pressure_kpa("site altitude, for the barometric pressure")
    if a.incomplete():
        return
    s = _state(a, t, p, hum)
    if s is None:
        return
    _state_result(a, s)
    a.result("Specific volume", "%s m3/kg dry air" % _f(s["v_m3_kg"], 4))
    a.result("Density", "%s kg/m3 moist air" % _f(s["density_kg_m3"], 4))
    a.result("Vapour pressure", "%s kPa (saturation %s kPa)" % (_f(s["pw_kpa"], 4),
                                                              _f(s["pws_kpa"], 4)))
    a.result("Barometric pressure", "%s kPa" % _f(p, 3))
    a.uses("wet bulb and dew point SOLVED from the equations, not read off a chart")
    a.cite(SRC_PSYCHRO)


@calculation("air_mixing", "Mixing outdoor and return air", "air")
def calc_air_mixing(a):
    """The mixed-air condition entering a coil from an outdoor-air stream and a return-air stream, mixed by dry-air mass rather than by volume."""
    qo = a.flow("outdoor airflow", prefix="outdoor_flow")
    to = a.number("outdoor_dry_bulb_c", "C", "outdoor dry bulb", -60, 70)
    ho = a.humidity("outdoor_", "outdoor humidity")
    qr = a.flow("return airflow", prefix="return_flow")
    tr = a.number("return_dry_bulb_c", "C", "return (room) dry bulb", -20, 60)
    hr = a.humidity("return_", "return (room) humidity")
    p = a.pressure_kpa("site altitude, for the barometric pressure")
    if a.incomplete():
        return
    so, sr = _state(a, to, p, ho), _state(a, tr, p, hr)
    if so is None or sr is None:
        return
    mo, mr = qo / 1000.0 / so["v_m3_kg"], qr / 1000.0 / sr["v_m3_kg"]
    try:
        sm = PSY.mix(mo, so, mr, sr, p)
    except PSY.PsychroRangeError as why:
        a.refuse(str(why))
        return
    qm = (mo + mr) * sm["v_m3_kg"] * 1000.0
    _state_result(a, sm, "Mixed ")
    a.result("Mixed airflow", flow_text(qm) + " at the mixed condition")
    a.result("Outdoor air share", "%s %% by mass, %s %% by volume"
             % (_f(100.0 * mo / (mo + mr), 1), _f(100.0 * qo / (qo + qr), 1)))
    a.uses("streams converted to dry-air mass with each one's own specific volume, "
           "W and h mixed by mass, mixed dry bulb solved from h and W")
    a.cite(SRC_PSYCHRO)
    a.check("OK", "next: coil_load with this mixed air as the entering condition")


@calculation("coil_load", "Cooling coil load", "air")
def calc_coil_load(a):
    """The load on a cooling coil - total, sensible, latent, tons and condensate - from the air entering it and the air leaving it.

    The entering air is either given, or mixed here from outdoor_flow_* and return_flow_* as air_mixing does.
    """
    mixing = any(a.has("outdoor_flow_" + u[0]) for u in _FLOW_UNITS)
    p = a.pressure_kpa("site altitude, for the barometric pressure")
    if mixing:
        qo = a.flow("outdoor airflow", prefix="outdoor_flow")
        to = a.number("outdoor_dry_bulb_c", "C", "outdoor dry bulb", -60, 70)
        ho = a.humidity("outdoor_", "outdoor humidity")
        qr = a.flow("return airflow", prefix="return_flow")
        tr = a.number("return_dry_bulb_c", "C", "return (room) dry bulb", -20, 60)
        hr = a.humidity("return_", "return (room) humidity")
    else:
        q = a.flow("airflow through the coil")
        at = a.choice("flow_measured_at", ("entering", "leaving"),
                      "where that airflow is measured - the same volume is a different "
                      "mass of air at the warm side and the cold side of a coil")
        te = a.number("entering_dry_bulb_c", "C", "air entering the coil", -60, 70)
        he = a.humidity("entering_", "humidity of the air entering the coil")
    tl = a.number("leaving_dry_bulb_c", "C", "air leaving the coil", -20, 60)
    hl = a.humidity("leaving_", "humidity of the air leaving the coil - typically its "
                                "dew point, or 90-95 % RH off a wet coil")
    if a.incomplete():
        return
    sl = _state(a, tl, p, hl)
    if mixing:
        so, sr = _state(a, to, p, ho), _state(a, tr, p, hr)
        if so is None or sr is None or sl is None:
            return
        mo, mr = qo / 1000.0 / so["v_m3_kg"], qr / 1000.0 / sr["v_m3_kg"]
        try:
            se = PSY.mix(mo, so, mr, sr, p)
        except PSY.PsychroRangeError as why:
            a.refuse(str(why))
            return
        m = mo + mr
        _state_result(a, se, "Entering (mixed) ")
    else:
        se = _state(a, te, p, he)
        if se is None or sl is None:
            return
        m = q / 1000.0 / (se["v_m3_kg"] if at == "entering" else sl["v_m3_kg"])
        _state_result(a, se, "Entering ")
    _state_result(a, sl, "Leaving ")
    total = m * (se["h_kj_kg"] - sl["h_kj_kg"]) * 1000.0
    sensible = m * (1.006 + 1.86 * se["w"]) * (se["t_c"] - sl["t_c"]) * 1000.0
    latent = total - sensible
    condensate = m * (se["w"] - sl["w"]) * 3600.0
    a.result("Dry-air mass flow", "%s kg/s" % _f(m, 4))
    a.result("Airflow", "%s entering, %s leaving"
             % (flow_text(m * se["v_m3_kg"] * 1000.0), _f(m * sl["v_m3_kg"] * 1000.0, 1) + " L/s"))
    a.result("Total coil load", power_text(total))
    a.result("Sensible", power_text(sensible))
    a.result("Latent", power_text(latent))
    if total > 0:
        a.result("Sensible heat ratio", _f(sensible / total, 3))
    a.result("Condensate", "%s kg/h  (about %s L/h)" % (_f(condensate, 2), _f(condensate, 1)))
    if total <= 0:
        a.check("WARN", "the leaving air holds MORE energy than the entering air - "
                        "this is heating, not cooling; check the two states")
    if sl["w"] > se["w"] + 1e-9:
        a.check("WARN", "the leaving air is wetter than the entering air - a cooling "
                        "coil cannot add moisture")
    a.uses("total = m (h_in - h_out); sensible = m (1.006 + 1.86 W_in)(t_in - t_out); "
           "latent = the remainder, m (W_in - W_out)(2501 + 1.86 t_out)")
    a.cite(SRC_PSYCHRO, SRC_LOADS)
    a.check("OK", "next: chw_flow with this load for the chilled-water flow")


# --- the sun -----------------------------------------------------------------
#
# ASHRAE's clear-sky model, as Fundamentals Ch. 14 gives it from the 2013
# edition on and as ASHRAE's own 2017 load-calculation workbook computes it:
# the solar position from the equation of time and declination, the air mass
# of Kasten & Young (1989), and the beam and diffuse irradiance from the
# site's own clear-sky optical depths tau_b and tau_d. Every equation here
# reproduces that workbook's figures; docs/41 s4 says how that was checked.

SRC_SOLAR = ("ASHRAE Handbook - Fundamentals (SI), Ch. 14 Climatic Design "
             "Information - clear-sky model with the 2013/2017 exponents, "
             "equation of time, declination and extraterrestrial irradiance; "
             "air mass by Kasten & Young (1989); irradiance on a surface with "
             "ASHRAE's Y factor for sky diffuse and a ground reflectance")

_MONTH_DAYS = (31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31)
_FACING = {"n": 0.0, "north": 0.0, "ne": 45.0, "northeast": 45.0, "e": 90.0,
           "east": 90.0, "se": 135.0, "southeast": 135.0, "s": 180.0, "south": 180.0,
           "sw": 225.0, "southwest": 225.0, "w": 270.0, "west": 270.0, "nw": 315.0,
           "northwest": 315.0}


def day_of_year(month, day):
    return sum(_MONTH_DAYS[:month - 1]) + day


def sun_position(latitude, longitude_east, utc_offset_h, n, standard_hour):
    """
    The sun at a local STANDARD time: solar altitude and azimuth in degrees,
    the azimuth measured from south, positive toward west, as ASHRAE measures
    it. Also the equation of time (min), declination, apparent solar time,
    hour angle and extraterrestrial normal irradiance (W/m2).
    """
    gamma = math.radians(360.0 * (n - 1) / 365.0)
    et = 2.2918 * (0.0075 + 0.1868 * math.cos(gamma) - 3.2077 * math.sin(gamma)
                   - 1.4615 * math.cos(2 * gamma) - 4.089 * math.sin(2 * gamma))
    declination = 23.45 * math.sin(math.radians(360.0 * (n + 284) / 365.0))
    eo = 1367.0 * (1.0 + 0.033 * math.cos(math.radians(360.0 * (n - 3) / 365.0)))
    ast = standard_hour + et / 60.0 + (longitude_east - 15.0 * utc_offset_h) / 15.0
    hour_angle = 15.0 * (ast - 12.0)
    lat, dec, h = (math.radians(latitude), math.radians(declination),
                   math.radians(hour_angle))
    sin_beta = math.cos(lat) * math.cos(dec) * math.cos(h) + math.sin(lat) * math.sin(dec)
    beta = math.asin(max(-1.0, min(1.0, sin_beta)))
    cos_beta = math.cos(beta)
    if cos_beta < 1e-12:
        phi = 0.0
    else:
        cos_phi = ((math.cos(h) * math.cos(dec) * math.sin(lat) - math.sin(dec) * math.cos(lat))
                   / cos_beta)
        phi = math.copysign(math.degrees(math.acos(max(-1.0, min(1.0, cos_phi)))), hour_angle)
    return {"n": n, "et_min": et, "declination": declination, "eo": eo, "ast": ast,
            "hour_angle": hour_angle, "altitude": math.degrees(beta), "azimuth": phi}


def clear_sky(eo, altitude, tau_b, tau_d):
    """Beam normal and diffuse horizontal clear-sky irradiance (W/m2), and the air mass."""
    if altitude <= 0:
        return 0.0, 0.0, None
    m = 1.0 / (math.sin(math.radians(altitude)) + 0.50572 * (6.07995 + altitude) ** -1.6364)
    ab = 1.454 - 0.406 * tau_b - 0.268 * tau_d + 0.021 * tau_b * tau_d
    ad = 0.507 + 0.205 * tau_b - 0.080 * tau_d - 0.190 * tau_b * tau_d
    return eo * math.exp(-tau_b * m ** ab), eo * math.exp(-tau_d * m ** ad), m


def on_surface(altitude, azimuth, beam, diffuse, surface_azimuth, tilt, rho):
    """
    Irradiance on a surface (W/m2): beam, sky diffuse, ground reflected, and
    the incidence angle. Azimuths from south, positive west; tilt 0 is a roof,
    90 a wall. Sky diffuse on a tilted surface takes the Y factor of a
    vertical surface facing the same way, as ASHRAE does.
    """
    b, g = math.radians(altitude), math.radians(azimuth - surface_azimuth)
    t = math.radians(tilt)
    cos_theta = math.cos(b) * math.cos(g) * math.sin(t) + math.sin(b) * math.cos(t)
    cos_vertical = math.cos(b) * math.cos(g)
    y = max(0.45, 0.55 + 0.437 * cos_vertical + 0.313 * cos_vertical ** 2)
    etb = beam * max(cos_theta, 0.0)
    etd = diffuse * (y * math.sin(t) + math.cos(t))
    etr = (beam * math.sin(b) + diffuse) * rho * (1.0 - math.cos(t)) / 2.0
    theta = math.degrees(math.acos(max(-1.0, min(1.0, cos_theta))))
    return {"beam": etb, "diffuse": etd, "reflected": etr, "total": etb + etd + etr,
            "incidence": theta, "y": y}


def _facing(view):
    raw = view.raw("facing")
    if raw is None or str(raw).strip() == "":
        view.answer.need(view.name("facing"),
                         "a compass word (N, NE, E ... NW) or degrees from north",
                         "the way the surface faces - west is 270; a roof's facing does "
                         "not matter when its tilt is 0")
        return None
    word = str(raw).strip().lower()
    if word in _FACING:
        return _FACING[word]
    try:
        value = _to_number(raw, view.name("facing"))
        _within(value, view.name("facing"), "degrees", 0, 360, False)
    except Refused as why:
        view.answer.refuse(str(why) + " - or a compass word, N to NW")
        return None
    return value % 360.0


@calculation("solar", "Clear-sky sun on a surface", "load")
def calc_solar(a):
    """The sun on a wall, roof or window at a design hour - position, and beam, diffuse and ground-reflected irradiance by ASHRAE's clear-sky model from the site's own optical depths - or every hour of the design day and its peak.

    Its total is the irradiance_w_m2 a cooling_load surface asks for.
    """
    lat = a.number("latitude_deg", "degrees", "site latitude, north positive", -90, 90)
    lon = a.number("longitude_deg", "degrees", "site longitude, EAST positive - Doha "
                   "is about 51.6", -180, 180)
    tz = a.number("utc_offset_h", "hours", "the site's STANDARD time zone - Qatar is +3",
                  -12, 14)
    month = a.integer("month", "design month", 1, 12)
    day = a.integer("day", "design day of the month - ASHRAE's design days are the 21st",
                    1, 31)
    tau_b = a.number("tau_b", "-", "the site's clear-sky BEAM optical depth for that month",
                     0.1, 2.0, reference=offer_table("design_weather"))
    tau_d = a.number("tau_d", "-", "the site's clear-sky DIFFUSE optical depth for that "
                     "month", 0.5, 4.0, reference=offer_table("design_weather"))
    facing = _facing(a.top)
    tilt = a.number("tilt_deg", "degrees", "0 for a flat roof, 90 for a wall", 0, 90)
    rho = a.number("ground_reflectance", "0-1", "the ground's solar reflectance in front "
                   "of the surface", 0, 1,
                   reference="ASHRAE's own 2017 worked example uses 0.2 - offer it, do not "
                             "assume it")
    hour = a.number("hour", "h", "local STANDARD time, e.g. 15 for 3 pm; left out, every "
                    "hour of the day and its peak", 0, 24, required=False)
    if a.incomplete():
        return
    if day > _MONTH_DAYS[month - 1]:
        a.refuse("month %d has no day %d" % (month, day))
        return
    n = day_of_year(month, day)
    psi = facing - 180.0
    a.cite(SRC_SOLAR)
    a.uses("ASHRAE's simplified sun position - within about 1 degree of NREL's precise "
           "algorithm in spring and autumn, within 0.1 degree in July - which is what "
           "ASHRAE's own design tables are computed with")

    def at(hour_):
        sun = sun_position(lat, lon, tz, n, hour_)
        beam, diffuse, m = clear_sky(sun["eo"], sun["altitude"], tau_b, tau_d)
        here = on_surface(sun["altitude"], sun["azimuth"], beam, diffuse, psi, tilt, rho)
        return sun, beam, diffuse, m, here

    if hour is not None:
        sun, beam, diffuse, m, here = at(hour)
        a.result("Day of year", "%d - equation of time %s min, declination %s deg"
                 % (n, _f(sun["et_min"], 2), _f(sun["declination"], 2)))
        a.result("Apparent solar time", "%s h (hour angle %s deg)" % (_f(sun["ast"], 3),
                                                                       _f(sun["hour_angle"], 2)))
        a.result("Sun", "altitude %s deg, azimuth %s deg from south (bearing %s deg)"
                 % (_f(sun["altitude"], 3), _f(sun["azimuth"], 3),
                    _f((sun["azimuth"] + 180.0) % 360.0, 1)))
        if sun["altitude"] <= 0:
            a.result("Irradiance", "none - the sun is below the horizon")
            return
        a.result("Clear sky", "beam normal %s W/m2, diffuse horizontal %s W/m2 (air mass %s)"
                 % (_f(beam, 1), _f(diffuse, 1), _f(m, 3)))
        a.result("Incidence on the surface", "%s deg" % _f(here["incidence"], 2))
        a.result("Beam on the surface", "%s W/m2" % _f(here["beam"], 1))
        a.result("Sky diffuse", "%s W/m2 (Y %s)" % (_f(here["diffuse"], 1), _f(here["y"], 4)))
        a.result("Ground reflected", "%s W/m2" % _f(here["reflected"], 1))
        a.result("Total on the surface", "%s W/m2 - the irradiance_w_m2 for cooling_load"
                 % _f(here["total"], 1))
        return
    rows, peak = [], None
    for h in range(1, 25):
        sun, beam, diffuse, m, here = at(float(h))
        if sun["altitude"] <= 0:
            continue
        rows.append([h, _f(sun["altitude"], 1), _f(here["incidence"], 1),
                     _f(here["beam"], 0), _f(here["diffuse"], 0), _f(here["reflected"], 0),
                     _f(here["total"], 0)])
        if peak is None or here["total"] > peak[1]:
            peak = (h, here["total"], here["incidence"])
    a.table("Clear-sky irradiance on the surface, %s/%s, W/m2 (local standard time)"
            % (day, month), ("hour", "altitude", "incidence", "beam", "diffuse",
                             "reflected", "total"), rows)
    if peak is not None:
        a.result("Peak on this surface", "%s W/m2 at %02d:00 (incidence %s deg) - the "
                 "irradiance_w_m2 for cooling_load" % (_f(peak[1], 1), peak[0], _f(peak[2], 1)))


# --- loads -----------------------------------------------------------------

# ASHRAE's long-wave correction e.dR/ho in the sol-air temperature: 0 for a
# vertical surface, which sees as much sky as ground, and for a horizontal one
# facing the sky dR = 63 W/m2 with e = 1 and ho = 17 W/m2.K - 3.7 K, exactly
# as ASHRAE's own 2017 load-calculation workbook computes it (20 Btu/h.ft2
# over 3.0). The Handbook's prose rounds it to "about 7 F", 3.9 K; docs/41 s12
# records both. Part of the METHOD, not a design value.
LONGWAVE_HORIZONTAL_K = 63.0 / 17.0


class _Outside(object):
    """
    The outdoor and room air a load calculation needs, read once and only
    where the calculation uses it - an input a calculation never reads must not
    appear in its catalogue entry.

    outdoor: True (needed), False (offered as optional) or None (not read).
    """

    def __init__(self, a, outdoor, need_humidity, need_air):
        self.a = a
        self.tr = a.number("room_dry_bulb_c", "C", "room design dry bulb", 5, 40)
        self.to = self.ho = None
        if outdoor is not None:
            self.to = a.number("outdoor_dry_bulb_c", "C", "outdoor design dry bulb - the "
                               "site's design condition for this load", -60, 60,
                               required=outdoor, reference=offer_table("design_weather"))
            self.ho = a.humidity("outdoor_", "outdoor design humidity - usually the "
                                 "coincident wet bulb", required=need_humidity)
        self.hr = a.humidity("room_", "room design humidity", required=need_humidity)
        self.standard = None
        self.p = None
        self.so = self.sr = None
        if need_air or need_humidity:
            self.standard = a.flag("standard_air", "true for ASHRAE's standard-air "
                                   "coefficients 1.23 / 3010 / 1.20 instead of the "
                                   "air's own properties at the site altitude")
            if self.standard:
                if a.has("altitude_m") or a.has("pressure_kpa"):
                    a.refuse("standard_air was asked for AND an altitude or pressure was "
                             "given - say which is meant")
                self.p = PSY.STANDARD_PRESSURE_KPA
            else:
                self.p = a.pressure_kpa("site altitude, for air density")

    def states(self):
        """The outdoor and room states, where a humidity was given for them."""
        a = self.a
        if self.p is None:
            return
        if self.ho is not None and self.to is not None:
            self.so = _state(a, self.to, self.p, self.ho)
        if self.hr is not None and self.tr is not None:
            self.sr = _state(a, self.tr, self.p, self.hr)

    def air_load(self, flow_ls):
        """
        (sensible W, latent W) of outdoor air brought to the room condition.
        Latent needs both humidities; without them it is not counted, and the
        answer says so.
        """
        wo = self.so["w"] if self.so else None
        wr = self.sr["w"] if self.sr else None
        if wo is None or wr is None:
            self.a.assume("outdoor or room humidity not given, so the latent load of "
                          "outdoor air is not counted")
        return air_heat(self.standard, flow_ls, self.to, self.tr, wo, wr, self.p)


def air_heat(standard, flow_ls, to, tr, wo, wr, p):
    """
    (sensible W, latent W) of `flow_ls` of outdoor air at `to` C and humidity
    ratio `wo` brought to the room's `tr` and `wr` - in standard air by ASHRAE's
    coefficients, otherwise by the air's own mass and enthalpy at `p` kPa.
    Latent is 0 when either humidity is None; the caller says so.
    """
    if standard:
        qs = STANDARD_SENSIBLE * flow_ls * (to - tr)
        ql = STANDARD_LATENT * flow_ls * (wo - wr) if None not in (wo, wr) else 0.0
        return qs, ql
    w_v = wo if wo is not None else 0.0
    m = flow_ls / 1000.0 / PSY.specific_volume(to, w_v, p)
    qs = m * (1.006 + 1.86 * w_v) * (to - tr) * 1000.0
    ql = m * (wo - wr) * (2501.0 + 1.86 * tr) * 1000.0 if None not in (wo, wr) else 0.0
    return qs, ql


def _check_qcs(a, qcs):
    """A load answer's line on the QCS edition the project follows (D-111)."""
    if qcs is None:
        a.check("WARN", "not checked: which edition of the QCS governs this project is "
                        "not known yet")
    elif qcs == "QCS 2014":
        a.check("WARN", "QCS 2014 governs this project: its Section 22 is reported - in "
                        "search summaries only - to set %s. Heron could not read the QCS "
                        "text, so confirm the conditions used here against it"
                % QCS_2014_REPORTED)
    elif qcs != NO_STANDARD:
        a.check("WARN", "%s governs this project and Heron holds nothing from it - take "
                        "its design conditions from the text" % qcs)


def _surface_needs_outdoor(view):
    return not view.has("temp_difference_k")


def _opaque(a, out, view, label, horizontal, rows):
    area = view.number("area_m2", "m2", "net area of this %s" % label, 0, 100000, positive=True)
    u = view.number("u_w_m2k", "W/m2.K", "U-value of this %s" % label, 0.01, 10)
    modes = [m for m in ("temp_difference_k", "irradiance_w_m2", "shaded") if view.has(m)]
    if not modes:
        a.need(view.name("irradiance_w_m2"),
               "W/m2 (or temp_difference_k, or shaded: true)",
               "the sun on this %s - its peak incident irradiance with absorptance_over_ho, "
               "or an equivalent temperature difference, or shaded: true" % label,
               "the `solar` calculation works out the clear-sky irradiance on a surface")
        return
    if len(modes) > 1:
        a.refuse("%s: give ONE of temp_difference_k, irradiance_w_m2 or shaded - got %s"
                 % (view.prefix, ", ".join(modes)))
        return
    if modes[0] == "temp_difference_k":
        dt = view.number("temp_difference_k", "K", "equivalent temperature difference", -30, 80)
        if area is None or u is None or dt is None:
            return
        q, how = u * area * dt, "U.A.dT with dT %s K as given" % _f(dt, 1)
    elif modes[0] == "irradiance_w_m2":
        e = view.number("irradiance_w_m2", "W/m2", "peak total irradiance on this surface", 0, 1400)
        aho = view.number("absorptance_over_ho", "m2.K/W", "solar absorptance over the outside "
                          "surface coefficient", 0, 0.2,
                          reference=offer_table("sol_air"))
        if area is None or u is None or e is None or aho is None or out.to is None:
            return
        te = out.to + aho * e - (LONGWAVE_HORIZONTAL_K if horizontal else 0.0)
        q = u * area * (te - out.tr)
        how = "sol-air %s C" % _f(te, 1)
        a.uses("sol-air temperature te = to + (a/ho).E - %s for a %s, then U.A.(te - "
               "troom) at steady state - no conduction time lag"
               % ("3.7 K" if horizontal else "0", "roof" if horizontal else "wall"))
        a.cite(SRC_SOLAIR)
    else:
        shaded = view.flag("shaded", "true when this surface sees no sun")
        if not shaded:
            a.refuse("%s.shaded is false - give irradiance_w_m2 or temp_difference_k instead"
                     % view.prefix)
            return
        if area is None or u is None or out.to is None:
            return
        q, how = u * area * (out.to - out.tr), "shaded, outdoor air temperature"
    rows.append([view.text("name") or view.prefix, q, 0.0, how])


def _partitions(views, tr, rows):
    """Each partition to an unconditioned space, as a row of the room's load."""
    for view in views or []:
        p_area = view.number("area_m2", "m2", "partition area", 0, 100000, positive=True)
        u = view.number("u_w_m2k", "W/m2.K", "U-value of the partition", 0.01, 10)
        ta = view.number("adjacent_temp_c", "C", "temperature of the space beyond it", -30, 70)
        if None in (p_area, u, ta) or tr is None:
            continue
        rows.append([view.text("name") or view.prefix, u * p_area * (ta - tr), 0.0,
                     "U.A.(tadjacent - troom)"])


def _internal_gains(a, area, rows):
    people = a.record("people", "the people in the room: count, sensible_w_each, "
                      "latent_w_each", required=False)
    if people is not None:
        n = people.integer("count", "how many people at the design hour", 0, 100000)
        s = people.number("sensible_w_each", "W", "sensible heat per person at their "
                          "activity", 0, 1000, reference=offer_table("people_heat_gain"))
        l = people.number("latent_w_each", "W", "latent heat per person at their "
                          "activity", 0, 1000, reference=offer_table("people_heat_gain"))
        if None not in (n, s, l):
            rows.append(["people (%d)" % n, n * s, n * l, "count x heat per person"])
    lighting = a.record("lighting", "the lights: watts or w_per_m2, and use_factor and "
                        "allowance_factor if they apply", required=False)
    if lighting is not None:
        _electric(a, lighting, "lighting", area, rows, latent=False)
    for view in a.records("equipment", "equipment: watts or w_per_m2, use_factor, "
                          "latent_w", required=False) or []:
        _electric(a, view, "equipment", area, rows, latent=True)


def _electric(a, view, label, area, rows, latent):
    has_w, has_d = view.has("watts"), view.has("w_per_m2")
    if has_w == has_d:
        if has_w:
            a.refuse("%s: give watts or w_per_m2, not both" % view.prefix)
        else:
            a.need(view.name("watts"), "W (or w_per_m2)", "installed %s power" % label,
                   offer_table("equipment_density") if label == "equipment" else None)
        return
    watts = (view.number("watts", "W", "installed %s power" % label, 0, 1e7) if has_w
             else view.number("w_per_m2", "W/m2", "%s power density" % label, 0, 2000))
    use = view.number("use_factor", "0-1", "share of it on at the design hour - "
                      "not applied if not given", 0, 1, required=False)
    allow = view.number("allowance_factor", "factor", "special allowance for ballasts "
                        "or drivers - not applied if not given", 1, 3, required=False)
    lat = view.number("latent_w", "W", "latent heat from it - none if not given", 0, 1e6,
                      required=False) if latent else None
    if watts is None or (has_d and area is None):
        return
    total = watts if has_w else watts * area
    factor = (1.0 if use is None else use) * (1.0 if allow is None else allow)
    if use is None:
        a.assume("%s: no use factor given, so all of it is on at the design hour" % label)
    rows.append([view.text("name") or label, total * factor, lat or 0.0,
                 "%s W x %s" % (_f(total, 0), _f(factor, 2))])


@calculation("cooling_load", "Room cooling load - peak component estimate", "load")
def calc_cooling_load(a):
    """A room's peak cooling load - walls, roofs, glass, partitions, people, lights, equipment and infiltration - split into sensible and latent, with the supply air it needs and the coil load once outdoor air is added.

    Not an hourly simulation. Every answer says so (NOT_HAP).
    """
    qcs = project_standards(a)["qcs_edition"]
    area = a.number("floor_area_m2", "m2", "floor area of the room", 0.5, 1e6, positive=True)
    height = a.number("room_height_m", "m", "room height - for the volume, ACH and an "
                      "infiltration rate given in ACH", 1.5, 60, required=False, positive=True)
    walls = a.records("walls", "walls: area_m2, u_w_m2k and the sun on each", required=False)
    roofs = a.records("roofs", "roofs: area_m2, u_w_m2k and the sun on each", required=False)
    windows = a.records("windows", "glazing: area_m2, u_w_m2k, shgc, irradiance_w_m2, "
                        "iac", required=False)
    partitions = a.records("partitions", "partitions to unconditioned space: area_m2, "
                           "u_w_m2k, adjacent_temp_c", required=False)
    infiltration = a.record("infiltration", "infiltration: flow_ls or ach", required=False)
    oa = a.flow("outdoor air through the unit serving the room - a COIL load, not a "
                "room load", prefix="outdoor_air", required=False)
    supply_t = a.number("supply_dry_bulb_c", "C", "supply air temperature - gives the "
                        "supply airflow", 2, 30, required=False)
    safety = a.number("safety_factor_pct", "%", "a margin added to the room loads - "
                      "none if not given", 0, 50, required=False)
    fan_heat = a.number("fan_heat_w", "W", "supply fan heat in the airstream - none "
                        "if not given", 0, 1e7, required=False)

    need_outdoor = bool(windows or infiltration or oa
                        or any(_surface_needs_outdoor(v) for v in (walls or []) + (roofs or [])))
    need_humidity = bool(infiltration or oa)
    out = _Outside(a, need_outdoor, need_humidity, bool(supply_t is not None))
    rows = []
    for view in walls or []:
        _opaque(a, out, view, "wall", False, rows)
    for view in roofs or []:
        _opaque(a, out, view, "roof", True, rows)
    for view in windows or []:
        g_area = view.number("area_m2", "m2", "glass area", 0, 100000, positive=True)
        u = view.number("u_w_m2k", "W/m2.K", "U-value of the glazing", 0.1, 10)
        shgc = view.number("shgc", "0-1", "solar heat gain coefficient", 0, 1)
        e = view.number("irradiance_w_m2", "W/m2", "peak total irradiance on the glass "
                        "(0 if it never sees the sun)", 0, 1400,
                        reference="the `solar` calculation works out the clear-sky "
                                  "irradiance on a surface")
        iac = view.number("iac", "0-1", "interior attenuation from blinds - not applied "
                          "if not given", 0, 1, required=False)
        if None in (g_area, u, shgc, e) or out.to is None or out.tr is None:
            continue
        if iac is None:
            a.assume("glazing: no interior attenuation (iac) given, so no blinds are credited")
        rows.append([(view.text("name") or view.prefix) + " conduction",
                     u * g_area * (out.to - out.tr), 0.0, "U.A.(to - troom)"])
        rows.append([(view.text("name") or view.prefix) + " solar",
                     g_area * shgc * e * (1.0 if iac is None else iac), 0.0,
                     "A x SHGC x E x IAC"])
    _partitions(partitions, out.tr, rows)
    _internal_gains(a, area, rows)
    infiltration_ls = None
    if infiltration is not None:
        infiltration_ls = _infiltration(a, infiltration, area, height)
    if a.incomplete():
        return
    out.states()
    if a.incomplete():
        return
    if infiltration_ls is not None:
        qs, ql = out.air_load(infiltration_ls)
        rows.append(["infiltration", qs, ql, "%s L/s of outdoor air" % _f(infiltration_ls, 1)])
    if fan_heat is not None:
        rows.append(["supply fan heat", fan_heat, 0.0, "as given"])

    sensible = sum(r[1] for r in rows)
    latent = sum(r[2] for r in rows)
    if safety is not None:
        rows.append(["safety factor %s %%" % _f(safety, 1), sensible * safety / 100.0,
                     latent * safety / 100.0, "on the room loads above"])
        sensible *= 1.0 + safety / 100.0
        latent *= 1.0 + safety / 100.0
    total = sensible + latent
    a.table("Room load by component", ("component", "sensible W", "latent W", "how"),
            [[r[0], _f(r[1], 0), _f(r[2], 0), r[3]] for r in rows])
    a.result("Room sensible load", power_text(sensible))
    a.result("Room latent load", power_text(latent))
    a.result("Room total load", power_text(total))
    if total > 0:
        a.result("Room sensible heat ratio", _f(sensible / total, 3))
    a.result("Load density", "%s W/m2  (%s m2 per TR)" % (_f(total / area, 1),
                                                        _f(area / (total / W_PER_TR), 1)
                                                        if total > 0 else "-"))
    for missing in ("people", "lighting", "equipment", "infiltration"):
        if not a.has(missing):
            a.assume("no %s given, so none is counted" % missing)
    if supply_t is not None:
        _supply_from_load(a, out, sensible, latent, supply_t, area, height)
    if out.so is not None and out.sr is not None and out.so["w"] < out.sr["w"]:
        a.check("WARN", "the outdoor air at this condition is DRIER than the room "
                        "(%s against %s g/kg), so its latent load is negative. A peak "
                        "dry-bulb condition is the wrong basis for latent and coil "
                        "sizing - use the site's dehumidification design condition, as "
                        "ASHRAE's climatic data gives it" % (_f(out.so["w"] * 1000, 2),
                                                             _f(out.sr["w"] * 1000, 2)))
    if oa is not None:
        oqs, oql = out.air_load(oa)
        a.result("Outdoor air load", "%s sensible, %s latent"
                 % (_f(oqs, 0) + " W", _f(oql, 0) + " W"))
        a.result("Coil load (room + outdoor air)", power_text(total + oqs + oql))
        a.uses("coil load = room load + outdoor air brought from the outdoor to the room "
               "condition; duct heat gain and return-air gains are not included")
    _check_qcs(a, qcs)
    a.uses(NOT_HAP)
    a.cite(SRC_LOADS)
    if out.standard:
        a.cite(SRC_STANDARD_AIR)
    else:
        a.cite(SRC_PSYCHRO)
    a.into_revit("READ_SPACE_LOADS reads what Revit's own analysis put on the Space, to "
                 "compare; WRITE_ELEMENT_PARAMETERS sets a Space's design airflow")


def _infiltration(a, view, area, height):
    if view.has("ach"):
        ach = view.number("ach", "1/h", "infiltration air changes per hour", 0, 20)
        if height is None:
            a.need("room_height_m", "m", "room height - an infiltration rate in ACH needs "
                   "the volume")
            return None
        return None if ach is None else ach * area * height / 3.6
    return view.flow("infiltration airflow", prefix="flow")


def _supply_from_load(a, out, sensible, latent, supply_t, area, height):
    if out.tr is None:
        return
    dt = out.tr - supply_t
    if dt <= 0:
        a.refuse("supply air at %s C is not below the room at %s C - it cannot cool it"
                 % (_g(supply_t), _g(out.tr)))
        return
    if out.standard:
        q = sensible / (STANDARD_SENSIBLE * dt)
        a.uses("supply airflow Q = qs / (1.23 dT) in standard air")
        ws_needed = None
        if out.sr is not None and latent > 0:
            ws_needed = out.sr["w"] - latent / (STANDARD_LATENT * q)
    else:
        w_room = out.sr["w"] if out.sr is not None else 0.0
        if out.sr is None:
            a.assume("no room humidity given, so dry-air specific heat for the supply "
                     "airflow - about 1-2 % more air than with the room's moisture")
        m = sensible / 1000.0 / ((1.006 + 1.86 * w_room) * dt)
        try:
            v_supply = PSY.specific_volume(supply_t, w_room, out.p)
        except PSY.PsychroRangeError as why:
            a.refuse(str(why))
            return
        q = m * v_supply * 1000.0
        a.uses("supply airflow from m = qs / (cp dT), cp = 1.006 + 1.86 W, as a volume "
               "at the supply temperature")
        ws_needed = None
        if out.sr is not None and latent > 0:
            ws_needed = out.sr["w"] - latent / 1000.0 / (m * (2501.0 + 1.86 * out.tr))
    a.result("Supply airflow", flow_text(q) + " at %s C supply, dT %s K"
             % (_f(supply_t, 1), _f(dt, 1)))
    if area is not None:
        a.result("Supply per floor area", "%s L/s per m2" % _f(q / area, 2))
        if height is not None:
            a.result("Supply air changes", "%s per hour" % _f(q * 3.6 / (area * height), 1))
    if ws_needed is not None:
        if ws_needed <= 0:
            a.check("FAIL", "the room latent load cannot be removed by this airflow at any "
                            "supply humidity - more air, or a lower latent load")
        else:
            try:
                tdp = PSY.dew_point(supply_t, ws_needed, out.p)
            except PSY.PsychroRangeError:
                tdp = None
            a.result("Supply humidity needed", "at most %s g/kg - a dew point of %s"
                     % (_f(ws_needed * 1000.0, 2), temp_text(tdp) if tdp is not None else "-"))
            if tdp is not None and tdp > supply_t:
                a.check("WARN", "that dew point is above the supply temperature - "
                                "check the supply condition")
            a.uses("supply humidity ratio Ws = Wroom - ql / (m hfg): the moisture the "
                   "supply air must leave room for")
    elif latent > 0:
        a.assume("no room humidity given, so the supply humidity the latent load needs "
                 "was not worked out")
    a.into_revit("terminal_flows splits this airflow across the room's diffusers as a "
                 "file SET_AIR_TERMINAL_FLOW reads")


# --- month by month ----------------------------------------------------------

# The design day's hourly shape - ASHRAE Fundamentals' procedure, as ASHRAE's
# own 2017 load-calculation workbook carries it ("Weather Data - OA", column
# O): the fraction of the daily range the dry bulb sits below its design value
# at each hour, 1 to 24, local standard time. The same fraction of the
# coincident wet-bulb range takes the wet bulb down, and the wet bulb is never
# above the dry bulb. Reproduced against the workbook's own Atlanta table within
# its 0.1 F display rounding (docs/41 s4). Part of the METHOD, not a design
# value.
DESIGN_DAY_FRACTION = (0.88, 0.92, 0.95, 0.98, 1.00, 0.98, 0.91, 0.74, 0.55, 0.38,
                       0.23, 0.13, 0.05, 0.00, 0.00, 0.06, 0.14, 0.24, 0.39, 0.50,
                       0.59, 0.68, 0.75, 0.82)

SRC_DESIGN_DAY = ("ASHRAE Handbook - Fundamentals (SI), Ch. 14 Climatic Design "
                  "Information - the design-day temperature profile, as ASHRAE's own "
                  "2017 load-calculation workbook carries it")

STEADY_HOURS = ("HOUR-BY-HOUR STEADY STATE: on each month's design day every gain is "
                "taken at its own hour and summed, which finds the peak month and hour - "
                "but with no thermal storage or time lag, so the radiant part of a gain "
                "is not delayed. An hourly method with radiant time series or heat "
                "balance (Carrier HAP, Trane TRACE, IES) shifts the peak later and "
                "lowers it - use one to select plant.")

# The weather sets Heron holds, by the name a modeller says. Naming one is the
# modeller choosing the design data AND its percentile (D-33) - nothing runs on
# a set nobody named.
WEATHER_SETS = collections.OrderedDict([("doha-0.4", "design_weather")])

_MONTH_NAMES = ("jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct",
                "nov", "dec")


def design_day(db_c, mcwb_c, db_range_k, wb_range_k):
    """[(hour, dry bulb C, wet bulb C)] for hours 1 to 24 of one design day."""
    out = []
    for hour, f in enumerate(DESIGN_DAY_FRACTION, 1):
        t = db_c - f * db_range_k
        out.append((hour, t, min(mcwb_c - f * wb_range_k, t)))
    return out


def _design_months(a):
    """
    (months, source) - each month a dict of its design-day figures - from a
    weather set Heron holds that the modeller NAMED, or from the modeller's own
    rows. (None, None) while that is still a question.
    """
    named, own = a.has("design_weather"), a.has("months")
    if named and own:
        a.refuse("give design_weather or months, not both - two sets of weather would "
                 "usually disagree, and preferring one would hide it")
        return None, None
    if not named and not own:
        a.need("design_weather", "one of: %s (or months)" % ", ".join(WEATHER_SETS),
               "the design weather each month is run on - a set Heron holds, named, or "
               "months: [{month, db_c, mcwb_c, db_range_k, wb_range_k, tau_b, tau_d}]",
               offer_table("design_weather"))
        return None, None
    if named:
        key = a.choice("design_weather", tuple(WEATHER_SETS), "the design weather set")
        if key is None:
            return None, None
        data = REFERENCES[WEATHER_SETS[key]]
        months = []
        for row in data["rows"]:
            got = dict(zip(data["columns"], row))
            months.append({"month": _MONTH_NAMES.index(row[0]) + 1,
                           "db": got["0.4 % DB C"], "mcwb": got["mean coincident WB C"],
                           "dbr": got["daily DB range K"], "wbr": got["coincident WB range K"],
                           "tau_b": got["tau_b"], "tau_d": got["tau_d"]})
        return months, data["source"]
    months, seen = [], set()
    for row in a.records("months", "each month to run: month, db_c, mcwb_c, db_range_k, "
                         "wb_range_k, tau_b, tau_d") or []:
        month = row.integer("month", "the month, 1 to 12", 1, 12)
        months.append({
            "month": month,
            "db": row.number("db_c", "C", "the month's design dry bulb", -30, 60),
            "mcwb": row.number("mcwb_c", "C", "its mean coincident wet bulb", -30, 40),
            "dbr": row.number("db_range_k", "K", "its mean daily dry-bulb range", 0, 30),
            "wbr": row.number("wb_range_k", "K", "its coincident wet-bulb range", 0, 30),
            "tau_b": row.number("tau_b", "-", "its clear-sky beam optical depth", 0.1, 2.0),
            "tau_d": row.number("tau_d", "-", "its clear-sky diffuse optical depth",
                                0.5, 4.0)})
        if month is not None and month in seen:
            a.refuse("month %d is given twice" % month)
        seen.add(month)
    return (sorted(months, key=lambda m: m["month"] or 0),
            "the modeller's own design weather")


def _sweep_surfaces(views, kind):
    """
    The surfaces of one kind, read once, as dicts. A wall and a window are
    vertical and a roof and a skylight are flat - what each word means - and
    the long-wave correction is a roof's alone, as in cooling_load.
    """
    out = []
    for view in views or []:
        opaque = kind in ("wall", "roof")
        area = view.number("area_m2", "m2", "net area of this %s" % kind, 0, 100000,
                           positive=True)
        u = view.number("u_w_m2k", "W/m2.K", "U-value of this %s" % kind,
                        0.01 if opaque else 0.1, 10)
        facing = _facing(view) if kind in ("wall", "window") else 180.0
        aho = shgc = iac = None
        if opaque:
            aho = view.number("absorptance_over_ho", "m2.K/W", "solar absorptance over the "
                              "outside surface coefficient", 0, 0.2,
                              reference=offer_table("sol_air"))
        else:
            shgc = view.number("shgc", "0-1", "solar heat gain coefficient", 0, 1)
            iac = view.number("iac", "0-1", "interior attenuation from blinds - not "
                              "applied if not given", 0, 1, required=False)
        shaded = view.flag("no_direct_sun", "true when no direct sun reaches this %s - "
                           "sky and ground light still count" % kind)
        if None in (area, u, facing) or (opaque and aho is None) or \
                (not opaque and shgc is None):
            continue
        out.append({"name": view.text("name") or view.prefix, "kind": kind, "area": area,
                    "u": u, "facing": facing, "tilt": 90.0 if kind in ("wall", "window") else 0.0,
                    "aho": aho, "shgc": shgc, "iac": iac, "direct": not shaded,
                    "said": shaded is not None})
    return out


def _sun_on(surfaces, sun, beam, diffuse, rho):
    """The irradiance on each surface at one hour, W/m2 - beam left out where no direct sun reaches."""
    if sun["altitude"] <= 0:
        return [0.0] * len(surfaces)
    out = []
    for s in surfaces:
        here = on_surface(sun["altitude"], sun["azimuth"], beam, diffuse, s["facing"] - 180.0,
                          s["tilt"], rho)
        out.append(here["total"] - (0.0 if s["direct"] else here["beam"]))
    return out


@calculation("monthly_load", "Room cooling load month by month - the peak month and hour",
             "load")
def calc_monthly_load(a):
    """A room's cooling load on each month's design day, hour by hour - outdoor temperature and humidity from ASHRAE's design-day profile, the sun on every wall, roof and window by the clear-sky model - with each month's peak, the peak month and hour, and the lowest month.

    Hour-by-hour steady state with no storage or time lag, so not HAP either, and every answer says so.
    """
    qcs = project_standards(a)["qcs_edition"]
    area = a.number("floor_area_m2", "m2", "floor area of the room", 0.5, 1e6, positive=True)
    height = a.number("room_height_m", "m", "room height - for an infiltration rate given "
                      "in ACH", 1.5, 60, required=False, positive=True)
    lat = a.number("latitude_deg", "degrees", "site latitude, north positive", -90, 90)
    lon = a.number("longitude_deg", "degrees", "site longitude, EAST positive - Doha is "
                   "about 51.6", -180, 180)
    tz = a.number("utc_offset_h", "hours", "the site's STANDARD time zone - Qatar is +3",
                  -12, 14)
    rho = a.number("ground_reflectance", "0-1", "the ground's solar reflectance in front "
                   "of the walls and windows", 0, 1,
                   reference="ASHRAE's own 2017 worked example uses 0.2 - offer it, do not "
                             "assume it")
    months, source = _design_months(a)
    tr = a.number("room_dry_bulb_c", "C", "room design dry bulb", 5, 40)
    surfaces = []
    for key, kind in (("walls", "wall"), ("roofs", "roof"), ("windows", "window"),
                      ("skylights", "skylight")):
        surfaces += _sweep_surfaces(a.records(key, "%s: area_m2, u_w_m2k and the rest"
                                              % key, required=False), kind)
    constant = []
    _partitions(a.records("partitions", "partitions to unconditioned space: area_m2, "
                          "u_w_m2k, adjacent_temp_c", required=False), tr, constant)
    _internal_gains(a, area, constant)
    infiltration = a.record("infiltration", "infiltration: flow_ls or ach", required=False)
    oa = a.flow("outdoor air through the unit serving the room - a COIL load, not a "
                "room load", prefix="outdoor_air", required=False)
    need_air = bool(infiltration is not None or oa is not None)
    hr = a.humidity("room_", "room design humidity", required=need_air)
    standard = p = None
    if need_air:
        standard = a.flag("standard_air", "true for ASHRAE's standard-air coefficients "
                          "1.23 / 3010 / 1.20 instead of the air's own properties at the "
                          "site altitude")
        if standard:
            if a.has("altitude_m") or a.has("pressure_kpa"):
                a.refuse("standard_air was asked for AND an altitude or pressure was "
                         "given - say which is meant")
            p = PSY.STANDARD_PRESSURE_KPA
        else:
            p = a.pressure_kpa("site altitude, for air density")
    infiltration_ls = None
    if infiltration is not None:
        infiltration_ls = _infiltration(a, infiltration, area, height)
    if a.incomplete():
        return
    if not surfaces and not constant and not need_air:
        a.refuse("nothing in the room gains heat - give walls, roofs, windows, people, "
                 "lighting, equipment, partitions, infiltration or outdoor air")
        return
    wr = None
    if need_air:
        room = _state(a, tr, p, hr)
        if room is None:
            return
        wr = room["w"]
    base_s = sum(r[1] for r in constant)
    base_l = sum(r[2] for r in constant)

    peaks = []
    for m in months:
        n = day_of_year(m["month"], 21)
        hours = []
        for hour, t, twb in design_day(m["db"], m["mcwb"], m["dbr"], m["wbr"]):
            sun = sun_position(lat, lon, tz, n, float(hour))
            beam, diffuse, _m = (clear_sky(sun["eo"], sun["altitude"], m["tau_b"], m["tau_d"])
                                 if sun["altitude"] > 0 else (0.0, 0.0, None))
            sens, lat_ = base_s, base_l
            for s, e in zip(surfaces, _sun_on(surfaces, sun, beam, diffuse, rho)):
                if s["kind"] in ("wall", "roof"):
                    te = t + s["aho"] * e - (LONGWAVE_HORIZONTAL_K if s["kind"] == "roof"
                                             else 0.0)
                    sens += s["u"] * s["area"] * (te - tr)
                else:
                    sens += s["u"] * s["area"] * (t - tr)
                    sens += s["area"] * s["shgc"] * e * (1.0 if s["iac"] is None else s["iac"])
            wo = PSY.humidity_ratio_from_wet_bulb(t, twb, p) if need_air else None
            if infiltration_ls is not None:
                qs, ql = air_heat(standard, infiltration_ls, t, tr, wo, wr, p)
                sens, lat_ = sens + qs, lat_ + ql
            coil = None
            if oa is not None:
                oqs, oql = air_heat(standard, oa, t, tr, wo, wr, p)
                coil = sens + lat_ + oqs + oql
            hours.append({"hour": hour, "t": t, "twb": twb, "s": sens, "l": lat_,
                          "total": sens + lat_, "coil": coil, "w": wo})
        peaks.append((m, hours, max(hours, key=lambda h: h["total"])))

    columns = ["month", "hour", "DB C", "WB C", "sensible W", "latent W", "total W"]
    if oa is not None:
        columns.append("peak coil W")
    rows = []
    for m, hrs, pk in peaks:
        row = [_MONTH_NAMES[m["month"] - 1], "%02d:00" % pk["hour"], _f(pk["t"], 1),
               _f(pk["twb"], 1), _f(pk["s"], 0), _f(pk["l"], 0), _f(pk["total"], 0)]
        if oa is not None:
            row.append(_f(max(h["coil"] for h in hrs), 0))
        rows.append(row)
    a.table("Each month's peak - the 21st, local standard time", columns, rows)
    top_m, top_hours, top = max(peaks, key=lambda x: x[2]["total"])
    low_m, _low_hours, low = min(peaks, key=lambda x: x[2]["total"])

    def when(m, h):
        return "%s 21 at %02d:00" % (_MONTH_NAMES[m["month"] - 1], h["hour"])

    a.result("Peak room load", "%s - %s, %s C DB / %s C WB outdoors"
             % (power_text(top["total"]), when(top_m, top), _f(top["t"], 1), _f(top["twb"], 1)))
    a.result("At the peak", "%s W sensible, %s W latent - sensible heat ratio %s"
             % (_f(top["s"], 0), _f(top["l"], 0),
                _f(top["s"] / top["total"], 3) if top["total"] > 0 else "-"))
    if top["total"] > 0:
        a.result("Load density at the peak", "%s W/m2  (%s m2 per TR)"
                 % (_f(top["total"] / area, 1), _f(area / (top["total"] / W_PER_TR), 1)))
    if len(peaks) > 1:
        a.result("Lowest monthly peak", "%s - %s; the smallest of the months run, not a "
                 "part-load figure" % (power_text(low["total"]), when(low_m, low)))
    if oa is not None:
        coil_m, coil_h = max(((m, h) for m, hs, _pk in peaks for h in hs),
                             key=lambda x: x[1]["coil"])
        a.result("Peak coil load (room + outdoor air)", "%s - %s"
                 % (power_text(coil_h["coil"]), when(coil_m, coil_h)))
    day = [[("%02d:00" % h["hour"]), _f(h["t"], 1), _f(h["twb"], 1), _f(h["s"], 0),
            _f(h["l"], 0), _f(h["total"], 0)] + ([_f(h["coil"], 0)] if oa is not None else [])
           for h in top_hours]
    a.table("The peak day hour by hour - %s 21" % _MONTH_NAMES[top_m["month"] - 1],
            ["hour", "DB C", "WB C", "sensible W", "latent W", "total W"]
            + (["coil W"] if oa is not None else []), day)
    if wr is not None and top["w"] is not None and top["w"] < wr:
        a.check("WARN", "the outdoor air at the peak hour is DRIER than the room (%s against "
                        "%s g/kg), so its latent load is negative. A peak dry-bulb day is the "
                        "wrong basis for latent and coil sizing - use the site's "
                        "dehumidification design condition, as ASHRAE's climatic data gives "
                        "it" % (_f(top["w"] * 1000, 2), _f(wr * 1000, 2)))
    _check_qcs(a, qcs)
    for s in surfaces:
        if s["kind"] in ("window", "skylight") and s["iac"] is None:
            a.assume("glazing: no interior attenuation (iac) given, so no blinds are credited")
    if any(not s["said"] for s in surfaces):
        a.assume("a surface not marked no_direct_sun takes the full clear-sky sun - no "
                 "shading is credited")
    for missing in ("people", "lighting", "equipment", "infiltration"):
        if not a.has(missing):
            a.assume("no %s given, so none is counted" % missing)
    a.uses(STEADY_HOURS)
    a.uses("outdoor t(h) = DB - f(h).DR and wet bulb = min(MCWB - f(h).WBR, t(h)), with "
           "ASHRAE's design-day fractions f(h), hours 1 to 24 local standard time")
    if source != "the modeller's own design weather":
        a.uses("design weather doha-0.4: each month's 0.4 % dry bulb and mean coincident wet "
               "bulb, with the daily ranges ASHRAE's workbook lists beside its 5 % dry bulb")
    a.uses("the sun on the 21st of each month at every hour by the clear-sky model, from that "
           "month's optical depths; a roof and a skylight flat, a wall and a window vertical")
    a.uses("sol-air te = to + (a/ho).E - 3.7 K for a roof, 0 for a wall, then U.A.(te - "
           "troom); glass A x SHGC x E x IAC plus U.A.(to - troom)")
    a.cite(SRC_LOADS, SRC_DESIGN_DAY, SRC_SOLAR, source)
    if any(s["kind"] in ("wall", "roof") for s in surfaces):
        a.cite(SRC_SOLAIR)
    if need_air:
        a.cite(SRC_STANDARD_AIR if standard else SRC_PSYCHRO)
    a.into_revit("READ_SPACE_LOADS reads what Revit's own analysis put on the Space, to "
                 "compare; WRITE_ELEMENT_PARAMETERS sets a Space's design airflow")


@calculation("heating_load", "Room heating load", "load")
def calc_heating_load(a):
    """A room's design heat loss - conduction through walls, roofs, glass, floors and partitions, and infiltration - with no credit for people, lights or sun, and the outdoor-air heating at the coil."""
    area = a.number("floor_area_m2", "m2", "floor area of the room", 0.5, 1e6, positive=True)
    height = a.number("room_height_m", "m", "room height - for an infiltration rate in ACH",
                      1.5, 60, required=False, positive=True)
    surfaces = a.records("surfaces", "every surface losing heat: area_m2, u_w_m2k, and "
                         "adjacent_temp_c where it is not outdoors")
    infiltration = a.record("infiltration", "infiltration: flow_ls or ach", required=False)
    oa = a.flow("outdoor air through the unit - a COIL load", prefix="outdoor_air",
                required=False)
    safety = a.number("safety_factor_pct", "%", "a margin added - none if not given",
                      0, 50, required=False)
    out = _Outside(a, True, False, bool(infiltration or oa))
    rows = []
    for view in surfaces or []:
        s_area = view.number("area_m2", "m2", "surface area", 0, 1e6, positive=True)
        u = view.number("u_w_m2k", "W/m2.K", "U-value", 0.01, 10)
        ta = view.number("adjacent_temp_c", "C", "temperature beyond it - leave out for "
                         "outdoors", -60, 40, required=False)
        if None in (s_area, u) or out.tr is None or (ta is None and out.to is None):
            continue
        beyond = out.to if ta is None else ta
        rows.append([view.text("name") or view.prefix, u * s_area * (out.tr - beyond),
                     "U.A.(troom - %s)" % ("toutdoor" if ta is None else "tadjacent")])
    infiltration_ls = _infiltration(a, infiltration, area, height) if infiltration else None
    if a.incomplete():
        return
    if infiltration_ls is not None or oa is not None:
        out.states()
    if a.incomplete():
        return
    if infiltration_ls is not None:
        rows.append(["infiltration", -out.air_load(infiltration_ls)[0],
                     "%s L/s of outdoor air" % _f(infiltration_ls, 1)])
    loss = sum(r[1] for r in rows)
    if safety is not None:
        rows.append(["safety factor %s %%" % _f(safety, 1), loss * safety / 100.0, "on the above"])
        loss *= 1.0 + safety / 100.0
    a.table("Heat loss by component", ("component", "W", "how"),
            [[r[0], _f(r[1], 0), r[2]] for r in rows])
    a.result("Room heat loss", power_text(loss))
    a.result("Load density", "%s W/m2" % _f(loss / area, 1))
    if oa is not None:
        oq = -out.air_load(oa)[0]
        a.result("Outdoor air heating", power_text(oq))
        a.result("Coil heating (room + outdoor air)", power_text(loss + oq))
    a.uses("steady-state conduction U.A.dT and sensible air loads; no credit for "
           "internal or solar gains, as a design heating load takes none")
    a.cite(SRC_LOADS)


@calculation("supply_airflow", "Supply airflow from a sensible load", "airflow")
def calc_supply_airflow(a):
    """The supply airflow that removes a room's sensible load at a supply temperature, and - given the latent load and room humidity - the supply moisture it must carry."""
    qs = a.power_w("room sensible load", prefix="sensible_load")
    ql = a.power_w("room latent load - for the supply humidity check", prefix="latent_load",
                   required=False)
    area = a.number("floor_area_m2", "m2", "floor area - for L/s per m2", 0.1, 1e6,
                    required=False, positive=True)
    height = a.number("room_height_m", "m", "room height - for air changes", 1.5, 60,
                      required=False, positive=True)
    supply_t = a.number("supply_dry_bulb_c", "C", "supply air temperature", 2, 30)
    out = _Outside(a, None, False, True)
    if a.incomplete():
        return
    out.states()
    if a.incomplete():
        return
    _supply_from_load(a, out, qs, ql or 0.0, supply_t, area, height)
    a.cite(SRC_LOADS, SRC_STANDARD_AIR if out.standard else SRC_PSYCHRO)


@calculation("air_changes", "Air changes per hour", "airflow")
def calc_air_changes(a):
    """Air changes per hour from an airflow and a room volume - or the airflow a stated number of air changes needs."""
    has_v = a.has("volume_m3")
    volume = (a.number("volume_m3", "m3", "room volume", 0.1, 1e7, positive=True) if has_v
              else None)
    if not has_v:
        area = a.number("floor_area_m2", "m2", "floor area (or volume_m3)", 0.1, 1e6, positive=True)
        height = a.number("room_height_m", "m", "room height (or volume_m3)", 1.0, 60, positive=True)
        volume = None if area is None or height is None else area * height
    has_ach = a.has("ach")
    if has_ach:
        ach = a.number("ach", "1/h", "air changes per hour wanted", 0.01, 200)
    else:
        q = a.flow("the airflow (or ach for the airflow it needs)")
    if a.incomplete():
        return
    a.result("Room volume", "%s m3" % _f(volume, 1))
    if has_ach:
        a.result("Airflow", flow_text(ach * volume / 3.6) + " for %s ACH" % _f(ach, 2))
    else:
        a.result("Air changes", "%s per hour" % _f(q * 3.6 / volume, 2))
    a.uses("ACH = airflow (m3/h) / volume (m3)")


# --- ventilation -------------------------------------------------------------

SYSTEMS = ("single-zone", "100-percent-outdoor-air", "multiple-zone")


def _default_people(occupancy, area):
    """
    The default-density population 62.1 allows ONLY when the real one cannot
    be established (Section 6.2.1.1.7, exception 2) - offered, never used.
    """
    data = REFERENCES.get("ventilation_rates")
    row = reference_lookup("ventilation_rates", occupancy)
    if row is None or data is None:
        return offer_table("ventilation_rates")
    density = row[data["columns"].index("default occupants per 100 m2")]
    if density in (None, ""):
        return "%s lists no default density for %s" % (data["source"], row[0])
    if area is None:
        return ("%s gives a default density of %s people per 100 m2 for %s - allowed "
                "only when the real population cannot be established; offer it, do not "
                "assume it" % (data["source"], _g(density), row[0]))
    return ("%s gives a default density of %s people per 100 m2 for %s - %s people on "
            "%s m2, allowed only when the real population cannot be established; offer "
            "it, do not assume it" % (data["source"], _g(density), row[0],
                                       _f(density * area / 100.0, 1), _g(area)))


@calculation("ventilation", "Outdoor air - ASHRAE 62.1 Ventilation Rate Procedure", "ventilation")
def calc_ventilation(a):
    """The outdoor air a system must take in, by ASHRAE 62.1's Ventilation Rate Procedure - a breathing-zone rate per person and per area, the zone air distribution effectiveness, and for a multiple-zone system its occupant diversity and system ventilation efficiency.

    Every rate is the modeller's: name a zone's `occupancy` and the 62.1 figures Heron holds for it are OFFERED beside the question, never applied.
    """
    edition = project_standards(a)["ventilation_standard"]
    system = a.choice("system", SYSTEMS, "the kind of system serving the zones - one zone, "
                      "100 % outdoor air (a DOAS), or several zones on one recirculating unit")
    zones = a.records("zones", "each zone: name, occupancy, area_m2, people, "
                      "rp_ls_per_person, ra_ls_per_m2, ez - and primary_flow_ls on a "
                      "multiple-zone system")
    population = None
    method = None
    if system == "multiple-zone":
        population = a.number("system_population", "people", "the most people in all the "
                              "zones at once (Ps), for occupant diversity - D is taken as "
                              "1 if not given", 0, 1e6, required=False)
        method = a.choice("ev_method", ("simplified", "appendix-a"),
                          "how to find the system ventilation efficiency Ev - 62.1's "
                          "simplified equation from diversity, or its Appendix A from "
                          "each zone's primary airflow")
    system_vps = None
    if system == "multiple-zone" and method == "appendix-a":
        system_vps = a.flow("the system's highest expected primary airflow at the design "
                            "condition (Vps) - the zones' sum if not given, which gives "
                            "the larger outdoor air", prefix="system_primary_flow",
                            required=False)
    if system == "single-zone" and zones is not None and len(zones) != 1:
        a.refuse("a single-zone system serves ONE zone and %d were given - use "
                 "multiple-zone or 100-percent-outdoor-air" % len(zones))
    rows = []
    for z in zones or []:
        occupancy = z.text("occupancy")
        name = z.text("name") or occupancy or z.prefix
        az = z.number("area_m2", "m2", "the zone's NET occupiable floor area - shafts "
                      "and column enclosures out, furniture in", 0, 1e6, positive=True)
        pz = z.number("people", "people", "the zone's design population - the peak "
                      "expected during typical use", 0, 1e5,
                      reference=_for_project(_default_people(occupancy, az), edition,
                                             HELD_62_1, "ASHRAE 62.1"))
        rp = z.number("rp_ls_per_person", "L/s per person", "outdoor air per person for "
                      "this occupancy", 0, 50,
                      reference=_for_project(
                          offer("ventilation_rates", occupancy, "Rp L/s per person",
                                "L/s per person") or offer_table("ventilation_rates"),
                          edition, HELD_62_1, "ASHRAE 62.1"))
        ra = z.number("ra_ls_per_m2", "L/s per m2", "outdoor air per floor area for this "
                      "occupancy", 0, 10,
                      reference=_for_project(
                          offer("ventilation_rates", occupancy, "Ra L/s per m2",
                                "L/s per m2") or offer_table("ventilation_rates"),
                          edition, HELD_62_1, "ASHRAE 62.1"))
        ez = z.number("ez", "factor", "zone air distribution effectiveness for how air "
                      "is supplied and returned", 0.5, 1.5,
                      reference=_for_project(offer_table("zone_air_distribution"), edition,
                                             HELD_62_1, "ASHRAE 62.1"))
        vpz = None
        if system == "multiple-zone" and method == "appendix-a":
            vpz = z.flow("the zone's LOWEST primary (supply) airflow at the design "
                         "condition - a VAV box's minimum", prefix="primary_flow")
        elif system == "multiple-zone" and method == "simplified":
            vpz = z.flow("the zone's minimum primary airflow - checked against the "
                         "1.5 x Voz the simplified procedure requires", prefix="primary_flow",
                         required=False)
        rows.append([name, az, pz, rp, ra, ez, vpz])
    if a.incomplete():
        return
    out = []
    for name, az, pz, rp, ra, ez, vpz in rows:
        vbz = rp * pz + ra * az
        voz = vbz / ez
        out.append({"name": name, "az": az, "pz": pz, "rp": rp, "ra": ra, "ez": ez,
                    "vbz": vbz, "voz": voz, "vpz": vpz})
    a.uses("Vbz = Rp.Pz + Ra.Az (breathing zone);  Voz = Vbz / Ez (zone)")
    a.cite(SRC_6221)
    _check_62_1(a, edition, "the procedure and the rates Heron offers are")
    sum_pz = sum(z["pz"] for z in out)
    if system == "single-zone":
        vot = out[0]["voz"]
        a.uses("single-zone system: Vot = Voz")
    elif system == "100-percent-outdoor-air":
        vot = sum(z["voz"] for z in out)
        a.uses("100 % outdoor-air system: Vot = sum of Voz")
    else:
        if population is not None and population > sum_pz + 1e-9:
            a.refuse("system_population %s is more than the zones' own total %s - "
                     "diversity cannot add people" % (_g(population), _g(sum_pz)))
            return
        d = 1.0 if population is None or sum_pz == 0 else population / sum_pz
        if population is None:
            a.assume("no system_population given, so no occupant diversity (D = 1)")
        vou = d * sum(z["rp"] * z["pz"] for z in out) + sum(z["ra"] * z["az"] for z in out)
        a.uses("Vou = D.sum(Rp.Pz) + sum(Ra.Az), with D = Ps / sum(Pz)")
        if method == "simplified":
            ev = 0.88 * d + 0.22 if d < 0.60 else 0.75
            a.uses("Ev = 0.88 D + 0.22 when D < 0.60, else 0.75 - the simplified procedure "
                   "(62.1-2019 and -2022, Eqs. 6-7 and 6-8)")
            for z in out:
                if z["vpz"] is None:
                    a.check("WARN", "%s: no primary_flow_ls, so the simplified procedure's "
                                    "own condition - minimum primary airflow at least "
                                    "1.5 x Voz = %s L/s (Eq. 6-9) - was not checked"
                            % (z["name"], _f(1.5 * z["voz"], 1)))
                elif z["vpz"] < 1.5 * z["voz"] - 1e-9:
                    a.check("FAIL", "%s: minimum primary airflow %s L/s is below 1.5 x Voz "
                                    "= %s L/s, which the simplified procedure requires "
                                    "(Eq. 6-9) - raise the minimum or use Appendix A"
                            % (z["name"], _f(z["vpz"], 1), _f(1.5 * z["voz"], 1)))
                else:
                    a.check("OK", "%s: minimum primary airflow %s L/s meets 1.5 x Voz = %s L/s"
                            % (z["name"], _f(z["vpz"], 1), _f(1.5 * z["voz"], 1)))
        else:
            vps = system_vps if system_vps is not None else sum(z["vpz"] for z in out)
            if system_vps is None:
                a.assume("no system_primary_flow_ls given, so Vps is the zones' sum - "
                         "the most air, and so the most outdoor air")
            xs = vou / vps
            for z in out:
                z["zpz"] = z["voz"] / z["vpz"]
                z["evz"] = 1.0 + xs - z["zpz"]
            ev = min(z["evz"] for z in out)
            critical = min(out, key=lambda z: z["evz"])
            a.uses("Appendix A, single supply: Xs = Vou / Vps, Zpz = Voz / Vpz, "
                   "Evz = 1 + Xs - Zpz, Ev = the smallest Evz")
            a.result("Average outdoor air fraction Xs", _f(xs, 3))
            a.result("Critical zone", "%s - Zpz %s, Evz %s"
                     % (critical["name"], _f(critical["zpz"], 3), _f(critical["evz"], 3)))
            a.assume("Appendix A is applied for a single supply path - no secondary "
                     "recirculation and no transfer air between zones")
        if ev <= 0:
            a.check("FAIL", "the system ventilation efficiency is %s - the critical zone "
                            "gets too little supply air for its outdoor air need"
                    % _f(ev, 3))
            return
        vot = vou / ev
        a.result("Occupant diversity D", _f(d, 3))
        a.result("Uncorrected outdoor air Vou", flow_text(vou))
        a.result("System ventilation efficiency Ev", _f(ev, 3))
        a.uses("Vot = Vou / Ev")
    columns = ["zone", "Az m2", "Pz", "Rp", "Ra", "Vbz L/s", "Ez", "Voz L/s"]
    if system == "multiple-zone" and method == "appendix-a":
        columns += ["Vpz L/s", "Zpz", "Evz"]
    table = []
    for z in out:
        row = [z["name"], _f(z["az"], 1), _f(z["pz"], 1), _f(z["rp"], 2), _f(z["ra"], 2),
               _f(z["vbz"], 1), _f(z["ez"], 2), _f(z["voz"], 1)]
        if "zpz" in z:
            row += [_f(z["vpz"], 1), _f(z["zpz"], 3), _f(z["evz"], 3)]
        table.append(row)
    a.table("Zones", columns, table)
    a.result("Outdoor air intake Vot", flow_text(vot))
    total_area = sum(z["az"] for z in out)
    if sum_pz > 0:
        a.result("Per person", "%s L/s" % _f(vot / sum_pz, 2))
    a.result("Per floor area", "%s L/s per m2" % _f(vot / total_area, 3))
    a.into_revit("WRITE_ELEMENT_PARAMETERS can set each Space's outdoor-air figures; "
                 "REPORT_SPACE_AIRFLOW reads design against actual")


@calculation("exhaust", "Exhaust airflow and pressure balance", "ventilation")
def calc_exhaust(a):
    """Exhaust airflow summed from fixture counts or floor areas at the modeller's rates - toilets, kitchens, stores - and how it balances against the air supplied to the same space."""
    edition = project_standards(a)["ventilation_standard"]
    rates = _for_project(offer_table("exhaust_rates"), edition, HELD_62_1, "ASHRAE 62.1")
    items = a.records("items", "each: name, and count with rate_ls_each, or area_m2 with "
                      "rate_ls_per_m2")
    supply = a.flow("air supplied to the same space - for the balance", prefix="supply_flow",
                    required=False)
    rows = []
    for item in items or []:
        name = item.text("name") or item.prefix
        if item.has("count") or item.has("rate_ls_each"):
            n = item.number("count", "fixtures", "how many", 0, 1e5)
            r = item.number("rate_ls_each", "L/s each", "exhaust per fixture", 0, 5000,
                            reference=rates)
            if None not in (n, r):
                rows.append([name, n * r, "%s x %s L/s" % (_g(n), _g(r))])
        else:
            ar = item.number("area_m2", "m2", "floor area exhausted (or count)", 0, 1e6)
            r = item.number("rate_ls_per_m2", "L/s per m2", "exhaust per floor area", 0, 100,
                            reference=rates)
            if None not in (ar, r):
                rows.append([name, ar * r, "%s m2 x %s L/s.m2" % (_g(ar), _g(r))])
    if a.incomplete():
        return
    total = sum(r[1] for r in rows)
    a.table("Exhaust", ("item", "L/s", "how"), [[r[0], _f(r[1], 1), r[2]] for r in rows])
    a.result("Total exhaust", flow_text(total))
    if supply is not None:
        net = supply - total
        if net < 0:
            a.result("Balance", "%s L/s more exhaust than supply - the space runs at "
                     "negative pressure, and that air transfers in from its neighbours"
                     % _f(-net, 1))
        else:
            a.result("Balance", "%s L/s more supply than exhaust - positive pressure"
                     % _f(net, 1))
    a.cite(SRC_6221)
    _check_62_1(a, edition, "the exhaust rates Heron offers are")


# --- ducts -------------------------------------------------------------------

def _duct_shape(view, why="the duct's size"):
    """('round', d, None, None), ('rectangular', None, w, h) or ('oval', None, major, minor)."""
    view = _view(view)
    has_d = view.has("diameter_mm")
    has_r = view.has("width_mm") or view.has("height_mm")
    has_o = view.has("major_mm") or view.has("minor_mm")
    if has_d + has_r + has_o > 1:
        view.answer.refuse("%s: give diameter_mm, OR width_mm and height_mm, OR major_mm "
                           "and minor_mm - one shape" % (view.prefix or "the duct"))
        return None, None, None, None
    if has_d:
        return "round", view.number("diameter_mm", "mm", why, 25, 5000), None, None
    if has_o:
        major = view.number("major_mm", "mm", "flat oval major axis", 50, 5000)
        minor = view.number("minor_mm", "mm", "flat oval minor axis", 25, 5000)
        if None not in (major, minor) and minor > major:
            view.answer.refuse("%s: the minor axis is longer than the major" % (view.prefix or "oval"))
        return "oval", None, major, minor
    if not has_r:
        view.answer.need(view.name("diameter_mm"),
                         "mm (or width_mm and height_mm, or major_mm and minor_mm)", why)
        return None, None, None, None
    return ("rectangular", None, view.number("width_mm", "mm", "duct width", 25, 5000),
            view.number("height_mm", "mm", "duct height", 25, 5000))


@calculation("duct_friction", "Duct friction loss", "duct")
def calc_duct_friction(a):
    """The velocity, velocity pressure and friction loss of an airflow in a duct of a given size - round, rectangular or flat oval."""
    q = a.flow("airflow in the duct")
    shape, d, w, h = _duct_shape(a)
    air = read_air(a, "temperature of the air in the duct, with the site altitude")
    rough, rough_note = read_roughness(a, "the duct's inside surface roughness")
    length = a.number("length_m", "m", "duct length - for the total friction loss", 0, 10000,
                      required=False, positive=True)
    if a.incomplete():
        return
    got = duct_friction_at(q, shape, air, rough, d, w, h)
    a.result("Size", size_text(shape, d, w, h))
    a.result("Velocity", velocity_text(got["velocity"]))
    a.result("Velocity pressure", pressure_text(got["pv"]))
    if shape != "round":
        a.result("Equivalent diameter", "%s mm" % _f(got["de"], 1))
    if shape == "rectangular":
        a.result("Aspect ratio", "%s : 1" % _f(max(w, h) / min(w, h), 2))
    a.result("Friction", friction_text(got["pa_m"]))
    if length is not None:
        a.result("Friction over %s m" % _g(length), pressure_text(got["pa_m"] * length))
    a.result("Reynolds number", _f(got["re"], 0))
    a.result("Friction factor", _f(got["f"], 5))
    a.result("Air", air.label)
    a.result("Roughness", rough_note)
    a.uses("dp/L = (f / De) . rho V^2 / 2; f from Colebrook; a rectangular or oval "
           "duct as its equivalent round at the same flow")
    a.cite(SRC_DUCT)


def _allowed_sides(view, step, sizes, low, high):
    if sizes is not None:
        return sorted(s for s in sizes if low <= s <= high)
    out, s = [], step * math.ceil(low / step)
    while s <= high + 1e-9:
        out.append(s)
        s += step
    return out


def _passes(got, max_f, max_v):
    return ((max_f is None or got["pa_m"] <= max_f + 1e-12)
            and (max_v is None or got["velocity"] <= max_v + 1e-12))


def _size_one(q, spec, air, rough, max_f, max_v):
    """The smallest allowed size meeting every limit given, or (None, why)."""
    if spec["shape"] == "round":
        for d in spec["sizes"]:
            got = duct_friction_at(q, "round", air, rough, d=d)
            if _passes(got, max_f, max_v):
                return dict(got, d=d, w=None, h=None), None
        return None, ("needs more than %s mm round, the largest size given"
                      % _f(spec["sizes"][-1], 0))
    sides = spec["sides"]
    if spec.get("fixed_height") is not None or spec.get("fixed_width") is not None:
        fixed = spec.get("fixed_height") or spec.get("fixed_width")
        for other in sides:
            w, h = (other, fixed) if spec.get("fixed_height") is not None else (fixed, other)
            got = duct_friction_at(q, "rectangular", air, rough, w=w, h=h)
            if _passes(got, max_f, max_v):
                return dict(got, d=None, w=w, h=h), None
        return None, ("needs more than %s mm across, the largest allowed side, at the "
                      "fixed %s mm" % (_f(sides[-1], 0), _f(fixed, 0)))
    ratio = spec["aspect"]
    for h in sides:
        wide = [s for s in sides if s >= ratio * h - 1e-9]
        if not wide:
            break
        w = wide[0]
        got = duct_friction_at(q, "rectangular", air, rough, w=w, h=h)
        if _passes(got, max_f, max_v):
            return dict(got, d=None, w=w, h=h), None
    return None, "no allowed size at aspect ratio %s meets the limits" % _f(ratio, 2)


@calculation("duct_size", "Duct sizing - equal friction, velocity, or both", "duct")
def calc_duct_size(a):
    """Sizes a duct - or a list of ducts - to a friction rate (the equal-friction method), a velocity limit, or both, snapping UP to the sizes the project actually uses, round or rectangular.

    Rounding UP is deliberate: the next size down puts the friction and the velocity above the figure given, the one direction that brings noise and rework.
    """
    items = a.records("items", "several ducts at once: id and flow_ls for each, e.g. as "
                      "read from Revit", required=False)
    q = None if items else a.flow("airflow in the duct (or items for several)")
    max_f = a.number("max_friction_pa_m", "Pa/m", "friction rate to size at - the "
                     "equal-friction method", 0.05, 20, required=False,
                     reference=offer_table("duct_design_guidance"))
    max_v = a.number("max_velocity_ms", "m/s", "velocity limit - the velocity method, or a "
                     "cap on equal friction", 0.5, 40, required=False,
                     reference=offer_table("duct_design_guidance"))
    if max_f is None and max_v is None and not a.has("max_friction_pa_m") \
            and not a.has("max_velocity_ms"):
        a.need("max_friction_pa_m", "Pa/m (and/or max_velocity_ms)",
               "what to size to - a friction rate, a velocity limit, or both",
               offer_table("duct_design_guidance"))
    shape = a.choice("shape", ("round", "rectangular"), "the duct's shape")
    spec = {"shape": shape}
    if shape == "round":
        spec["sizes"] = a.numbers("round_sizes_mm", "mm", "the round sizes the project "
                                  "uses - the duct type's own size table, or Revit snaps "
                                  "to its nearest", 25, 5000,
                                  reference=offer_table("round_duct_sizes"))
    elif shape == "rectangular":
        has_step, has_list = a.has("rect_step_mm"), a.has("rect_sizes_mm")
        if has_step == has_list:
            if has_step:
                a.refuse("give rect_step_mm or rect_sizes_mm, not both")
            else:
                a.need("rect_step_mm", "mm (or rect_sizes_mm)", "the increment the "
                       "project's rectangular sizes come in, or the list of them")
        step = a.number("rect_step_mm", "mm", "size increment", 5, 500) if has_step else None
        sizes = a.numbers("rect_sizes_mm", "mm", "allowed sides", 25, 5000) if has_list else None
        low = a.number("min_side_mm", "mm", "smallest side allowed - the step if not given",
                       25, 2000, required=False) or (step or 25)
        high = a.number("max_side_mm", "mm", "largest side allowed - 3000 mm searched if "
                        "not given", 100, 5000, required=False) or 3000.0
        modes = [m for m in ("fixed_height_mm", "fixed_width_mm", "aspect_ratio") if a.has(m)]
        if len(modes) != 1:
            if modes:
                a.refuse("give ONE of fixed_height_mm, fixed_width_mm or aspect_ratio - "
                         "got %s" % ", ".join(modes))
            else:
                a.need("fixed_height_mm", "mm (or fixed_width_mm, or aspect_ratio)",
                       "what holds the rectangle's shape - a depth the ceiling void "
                       "allows, a fixed width, or a ratio")
        else:
            mode = modes[0]
            if mode == "aspect_ratio":
                spec["aspect"] = a.number("aspect_ratio", "width:height", "width over "
                                          "height wanted", 1, 8)
            elif mode == "fixed_height_mm":
                spec["fixed_height"] = a.number("fixed_height_mm", "mm", "duct depth held",
                                                25, 3000)
            else:
                spec["fixed_width"] = a.number("fixed_width_mm", "mm", "duct width held",
                                               25, 5000)
        max_ar = a.number("max_aspect_ratio", "width:height", "a limit to flag sizes "
                          "flatter than", 1, 10, required=False)
        if not a.incomplete():
            spec["sides"] = _allowed_sides(a, step, sizes, low, high)
            if not spec["sides"]:
                a.refuse("no allowed side between %s and %s mm" % (_g(low), _g(high)))
    air = read_air(a, "temperature of the air in the duct, with the site altitude")
    rough, rough_note = read_roughness(a, "the duct's inside surface roughness")
    flows = []
    for item in items or []:
        fl = item.flow("airflow in this duct")
        flows.append((item.text("id") or item.prefix, fl))
    if a.incomplete():
        return
    if q is not None:
        flows = [("the duct", q)]
    if shape == "round":
        spec["sizes"] = sorted(spec["sizes"])
    rows, groups, refused = [], collections.OrderedDict(), []
    for ident, fl in flows:
        got, why = _size_one(fl, spec, air, rough, max_f, max_v)
        if got is None:
            refused.append("%s (%s L/s): %s" % (ident, _f(fl, 1), why))
            continue
        label = size_text(shape, got["d"], got["w"], got["h"])
        note = ""
        if shape == "rectangular":
            ar = max(got["w"], got["h"]) / min(got["w"], got["h"])
            note = "AR %s:1" % _f(ar, 1)
            if max_ar is not None and ar > max_ar + 1e-9:
                note += " - flatter than %s:1" % _f(max_ar, 1)
                a.check("WARN", "%s at %s is flatter than the %s:1 given"
                        % (ident, label, _f(max_ar, 1)))
        rows.append([ident, _f(fl, 1), label, _f(got["velocity"], 2), _f(got["pa_m"], 3), note])
        key = ((("diameter", got["d"]),) if shape == "round"
               else (("width", got["w"]), ("height", got["h"])))
        groups.setdefault(key, []).append(ident)
        if len(flows) == 1:
            a.result("Size", label)
            a.result("Velocity", velocity_text(got["velocity"]))
            a.result("Friction", friction_text(got["pa_m"]))
            a.result("Velocity pressure", pressure_text(got["pv"]))
            if shape != "round":
                a.result("Equivalent diameter", "%s mm" % _f(got["de"], 1))
            if max_f is not None:
                exact = _bisect_up(lambda dd: duct_friction_at(fl, "round", air, rough,
                                                               d=dd)["pa_m"] <= max_f, 10.0, 6000.0)
                if exact is not None:
                    a.result("Exact round for %s Pa/m" % _g(max_f), "%s mm, before snapping up"
                             % _f(exact, 1))
    if len(flows) > 1:
        a.table("Sizes", ("id", "L/s", "size", "m/s", "Pa/m", "note"), rows)
    for line in refused:
        a.check("FAIL", line)
    for key, ids in groups.items():
        a.into_revit("SET_MEP_SIZE %s for %d duct(s): %s"
                     % (", ".join("%s=%s" % (k, _f(v, 0)) for k, v in key), len(ids),
                        ", ".join(ids)))
    a.result("Air", air.label)
    a.result("Roughness", rough_note)
    limits = []
    if max_f is not None:
        limits.append("friction at most %s Pa/m" % _g(max_f))
    if max_v is not None:
        limits.append("velocity at most %s m/s" % _g(max_v))
    a.uses("smallest allowed size with %s; never rounded down" % " and ".join(limits),
           "friction by Darcy-Weisbach with Colebrook, a rectangle as its Huebscher "
           "equivalent round at the same flow")
    a.cite(SRC_DUCT)
    a.into_revit("MEASURE_MEP_VELOCITY reads the velocity back after sizing; AUTO_SIZE_MEP "
                 "is the in-model velocity method")


@calculation("equivalent_diameter", "Equivalent round diameter", "duct")
def calc_equivalent_diameter(a):
    """The round duct with the same friction at the same flow as a rectangular or flat-oval one - or, from a round size and one side, the other side of the rectangle that matches it."""
    max_ar = a.number("max_aspect_ratio", "width:height", "a limit to flag a rectangle "
                      "flatter than", 1, 10, required=False)
    if a.has("diameter_mm"):
        d = a.number("diameter_mm", "mm", "the round size to match", 25, 5000)
        has_h, has_w = a.has("height_mm"), a.has("width_mm")
        if has_h == has_w:
            if has_h:
                a.refuse("give one side - height_mm or width_mm - and Heron finds the other")
            else:
                a.need("height_mm", "mm (or width_mm)", "the side of the rectangle that is "
                       "fixed, usually the depth the ceiling allows")
        side = a.number("height_mm" if has_h else "width_mm", "mm", "the fixed side",
                        25, 5000) if (has_h or has_w) else None
        if a.incomplete():
            return
        other = _bisect_up(lambda x: huebscher(x, side) >= d, 1.0, 20000.0)
        if other is None:
            a.refuse("no rectangle with a %s mm side matches %s mm round" % (_g(side), _g(d)))
            return
        a.result("Matching side", "%s mm - so %s x %s mm matches %s mm round"
                 % (_f(other, 1), _f(other, 0) if has_h else _f(side, 0),
                    _f(side, 0) if has_h else _f(other, 0), _f(d, 0)))
        ratio = max(other, side) / min(other, side)
        a.result("Aspect ratio", "%s : 1" % _f(ratio, 2))
        if max_ar is not None and ratio > max_ar + 1e-9:
            a.check("WARN", "flatter than the %s:1 given - more metal and more friction "
                            "for the same air" % _g(max_ar))
    else:
        shape, _d, w, h = _duct_shape(a, "the rectangular or flat-oval size")
        if a.incomplete():
            return
        if shape == "round":
            a.refuse("a round duct is its own equivalent")
            return
        if shape == "oval":
            de = flat_oval_equivalent(w, h)
            area, per = flat_oval_area_perimeter(w, h)
            a.uses("De = 1.55 A^0.625 / P^0.25 (Heyt & Diaz, flat oval)")
        else:
            de = huebscher(w, h)
            area, per = w * h, 2.0 * (w + h)
            a.uses("De = 1.30 (ab)^0.625 / (a + b)^0.25 (Huebscher, rectangular)")
            a.result("Aspect ratio", "%s : 1" % _f(max(w, h) / min(w, h), 2))
        a.result("Equivalent diameter", "%s mm - same friction at the same flow" % _f(de, 1))
        a.result("Hydraulic diameter", "%s mm (4A/P)" % _f(4.0 * area / per, 1))
        a.result("Area", "%s m2, against %s m2 for the round equivalent"
                 % (_f(area / 1e6, 4), _f(math.pi * de * de / 4e6, 4)))
    a.cite(SRC_DUCT)


@calculation("duct_pressure", "Duct system pressure along the index run", "duct")
def calc_duct_pressure(a):
    """The total pressure a fan must make for its index run - friction in each section, each fitting as its loss coefficient times the velocity pressure, and the drop through every component in the run.

    Loss coefficients are the modeller's, from the ASHRAE Duct Fitting Database or the manufacturer; Heron holds none, because a coefficient depends on the exact fitting geometry and flow split.
    """
    sections = a.records("sections", "the index run, section by section: name, flow_ls, "
                         "length_m, diameter_mm or width_mm and height_mm, "
                         "loss_coefficients")
    components = a.records("components", "coils, filters, dampers, terminals in the run: "
                           "name and pressure_pa", required=False)
    air = read_air(a, "temperature of the air in the run, with the site altitude")
    rough, rough_note = read_roughness(a, "the ducts' inside surface roughness")
    parts = []
    for s in sections or []:
        fl = s.flow("airflow in this section")
        length = s.number("length_m", "m", "section length", 0, 10000)
        shape, d, w, h = _duct_shape(s)
        cs = s.numbers("loss_coefficients", "C", "loss coefficient of each fitting in this "
                       "section, on its velocity pressure", 0, 100, required=False,
                       positive=False)
        parts.append((s.text("name") or s.prefix, fl, length, shape, d, w, h, cs))
    fixed = []
    for c in components or []:
        dp = c.number("pressure_pa", "Pa", "pressure drop through it at this airflow", 0, 5000)
        fixed.append((c.text("name") or c.prefix, dp))
    if a.incomplete():
        return
    rows, total = [], 0.0
    for name, fl, length, shape, d, w, h, cs in parts:
        got = duct_friction_at(fl, shape, air, rough, d, w, h)
        friction = got["pa_m"] * length
        fittings = sum(cs or []) * got["pv"]
        total += friction + fittings
        rows.append([name, _f(fl, 1), size_text(shape, d, w, h), _f(got["velocity"], 2),
                     _f(got["pv"], 1), _f(got["pa_m"], 3), _f(friction, 1),
                     _f(sum(cs or []), 2), _f(fittings, 1), _f(friction + fittings, 1)])
        if not cs:
            a.assume("%s: no loss coefficients given, so no fitting losses in it" % name)
    a.table("Index run", ("section", "L/s", "size", "m/s", "Pv Pa", "Pa/m", "friction Pa",
                          "sum C", "fittings Pa", "total Pa"), rows)
    comp = sum(dp for _n, dp in fixed)
    if fixed:
        a.table("Components", ("component", "Pa"), [[n, _f(dp, 1)] for n, dp in fixed])
    a.result("Ductwork", pressure_text(total))
    a.result("Components", pressure_text(comp))
    a.result("Index run total pressure", pressure_text(total + comp))
    above = [r for r in REFERENCES["smacna_pressure_classes"]["rows"]
             if r[1] >= total + comp - 1e-9]
    if above:
        a.result("SMACNA class at or above it", "%s - offered: a class is set by the STATIC "
                 "pressure each part of the run sees, which the engineer confirms" % above[0][0])
    a.result("Air", air.label)
    a.result("Roughness", rough_note)
    a.uses("each section: friction (dp/L x L) + sum(C) x velocity pressure; the run's "
           "total is what the fan must overcome, before any system effect at its inlet "
           "and outlet")
    a.cite(SRC_DUCT)
    a.into_revit("REPORT_MEP_PRESSURE_DROP reads Revit's own pressure drop on the same "
                 "run, to compare")
    a.check("OK", "next: fan_power with this pressure")


@calculation("fan_power", "Fan power and specific fan power", "fan")
def calc_fan_power(a):
    """The power a fan draws for an airflow and a total pressure through its fan, motor and drive efficiencies, and the specific fan power that results."""
    energy = project_standards(a)["energy_standard"]
    q = a.flow("fan airflow")
    dp = a.number("total_pressure_pa", "Pa", "fan total pressure", 1, 10000,
                  reference="duct_pressure sums it along the index run")
    nf = a.number("fan_efficiency_pct", "%", "fan total efficiency at the duty point, "
                  "from the fan selection", 5, 95)
    nm = a.number("motor_efficiency_pct", "%", "motor efficiency at that load", 30, 99)
    nd = a.number("drive_efficiency_pct", "%", "drive efficiency - 100 for a direct drive",
                  50, 100)
    limit = a.number("sfp_limit_w_per_ls", "W per L/s", "a specific fan power limit to "
                     "check against", 0.05, 10, required=False,
                     reference=offer_table("fan_power_limits"))
    if a.incomplete():
        return
    air_w = q / 1000.0 * dp
    shaft = air_w / (nf / 100.0)
    electric = shaft / (nm / 100.0 * nd / 100.0)
    sfp = electric / q
    a.result("Air power", "%s W" % _f(air_w, 0))
    a.result("Shaft power", "%s W  (%s kW, %s hp)" % (_f(shaft, 0), _f(shaft / 1000.0, 2),
                                                     _f(shaft / 745.69987158227022, 2)))
    a.result("Electrical input", "%s W  (%s kW)" % (_f(electric, 0), _f(electric / 1000.0, 2)))
    a.result("Specific fan power", "%s W per L/s  (%s kW per m3/s, %s W per cfm)"
             % (_f(sfp, 3), _f(sfp, 3), _f(sfp * LS_PER_CFM, 3)))
    if limit is not None:
        a.check("OK" if sfp <= limit else "FAIL",
                "specific fan power %s W per L/s against the %s given"
                % (_f(sfp, 3), _g(limit)))
    elif _applies_90_1(energy) is None:
        a.check("WARN", "not checked against an energy code: whether one applies to this "
                        "project is not known yet")
    elif _applies_90_1(energy):
        a.check("WARN", "ASHRAE %s applies to this project and limits the fan power of a "
                        "system above 5 hp - `reference fan_power_limits` holds %s's "
                        "limits; give sfp_limit_w_per_ls to check against one%s"
                % (energy, HELD_90_1, _edition_said(energy, HELD_90_1)))
    elif energy == OTHER_STANDARD:
        a.check("WARN", "this project's energy code is not ASHRAE 90.1 - give its fan power "
                        "limit as sfp_limit_w_per_ls to check against it")
    a.uses("air power = Q x dp; shaft = air / fan efficiency; input = shaft / (motor x drive)")
    a.cite(SRC_FAN)


# --- air distribution --------------------------------------------------------

def _grid(n_min, length, width, smax, exact):
    """(rows, cols) - the fewest diffusers meeting the count and spacing, squarest modules first."""
    best = None
    for rows in range(1, 201):
        for cols in range(1, 201):
            n = rows * cols
            if exact and n != n_min:
                continue
            if n < n_min:
                continue
            sx, sy = length / cols, width / rows
            if smax is not None and (sx > smax + 1e-9 or sy > smax + 1e-9):
                continue
            score = (n, abs(sx - sy))
            if best is None or score < best[0]:
                best = (score, rows, cols)
            break
    return None if best is None else (best[1], best[2])


@calculation("diffuser_layout", "Diffuser count and layout", "diffuser")
def calc_diffuser_layout(a):
    """How many ceiling diffusers a room needs and where - a grid from the room's size, its airflow and a limit per diffuser or on spacing - with each one's flow, its characteristic length, the throw that suits it and its position."""
    length = a.number("room_length_m", "m", "room length, along its x axis", 0.5, 500,
                      positive=True)
    width = a.number("room_width_m", "m", "room width, along its y axis", 0.5, 500,
                     positive=True)
    q = a.flow("total supply airflow to the room")
    count = a.integer("count", "diffusers wanted", 1, 10000, required=False)
    qmax = a.flow("most one diffuser may carry - from the diffuser's selection data",
                  prefix="max_flow_per_diffuser", required=False)
    smax = a.number("max_spacing_m", "m", "largest centre-to-centre spacing allowed",
                    0.3, 50, required=False)
    if count is None and qmax is None and smax is None and not a.refused:
        a.need("max_flow_per_diffuser_ls", "L/s (or count, or max_spacing_m)",
               "what limits a diffuser - the most air one may carry (from its NC and "
               "throw data), a count, or a spacing")
    if count is not None and (qmax is not None or smax is not None):
        a.refuse("give count, OR max_flow_per_diffuser and/or max_spacing_m - a count "
                 "already decides the layout")
    dtype = a.word("diffuser_type", "the diffuser kind, for the ADPI throw ratio offered "
                   "from `reference adpi`", required=False)
    load_density = a.number("room_load_w_m2", "W/m2", "the room's cooling load per floor "
                            "area - picks the ADPI row", 1, 1000, required=False)
    ox = a.number("origin_x_mm", "mm", "x of the room corner the grid starts at, for "
                  "placement points", -1e9, 1e9, required=False)
    oy = a.number("origin_y_mm", "mm", "y of that corner", -1e9, 1e9, required=False)
    z = a.number("mounting_height_mm", "mm", "elevation of the diffusers - asked, never "
                 "defaulted, because a point with no height lands on the floor",
                 -1e6, 1e6, required=False)
    rot = a.number("rotation_deg", "degrees", "the room's x axis angle from project x - "
                   "0 if not given", -360, 360, required=False)
    if a.incomplete():
        return
    if count is not None:
        grid = _grid(count, length, width, None, True)
    else:
        n_min = int(math.ceil(q / qmax - 1e-9)) if qmax is not None else 1
        grid = _grid(n_min, length, width, smax, False)
    if grid is None:
        a.refuse("no grid of up to 200 x 200 meets that count and spacing")
        return
    rows, cols = grid
    n = rows * cols
    sx, sy = length / cols, width / rows
    each = q / n
    char = min(sx, sy) / 2.0
    a.result("Diffusers", "%d - %d along the length x %d across" % (n, cols, rows))
    a.result("Spacing", "%s m along x, %s m along y" % (_f(sx, 2), _f(sy, 2)))
    a.result("Flow per diffuser", flow_text(each))
    if qmax is not None and each > qmax + 1e-9:
        a.check("FAIL", "%s L/s each is above the %s L/s given" % (_f(each, 1), _f(qmax, 1)))
    a.result("Characteristic length L", "%s m - half the smaller module, to the wall or "
             "to where neighbouring jets meet" % _f(char, 2))
    a.result("Floor area per diffuser", "%s m2" % _f(sx * sy, 2))
    found = reference_rows("adpi", dtype) if dtype else []
    if dtype and not found:
        a.check("WARN", "diffuser_type %r is not in the ADPI table, so no throw ratio is "
                        "offered - %s" % (dtype, offer_table("adpi")))
    elif found:
        columns = REFERENCES["adpi"]["columns"]
        at = dict((c, columns.index(c)) for c in columns)
        found = sorted(found, key=lambda r: r[at["room load W/m2"]])
        if load_density is not None:
            higher = [r for r in found if r[at["room load W/m2"]] >= load_density]
            found = [higher[0] if higher else found[-1]]
        a.table("Throw to look for in the catalogue at %s L/s - OFFERED from the classic "
                "ADPI guide, L = %s m" % (_f(each, 1), _f(char, 2)),
                ("room load W/m2", "throw", "T/L best", "throw for best ADPI m",
                 "range m", "ADPI above"),
                [[r[at["room load W/m2"]], r[at["throw"]], _g(r[at["T/L for best ADPI"]]),
                  _f(r[at["T/L for best ADPI"]] * char, 2),
                  "%s to %s" % (_f(r[at["T/L range low"]] * char, 2),
                                _f(r[at["T/L range high"]] * char, 2)),
                  r[at["ADPI above"]]] for r in found])
        if load_density is not None and found[0][at["room load W/m2"]] < load_density:
            a.check("WARN", "%s W/m2 is above the highest load the ADPI table holds for a "
                            "%s - its row is a guide at best" % (_g(load_density), dtype))
        a.cite(REFERENCES["adpi"]["source"])
    points = []
    for j in range(rows):
        for i in range(cols):
            points.append(((i + 0.5) * sx * 1000.0, (j + 0.5) * sy * 1000.0))
    if None not in (ox, oy):
        r = math.radians(rot or 0.0)
        placed = [(ox + x * math.cos(r) - y * math.sin(r), oy + x * math.sin(r) + y * math.cos(r))
                  for x, y in points]
        a.table("Diffuser points (mm)", ("#", "x", "y", "z"),
                [[k + 1, "%.0f" % px, "%.0f" % py, "%.0f" % z if z is not None else "ASK"]
                 for k, (px, py) in enumerate(placed)])
        if z is None:
            a.check("WARN", "no mounting_height_mm - ask for it before placing, or every "
                            "diffuser lands at the level's own elevation")
        else:
            a.into_revit("PLACE_FAMILY_INSTANCES at these points, on the level they are "
                         "measured from")
    else:
        a.table("Diffuser points from the room corner (mm)", ("#", "x", "y"),
                [[k + 1, "%.0f" % px, "%.0f" % py] for k, (px, py) in enumerate(points)])
    a.uses("a grid of rows x cols with rows.cols >= the count needed, each diffuser "
           "centred in its module; the fewest diffusers first, then the squarest module")
    a.cite(SRC_DIFFUSION)
    a.into_revit("terminal_flows writes each terminal's share as the file "
                 "SET_AIR_TERMINAL_FLOW reads")


def _interpolate(points, x):
    """Linear interpolation over (x, y) points sorted by x; None outside them."""
    pts = sorted(points)
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        if x0 <= x <= x1:
            return y0 if x1 == x0 else y0 + (y1 - y0) * (x - x0) / (x1 - x0)
    if len(pts) == 1 and abs(pts[0][0] - x) < 1e-9:
        return pts[0][1]
    return None


# What puts a chosen terminal into the model: its TYPE carries the size.
TERMINAL_INTO_REVIT = ("CHANGE_ELEMENT_TYPE swaps a placed terminal to the type of the size "
                       "chosen, where the family carries that size; SET_AIR_TERMINAL_FLOW "
                       "sets its flow")


def _neck_limit(a):
    """
    (limit in m/s, the sentence saying whose it is) for a supply neck: the
    modeller's own figure when given; else the office's own for an NC/RC 30
    room (D-110); else a question, with ASHRAE's row for the room's criterion
    offered beside it. (None, None) when it is still a question.
    """
    nc = a.number("max_nc", "NC", "the room's noise criterion, NC or RC - for an NC/RC %d "
                  "room the office's own %s m/s neck limit applies (D-110)"
                  % (OFFICE_SUPPLY_NECK_NC, _g(OFFICE_SUPPLY_NECK_MS)), 10, 70,
                  required=False)
    if a.has("max_neck_velocity_ms"):
        given = a.number("max_neck_velocity_ms", "m/s", "the neck velocity not to exceed",
                         0.5, 15)
        return given, None if given is None else "%s m/s, as given" % _g(given)
    ashrae = reference_lookup("air_terminal_guidance", "RC/NC %d" % OFFICE_SUPPLY_NECK_NC)
    if nc is not None and abs(nc - OFFICE_SUPPLY_NECK_NC) < 1e-9:
        return OFFICE_SUPPLY_NECK_MS, (
            "%s m/s - the office's own figure for a supply neck at NC/RC %d (D-110), not "
            "the %s m/s in %s; give max_neck_velocity_ms to use another"
            % (_g(OFFICE_SUPPLY_NECK_MS), OFFICE_SUPPLY_NECK_NC,
               _g(ashrae[1]) if ashrae else "-",
               REFERENCES["air_terminal_guidance"]["source"]))
    row = None
    if nc is not None and abs(nc - round(nc)) < 1e-9:
        row = offer("air_terminal_guidance", "RC/NC %d" % int(round(nc)),
                    "supply outlet neck m/s", "m/s")
    a.need("max_neck_velocity_ms", "m/s (or max_nc)",
           "the neck velocity not to exceed - the manufacturer's guidance for the NC "
           "wanted. For an NC/RC %d room give max_nc instead: the office's own %s m/s "
           "applies (D-110)" % (OFFICE_SUPPLY_NECK_NC, _g(OFFICE_SUPPLY_NECK_MS)),
           row or offer_table("air_terminal_guidance"))
    return None, None


@calculation("diffuser_select", "Diffuser or grille selection", "diffuser")
def calc_diffuser_select(a):
    """Picks a diffuser size for a flow - from neck sizes and a neck-velocity limit, or from the manufacturer's own catalogue rows with an NC limit and a throw range.

    NC and throw belong to the product. Heron interpolates the catalogue it is given; without one it can size the neck and say nothing about noise.
    """
    q = a.flow("airflow through one diffuser")
    catalogue = a.records("catalogue", "the manufacturer's rows: size, flow_ls, nc, and "
                          "t50_m and total_pressure_pa where listed", required=False)
    if catalogue is None:
        sizes = a.numbers("neck_sizes_mm", "mm", "the neck sizes the product comes in",
                          25, 2000)
        neck = a.choice("neck_shape", ("round", "square"), "round or square necks")
        vmax, limit_said = _neck_limit(a)
        if a.incomplete():
            return
        for s in sorted(sizes):
            area = (math.pi * s * s / 4.0 if neck == "round" else s * s) / 1e6
            v = q / 1000.0 / area
            if v <= vmax + 1e-12:
                a.result("Neck size", "%s mm %s" % (_f(s, 0), neck))
                a.result("Neck velocity", velocity_text(v))
                a.result("Neck velocity limit", limit_said)
                a.result("Neck velocity pressure", pressure_text(1.2 * v * v / 2.0)
                         + " (at 1.2 kg/m3)")
                a.check("WARN", "NC and throw are the product's - check this size at "
                                "%s L/s in the manufacturer's table" % _f(q, 1))
                a.uses("smallest neck with velocity Q / A at or below the limit")
                a.cite(SRC_DIFFUSION)
                a.into_revit(TERMINAL_INTO_REVIT)
                return
        a.refuse("%s L/s needs more than the largest neck given (%s mm) at %s m/s"
                 % (_f(q, 1), _f(max(sizes), 0), _g(vmax)))
        return
    max_nc = a.number("max_nc", "NC", "the noise criterion the room may not exceed", 10, 70)
    t_lo = a.number("t50_min_m", "m", "shortest T50 throw acceptable", 0, 50, required=False)
    t_hi = a.number("t50_max_m", "m", "longest T50 throw acceptable", 0, 50, required=False)
    by_size = collections.OrderedDict()
    for row in catalogue:
        size = row.word("size", "the size label, as the catalogue prints it")
        fl = row.flow("catalogue airflow")
        nc = row.number("nc", "NC", "catalogue NC at that airflow", 0, 90)
        t50 = row.number("t50_m", "m", "catalogue T50 throw", 0, 100, required=False)
        tp = row.number("total_pressure_pa", "Pa", "catalogue total pressure", 0, 2000,
                        required=False)
        if None not in (size, fl, nc):
            by_size.setdefault(size, []).append((fl, nc, t50, tp))
    if a.incomplete():
        return
    choices = []
    for size, pts in by_size.items():
        nc = _interpolate([(p[0], p[1]) for p in pts], q)
        if nc is None:
            choices.append((size, None, None, None, "%s L/s is outside this size's "
                            "catalogue range" % _f(q, 1)))
            continue
        t50 = _interpolate([(p[0], p[2]) for p in pts if p[2] is not None], q)
        tp = _interpolate([(p[0], p[3]) for p in pts if p[3] is not None], q)
        ok = nc <= max_nc + 1e-9
        why = "NC %s" % _f(nc, 0)
        if t50 is not None and t_lo is not None and t50 < t_lo:
            ok, why = False, why + ", T50 %s m short" % _f(t50, 1)
        if t50 is not None and t_hi is not None and t50 > t_hi:
            ok, why = False, why + ", T50 %s m long" % _f(t50, 1)
        if (t_lo is not None or t_hi is not None) and t50 is None:
            ok, why = False, why + ", no T50 listed"
        choices.append((size, nc, t50, tp, why if not ok else "meets"))
    a.table("Catalogue at %s L/s" % _f(q, 1), ("size", "NC", "T50 m", "Pa", "verdict"),
            [[c[0], _f(c[1], 0) if c[1] is not None else "-",
              _f(c[2], 1) if c[2] is not None else "-",
              _f(c[3], 1) if c[3] is not None else "-", c[4]] for c in choices])
    winners = [c for c in choices if c[4] == "meets"]
    if not winners:
        a.check("FAIL", "no size in the catalogue meets NC %s%s at %s L/s"
                % (_g(max_nc), " and the throw range" if (t_lo or t_hi) else "", _f(q, 1)))
    else:
        first = winners[0]
        a.result("Selected", "%s - NC %s%s" % (first[0], _f(first[1], 0),
                 ", T50 %s m" % _f(first[2], 1) if first[2] is not None else ""))
        a.into_revit(TERMINAL_INTO_REVIT)
        a.uses("the first size, in the catalogue's own order, that meets the NC limit and "
               "the throw range - so list the sizes smallest first")
    a.uses("NC, T50 and pressure interpolated linearly between the catalogue's flow points "
           "for each size, never extrapolated")
    a.cite(SRC_DIFFUSION)


@calculation("throw", "Jet throw from an outlet", "diffuser")
def calc_throw(a):
    """How far a supply jet carries before slowing to 0.75, 0.5 and 0.25 m/s - the T150, T100 and T50 throws - from the outlet's throw constant, effective area and airflow."""
    k = a.number("throw_constant", "K", "the outlet's throw constant - from the "
                 "manufacturer or ASHRAE Fundamentals Ch. 20", 0.5, 12)
    area = a.number("effective_area_m2", "m2", "the outlet's effective (core x discharge) "
                    "area", 1e-4, 10, positive=True)
    q = a.flow("airflow through the outlet")
    if a.incomplete():
        return
    v0 = q / 1000.0 / area
    a.result("Outlet velocity", velocity_text(v0))
    for name, vx in (("T150", 0.75), ("T100", 0.5), ("T50", 0.25)):
        x = k * v0 * math.sqrt(area) / vx
        a.result("%s throw (to %s m/s)" % (name, _g(vx)), "%s m" % _f(x, 2))
        if x < 4.0 * math.sqrt(area):
            a.check("WARN", "%s falls inside the jet's core zone, where the centreline "
                            "equation does not hold" % name)
    a.uses("main-zone centreline velocity Vx / V0 = K sqrt(A0) / x, solved for x at each "
           "terminal velocity")
    a.cite(SRC_DIFFUSION)


@calculation("terminal_flows", "Split a room's airflow across its terminals", "diffuser")
def calc_terminal_flows(a):
    """Splits a room's airflow across its air terminals - equally, or by weights - so the shares add up exactly, and writes them as the id-and-flow file SET_AIR_TERMINAL_FLOW reads."""
    q = a.flow("the room's total airflow")
    ids = a.words("terminal_ids", "the air terminals' element ids - or count",
                  required=False)
    count = None if ids else a.integer("count", "how many terminals (or terminal_ids)",
                                       1, 10000)
    weights = a.numbers("weights", "relative share", "a share for each terminal - equal "
                        "if not given", 0, 1e6, required=False)
    if a.incomplete():
        return
    n = len(ids) if ids else count
    if weights is not None and len(weights) != n:
        a.refuse("%d weights for %d terminals" % (len(weights), n))
        return
    shares = [1.0] * n if weights is None else weights
    total = sum(shares)
    if total <= 0:
        a.refuse("the weights add up to nothing")
        return
    tenths = int(round(q * 10))
    raw = [q * 10 * s / total for s in shares]
    floors = [int(math.floor(r)) for r in raw]
    order = sorted(range(n), key=lambda i: raw[i] - floors[i], reverse=True)
    for i in order[:tenths - sum(floors)]:
        floors[i] += 1
    flows = [f / 10.0 for f in floors]
    labels = ids or ["#%d" % (i + 1) for i in range(n)]
    a.table("Terminal flows", ("terminal", "L/s"), [[l, _f(f, 1)] for l, f in zip(labels, flows)])
    a.result("Total", "%s L/s over %d terminals - the shares add up exactly to 0.1 L/s"
             % (_f(sum(flows), 1), n))
    if ids:
        a.csv = "element_id,flow_ls\n" + "".join("%s,%s\n" % (i, _f(f, 1))
                                                 for i, f in zip(ids, flows))
        a.into_revit("SET_AIR_TERMINAL_FLOW with the CSV below saved as a file - its "
                     "first line is a header")
    a.uses("largest-remainder rounding to 0.1 L/s, so no tenth is lost or invented")


@calculation("grille_velocity", "Grille face velocity", "diffuser")
def calc_grille_velocity(a):
    """The face and free-area velocity through a return, exhaust or transfer grille, and whether it is under the limit given."""
    q = a.flow("airflow through the grille")
    has_area = a.has("face_area_m2")
    area = (a.number("face_area_m2", "m2", "the grille's face area", 1e-4, 50, positive=True)
            if has_area else None)
    if not has_area:
        w = a.number("width_mm", "mm", "grille face width (or face_area_m2)", 25, 10000)
        h = a.number("height_mm", "mm", "grille face height", 25, 10000)
        area = None if None in (w, h) else w * h / 1e6
    free = a.number("free_area_pct", "%", "the grille's free area - from the "
                    "manufacturer", 5, 100, required=False)
    vmax = a.number("max_face_velocity_ms", "m/s", "the face velocity not to exceed",
                    0.2, 20, required=False, reference=offer_table("air_terminal_guidance"))
    if a.incomplete():
        return
    v = q / 1000.0 / area
    a.result("Face velocity", velocity_text(v))
    if free is not None:
        a.result("Free-area velocity", velocity_text(v / (free / 100.0)))
    if vmax is not None:
        a.check("OK" if v <= vmax + 1e-12 else "FAIL",
                "face velocity %s m/s against the %s m/s given" % (_f(v, 2), _g(vmax)))
    a.uses("face velocity = Q / face area; free-area velocity = face velocity / free share")


# --- water -------------------------------------------------------------------

@calculation("chw_flow", "Chilled-water flow for a coil load", "water")
def calc_chw_flow(a):
    """The chilled-water flow a coil load needs between the supply and return temperatures given - in L/s, m3/h and US gpm."""
    energy = project_standards(a)["energy_standard"]
    load = a.power_w("coil load", prefix="load")
    ts = a.number("supply_temp_c", "C", "chilled-water supply temperature", 0.5, 30,
                  reference=offer_table("chilled_water_practice"))
    tr = a.number("return_temp_c", "C", "chilled-water return temperature", 1, 40)
    if a.incomplete():
        return
    if tr <= ts:
        a.refuse("return %s C is not above supply %s C - no heat is picked up" % (_g(tr), _g(ts)))
        return
    mean = (ts + tr) / 2.0
    rho, cp = PSY.water_density(mean), PSY.water_cp(mean)
    mass = load / 1000.0 / (cp * (tr - ts))
    ls = mass / rho * 1000.0
    a.result("Water flow", "%s L/s  (%s m3/h, %s US gpm)" % (_f(ls, 3), _f(ls * 3.6, 2),
                                                           _f(ls / LS_PER_USGPM, 1)))
    a.result("Mass flow", "%s kg/s" % _f(mass, 3))
    a.result("Temperature rise", "%s K, water at %s C mean (%s kg/m3, cp %s kJ/kg.K)"
             % (_f(tr - ts, 1), _f(mean, 1), _f(rho, 2), _f(cp, 4)))
    a.uses("m = Q / (cp dT), with density and cp at the mean water temperature")
    a.cite(SRC_WATER)
    rise_ok, leaving_ok = tr - ts >= 8.33 - 1e-9, tr >= 13.89 - 1e-9
    rule = ("a coil is selected for at least 8.33 K rise and 13.89 C leaving water, with "
            "exceptions (%s Section 6.5.4.7) - this one is %s K and %s C"
            % (HELD_90_1, _f(tr - ts, 2), _f(tr, 2)))
    applies = _applies_90_1(energy)
    if applies is None:
        a.check("WARN", "not checked: whether ASHRAE 90.1 applies to this project is not "
                        "known yet. Where it does, " + rule)
    elif applies:
        a.check("OK" if rise_ok and leaving_ok else "WARN",
                "ASHRAE %s applies to this project: %s%s"
                % (energy, rule, _edition_said(energy, HELD_90_1)))
    else:
        a.check("OK", "ASHRAE 90.1's coil selection rule is not this project's - its energy "
                      "standard is %s" % energy)
    a.check("OK", "next: pipe_size with this flow")


@calculation("pipe_size", "Water pipe sizing", "water")
def calc_pipe_size(a):
    """Sizes a water pipe - or several - to a friction rate, a velocity limit or both, from the internal diameters of the pipe the project uses."""
    energy = project_standards(a)["energy_standard"]
    items = a.records("items", "several pipes at once: id and flow_ls for each",
                      required=False)
    q = None if items else a.flow("water flow in the pipe (or items for several)", water=True)
    t = a.number("water_temp_c", "C", "water temperature - for density and viscosity", 0, 100)
    has_ids, has_series = a.has("internal_diameters_mm"), a.has("pipe_series")
    series = None
    if has_ids == has_series:
        if has_ids:
            a.refuse("give internal_diameters_mm or pipe_series, not both")
        else:
            a.need("internal_diameters_mm", "list of mm (or pipe_series)",
                   "the INSIDE diameters of the pipe sizes the project uses",
                   offer_table("pipe_sizes"))
    diameters = (a.numbers("internal_diameters_mm", "mm", "inside diameters", 3, 2000)
                 if has_ids else None)
    if has_series:
        series = a.word("pipe_series", "a pipe series from `reference pipe_sizes`")
        data = REFERENCES.get("pipe_sizes")
        rows = [r for r in (data or {}).get("rows", []) if series and str(r[0]).lower() == series.lower()]
        if not rows:
            a.refuse("pipe_series %r is not in `reference pipe_sizes`" % series)
        else:
            diameters = [float(r[2]) for r in rows]
    max_f = a.number("max_friction_pa_m", "Pa/m", "friction rate not to exceed", 1, 5000,
                     required=False, reference=offer_table("pipe_design_guidance"))
    max_v = a.number("max_velocity_ms", "m/s", "velocity not to exceed", 0.1, 10,
                     required=False, reference=offer_table("pipe_design_guidance"))
    if max_f is None and max_v is None and not a.has("max_friction_pa_m") \
            and not a.has("max_velocity_ms"):
        a.need("max_friction_pa_m", "Pa/m (and/or max_velocity_ms)",
               "what to size to - a friction rate, a velocity limit, or both",
               offer_table("pipe_design_guidance"))
    rough, rough_note = read_roughness(a, "the pipe's inside roughness", water=True)
    flows = []
    for item in items or []:
        fl = item.flow("water flow in this pipe", water=True)
        flows.append((item.text("id") or item.prefix, fl))
    if a.incomplete():
        return
    if q is not None:
        flows = [("the pipe", q)]
    rho, mu = PSY.water_density(t), PSY.water_viscosity(t)
    labels = {}
    if series:
        for r in REFERENCES["pipe_sizes"]["rows"]:
            if str(r[0]).lower() == series.lower():
                labels[float(r[2])] = "%s (%s mm bore)" % (r[1], _f(float(r[2]), 1))
    table = []
    for ident, fl in flows:
        chosen = None
        for d in sorted(diameters):
            v, pv, re, f, pa_m = pipe_friction(fl, d, rho, mu, rough)
            if (max_f is None or pa_m <= max_f + 1e-12) and (max_v is None or v <= max_v + 1e-12):
                chosen = (d, v, pa_m, re)
                break
        if chosen is None:
            a.check("FAIL", "%s (%s L/s) needs more than the largest bore given"
                    % (ident, _f(fl, 2)))
            continue
        d, v, pa_m, re = chosen
        name = labels.get(d, "%s mm bore" % _f(d, 1))
        table.append([ident, _f(fl, 2), name, _f(v, 2), _f(pa_m, 1),
                      _f(pa_m / (rho * 9.80665) * 100.0, 2)])
        if len(flows) == 1:
            a.result("Pipe", name)
            a.result("Velocity", velocity_text(v))
            a.result("Friction", "%s Pa/m  (%s m water per 100 m, %s ft per 100 ft)"
                     % (_f(pa_m, 1), _f(pa_m / (rho * 9.80665) * 100.0, 2),
                        _f(pa_m / (rho * 9.80665) * 100.0, 2)))
            a.result("Reynolds number", _f(re, 0))
    if len(flows) > 1:
        a.table("Pipe sizes", ("id", "L/s", "pipe", "m/s", "Pa/m", "m/100 m"), table)
    a.result("Water", "%s C - %s kg/m3, %s mPa.s" % (_f(t, 1), _f(rho, 2), _f(mu * 1000.0, 4)))
    a.result("Roughness", rough_note)
    if _applies_90_1(energy) is None:
        a.check("WARN", "not checked: whether ASHRAE 90.1 applies to this project is not "
                        "known yet - where it does, it caps a pipe's flow by its size and "
                        "operating hours")
    elif _applies_90_1(energy):
        a.check("WARN", "ASHRAE %s applies to this project: it caps a pipe's flow by its "
                        "size and operating hours - `reference pipe_design_guidance` holds "
                        "%s's Table 6.5.4.6; check each size chosen against it%s"
                % (energy, HELD_90_1, _edition_said(energy, HELD_90_1)))
    a.uses("smallest bore meeting every limit given; Darcy-Weisbach with Colebrook")
    a.cite(SRC_WATER)
    a.into_revit("SET_MEP_SIZE sets a pipe's diameter; AUTO_SIZE_PIPE is the in-model method")


@calculation("unit_select", "Fan coil or split unit selection", "equipment")
def calc_unit_select(a):
    """Picks a fan coil or split unit for a room from the manufacturer's own catalogue rows - the smallest that covers the total load, and the sensible load and the airflow where given - and says how far each is over the load.

    Capacity belongs to the product, at the conditions its maker rates it at; Heron holds no catalogue and picks nothing without one.
    """
    kind = a.choice("unit_type", ("fan-coil", "split"),
                    "fan-coil (chilled water) or split (refrigerant)")
    total = a.power_w("the room's total cooling load - cooling_load or monthly_load gives "
                      "it", prefix="load")
    sensible = a.power_w("the room's sensible load - given, a unit must cover it too",
                         prefix="sensible", required=False)
    flow = a.flow("the supply airflow the room needs - given, a unit must move it",
                  prefix="flow", required=False)
    over = a.number("max_oversize_pct", "%", "how far over the total load a unit may be "
                    "before it is flagged", 0, 500, required=False)
    units = []
    for row in a.records("catalogue", "the maker's rows, smallest first: model and "
                         "total_kw, with sensible_kw and airflow_ls where the maker lists "
                         "them - at the conditions the maker rates them") or []:
        model = row.word("model", "the model, as the maker names it")
        tot = row.number("total_kw", "kW", "total cooling capacity", 0.1, 5000, positive=True)
        sen = row.number("sensible_kw", "kW", "sensible cooling capacity", 0.1, 5000,
                         required=False, positive=True)
        air = row.flow("rated airflow", prefix="airflow", required=False)
        if None not in (model, tot):
            units.append((model, tot * 1000.0, None if sen is None else sen * 1000.0, air))
    if a.incomplete():
        return
    if sensible is not None and sensible > total + 1e-9:
        a.refuse("the sensible load %s W is more than the total %s W" % (_f(sensible, 0),
                                                                         _f(total, 0)))
        return
    rows, chosen = [], None
    for model, tot, sen, air in units:
        short = []
        if tot < total - 1e-9:
            short.append("total %s kW short" % _f((total - tot) / 1000.0, 2))
        if sensible is not None:
            if sen is None:
                short.append("no sensible capacity listed")
            elif sen < sensible - 1e-9:
                short.append("sensible %s kW short" % _f((sensible - sen) / 1000.0, 2))
        if flow is not None:
            if air is None:
                short.append("no airflow listed")
            elif air < flow - 1e-9:
                short.append("airflow %s L/s short" % _f(flow - air, 1))
        rows.append([model, _f(tot / 1000.0, 2), "-" if sen is None else _f(sen / 1000.0, 2),
                     "-" if air is None else _f(air, 0),
                     "%+.0f %%" % ((tot - total) / total * 100.0) if total > 0 else "-",
                     ", ".join(short) or "meets"])
        if not short and chosen is None:
            chosen = (model, tot, sen, air)
    a.table("The catalogue against a %s kW load" % _f(total / 1000.0, 2),
            ("model", "total kW", "sensible kW", "airflow L/s", "over the load", "verdict"),
            rows)
    if chosen is None:
        a.check("FAIL", "no unit in the catalogue covers the load%s%s"
                % (" and the sensible load" if sensible is not None else "",
                   " and the airflow" if flow is not None else ""))
    else:
        model, tot, sen, air = chosen
        margin = (tot - total) / total * 100.0 if total > 0 else 0.0
        a.result("Selected", "%s - %s kW total against a %s kW load, %s %% over"
                 % (model, _f(tot / 1000.0, 2), _f(total / 1000.0, 2), _f(margin, 0)))
        if sensible is not None:
            a.result("Sensible", "%s kW against %s kW" % (_f(sen / 1000.0, 2),
                                                          _f(sensible / 1000.0, 2)))
        if flow is not None:
            a.result("Airflow", "%s L/s against %s L/s" % (_f(air, 0), _f(flow, 0)))
        if over is not None and margin > over + 1e-9:
            a.check("WARN", "%s is %s %% over the load, past the %s %% given"
                    % (model, _f(margin, 0), _g(over)))
    a.uses("the first unit, in the catalogue's own order, that covers the total load%s%s - "
           "so list the units smallest first"
           % (", the sensible load" if sensible is not None else "",
              " and the airflow" if flow is not None else ""))
    a.cite("the manufacturer's own catalogue, as given - each capacity at the conditions "
           "its maker rates it at")
    if kind == "fan-coil":
        a.check("OK", "next: chw_flow with this load for the coil's chilled water, then "
                      "pipe_size")
    else:
        a.check("WARN", "refrigerant piping - its length, lift and line sizes - is the "
                        "maker's to state, and Heron does not size it")
    a.into_revit("CHANGE_ELEMENT_TYPE swaps a placed unit to the selected model's type, "
                 "where the family carries it")


# --- reference ---------------------------------------------------------------

@calculation("reference", "Reference tables from the standards", "reference")
def calc_reference(a):
    """The cited reference tables Heron holds - ventilation rates, zone air distribution effectiveness, exhaust rates, people heat gains, duct roughness, ADPI throw ratios and the rest - shown so a figure can be OFFERED, never applied by themselves."""
    table = a.word("table", "which table - left out, the list of tables", required=False)
    search = a.word("search", "only the rows containing this text", required=False)
    if a.incomplete():
        return
    if not table:
        a.table("Reference tables", ("table", "what it holds", "source"),
                [[k, v["title"], v["source"]] for k, v in REFERENCES.items()])
        return
    key = table.strip().lower().replace("-", "_").replace(" ", "_")
    if key not in REFERENCES:
        a.refuse("no reference table %r - Heron holds: %s" % (table, ", ".join(REFERENCES)))
        return
    data = REFERENCES[key]
    rows = data["rows"]
    if search:
        wanted = search.lower()
        rows = [r for r in rows if any(wanted in str(c).lower() for c in r)]
    a.table("%s - %s" % (data["title"], data["source"]), data["columns"],
            [[_g(c) if isinstance(c, float) else c for c in r] for r in rows])
    if data.get("note"):
        a.uses(data["note"])
    a.cite(data["source"])
    a.check("WARN", "a reference figure is OFFERED to the modeller and used only once they "
                    "confirm it - check it against the edition your authority adopted")


# ---------------------------------------------------------------------------
# Running one, and saying what came out
# ---------------------------------------------------------------------------

def parse_inputs(text):
    """The inputs as a dict, from a JSON object string - or Refused saying why not."""
    if text is None or (isinstance(text, str) and not text.strip()):
        return {}
    if isinstance(text, dict):
        return text
    try:
        value = json.loads(text)
    except ValueError as why:
        raise Refused("inputs must be a JSON object - %s" % why)
    if not isinstance(value, dict):
        raise Refused("inputs must be a JSON object of named values, not %s"
                      % type(value).__name__)
    return value


def run(name, inputs=None, recorded=None):
    """
    One calculation, as a dict whose `status` is ok, missing, refused or unknown.

    A calculation that did not complete carries no results at all - a partial
    answer is not shown as a smaller one.

    `recorded` is the project's governing standards as kept for it - a dict of
    name to {"value", "recorded"} - and the caller's to keep (D-111). The answer
    says which standards it used and where each came from (`standards`), and
    which are still to be asked once (`ask_once`), whatever its status: an
    answer given in a call that is missing something else is still an answer.
    """
    key = (name or "").strip().lower().replace("-", "_").replace(" ", "_")
    if key not in CALCULATIONS:
        return {"calculation": key, "status": "unknown", "title": None,
                "known": list(CALCULATIONS), "missing": [], "refused": [],
                "ignored": [], "results": [], "tables": [], "checks": [], "method": [],
                "sources": [], "assumed": [], "next": [], "csv": None,
                "standards": {}, "ask_once": [], "memory": []}
    if not isinstance(inputs, dict):
        try:
            inputs = parse_inputs(inputs)
        except Refused as why:
            answer = Answer(key, {}, recorded)
            answer.refuse(str(why))
            return _as_dict(answer, CALCULATIONS[key])
    answer = Answer(key, inputs, recorded)
    try:
        CALCULATIONS[key]["run"](answer)
    except (Refused, PSY.PsychroRangeError) as why:
        answer.refuse(str(why))
    return _as_dict(answer, CALCULATIONS[key])


def _as_dict(answer, entry):
    status = "refused" if answer.refused else ("missing" if answer.missing else "ok")
    done = status == "ok"
    return {
        "calculation": answer.name,
        "title": entry["title"],
        "status": status,
        "missing": answer.missing,
        "refused": answer.refused,
        "ignored": answer.ignored(),
        "results": answer.results if done else [],
        "tables": answer.tables if done else [],
        "checks": answer.checks if done else [],
        "method": answer.method if done else [],
        "sources": answer.sources if done else [],
        "assumed": answer.assumed if done else [],
        "next": answer.next if done else [],
        "csv": answer.csv if done else None,
        "standards": dict(answer.standards),
        "ask_once": answer.ask_once,
        # What the caller did with the project's standards - kept, changed, or
        # not kept for want of a project. The caller's to fill in (D-111).
        "memory": [],
    }


def catalogue():
    """
    Every calculation, and what it needs - DERIVED by running each on nothing,
    so it is the list the code reads rather than a description of it.
    """
    out = []
    for key, entry in CALCULATIONS.items():
        answer = Answer(key, {})
        try:
            entry["run"](answer)
        except (Refused, PSY.PsychroRangeError):
            pass
        needs = [m["input"] for m in answer.missing]
        out.append({"calculation": key, "title": entry["title"], "group": entry["group"],
                    "purpose": entry["purpose"], "needs": needs,
                    "optional": [o[0] for o in answer.optional if o[0] not in needs],
                    "asks_once": [q["input"] for q in answer.ask_once]})
    return out


def _table_text(table):
    rows = [table["columns"]] + table["rows"]
    widths = [max(len(str(r[i])) if i < len(r) else 0 for r in rows)
              for i in range(len(table["columns"]))]
    lines = ["  " + table["title"]]
    for n, row in enumerate(rows):
        lines.append("    " + "  ".join(str(c).ljust(widths[i]) for i, c in enumerate(row)).rstrip())
        if n == 0:
            lines.append("    " + "  ".join("-" * w for w in widths))
    return lines


def _standards_lines(answer):
    """The project standards an answer used, where each came from, and what was kept."""
    out = []
    for name, entry in (answer.get("standards") or {}).items():
        if entry.get("from") == "record":
            where = "recorded for this project %s" % (entry.get("recorded") or "")[:10]
        else:
            where = "given in this request"
        out.append("  %-21s %-10s %s" % (name, standard_text(entry["value"]), where))
    out.extend("  %s" % line for line in answer.get("memory") or [])
    return out


def describe(answer):
    """The answer as the text a modeller reads."""
    title = answer.get("title") or answer["calculation"]
    status = answer["status"]
    lines = []
    if status == "unknown":
        lines.append("Heron has no HVAC calculation called %r." % answer["calculation"])
        lines.append("It has: %s." % ", ".join(answer["known"]))
        lines.append("Call with no calculation for the list and what each one needs.")
        return "\n".join(lines)
    lines.append("HVAC design - %s%s" % (title, "" if status == "ok" else
                                         " - NOT CALCULATED"))
    if answer["refused"]:
        lines.append("")
        lines.append("REFUSED - an input cannot be a design figure as given:")
        lines.extend("  - %s" % r for r in answer["refused"])
    if answer["missing"]:
        lines.append("")
        lines.append("ASK THE MODELLER - Heron supplies no design value it was not given "
                     "(D-33):")
        for m in answer["missing"]:
            lines.append("  - %s  [%s]  %s" % (m["input"], m["unit"], m["why"]))
            if m.get("reference"):
                lines.append("      reference: %s" % m["reference"])
    if answer.get("ask_once"):
        lines.append("")
        lines.append("ASK ONCE FOR THIS PROJECT - which standards govern it. Heron keeps the "
                     "answers for the project and does not ask again (D-111); until then the "
                     "checks that depend on them are not run:")
        for q in answer["ask_once"]:
            lines.append("  - %s  [%s]  %s" % (q["input"], q["unit"], q["why"]))
    if answer["ignored"]:
        lines.append("")
        lines.append("IGNORED - not an input of this calculation, so nothing was worked out "
                     "from it: %s" % ", ".join(answer["ignored"]))
    if status != "ok":
        if answer.get("memory"):
            lines.append("")
            lines.append("PROJECT STANDARDS")
            lines.extend(_standards_lines(answer))
        lines.append("")
        lines.append("Nothing was calculated.")
        return "\n".join(lines)
    if answer["results"]:
        lines.append("")
        width = max(len(label) for label, _t in answer["results"])
        for label, text in answer["results"]:
            lines.append("  %s  %s" % (label.ljust(width), text))
    for table in answer["tables"]:
        lines.append("")
        lines.extend(_table_text(table))
    if answer["csv"]:
        lines.append("")
        lines.append("  CSV")
        lines.extend("    " + row for row in answer["csv"].rstrip("\n").split("\n"))
    if answer["checks"]:
        lines.append("")
        lines.append("CHECKS")
        lines.extend("  %-4s  %s" % (level, text) for level, text in answer["checks"])
    if answer.get("standards") or answer.get("memory"):
        lines.append("")
        lines.append("PROJECT STANDARDS")
        lines.extend(_standards_lines(answer))
    if answer["assumed"]:
        lines.append("")
        lines.append("NOT GIVEN, SO NOT APPLIED")
        lines.extend("  - %s" % x for x in answer["assumed"])
    if answer["method"]:
        lines.append("")
        lines.append("METHOD")
        lines.extend("  - %s" % x for x in answer["method"])
    if answer["sources"]:
        lines.append("")
        lines.append("SOURCES")
        lines.extend("  - %s" % x for x in answer["sources"])
    if answer["next"]:
        lines.append("")
        lines.append("INTO REVIT")
        lines.extend("  - %s" % x for x in answer["next"])
    lines.append("")
    lines.append(DISCLAIMER)
    return "\n".join(lines)


def describe_catalogue():
    """Every calculation and what it needs, as text."""
    lines = ["Heron's HVAC design calculations. Each takes its inputs as named values "
             "and supplies no design value it was not given (D-33).", ""]
    group = None
    for entry in catalogue():
        if entry["group"] != group:
            group = entry["group"]
            lines.append(group.upper())
        lines.append("  %s - %s" % (entry["calculation"], entry["title"]))
        lines.append("      %s" % entry["purpose"])
        if entry["needs"]:
            lines.append("      needs: %s" % ", ".join(entry["needs"]))
        if entry["optional"]:
            lines.append("      may take: %s" % ", ".join(entry["optional"]))
        if entry["asks_once"]:
            lines.append("      asks once per project: %s" % ", ".join(entry["asks_once"]))
    lines.append("")
    lines.append("A project's governing standards are asked once and kept for that project "
                 "(D-111). A supply diffuser neck in an NC/RC %d room is held to the "
                 "office's own %s m/s (D-110)." % (OFFICE_SUPPLY_NECK_NC,
                                                   _g(OFFICE_SUPPLY_NECK_MS)))
    lines.append("")
    lines.append(DISCLAIMER)
    return "\n".join(lines)


def main(argv):
    if len(argv) < 2:
        print(describe_catalogue())
        return 0
    name = argv[1]
    if name == "reference":
        inputs = {"table": argv[2]} if len(argv) > 2 else {}
        if len(argv) > 3:
            inputs["search"] = argv[3]
    else:
        try:
            inputs = parse_inputs(argv[2] if len(argv) > 2 else "")
        except Refused as why:
            print("refused: %s" % why)
            return 2
    answer = run(name, inputs)
    print(describe(answer))
    return 0 if answer["status"] == "ok" else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))

