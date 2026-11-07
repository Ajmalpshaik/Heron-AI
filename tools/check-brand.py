# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Which colours does Heron show that the brand's palette does not have?

    python tools/check-brand.py             # every finding, file and line
    python tools/check-brand.py --summary   # one line per file
    python tools/check-brand.py --strict    # exit 1 on a finding

A REPORT: it exits 0 whatever it finds, 2 when it could not look - no git,
or no palette table it can read. A 2 is never a pass. --strict makes a finding
exit 1, for the day the existing parts are in line and this becomes a gate.

WHY IT EXISTS (D-149)
---------------------
Each part of Heron picked its own colours as it was built: the add-in's windows
highlight in blue, the activity banner keeps sixteen colours of its own, the
Companion page and each report have their own palettes. On 2026-11-07 the owner
made the designer's brand kit the one palette for everything Heron shows, now
and in future. A rule in prose is followed until somebody forgets it; this
names the colour, the file and the line.

THE PALETTE IS READ, NEVER COPIED
---------------------------------
The colours are the hex values in the table under "## The colours" in
docs/brand/README.md. This file holds no list of its own, so the palette
cannot drift between the page and the check. A colour is in the palette at any
opacity: #FD641A, #FD641A80 and rgba(253, 100, 26, 0.5) are all Accent.

WHAT IT READS, AND WHAT IT LEAVES OUT
-------------------------------------
Tracked files under revit/, mcp/, brain/ and platform/ - the add-in's windows,
banner and ribbon, the Companion page, the reports, the installer. It finds
#RGB (in .css, .html and .js only - elsewhere a # is too often an issue number),
#RRGGBB and #RRGGBBAA, CSS rgb()/rgba(), and C#'s Color.FromRgb and
Color.FromArgb.

brain/fragments/ is left out on purpose: a fragment writes the MODEL's colours
- a duct by its system, a room by its department - and those follow the
project's standard, not the brand (D-149 item 6). tests/, docs/ and tools/ are
left out because a modeller never sees them.

