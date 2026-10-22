#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The visualiser's two preset cards say the same thing, and the read reads.

    python tests/test_visual_presets.py

WHY IT EXISTS - FRAGMENT-ISSUES ROW 5b-1269
-------------------------------------------
APPLY_VISUAL_PRESET (FRG-VIEW-148) gives a view a whole look in one call, and
REPORT_VISUAL_PRESET (FRG-VIEW-149) says what that call would do first. A
fragment is compiled on its own inside Revit and can share no code with
another, so the presets and the way each setting is read are written twice -
once in each impl/any/fragment.cs, between the markers SHARED BLOCK - begin
and SHARED BLOCK - end. Two copies of a preset is how one of them goes stale,
and the read would then describe a look the write does not give. This suite
is what holds the two together.

WHAT IT CHECKS, AS TEXT
  1. THE CARDS. One card declares each capability; APPLY_VISUAL_PRESET is a
     MODIFY and REPORT_VISUAL_PRESET a READ.
  2. THE SHARED BLOCK is there in both files and is the same text, byte for
     byte once line endings are set aside.
  3. THE BLOCK ONLY READS. With comments removed, it assigns to no Revit
     property, calls no Set...( ), no Regenerate and no SetBackground - so the
     read, which carries all of it, cannot change the model through it.
  4. THE READ HAS NO WRITES OUTSIDE THE BLOCK EITHER, and opens no transaction.
  5. THE PRESETS. Every preset in the table is named in both cards' `purpose`,
     and the table holds no other; every setting it uses is one the block
     reads or one named as by hand; "by purpose" is only the render quality
     and output; every number is inside the range Revit's own tool takes - an
     intensity 0 to 100, a colour 0 to 255, an exposure value 0 to 21, a
     white point 1800 to 15000 (SET_RENDER_SETTINGS' ranges).
  6. BY HAND STAYS BY HAND ONLY WHILE REVIT HAS NO MEMBER. The two settings
     the presets leave to the modeller - Cast Shadows and Ambient Shadows -
     are the ones SET_GRAPHIC_DISPLAY_OPTIONS still names as unreachable. If
     that card ever reaches one, this goes red: the preset should then set it.
  7. THE READ'S SEARCHED WORDS HOLD NO CHANGE VERB (READ_VIEW_SCALE's rule).
  8. THE METHOD. brain/skills/visualise-a-view.yaml needs both capabilities,
     and every capability its purpose names in capitals is in its `needs`.

Comments are removed before the code is read, so a rule described in words
does not count.

WHAT IT CANNOT DO
Run either fragment. That Revit takes each setting, that the dusk time is half
an hour before the sunset the Sun Settings dialog shows, and that the picture
looks right are NEEDS REAL REVIT - NEEDS-CHECKING Group IC. A green run here
means the two copies agree, not that either works.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
APPLY = os.path.join(FRAGMENTS, "apply-visual-preset", "impl", "any", "fragment.cs")
REPORT = os.path.join(FRAGMENTS, "report-visual-preset", "impl", "any", "fragment.cs")
DISPLAY = os.path.join(FRAGMENTS, "set-graphic-display-options", "impl", "any", "fragment.cs")
METHOD = os.path.join(ROOT, "brain", "skills", "visualise-a-view.yaml")

BEGIN = "// ==== SHARED BLOCK - begin ===="
END = "// ==== SHARED BLOCK - end ===="

# The settings the block reads, each by the test its reader makes.
READ_KEYS = (
    "Visual Style", "Sun Intensity", "Shadow Intensity", "Silhouettes", "Smooth Lines",
    "Sketchy Lines", "Depth Cueing", "Display Background", "Sun", "Render Quality",
    "Render Output", "Render Lighting", "Render Background", "Exposure",
)
BY_HAND = ("Cast Shadows", "Ambient Shadows")

