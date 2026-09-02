// NOT STANDALONE. Assumes `doc`, `elements`, `direction`, `spacingMm` and
// `count` are in scope; leaves `created`, `copiesEach` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// COUNT INCLUDES THE ORIGINAL, so eight means the original plus seven copies.
// That is what Revit's own array does and what somebody counting the result
// expects. Reporting `copiesEach` separately makes the arithmetic visible
// rather than leaving a caller to work out whether "8" meant 8 or 9.
//
// THE DIRECTION IS NORMALISED, so its length is ignored and the spacing is the
// spacing. A direction handed in as a vector between two points would
// otherwise multiply the spacing by that distance - a mistake that produces a
// plausible-looking run at ten times the pitch.
//
// A PLAIN COPY, NOT A REVIT ARRAY ELEMENT. A real Array is a constrained group
// that stays linked and renumbers itself; these are independent. Which one is
// wanted changes what every later edit does, and this fragment does only the
// first.
//
// MILLIMETRES TO FEET IS ARITHMETIC (D-20).

var created = new List<ElementId>();
var refused = new List<ElementId>();

const double MillimetresPerFoot = 304.8;
var copiesEach = count > 1 ? count - 1 : 0;

var length = direction == null ? 0.0 : direction.GetLength();
if (length <= 0.0 || copiesEach <= 0 || spacingMm <= 0.0)
{
    // A zero direction, a count of one, or no spacing. Every element refused
    // BY NAME rather than the batch quietly doing nothing, which reads as a
    // tool that ran and achieved nothing.
    foreach (var element in elements)
    {
        if (element != null) refused.Add(element.Id);
    }
}
else
{
    var unit = direction.Normalize();
    var step = spacingMm / MillimetresPerFoot;

    foreach (var element in elements)
    {
        if (element == null) continue;

        for (var i = 1; i <= copiesEach; i++)
        {
            try
            {
                var offset = unit.Multiply(step * i);
                var copies = ElementTransformUtils.CopyElement(doc, element.Id, offset);

                // An empty return is Revit accepting the call and copying
                // nothing - the silent no-op shape already met on the move
                // path. Recorded as a refusal, never counted as a copy.
                if (copies == null || copies.Count == 0)
                {
                    refused.Add(element.Id);
                    continue;
                }

                foreach (var id in copies) created.Add(id);
            }
            catch (Exception)
            {
                refused.Add(element.Id);
            }
        }
    }
}
