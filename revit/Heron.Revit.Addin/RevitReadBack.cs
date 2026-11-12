// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Reads elements' values as text, for HeronReadBack to compare - the half
    /// of D-156 that needs Revit.
    ///
    /// WHAT IS READ: the element's name, its type's name, where it sits (a
    /// point, or a line's two ends, in millimetres), and every parameter Revit
    /// lists on it, each as the text Revit shows the modeller -
    /// AsValueString, which carries the project's own units - or, where Revit
    /// gives none, the stored value. Nothing is formatted by Heron that Revit
    /// can format itself, so "2400 mm" before and "2700 mm" after is what the
    /// Properties palette would have said.
    ///
    /// IT CHANGES NOTHING AND OPENS NOTHING. Every read is a getter, and every
    /// one is caught on its own: one parameter that throws costs that value,
    /// never the element, and one element that throws costs that element,
    /// never the run. A read is a convenience laid over the change; it must
    /// never be the reason a change fails. Every member used here is the same
    /// on 2020 to 2027.
    /// </summary>
    internal static class RevitReadBack
    {
        private const double MillimetresPerFoot = 304.8;

        /// <summary>
        /// The modeller's selection in `target`, or nothing when `target` is
        /// not the model on screen - a selection belongs to the view the
        /// modeller is looking at, never to a model run in the background.
        /// </summary>
        internal static ICollection<ElementId> SelectedIn(UIApplication app, Document target)
        {
            try
            {
                var front = app == null ? null : app.ActiveUIDocument;
                if (front == null || front.Document == null || target == null
                    || !front.Document.Equals(target))
                    return new List<ElementId>();
                var ids = front.Selection.GetElementIds();
                return ids == null ? new List<ElementId>() : new List<ElementId>(ids);
            }
            catch (Exception)
            {
                return new List<ElementId>();
            }
        }

        /// <summary>
        /// Up to `most` of `ids`, read, by id as text. With `only`, an id not
        /// in it is skipped before it counts toward `most` - so the elements
        /// the other side holds are the ones read, not the first `most` Revit
        /// happened to name.
        /// </summary>
        internal static Dictionary<string, HeronReadBack.Values> Read(
            Document document, IEnumerable<ElementId> ids, int most,
            ICollection<string> only = null)
        {
            var read = new Dictionary<string, HeronReadBack.Values>(StringComparer.Ordinal);
            if (document == null || ids == null) return read;

            foreach (var id in ids)
            {
                if (read.Count >= most) break;
                if (id == null) continue;
                var key = id.ToString();
                if (only != null && !only.Contains(key)) continue;
                if (read.ContainsKey(key)) continue;

                try
                {
                    var element = document.GetElement(id);
                    if (element == null) continue;
                    read[key] = Of(document, element);
                }
                catch (Exception)
                {
                    // Not read - HeronReadBack counts it as NOT READ, never as unchanged.
                }
            }
            return read;
        }

        /// <summary>The ids as text, for Read's `only`.</summary>
        internal static ICollection<string> Keys(Dictionary<string, HeronReadBack.Values> read)
        {
            return read == null ? null : new HashSet<string>(read.Keys, StringComparer.Ordinal);
        }

        private static HeronReadBack.Values Of(Document document, Element element)
        {
            string category = null;
            try { category = element.Category == null ? null : element.Category.Name; }
            catch (Exception) { category = null; }

            var values = new HeronReadBack.Values(category);

            try { values.Add("Name", element.Name); }
            catch (Exception) { }

            try
            {
                var typeId = element.GetTypeId();
                if (typeId != null && typeId != ElementId.InvalidElementId)
                {
                    var type = document.GetElement(typeId);
                    if (type != null) values.Add("Type", type.Name);
                }
            }
            catch (Exception) { }

            try
            {
                var where = Where(element.Location);
                if (where != null) values.Add("Location", where);
            }
            catch (Exception) { }

            ParameterSet parameters = null;
            try { parameters = element.Parameters; }
            catch (Exception) { parameters = null; }
            if (parameters == null) return values;

            // GATHERED WITH EACH PARAMETER'S OWN ID, WHICH IS WHAT THE TWO SIDES
            // ARE PAIRED BY (HeronReadBack.Values.AddById). Two parameters may
            // share a name - a built-in and a project parameter both called
            // "Mark" - the order ParameterSet hands them over in is not
            // documented to be the same twice, and a type change alters which
            // of them the element has at all. An id says which is which.
            var read = new List<HeronReadBack.ParameterRead>();
            foreach (Parameter parameter in parameters)
            {
                try
                {
                    if (parameter == null || parameter.Definition == null) continue;
                    read.Add(new HeronReadBack.ParameterRead(
                        Number(parameter.Id), parameter.Definition.Name, Text(parameter)));
                }
                catch (Exception) { }
            }
            values.AddById(read);
            return values;
        }

        /// <summary>
        /// A parameter's id as a number. Read from the id's own text, the
        /// way every id here is: ElementId's integer member changed name and
        /// width at Revit 2024, and this add-in carries no version #if (D-05).
        /// One that cannot be read is HeronReadBack.NoId, and the rule says
        /// what it does with a parameter it has no id to pair by.
        /// </summary>
        private static long Number(ElementId id)
        {
            long number;
            return id != null && long.TryParse(id.ToString(), NumberStyles.Integer,
                                               CultureInfo.InvariantCulture, out number)
                ? number : HeronReadBack.NoId;
        }

        /// <summary>A parameter as the modeller would read it, or its stored value.</summary>
        private static string Text(Parameter parameter)
        {
            if (!parameter.HasValue) return "";

            string shown = null;
            try { shown = parameter.AsValueString(); }
            catch (Exception) { shown = null; }

            // The stored value when Revit shows no text - and, for a
            // parameter that stores text, when the text it shows is empty
            // (HeronReadBack.HasText says why).
            if (HeronReadBack.HasText(shown, parameter.StorageType == StorageType.String))
                return shown;

            switch (parameter.StorageType)
            {
                case StorageType.String:
                    return parameter.AsString() ?? "";
                case StorageType.Integer:
                    return parameter.AsInteger().ToString(CultureInfo.InvariantCulture);
                case StorageType.Double:
                    return parameter.AsDouble().ToString("R", CultureInfo.InvariantCulture);
                case StorageType.ElementId:
                    var id = parameter.AsElementId();
                    return id == null ? "" : id.ToString();
                default:
                    return "";
            }
        }

        /// <summary>A point, or a line's two ends, in whole millimetres; null for anything else.</summary>
        private static string Where(Location location)
        {
            var point = location as LocationPoint;
            if (point != null && point.Point != null)
            {
                // A point carries a turn as well, and a rotation is a change
                // the point alone would hide. Not every element can say it.
                double turn;
                try { turn = point.Rotation * 180.0 / Math.PI; }
                catch (Exception) { return Mm(point.Point); }
                return Mm(point.Point) + string.Format(CultureInfo.InvariantCulture,
                                                       ", turned {0:0.##} degrees", turn);
            }

            var curve = location as LocationCurve;
            if (curve != null && curve.Curve != null)
                return Mm(curve.Curve.GetEndPoint(0)) + " to " + Mm(curve.Curve.GetEndPoint(1));

            return null;
        }

        private static string Mm(XYZ p)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:0}, {1:0}, {2:0}) mm",
                                 p.X * MillimetresPerFoot, p.Y * MillimetresPerFoot,
                                 p.Z * MillimetresPerFoot);
        }
    }
}
