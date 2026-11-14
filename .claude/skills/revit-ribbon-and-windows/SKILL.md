---
name: revit-ribbon-and-windows
description: Use when adding, changing, reviewing or fixing anything the user sees inside Revit - a ribbon tab, panel, push button, icon, tooltip or its wording, and any tool window whether modal or modeless. Covers the ribbon build order and why a button appears greyed out or missing, the ExternalEvent pattern that a modeless window must use to reach the Revit API without crashing, keeping API calls out of code-behind, dockable panes, the wording and confirmation rules for a button that does something risky, and the brand every colour, font and icon must come from (D-149). Trigger on "add a button", "my button is greyed out", "the ribbon did not appear", "build a window for this tool", "what colour should this be", "draw an icon", "my window crashes Revit", or "outside API context".
---

# Ribbon and Windows

Everything the user sees inside Revit. Read the `revit-addin-conventions` skill first — the threading
constraint and the message-wording rules there apply here and are the two things most often got wrong.

## The ribbon

Built once, in `OnStartup`, from `IExternalApplication`. Nothing else may build it.

```csharp
try { application.CreateRibbonTab(TabName); }
catch (Autodesk.Revit.Exceptions.ArgumentException) { /* another add-in already made it */ }

var panel = application.CreateRibbonPanel(TabName, PanelName);

var button = new PushButtonData(
    "HeronConnect",                       // internal name, unique, never shown
    "Connect\nHeron",                     // the label. \n controls where it wraps
    Assembly.GetExecutingAssembly().Location,
    typeof(ConnectCommand).FullName);     // typeof, never a hand-typed string

button.ToolTip = "Start the Heron bridge for this Revit session.";
button.LongDescription = "Opens a local named pipe so Heron can reach this session.";
panel.AddItem(button);
```

**Creating the tab throws if it already exists.** Catch it and carry on — another add-in, or a reloaded
Heron, may have made it first.

**Use `typeof(X).FullName`, never a literal class name.** A renamed class then breaks the build instead
of producing a button that fails at click time with a message the user cannot act on.

### A button that is also a state

Revit gives a `PushButton` no on/off state of its own. For something that toggles — connected or not,
running or not — **the button's own picture is the state**, and the command swaps it after every click.

Two things make it work:

```csharp
// 1. Capture the button as the ribbon is built. There is no way to look one
//    up afterwards, so if it is not kept here it cannot be reached again.
BridgeButton = panel.AddItem(toggle) as PushButton;

// 2. Swap both images after every toggle, and at startup.
button.LargeImage = loader.LoadLarge(connected ? ConnectedIcon : DisconnectedIcon);
button.Image      = loader.LoadSmall(connected ? ConnectedIcon : DisconnectedIcon);
```

Set the icon from **what the state actually is**, never from what was just attempted — read it back from
the thing itself. An operation that half-succeeded and rolled itself back must not leave the button
claiming otherwise.

A persistent picture beats a dialog here: it is visible at a glance, it is still visible ten minutes
later, and it needs no dismissing. This is the case that makes "no success popup" easy to obey — there
is something better to use instead.

Icons load from a `Resources` folder deployed beside the assembly. Normalise them to 96 DPI: Revit sizes
a ribbon image from its DPI, not its pixels, so an icon exported at 72 or 144 renders at the wrong size
while looking perfectly correct in any image viewer.

**A SPLIT BUTTON EATS A STATE INDICATOR UNLESS YOU TURN ONE THING OFF.** Putting the toggle and its
detail view in one `SplitButtonData` is right — connecting and asking about the connection are the same
subject at two depths — but a split button **promotes whatever was last picked from its list to the
top**. When the top button's picture IS the state, one look at the detail view replaces the state
indicator with something that has no state, **and it never comes back**:

```csharp
var group = panel.AddItem(new SplitButtonData("HeronBridge", "Heron")) as SplitButton;
BridgeButton = group.AddPushButton(toggle);   // AddPushButton hands back the live button
group.AddPushButton(status);
group.IsSynchronizedWithCurrentItem = false;  // or the picture stops being the state
```

Note that a split button's children are added with `AddPushButton`, which returns the live button —
`panel.AddItem` is for a button that stands on its own, and both routes have to hand something back,
because there is still no way to look a button up later.

### When the button does not appear

Work down this list; it is ordered by how often each is the real cause.

1. **The manifest is not where Revit looks.** Per-user is `%APPDATA%\Autodesk\Revit\Addins\<version>\`.
   The add-in log is the fastest way to tell: if there is no log at all, Revit never loaded the assembly.
2. **The assembly path inside the manifest is wrong.** `tools/deploy-addin.ps1` rewrites it at deploy
   time to point at the subfolder it copies into.
3. **`OnStartup` threw.** Revit disables the add-in and the ribbon never gets built. Wrap `OnStartup` so
   a failure is visible and inert, never fatal — a broken add-in must not take Revit down with it.
4. **Built for the wrong framework family.** A `net48` assembly will not load in Revit 2025. See the
   `revit-version-support` skill.
5. **Revit was open during deploy.** A loaded assembly cannot be replaced, so the copy half-succeeds and
   looks like it worked. `tools/deploy-addin.ps1` and `tools/setup.ps1` both refuse while that
   release's Revit is open, for this reason - one check, `tools/HeronRevit.ps1`'s, made per release; a
   Revit whose release cannot be told blocks every one.

### When the button is greyed out

A `PushButton` is disabled when its command's availability class says so, and — for most commands —
when no document is open. A command that genuinely needs no document must say so, or it will be dead on
the start screen where the user first looks for it.

## Windows

### Modal

A modal dialog blocks Revit until it closes, so it runs inside the API context and may call the API
directly. Simple, and correct for a short question or a confirmation.

### Modeless — the one that crashes Revit

A modeless window stays open while the user keeps working. Its event handlers therefore run **outside**
the API context, and calling the Revit API from one throws *"Attempting to create an ApplicationEntity
outside of API context"* or takes Revit down outright.

> **A modeless window may never call the Revit API directly.** It raises an `ExternalEvent`; the handler
> runs on Revit's thread and does the work.

```csharp
public sealed class HeronEventHandler : IExternalEventHandler
{
    public Action<UIApplication> Work { get; set; }

