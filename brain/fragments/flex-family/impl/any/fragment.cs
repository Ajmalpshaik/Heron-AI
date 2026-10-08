// NOT STANDALONE. Assumes `doc` and `trials` are in scope; leaves `allHeld`,
// `trialResults`, `restored`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THIS WRITES, AND IS DECLARED MODIFY FOR THAT REASON. A read may not change a
// value and roll it back to measure something - Golden Rule 16 and the recipe
// both say so - and this changes values on purpose. It puts every one back,
// reads them back, and measures the solid against how it started; a family
// that does not come back exactly THROWS, and the host rolls the whole call
// back.
//
// THE SOLID IS MEASURED, NOT THE PLANES. Planes moving prove the dimensions
// work, not that the geometry followed - reported from an earlier family
// build. Each trial reads EVERY solid form's own extent - a neck that moves
// inside a body that does not is invisible in their union - compares every
// labelled dimension with its parameter, and every connector size tied to a
// parameter with that parameter. Labelled planes that moved while no solid
// did is named for what it is: geometry not locked to them. A dimension that
// cannot reach its parameter is a size the family cannot take, and it THROWS
// naming the trial, because at commit it would be lost or kept silently.
//
// EACH TRIAL STARTS FROM THE ORIGINAL VALUES plus only what it names, so the
// trials do not leak into one another.
//
// A SOLID IS FOLLOWED BY ITS OWN ID - except an ARRAY'S MEMBER THAT REVIT
// REMADE (version 2, 5b-373). When an array's count changes, Revit remakes its
// copies and their ids change with it, so a flex through a labelled count
// came back with the same solids under new ids and was called "not back as it
// was". Every solid whose id survived is still compared with itself; only the
// array members whose ids did not survive are compared by shape - the same
// boxes, matched one to one, at the same tolerance - and the findings say how
// many were judged each way. A solid that is no array's member is never
// matched by shape: its id changing is itself a change.

var findings = new List<string>();
var allHeld = false;
var trialResults = new List<string>();
var restored = false;
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
var tolerance = 0.01 / 304.8;

// TWO KINDS ARE THE SAME KIND WHATEVER VERSION THEIR IDS CARRY. A ForgeTypeId
// names its version - "...:length-2.0.0" - and plain equality may compare it,
// so a length a template made under an older version would read as some other
// kind. NameEquals compares the name alone, from 2021; before that a kind is
// an enum and plain equality is exact.
Func<object, object, bool> sameKind = (first, second) =>
{
    if (first == null || second == null) return false;
    var nameEquals = first.GetType().GetMethod("NameEquals", new[] { second.GetType() });
    if (nameEquals != null)
    {
        try { return (bool)nameEquals.Invoke(first, new[] { second }); }
        catch (Exception) { }
    }
    return first.Equals(second);
};

Func<string, string, object> specNamed = (owner, property) =>
{
    var type = revitAssembly.GetType(dbNamespace + "." + owner);
    var found = type == null ? null : type.GetProperty(property, flags);
    return found == null ? null : found.GetValue(null);
};

var kindsNew = new List<KeyValuePair<string, object>>
{
    new KeyValuePair<string, object>("length",  specNamed("SpecTypeId", "Length")),
    new KeyValuePair<string, object>("angle",   specNamed("SpecTypeId", "Angle")),
    new KeyValuePair<string, object>("number",  specNamed("SpecTypeId", "Number")),
    new KeyValuePair<string, object>("integer", specNamed("SpecTypeId+Int", "Integer")),
    new KeyValuePair<string, object>("yesno",   specNamed("SpecTypeId+Boolean", "YesNo")),
};
var kindsOld = new Dictionary<string, string>
{
    { "Length", "length" }, { "Angle", "angle" }, { "Number", "number" },
    { "Integer", "integer" }, { "YesNo", "yesno" },
};

Func<FamilyParameter, string> kindOf = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            if (spec == null) return "unknown";
            foreach (var entry in kindsNew)
                if (sameKind(entry.Value, spec)) return entry.Key;
            // A ForgeTypeId prints as its CLASS name, so the id string is shown.
            var typeId = spec.GetType().GetProperty("TypeId");
            var id = typeId == null ? null : typeId.GetValue(spec) as string;
            return string.IsNullOrEmpty(id) ? "an unnamed kind" : id;
        }
        var property = definition.GetType().GetProperty("ParameterType");
        var old = property == null ? null : property.GetValue(definition);
        if (old == null) return "unknown";
        string word;
        return kindsOld.TryGetValue(old.ToString(), out word) ? word : old.ToString();
    }
    catch (Exception)
    {
        return "unknown";
    }
};

