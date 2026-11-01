// NOT STANDALONE. Assumes `doc`, `elements`, `showTitle`, `showHeaders`,
// `gridLinesOnSheets`, `titleTextType`, `headerTextType`, `bodyTextType`,
// `stripedRows`, `stripedRowsOnSheets`, `stripedRowColour`, `rowHeightMm`,
// `includeLinks`, `filterBySheet` and `embeddedSchedule` are in scope; leaves
// `changed`, `applied`, `refused` and `notApplicable` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// THE SCHEDULE'S OWN SWITCHES, NOT A COLUMN'S. Schedule Properties,
// Appearance tab - Show Title, Show Headers, Grid lines on sheets, the Title,
// Header and Body text, Striped rows and (2024 on) the row height - plus three
// switches that sit on other tabs and belong to the whole schedule: Include
// elements in links (Fields), Filter by sheet (Filter, 2023 on) and the
// Embedded Schedule (Fields, rooms and spaces). What one COLUMN looks like is
// SET_SCHEDULE_APPEARANCE's; sorting and the grand total are
// SET_SCHEDULE_SORT_GROUP's.
//
// EVERY INPUT IS "LEAVE" UNLESS IT IS GIVEN. Each is `optional: true` with
// `absent: leave`, so a request names only what it wants changed. Nothing
// asked at all is refused, not run as a no-op that reads like success.
//
// ONE BAD SETTING DOES NOT STOP THE OTHERS - the rule SET_SCHEDULE_APPEARANCE
// keeps (a heading refused for many columns, the rest still applied). Every
// word is checked before any schedule is touched: a word that is not yes or
// no, a number that is not a number, a text type or category the project does
// not have, refuses THAT setting, named once, and every other setting asked is
// still applied. A setting one schedule cannot take - Revit's own Can* answer
// says no, or the release has not got it - is named for that schedule and the
// rest still applied to it. Chosen so a long request is not lost to one typo;
// the refusal says exactly what was not done, and Revit's undo takes the rest
// back in one entry if that is not what was wanted.
//
// A TEXT TYPE IS CHOSEN, NEVER EDITED. Title, Header and Body text are which
// TextNoteType this schedule uses; the type itself - shared with every other
// schedule and note that uses it - is not touched. That is how "make the
// schedule text bold" is served without restyling other drawings: pick a bold
// text type. IsValidTextTypeId is Revit's own answer, asked first.
//
// FOUR MEMBERS ARE NOT ON EVERY RELEASE, AND ARE REACHED BY NAME AT RUN TIME,
// one implementation for every release (the add-in has handed fragments the
// release symbols since FRAGMENT-ISSUES 5b-181's fix; this is a choice). Read off the shipped reference assemblies of all
// eight releases (tools/api-surface --members), 2026-11-01:
//   ViewSchedule.UseStripedRowsOnSheets, Get/SetStripedRowsColor   2021 on
//   ScheduleDefinition.IsFilteredBySheet, IsValidCategoryForFilterBySheet
//                                                                   2023 on
//   ViewSchedule.RowHeight, RowHeightOverride                       2024 on
// On a release without one, that one setting is named in `notApplicable`.
//
// ASSUMPTIONS THE PROOF SETTLES (NEEDS-CHECKING Group KK), not claims:
//   - STRIPED ROW COLOUR. A colour alone is written to StripedRowPattern
//     SecondRow, taken to be the coloured stripe; "first row #RRGGBB" and
//     "second row #RRGGBB" name one. Both rows' colours are read back and
//     reported, so the proof shows which one the dialog's swatch is.
//   - ROW HEIGHT. RowHeightOverride is set to All before RowHeight, in case
//     Revit ignores a height while the override is None. Both are read back.
//
// READ FIRST, WRITE, READ BACK. `applied` is a second read of each setting
// after the write, "before -> after", never an echo of the request. A setting
// that reads back other than asked is refused naming what it reads.

var changed = 0;
var applied = "";
var refused = new List<string>();
var notApplicable = new List<string>();

