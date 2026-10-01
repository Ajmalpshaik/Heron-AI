// NOT STANDALONE. Assumes `doc`, `uidoc` and `forms` are in scope; leaves
// `combinationId`, `combined`, `cut`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// JOIN SOLIDS, OR CUT A SOLID WITH A VOID, in the family open in the Family
// Editor - Revit's Join Geometry and Cut Geometry there - by combining the
// forms named into ONE geometry combination. Solids in it are joined; a void in
// it cuts them.
//
// THIS IS THE ONLY ROUTE THE API HAS IN AN ORDINARY FAMILY, and its own remarks
// say why, 2020 to 2027: the solid-cut utility refuses any document that is not
// a project, a conceptual mass, a curtain panel or an adaptive component, and
// the join utility says it "is not available for family documents". A geometry
// combination is "created by Join and Cut operations ... in a family
// document", and Document.CombineElements makes one.
//
// A VOID MADE THROUGH THE API CUTS NOTHING UNTIL THIS. A void drawn by hand
// cuts what it touches when its sketch is finished; one made by the API does
// not - which is what an earlier family build measured five ways and called a
// void that never cut.
//
// THE CUT IS MEASURED, NOT ASSUMED. The solids' volume is read before, and the
// combination's after: a void that took nothing away fails the call, and so
// does a join that came out bigger than its parts. Nothing is kept from a call
// that fails.
//
// A FORM ALREADY IN A COMBINATION IS NAMED BY ITS COMBINATION. Revit's remarks:
// a solid belongs to at most one, so naming a member of one would quietly
// merge two; it is refused with the combination's id instead. A VOID may cut
// several combinations - run this once per body to cut two bodies with one void
// and keep them apart.

var findings = new List<string>();
var combinationId = "";
var combined = "";
var cut = false;
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> litres = cubicFeet => Math.Round(cubicFeet * 28.316846592, 3).ToString(invariant);
// One cubic centimetre, in cubic feet - less than any cut worth making.
var crumb = 1.0 / (304.8 * 304.8 * 304.8) * 1000.0;

Func<Element, double> volumeOf = element =>
{
    var total = 0.0;
    var geometry = element.get_Geometry(new Options());
    if (geometry == null) return 0.0;
    foreach (GeometryObject piece in geometry)
    {
        var body = piece as Solid;
        if (body != null && body.Volume > 0) total += body.Volume;
    }
    return total;
};

Func<Element, string> describe = element =>
{
    var form = element as GenericForm;
    if (form == null) return "the combination " + element.UniqueId;
    var kind = element is Extrusion ? "extrusion" : element is Revolution ? "revolve" : element is Blend ? "blend"
        : element is SweptBlend ? "swept blend" : element is Sweep ? "sweep" : "form";
    return (form.IsSolid ? "solid " : "void ") + kind + " " + element.UniqueId;
};