WHAT IT CANNOT SEE
------------------
A colour built at run time from numbers it cannot read here, a named colour
(Colors.Red, Brushes.White, "red" in CSS), and the pixels of a PNG icon. A
clean run means no typed colour is outside the palette - not that every
window looks right. Look at it in Revit.
"""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PALETTE_PAGE = "docs/brand/README.md"

SCANNED = ("revit/", "mcp/", "brain/", "platform/")
LEFT_OUT = ("brain/fragments/",)
EXTENSIONS = (".cs", ".xaml", ".py", ".html", ".css", ".js", ".ts", ".tsx")
SHORT_HEX_OK = (".css", ".html", ".js")

HEX = re.compile(r"(?<![0-9A-Za-z&])#([0-9A-Fa-f]{8}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})(?![0-9A-Za-z])")
# Not after a letter: C#'s Color.FromArgb( ends in "rgb(" too.
CSS_RGB = re.compile(r"(?<![A-Za-z])rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})")
NUM = r"\s*(0x[0-9A-Fa-f]{1,2}|\d{1,3})\s*"
FROM_RGB = re.compile(r"Color\.FromRgb\(" + NUM + "," + NUM + "," + NUM + r"\)")
FROM_ARGB = re.compile(r"Color\.FromArgb\(" + NUM + "," + NUM + "," + NUM + "," + NUM + r"\)")


def out(text: str) -> None:
    sys.stdout.write(text)


def read_palette() -> dict[str, str]:
    """{'FD641A': 'Accent', ...} from the table under '## The colours'."""
    path = os.path.join(ROOT, *PALETTE_PAGE.split("/"))
    try:
        with open(path, encoding="utf-8") as handle:
            text = handle.read()
    except OSError:
        return {}
    section = re.search(r"^## The colours\s*$(.*?)(?=^## )", text, re.M | re.S)
    if not section:
        return {}
    palette = {}
    for row in section.group(1).splitlines():
        match = re.match(r"\|\s*\*\*([^*]+)\*\*\s*\|\s*`#([0-9A-Fa-f]{6})`", row)
        if match:
            palette[match.group(2).upper()] = match.group(1).strip()
    return palette


def tracked_files() -> list[str] | None:
    try:
        listing = subprocess.run(["git", "ls-files"], cwd=ROOT, capture_output=True,
                                 text=True, check=True).stdout
    except (OSError, subprocess.CalledProcessError):
        return None
    files = []
    for name in listing.splitlines():
        if not name.startswith(SCANNED) or name.startswith(LEFT_OUT):
            continue
        if name.lower().endswith(EXTENSIONS):
            files.append(name)
    return files


def to_hex(red: int, green: int, blue: int) -> str | None:
    if max(red, green, blue) > 255:
        return None
    return "%02X%02X%02X" % (red, green, blue)


def number(text: str) -> int:
    return int(text, 16) if text.lower().startswith("0x") else int(text)


def colours_in(line: str, extension: str) -> list[str]:
    """Every colour typed on this line, as RRGGBB."""
    found = []
    for match in HEX.finditer(line):
        digits = match.group(1)
        if len(digits) == 3:
            if extension not in SHORT_HEX_OK:
                continue
            digits = "".join(ch * 2 for ch in digits)
        found.append(digits[:6].upper())
    for match in CSS_RGB.finditer(line):
        colour = to_hex(*(int(g) for g in match.groups()))
        if colour:
            found.append(colour)
    for match in FROM_RGB.finditer(line):
        colour = to_hex(*(number(g) for g in match.groups()))
        if colour:
            found.append(colour)
    for match in FROM_ARGB.finditer(line):
        colour = to_hex(*(number(g) for g in match.groups()[1:]))
        if colour:
            found.append(colour)
    return found


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0].strip())
    parser.add_argument("--summary", action="store_true", help="one line per file")
    parser.add_argument("--strict", action="store_true", help="exit 1 on a finding")
    args = parser.parse_args()

    palette = read_palette()
    if not palette:
        out("NOT RUN: no colour table under '## The colours' in %s - nothing to check against.\n"
            % PALETTE_PAGE)
        return 2
    files = tracked_files()
    if files is None:
        out("NOT RUN: git ls-files failed - this is not a checkout.\n")
        return 2

    out("Palette, read from %s: %s\n\n" % (
        PALETTE_PAGE, ", ".join("%s #%s" % (name, hx) for hx, name in palette.items())))

    findings: dict[str, list[tuple[int, str]]] = {}
    unreadable = []
    for name in files:
        extension = os.path.splitext(name)[1].lower()
        try:
            with open(os.path.join(ROOT, name), encoding="utf-8") as handle:
                lines = handle.read().splitlines()
        except (OSError, UnicodeDecodeError):
            unreadable.append(name)
            continue
        for number_, line in enumerate(lines, 1):
            for colour in colours_in(line, extension):
                if colour not in palette:
                    findings.setdefault(name, []).append((number_, colour))

    distinct = sorted({colour for rows in findings.values() for _, colour in rows})
    total = sum(len(rows) for rows in findings.values())
    for name in sorted(findings):
        rows = findings[name]
        if args.summary:
            out("  %4d  %s\n" % (len(rows), name))
            continue
        out("%s\n" % name)
        for line_no, colour in rows:
            out("  line %d: #%s\n" % (line_no, colour))
    if unreadable:
        out("\nNot read (not UTF-8 or unreadable): %s\n" % ", ".join(unreadable))

    out("\n%d file(s) read; %d colour(s) outside the palette in %d file(s), %d distinct.\n"
        % (len(files), total, len(findings), len(distinct)))
    if not findings:
        out("Every typed colour is in the palette. That is not the same as every window\n"
            "looking right - the checker cannot see a PNG or a named colour.\n")
        return 0
    out("Each one is either brought into the palette (D-149) or, if the brand truly\n"
        "lacks it, asked of the owner and added to the table first.\n")
    return 1 if args.strict else 0


if __name__ == "__main__":
    sys.exit(main())
