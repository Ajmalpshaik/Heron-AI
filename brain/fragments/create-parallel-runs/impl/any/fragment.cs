// NOT STANDALONE. Assumes `doc`, `elements`, `count`, `spacingMm` and `side`
// are in scope; leaves `copies`, `perCopy`, `summary`, `notRemade` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// REVIT'S PARALLEL PIPES AND PARALLEL CONDUITS, WHICH THE API DOES NOT HAVE.
// There is no call for them, so this does what they do: it reads ONE connected
// run of pipes or of conduits, works out its centreline as a chain of corner
// points, offsets that chain once per copy, draws each copy's straight pieces
// on the original piece's own type, system, level and size, and re-makes the
// fitting at every joint between them - an elbow at a bend, a union or a
// transition where two pieces meet in line.
//
// WHY NOT A PLAIN COPY. COPY_ELEMENTS moves a copy by one vector. That is a
// parallel run only for a straight run, or for a copy straight up or down:
// copy an L-shaped run 150 mm east and its east-west leg lands ON ITS OWN
// LINE, inside the original. Here each piece moves at right angles to ITSELF
// and the corners are re-mitred, so the inner copies are shorter and the outer
// ones longer, as Revit's own tool draws them.
//
// THE SIDE. `side` is one of:
//   up, down                   - every corner moved straight up or down
//   left, right                - looking along the run from its START, at
//                                right angles to each piece in plan; left
//                                stays left round every bend
//   north, south, east, west   - left or right, whichever points that way on
//                                the run's first piece. North is the model's
//                                up-the-screen in a plan not turned to true
//                                north
// THE START is the run's west end - its south end when it runs exactly north
// to south, its lower end when it is a riser - and the summary names the point,
// so a copy on the wrong side is put right by saying the other word.
//
// NEVER GUESSED. `count` and `spacingMm` have no defaults. The spacing is
// centre to centre between one copy and the next, the first copy one spacing
// from the original, and it must be wider than the run's largest diameter or
// the copies would sit inside one another.
//
// A RUN, NOT A NETWORK. Every pipe or conduit handed in must form one chain,
// end to end, through fittings or straight onto each other. A tee or cross
// joining THREE handed-in pieces is a branch and is refused, naming the
// fitting; a tee at the edge of the selection, with only one handed-in piece on
// each side, is just a joint and is copied as one. Ducts are refused by name -
// this is the pipe and conduit tool - and so is anything else that is not a
// pipe, a conduit or their fitting: a valve in the run would be silently
// missing from every copy.
//
// THE ORIGINAL IS NEVER TOUCHED. Nothing is moved, resized, disconnected or
// rejoined on it. The copies' far ends are left OPEN - a copy of a run that
// ends on a pump or a box ends on nothing.
//
// REFUSED BEFORE, OR ALL OR NOTHING AFTER - ARTICLE 8. What can be known
// before drawing - the run, the values, a copy whose inside corner the spacing
// closes up - is refused in `refused` with nothing made. A piece or a fitting
// Revit will not make, or a size the type snaps to something else, THROWS,
// naming each with Revit's own words, and the caller's transaction takes the
// whole job back. A parallel run with one elbow missing looks drawn and is
// two runs.
//
// SLOPED RUNS keep the original's height at every corner, so a copy falls
// between the same heights as the run beside it; on the inside of a bend that
// is a slightly shorter piece and so a slightly steeper fall. Said, not hidden.
//
// MILLIMETRES IN, FEET INSIDE - divided by 304.8, once, here (D-20). No
// ElementId is ever read as a number.

const double mmPerFoot = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

var copies = new List<ElementId>();
string perCopy = null;
string summary = null;
var notRemade = new List<ElementId>();
string refused = null;

var problems = new List<string>();

