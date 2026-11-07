# Heron brand

**The logo, the colours and the font Heron AI is shown in, everywhere and from now on**
([D-149](../decisions/D-149.md)). Revit's windows and ribbon, the Companion page, every report, the
installer and GitHub all take their colours, font and logos from this page. Don't draw a new logo and
don't pick a new colour: if a colour you need is not in the table below, ask the owner, and add it here
before any code uses it.

The source is the designer's brand kit (2026-11-07): a style sheet with the logo on light and dark
backgrounds, the app icon, the three core colours and the font, plus a design for Heron's status
notification that adds the colours for a state - done, needs attention, failed. Every colour below is
in one of those two, by its hex value. The status design's other colours belong to the pretend Windows
desktop it is drawn on (the Recycle Bin, the taskbar, Teams) and are not Heron's.

## The files

| File | What it is | Use it for |
|---|---|---|
| [`heron-logo-ink.svg`](heron-logo-ink.svg) | The heron with "Heron AI - Design Automate Deliver", near-black, transparent background | Anywhere light: the README in GitHub's light theme, documents, a white page |
| [`heron-logo-white.svg`](heron-logo-white.svg) | The same logo in white | Anywhere dark: the README in GitHub's dark theme, a dark slide |
| [`heron-mark-ink.svg`](heron-mark-ink.svg) | The heron alone, near-black | A small place where the words would be unreadable |
| [`heron-mark-white.svg`](heron-mark-white.svg) | The heron alone, white | The same, on a dark background |
| [`heron-3d-orange.png`](heron-3d-orange.png) | The heron in glossy 3D orange, transparent background, 1124 px | The main **Heron** button on the Revit ribbon when Revit is connected (`revit/Heron.Revit.Addin/Resources/BridgeConnected.png` is this, cropped to the bird, 96 px) |
| [`heron-3d-ink.png`](heron-3d-ink.png) | The same heron in glossy 3D black | The **Heron** button when Revit is not connected (`BridgeDisconnected.png`, cropped the same way). Faint on Revit's dark theme, as the icon it replaced was |
| [`heron-app-icon-ink.png`](heron-app-icon-ink.png) | White heron on a near-black square, 512 px | An app or program icon |
| [`heron-app-icon-orange.png`](heron-app-icon-orange.png) | White heron on an orange square, 512 px | An app icon that has to stand out |
| [`heron-social-preview.png`](heron-social-preview.png) | The logo on Mist with an orange line, 1280 x 640 px | The picture GitHub shows when the repository's link is shared. It is uploaded by hand under the repository's **Settings - Social preview**, because GitHub offers no other way |

The SVG files are shapes only, with no embedded text, font or picture, so they stay sharp at any size and
look the same on every machine.

## The colours

**This table is the palette.** [`tools/check-brand.py`](../../tools/check-brand.py) reads its hex values
from here - nowhere else holds a copy - and names every file that shows a colour this table does not have.

| Name | Hex | From | Where |
|---|---|---|---|
| **Ink** | `#0F0F10` | style sheet | Text and the logo on a light background; the dark background |
| **Mist** | `#F7F5F2` | style sheet | The light background; text and the logo on a dark background |
| **Accent** | `#FD641A` | style sheet | The one highlight colour - the thing to look at, the main button. Used sparingly, and never to mean danger |
| **Accent Light** | `#FF9A5C` | status design | Only as the light end of an Accent gradient, as on the Heron app tile - never alone |
| **Surface** | `#1B1B1F` | status design | A panel or card on the dark background |
| **Success** | `#3ECF8E` | status design | Done, passed, connected |
| **Warning** | `#F5A524` | status design | Needs attention, not yet a failure |
| **Error** | `#FF5A4E` | status design | Failed, refused - and a switch that lets Heron change the model, which is the state to notice |
| **White** | `#FFFFFF` | app icon | The heron on the app icon; text on Accent |
| **Shadow** | `#000000` | status design | Only as a see-through shadow, never as a solid colour |

**Shades are see-through versions of these, not new colours.** The status design puts Mist over Ink at
92 % for main text, 72 % for secondary text, 55 % for quiet text, 14 % for a line and 8 % or 4 % for a
faint fill. Any colour above at any opacity is in the palette; a hex value that is not in the table is not.

**A Revit model's colours are not the brand.** A fragment that colours ducts by system, fills a room by its
department or sets a material writes the *model's* colours, chosen by the modeller and the project's
standard. `brain/fragments/` is outside this rule and outside the checker.

## The font

**Manrope**, in ExtraBold, SemiBold, Medium and Regular. It is not kept in this folder yet: it comes in
with the change that first uses it, together with its licence file. Until a window or page has it, the
fallback is the system font - Segoe UI on Windows.

## The logo is not under the code's licence

Heron's code is under Apache 2.0 ([D-08](../decisions/D-08.md)). Section 6 of that licence grants no
right to use the project's name or marks, so these files may be used to show Heron itself but not to
present another product as Heron.
