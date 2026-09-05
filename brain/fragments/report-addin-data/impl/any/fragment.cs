// NOT STANDALONE. Assumes `doc`, `countCarriers` and `showFields` are in scope;
// leaves `findings`, `schemaNames`, `schemasRegistered`, `schemasUsedHere` and
// `carriersFound` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EXTENSIBLE STORAGE IS THE FOURTH PLACE DATA HIDES. Parameters, project
// information and element properties are all visible in Revit's interface. A
// schema attached by an add-in is not - it is inside the .rvt with nothing on
// screen showing it, and REPORT_PARAMETER_INVENTORY would list none of it.
//
// A CLOSED SCHEMA IS OBEYED, NOT WORKED AROUND. ReadAccessGranted() false means
// the vendor signed their data shut. Name, vendor, GUID and carrier count are
// still visible; the FIELDS are not, and that is reported as a refusal rather
// than as an empty list.
//
// SCHEMAS ARE SESSION-WIDE, CARRIERS ARE PER-MODEL. ListSchemas() returns what
// every loaded add-in has registered in this Revit session. Zero carriers means
// the add-in is installed; carriers mean this model has been written to. Both
// are listed, each labelled, because dropping one hides the difference.
//
// THE EXTENSIBLE STORAGE NAMESPACE IS DECLARED IN THE HARNESS, NOT WRITTEN OUT
// HERE. A fragment may use only what the harness says it has in scope - a
// fully-qualified name would smuggle in a namespace the executor was never told
// to supply, which compiles green off the machine and is missing at the PC.
// tools/check-structure.py is what enforces that, and it caught this fragment.
//
// DataStorage IS NEVER NAMED. The document-wide invisible carrier is that type,
// and it is not publicly accessible - naming it does not compile on any release
// here. It is matched on GetType().Name and described in words.

var findings = new List<string>();
var schemaNames = new List<string>();
var schemasRegistered = 0;
var schemasUsedHere = 0;
var carriersFound = 0;

IList<Schema> schemas = null;
try
{
    schemas = Schema.ListSchemas();
}
catch (Exception ex)
{
    findings.Add("The schema list could not be read at all: " + ex.Message
        + ". That is not the same as a model with no add-in data - nothing was checked");
}

if (schemas == null || schemas.Count == 0)
{
    if (findings.Count == 0)
    {
        findings.Add("No extensible storage schema is registered in this Revit session, so no add-in "
            + "has left data of this kind anywhere. Note this is a fact about the SESSION: a model "
            + "written by an add-in that is not installed here would still show nothing");
    }
}
else
{
    schemasRegistered = schemas.Count;

    foreach (var schema in schemas)
    {
        var name = "(unnamed)";
        var vendor = "(unknown)";
        var guidText = "(unreadable)";
        var authorNote = "";
        try { name = schema.SchemaName; } catch (Exception) { }
        try { vendor = schema.VendorId; } catch (Exception) { }
        try { guidText = schema.GUID.ToString(); } catch (Exception) { }
        try { authorNote = schema.Documentation ?? ""; } catch (Exception) { }

        var readLevel = "?";
        var writeLevel = "?";
        try { readLevel = schema.ReadAccessLevel.ToString(); } catch (Exception) { }
        try { writeLevel = schema.WriteAccessLevel.ToString(); } catch (Exception) { }

        var canRead = false;
        try { canRead = schema.ReadAccessGranted(); } catch (Exception) { }

        schemaNames.Add(name);
        findings.Add(string.Format("{0}  [vendor {1}]  read {2} / write {3}  {4}",
            name, vendor, readLevel, writeLevel,
            canRead ? "" : "- CLOSED: this vendor's fields stay hidden, and that is respected"));
        findings.Add("    id " + guidText);
        if (!string.IsNullOrEmpty(authorNote))
            findings.Add("    the author's own words: " + authorNote);

        if (showFields)
        {
            if (!canRead)
            {
                findings.Add("    fields: not shown - read access is not granted for this schema");
            }
            else
            {
                try
                {
                    var fields = schema.ListFields();
                    if (fields == null || fields.Count == 0)
                    {
                        findings.Add("    fields: none");
                    }
                    else
                    {
                        foreach (var field in fields)
                        {
                            var fieldName = "?";
                            var fieldType = "?";
                            try { fieldName = field.FieldName; } catch (Exception) { }
                            try { fieldType = field.ValueType == null ? "?" : field.ValueType.Name; }
                            catch (Exception) { }
                            findings.Add("    field  " + fieldName + " : " + fieldType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    findings.Add("    fields: could not be listed - " + ex.Message);
                }
            }
        }

        if (countCarriers)
        {
            try
            {
                var carriers = new FilteredElementCollector(doc)
                    .WherePasses(new ExtensibleStorageFilter(schema.GUID))
                    .ToElements();

                if (carriers.Count == 0)
                {
                    findings.Add("    carriers in THIS model: none. The schema is registered by an "
                        + "installed add-in and holds nothing here");
                }
                else
                {
                    schemasUsedHere++;
                    carriersFound += carriers.Count;

                    var perBucket = new Dictionary<string, int>();
                    var order = new List<string>();
                    foreach (var carrier in carriers)
                    {
                        if (carrier == null) continue;

                        // DataStorage is not publicly accessible, so it is matched by NAME. It is
                        // the document-wide record - not selectable, no category - and a
                        // category-only grouping would file it under "(no category)" and teach
                        // nobody what it is.
                        var bucket = carrier.GetType().Name == "DataStorage"
                            ? "a document-wide record, not selectable in the model"
                            : (carrier.Category != null
                                ? carrier.Category.Name
                                : carrier.GetType().Name);

                        if (!perBucket.ContainsKey(bucket))
                        {
                            perBucket[bucket] = 0;
                            order.Add(bucket);
                        }
                        perBucket[bucket] = perBucket[bucket] + 1;
                    }

                    findings.Add(string.Format("    carriers in THIS model: {0}", carriers.Count));
                    foreach (var bucket in order)
                        findings.Add(string.Format("      {0}: {1}", bucket, perBucket[bucket]));
                }
            }
            catch (Exception ex)
            {
                findings.Add("    carriers: could not be counted - " + ex.Message
                    + ". The schema is still real; the count is the part that failed");
            }
        }
    }
}

findings.Insert(0, string.Format("{0} schema(s) registered in this Revit session{1}. Extensible "
    + "storage is data an add-in wrote INTO the file, and none of it appears anywhere in Revit's own "
    + "interface. Nothing was written, changed or cleared",
    schemasRegistered,
    countCarriers
        ? string.Format("; {0} of them hold data in THIS model, on {1} element(s)",
            schemasUsedHere, carriersFound)
        : "; carriers were NOT counted, so nothing here says which of them touch THIS model"));
