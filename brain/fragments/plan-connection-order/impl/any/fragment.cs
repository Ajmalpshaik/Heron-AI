// NOT STANDALONE. Assumes `doc`, `elements`, `mode`, `groupKeys` and
// `orthogonalDistance` are in scope; leaves `order`, `links`, `totalLengthMm`,
// `guarantee`, `noLocation` and `findings` behind.
//
// READ ONLY. Opens no transaction, needs none, and changes nothing.
//
// A TREE AND A CHAIN ARE DIFFERENT QUESTIONS AND ONLY ONE GIVES AN ORDER.
// A tree connects everything with the least total length and may branch, which
// is how a homerun behaves. It has no sequence, so `order` comes back EMPTY for
// it and `links` carries the answer. A chain visits each element once, which is
// what a sequence means.
//
// THE GUARANTEES DIFFER. The tree is EXACT for the distances given. The chain
// is a heuristic - nearest neighbour improved by pair-swapping - usually within
// a few per cent and never guaranteed. `guarantee` carries the word so nothing
// downstream claims "shortest possible" for a chain.
//
// UNGROUPED, THIS DOES NOT KNOW WALLS EXIST. It will hop into the next room for
// a fitting 200 mm closer and come back. Grouping routes each room as its own
// run and links the runs - and REPORTS A LONGER TOTAL, because a constraint
// costs length. The ungrouped number is the unbuildable one.
//
// LENGTHS ARE ESTIMATES. Straight line is a lower bound nothing achieves;
// orthogonal assumes running square along ceilings and down walls. Neither
// knows about obstructions or a drop nobody modelled.

var mmPerFoot = 304.8;

var order = new List<ElementId>();
var links = new List<string>();
var noLocation = new List<ElementId>();
var findings = new List<string>();
var totalLengthMm = 0.0;
var guarantee = "none";

var wanted = string.IsNullOrEmpty(mode) ? "" : mode.Trim().ToLowerInvariant();