// ---------------------------------------------------------------------------
// READ THE TRIALS, AND CHECK THEM ALL BEFORE THE FIRST WRITE
// ---------------------------------------------------------------------------

// Each trial: (parameter, value in Revit's own unit, as typed).
var planned = new List<List<Tuple<FamilyParameter, object, string>>>();
var touched = new Dictionary<string, FamilyParameter>();
var originals = new Dictionary<string, object>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor, so there is "
        + "nothing to flex. Open the family first.";
}
else if (doc.FamilyManager.CurrentType == null)
{
    refused = "This family has no type yet, so there are no values to flex from or put back. "
        + "SET_FAMILY_TYPE_VALUES makes the first type.";
}
else if (string.IsNullOrEmpty(trials) || trials.Trim().Length == 0)
{
    refused = "No sizes were given to try - \"Width=900; Depth=500 | Width=400; Depth=700\", a pipe "
        + "between trials.";
}
else
{
    var fm = doc.FamilyManager;
    var current = fm.CurrentType;
    var problems = new List<string>();
    var pieces = trials.Split('|').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();

    if (pieces.Count > 10)
        problems.Add(pieces.Count + " trials were given. Ten is the most one flex tries - each one "
            + "regenerates the whole family.");

    foreach (var piece in pieces)
    {
        var trial = new List<Tuple<FamilyParameter, object, string>>();

        foreach (var entry in piece.Split(';').Select(e => e.Trim()).Where(e => e.Length > 0))
        {
            var split = entry.IndexOf('=');
            if (split <= 0) { problems.Add("\"" + entry + "\" is not name=value."); continue; }

            var name = entry.Substring(0, split).Trim();
            var text = entry.Substring(split + 1).Trim();
            var p = fm.get_Parameter(name);

            if (p == null) { problems.Add("\"" + name + "\" is not a parameter of this family."); continue; }
            if (p.IsDeterminedByFormula) { problems.Add("\"" + name + "\" is driven by the formula \"" + p.Formula + "\" and cannot be flexed directly - flex what the formula reads."); continue; }
            if (p.IsReporting) { problems.Add("\"" + name + "\" is a reporting parameter - it is read off the geometry, not flexed."); continue; }

            var kindWord = kindOf(p);
            double number;
            var isNumber = double.TryParse(text, System.Globalization.NumberStyles.Float, invariant, out number)
            && !double.IsNaN(number) && !double.IsInfinity(number);
            object value = null;

            if (kindWord == "length" && isNumber) value = number / 304.8;
            else if (kindWord == "angle" && isNumber) value = number * Math.PI / 180.0;
            else if (kindWord == "number" && isNumber) value = number;
            else if (kindWord == "integer" && isNumber && Math.Abs(number - Math.Round(number)) < 1e-9) value = (int)Math.Round(number);
            else if (kindWord == "yesno")
            {
                var lowered = text.ToLowerInvariant();
                if (lowered == "true" || lowered == "yes" || lowered == "1") value = 1;
                else if (lowered == "false" || lowered == "no" || lowered == "0") value = 0;
            }

            if (value == null)
            {
                problems.Add("\"" + text + "\" cannot be tried for \"" + name + "\" (" + kindWord + ") - a "
                    + "length is millimetres, digits only; an angle degrees; a yes/no true or false.");
                continue;
            }

            // THE ORIGINAL, taken once, in the unit Revit holds it in.
            if (!touched.ContainsKey(name))
            {
                if (!current.HasValue(p)) { problems.Add("\"" + name + "\" has no value in type \"" + current.Name + "\", so there would be nothing to put back."); continue; }
                touched[name] = p;
                originals[name] = value is double ? (object)current.AsDouble(p).Value : (object)current.AsInteger(p).Value;
            }

            trial.Add(Tuple.Create(p, value, name + "=" + text));
        }

        if (trial.Count > 0) planned.Add(trial);
    }

    if (problems.Count > 0) refused = "Nothing was tried. " + string.Join(" ", problems);
    else if (planned.Count == 0) refused = "No sizes were given to try.";
}

