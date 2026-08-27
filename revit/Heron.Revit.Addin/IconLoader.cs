// Heron-Agent:  none
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Loads ribbon icons from the Resources folder deployed beside the assembly.
    ///
    /// Two details matter and both are easy to get wrong:
    ///
    ///   * DecodePixelWidth/Height at the size actually wanted. Letting WPF
    ///     scale a 32 px image down to 16 px afterwards gives a soft, muddy
    ///     small icon.
    ///
    ///   * The DPI must be normalised to 96. Revit sizes a ribbon image by its
    ///     DPI-derived size rather than its pixel size, so an icon exported at
    ///     72 or 144 DPI renders too large or too small no matter what the
    ///     pixel dimensions say. Nothing in the image looks wrong; only the
    ///     button does.
    ///
    /// Returns null rather than throwing when an icon is missing. A button with
    /// no picture is a cosmetic problem; an exception here happens during
    /// OnStartup and costs the whole add-in.
    /// </summary>
    internal sealed class IconLoader
    {
        private const int LargeIconSize = 32;
        private const int SmallIconSize = 16;
        private const double TargetDpi = 96.0;

        private readonly string _resourcesFolder;

        public IconLoader(string assemblyPath)
        {
            var folder = Path.GetDirectoryName(assemblyPath);
            _resourcesFolder = string.IsNullOrEmpty(folder)
                ? string.Empty
                : Path.Combine(folder, "Resources");
        }

        /// <summary>The 32 px image for a large ribbon button.</summary>
        public BitmapSource LoadLarge(string fileName)
        {
            return Load(fileName, LargeIconSize);
        }

        /// <summary>The 16 px image, used when the panel is collapsed.</summary>
        public BitmapSource LoadSmall(string fileName)
        {
            return Load(fileName, SmallIconSize);
        }

        private BitmapSource Load(string fileName, int decodePixels)
        {
            if (string.IsNullOrEmpty(_resourcesFolder) || string.IsNullOrEmpty(fileName))
                return null;

            var path = Path.Combine(_resourcesFolder, fileName);
            if (!File.Exists(path)) return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                // OnLoad, so the file is not left open. A locked icon file cannot
                // be replaced by an update while Revit is running.
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = decodePixels;
                bitmap.DecodePixelHeight = decodePixels;
                bitmap.EndInit();
                bitmap.Freeze();
                return NormalizeDpi(bitmap);
            }
            catch (NotSupportedException) { return null; }   // not a readable image
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        private static BitmapSource NormalizeDpi(BitmapSource source)
        {
            if (source == null) return null;
            if (Math.Abs(source.DpiX - TargetDpi) < 0.1 &&
                Math.Abs(source.DpiY - TargetDpi) < 0.1)
            {
                return source;
            }

            var stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            var pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            var normalized = BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                TargetDpi, TargetDpi,
                source.Format, source.Palette, pixels, stride);
            normalized.Freeze();
            return normalized;
        }
    }
}
