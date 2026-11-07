# Heron brand

**The logo, the colours and the font Heron AI is shown in.** Pick a file from this folder whenever Heron
needs a logo or icon, so it looks the same everywhere: on GitHub, on the Revit ribbon, in Heron's windows
and on the installer. Don't draw a new one.

The source is the designer's brand kit (2026-11-07). Its style sheet shows the logo on light and dark
backgrounds, the app icon, the three colours and the font.

## The files

| File | What it is | Use it for |
|---|---|---|
| [`heron-logo-ink.svg`](heron-logo-ink.svg) | The heron with "Heron AI - Design Automate Deliver", near-black, transparent background | Anywhere light: the README in GitHub's light theme, documents, a white page |
| [`heron-logo-white.svg`](heron-logo-white.svg) | The same logo in white | Anywhere dark: the README in GitHub's dark theme, a dark slide |
| [`heron-mark-ink.svg`](heron-mark-ink.svg) | The heron alone, near-black | A small place where the words would be unreadable |
| [`heron-mark-white.svg`](heron-mark-white.svg) | The heron alone, white | The same, on a dark background |
| [`heron-app-icon-ink.png`](heron-app-icon-ink.png) | White heron on a near-black square, 512 px | An app or program icon |
| [`heron-app-icon-orange.png`](heron-app-icon-orange.png) | White heron on an orange square, 512 px | An app icon that has to stand out |
| [`heron-social-preview.png`](heron-social-preview.png) | The logo on Mist with an orange line, 1280 x 640 px | The picture GitHub shows when the repository's link is shared. It is uploaded by hand under the repository's **Settings - Social preview**, because GitHub offers no other way |

The SVG files are shapes only, with no embedded text, font or picture, so they stay sharp at any size and
look the same on every machine.

## The colours

| Name | Hex | Where |
|---|---|---|
| **Ink** | `#0F0F10` | Text and the logo on a light background |
| **Mist** | `#F7F5F2` | The light background |
| **Accent** | `#FD641A` | The one highlight colour, used sparingly |

## The font

**Manrope**, in ExtraBold, SemiBold, Medium and Regular. It is not kept in this folder yet: it comes in
with the change that first uses it, together with its licence file.

## The logo is not under the code's licence

Heron's code is under Apache 2.0 ([D-08](../decisions/D-08.md)). Section 6 of that licence grants no
right to use the project's name or marks, so these files may be used to show Heron itself but not to
present another product as Heron.