// ---------------------------------------------------------------------------
// FLEX
// ---------------------------------------------------------------------------

if (refused == null)
{
    var fm = doc.FamilyManager;

    // WHAT THE FAMILY READS NOW, as one snapshot:
    //   Item1  the union of every solid's extent - what the report shows
    //   Item2  each solid form's OWN extent, by its unique id - what is judged.
    //          A neck that moves inside a body that does not is invisible in
    //          the union, and a flex judged on the union would call a
    //          correctly locked neck "not locked"
    //   Item3  labelled dimensions that disagree with their parameter
    //   Item4  tied connector sizes that disagree with their parameter
    //   Item5  how many labels and ties were checked
    //   Item6  each labelled dimension's reading, to see whether planes moved
    //   Item7  the unique ids of every array's members, and of the forms in
    //          them - every member, the original too, is a model group (5b-358)
    Func<Tuple<double[], Dictionary<string, double[]>, List<string>, List<string>, int, Dictionary<string, double>, HashSet<string>>> measure = () =>
    {
        double[] union = null;
        var boxes = new Dictionary<string, double[]>();
        var everything = new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements();
        var arrayed = new HashSet<string>();
        foreach (var array in everything.OfType<BaseArray>())
        {
            var memberIds = new List<ElementId>();
            try { memberIds.AddRange(array.GetOriginalMemberIds()); } catch (Exception) { }
            try { memberIds.AddRange(array.GetCopiedMemberIds()); } catch (Exception) { }
            foreach (var id in memberIds)
            {
                var member = doc.GetElement(id);
                if (member == null) continue;
                arrayed.Add(member.UniqueId);
                var group = member as Group;
                if (group == null) continue;
                foreach (var inner in group.GetMemberIds())
                {
                    var held = doc.GetElement(inner);
                    if (held != null) arrayed.Add(held.UniqueId);
                }
            }
        }
        var forms = everything.OfType<GenericForm>().Where(f => f.IsSolid).ToList();
        foreach (var form in forms)
        {
            var bounds = form.get_BoundingBox(null);
            if (bounds == null) continue;
            var own = new[] { bounds.Min.X, bounds.Min.Y, bounds.Min.Z, bounds.Max.X, bounds.Max.Y, bounds.Max.Z };
            boxes[form.UniqueId] = own;
            if (union == null) union = (double[])own.Clone();
            else
            {
                for (var k = 0; k < 3; k++) union[k] = Math.Min(union[k], own[k]);
                for (var k = 3; k < 6; k++) union[k] = Math.Max(union[k], own[k]);
            }
        }

        var dimensionMisses = new List<string>();
        var portMisses = new List<string>();
        var checks = 0;
        var labels = new Dictionary<string, double>();
        var type = fm.CurrentType;

        foreach (var dimension in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
        {
            FamilyParameter label = null;
            try { label = dimension.FamilyLabel; } catch (Exception) { }
            if (label == null) continue;
            var reads = dimension.Value;
            var holds = type.AsDouble(label);
            if (!reads.HasValue || !holds.HasValue) continue;
            checks++;
            labels[dimension.UniqueId] = reads.Value;
            if (Math.Abs(reads.Value - holds.Value) > tolerance)
                dimensionMisses.Add("the dimension labelled " + label.Definition.Name + " reads " + mm(reads.Value)
                    + " mm and the parameter holds " + mm(holds.Value) + " mm");
        }

        var sized = new[]
        {
            Tuple.Create(BuiltInParameter.CONNECTOR_WIDTH, "width"),
            Tuple.Create(BuiltInParameter.CONNECTOR_HEIGHT, "height"),
            Tuple.Create(BuiltInParameter.CONNECTOR_DIAMETER, "diameter"),
        };
        foreach (var port in new FilteredElementCollector(doc).OfClass(typeof(ConnectorElement)).Cast<ConnectorElement>())
        {
            foreach (var tie in sized)
            {
                var ownParameter = port.get_Parameter(tie.Item1);
                if (ownParameter == null) continue;
                FamilyParameter driver = null;
                try { driver = fm.GetAssociatedFamilyParameter(ownParameter); } catch (Exception) { }
                if (driver == null) continue;
                var holds = type.AsDouble(driver);
                if (!holds.HasValue) continue;
                checks++;
                var reads = ownParameter.AsDouble();
                if (Math.Abs(reads - holds.Value) > tolerance)
                    portMisses.Add("a connector's " + tie.Item2 + " reads " + mm(reads) + " mm and "
                        + driver.Definition.Name + " holds " + mm(holds.Value) + " mm");
            }
        }

        return Tuple.Create(union, boxes, dimensionMisses, portMisses, checks, labels, arrayed);
    };

    Func<double[], string> describe = box => box == null
        ? "no solid geometry"
        : "solid " + mm(box[3] - box[0]) + " x " + mm(box[4] - box[1]) + " x " + mm(box[5] - box[2])
          + " mm, centre X " + mm((box[0] + box[3]) / 2) + " Y " + mm((box[1] + box[4]) / 2)
          + ", base Z " + mm(box[2]);

    Func<double[], double[], bool> sameBox = (first, second) =>
    {
        if (first == null || second == null) return first == null && second == null;
        for (var i = 0; i < 6; i++) if (Math.Abs(first[i] - second[i]) > 1e-6) return false;
        return true;
    };

    // Every solid where it was, form by form - and none appearing or vanishing.
    // Answers: the same or not; how many were compared by their own id; how
    // many array members Revit remade were matched by shape; and, when not the
    // same, what differed in words.
    Func<Tuple<double[], Dictionary<string, double[]>, List<string>, List<string>, int, Dictionary<string, double>, HashSet<string>>,
         Tuple<double[], Dictionary<string, double[]>, List<string>, List<string>, int, Dictionary<string, double>, HashSet<string>>,
         Tuple<bool, int, int, string>> compareForms = (before, after) =>
    {
        var first = before.Item2;
        var second = after.Item2;
        if (first.Count != second.Count)
            return Tuple.Create(false, 0, 0, second.Count + " solid(s) where there were " + first.Count);

        var byId = 0;
        foreach (var entry in first)
        {
            if (!second.ContainsKey(entry.Key)) continue;
            if (!sameBox(entry.Value, second[entry.Key]))
                return Tuple.Create(false, byId, 0, "the solid " + entry.Key + " is not where it was");
            byId++;
        }

        // THE IDS THAT DID NOT SURVIVE - matched by shape only when every one,
        // before and after, is an array's member.
        var gone = first.Keys.Where(k => !second.ContainsKey(k)).ToList();
        var made = second.Keys.Where(k => !first.ContainsKey(k)).ToList();
        if (gone.Count == 0) return Tuple.Create(true, byId, 0, "");
        var loose = gone.Where(k => !before.Item7.Contains(k)).Concat(made.Where(k => !after.Item7.Contains(k))).ToList();
        if (loose.Count > 0)
            return Tuple.Create(false, byId, 0, loose.Count + " solid(s) that are no array's member came back under a "
                + "different id - " + string.Join(", ", loose.Take(3)) + (loose.Count > 3 ? ", ..." : ""));

        var unmatched = made.Select(k => second[k]).ToList();
        foreach (var key in gone)
        {
            var match = unmatched.FirstOrDefault(box => sameBox(first[key], box));
            if (match == null)
                return Tuple.Create(false, byId, 0, "an array member Revit remade is not where one was: "
                    + describe(first[key]) + " has no match among the " + made.Count + " remade");
            unmatched.Remove(match);
        }
        return Tuple.Create(true, byId, gone.Count, "");
    };

    // Writes every touched parameter: the trial's value where it names one,
    // the original everywhere else.
    Action<List<Tuple<FamilyParameter, object, string>>> apply = trial =>
    {
        foreach (var entry in touched)
        {
            var named = trial == null ? null : trial.FirstOrDefault(t => t.Item1.Id == entry.Value.Id);
            var value = named != null ? named.Item2 : originals[entry.Key];
            if (value is double) fm.Set(entry.Value, (double)value);
            else fm.Set(entry.Value, (int)value);
        }
    };

    var start = measure();
    var every = true;
    var number = 0;

    foreach (var trial in planned)
    {
        number++;
        var said = string.Join(", ", trial.Select(t => t.Item3));

        try
        {
            apply(trial);
            doc.Regenerate();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Trial " + number + " (" + said + ") broke the family: "
                + ex.Message + " That size is one the family cannot take. NOTHING from this flex was kept. "
                + (trialResults.Count > 0 ? "Before it: " + string.Join(" / ", trialResults) : ""));
        }

        var now = measure();

        // A LABELLED DIMENSION THAT DID NOT REACH ITS PARAMETER is a size the
        // constraints cannot take. Revit may post it as a warning the host
        // dismisses, or as an error that rolls everything back at commit and
        // loses which trial it was - so it is thrown HERE, naming the trial.
        if (now.Item3.Count > 0)
            throw new InvalidOperationException("Trial " + number + " (" + said + ") could not be solved: "
                + string.Join("; ", now.Item3) + ". That size is one the family cannot take. NOTHING from "
                + "this flex was kept. " + (trialResults.Count > 0 ? "Before it: " + string.Join(" / ", trialResults) : ""));

        var misses = new List<string>(now.Item4);
        var formsMoved = !compareForms(start, now).Item1;

        // PLANES MOVED AND NO SOLID DID: the failure a flex exists for.
        var planesMoved = now.Item6.Any(l => start.Item6.ContainsKey(l.Key)
                                          && Math.Abs(start.Item6[l.Key] - l.Value) > tolerance);
        if (planesMoved && !formsMoved)
            misses.Add("the labelled planes moved and no solid did - the geometry is not locked to them");

        if (now.Item5 == 0 && !formsMoved)
            misses.Add("nothing in this family is driven by these parameters - no labelled dimension, no "
                + "tied connector, and no solid moved");

        if (misses.Count > 0) every = false;

        trialResults.Add("Trial " + number + " (" + said + "): " + describe(now.Item1) + " across "
            + now.Item2.Count + " solid(s); " + (now.Item5 - now.Item4.Count) + " of " + now.Item5
            + " driven sizes read their parameters"
            + (misses.Count > 0 ? " - NOT HELD: " + string.Join("; ", misses) : ""));
    }

    // PUT EVERYTHING BACK, and prove it - every solid, not only their union.
    try
    {
        apply(null);
        doc.Regenerate();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Putting the original values back was refused: " + ex.Message
            + " NOTHING from this flex was kept. Trials: " + string.Join(" / ", trialResults));
    }

    var end = measure();
    var valuesBack = touched.All(entry =>
    {
        var original = originals[entry.Key];
        if (original is double)
        {
            var now = fm.CurrentType.AsDouble(entry.Value);
            return now.HasValue && Math.Abs(now.Value - (double)original) < 1e-9;
        }
        var whole = fm.CurrentType.AsInteger(entry.Value);
        return whole.HasValue && whole.Value == (int)original;
    });

    var back = compareForms(start, end);
    restored = valuesBack && back.Item1;

    if (!restored)
        throw new InvalidOperationException("After the flex the family did not come back as it was - "
            + (valuesBack ? "" : "a value did not return; ") + (back.Item1 ? "" : back.Item4 + "; ")
            + "it started as " + describe(start.Item1) + " and ended as " + describe(end.Item1)
            + " (every solid is compared, not only the whole). "
            + "NOTHING from this flex was kept. Trials: " + string.Join(" / ", trialResults));

    allHeld = every && start.Item5 > 0;

    findings.Add("Flexed " + planned.Count + " size(s) and put the family back: " + describe(end.Item1)
        + ", every solid where it started"
        + (back.Item3 > 0
            ? " - " + back.Item2 + " compared by their own ids, and " + back.Item3 + " array member(s) Revit remade "
              + "with new ids when the count changed compared by shape: the same boxes, matched one to one"
            : ", each compared by its own id")
        + ", with every value read back.");
    findings.AddRange(trialResults);
    findings.Add(allHeld
        ? "Every labelled dimension and every tied connector read its parameter in every trial, and the "
          + "solids moved with their planes. Compare the sizes above with the sizes tried - that "
          + "comparison is the modeller's, and a square trial alone hides a width and depth swapped."
        : "NOT EVERYTHING HELD - see the trials marked NOT HELD. The family is back as it was.");
}

if (refused != null) findings.Add(refused);
