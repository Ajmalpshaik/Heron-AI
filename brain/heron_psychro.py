# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Moist air and water, worked out from the published equations - the physics
every HVAC answer in brain/heron_hvac.py stands on.

    python brain/heron_psychro.py 24 50          a state point: 24 C, 50 % RH
    python brain/heron_psychro.py 24 50 300      ... at 300 m above sea level

WHAT IS IN HERE, AND WHERE EACH PIECE COMES FROM
------------------------------------------------
Every equation is the published one, re-authored here rather than copied from
any library (D-25), and each function names its source in its docstring:

  saturation pressure   Hyland & Wexler (1983), as ASHRAE Handbook -
                        Fundamentals Ch. 1 gives it: eq. 5 over ice, eq. 6
                        over liquid water
  standard atmosphere   ASHRAE Fundamentals Ch. 1, eq. 3
  humidity ratio,       ASHRAE Fundamentals Ch. 1 - the ideal-gas relations
  enthalpy, volume      for moist air (eqs. 20, 22, 26/28, 32)
  wet bulb              ASHRAE Fundamentals Ch. 1, eq. 33 (above freezing)
                        and eq. 35 (below), SOLVED here rather than read off a
                        chart
  dew point             the saturation equations above solved backwards - the
                        exact answer the Peppers correlation approximates
  air viscosity         Sutherland's law, the textbook three-constant form
  water                 Kell (1975) density and Laliberte (2007) viscosity,
                        both checked against IAPWS; cp from IAPWS-95 values

NOTHING HERE IS A DESIGN VALUE
------------------------------
D-33: Heron never supplies a value the user did not give. Everything in this
file is a constant of nature or a definition - the Hyland-Wexler coefficients,
the gas-constant ratio 0.621945, the exact foot - and none of it is a choice a
designer makes. The design numbers (room temperature, supply temperature,
humidity, altitude) arrive as arguments, every time.