// ONE PASS, LEFT EARLY ON A REFUSAL. A refusal BEFORE anything is drawn - a
// duct, a branch, a value not given, a spacing too tight - is said in
// `refused`, with nothing made and `copies` empty: there is nothing to take
// back. A failure AFTER something is drawn THROWS, so the caller's transaction
// takes back every piece already made (Article 8).
do
{
    // ---- 1. WHAT WAS HANDED IN ------------------------------------------------
    var pipeFittingCat = new ElementId(BuiltInCategory.OST_PipeFitting);
    var conduitFittingCat = new ElementId(BuiltInCategory.OST_ConduitFitting);
    var ductCats = new List<ElementId>
    {
        new ElementId(BuiltInCategory.OST_DuctCurves),
        new ElementId(BuiltInCategory.OST_DuctFitting),
        new ElementId(BuiltInCategory.OST_FlexDuctCurves),
    };

    var curves = new List<MEPCurve>();
    var curveIds = new HashSet<ElementId>();
    var fittingsGiven = new List<ElementId>();
    var ducts = new List<string>();
    var others = new List<string>();
    var pipes = 0;
    var conduits = 0;

    foreach (var element in elements ?? new List<Element>())
    {
        if (element == null) continue;
        var category = element.Category;
        if (category != null && ductCats.Contains(category.Id))
        {
            ducts.Add(element.Id.ToString());
            continue;
        }
        if (element is Pipe || element is Conduit)
        {
            if (curveIds.Add(element.Id))
            {
                curves.Add((MEPCurve)element);
                if (element is Pipe) pipes++; else conduits++;
            }
            continue;
        }
        if (element is FamilyInstance && category != null
            && (category.Id == pipeFittingCat || category.Id == conduitFittingCat))
        {
            fittingsGiven.Add(element.Id);
            continue;
        }
        others.Add((category == null ? "an element" : category.Name) + " " + element.Id.ToString());
    }

    if (ducts.Count > 0)
        problems.Add("ducts are not copied by this - it makes parallel PIPES and CONDUITS, as Revit's Parallel Pipes "
            + "and Parallel Conduits do, and Revit has no parallel ducts; the duct pieces handed in: "
            + string.Join(", ", ducts));
    if (others.Count > 0)
        problems.Add("only pipes, conduits and their fittings make a run here, and these are neither - a valve or "
            + "an accessory would be missing from every copy: " + string.Join(", ", others));
    if (pipes > 0 && conduits > 0)
        problems.Add("pipes and conduits were handed in together - copy one run at a time");
    if (curves.Count == 0 && ducts.Count == 0 && others.Count == 0)
        problems.Add("no pipe or conduit was handed in - select the run, its pipes or conduits and their fittings");

    // ---- 2. THE VALUES ASKED - never guessed -----------------------------------
    if (count < 1)
        problems.Add("count is " + count.ToString(invariant) + " - say how many copies, 1 or more");
    else if (count > 50)
        problems.Add("count is " + count.ToString(invariant) + " - more than 50 copies of one run in one go is "
            + "refused; say it in smaller batches");
    if (!(spacingMm > 0))
        problems.Add("spacingMm is " + spacingMm.ToString(invariant) + " - say the spacing centre to centre in "
            + "millimetres, greater than 0");

    var sideWord = (side ?? "").Trim().ToLowerInvariant();
    var vertical = sideWord == "up" || sideWord == "down" || sideWord == "above" || sideWord == "below";
    var compass = sideWord == "north" || sideWord == "south" || sideWord == "east" || sideWord == "west";
    var planSide = sideWord == "left" || sideWord == "right" || compass;
    if (!vertical && !planSide)
        problems.Add("side '" + side + "' is not a side this knows - say left, right, up, down, north, south, "
            + "east or west");

    if (problems.Count > 0)
    {
        refused = string.Format("{0} problem(s), so no copy was made and the run was not touched: {1}",
            problems.Count, string.Join(" | ", problems));
        break;
    }

    var drawn = pipes > 0 ? "pipe" : "conduit";
    var diameterParam = pipes > 0 ? BuiltInParameter.RBS_PIPE_DIAMETER_PARAM : BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM;

    // ---- 3. THE CHAIN - who joins whom, through connectors --------------------
    Func<Element, ConnectorSet> connectorsOf = owner =>
    {
        var curve = owner as MEPCurve;
        if (curve != null) return curve.ConnectorManager == null ? null : curve.ConnectorManager.Connectors;
        var instance = owner as FamilyInstance;
        if (instance != null && instance.MEPModel != null && instance.MEPModel.ConnectorManager != null)
            return instance.MEPModel.ConnectorManager.Connectors;
        return null;
    };

    var neighbours = new Dictionary<ElementId, List<ElementId>>();
    var jointPoint = new Dictionary<string, XYZ>();     // "a>b" -> a's connector origin toward b
    var fittingsUsed = new HashSet<ElementId>();
    var branches = new List<string>();

    foreach (var curve in curves)
    {
        var mine = new List<ElementId>();
        neighbours[curve.Id] = mine;
        var set = connectorsOf(curve);
        if (set == null) continue;

        foreach (Connector end in set)
        {
            if (end.ConnectorType != ConnectorType.End || !end.IsConnected) continue;
            foreach (Connector other in end.AllRefs)
            {
                var owner = other.Owner;
                if (owner == null || owner.Id == curve.Id) continue;

                if (curveIds.Contains(owner.Id))
                {
                    if (!mine.Contains(owner.Id)) mine.Add(owner.Id);
                    jointPoint[curve.Id.ToString() + ">" + owner.Id.ToString()] = end.Origin;
                    continue;
                }

                var fitting = owner as FamilyInstance;
                if (fitting == null || fitting.Category == null
                    || !(fitting.Category.Id == pipeFittingCat || fitting.Category.Id == conduitFittingCat)) continue;

                var fittingSet = connectorsOf(fitting);
                if (fittingSet == null) continue;
                var reached = new List<ElementId>();
                foreach (Connector port in fittingSet)
                {
                    if (!port.IsConnected) continue;
                    foreach (Connector beyond in port.AllRefs)
                    {
                        var far = beyond.Owner;
                        if (far == null || far.Id == fitting.Id || far.Id == curve.Id) continue;
                        if (curveIds.Contains(far.Id) && !reached.Contains(far.Id)) reached.Add(far.Id);
                    }
                }
                if (reached.Count > 1)
                {
                    var named = "fitting " + fitting.Id.ToString();
                    if (!branches.Contains(named)) branches.Add(named);
                }
                foreach (var far in reached)
                {
                    if (!mine.Contains(far)) mine.Add(far);
                    jointPoint[curve.Id.ToString() + ">" + far.ToString()] = end.Origin;
                    fittingsUsed.Add(fitting.Id);
                }
            }
        }
    }

    if (branches.Count > 0)
        problems.Add("the run branches at " + string.Join(", ", branches) + " - a tee or cross joins three of the "
            + "pieces handed in. Parallel copies are made of ONE run: select it up to the branch");

    var ends = new List<MEPCurve>();
    foreach (var curve in curves)
    {
        var degree = neighbours[curve.Id].Count;
        if (degree > 2 && branches.Count == 0)
            problems.Add(drawn + " " + curve.Id.ToString() + " meets " + degree.ToString(invariant)
                + " of the other pieces handed in - that is a branch, not one run");
        if (degree <= 1) ends.Add(curve);
        if (degree == 0 && curves.Count > 1)
            problems.Add(drawn + " " + curve.Id.ToString() + " is not joined to any other piece handed in - "
                + "select one connected run, end to end");
    }

    // Every piece must be a straight line.
    var lineOf = new Dictionary<ElementId, Line>();
    foreach (var curve in curves)
    {
        var location = curve.Location as LocationCurve;
        var line = location == null ? null : location.Curve as Line;
        if (line == null) problems.Add(drawn + " " + curve.Id.ToString() + " is not a straight piece");
        else lineOf[curve.Id] = line;
    }

    if (problems.Count == 0 && curves.Count > 1 && ends.Count != 2)
        problems.Add("the pieces handed in are not one run with two ends - " + ends.Count.ToString(invariant)
            + " piece(s) have a free end. Select one connected run, end to end");

    if (problems.Count > 0)
    {
        refused = string.Format("{0} problem(s), so no copy was made and the run was not touched: {1}",
            problems.Count, string.Join(" | ", problems));
        break;
    }

    // The point where a run-end piece is free: the end far from its neighbour.
    Func<MEPCurve, XYZ> freeEndOf = piece =>
    {
        var line = lineOf[piece.Id];
        var p0 = line.GetEndPoint(0);
        var p1 = line.GetEndPoint(1);
        if (neighbours[piece.Id].Count == 0) return null;
        var toward = jointPoint[piece.Id.ToString() + ">" + neighbours[piece.Id][0].ToString()];
        return p0.DistanceTo(toward) > p1.DistanceTo(toward) ? p0 : p1;
    };

    // West first, then south, then low - 1 mm decides "the same".
    var oneMm = 1.0 / mmPerFoot;
    Func<XYZ, XYZ, bool> comesFirst = (a, b) =>
    {
        if (Math.Abs(a.X - b.X) > oneMm) return a.X < b.X;
        if (Math.Abs(a.Y - b.Y) > oneMm) return a.Y < b.Y;
        return a.Z <= b.Z;
    };

    var order = new List<MEPCurve>();
    XYZ startPoint;
    if (curves.Count == 1)
    {
        order.Add(curves[0]);
        var only = lineOf[curves[0].Id];
        startPoint = comesFirst(only.GetEndPoint(0), only.GetEndPoint(1)) ? only.GetEndPoint(0) : only.GetEndPoint(1);
    }
    else
    {
        var endA = freeEndOf(ends[0]);
        var endB = freeEndOf(ends[1]);
        var first = comesFirst(endA, endB) ? ends[0] : ends[1];
        startPoint = first == ends[0] ? endA : endB;

        var byId = new Dictionary<ElementId, MEPCurve>();
        foreach (var curve in curves) byId[curve.Id] = curve;
        var seen = new HashSet<ElementId>();
        var at = first;
        while (at != null && seen.Add(at.Id))
        {
            order.Add(at);
            MEPCurve next = null;
            foreach (var id in neighbours[at.Id]) if (!seen.Contains(id)) { next = byId[id]; break; }
            at = next;
        }
        if (order.Count != curves.Count)
        {
            refused = "the pieces handed in are not one connected run - " + order.Count.ToString(invariant) + " of "
                + curves.Count.ToString(invariant) + " join up from its end, so no copy was made and the run was not touched";
            break;
        }
    }

    // Each piece pointed along the run: a = toward the start, b = toward the end.
    var n = order.Count;
    var aPt = new XYZ[n];
    var bPt = new XYZ[n];
    for (var i = 0; i < n; i++)
    {
        var line = lineOf[order[i].Id];
        var p0 = line.GetEndPoint(0);
        var p1 = line.GetEndPoint(1);
        XYZ toward;
        bool p1IsEnd;
        if (i < n - 1)
        {
            toward = jointPoint[order[i].Id.ToString() + ">" + order[i + 1].Id.ToString()];
            p1IsEnd = p1.DistanceTo(toward) <= p0.DistanceTo(toward);
        }
        else if (i > 0)
        {
            toward = jointPoint[order[i].Id.ToString() + ">" + order[i - 1].Id.ToString()];
            p1IsEnd = p0.DistanceTo(toward) <= p1.DistanceTo(toward);
        }
        else p1IsEnd = p0.DistanceTo(startPoint) <= p1.DistanceTo(startPoint);
        aPt[i] = p1IsEnd ? p0 : p1;
        bPt[i] = p1IsEnd ? p1 : p0;
    }

    var dir = new XYZ[n];
    var isRiser = new bool[n];
    for (var i = 0; i < n; i++)
    {
        dir[i] = (bPt[i] - aPt[i]).Normalize();
        isRiser[i] = Math.Abs(dir[i].Z) > 0.999;
    }

    // ---- 4. THE CORNERS of the original's centreline ----------------------------
    // Where each piece's line meets the next one's - the elbow's own corner. Two
    // pieces in line meet half way across the union or transition between them.
    var corner = new XYZ[n + 1];
    corner[0] = aPt[0];
    corner[n] = bPt[n - 1];
    var bend = new bool[n + 1];
    var tenMm = 10.0 / mmPerFoot;
    for (var i = 1; i < n; i++)
    {
        var u = dir[i - 1];
        var v = dir[i];
        var cosine = u.DotProduct(v);
        if (Math.Abs(cosine) > 0.99985)   // within about a degree: in line
        {
            if (cosine < 0)
                problems.Add("pieces " + order[i - 1].Id.ToString() + " and " + order[i].Id.ToString()
                    + " double straight back on each other - no parallel copy can follow that");
            corner[i] = (bPt[i - 1] + aPt[i]) * 0.5;
            continue;
        }
        bend[i] = true;
        var w = bPt[i - 1] - aPt[i];
        var d = u.DotProduct(w);
        var e = v.DotProduct(w);
        var denominator = 1.0 - cosine * cosine;
        var t = (cosine * e - d) / denominator;
        var s = (e - cosine * d) / denominator;
        var onFirst = bPt[i - 1] + u * t;
        var onSecond = aPt[i] + v * s;
        if (onFirst.DistanceTo(onSecond) > tenMm)
            problems.Add("pieces " + order[i - 1].Id.ToString() + " and " + order[i].Id.ToString() + " do not meet at "
                + "a corner - their centrelines pass " + Math.Round(onFirst.DistanceTo(onSecond) * mmPerFoot).ToString(invariant)
                + " mm apart, a rolling offset this does not copy");
        corner[i] = (onFirst + onSecond) * 0.5;
    }

    // ---- 5. THE OFFSET of every corner, per millimetre of spacing ----------------
    // In plan: each piece's left is Z x its direction. A riser takes the side of
    // the piece before it, or after it at the start. Each corner moves along the
    // mitre of the two pieces' sides, so both offset lines meet at it exactly.
    var shift = new XYZ[n + 1];
    string sideSaid;
    if (vertical)
    {
        var upward = sideWord == "up" || sideWord == "above";
        for (var i = 0; i < n; i++)
            if (isRiser[i])
                problems.Add(drawn + " " + order[i].Id.ToString() + " runs straight up - a copy above or below it "
                    + "would lie inside it. Say left or right for a run with a riser");
        for (var i = 0; i <= n; i++) shift[i] = new XYZ(0, 0, upward ? 1.0 : -1.0);
        sideSaid = upward ? "above" : "below";
    }
    else
    {
        var normal = new XYZ[n];
        for (var i = 0; i < n; i++)
            if (!isRiser[i]) normal[i] = new XYZ(-dir[i].Y, dir[i].X, 0).Normalize();
        for (var i = 1; i < n; i++) if (normal[i] == null) normal[i] = normal[i - 1];
        for (var i = n - 2; i >= 0; i--) if (normal[i] == null) normal[i] = normal[i + 1];

        if (normal[0] == null)
        {
            problems.Add("the run only goes straight up, so it has no left or right in plan, and a copy above "
                + "or below would lie inside it - a riser alone is not copied here");
        }
        else
        {
            var sign = 1.0;
            if (sideWord == "right") sign = -1.0;
            else if (compass)
            {
                var toward = sideWord == "north" ? XYZ.BasisY : sideWord == "south" ? -XYZ.BasisY
                    : sideWord == "east" ? XYZ.BasisX : -XYZ.BasisX;
                var lean = normal[0].DotProduct(toward);
                if (Math.Abs(lean) < 0.1)
                    problems.Add("the run's first piece points " + sideWord + " itself, so " + sideWord
                        + " is neither of its sides - say left or right");
                sign = lean >= 0 ? 1.0 : -1.0;
            }

            shift[0] = normal[0] * sign;
            shift[n] = normal[n - 1] * sign;
            for (var i = 1; i < n; i++)
            {
                var together = 1.0 + normal[i - 1].DotProduct(normal[i]);
                if (together < 1e-6)
                {
                    problems.Add("the run turns straight back at " + drawn + " " + order[i].Id.ToString()
                        + " - no parallel copy can follow that");
                    shift[i] = normal[i] * sign;
                }
                else shift[i] = (normal[i - 1] + normal[i]) * (sign / together);
            }
        }
        sideSaid = "to the " + sideWord;
    }

    // The spacing must clear the run's own size.
    var spacing = spacingMm / mmPerFoot;
    var widest = 0.0;
    var sizeOf = new double[n];
    var typeOf = new ElementId[n];
    var levelOf = new ElementId[n];
    var systemOf = new ElementId[n];
    for (var i = 0; i < n; i++)
    {
        var piece = order[i];
        var size = piece.get_Parameter(diameterParam);
        sizeOf[i] = size == null ? 0 : size.AsDouble();
        if (sizeOf[i] > widest) widest = sizeOf[i];
        typeOf[i] = piece.GetTypeId();
        levelOf[i] = piece.ReferenceLevel == null ? null : piece.ReferenceLevel.Id;
        if (levelOf[i] == null) problems.Add(drawn + " " + piece.Id.ToString() + " has no reference level to draw its copy on");
        if (pipes > 0)
        {
            var system = piece.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            systemOf[i] = system == null ? ElementId.InvalidElementId : system.AsElementId();
            if (systemOf[i] == null || systemOf[i] == ElementId.InvalidElementId)
                problems.Add("pipe " + piece.Id.ToString() + " has no piping system type, and a copy is drawn on the original's");
        }
    }
    if (spacing <= widest)
        problems.Add("a spacing of " + spacingMm.ToString("0.#", invariant) + " mm centre to centre is not wider than the run's "
            + "largest " + drawn + ", " + (widest * mmPerFoot).ToString("0.#", invariant) + " mm - the copies would sit "
            + "inside one another");

    // Every copy's pieces, checked before anything is drawn: an inside corner that
    // closes up, or a piece too short to exist, refuses the job now.
    var shortest = doc.Application.ShortCurveTolerance;
    var copyCorner = new XYZ[count + 1, n + 1];
    for (var k = 1; k <= count && problems.Count == 0; k++)
    {
        for (var i = 0; i <= n; i++) copyCorner[k, i] = corner[i] + shift[i] * (spacing * k);
        for (var i = 0; i < n; i++)
        {
            var piece = copyCorner[k, i + 1] - copyCorner[k, i];
            var along = piece.DotProduct(dir[i]);
            if (along <= shortest)
                problems.Add("copy " + k.ToString(invariant) + ", piece " + (i + 1).ToString(invariant) + ": the spacing closes "
                    + "the inside of the bend up - it would be " + Math.Round(along * mmPerFoot).ToString(invariant)
                    + " mm long. Copy to the other side, or fewer copies");
        }
    }

    if (problems.Count > 0)
    {
        refused = string.Format("{0} problem(s), so no copy was made and the run was not touched: {1}",
            problems.Count, string.Join(" | ", problems));
        break;
    }

    foreach (var id in fittingsGiven) if (!fittingsUsed.Contains(id)) notRemade.Add(id);

    // ---- 6. DRAW every copy's pieces, at the original's own size ----------------
    var made = new MEPCurve[count + 1, n];
    var copyLines = new List<string>();
    for (var k = 1; k <= count; k++)
    {
        for (var i = 0; i < n; i++)
        {
            var start = copyCorner[k, i];
            var end = copyCorner[k, i + 1];
            MEPCurve piece = null;
            try
            {
                piece = pipes > 0
                    ? (MEPCurve)Pipe.Create(doc, systemOf[i], typeOf[i], levelOf[i], start, end)
                    : Conduit.Create(doc, typeOf[i], start, end, levelOf[i]);
            }
            catch (Exception failure)
            {
                problems.Add("copy " + k.ToString(invariant) + ", piece " + (i + 1).ToString(invariant)
                    + ": Revit would not create the " + drawn + " - " + failure.Message);
                continue;
            }
            if (piece == null)
            {
                problems.Add("copy " + k.ToString(invariant) + ", piece " + (i + 1).ToString(invariant)
                    + ": Revit returned no " + drawn + " and raised no error");
                continue;
            }
            made[k, i] = piece;
            if (sizeOf[i] > 0)
            {
                var size = piece.get_Parameter(diameterParam);
                if (size == null || size.IsReadOnly || !size.Set(sizeOf[i]))
                    problems.Add("copy " + k.ToString(invariant) + ", piece " + (i + 1).ToString(invariant)
                        + ": the " + drawn + " type would not take the original's "
                        + (sizeOf[i] * mmPerFoot).ToString("0.#", invariant) + " mm");
            }
        }
    }

    if (problems.Count > 0)
        throw new InvalidOperationException(string.Format(
            "{0} problem(s) drawing the copies, so NOTHING was kept - one request is one change, whole or not "
            + "at all (Article 8): {1}", problems.Count, string.Join(" | ", problems)));

    // A size is read back, never assumed: Revit snaps to the type's sizes.
    doc.Regenerate();
    for (var k = 1; k <= count; k++)
        for (var i = 0; i < n; i++)
        {
            if (sizeOf[i] <= 0) continue;
            var size = made[k, i].get_Parameter(diameterParam);
            var stored = size == null ? double.NaN : size.AsDouble();
            if (double.IsNaN(stored) || Math.Abs(stored - sizeOf[i]) > 1e-6)
                problems.Add("copy " + k.ToString(invariant) + ", piece " + (i + 1).ToString(invariant) + ": asked for "
                    + (sizeOf[i] * mmPerFoot).ToString("0.#", invariant) + " mm and the type made it "
                    + (double.IsNaN(stored) ? "nothing" : (stored * mmPerFoot).ToString("0.#", invariant) + " mm"));
        }

    // ---- 7. RE-MAKE every joint: an elbow at a bend, a union or transition in line
    Func<MEPCurve, XYZ, Connector> endNear = (piece, point) =>
    {
        Connector best = null;
        var bestGap = double.MaxValue;
        foreach (Connector c in piece.ConnectorManager.Connectors)
        {
            if (c.ConnectorType != ConnectorType.End) continue;
            var gap = c.Origin.DistanceTo(point);
            if (gap < bestGap) { bestGap = gap; best = c; }
        }
        return best;
    };

    var elbows = 0;
    var inLine = 0;
    for (var k = 1; k <= count && problems.Count == 0; k++)
    {
        var pieceIds = new List<string>();
        var fittingIds = new List<string>();
        for (var i = 0; i < n; i++)
        {
            copies.Add(made[k, i].Id);
            pieceIds.Add(made[k, i].Id.ToString());
        }
        for (var i = 1; i < n; i++)
        {
            var first = endNear(made[k, i - 1], copyCorner[k, i]);
            var second = endNear(made[k, i], copyCorner[k, i]);
            var what = bend[i] ? "elbow" : Math.Abs(sizeOf[i - 1] - sizeOf[i]) < 1e-9 ? "union" : "transition";
            FamilyInstance fitting = null;
            try
            {
                if (first == null || second == null) throw new InvalidOperationException("a new piece has no open end there");
                fitting = what == "elbow" ? doc.Create.NewElbowFitting(first, second)
                    : what == "union" ? doc.Create.NewUnionFitting(first, second)
                    : doc.Create.NewTransitionFitting(first, second);
            }
            catch (Exception failure)
            {
                problems.Add("copy " + k.ToString(invariant) + ", the " + what + " between pieces " + i.ToString(invariant)
                    + " and " + (i + 1).ToString(invariant) + ": Revit would not make it - " + failure.Message);
                continue;
            }
            if (fitting == null)
            {
                problems.Add("copy " + k.ToString(invariant) + ", the " + what + " between pieces " + i.ToString(invariant)
                    + " and " + (i + 1).ToString(invariant) + ": Revit returned no fitting and raised no error");
                continue;
            }
            copies.Add(fitting.Id);
            fittingIds.Add(fitting.Id.ToString());
            if (what == "elbow") elbows++; else inLine++;
        }
        copyLines.Add("copy " + k.ToString(invariant) + ", " + (spacingMm * k).ToString("0.#", invariant) + " mm " + sideSaid
            + ": " + drawn + "s " + string.Join(", ", pieceIds)
            + (fittingIds.Count > 0 ? "; fittings " + string.Join(", ", fittingIds) : "; no fitting"));
    }

    if (problems.Count > 0)
        throw new InvalidOperationException(string.Format(
            "{0} problem(s) making the copies, so NOTHING was kept - one request is one change, whole or not "
            + "at all (Article 8): {1}", problems.Count, string.Join(" | ", problems)));

    perCopy = string.Join(" || ", copyLines);
    summary = string.Format(
        "{0} parallel cop{1} of a {2}-piece {3} run, {4} mm apart centre to centre, {5}: {6} {3}(s), {7} elbow(s) "
        + "and {8} union(s) or transition(s) made. Read from the run's start at {9}, {10}, {11} mm - say the other "
        + "side if the copies went the wrong way. The original was not touched. Each copy's ends are left OPEN, "
        + "and insulation, lining and any hand-picked fitting type are the {3} type's own, not copied.",
        count, count == 1 ? "y" : "ies", n, drawn, spacingMm.ToString("0.#", invariant), sideSaid, count * n,
        elbows, inLine,
        Math.Round(startPoint.X * mmPerFoot).ToString(invariant), Math.Round(startPoint.Y * mmPerFoot).ToString(invariant),
        Math.Round(startPoint.Z * mmPerFoot).ToString(invariant))
        + (notRemade.Count > 0 ? " " + notRemade.Count.ToString(invariant) + " fitting(s) handed in join no two pieces of "
            + "the run - a cap or an end fitting - and were not copied." : "");
}
while (false);