CHANGE_VERBS = frozenset((
    "set", "sets", "setting", "change", "changes", "changed", "changing",
    "update", "updates", "updated", "updating", "adjust", "adjusts", "adjusted",
    "adjusting", "modify", "modifies", "modified", "modifying", "edit", "edits",
    "edited", "editing", "alter", "alters", "altered", "altering", "apply",
    "applies", "applied", "applying", "make", "makes", "making",
))

failures = []
passes = [0]


def check(ok, what):
    print("  %-5s %s" % ("ok" if ok else "FAIL", what))
    if ok:
        passes[0] += 1
    else:
        failures.append(what)


def read(path):
    # Normalised at the read: a CRLF checkout must not move a match.
    if not os.path.isfile(path):
        return ""
    with io.open(path, encoding="utf-8") as handle:
        return handle.read().replace("\r\n", "\n")


def code_only(text):
    """The C# with its comments removed and its strings kept, so a rule
    written in a comment never satisfies a check."""
    out = []
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c == '"':
            j = i + 1
            while j < n and text[j] != '"':
                j += 2 if text[j] == "\\" else 1
            out.append(text[i:j + 1])
            i = j + 1
        elif text.startswith("//", i):
            while i < n and text[i] != "\n":
                i += 1
        elif text.startswith("/*", i):
            end = text.find("*/", i + 2)
            i = n if end < 0 else end + 2
        else:
            out.append(c)
            i += 1
    return "".join(out)


def strings_out(code):
    """The code with every string's contents blanked, so a word inside a
    message never reads as a call."""
    return re.sub(r'"(?:\\.|[^"\\])*"', '""', code)


def shared(text):
    """The shared block, markers included, or "" when either marker is missing."""
    start, end = text.find(BEGIN), text.find(END)
    if start < 0 or end < 0 or end < start:
        return ""
    return text[start:end + len(END)]


def outside(text):
    """The file without its shared block."""
    block = shared(text)
    return text.replace(block, "") if block else text


def cards():
    import heron_fragment as FRAG
    on_disk, _problems = FRAG.load_all()
    return on_disk


def card_of(on_disk, capability):
    found = [f for f in on_disk.values() if f.data.get("capability") == capability]
    return found[0] if len(found) == 1 else None


# Writes the block and the read must never hold. A property assignment is a
# name, a dot, a capitalised member and "=" that is not "==".
WRITE_CALLS = re.compile(r"\.(Set[A-Z]\w*|Regenerate|SetBackground)\s*\(")
ASSIGNMENT = re.compile(r"\.[A-Z]\w*\s*=(?!=)")


def the_cards(on_disk):
    print("1. The cards")
    apply_card = card_of(on_disk, "APPLY_VISUAL_PRESET")
    report_card = card_of(on_disk, "REPORT_VISUAL_PRESET")
    check(apply_card is not None, "exactly one card declares APPLY_VISUAL_PRESET")
    check(report_card is not None, "exactly one card declares REPORT_VISUAL_PRESET")
    check(apply_card is not None and apply_card.data.get("risk") == "MODIFY",
          "APPLY_VISUAL_PRESET is a MODIFY")
    check(report_card is not None and report_card.data.get("risk") == "READ",
          "REPORT_VISUAL_PRESET is a READ")
    return apply_card, report_card


def the_shared_block(apply_text, report_text):
    print("\n2. The shared block")
    a, r = shared(apply_text), shared(report_text)
    check(bool(a), "APPLY_VISUAL_PRESET has its shared block, both markers")
    check(bool(r), "REPORT_VISUAL_PRESET has its shared block, both markers")
    check(bool(a) and a == r, "the two shared blocks are the same text, byte for byte")
    if a and r and a != r:
        for number, (left, right) in enumerate(zip(a.split("\n"), r.split("\n")), 1):
            if left != right:
                print("        first difference, block line %d:\n          apply:  %s\n          report: %s"
                      % (number, left.strip(), right.strip()))
                break
    return a