var members = new List<Element>();
var problems = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. Forms are joined and "
        + "cut inside the family - open it first.";
}
else
{
    var said = (forms ?? "").Trim();
    var named = new List<Element>();
    if (string.Equals(said, "selected", StringComparison.OrdinalIgnoreCase)
        || string.Equals(said, "selection", StringComparison.OrdinalIgnoreCase))
    {
        if (uidoc == null || uidoc.Document == null || !uidoc.Document.Equals(doc))
            problems.Add("\"selected\" means what is selected in this family's window, and this family is not the "
                + "window in front. Bring it to the front, or name the forms by their ids.");
        else
            foreach (var id in uidoc.Selection.GetElementIds())
            {
                var element = doc.GetElement(id);
                if (element != null) named.Add(element);
            }
    }
    else
    {
        foreach (var part in said.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
        {
            var element = doc.GetElement(part);
            if (element == null) problems.Add("No element in this family has the id \"" + part + "\".");
            else named.Add(element);
        }
    }

    foreach (var element in named)
    {
        if (members.Any(m => m.Id == element.Id)) continue;
        var combinable = element as CombinableElement;
        if (combinable == null || !(element is GenericForm || element is GeomCombination))
        {
            problems.Add("\"" + (element.Name ?? element.UniqueId) + "\" (" + element.UniqueId + ") is not a form - "
                + "an extrusion, revolve, blend, sweep or swept blend - nor a join of them.");
            continue;
        }
        var form = element as GenericForm;
        if (form != null && form.IsSolid)
        {
            GeomCombination already = null;
            foreach (GeomCombination one in combinable.Combinations) { already = one; break; }
            if (already != null)
            {
                problems.Add("The " + describe(element) + " is already joined or cut in the combination "
                    + already.UniqueId + " - name that combination instead, so the two are not merged without "
                    + "being asked.");
                continue;
            }
        }
        members.Add(element);
    }

    if (problems.Count == 0 && members.Count < 2)
        problems.Add((members.Count == 0 ? "Nothing was named" : "Only one form was named") + " - joining or "
            + "cutting needs at least two forms. Name them by the ids the form tools gave back, commas between, or "
            + "select them and say \"selected\".");

    if (problems.Count == 0
        && members.All(m => m is GenericForm && !((GenericForm)m).IsSolid))
        problems.Add("Every form named is a void. A void cuts a solid - name the solid it should cut as well.");

    if (problems.Count > 0) refused = "Nothing was joined or cut. " + string.Join(" ", problems);
}

if (refused == null)
{
    var solids = members.Where(m => !(m is GenericForm) || ((GenericForm)m).IsSolid).ToList();
    var voids = members.Where(m => m is GenericForm && !((GenericForm)m).IsSolid).ToList();
    cut = voids.Count > 0;

    var before = solids.Sum(s => volumeOf(s));
    if (before <= 0)
        throw new InvalidOperationException("The solids named hold no volume to measure, so nothing could prove a "
            + "join or a cut. NOTHING from this call was kept.");

    GeomCombination combination;
    try
    {
        var array = new CombinableElementArray();
        foreach (var m in members) array.Append((CombinableElement)m);
        combination = doc.CombineElements(array);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to combine " + string.Join(", ", members.Select(describe))
            + ": " + ex.Message + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    if (combination == null)
        throw new InvalidOperationException("Revit combined nothing. NOTHING from this call was kept.");

    var after = volumeOf(combination);
    if (after <= 0)
        throw new InvalidOperationException("Revit made a combination with no solid in it to measure. NOTHING from "
            + "this call was kept.");

    if (cut && after > before - crumb)
        throw new InvalidOperationException("The solids measured " + litres(before) + " L before and the cut "
            + "combination " + litres(after) + " L - the void took nothing away. It does not pass through them, or "
            + "Revit did not cut with it. NOTHING from this call was kept.");
    if (!cut && after > before + crumb)
        throw new InvalidOperationException("The solids measured " + litres(before) + " L apart and " + litres(after)
            + " L joined - a join cannot add material. NOTHING from this call was kept.");

    combinationId = combination.UniqueId;
    combined = (cut ? "Cut " + solids.Count + " solid(s) with " + voids.Count + " void(s)"
                    : "Joined " + solids.Count + " solid(s)")
        + ": " + litres(before) + " L before, " + litres(after) + " L after"
        + (cut ? " - " + litres(before - after) + " L taken away." : (before - after > crumb
            ? " - " + litres(before - after) + " L where they overlapped, now counted once." : " - they only touched."));

    findings.Add(combined + " The combination's id is " + combinationId + ".");
    findings.Add("Combined: " + string.Join(", ", members.Select(describe)) + ".");
    findings.Add("Undo, or Revit's Unjoin Geometry and Uncut Geometry, take it apart again; the forms themselves "
        + "are unchanged.");
}

if (refused != null) findings.Add(refused);
