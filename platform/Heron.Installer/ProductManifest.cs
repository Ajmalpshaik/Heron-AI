// Heron-Agent:  HERON-INS-PKG-012
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Heron.Installer
{
    /// <summary>One row of platform/heron-products.json.</summary>
    /// <remarks>
    /// Read as DATA and never executed - R-13, D-91, Golden Rule 19. Nothing
    /// in this file runs anything the manifest says; it only answers
    /// questions about it.
    /// </remarks>
    public sealed class HeronProduct
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tab { get; set; }
        public string Addin { get; set; }
        public string Assembly { get; set; }
        public string AddInId { get; set; }
        public string Folder { get; set; }
        public string Version { get; set; }
        public string PartOf { get; set; }
        public string State { get; set; }
        public IReadOnlyList<string> Revit { get; set; }
        public IReadOnlyList<string> Requires { get; set; }

        /// <summary>
        /// A heading is a tab built by its pieces, so it installs nothing of
        /// its own. D-93: this is DERIVED from partOf and is never a field
        /// somebody could set wrongly - an entry is a heading exactly when
        /// another entry names it.
        /// </summary>
        public bool IsHeading { get; internal set; }

        /// <summary>
        /// Whether a user may be offered this at all.
        ///
        /// SHIPPED only. PROVING is a Stage 2 shape proof carrying a dummy
        /// button and PLANNED does not exist yet - offering either would put
        /// something in a modeller's ribbon that does nothing.
        /// </summary>
        public bool MayBeOffered
        {
            get { return string.Equals(State, "SHIPPED", StringComparison.Ordinal); }
        }

        public bool SupportsRevit(string release)
        {
            if (Revit == null) return false;
            foreach (var r in Revit)
                if (string.Equals(r, release, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    /// <summary>
    /// The list of Heron products, read from the manifest.
    ///
    /// D-93: adding a product is a line in that file, never a rebuild of the
    /// installer. So NOTHING in this assembly may name a product - if
    /// "heron-doc" appears anywhere outside a test, that rule is broken.
    /// </summary>
    public sealed class ProductManifest
    {
        private readonly List<HeronProduct> _products;

        private ProductManifest(List<HeronProduct> products)
        {
            _products = products;
        }

        public IReadOnlyList<HeronProduct> Products { get { return _products; } }

        /// <summary>
        /// Which GitHub repository publishes this product list's releases, or
        /// null when the file does not say.
        ///
        /// DATA, NOT A CONSTANT IN CODE - Stage 5, and the same reasoning as
        /// R-3. A repository that is renamed or moved is then a line in the
        /// manifest rather than a rebuild of the installer. Nothing is
        /// executed from it; it only ever becomes a URL.
        /// </summary>
        public string SourceOwner { get; private set; }

        public string SourceRepo { get; private set; }

        /// <summary>Whether this manifest says where to fetch a release.</summary>
        public bool KnowsItsSource
        {
            get
            {
                return !string.IsNullOrEmpty(SourceOwner)
                       && !string.IsNullOrEmpty(SourceRepo);
            }
        }

        public HeronProduct Find(string id)
        {
            foreach (var p in _products)
                if (string.Equals(p.Id, id, StringComparison.Ordinal)) return p;
            return null;
        }

        /// <summary>The pieces that build a heading's tab, in manifest order.</summary>
        public IReadOnlyList<HeronProduct> PiecesOf(string headingId)
        {
            var pieces = new List<HeronProduct>();
            foreach (var p in _products)
                if (string.Equals(p.PartOf, headingId, StringComparison.Ordinal)) pieces.Add(p);
            return pieces;
        }

        public static ProductManifest Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "The Heron product list is not at " + path + ", so there is " +
                    "nothing to install. Reinstall Heron, or say which file to read.",
                    path);

            return Parse(File.ReadAllText(path));
        }

        public static ProductManifest Parse(string json)
        {
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (JsonException e)
            {
                // What happened, then what to do next - the wording rule in
                // docs/14. A modeller reading this has a corrupt download.
                throw new InvalidDataException(
                    "The Heron product list could not be read - it is not valid " +
                    "JSON. The download is damaged. Fetch it again. (" +
                    e.Message + ")", e);
            }

            using (doc)
            {
                JsonElement products;
                if (!doc.RootElement.TryGetProperty("products", out products)
                    || products.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidDataException(
                        "The Heron product list has no products in it, so there " +
                        "is nothing to install. Fetch it again.");
                }

                var list = new List<HeronProduct>();
                foreach (var e in products.EnumerateArray()) list.Add(ReadProduct(e));

                // Headings are derived here and nowhere else - D-93.
                var named = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in list)
                    if (!string.IsNullOrEmpty(p.PartOf)) named.Add(p.PartOf);
                foreach (var p in list)
                    p.IsHeading = p.Id != null && named.Contains(p.Id);

                var manifest = new ProductManifest(list);

                // ABSENT IS AN ANSWER, not a fault. A manifest with no source
                // is one nothing can be downloaded from, and the installer
                // says exactly that rather than guessing a repository name.
                JsonElement source;
                if (doc.RootElement.TryGetProperty("source", out source)
                    && source.ValueKind == JsonValueKind.Object)
                {
                    manifest.SourceOwner = Str(source, "owner");
                    manifest.SourceRepo = Str(source, "repo");
                }

                return manifest;
            }
        }

        private static HeronProduct ReadProduct(JsonElement e)
        {
            return new HeronProduct
            {
                Id = Str(e, "id"),
                Name = Str(e, "name"),
                Description = Str(e, "description"),
                Tab = Str(e, "tab"),
                Addin = Str(e, "addin"),
                Assembly = Str(e, "assembly"),
                AddInId = Str(e, "addInId"),
                Folder = Str(e, "folder"),
                Version = Str(e, "version"),
                PartOf = Str(e, "partOf"),
                State = Str(e, "state"),
                Revit = Strings(e, "revit"),
                Requires = Strings(e, "requires"),
            };
        }

        private static string Str(JsonElement e, string name)
        {
            JsonElement v;
            if (!e.TryGetProperty(name, out v)) return null;
            return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        }

        private static IReadOnlyList<string> Strings(JsonElement e, string name)
        {
            JsonElement v;
            if (!e.TryGetProperty(name, out v) || v.ValueKind != JsonValueKind.Array)
                return new string[0];

            var list = new List<string>();
            foreach (var item in v.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String) list.Add(item.GetString());
            return list;
        }
    }
}