def the_block_only_reads(block):
    print("\n3. The shared block only reads")
    code = strings_out(code_only(block))
    calls = sorted(set(WRITE_CALLS.findall(code)))
    check(not calls, "no Set...( ), Regenerate or SetBackground in the block%s"
          % ("" if not calls else " - found " + ", ".join(calls)))
    assigned = sorted(set(m.group(0) for m in ASSIGNMENT.finditer(code)))
    check(not assigned, "no Revit property assigned in the block%s"
          % ("" if not assigned else " - found " + ", ".join(assigned)))
    check("Transaction" not in code, "no transaction in the block")


def the_read_reads(report_text):
    print("\n4. The read has no writes outside the block")
    code = strings_out(code_only(outside(report_text)))
    calls = sorted(set(WRITE_CALLS.findall(code)))
    check(not calls, "no Set...( ), Regenerate or SetBackground in REPORT_VISUAL_PRESET%s"
          % ("" if not calls else " - found " + ", ".join(calls)))
    assigned = sorted(set(m.group(0) for m in ASSIGNMENT.finditer(code)))
    check(not assigned, "no Revit property assigned in REPORT_VISUAL_PRESET%s"
          % ("" if not assigned else " - found " + ", ".join(assigned)))
    check("Transaction" not in code, "REPORT_VISUAL_PRESET opens no transaction")


def table(block):
    """{preset: [(setting, value), ...]} read off the block's C#."""
    code = code_only(block)
    found = {}
    for match in re.finditer(r'new KeyValuePair<string, string\[\]\[\]>\("([^"]+)", new\[\]\s*\{(.*?)\}\),', code,
                             re.S):
        found[match.group(1)] = re.findall(r'new\[\] \{ "([^"]+)", "([^"]*)" \}', match.group(2))
    return found


def in_range(text, low, high):
    try:
        value = float(text)
    except ValueError:
        return False
    return low <= value <= high


def the_presets(block, apply_card, report_card):
    print("\n5. The presets")
    presets = table(block)
    check(len(presets) >= 1, "the table holds presets (%d)" % len(presets))
    for card, name in ((apply_card, "APPLY_VISUAL_PRESET"), (report_card, "REPORT_VISUAL_PRESET")):
        purpose = " ".join(str((card.data if card else {}).get("purpose") or "").split())
        missing = [p for p in presets if p not in purpose]
        check(not missing, "%s's purpose names every preset%s" % (name, "" if not missing else " - not " + ", ".join(missing)))
    code = code_only(block)
    for key in READ_KEYS:
        reader = ('key == "%s"' % key) in code or (key.startswith("Render") and 'key.StartsWith("Render")' in code) \
            or (key == "Exposure" and 'key == "Exposure"' in code)
        check(reader, "the block reads %s" % key)
    for preset, settings in sorted(presets.items()):
        keys = [k for k, _v in settings]
        unknown = [k for k in keys if k not in READ_KEYS and k not in BY_HAND]
        check(not unknown, "%s uses only settings the block reads or names by hand%s"
              % (preset, "" if not unknown else " - " + ", ".join(unknown)))
        check(len(keys) == len(set(keys)), "%s names each setting once" % preset)
        problems = []
        for key, value in settings:
            if value == "by purpose" and key not in ("Render Quality", "Render Output"):
                problems.append("%s is 'by purpose'" % key)
            if key in ("Sun Intensity", "Shadow Intensity") and not in_range(value, 0, 100):
                problems.append("%s %s" % (key, value))
            for rgb in re.findall(r"(\d+),(\d+),(\d+)", value):
                if not all(in_range(x, 0, 255) for x in rgb):
                    problems.append("%s colour %s" % (key, ",".join(rgb)))
            if key == "Exposure":
                pairs = dict((k.strip().replace(" ", ""), v.strip()) for k, v in
                             (p.split("=", 1) for p in value.split(";") if "=" in p))
                if not in_range(pairs.get("value", ""), 0, 21):
                    problems.append("exposure value %s" % pairs.get("value"))
                if not in_range(pairs.get("whitepoint", ""), 1800, 15000):
                    problems.append("white point %s" % pairs.get("whitepoint"))
            if key == "Sun" and not re.match(r"^(still (\d\d:\d\d|sunset-30)|lighting \d+/\d+ relative to (view|model))$",
                                             value):
                problems.append("sun '%s'" % value)
        check(not problems, "%s's values are inside Revit's ranges%s"
              % (preset, "" if not problems else " - " + "; ".join(problems)))
        renders = [k for k in keys if k.startswith("Render") or k == "Exposure"]
        check(not renders or ("Render Quality" in keys and "Render Output" in keys),
              "%s sets the render quality and output whenever it sets any render setting" % preset)
    return presets


