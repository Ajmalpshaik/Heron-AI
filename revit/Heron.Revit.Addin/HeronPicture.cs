// Heron-Agent:  none
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Two rules RevitCapture keeps for a picture of a view, with nothing
    /// Revit-shaped in them - FRAGMENT-ISSUES row 5b-1890, issue #1112.
    ///
    /// SPLIT OUT OF RevitCapture.cs SO IT CAN BE PROVED WITHOUT REVIT, the
    /// move HeronFailureNote.cs makes: this file names no Autodesk type, so
    /// tests/Heron.Picture.TestHost links it BY SOURCE and there is ONE copy
    /// of each rule. RevitCapture reads the picture and the boxes and does
    /// what this decides.
    ///
    /// WHY (seen 2026-11-19 on Revit 2020, issue #1051 item A5). A rod hanger
    /// about 760 x 140 mm, pictured as its View 1, came back 1000 x 5159 px:
    /// the export fits the width asked and lets the height follow the view.
    /// The chat scales any picture down to its own size before it reads it,
    /// so the pixels past the width are bytes nobody sees. And with
    /// `elements=` the frame zoomed out to a context slab crossing the
    /// section box, so the small parts could not be shown close up.
    ///
    ///   THE LONGEST SIDE IS THE WIDTH ASKED. A picture that comes back
    ///   taller than that is taken again fitted on its height - Revit's own
    ///   FitDirectionType.Vertical, "fit a specific number of pixels in
    ///   vertical direction" (RevitAPI.xml, on every release since 2011). Its
    ///   shape is the view's; only its size changes.
    ///
    ///   IN A FAMILY, WHAT REACHES OUT OF THE BOX IS NOT THE PICTURE. A model
    ///   element that was not asked for and whose extent passes a face of
    ///   the section box is hidden in the temporary view and named - the
    ///   move already made for connectors (row 5b-1327), whose symbols drew
    ///   past the box and the picture was fitted to them. What lies wholly
    ///   inside the box stays as context.
    /// </summary>
    internal static class HeronPicture
    {
        /// <summary>
        /// The eight bytes every PNG starts with, then the IHDR chunk, whose
        /// first two fields are the width and the height, four bytes each,
        /// most significant first (the PNG specification, section 11.2.2).
        /// </summary>
        private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        /// The picture's width and height in pixels, read from its PNG header -
        /// or false, both 0, when the bytes are not a PNG whose header can be
        /// read. Never throws: a picture whose size cannot be read is sent as
        /// it came, never refused for it.
        /// </summary>
        internal static bool Size(byte[] png, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (png == null || png.Length < 24) return false;
            for (var i = 0; i < Signature.Length; i++)
            {
                if (png[i] != Signature[i]) return false;
            }
            if (png[12] != (byte)'I' || png[13] != (byte)'H' || png[14] != (byte)'D' || png[15] != (byte)'R')
                return false;

            var w = BigEndian(png, 16);
            var h = BigEndian(png, 20);
            // The specification allows 1 to 2^31 - 1. Anything else is not a
            // header, and is not believed.
            if (w <= 0 || h <= 0 || w > int.MaxValue || h > int.MaxValue) return false;
            width = (int)w;
            height = (int)h;
            return true;
        }

        /// <summary>
        /// Whether a picture asked for at WIDTH and come back WIDE x HIGH must
        /// be taken again fitted on its height: true only when it is taller
        /// than the width asked. A picture whose size could not be read (0) is
        /// never taken again - there is nothing to judge it by.
        /// </summary>
        internal static bool TooTall(int asked, int wide, int high)
        {
            return asked > 0 && wide > 0 && high > asked;
        }

        /// <summary>
        /// Whether an element's extent, LOW to HIGH, passes any face of the
        /// section box BOXLOW to BOXHIGH by more than TOLERANCE - the same
        /// units on both sides (Revit's feet). Each is three numbers, x, y, z.
        /// An extent not given, or not three numbers, never reaches out: the
        /// element is left as it was.
        /// </summary>
        internal static bool ReachesOut(double[] boxLow, double[] boxHigh,
                                        double[] low, double[] high, double tolerance)
        {
            if (!Three(boxLow) || !Three(boxHigh) || !Three(low) || !Three(high)) return false;
            for (var axis = 0; axis < 3; axis++)
            {
                if (low[axis] < boxLow[axis] - tolerance) return true;
                if (high[axis] > boxHigh[axis] + tolerance) return true;
            }
            return false;
        }

        private static bool Three(double[] point)
        {
            if (point == null || point.Length != 3) return false;
            foreach (var value in point)
            {
                if (double.IsNaN(value) || double.IsInfinity(value)) return false;
            }
            return true;
        }

        private static long BigEndian(byte[] bytes, int at)
        {
            return ((long)bytes[at] << 24) | ((long)bytes[at + 1] << 16)
                 | ((long)bytes[at + 2] << 8) | bytes[at + 3];
        }
    }
}