Func<Element, ViewSchedule> scheduleBehind = candidate =>
{
    var direct = candidate as ViewSchedule;
    if (direct != null) return direct;
    var placed = candidate as ScheduleSheetInstance;
    if (placed == null) return null;
    try { return doc.GetElement(placed.ScheduleId) as ViewSchedule; }
    catch { return null; }
};

{
    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    var appliedParts = new List<string>();
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');

    // ---- the words ------------------------------------------------------------
    Func<string, bool> isLeave = text =>
    {
        var t = (text ?? "").Trim().ToLowerInvariant();
        return t.Length == 0 || t == "leave" || t == "unchanged" || t == "keep";
    };
    // 1 yes, 0 no, -1 leave, -2 not a word this takes.
    Func<string, int> yesNo = text =>
    {
        if (isLeave(text)) return -1;
        var t = text.Trim().ToLowerInvariant();
        if (t == "yes" || t == "true" || t == "on" || t == "show" || t == "1" || t == "tick" || t == "ticked") return 1;
        if (t == "no" || t == "false" || t == "off" || t == "hide" || t == "0" || t == "untick" || t == "unticked") return 0;
        return -2;
    };
    Func<bool, string> yesNoText = value => value ? "yes" : "no";
    Func<Color, string> colourText = c =>
    {
        if (c == null) return "(none)";
        try
        {
            if (!c.IsValid) return "(none)";
            return c.Red.ToString(invariant) + "," + c.Green.ToString(invariant) + "," + c.Blue.ToString(invariant);
        }
        catch { return "(unreadable)"; }
    };
    var namedColours = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
    {
        { "white", new Color(255, 255, 255) }, { "black", new Color(0, 0, 0) },
        { "grey", new Color(128, 128, 128) }, { "gray", new Color(128, 128, 128) },
        { "light grey", new Color(230, 230, 230) }, { "light gray", new Color(230, 230, 230) },
        { "red", new Color(255, 0, 0) }, { "green", new Color(0, 128, 0) }, { "blue", new Color(0, 0, 255) },
        { "yellow", new Color(255, 255, 0) }, { "light blue", new Color(220, 235, 250) },
        { "light green", new Color(225, 245, 225) }, { "light yellow", new Color(255, 250, 220) }
    };
    Func<string, Color> colourOf = text =>
    {
        var t = (text ?? "").Trim();
        Color found;
        if (namedColours.TryGetValue(t, out found)) return found;
        if (t.StartsWith("#") && t.Length == 7)
        {
            int rgb;
            if (int.TryParse(t.Substring(1), System.Globalization.NumberStyles.HexNumber, invariant, out rgb))
                return new Color((byte)((rgb >> 16) & 255), (byte)((rgb >> 8) & 255), (byte)(rgb & 255));
            return null;
        }
        var channels = t.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        int r, g, b;
        if (channels.Length == 3 && int.TryParse(channels[0], out r) && int.TryParse(channels[1], out g)
            && int.TryParse(channels[2], out b) && r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
            return new Color((byte)r, (byte)g, (byte)b);
        return null;
    };

    var asked = 0;
    // The switches, as (label, value): -1 leave, 0 no, 1 yes.
    var switchWords = new List<Tuple<string, string>>
    {
        Tuple.Create("Show Title", showTitle),
        Tuple.Create("Show Headers", showHeaders),
        Tuple.Create("Grid lines on sheets", gridLinesOnSheets),
        Tuple.Create("Striped rows", stripedRows),
        Tuple.Create("Striped rows on sheets", stripedRowsOnSheets),
        Tuple.Create("Include elements in links", includeLinks),
        Tuple.Create("Filter by sheet", filterBySheet)
    };
    var wantSwitch = new Dictionary<string, int>();
    foreach (var pair in switchWords)
    {
        var value = yesNo(pair.Item2);
        if (value == -1) continue;
        asked++;
        if (value == -2)
        {
            refused.Add(pair.Item1 + " takes yes or no, not '" + pair.Item2.Trim() + "' - left as it was on every schedule");
            continue;
        }
        wantSwitch[pair.Item1] = value;
    }

    // ---- text types, by name ------------------------------------------------
    var textTypes = new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<Element>().ToList();
    Func<Element, string> nameOf = e => { try { return e.Name ?? ""; } catch { return ""; } };
    var textTypeNames = string.Join(", ", textTypes.Select(nameOf).Where(n => n.Length > 0)
        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).Select(n => "'" + n + "'").ToArray());
    var wantText = new Dictionary<string, Element>();
    foreach (var pair in new[] { Tuple.Create("Title text", titleTextType), Tuple.Create("Header text", headerTextType),
                                 Tuple.Create("Body text", bodyTextType) })
    {
        if (isLeave(pair.Item2)) continue;
        asked++;
        var wanted = pair.Item2.Trim();
        var exact = textTypes.Where(t => nameOf(t) == wanted).ToList();
        if (exact.Count == 0) exact = textTypes.Where(t => string.Equals(nameOf(t), wanted, StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count != 1)
        {
            refused.Add(pair.Item1 + ": " + (exact.Count == 0 ? "no text type is called '" + wanted + "'"
                : exact.Count + " text types answer to '" + wanted + "' - type its name exactly")
                + ". The text types in this project are " + textTypeNames + " - left as it was on every schedule");
            continue;
        }
        wantText[pair.Item1] = exact[0];
    }

    // ---- striped row colour -------------------------------------------------
    Color wantColour = null;
    var colourRow = "SecondRow";
    if (!isLeave(stripedRowColour))
    {
        asked++;
        var t = stripedRowColour.Trim();
        var lower = t.ToLowerInvariant();
        if (lower.StartsWith("first row")) { colourRow = "FirstRow"; t = t.Substring(9).Trim(); }
        else if (lower.StartsWith("second row")) { colourRow = "SecondRow"; t = t.Substring(10).Trim(); }
        wantColour = colourOf(t);
        if (wantColour == null)
            refused.Add("Striped row colour '" + stripedRowColour.Trim() + "' is not a colour - give #RRGGBB, "
                + "'230 230 230', or one of " + string.Join(", ", namedColours.Keys.ToArray())
                + ", optionally after 'first row' or 'second row' - left as it was on every schedule");
    }

    // ---- row height ---------------------------------------------------------
    double wantHeightFeet = -1;
    if (!isLeave(rowHeightMm))
    {
        asked++;
        double mm;
        var text = rowHeightMm.Trim();
        if (text.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) text = text.Substring(0, text.Length - 2).Trim();
        if (!double.TryParse(text, System.Globalization.NumberStyles.Float, invariant, out mm) || mm <= 0 || mm > 1000)
            refused.Add("Row height '" + rowHeightMm.Trim() + "' is not a height in millimetres above 0 - "
                + "left as it was on every schedule");
        else wantHeightFeet = mm / 304.8;
    }

    // ---- embedded schedule --------------------------------------------------
    var embedWord = isLeave(embeddedSchedule) ? "" : embeddedSchedule.Trim();
    var removeEmbedded = false;
    if (embedWord.Length > 0)
    {
        asked++;
        var lower = embedWord.ToLowerInvariant();
        removeEmbedded = lower == "none" || lower == "remove" || lower == "no";
    }

    if (asked == 0)
        refused.Add("nothing was asked - name at least one setting: showTitle, showHeaders, gridLinesOnSheets, "
            + "titleTextType, headerTextType, bodyTextType, stripedRows, stripedRowsOnSheets, stripedRowColour, "
            + "rowHeightMm, includeLinks, filterBySheet or embeddedSchedule. NOTHING WAS CHANGED. (Freeze Header "
            + "is not one: it is a switch of the Revit session, not saved in the model - Modify Schedule/"
            + "Quantities, Freeze Header)");

    // ---- the members not on every release, by name ----------------------------
    var api = typeof(ViewSchedule).Assembly;
    var space = typeof(ViewSchedule).Namespace + ".";
    var onSheetsProperty = typeof(ViewSchedule).GetProperty("UseStripedRowsOnSheets");
    var patternType = api.GetType(space + "StripedRowPattern");
    var setStripeColour = patternType == null ? null
        : typeof(ViewSchedule).GetMethod("SetStripedRowsColor", new[] { patternType, typeof(Color) });
    var getStripeColour = patternType == null ? null
        : typeof(ViewSchedule).GetMethod("GetStripedRowsColor", new[] { patternType });
    var rowHeightProperty = typeof(ViewSchedule).GetProperty("RowHeight");
    var rowOverrideProperty = typeof(ViewSchedule).GetProperty("RowHeightOverride");
    var bySheetProperty = typeof(ScheduleDefinition).GetProperty("IsFilteredBySheet");
    var bySheetValid = typeof(ScheduleDefinition).GetMethod("IsValidCategoryForFilterBySheet", Type.EmptyTypes);
    Func<ViewSchedule, string, string> stripeColour = (s, row) =>
    {
        try { return colourText(getStripeColour.Invoke(s, new[] { Enum.Parse(patternType, row) }) as Color); }
        catch (Exception failure) { return "(unreadable: " + revitSaid(failure) + ")"; }
    };
    Func<ViewSchedule, string> rowHeightNow = s =>
    {
        try
        {
            var feet = (double)rowHeightProperty.GetValue(s, null);
            var over = rowOverrideProperty == null ? "" : ", override " + rowOverrideProperty.GetValue(s, null);
            return (feet * 304.8).ToString("0.##", invariant) + " mm" + over;
        }
        catch (Exception failure) { return "(unreadable: " + revitSaid(failure) + ")"; }
    };
    Func<ViewSchedule, string> rowHeightMmNow = s =>
        ((double)rowHeightProperty.GetValue(s, null) * 304.8).ToString("0.##", invariant) + " mm";

    var handed = 0;
    var schedulesSeen = 0;
    var seenIds = new HashSet<ElementId>();

    foreach (var element in elements ?? new List<Element>())
    {
        if (asked == 0) break;
        if (element == null) continue;
        handed++;
        var schedule = scheduleBehind(element);
        if (schedule == null) continue;
        if (!seenIds.Add(schedule.Id)) continue;
        schedulesSeen++;

        ScheduleDefinition definition;
        try { definition = schedule.Definition; }
        catch (Exception failure) { refused.Add("'" + schedule.Name + "' - its definition could not be read: " + revitSaid(failure)); continue; }

        var label = "'" + schedule.Name + "'";
        var parts = new List<string>();

        // One setting: read, write, read back. `write` returns null when done,
        // or a sentence when this schedule cannot take it.
        Action<string, Func<string>, Func<string>, string> one = (setting, read, write, wanted) =>
        {
            string before;
            try { before = read(); }
            catch (Exception failure) { refused.Add(label + " - " + setting + " could not be read: " + revitSaid(failure)); return; }
            if (before == wanted) { parts.Add(setting + " " + before + " (already)"); return; }
            string cannot;
            try { cannot = write(); }
            catch (Exception failure) { refused.Add(label + " - " + setting + ": Revit refused it: " + revitSaid(failure)); return; }
            if (cannot != null) { refused.Add(label + " - " + setting + ": " + cannot); return; }
            string after;
            try { after = read(); }
            catch (Exception failure) { refused.Add(label + " - " + setting + " was written and could not be read back: " + revitSaid(failure)); return; }
            if (wanted != null && after != wanted)
            {
                refused.Add(label + " - " + setting + ": asked for " + wanted + ", it reads back " + after);
                return;
            }
            parts.Add(setting + " " + before + " -> " + after);
            changed++;
        };

        int value;
        if (wantSwitch.TryGetValue("Show Title", out value))
            one("Show Title", () => yesNoText(definition.ShowTitle), () => { definition.ShowTitle = value == 1; return null; }, yesNoText(value == 1));
        if (wantSwitch.TryGetValue("Show Headers", out value))
        {
            var v = value;
            one("Show Headers", () => yesNoText(definition.ShowHeaders), () => { definition.ShowHeaders = v == 1; return null; }, yesNoText(v == 1));
        }
        if (wantSwitch.TryGetValue("Grid lines on sheets", out value))
        {
            var v = value;
            one("Grid lines on sheets", () => yesNoText(definition.ShowGridLines), () => { definition.ShowGridLines = v == 1; return null; }, yesNoText(v == 1));
        }

        foreach (var pair in wantText)
        {
            var setting = pair.Key;
            var type = pair.Value;
            Func<string> read = () =>
            {
                var id = setting == "Title text" ? schedule.TitleTextTypeId
                    : setting == "Header text" ? schedule.HeaderTextTypeId : schedule.BodyTextTypeId;
                var current = id == null || id == ElementId.InvalidElementId ? null : doc.GetElement(id);
                return current == null ? "(none)" : "'" + nameOf(current) + "'";
            };
            one(setting, read, () =>
            {
                if (!schedule.IsValidTextTypeId(type.Id)) return "Revit says '" + nameOf(type) + "' is not a text type this schedule can use";
                if (setting == "Title text") schedule.TitleTextTypeId = type.Id;
                else if (setting == "Header text") schedule.HeaderTextTypeId = type.Id;
                else schedule.BodyTextTypeId = type.Id;
                return null;
            }, "'" + nameOf(type) + "'");
        }

        if (wantSwitch.TryGetValue("Striped rows", out value))
        {
            var v = value;
            one("Striped rows", () => yesNoText(schedule.HasStripedRows), () => { schedule.HasStripedRows = v == 1; return null; }, yesNoText(v == 1));
        }
        if (wantSwitch.TryGetValue("Striped rows on sheets", out value))
        {
            if (onSheetsProperty == null)
                notApplicable.Add(label + " - Striped rows on sheets: Revit 2020 has no such setting - it arrived at Revit 2021");
            else
            {
                var v = value;
                one("Striped rows on sheets", () => yesNoText((bool)onSheetsProperty.GetValue(schedule, null)),
                    () => { onSheetsProperty.SetValue(schedule, v == 1, null); return null; }, yesNoText(v == 1));
            }
        }
        if (wantColour != null)
        {
            if (setStripeColour == null || getStripeColour == null)
                notApplicable.Add(label + " - Striped row colour: Revit 2020 cannot set it - it arrived at Revit 2021");
            else
            {
                var otherRow = colourRow == "SecondRow" ? "FirstRow" : "SecondRow";
                var otherBefore = stripeColour(schedule, otherRow);
                one("Striped row colour (" + colourRow + ")", () => stripeColour(schedule, colourRow), () =>
                {
                    setStripeColour.Invoke(schedule, new object[] { Enum.Parse(patternType, colourRow), wantColour });
                    return null;
                }, colourText(wantColour));
                parts.Add("the other stripe (" + otherRow + ") " + otherBefore + " -> " + stripeColour(schedule, otherRow));
                if (!schedule.HasStripedRows)
                    parts.Add("striped rows are OFF on this schedule, so the colour does not show until they are on");
            }
        }
        if (wantHeightFeet > 0)
        {
            if (rowHeightProperty == null)
                notApplicable.Add(label + " - Row height: Revit 2020 to 2023 have no schedule row height - it arrived at Revit 2024");
            else
            {
                var wantedText = (wantHeightFeet * 304.8).ToString("0.##", invariant) + " mm";
                var overrideBefore = rowHeightNow(schedule);
                one("Row height", () => rowHeightMmNow(schedule), () =>
                {
                    // ASSUMPTION, settled by the proof: the override is set to
                    // All first, in case a height is ignored while it is None.
                    if (rowOverrideProperty != null)
                    {
                        var overrideType = rowOverrideProperty.PropertyType;
                        rowOverrideProperty.SetValue(schedule, Enum.Parse(overrideType, "All"), null);
                    }
                    rowHeightProperty.SetValue(schedule, wantHeightFeet, null);
                    return null;
                }, wantedText);
                parts.Add("row height and override " + overrideBefore + " -> " + rowHeightNow(schedule));
            }
        }
        if (wantSwitch.TryGetValue("Include elements in links", out value))
        {
            var v = value;
            if (v == 1 && !definition.CanIncludeLinkedFiles())
                refused.Add(label + " - Include elements in links: Revit says this schedule cannot include elements from linked files");
            else
                one("Include elements in links", () => yesNoText(definition.IncludeLinkedFiles),
                    () => { definition.IncludeLinkedFiles = v == 1; return null; }, yesNoText(v == 1));
        }
        if (wantSwitch.TryGetValue("Filter by sheet", out value))
        {
            if (bySheetProperty == null || bySheetValid == null)
                notApplicable.Add(label + " - Filter by sheet: Revit 2020 to 2022 have no such setting - it arrived at Revit 2023");
            else
            {
                var v = value;
                var valid = false;
                try { valid = (bool)bySheetValid.Invoke(definition, null); } catch { valid = false; }
                if (v == 1 && !valid)
                    refused.Add(label + " - Filter by sheet: Revit says this schedule's category cannot be filtered by sheet");
                else
                    one("Filter by sheet", () => yesNoText((bool)bySheetProperty.GetValue(definition, null)),
                        () => { bySheetProperty.SetValue(definition, v == 1, null); return null; }, yesNoText(v == 1));
            }
        }

        if (embedWord.Length > 0)
        {
            Func<string> embeddedNow = () =>
            {
                if (!definition.HasEmbeddedSchedule) return "none";
                var inner = definition.EmbeddedDefinition;
                var category = inner == null ? null : Category.GetCategory(doc, inner.CategoryId);
                return category == null ? "(a category it cannot name)" : "'" + category.Name + "'";
            };
            if (removeEmbedded)
            {
                one("Embedded schedule", embeddedNow, () => { definition.RemoveEmbeddedSchedule(); return null; }, "none");
            }
            else if (!definition.CanHaveEmbeddedSchedule())
            {
                refused.Add(label + " - Embedded schedule: Revit says this schedule cannot have one - an embedded "
                    + "schedule is for a schedule of rooms, spaces and the like");
            }
            else
            {
                var valid = definition.GetValidCategoriesForEmbeddedSchedule();
                var validNames = new List<string>();
                Category chosen = null;
                foreach (var id in valid)
                {
                    var category = Category.GetCategory(doc, id);
                    if (category == null) continue;
                    validNames.Add(category.Name);
                    if (string.Equals(category.Name, embedWord, StringComparison.OrdinalIgnoreCase)) chosen = category;
                }
                if (chosen == null)
                    refused.Add(label + " - Embedded schedule: '" + embedWord + "' is not a category this schedule can embed. "
                        + "It can embed " + string.Join(", ", validNames.OrderBy(n => n).Select(n => "'" + n + "'").ToArray())
                        + ", or 'none' to remove one");
                else if (definition.HasEmbeddedSchedule && embeddedNow() != "'" + chosen.Name + "'")
                    refused.Add(label + " - Embedded schedule: it already embeds " + embeddedNow() + ". Remove it first "
                        + "(embeddedSchedule none) - replacing it here would lose that schedule's fields without a word");
                else
                    one("Embedded schedule", embeddedNow, () =>
                    {
                        if (!definition.IsValidCategoryForEmbeddedSchedule(chosen.Id)) return "Revit says '" + chosen.Name + "' cannot be embedded here";
                        definition.AddEmbeddedSchedule(chosen.Id);
                        return null;
                    }, "'" + chosen.Name + "'");
            }
        }

        appliedParts.Add(label + ": " + (parts.Count == 0 ? "nothing it could set" : string.Join(", ", parts.ToArray())));
    }

    applied = string.Join("; ", appliedParts.ToArray());

    if (asked > 0 && handed == 0)
        refused.Add("no schedule was handed in - nothing was found by that name, and nothing is selected - so "
            + "NOTHING WAS CHANGED. Check the schedule's name, or click it on a sheet");
    else if (asked > 0 && schedulesSeen == 0)
        refused.Add("not one of the " + handed + " element(s) handed in is a schedule, so NOTHING WAS CHANGED. A "
            + "schedule is a VIEW and cannot be selected in the model; what CAN be selected is a schedule PLACED "
            + "ON A SHEET, which this reads through to the schedule behind it");
}
