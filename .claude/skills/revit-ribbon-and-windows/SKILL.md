---
name: revit-ribbon-and-windows
description: Use when adding, changing, reviewing or fixing anything the user sees inside Revit - a ribbon tab, panel, push button, icon, tooltip or its wording, and any tool window whether modal or modeless. Covers the ribbon build order and why a button appears greyed out or missing, the ExternalEvent pattern that a modeless window must use to reach the Revit API without crashing, keeping API calls out of code-behind, dockable panes, and the wording and confirmation rules for a button that does something risky. Trigger on "add a button", "my button is greyed out", "the ribbon did not appear", "build a window for this tool", "my window crashes Revit", or "outside API context".
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
   looks like it worked. Both scripts refuse to run while Revit is running, for this reason.

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