// A mode that is not understood computes nothing. Falling back to one of them
// would answer a different question than was asked, and the caller cannot tell
// which answer they got.
if (wanted != "tree" && wanted != "chain" && wanted != "continuous")
{
    findings.Add("Mode \"" + mode + "\" is not one of tree, chain, continuous. Nothing was "
        + "computed - defaulting to one of them would silently answer a different question.");
}
else
{
    // Where each element is. A point-based element gives its point; a linear one
    // its midpoint; anything else the centre of its bounding box, which is the
    // last honest fallback before giving up on it.
    var points = new List<XYZ>();
    var ids = new List<ElementId>();

    foreach (var element in elements)
    {
        if (element == null) continue;

        XYZ where = null;

        var asPoint = element.Location as LocationPoint;
        if (asPoint != null) where = asPoint.Point;

        if (where == null)
        {
            var asCurve = element.Location as LocationCurve;
            if (asCurve != null && asCurve.Curve != null)
            {
                where = asCurve.Curve.Evaluate(0.5, true);
            }
        }

        if (where == null)
        {
            var box = element.get_BoundingBox(null);
            if (box != null) where = (box.Min + box.Max) * 0.5;
        }

        if (where == null) { noLocation.Add(element.Id); continue; }

        points.Add(where);
        ids.Add(element.Id);
    }

    // One key per element, same order, same length. A mismatch is refused
    // rather than paired by position - a wrong key against a right element
    // routes a fitting into the wrong room, and the total still looks
    // plausible.
    var keys = new List<string>();
    var grouped = false;

    if (groupKeys != null && groupKeys.Count > 0)
    {
        if (groupKeys.Count != elements.Count)
        {
            findings.Add("The group keys and the elements are different lengths, so they cannot "
                + "be paired. Everything is routed as one group and the total below is the "
                + "unbuildable one - it will cross between rooms freely.");
        }
        else
        {
            grouped = true;
            var at = 0;
            foreach (var element in elements)
            {
                if (element == null) { at++; continue; }
                if (noLocation.Contains(element.Id)) { at++; continue; }
                var key = groupKeys[at];
                keys.Add(string.IsNullOrEmpty(key) ? "(no group)" : key);
                at++;
            }
        }
    }

    if (!grouped)
    {
        foreach (var id in ids) keys.Add("(all)");
    }

    if (wanted == "continuous" && !grouped)
    {
        findings.Add("Continuous needs groups - it is defined by finishing one group before "
            + "moving to the next. Without them it is the same as a chain, and that is what ran.");
        wanted = "chain";
    }

    if (points.Count < 2)
    {
        findings.Add("Fewer than two elements have a location, so there is nothing to connect.");
    }
    else
    {
        Func<int, int, double> distance = (a, b) =>
        {
            var one = points[a];
            var other = points[b];
            if (orthogonalDistance)
            {
                return Math.Abs(one.X - other.X)
                     + Math.Abs(one.Y - other.Y)
                     + Math.Abs(one.Z - other.Z);
            }
            return one.DistanceTo(other);
        };

        // Groups, in the order their first member was given, so the answer is
        // the same every run.
        var groupOrder = new List<string>();
        var groups = new Dictionary<string, List<int>>();

        for (int i = 0; i < ids.Count; i++)
        {
            var key = keys[i];
            if (!groups.ContainsKey(key))
            {
                groups[key] = new List<int>();
                groupOrder.Add(key);
            }
            groups[key].Add(i);
        }

        var total = 0.0;

        // Prim's algorithm over a set of indices. Exact: the tree it returns IS
        // the shortest total for these distances.
        Func<List<int>, double> spanningTree = members =>
        {
            var length = 0.0;
            if (members.Count < 2) return length;

            var inTree = new List<int>();
            var outside = new List<int>();
            foreach (var m in members) outside.Add(m);

            inTree.Add(outside[0]);
            outside.RemoveAt(0);

            while (outside.Count > 0)
            {
                var bestInside = -1;
                var bestOutside = -1;
                var bestDistance = double.MaxValue;

                foreach (var inside in inTree)
                {
                    foreach (var candidate in outside)
                    {
                        var d = distance(inside, candidate);
                        if (d >= bestDistance) continue;
                        bestDistance = d;
                        bestInside = inside;
                        bestOutside = candidate;
                    }
                }

                if (bestOutside < 0) break;

                links.Add(ids[bestInside] + " - " + ids[bestOutside] + " ("
                    + (bestDistance * mmPerFoot).ToString("F0") + " mm)");
                length += bestDistance;

                inTree.Add(bestOutside);
                outside.Remove(bestOutside);
            }

            return length;
        };

        // Nearest neighbour from a given start, optionally forced to finish at
        // a given member. Used on its own for a chain, and once per candidate
        // exit for continuous.
        Func<List<int>, int, int, List<int>> nearestNeighbour = (members, start, forcedLast) =>
        {
            var route = new List<int>();
            var left = new List<int>();
            foreach (var m in members)
            {
                if (m == start) continue;
                if (forcedLast >= 0 && m == forcedLast) continue;
                left.Add(m);
            }

            var at = start;
            route.Add(at);

            while (left.Count > 0)
            {
                var best = -1;
                var bestDistance = double.MaxValue;

                foreach (var candidate in left)
                {
                    var d = distance(at, candidate);
                    if (d >= bestDistance) continue;
                    bestDistance = d;
                    best = candidate;
                }

                if (best < 0) break;

                route.Add(best);
                left.Remove(best);
                at = best;
            }

            if (forcedLast >= 0 && forcedLast != start) route.Add(forcedLast);
            return route;
        };

        Func<List<int>, double> routeLength = route =>
        {
            var length = 0.0;
            for (int i = 1; i < route.Count; i++) length += distance(route[i - 1], route[i]);
            return length;
        };

        // 2-opt: reverse any stretch that makes the route shorter, repeatedly.
        // This is the "improved" half of the heuristic and it is where most of
        // the nearest-neighbour silliness comes out.
        Action<List<int>> improve = route =>
        {
            var improved = true;
            var passes = 0;

            while (improved && passes < 4)
            {
                improved = false;
                passes++;

                for (int i = 0; i < route.Count - 2; i++)
                {
                    for (int j = i + 2; j < route.Count; j++)
                    {
                        var before = distance(route[i], route[i + 1]);
                        if (j + 1 < route.Count) before += distance(route[j], route[j + 1]);

                        var after = distance(route[i], route[j]);
                        if (j + 1 < route.Count) after += distance(route[i + 1], route[j + 1]);

                        if (after >= before - 1e-9) continue;

                        route.Reverse(i + 1, j - i);
                        improved = true;
                    }
                }
            }
        };

        if (wanted == "tree")
        {
            guarantee = "exact";

            foreach (var key in groupOrder) total += spanningTree(groups[key]);

            // The feeders between groups: one link per group joined to the
            // nearest already-joined group, at the nearest pair of points. Local
            // runs per room, feeders between them - the real shape.
            if (grouped && groupOrder.Count > 1)
            {
                var joined = new List<string>();
                var pending = new List<string>();
                foreach (var key in groupOrder) pending.Add(key);

                joined.Add(pending[0]);
                pending.RemoveAt(0);

                while (pending.Count > 0)
                {
                    var bestFrom = -1;
                    var bestTo = -1;
                    var bestKey = "";
                    var bestDistance = double.MaxValue;

                    foreach (var have in joined)
                    {
                        foreach (var want in pending)
                        {
                            foreach (var a in groups[have])
                            {
                                foreach (var b in groups[want])
                                {
                                    var d = distance(a, b);
                                    if (d >= bestDistance) continue;
                                    bestDistance = d;
                                    bestFrom = a;
                                    bestTo = b;
                                    bestKey = want;
                                }
                            }
                        }
                    }

                    if (bestTo < 0) break;

                    links.Add("FEEDER " + ids[bestFrom] + " - " + ids[bestTo] + " ("
                        + (bestDistance * mmPerFoot).ToString("F0") + " mm)");
                    total += bestDistance;

                    joined.Add(bestKey);
                    pending.Remove(bestKey);
                }
            }

            findings.Add("A tree has no sequence, so `order` is empty on purpose. It branches - "
                + "that is what makes it the shortest - and `links` is the answer. Anything "
                + "expecting a sequence needs mode chain or continuous.");
        }
        else if (wanted == "chain")
        {
            guarantee = "heuristic";

            foreach (var key in groupOrder)
            {
                var members = groups[key];
                var route = nearestNeighbour(members, members[0], -1);
                improve(route);

                for (int i = 0; i < route.Count; i++)
                {
                    order.Add(ids[route[i]]);
                    if (i == 0) continue;

                    var step = distance(route[i - 1], route[i]);
                    total += step;
                    links.Add(ids[route[i - 1]] + " - " + ids[route[i]] + " ("
                        + (step * mmPerFoot).ToString("F0") + " mm)");
                }
            }
        }
        else
        {
            guarantee = "heuristic";

            var unvisited = new List<int>();
            for (int i = 0; i < ids.Count; i++) unvisited.Add(i);

            // The first element given is the start. Deterministic, and it is the
            // caller's implicit choice of where the run begins.
            var entry = 0;
            var currentKey = keys[0];

            while (true)
            {
                var members = new List<int>();
                foreach (var m in groups[currentKey])
                {
                    if (unvisited.Contains(m)) members.Add(m);
                }

                if (members.Count == 0) break;

                // THE EXIT IS OPTIMISED, NOT ACCEPTED. Every candidate last
                // fitting in this group is tried, scored on the path through the
                // group PLUS the jump out of it. That one choice is the
                // difference between a grouped route that costs a quarter more
                // than the unbuildable ideal and one within a few per cent.
                List<int> bestRoute = null;
                var bestCost = double.MaxValue;

                foreach (var candidateExit in members)
                {
                    var route = nearestNeighbour(members, entry,
                        candidateExit == entry ? -1 : candidateExit);
                    improve(route);

                    // Improving can move the forced exit, so the jump is scored
                    // from wherever the route actually ends.
                    var endsAt = route[route.Count - 1];

                    var cost = routeLength(route);

                    var jump = 0.0;
                    var haveJump = false;
                    foreach (var candidate in unvisited)
                    {
                        if (members.Contains(candidate)) continue;
                        var d = distance(endsAt, candidate);
                        if (haveJump && d >= jump) continue;
                        jump = d;
                        haveJump = true;
                    }

                    if (haveJump) cost += jump;

                    if (cost >= bestCost) continue;
                    bestCost = cost;
                    bestRoute = route;
                }

                if (bestRoute == null) break;

                for (int i = 0; i < bestRoute.Count; i++)
                {
                    order.Add(ids[bestRoute[i]]);
                    unvisited.Remove(bestRoute[i]);

                    if (i == 0) continue;

                    var step = distance(bestRoute[i - 1], bestRoute[i]);
                    total += step;
                    links.Add(ids[bestRoute[i - 1]] + " - " + ids[bestRoute[i]] + " ("
                        + (step * mmPerFoot).ToString("F0") + " mm)");
                }

                if (unvisited.Count == 0) break;

                // The jump decides which group comes next AND where it is
                // entered - one choice, not two.
                var from = bestRoute[bestRoute.Count - 1];
                var next = -1;
                var nextDistance = double.MaxValue;

                foreach (var candidate in unvisited)
                {
                    var d = distance(from, candidate);
                    if (d >= nextDistance) continue;
                    nextDistance = d;
                    next = candidate;
                }

                if (next < 0) break;

                total += nextDistance;
                links.Add("JUMP " + ids[from] + " - " + ids[next] + " ("
                    + (nextDistance * mmPerFoot).ToString("F0") + " mm)");

                entry = next;
                currentKey = keys[next];
            }
        }

        totalLengthMm = total * mmPerFoot;

        findings.Add("Total " + totalLengthMm.ToString("F0") + " mm over " + ids.Count
            + " element(s), mode " + wanted + ", " + guarantee + ". Measured "
            + (orthogonalDistance
                ? "across, along and up - how it is installed, and still blind to obstructions"
                : "STRAIGHT LINE, which is a lower bound nothing can achieve")
            + ". This is the connection ORDER plus a length estimate; it is not a cable schedule.");

        if (grouped)
        {
            findings.Add("Routed as " + groupOrder.Count + " group(s). The total is LONGER than "
                + "the ungrouped one would be, and that is correct - grouping is a constraint, "
                + "and constraints cost length. The ungrouped figure is shorter only because it "
                + "crosses between rooms freely, which is to say it runs through walls.");
        }
        else
        {
            findings.Add("Routed as ONE group. Nothing here knows a wall exists, so this route "
                + "will hop into the next room for a fitting 200 mm closer and come back. Pass "
                + "group keys for a total anybody could build.");
        }
    }

    if (noLocation.Count > 0)
    {
        findings.Add(noLocation.Count + " element(s) have no readable location and are not in the "
            + "route at all. They are not connected by something this did not measure.");
    }
}
