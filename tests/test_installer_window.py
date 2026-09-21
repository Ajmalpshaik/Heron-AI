#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The installer window, checked the only way a Linux machine can check a window.

WHAT THIS PROVES
----------------
Not that it looks right. It is WPF and nothing here has drawn a pixel. What it
proves is the thing a screenshot would NOT show:

    the window holds no rules - every one of them is in InstallerScreen,
      which tests/test_installer_engine.py runs against a fake Revit
    no product is named in it, so adding one is a line in the manifest - R-3
    there is no Update button and no Repair button - R-23a
    the reason a row is greyed out is PRINTED, not only in a tooltip - R-10
    a row's own text WRAPS rather than clipping in silence - AB1, row 5b-97
    Close Revit first is built BEFORE the Install button, not after a failure
    the waiting happens off the window's thread, so it cannot freeze

WHAT IT CANNOT PROVE
--------------------
That the window appears, that it is readable, or that Install works. Those are
rows AB1 to AB7 in docs/NEEDS-CHECKING.md and need the owner's PC.

    python tests/test_installer_window.py
"""

import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APP = os.path.join(ROOT, "platform", "Heron.Installer.App")
WINDOW = os.path.join(APP, "InstallerWindow.cs")
ENTRY = os.path.join(APP, "Program.cs")
PROJ = os.path.join(APP, "Heron.Installer.App.csproj")
SCREEN = os.path.join(ROOT, "platform", "Heron.Installer", "InstallerScreen.cs")
MANIFEST = os.path.join(ROOT, "platform", "heron-products.json")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    return io.open(path, encoding="utf-8").read()


def code_of(text):
    """The C# with its comments taken out, so a comment cannot pass a check."""
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    return "\n".join(line for line in text.split("\n")
                     if not line.lstrip().startswith("//"))