    public void Execute(UIApplication app)      // runs on Revit's thread, in context
    {
        var job = Work;
        if (job != null) job(app);
    }

    public string GetName() { return "Heron"; }
}

// created once, on the Revit thread, during OnStartup
_handler = new HeronEventHandler();
_event   = ExternalEvent.Create(_handler);

// from the window, any thread:
_handler.Work = app => { /* API calls are safe here */ };
_event.Raise();
```

Four things to hold on to:

- **`Raise()` is a request, not a guarantee.** Revit runs the handler when it is idle. With a modal
  dialog open it will not run at all — the caller must time out cleanly and say *"Revit is busy"*.
- **Create the event on the Revit thread**, during startup. Not lazily from a background thread.
- **Never cache a `Document`** in the window. Ask the handler for the current one each time. A cached
  document is how a tool ends up writing to a model the user closed twenty minutes ago.
- **One event, one queue, one handler** for the whole add-in — [D-09](../../../docs/DECISIONS.md). Not
  one per window.

### Keep the API out of code-behind

Code-behind handles the click and calls a service. The service does the Revit work. This keeps the
window testable, keeps the threading rule in one place, and stops API calls quietly appearing in an
event handler where they will crash.

### Dockable panes

Registered in `OnStartup` and never after — Revit will not accept one later. Same threading rule: a
dockable pane is modeless.

**A pane button that must reach Revit goes through the ONE dispatcher, from a background thread**
([D-163](../../../docs/decisions/D-163.md), the Activity pane): `HeronApplication.DispatchFromPane` hands
the request to `RevitDispatcher.Dispatch`, which queues it and raises the one `ExternalEvent` - the same
gate, model hold and audit trail a chat's request meets. Called from `Task.Run`, never from the click
handler: `Dispatch` waits for Revit's thread, which is the click's own thread, so waiting there waits for
itself. The answer comes back with the pane's `Dispatcher.BeginInvoke`. A pane that needs no Revit work -
the Commands pane - starts a process instead and never dispatches at all.

## Colours, font and icons - the brand

**Every colour, the font and every icon come from [`docs/brand/README.md`](../../../docs/brand/README.md)**
([D-149](../../../docs/decisions/D-149.md)). Its colour table is the whole palette: Ink, Mist, Accent,
Accent Light (a gradient's end only), Surface, Success, Warning, Error, White and a see-through Shadow -
every one taken from the owner's brand kit, none chosen by a developer. A shade is one of those at an opacity.
**A colour that is not in that table is never typed into a window, banner or icon.** If one is missing,
ask the owner and add it to the table first.

- **Windows take their colours from `HeronWindowStyle`,** and `HeronWindowStyle` takes them from the
  table. A window that builds its own `Color.FromRgb` is the drift D-149 was written to stop.
- **The ribbon holds three things ([D-150](../../../docs/decisions/D-150.md)):** the Heron button, whose
  menu has Bridge Status, Free this model, Commands (the saved-commands pane,
  [D-151](../../../docs/decisions/D-151.md), Proposed) and Activity (Heron's recent changes,
  [D-163](../../../docs/decisions/D-163.md)) as text rows; Settings, the window that holds
  Changes, Admin, Publish, Ask to Keep and - since D-150 was amended on 2026-11-08 - the Companion's
  on/off, each state as its own button; and Companion, one push button. A new switch goes in Settings,
  not on the ribbon. Hover texts are one short sentence.
- **Accent (orange) is the highlight, never the danger.** A padlock that is open - Heron may change the
  model - is Error. Closed and safe is Ink, Mist or a quiet shade. A state icon keeps its meaning when it
  is redrawn: only its colours move to the palette.
- **The font is Manrope,** with Segoe UI as the fallback until a window carries it.
- **Logos come from `docs/brand/`.** The main Heron button carries the heron mark; nobody draws a new one.
- **A Revit model's colours are not the brand.** What a fragment writes into a model follows the
  project's standard, not this table.

Before you call a window or icon done, run `python tools/check-brand.py`: it names every colour outside
the table, with the file and line.

## Wording and safety

The label on a button is the whole of the user's documentation. Say what it does, in their words.

| | |
|---|---|
| Label | Two short words. `\n` chooses the wrap point |
| ToolTip | One sentence — what it does |
| LongDescription | Two or three sentences — what it does, and anything surprising about it |

**Anything that deletes, moves, renames, overwrites or bulk-edits must confirm first**, stating what will
happen and to how many elements — *"This will move 247 ducts up 200 mm. 12 are owned by another user
and will be skipped."* That is a safety gate, not an information message, and it is one of the few
dialogs that is always allowed.

**A read-only button never confirms and never announces success.** No "Done", no "Connected". If the
user needs to see the result, show it in a status line, a list, or the log.

## An honest report

When a tool finishes, say what actually happened — how many were affected, how many were skipped, and
why. Never report a count without naming the document it came from: *"Selected 126 ducts in
Tower-A.rvt"*, never just *"selected 126 ducts"*. Which model was touched is the thing the user most
needs to be sure of, and the field notes show it is where silent wrong-model damage begins.