IT REFUSES RATHER THAN EXTRAPOLATES
-----------------------------------
Each equation has a range it was fitted over. Outside it the arithmetic still
returns a number, and a number is the most convincing wrong answer there is.
So every function that has a range raises PsychroRangeError when asked outside
it, naming the range - the same choice HeronUnits makes for a length past
100 km (D-20): this is the last place a malformed number can be stopped.
"""

import math
import sys


class PsychroRangeError(ValueError):
    """Asked for a property outside the range its equation holds over."""


# ---------------------------------------------------------------------------
# Definitions. Not measurements, not design values.
# ---------------------------------------------------------------------------

KELVIN = 273.15                # 0 C in kelvin, by definition
TRIPLE_POINT_C = 0.01          # where ice and liquid saturation curves meet
STANDARD_PRESSURE_KPA = 101.325

# Molar-mass ratio of water vapour to dry air, as ASHRAE Fundamentals
# (2009 onward) writes it: 18.015268 / 28.966.
MOLAR_RATIO = 0.621945

# Dry-air gas constant in kJ/(kg.K), and the factor that makes moist-air
# specific volume carry its vapour - ASHRAE Fundamentals Ch. 1, eq. 26/28.
R_DRY_AIR = 0.287042
VAPOUR_FACTOR = 1.607858

# Moist-air enthalpy terms, kJ/kg, ASHRAE Fundamentals Ch. 1 eq. 32:
#   h = 1.006 t + W (2501 + 1.86 t)
CP_DRY_AIR = 1.006
CP_VAPOUR = 1.86
HFG_0C = 2501.0


# ---------------------------------------------------------------------------
# Saturation - Hyland & Wexler, as ASHRAE Fundamentals Ch. 1 eqs. 5 and 6.
# pws in Pa, T in K. Over ice from -100 C to the triple point, over liquid
# water from the triple point to 200 C.
# ---------------------------------------------------------------------------

_ICE = (-5.6745359e+03, 6.3925247e+00, -9.6778430e-03, 6.2215701e-07,
        2.0747825e-09, -9.4840240e-13, 4.1635019e+00)
_LIQUID = (-5.8002206e+03, 1.3914993e+00, -4.8640239e-02, 4.1764768e-05,
           -1.4452093e-08, 6.5459673e+00)

SATURATION_RANGE_C = (-100.0, 200.0)


def _in_range(name, value, low, high, unit):
    if value != value or value in (float("inf"), float("-inf")):
        raise PsychroRangeError("%s is not a number (%r)" % (name, value))
    if value < low or value > high:
        raise PsychroRangeError(
            "%s %.6g %s is outside %.6g to %.6g %s, the range the equation "
            "holds over - Heron refuses rather than extrapolates"
            % (name, value, unit, low, high, unit))


def saturation_pressure(t_c):
    """
    Saturation pressure of water vapour, kPa, at t_c degrees Celsius.

    ASHRAE Handbook - Fundamentals Ch. 1, eq. 5 (over ice, -100 C to the
    triple point) and eq. 6 (over liquid water, triple point to 200 C), the
    Hyland & Wexler (1983) formulation.
    """
    _in_range("temperature", t_c, SATURATION_RANGE_C[0], SATURATION_RANGE_C[1], "C")
    t = t_c + KELVIN
    if t_c < TRIPLE_POINT_C:
        c1, c2, c3, c4, c5, c6, c7 = _ICE
        ln_p = (c1 / t + c2 + c3 * t + c4 * t ** 2 + c5 * t ** 3
                + c6 * t ** 4 + c7 * math.log(t))
    else:
        c8, c9, c10, c11, c12, c13 = _LIQUID
        ln_p = (c8 / t + c9 + c10 * t + c11 * t ** 2 + c12 * t ** 3
                + c13 * math.log(t))
    return math.exp(ln_p) / 1000.0


def pressure_at_altitude(z_m):
    """
    Standard-atmosphere barometric pressure, kPa, at z_m metres above sea level.

    ASHRAE Fundamentals Ch. 1, eq. 3: p = 101.325 (1 - 2.25577e-5 Z)^5.2559.
    Held to -500 m (the Dead Sea shore is about -430 m) and 11 000 m, the top
    of the troposphere where the equation stops being the standard atmosphere.
    """
    _in_range("altitude", z_m, -500.0, 11000.0, "m")
    return STANDARD_PRESSURE_KPA * (1.0 - 2.25577e-5 * z_m) ** 5.2559


def _pressure_ok(p_kpa):
    _in_range("barometric pressure", p_kpa, 20.0, 120.0, "kPa")


# ---------------------------------------------------------------------------
# Humidity
# ---------------------------------------------------------------------------

def humidity_ratio_from_vapour_pressure(pw_kpa, p_kpa):
    """W, kg water per kg dry air. ASHRAE Fundamentals Ch. 1, eq. 20."""
    _pressure_ok(p_kpa)
    if pw_kpa < 0 or pw_kpa >= p_kpa:
        raise PsychroRangeError(
            "vapour pressure %.4g kPa must be at least 0 and below the "
            "barometric pressure %.4g kPa" % (pw_kpa, p_kpa))
    return MOLAR_RATIO * pw_kpa / (p_kpa - pw_kpa)


def vapour_pressure_from_humidity_ratio(w, p_kpa):
    """pw, kPa - eq. 20 rearranged."""
    _pressure_ok(p_kpa)
    if w < 0:
        raise PsychroRangeError("humidity ratio %.6g cannot be negative" % w)
    return p_kpa * w / (MOLAR_RATIO + w)


def saturation_humidity_ratio(t_c, p_kpa):
    """Ws at t_c - ASHRAE Fundamentals Ch. 1, eq. 23."""
    pws = saturation_pressure(t_c)
    if pws >= p_kpa:
        raise PsychroRangeError(
            "at %.4g C water boils at %.4g kPa - there is no saturated moist "
            "air to describe" % (t_c, p_kpa))
    return humidity_ratio_from_vapour_pressure(pws, p_kpa)


def humidity_ratio_from_rh(t_c, rh_pct, p_kpa):
    """W from dry bulb and relative humidity, percent."""
    _in_range("relative humidity", rh_pct, 0.0, 100.0, "%")
    pw = rh_pct / 100.0 * saturation_pressure(t_c)
    return humidity_ratio_from_vapour_pressure(pw, p_kpa)


def rh_from_humidity_ratio(t_c, w, p_kpa):
    """Relative humidity, percent, from dry bulb and W."""
    return 100.0 * vapour_pressure_from_humidity_ratio(w, p_kpa) / saturation_pressure(t_c)


def humidity_ratio_from_wet_bulb(t_c, twb_c, p_kpa):
    """
    W from dry bulb and thermodynamic wet bulb.

    ASHRAE Fundamentals Ch. 1, eq. 33 for a wet bulb at or above freezing and
    eq. 35 below it, with Ws* the saturation humidity ratio at the wet bulb.
    """
    if twb_c > t_c + 1e-9:
        raise PsychroRangeError(
            "wet bulb %.4g C cannot be above the dry bulb %.4g C" % (twb_c, t_c))
    w = _w_at_wet_bulb(t_c, twb_c, p_kpa)
    if w < 0:
        raise PsychroRangeError(
            "a wet bulb of %.4g C at %.4g C dry bulb is drier than dry air - "
            "check the two readings" % (twb_c, t_c))
    return w


def _w_at_wet_bulb(t_c, twb_c, p_kpa):
    """
    Eq. 33 / 35 as arithmetic, with no judgement on the result. The wet-bulb
    solver needs it raw: bracketing hot dry air - Doha at 45.7 C against a
    22.2 C wet bulb - it evaluates trial wet bulbs near the dew point, where
    the equation goes negative, and a raise there stopped the solve on the
    very air the engine most needs to describe.
    """
    ws_star = saturation_humidity_ratio(twb_c, p_kpa)
    if twb_c >= 0:
        return (((2501.0 - 2.326 * twb_c) * ws_star - 1.006 * (t_c - twb_c))
                / (2501.0 + 1.86 * t_c - 4.186 * twb_c))
    return (((2830.0 - 0.24 * twb_c) * ws_star - 1.006 * (t_c - twb_c))
            / (2830.0 + 1.86 * t_c - 2.1 * twb_c))


def _bisect(f, low, high, tolerance=1e-7, limit=200):
    """A root of f between low and high, where f(low) and f(high) differ in sign."""
    f_low = f(low)
    for _ in range(limit):
        mid = 0.5 * (low + high)
        f_mid = f(mid)
        if (f_mid > 0) == (f_low > 0):
            low, f_low = mid, f_mid
        else:
            high = mid
        if high - low < tolerance:
            break
    return 0.5 * (low + high)


def wet_bulb(t_c, w, p_kpa):
    """
    Thermodynamic wet bulb, C, from dry bulb and W - eq. 33/35 solved for t*.

    Solved by bisection between the dew point and the dry bulb, where the
    eq. 33/35 humidity ratio rises monotonically with t*. A chart reads it to
    a few tenths; this reads it to a ten-thousandth.
    """
    low = dew_point(t_c, w, p_kpa)
    if low >= t_c:
        return t_c
    return _bisect(lambda tw: _w_at_wet_bulb(t_c, tw, p_kpa) - w, low, t_c)


def dew_point(t_c, w, p_kpa):
    """
    Dew point, C - the temperature at which the air's own vapour pressure is
    the saturation pressure. Below the triple point it is the frost point,
    over ice, as eq. 5 defines saturation there.

    Solved from the saturation equations rather than from the Peppers
    correlation (ASHRAE eq. 39/40), which is a fit to exactly this.
    """
    pw = vapour_pressure_from_humidity_ratio(w, p_kpa)
    if pw <= 0:
        raise PsychroRangeError("perfectly dry air has no dew point")
    if pw > saturation_pressure(t_c) * (1.0 + 1e-9):
        raise PsychroRangeError(
            "W %.6g at %.4g C is supersaturated - more water than the air can "
            "hold as vapour" % (w, t_c))
    low = SATURATION_RANGE_C[0]
    if saturation_pressure(low) > pw:
        raise PsychroRangeError("the dew point is below -100 C")
    return _bisect(lambda td: saturation_pressure(td) - pw, low, t_c)


# ---------------------------------------------------------------------------
# Energy and volume
# ---------------------------------------------------------------------------

def enthalpy(t_c, w):
    """Moist-air enthalpy, kJ per kg DRY air. ASHRAE Fundamentals Ch. 1, eq. 32."""
    return CP_DRY_AIR * t_c + w * (HFG_0C + CP_VAPOUR * t_c)


def humid_specific_heat(w):
    """cp of moist air per kg DRY air, kJ/(kg.K): 1.006 + 1.86 W, from eq. 32."""
    return CP_DRY_AIR + CP_VAPOUR * w


def specific_volume(t_c, w, p_kpa):
    """
    Moist-air specific volume, m3 per kg DRY air.
    ASHRAE Fundamentals Ch. 1, eq. 26/28: v = 0.287042 (t + 273.15)(1 + 1.607858 W) / p.
    """
    _pressure_ok(p_kpa)
    return R_DRY_AIR * (t_c + KELVIN) * (1.0 + VAPOUR_FACTOR * w) / p_kpa


def density(t_c, w, p_kpa):
    """Moist-air density, kg of moist air per m3: (1 + W) / v."""
    return (1.0 + w) / specific_volume(t_c, w, p_kpa)


def air_viscosity(t_c):
    """
    Dynamic viscosity of dry air, Pa.s - Sutherland's law with the textbook
    constants mu0 = 1.716e-5 Pa.s at 273.15 K and S = 110.4 K. Gives
    1.81e-5 Pa.s at 20 C, the figure ASHRAE's standard air is quoted with.
    Moisture changes it by well under one percent at HVAC humidities.
    """
    _in_range("air temperature", t_c, -100.0, 400.0, "C")
    t = t_c + KELVIN
    return 1.716e-5 * (t / 273.15) ** 1.5 * (273.15 + 110.4) / (t + 110.4)


# ---------------------------------------------------------------------------
# A whole state point
# ---------------------------------------------------------------------------

def state(t_c, p_kpa, rh_pct=None, twb_c=None, tdp_c=None, w=None):
    """
    Every property of one moist-air state, from the dry bulb and exactly one
    of relative humidity, wet bulb, dew point or humidity ratio.

    Exactly one, and that is checked: two given at once would usually
    disagree, and quietly preferring one would hide which reading was wrong.
    """
    given = [name for name, value in (("rh_pct", rh_pct), ("twb_c", twb_c),
                                      ("tdp_c", tdp_c), ("w", w))
             if value is not None]
    if len(given) != 1:
        raise PsychroRangeError(
            "a state point needs the dry bulb and exactly ONE of relative "
            "humidity, wet bulb, dew point or humidity ratio - got %s"
            % (", ".join(given) or "none"))
    _pressure_ok(p_kpa)
    if rh_pct is not None:
        w = humidity_ratio_from_rh(t_c, rh_pct, p_kpa)
    elif twb_c is not None:
        w = humidity_ratio_from_wet_bulb(t_c, twb_c, p_kpa)
    elif tdp_c is not None:
        if tdp_c > t_c + 1e-9:
            raise PsychroRangeError(
                "dew point %.4g C cannot be above the dry bulb %.4g C"
                % (tdp_c, t_c))
        w = humidity_ratio_from_vapour_pressure(saturation_pressure(tdp_c), p_kpa)
    if w < 0:
        raise PsychroRangeError("humidity ratio %.6g cannot be negative" % w)
    ws = saturation_humidity_ratio(t_c, p_kpa)
    if w > ws * (1.0 + 1e-9):
        raise PsychroRangeError(
            "W %.6g is above saturation (%.6g) at %.4g C" % (w, ws, t_c))
    v = specific_volume(t_c, w, p_kpa)
    return {
        "t_c": t_c,
        "p_kpa": p_kpa,
        "w": w,
        "rh_pct": rh_from_humidity_ratio(t_c, w, p_kpa),
        "twb_c": wet_bulb(t_c, w, p_kpa),
        "tdp_c": dew_point(t_c, w, p_kpa) if w > 0 else None,
        "h_kj_kg": enthalpy(t_c, w),
        "v_m3_kg": v,
        "density_kg_m3": (1.0 + w) / v,
        "pw_kpa": vapour_pressure_from_humidity_ratio(w, p_kpa),
        "pws_kpa": saturation_pressure(t_c),
        "degree_of_saturation": w / ws if ws > 0 else None,
        "cp_kj_kgk": humid_specific_heat(w),
        "viscosity_pa_s": air_viscosity(t_c),
    }


def mix(mass_1, state_1, mass_2, state_2, p_kpa):
    """
    Adiabatic mixing of two streams, by DRY-AIR MASS - ASHRAE Fundamentals
    Ch. 1: the mixed W and h are mass-weighted, and the mixed dry bulb is the
    temperature at which that h and W coexist. Mixing volumes instead is the
    common shortcut, and wrong by the density difference between the streams.
    """
    total = mass_1 + mass_2
    if mass_1 < 0 or mass_2 < 0 or total <= 0:
        raise PsychroRangeError("mixing needs two non-negative flows that add up to more than zero")
    w = (mass_1 * state_1["w"] + mass_2 * state_2["w"]) / total
    h = (mass_1 * state_1["h_kj_kg"] + mass_2 * state_2["h_kj_kg"]) / total
    t = (h - HFG_0C * w) / (CP_DRY_AIR + CP_VAPOUR * w)
    return state(t, p_kpa, w=w)


# ---------------------------------------------------------------------------
# Water, for coils and pipes - liquid at atmospheric pressure, 0 to 100 C.
#
# Two published correlations and one short table, each checked against the
# IAPWS reference values for liquid water (IAPWS-95 for density and specific
# heat, IAPWS 2008 for viscosity) at every whole degree from 0 to 99 C:
#
#   density     Kell (1975), J. Chem. Eng. Data 20: 97, eq. 16 - within
#               0.0015 % of IAPWS-95
#   viscosity   Laliberte (2007), J. Chem. Eng. Data 52: 321, eq. 11 - within
#               0.32 % of IAPWS 2008, worst near 90 C
#   cp          IAPWS-95 values, interpolated - cp moves only between 4.179
#               and 4.219 kJ/(kg.K) over the whole range
#
# A pipe's friction moves with the fifth root of viscosity, so 0.32 % there
# is under a tenth of a percent on a pressure drop.
# ---------------------------------------------------------------------------

_CP = ((0.01, 4.2194), (5.0, 4.2050), (10.0, 4.1952), (15.0, 4.1885),
       (20.0, 4.1841), (25.0, 4.1813), (30.0, 4.1798), (40.0, 4.1794),
       (50.0, 4.1813), (60.0, 4.1850), (70.0, 4.1901), (80.0, 4.1968),
       (90.0, 4.2052), (99.9, 4.2156))


def _water_range(t_c):
    _in_range("water temperature", t_c, 0.0, 100.0, "C")


def water_density(t_c):
    """Liquid water density, kg/m3, 0 to 100 C - Kell (1975) eq. 16."""
    _water_range(t_c)
    t = t_c
    return ((999.83952 + 16.945176 * t - 7.9870401e-3 * t ** 2 - 46.170461e-6 * t ** 3
             + 105.56302e-9 * t ** 4 - 280.54253e-12 * t ** 5) / (1.0 + 16.879850e-3 * t))


def water_viscosity(t_c):
    """Liquid water dynamic viscosity, Pa.s, 0 to 100 C - Laliberte (2007) eq. 11."""
    _water_range(t_c)
    t = t_c
    return (t + 246.0) / ((0.05594 * t + 5.2842) * t + 137.37) / 1000.0


def water_cp(t_c):
    """Liquid water specific heat, kJ/(kg.K), 0 to 100 C - IAPWS-95 values interpolated."""
    _water_range(t_c)
    if t_c <= _CP[0][0]:
        return _CP[0][1]
    for (t0, c0), (t1, c1) in zip(_CP, _CP[1:]):
        if t0 <= t_c <= t1:
            return c0 + (c1 - c0) * (t_c - t0) / (t1 - t0)
    return _CP[-1][1]


def main(argv):
    if len(argv) < 3:
        print(__doc__.strip().splitlines()[0])
        print("usage: python brain/heron_psychro.py DRY_BULB_C RH_PCT [ALTITUDE_M]")
        return 2
    try:
        t, rh = float(argv[1]), float(argv[2])
        z = float(argv[3]) if len(argv) > 3 else 0.0
        p = pressure_at_altitude(z)
        s = state(t, p, rh_pct=rh)
    except (ValueError, PsychroRangeError) as why:
        print("refused: %s" % why)
        return 1
    print("Moist air at %.2f C, %.1f %% RH, %.0f m (%.3f kPa)" % (t, rh, z, p))
    print("  humidity ratio  %.5f kg/kg  (%.2f g/kg)" % (s["w"], s["w"] * 1000))
    print("  wet bulb        %.2f C" % s["twb_c"])
    print("  dew point       %.2f C" % s["tdp_c"])
    print("  enthalpy        %.2f kJ/kg dry air" % s["h_kj_kg"])
    print("  volume          %.4f m3/kg dry air" % s["v_m3_kg"])
    print("  density         %.4f kg/m3" % s["density_kg_m3"])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
