// NOT STANDALONE. Assumes `elements` is in scope, and leaves the six flow
// dictionaries, `noDesignFigure` and `unplaced` behind.
//
// READ ONLY. Opens no transaction and needs none, and it never sets a flow.
//
// A SPACE WITH NO DESIGN FLOW LOOKS BALANCED.
//
// Design zero, actual zero, difference zero - a balance check passes. That
// space was never given a target at all, so the report declares the very rooms
// nobody has designed to be the ones with no problem. `noDesignFigure` is the
// list a coordinator acts on, and it is the reason this fragment exists rather
// than a subtraction.
//
// DESIGN AND ACTUAL ARE DIFFERENT KINDS OF NUMBER.
//
//   design   what somebody typed, or what Revit computed from the space load
//   actual   the sum of what the terminals connected to the space carry
//
// "What should it be" and "what is it". Folded into one difference, which side
// is wrong disappears - and an under-served space and an un-designed one go to
// different people.
//
// Values stay in Revit's internal cubic feet per second. D-20 keeps the
// conversion to litres per second at the edge where the user is, and not
// touching a units API is what keeps this clear of the 2021 units move.

var designSupply = new Dictionary<ElementId, double>();
var actualSupply = new Dictionary<ElementId, double>();
var designReturn = new Dictionary<ElementId, double>();
var actualReturn = new Dictionary<ElementId, double>();
var designExhaust = new Dictionary<ElementId, double>();
var actualExhaust = new Dictionary<ElementId, double>();
var noDesignFigure = new List<ElementId>();
var unplaced = new List<ElementId>();

Func<Element, BuiltInParameter, double> flow = (element, which) =>
{
    try
    {
        var p = element.get_Parameter(which);
        if (p != null && p.HasValue) return p.AsDouble();
    }
    catch { }
    return 0.0;
};

foreach (var element in elements)
{
    var spatial = element as SpatialElement;
    if (spatial == null) continue;

    // By CATEGORY. `element is Space` WOULD compile - Mechanical is imported,
    // measured rather than assumed - but the sibling fragment separating Rooms
    // from Spaces cannot name `Room` at all, and one idiom across the pair is
    // worth more than one saved line.
    bool isSpace = spatial.Category != null
                   && spatial.Category.Id == new ElementId(BuiltInCategory.OST_MEPSpaces);
    if (!isSpace) continue;

    double area = 0.0;
    try { area = spatial.Area; } catch { }
    if (area <= 0.0)
    {
        // Worth SEEING rather than hiding: a space in the schedule that is not
        // in the model is a real finding, and dropping it makes the count
        // disagree with the schedule for no visible reason.
        unplaced.Add(spatial.Id);
        continue;
    }

    double ds = flow(spatial, BuiltInParameter.ROOM_DESIGN_SUPPLY_AIRFLOW_PARAM);
    double dr = flow(spatial, BuiltInParameter.ROOM_DESIGN_RETURN_AIRFLOW_PARAM);
    double de = flow(spatial, BuiltInParameter.ROOM_DESIGN_EXHAUST_AIRFLOW_PARAM);

    designSupply[spatial.Id] = ds;
    designReturn[spatial.Id] = dr;
    designExhaust[spatial.Id] = de;

    actualSupply[spatial.Id] = flow(spatial, BuiltInParameter.ROOM_ACTUAL_SUPPLY_AIRFLOW_PARAM);
    actualReturn[spatial.Id] = flow(spatial, BuiltInParameter.ROOM_ACTUAL_RETURN_AIRFLOW_PARAM);
    actualExhaust[spatial.Id] = flow(spatial, BuiltInParameter.ROOM_ACTUAL_EXHAUST_AIRFLOW_PARAM);

    // No target of ANY kind. Not "supply is zero", which can be correct for a
    // return-only space - all three, which means nobody has designed it.
    if (ds <= 0.0 && dr <= 0.0 && de <= 0.0) noDesignFigure.Add(spatial.Id);
}
