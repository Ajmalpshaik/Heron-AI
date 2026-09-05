// NOT STANDALONE. Assumes `doc` and `templateName` are in scope; leaves
// `copyId`, `copyName` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// View.Duplicate CANNOT COPY A TEMPLATE, AND THE GUARD LIES TOO.
// CanViewBeDuplicated returns FALSE for every template on every option, so the
// obvious implementation reports "cannot be duplicated" and does nothing for
// ever. Removing the guard does not help - Duplicate then throws.
// ElementTransformUtils.CopyElements does work, verified on a real template
// which came across carrying its scale and detail level.
//
// THIS DOES NOT NAME THE COPY, AND GOLDEN RULE 16 IS WHY. Template names must
// be unique, so Revit auto-suffixes - 'Mechanical Plan' becomes 'Mechanical
// Plan1' - and that name is only readable once the copy has COMMITTED. A
// fragment that never opens a transaction cannot rename it in the same breath,
// so it reports the name Revit gave and RENAME_ELEMENTS does the rest.

var copyId = ElementId.InvalidElementId;
var copyName = "";
var findings = new List<string>();

var wanted = string.IsNullOrEmpty(templateName) ? "" : templateName.Trim();

if (wanted.Length == 0)
{
    findings.Add("No template was named - name the view template to copy");
}
else
{
    View source = null;
    var available = new List<string>();

    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
    {
        if (!candidate.IsTemplate) continue;
        available.Add(candidate.Name);
        if (source == null && string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
            source = candidate;
    }

    if (source == null)
    {
        findings.Add(string.Format("No view template is called '{0}'. This model has: {1}",
            wanted, available.Count == 0 ? "none" : string.Join(", ", available.ToArray())));
    }
    else
    {
        try
        {
            var toCopy = new List<ElementId>();
            toCopy.Add(source.Id);

            var made = ElementTransformUtils.CopyElements(doc, toCopy, XYZ.Zero);

            if (made == null || made.Count == 0)
            {
                findings.Add(string.Format("Copying '{0}' returned nothing. Nothing was created",
                    source.Name));
            }
            else
            {
                foreach (var id in made) { copyId = id; break; }

                var copy = doc.GetElement(copyId) as View;
                copyName = copy == null ? "" : copy.Name;

                findings.Add(string.Format("'{0}' was copied. Revit named the copy '{1}' (id {2}) - "
                    + "template names must be unique, so it adds a suffix of its own",
                    source.Name,
                    string.IsNullOrEmpty(copyName) ? "(name not readable yet)" : copyName,
                    copyId));

                findings.Add("THE COPY IS NOT RENAMED HERE. Its final name is only settled once this "
                    + "transaction commits, and this fragment does not open one - so renaming it is a "
                    + "SEPARATE call to RENAME_ELEMENTS, after the commit, using the id above");

                if (copy != null && !copy.IsTemplate)
                    findings.Add("What came back is NOT a template. That should not happen through this "
                        + "route, and the copy should be looked at before it is used");
            }
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("Copying '{0}' failed: {1}. View.Duplicate is not an "
                + "alternative here - it cannot copy a template at all", source.Name, ex.Message));
        }
    }
}
