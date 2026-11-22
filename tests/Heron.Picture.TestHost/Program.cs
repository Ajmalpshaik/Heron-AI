// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Revit.Addin;

namespace Heron.Picture.TestHost
{
    /// <summary>
    /// The two rules a picture of a view keeps (FRAGMENT-ISSUES row 5b-1890),
    /// checked without Revit: a picture taller than the width asked is taken
    /// again fitted on its height, and in a family a model element that was
    /// not asked for and reaches out of the section box is hidden. Whether
    /// Revit then exports and hides as told is the PC's to prove, NEEDS-CHECKING
    /// Group PM.
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        /// <summary>
        /// The first 24 bytes of a PNG of WIDE x HIGH, and some bytes after -
        /// the signature, the IHDR chunk's length and type, then its width and
        /// height, most significant first (PNG specification 11.2.2).
        /// </summary>
        private static byte[] Png(long wide, long high)
        {
            var bytes = new List<byte> { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
                                         0x00, 0x00, 0x00, 0x0D, (byte)'I', (byte)'H', (byte)'D', (byte)'R' };
            foreach (var value in new[] { wide, high })
            {
                bytes.Add((byte)((value >> 24) & 0xFF));
                bytes.Add((byte)((value >> 16) & 0xFF));
                bytes.Add((byte)((value >> 8) & 0xFF));
                bytes.Add((byte)(value & 0xFF));
            }
            bytes.AddRange(new byte[] { 8, 6, 0, 0, 0 });
            return bytes.ToArray();
        }

        private static int Main()
        {
            Console.WriteLine("A picture's longest side is the width asked, and a family's frame is its parts");
            Console.WriteLine();

            // 1. THE SIZE, FROM THE HEADER. The field report's picture.
            int w, h;
            Check(HeronPicture.Size(Png(1000, 5159), out w, out h) && w == 1000 && h == 5159,
                  "a PNG's width and height are read from its header - " + w + " x " + h);
            Check(HeronPicture.Size(Png(1, 1), out w, out h) && w == 1 && h == 1,
                  "a one-pixel picture reads 1 x 1");
            Check(HeronPicture.Size(Png(70000, 300), out w, out h) && w == 70000 && h == 300,
                  "a width past 65535 reads whole - four bytes, not two");

            // ...AND NOTHING THAT IS NOT ONE IS BELIEVED.
            var jpeg = Png(10, 10);
            jpeg[0] = 0xFF;
            Check(!HeronPicture.Size(jpeg, out w, out h) && w == 0 && h == 0,
                  "bytes without the PNG signature are not read, and say 0 x 0");
            var other = Png(10, 10);
            other[12] = (byte)'X';
            Check(!HeronPicture.Size(other, out w, out h), "a PNG whose first chunk is not IHDR is not read");
            Check(!HeronPicture.Size(new byte[23], out w, out h) && !HeronPicture.Size(null, out w, out h),
                  "too few bytes, or none, are not read - and nothing throws");
            Check(!HeronPicture.Size(Png(0, 10), out w, out h) && !HeronPicture.Size(Png(10, 0), out w, out h),
                  "a zero width or height is not a header");
            Check(!HeronPicture.Size(Png(0x80000000L, 10), out w, out h),
                  "a width past 2^31 - 1, which the specification forbids, is not believed");

            // 2. TAKEN AGAIN ONLY WHEN TALLER THAN THE WIDTH ASKED.
            Check(HeronPicture.TooTall(1000, 1000, 5159),
                  "the rod hanger's 1000 x 5159 at 1000 asked is taken again, fitted on its height");
            Check(!HeronPicture.TooTall(1280, 1280, 720) && !HeronPicture.TooTall(1280, 1280, 1280),
                  "a wide picture, or a square one, is sent as it came");
            Check(HeronPicture.TooTall(1280, 1280, 1281),
                  "one pixel taller than the width asked is taken again");
            Check(!HeronPicture.TooTall(1280, 0, 0) && !HeronPicture.TooTall(0, 1000, 5159),
                  "a size not read, or no width asked, never takes it again");

            // 3. WHAT REACHES OUT OF THE BOX. A box 0..10 each way, in feet.
            var low = new[] { 0.0, 0.0, 0.0 };
            var high = new[] { 10.0, 10.0, 10.0 };
            const double tolerance = 0.003;
            Check(!HeronPicture.ReachesOut(low, high, new[] { 2.0, 2.0, 2.0 }, new[] { 3.0, 3.0, 3.0 }, tolerance),
                  "a part wholly inside the box stays - it is the parts' context");
            Check(HeronPicture.ReachesOut(low, high, new[] { -40.0, 4.0, 4.0 }, new[] { 50.0, 6.0, 5.0 }, tolerance),
                  "a context slab running past two faces of the box reaches out");
            for (var axis = 0; axis < 3; axis++)
            {
                var a = new[] { 2.0, 2.0, 2.0 };
                var b = new[] { 3.0, 3.0, 3.0 };
                b[axis] = 10.5;
                Check(HeronPicture.ReachesOut(low, high, a, b, tolerance),
                      "past the high face on axis " + "xyz"[axis] + " reaches out");
                a = new[] { 2.0, 2.0, 2.0 };
                b = new[] { 3.0, 3.0, 3.0 };
                a[axis] = -0.5;
                Check(HeronPicture.ReachesOut(low, high, a, b, tolerance),
                      "past the low face on axis " + "xyz"[axis] + " reaches out");
            }
            Check(!HeronPicture.ReachesOut(low, high, new[] { -0.002, 0.0, 0.0 }, new[] { 10.002, 10.0, 10.0 }, tolerance),
                  "an extent within the tolerance of the faces does not - a rounding in a box is not reaching out");
            Check(!HeronPicture.ReachesOut(low, high, null, high, tolerance)
                  && !HeronPicture.ReachesOut(low, high, new[] { 1.0, 1.0 }, high, tolerance)
                  && !HeronPicture.ReachesOut(low, high, new[] { double.NaN, 1.0, 1.0 }, new[] { 50.0, 1.0, 1.0 }, tolerance),
                  "an extent not given, short, or not a number never reaches out - the element is left as it was");

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks);
                return 1;
            }
            Console.WriteLine("PASSED - " + _checks + " checks.");
            return 0;
        }
    }
}