def the_by_hand(block):
    print("\n6. By hand only while Revit has no member")
    display = code_only(read(DISPLAY))
    check(bool(display), "SET_GRAPHIC_DISPLAY_OPTIONS' impl is there")
    reachable = re.search(r"var byHand = new List<string>\(\);\s*foreach \(var unreachable in (.*?)\)\s*if", display, re.S)
    unreachable = reachable.group(1) if reachable else ""
    for key in BY_HAND:
        word = "Show Ambient Shadows" if key == "Ambient Shadows" else key
        check('"%s"' % word in unreachable,
              "SET_GRAPHIC_DISPLAY_OPTIONS still names %s as having no API member" % word)
    code = code_only(block)
    check('var byHandKeys = new[] { "Cast Shadows", "Ambient Shadows" };' in code,
          "the block leaves exactly Cast Shadows and Ambient Shadows by hand")


def the_words(report_card):
    print("\n7. The read's searched words hold no change verb")
    data = report_card.data if report_card else {}
    text = " ".join([str(data.get("semantic-identity") or ""), str(data.get("purpose") or "")]
                    + [str(u) for u in (data.get("utterances") or [])])
    said = set(re.findall(r"[a-z]+", text.lower()))
    verbs = sorted(said & CHANGE_VERBS)
    check(bool(text.strip()) and not verbs, "no change verb in REPORT_VISUAL_PRESET's identity, purpose or utterances%s"
          % ("" if not verbs else " - " + ", ".join(verbs)))


def the_method():
    print("\n8. The method")
    import yaml
    data = yaml.safe_load(read(METHOD)) or {}
    needs = [str(n) for n in (data.get("needs") or [])]
    for capability in ("APPLY_VISUAL_PRESET", "REPORT_VISUAL_PRESET"):
        check(capability in needs, "visualise-a-view needs %s" % capability)
    named = set(re.findall(r"\b[A-Z][A-Z0-9]+(?:_[A-Z0-9]+)+\b", str(data.get("purpose") or "")))
    missing = sorted(c for c in named if c not in needs)
    check(not missing, "every capability the method's purpose names is in its needs%s"
          % ("" if not missing else " - not " + ", ".join(missing)))


def main():
    on_disk = cards()
    apply_card, report_card = the_cards(on_disk)
    apply_text, report_text = read(APPLY), read(REPORT)
    block = the_shared_block(apply_text, report_text)
    the_block_only_reads(block)
    the_read_reads(report_text)
    the_presets(block, apply_card, report_card)
    the_by_hand(block)
    the_words(report_card)
    the_method()

    print()
    if failures:
        print("FAILED - %d check(s), %d passed:" % (len(failures), passes[0]))
        for one in failures:
            print("  - %s" % one)
        return 1
    print("PASSED - %d checks. Written as text; how it behaves in Revit is NEEDS REAL REVIT (Group IC)."
          % passes[0])
    return 0


if __name__ == "__main__":
    sys.exit(main())
