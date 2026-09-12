// Heron-Agent:  HERON-REVIT-UNI-035
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Globalization;

namespace Heron.Core
{
    /// <summary>
    /// Unit Conversion Agent. Millimetres, which is what the user says, to
    /// Revit's internal length unit, which is what the API takes.
    ///
    /// WHY THIS IS PLAIN ARITHMETIC AND NOT UnitUtils.
    ///
    /// Revit stores every length internally in decimal FEET, in every release
    /// from 2020 to 2027. The international foot has been exactly 304.8 mm by
    /// definition since 1959, so the conversion is exact and has no version,
    /// no locale and no project setting in it.
    ///
    /// UnitUtils would give the same number by a longer road, and that road
    /// has a hole in it: the units API was replaced at Revit 2021.
    /// DisplayUnitType.DUT_MILLIMETERS became UnitTypeId.Millimeters, and the
    /// old overloads were deprecated and then removed. One codebase spanning
    /// 2020 to 2027 would therefore need a compile symbol around a conversion
    /// whose answer never actually changes - and this repository has no
    /// REVIT2021_OR_GREATER symbol to hang it on.
    ///
    /// That is not a hypothetical. A unit call that a newer Revit rejects
    /// outright is a failure this project has already seen elsewhere, and it
    /// hid for months because nothing exercised it. Arithmetic cannot rot the
    /// same way: there is no API here for Autodesk to move.
    ///
    /// The rule for anyone extending this: a conversion with a FIXED ratio
    /// (length, angle) belongs here. A conversion that depends on what the
    /// project is set to display, or on a unit family Heron does not define,
    /// does NOT - that genuinely needs the API, and it needs the version split
    /// that comes with it.
    ///
    /// Lives in the Kernel because it is arithmetic: no Revit reference, no
    /// BIM knowledge. It never mentions what is being moved.
    /// </summary>
    public static class HeronUnits
    {
        /// <summary>
        /// Exact by definition (international foot, 1959). Not a measurement,
        /// so it is not approximate and must never be "improved" to more
        /// decimal places.
        /// </summary>
        public const double MillimetresPerFoot = 304.8;

        /// <summary>
        /// The largest distance Heron will convert, in millimetres: 100 km.
        ///
        /// Far beyond any real building, and far short of where double
        /// precision gets interesting. It exists to catch the transcription
        /// error - a value that arrived in the wrong unit, or with a stray
        /// digit - BEFORE it reaches a transaction. "Move it 200000000 mm"
        /// is not a move anyone meant, and a model that has been moved that
        /// far is not obviously recoverable by eye.
        /// </summary>
        public const double MaxMillimetres = 100.0 * 1000.0 * 1000.0;

        /// <summary>Millimetres to Revit's internal feet.</summary>
        public static double MillimetresToFeet(double millimetres)
        {
            return millimetres / MillimetresPerFoot;
        }

        /// <summary>Revit's internal feet back to millimetres.</summary>
        public static double FeetToMillimetres(double feet)
        {
            return feet * MillimetresPerFoot;
        }

        /// <summary>
        /// Is this a distance Heron is willing to act on?
        ///
        /// Rejects NaN and both infinities. They are not exotic: they are what
        /// arithmetic on a missing or malformed JSON number produces, and they
        /// propagate silently. A NaN reaching ElementTransformUtils is a
        /// geometry operation with no defined result, inside a transaction,
        /// on the user's model.
        ///
        /// Zero is allowed through here on purpose. It is a legitimate number
        /// and a meaningless move, so it is refused where the operation can
        /// say WHY in the user's own terms, not silently classed as invalid
        /// input alongside a NaN.
        /// </summary>
        public static bool IsUsableMillimetres(double millimetres)
        {
            if (double.IsNaN(millimetres) || double.IsInfinity(millimetres)) return false;
            return Math.Abs(millimetres) <= MaxMillimetres;
        }

        /// <summary>
        /// A distance as the user would write it. Always millimetres, always
        /// invariant culture.
        ///
        /// Culture matters more than it looks. On a machine set to a comma
        /// decimal separator, "200.5" formats as "200,5" - which reads as two
        /// numbers in a list, in a sentence the user is being asked to approve
        /// before a write. The number a person confirms and the number Heron
        /// applies must be written the same way everywhere.
        ///
        /// Whole numbers lose their trailing zeros, because "200 mm" is what
        /// was asked for and "200.00 mm" invites the reader to wonder what the
        /// extra precision is telling them.
        /// </summary>
        public static string DescribeMillimetres(double millimetres)
        {
            if (double.IsNaN(millimetres)) return "an unusable distance";
            if (double.IsInfinity(millimetres)) return "an unusable distance";

            var rounded = Math.Round(millimetres, 3);
            var text = rounded.ToString("0.###", CultureInfo.InvariantCulture);
            return text + " mm";
        }

        /// <summary>
        /// A vertical move as a modeller says it: "up 200 mm", "down 50 mm".
        ///
        /// THE SIGN IS NOT THE READER'S JOB. Every caller used to write
        /// " up " itself and hand the signed number to DescribeMillimetres,
        /// so asking to lower something produced *"move 9 ducts up -50 mm"* -
        /// and the undo entry in Revit's own history said the same. It did the
        /// right thing; it described it the way a programmer would.
        ///
        /// Down is a DIRECTION, not a negative up. Found in front of a model
        /// on 2026-09-12 while proving D6, which exists to check exactly that
        /// a negative distance is not treated as an error.
        ///
        /// Zero keeps no direction: "0 mm" moves nowhere, and "up 0 mm" would
        /// be claiming a direction the move does not have.
        /// </summary>
        public static string DescribeVerticalMove(double millimetres)
        {
            if (double.IsNaN(millimetres) || double.IsInfinity(millimetres))
                return DescribeMillimetres(millimetres);

            if (Math.Round(millimetres, 3) == 0) return DescribeMillimetres(0);

            return millimetres < 0
                ? "down " + DescribeMillimetres(-millimetres)
                : "up " + DescribeMillimetres(millimetres);
        }
    }
}
