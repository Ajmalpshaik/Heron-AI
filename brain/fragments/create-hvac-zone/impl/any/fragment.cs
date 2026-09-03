// NOT STANDALONE. Assumes `doc`, `zoneName`, `level`, `phase` and `elements` are
// in scope; leaves `zone`, `added`, `movedFromAnotherZone` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// REVIT 2027 REMOVES THIS CAPABILITY FROM THE API. Checked against 2027's own
// assembly, not assumed: the creation factory has no zone method left, and
// Zone.AddSpaces is gone from the Zone class - while Zone itself, Zone.Spaces
// and Zone.Area remain. Naming either vanished call directly would break the
// 2027 BUILD, and a try/catch does not help because it never runs. So both are
// reached BY NAME, resolved once here. One source, eight releases, and a plain
// reason on the one where the only route left is the Revit interface.
//
// A SPACE BELONGS TO ONE ZONE AND REVIT MOVES IT WITHOUT ASKING. A space already
// in another zone silently leaves it. Reported per space, because the damage is
// to a zone nobody was looking at.
//
// A NEW ZONE REPORTS ZERO AREA. Revit computes it later, so this reports what
// was ADDED and never the zone's area - anything wanting that must ask again in
// a later call.

Element zone = null;
var added = 0;
var movedFromAnotherZone = new List<string>();
var refused = new List<string>();

var creationFactory = doc.Create;
var newZoneMethod = creationFactory.GetType().GetMethod("NewZone",
    new[] { typeof(Level), typeof(Phase) });

if (newZoneMethod == null)
{
    refused.Add("this Revit version has removed HVAC zone creation from the API - the creation factory "
        + "has no zone method left. The zone has to be made through the Revit interface, under Analyze, "
        + "Spaces and Zones. Nothing here can do it, and nothing here is broken");
}
else
{
    var asPhase = phase as Phase;
    if (asPhase == null)
    {
        refused.Add("a zone needs a Phase, and what was given is not one. The zone and its spaces must "
            + "share a phase - a mismatch is the usual reason a space is rejected");
    }
    else
    {
        try
        {
            zone = newZoneMethod.Invoke(creationFactory, new object[] { level, asPhase }) as Element;
        }
        catch (Exception ex)
        {
            var reason = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
            refused.Add(string.Format("could not create the zone - {0}", reason));
        }

        if (zone != null)
        {
            if (!string.IsNullOrEmpty(zoneName))
            {
                try { zone.Name = zoneName; }
                catch
                {
                    refused.Add(string.Format("'{0}' is already a zone name here - the zone was created "
                        + "as '{1}'", zoneName, zone.Name));
                }
            }

            var addSpaces = zone.GetType().GetMethod("AddSpaces", new[] { typeof(SpaceSet) });

            if (addSpaces == null)
            {
                refused.Add("the zone was created, but this Revit version has removed the call that "
                    + "adds spaces to it. Add them through the Revit interface");
            }
            else
            {
                foreach (var element in elements)
                {
                    // `Space` comes from the mechanical namespace, which the wrapper imports -
                    // naming it in full would cross the adapter boundary.
                    var space = element as Space;
                    if (space == null)
                    {
                        refused.Add(string.Format("{0} (id {1}) is not a Space - a room is not a space, "
                            + "and only a space can go in a zone", element.Name, element.Id));
                        continue;
                    }

                    // Where it is now, BEFORE the move - afterwards there is
                    // nothing left to read.
                    string previous = null;
                    try
                    {
                        var existing = space.Zone;
                        if (existing != null && existing.Id != zone.Id) previous = existing.Name;
                    }
                    catch { }

                    try
                    {
                        var set = new SpaceSet();
                        set.Insert(space);
                        addSpaces.Invoke(zone, new object[] { set });
                        added++;

                        if (previous != null)
                        {
                            movedFromAnotherZone.Add(string.Format("{0} (id {1}) LEFT zone '{2}'",
                                space.Name, space.Id, previous));
                        }
                    }
                    catch (Exception ex)
                    {
                        var reason = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                        refused.Add(string.Format("{0} (id {1}) was rejected - {2}. A phase that does "
                            + "not match the zone's is the usual cause",
                            space.Name, space.Id, reason));
                    }
                }
            }

            if (movedFromAnotherZone.Count > 0)
            {
                refused.Add(string.Format("{0} space(s) were TAKEN OUT of another zone to be put in this "
                    + "one - Revit allows a space only one zone and moves it without asking",
                    movedFromAnotherZone.Count));
            }

            refused.Add(string.Format("{0} space(s) added. The zone's AREA reads zero until Revit "
                + "recomputes it - do not report that zero as the answer; ask again in a later call",
                added));
        }
    }
}
