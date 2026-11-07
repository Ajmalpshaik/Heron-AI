// Heron-Agent:  HERON-REVIT-UI-022
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The one question Heron asks before it may change anything -
    /// HERON-REVIT-UI-022, and the visible half of D-19.
    ///
    /// SINCE 2026-11-07 IT IS DRAWN BY HeronSwitchWindow, in the short form the
    /// owner asked for that day - "no one will read this much, make it simple
    /// and powerful": the padlock, one question, one line, and two ticks. The
    /// rules that made this window right are HeronSwitchWindow's too and are
    /// kept: amber, not red and not green; NO DEFAULT BUTTON, so leaning on
    /// Enter grants nothing; Esc and the X both leave it off; it asks on the
    /// way on and never on the way off (D-19's shape).
    ///
    /// EVERY TICK IS TRUE IN THE CODE: each change from a chat is one named
    /// undo step (D-126's group), and Ask to Keep, when on, shows each change
    /// first for Keep or Discard (D-138).
    ///
    /// IT TOUCHES NO REVIT API and decides nothing. It returns what the person
    /// said - NULL when the window could not draw, which is not "no" - and the
    /// command does the rest.
    /// </summary>
    internal static class HeronChangesWindow
    {
        internal static bool? Ask(Action<string> log)
        {
            return HeronSwitchWindow.Ask(new HeronSwitchQuestion
            {
                Name = "Changes",
                Icon = HeronApplication.WriteUnlockedIcon,
                Headline = "Turn Changes on?",
                What = "Heron can move, edit and create elements when you ask.",
                Promises = new[]
                {
                    "Every change is one Ctrl+Z.",
                    "Ask to Keep can show each change first.",
                },
                TurnOn = "Turn on",
            }, log);
        }
    }
}