def main():
    for path in (WINDOW, ENTRY, PROJ, SCREEN):
        if not os.path.exists(path):
            print("FAILED  %s is missing" % path)
            return 1

    window = read(WINDOW)
    entry = read(ENTRY)
    proj = read(PROJ)
    screen = read(SCREEN)
    manifest = json.loads(io.open(MANIFEST, encoding="utf-8-sig").read())

    window_code = code_of(window)
    entry_code = code_of(entry)
    screen_code = code_of(screen)

    print("NOT ONE PRODUCT IS NAMED - R-3, and the whole point of the design")
    # Adding Heron Structure next year must be a line in the manifest and
    # never a rebuild of the installer. A single id written into the window
    # is that promise broken, and it would not look wrong on screen.
    # THE HEADING'S OWN NAME IS EXEMPT, and only that. It is "Heron", which
    # is the platform's name, the window's title and the namespace these
    # files sit in - a check that refused it would be a check about the word
    # rather than about the product. Every other id and name is a product,
    # and a product named in here is R-3 broken.
    heading = None
    for product in manifest["products"]:
        if not product.get("partOf") and not product.get("folder"):
            heading = product["id"]

    for product in manifest["products"]:
        if product["id"] == heading:
            continue
        for where, text in (("the window", window_code),
                            ("the entry point", entry_code),
                            ("the screen model", screen_code)):
            check(product["id"] not in text,
                  "%s never says %r" % (where, product["id"]))
        check(product["name"] not in window_code,
              "the window never says %r either" % product["name"])

    print()
    print("THE WINDOW DECIDES NOTHING - every rule is where it can be tested")
    for rule, why in [
        ("CanBeTicked", "whether a row may be ticked"),
        ("WhyNot", "the sentence saying why it may not"),
        ("IsPiece", "whether it is drawn under a heading"),
        ("ToInstall", "what a tick actually installs"),
        ("ChosenReleases", "which Revit releases were chosen"),
    ]:
        check(rule in screen_code,
              "%s is decided by InstallerScreen" % why)
    check("MayBeOffered" not in window_code and "SupportsRevit" not in window_code,
          "and the window asks a product NOTHING directly")
    check("HeronProduct" not in window_code,
          "it never touches a product at all - only the rows it was handed")
    check("row.IsPiece" in window_code and "PartOf" not in window_code,
          "so what is drawn under what is TOLD to it, not worked out")

    print()
    print("No Update button and no Repair button - R-23a, Stage 4 item 3b")
    # Every Content= string in the file, which is every word the window
    # puts on a control. Update and Repair must be in none of them.
    contents = re.findall(r'Content\s*=\s*"([^"]+)"', window)
    check("Install" in contents and "Close" in contents,
          "Install and Close are both there")
    offered = [c for c in contents
               if "update" in c.lower() or "repair" in c.lower()]
    check(not offered,
          "and nothing says Update or Repair: %s"
          % (", ".join(offered) if offered else "nothing does"))
    check("new Button" in window and window.count("new Button") == 2,
          "there are exactly two buttons, not three")
    check("IfYouInstallAgain" in window,
          "and an installed product is told what Install will do instead")

    print()
    print("A greyed row says why in WORDS, not only in a tooltip")
    check("ToolTip" in window, "the reason is on the tooltip")
    check(window.index("ToolTip") < window.index("row.WhyNot != null"),
          "and printed as well, because nobody hovers over a row they "
          "cannot tick")

    print()
    print("A row's own text WRAPS, so it cannot be clipped in silence - AB1")
    # AB1 FAILED ON A REAL MACHINE on 2026-09-21 and was fixed the same day
    # (#235): the tick box carried a bare string, the window is a fixed 620
    # wide with ResizeMode.CanMinimize, and the AI Bridge row lost its last
    # word with NO ellipsis - "The three buttons that exist today." rendered
    # as "...that exist", which still reads as a finished sentence.
    #
    # WHETHER IT LOOKS RIGHT NEEDS WINDOWS and this suite says so at the top.
    # WHETHER IT CAN WRAP AT ALL DOES NOT. Nothing held the fix: reverting to
    # `Content = row.Name + "    " + row.Description` compiles, passes every
    # suite, and clips again on a machine nobody is watching. Row 5b-97.
    # SEARCHED FROM THE TICK BOX, not from the top of the file. Every other
    # block in this window already wraps, so a bare find() returns the FIRST
    # one - which sits above the tick box and made this check red against the
    # fixed file. The occurrence that matters is the one inside this block.
    tick = window.find("var tick = new CheckBox")
    enabled = window.find("IsEnabled = row.CanBeTicked", tick if tick != -1 else 0)
    wraps = window.find("TextWrapping = TextWrapping.Wrap",
                        tick if tick != -1 else 0)
    # BOTH ENDS FIRST - str.find gives -1 for a string that is not there, and
    # -1 is less than every real position, so a bare ordering check passes
    # LOUDEST when the thing it guards has been deleted. Row 5b-79 paid for
    # that lesson in the suite next door.
    check(tick != -1, "the tick box is built here at all")
    check(wraps != -1,
          "and something in this window wraps its text rather than clipping")
    check(tick != -1 and enabled != -1 and wraps != -1
          and tick < wraps < enabled,
          "and it is THE TICK BOX's own content that wraps, between the "
          "CheckBox and its IsEnabled - not some other block further down")
    # ASKED OF THE TICK BOX, NOT OF THE FILE. `AB1` is named elsewhere in
    # this window too, so a plain `in window` passed against the version
    # that had NOT been fixed - a check that is true either way is not a
    # check. The block itself is what these three are about.
    block = window[tick:enabled] if -1 not in (tick, enabled) else ""
    check("Content = new TextBlock" in block,
          "the content is a TextBlock, not a bare string - a string cannot "
          "wrap and clips without an ellipsis")
    check("AB1" in block,
          "and the block names the row that found it, so the next person to "
          "tidy this knows it was measured rather than guessed")
    # THE FIX THAT WAS REJECTED, kept so it is not tried again. Widening to
    # fit today's longest line clips the next one just as quietly, and R-3
    # promises adding a product is a line in a file.
    check("Widening" in block or "widened" in block,
          "and it records why widening the window was refused")

    print()
    print("Close Revit first is BUILT BEFORE the Install button - item 6")
    check("CloseRevitFirst" in window, "the line is in the window")
    check(window.index("screen.CloseRevitFirst") < window.index('Content = "Install"'),
          "and it is added to the page above the button, not after a failure")

    print()
    print("A tab's tick moves its pieces, and it is wired AFTER the list exists")
    check("Follow(" in window_code, "the tab and its pieces are tied together")
    # A heading is drawn before its pieces, so wiring it inside the drawing
    # loop finds none of them and quietly does nothing. It was written that
    # way first, on 2026-09-21, and a compile and a screenshot would both
    # have looked right.
    check(window_code.index("foreach (var heading in headings)")
          > window_code.index("panel.Children.Add(line);"),
          "and wired after every row is drawn, not while they are being drawn")
    check("piece.IsEnabled" in window_code,
          "a greyed piece is never moved by it - it cannot be installed")

    print()
    print("Waiting cannot freeze the window - R-38a")
    check("new Thread(" in entry_code,
          "the engine runs off the window's thread")
    check("Dispatcher.Invoke" in entry_code,
          "and comes back onto it to say anything")
    check("Waiting" in window_code,
          "a wait is SAID, because a silent one looks like a hang")

    print()
    print("It is a window and nothing else")
    check("<UseWPF>true</UseWPF>" in proj, "WPF")
    check("net8.0-windows" in proj, "on net8.0-windows, which WPF needs")
    check("Heron.Installer.csproj" in proj,
          "and it references the engine rather than repeating it")
    check("InstallPlan" not in window_code and "InstallEngine" not in window_code,
          "the window itself never touches the plan or the engine")
    check("RollForward" in proj,
          "and it rolls forward, so a machine with only a later .NET still "
          "runs it rather than asking the user to install one")

    print()
    if FAILURES:
        print("FAILED (%d)" % len(FAILURES))
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("The window holds no rules, names no product, has no Update button,")
    print("and says what it cannot offer rather than leaving a grey tick box.")
    print()
    print("NO PIXEL HAS BEEN DRAWN. This is a text check on WPF source from a")
    print("Linux machine. Whether the window appears, is readable, and installs")
    print("anything is owed on Windows - AB1 to AB7 in docs/NEEDS-CHECKING.md.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
