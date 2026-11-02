// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves
// `justifications`, `notFraming` and `findings` behind.
//
// READS ONLY. Opens no transaction and changes nothing.
//
// EVERY JUSTIFICATION VALUE OF A BEAM OR BRACE, IN PROPERTIES' WORDS, ONE LINE
// PER MEMBER: yz Justification; then, for a Uniform member, its y and z
// Justification and y and z Offset Value, and for an Independent one the Start
// and End of each; then its Start and End Level Offset. A choice is given as
// the word Revit shows for it (AsValueString), never as the number it stores;
// lengths in millimetres, feet * 304.8 (D-20). A parameter the member does not
// carry is said to be absent, never read as 0.

var justifications = new List<string>();
var notFraming = new List<string>();
var findings = new List<string>();

Func<Element, string> describe = e => string.Format("{0} '{1}' (id {2})",
    e.Category != null ? e.Category.Name : "element", e.Name, e.Id);
Func<double, string> mm = v => Math.Round(v, 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " mm";

Func<Element, BuiltInParameter, string, string> read = (e, bip, label) =>
{
    var p = e.get_Parameter(bip);
    if (p == null) return label + " absent";
    if (p.StorageType == StorageType.Double) return label + " " + mm(p.AsDouble() * 304.8);
    if (p.StorageType == StorageType.Integer) return label + " " + (p.AsValueString() ?? p.AsInteger().ToString());
    return label + " " + (p.AsValueString() ?? "");
};

var framingCategory = new ElementId(BuiltInCategory.OST_StructuralFraming);

if (elements != null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (element.Category == null || element.Category.Id != framingCategory || !(element is FamilyInstance))
        {
            notFraming.Add(describe(element) + ": not structural framing - it has no yz justification");
            continue;
        }
        var parts = new List<string>();
        parts.Add(read(element, BuiltInParameter.YZ_JUSTIFICATION, "yz Justification"));
        var yz = element.get_Parameter(BuiltInParameter.YZ_JUSTIFICATION);
        var independent = yz != null && yz.AsInteger() == 1;
        if (!independent)
        {
            parts.Add(read(element, BuiltInParameter.Y_JUSTIFICATION, "y Justification"));
            parts.Add(read(element, BuiltInParameter.Z_JUSTIFICATION, "z Justification"));
            parts.Add(read(element, BuiltInParameter.Y_OFFSET_VALUE, "y Offset Value"));
            parts.Add(read(element, BuiltInParameter.Z_OFFSET_VALUE, "z Offset Value"));
        }
        else
        {
            parts.Add(read(element, BuiltInParameter.START_Y_JUSTIFICATION, "Start y Justification"));
            parts.Add(read(element, BuiltInParameter.END_Y_JUSTIFICATION, "End y Justification"));
            parts.Add(read(element, BuiltInParameter.START_Z_JUSTIFICATION, "Start z Justification"));
            parts.Add(read(element, BuiltInParameter.END_Z_JUSTIFICATION, "End z Justification"));
            parts.Add(read(element, BuiltInParameter.START_Y_OFFSET_VALUE, "Start y Offset Value"));
            parts.Add(read(element, BuiltInParameter.END_Y_OFFSET_VALUE, "End y Offset Value"));
            parts.Add(read(element, BuiltInParameter.START_Z_OFFSET_VALUE, "Start z Offset Value"));
            parts.Add(read(element, BuiltInParameter.END_Z_OFFSET_VALUE, "End z Offset Value"));
        }
        parts.Add(read(element, BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION, "Start Level Offset"));
        parts.Add(read(element, BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION, "End Level Offset"));
        justifications.Add(describe(element) + ": " + string.Join("; ", parts));
    }
}

findings.Add(string.Format("{0} beam(s) or brace(s) read; {1} handed in that are not structural framing",
    justifications.Count, notFraming.Count));
findings.AddRange(justifications);
findings.AddRange(notFraming);
