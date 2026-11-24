#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-SRV-001
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
The values a fragment door sends, and the lines it prints of what came back:

    _values_array      "name=value" lines into the {name, value} objects the
                       add-in reads (D-54: values cross as text)
    _ignored_lines     the values a capability did not take, said as IGNORED
    _provides_lines    what a fragment left in its reply, as lines a person reads

NOT RUN, AND NO TOOL HERE. The doors - revit_change, revit_read, revit_show -
call all three, and revit_offer_settings, heron_commands and heron_checklist
the first. Every one of those stays in heron_mcp_server.py, which imports this
file near its top. A file that holds no tool registers nothing, so where it is
imported changes nothing a host is served (heron_tools.served_order).

WHY THEY ARE HERE. They sat in heron_mcp_server.py until 2026-11-24 and were
moved whole and unchanged, to make that file smaller. They need nothing from
any file of the server, so this one imports none - and never the entry file,
which runs as `__main__` and would run a second time.

The entry file imports each of them by name, so `heron_mcp_server._values_array`
is still there for a suite that reads it off that module.
"""


def _values_array(values):
    """
    "name=value" lines into the {name, value} objects the add-in reads.

    D-54: caller values cross as TEXT and are typed on the far side, where the
    model is. A view NAME is not a view until something looks it up, and only
    Revit can do that.

    A NEWLINE SEPARATES VALUES AND A SEMICOLON DOES NOT, which is the whole
    repair here. This line used to run every semicolon through a replace into
    a line break before splitting, so a value whose own text carries
    semicolons was torn into pieces.

    THAT IS EXACTLY WHAT `overrides` IS. OneOverride in RevitFragment.cs
    documents the separator it expects - "SEMICOLONS BETWEEN SETTINGS,
    because a colour is already three comma-separated numbers and a comma
    cannot do both jobs" - so the two sides disagreed about what a semicolon
    meant, and this side won:

        overrides=cut-line-colour=255,0,0; projection-line-colour=255,0,0

    arrived as `overrides` carrying ONLY `cut-line-colour=255,0,0`, plus a
    second value named `projection-line-colour` that matches no declared need
    and is dropped without a word. The caller is told the whole thing was
    applied. Found on 2026-09-22 against a real model: walls asked to go red
    stayed white through four separate writes, because the only setting that
    survived was whichever happened to be written first - and in a floor plan
    that was the projection colour, which a CUT wall never draws.

    The tool's own docstring has always said one "name=value" per line. This
    makes the code agree with it.

    A VALUE IN DOUBLE QUOTES KEEPS ITS OWN SPACES (rows 5b-215 and 5b-1475,
    decided by the owner on 2026-11-07). Every value is stripped, which is
    right for "view = Level 1" and wrong for a prefix or a find-and-replace
    whose space is the point: text="Warehouse - " arrived as "Warehouse -",
    the rename it drove left a leading space Revit dropped, and a rename that
    was done came back unverified. So a value written whole in one pair of
    double quotes arrives as exactly what is inside them - `replacement=""` is
    empty, `prefix=" L1"` keeps its space. Unquoted values are stripped as
    before, a quote inside a value or a lone quote is text, and a value that
    must carry its own quotes is written in a second pair: `""Level 1""`.
    """
    out = []
    for piece in (values or "").splitlines():
        piece = piece.strip()
        if not piece or "=" not in piece:
            continue
        name, text = piece.split("=", 1)
        text = text.strip()
        if len(text) >= 2 and text[0] == '"' and text[-1] == '"':
            text = text[1:-1]
        if name.strip():
            out.append({"name": name.strip(), "value": text})
    return out


def _ignored_lines(undeclared, takeable):
    """
    What the reply says about a value no need declared - both doors, one copy.

    `bridge.undeclared_values` decides WHICH names were dropped (row 71's rule,
    in one place); this is only how that is said. revit_change and revit_read
    both print it, and two copies of the sentence would drift the way two
    copies of the rule once did.

    IT IS PRINTED WITH THE RESULT rather than before it, because the dangerous
    case is not the refusal - it is the clean success that did nothing by the
    name you typed.
    """
    if not undeclared:
        return []
    one = len(undeclared) == 1
    return [
        "",
        "  IGNORED: '%s' - %s not %s this capability takes, so %s dropped."
        % ("', '".join(undeclared),
           "is" if one else "are",
           "a value" if one else "values",
           "it was" if one else "they were"),
        "  It takes: %s"
        % (", ".join(takeable) if takeable else "no caller values at all"),
        "  A mistyped name and an invented one look the same here, so "
        "check the spelling before trusting what this run did.",
    ]


def _provides_lines(provides, entries=None, items=None):
    """
    What a fragment left behind, one aligned line each - both doors, one copy.

    The add-in's `Report` has already summarised every value - a collection
    arrives as its count and its first three names - so nothing here can
    carry the model in bulk (D-26). Empty when the fragment left nothing.

    A DICTIONARY ARRIVES AS ITS SIZE AND NOTHING ELSE, AND THE REPLY SAYS SO.
    `RevitFragment.Describe` renders one as "3 entry(ies)" - no first three,
    no "...", nothing to show anything was left behind - so a size read as
    the whole answer. Measured 2026-09-23 in Project1, Revit 2024:
    REPORT_CONNECTORS answered `connectorFacts 1 entry(ies)`, `connections 1
    entry(ies)` and `openConnectors 1 entry(ies)`, and where each connector
    sat and which way it faced never reached the chat. So every name that
    held something is named on one NOT SENT line, and neither the modeller nor
    the assistant takes a size for the contents. FRAGMENT-ISSUES 5b-195.

    SAID HERE, NOT IN THE ADD-IN. The same "N entry(ies)" is what every proof
    record carries and what heron_validate._as_count reads as a quantity, so
    words added in `Describe` would change every proof taken after them - and
    a change there costs a rebuild and a Revit restart on every release. It
    is recognised by its exact shape, a whole number and that one word, so a
    fragment's own sentence that happens to say "entry(ies)" is left alone.

    THE ENTRIES THEMSELVES TRAVEL BESIDE THE COUNT, SINCE 2026-10-22. The
    add-in's `Report` sends `providesEntries` - for every dictionary that
    holds something, EVERY entry as "key": "value" (its first twelve until
    2026-10-28, row 5b-1341), each side whole to 2,000 characters (cut at 60,
    as a summary's sample is, until 2026-11-20 - row 5b-194) - and
    `Describe` is left byte for byte as it was.
    `entries` is that field. Each entry prints on its own line under the
    count, "    14194: Yes", and a dictionary larger than what arrived says
    how many more it holds. Measured 2026-10-22 on Revit 2020 (session
    78736): READ_ELEMENT_PARAMETERS asked for one parameter on four connectors
    answered `values 4 entry(ies)` and `resolvedFrom 4 entry(ies)` and not one
    value reached the chat (FRAGMENT-ISSUES 5b-1325). NOT SENT is now said
    only of a dictionary whose entries did NOT arrive - an add-in from before
    this field - so an older add-in still says what it withheld.

    A VALUE THAT IS CODE, NOT DATA, IS NAMED AND NOT PRINTED. `Report` hands
    over every variable a fragment's script declared, and many hold a helper:
    a lambda (`Func`2`), `CultureInfo`, `BindingFlags`, a namespace string, or
    a list whose items print as `System.Int32[]`. None says anything about the
    model. Measured on a 107-form family on 2026-10-09: 29% of the 322,725
    characters revit_change returned were such lines - context every later
    turn carried (5b-407). They are recognised by shape and listed by NAME on
    one line, so nothing disappears unannounced. Only these go: a value the
    card does not declare is still printed when it holds data - 317 fragments'
    proofs show undeclared values that are real results (ids, counts, members),
    so "declared only" would have hidden answers.

    A LIST OF SENTENCES ARRIVES WHOLE BESIDE ITS SUMMARY, SINCE 2026-11-07.
    `Describe` shows a list as "N item(s) [a, b, c, ...]" - three items, each
    cut at 60 characters - and that line stays as it is. Where it hid
    something, the add-in sends `providesItems`: that list's text items,
    numbered, each in full (to 2,000 characters, the first 100). `items` is
    that field, and each prints on its own line under the count. Measured
    2026-11-07 on Revit 2024: APPLY_VISUAL_PRESET's finding "Replaced what
    '{3D}' had: Visual Style Hidden Line -> Realis..." - the sentence group
    MQ's MQ5 checks - could not be read anywhere (FRAGMENT-ISSUES 5b-1467).
    """
    if not isinstance(provides, dict) or not provides:
        return []

    def is_code(value):
        text = str(value).strip()
        head = text.split(" ", 1)[0]
        if head.isdigit() and text[len(head):].startswith(" item(s) ["):
            shown = text.split("[", 1)[1]
            return shown.startswith(("System.", "Autodesk.", "(System.",
                                     "(Autodesk.", "[System.", "[Autodesk."))
        if not text or " " in text:
            return False
        if "`" in text and text.split("`", 1)[0] in (
                "Func", "Action", "Predicate", "Comparison", "Converter",
                "Tuple", "ValueTuple", "KeyValuePair", "Lazy", "List",
                "HashSet", "Dictionary", "IEnumerable", "IList"):
            return True
        if text.startswith(("System.", "Autodesk.")):
            return True
        return text in ("CultureInfo", "BindingFlags", "RuntimeAssembly",
                        "RuntimeMethodInfo", "RuntimeType", "RuntimePropertyInfo",
                        "RuntimeFieldInfo", "RuntimeConstructorInfo", "Regex",
                        "StringBuilder", "StringComparer", "Stopwatch", "Random")

    code = sorted(str(name) for name in provides if is_code(provides[name]))
    data = dict((name, provides[name]) for name in provides
                if str(name) not in code)

    def dictionary_size(value):
        """How many entries a value's "N entry(ies)" says, or None when it
        is not that exact shape - a fragment's own sentence is left alone."""
        words = str(value).strip().split(" ")
        if (len(words) == 2 and words[1] == "entry(ies)"
                and words[0].isdecimal()):
            return int(words[0])
        return None

    def list_size(value):
        """How many items a value's "N item(s)" summary says, or None."""
        text = str(value).strip()
        head = text.split(" ", 1)[0]
        if head.isdecimal() and text[len(head):].startswith(" item(s)"):
            return int(head)
        return None

    def items_arrived(name):
        """The text items the add-in sent whole for this list, in order - or
        None when none came."""
        if not isinstance(items, dict):
            return None
        sent = items.get(str(name))
        if not isinstance(sent, dict) or not sent:
            return None
        return sent

    def arrived(name):
        """The entries the add-in sent for this dictionary, in the order it
        sent them - or None when none came."""
        if not isinstance(entries, dict):
            return None
        sent = entries.get(str(name))
        if not isinstance(sent, dict) or not sent:
            return None
        return sent

    # A LIST WHOSE EVERY LINE IS PRINTED ELSEWHERE IN THIS REPLY IS NAMED, NOT
    # PRINTED AGAIN (row 5b-1941, issue #1162). The add-in sends every list of
    # text a fragment leaves at its top level, so a card that joins one into an
    # output, or adds it into `findings`, printed each line two or three times -
    # 211 live cards had that shape, 171 of them PROVEN. Said here because it
    # is how the answer is printed: no card's code changes and no proof goes
    # stale. Where a line is printed is a text value, which arrives whole, or
    # another list printed in full - never a list's summary, whose three items
    # are cut at 60 characters. Only a list that arrived WHOLE qualifies: one
    # longer than what arrived has lines nobody here has seen. The longest is
    # kept first, so of two lists with the same lines one still prints, and
    # where a line is printed does not depend on which name sorts first.
    texts = dict((str(name), str(data[name])) for name in data
                 if list_size(data[name]) is None
                 and dictionary_size(data[name]) is None)
    printed_elsewhere = {}
    kept_lists = []

    def word_for_word(line, text):
        """True when `line` stands whole in `text` - "1" inside "1020206"
        is not "1", so a list of short values is never taken as printed.
        Plain string work, so the helper still runs on its own (no `re`)."""
        def joins(char):
            return char.isalnum() or char == "_"
        start = text.find(line)
        while start >= 0:
            end = start + len(line)
            if not (start > 0 and joins(text[start - 1])) \
                    and not (end < len(text) and joins(text[end])):
                return True
            start = text.find(line, start + 1)
        return False

    def whole_list(name):
        sent = items_arrived(name)
        count = list_size(data[name])
        if not sent or count is None or count != len(sent):
            return None
        return sent

    candidates = [name for name in data if whole_list(name)]
    for name in sorted(candidates, key=lambda n: (-len(whole_list(n)), str(n))):
        found_in = []
        for line in whole_list(name).values():
            line = str(line)
            where = [other for other in sorted(texts) if word_for_word(line, texts[other])]
            if not where:
                where = [other for other in kept_lists
                         if line in [str(v) for v in whole_list(other).values()]]
            if not where:
                found_in = None
                break
            if where[0] not in found_in:
                found_in.append(where[0])
        if found_in and any(str(v).strip() for v in whole_list(name).values()):
            printed_elsewhere[str(name)] = found_in
        else:
            kept_lists.append(name)

    lines = [""]
    if data:
        width = max(len(str(name)) for name in data)
        for name in sorted(data):
            lines.append("  %-*s  %s" % (width, name, data[name]))
            whole = items_arrived(name)
            where = printed_elsewhere.get(str(name))
            if where:
                lines.append("    its %d line(s) are printed under %s - not printed again."
                             % (len(whole), " and ".join(where)))
            elif whole:
                for number in whole:
                    lines.append("    %s. %s" % (number, whole[number]))
                count = list_size(data[name])
                if count and count > len(whole):
                    lines.append("    ... and %d more: the first %d of %d are shown."
                                 % (count - len(whole), len(whole), count))
            size = dictionary_size(data[name])
            sent = arrived(name) if size else None
            if not sent:
                continue
            for key in sent:
                lines.append("    %s: %s" % (key, sent[key]))
            if size > len(sent):
                lines.append("    ... and %d more: the first %d of %d are shown. "
                             "Read fewer at a time to see every one."
                             % (size - len(sent), len(sent), size))
    if code:
        lines.append("  NOT PRINTED, holding code rather than data: %s."
                     % ", ".join(code))

    held = []
    for name in sorted(data):
        size = dictionary_size(data[name])
        if size and not arrived(name):
            held.append(str(name))
    if held:
        one = len(held) == 1
        lines.append("  NOT SENT: what is in %s. Only how many entries %s "
                     "reached this reply."
                     % (held[0] if one else
                        ", ".join(held[:-1]) + " and " + held[-1],
                        "it holds" if one else "each holds"))
    return lines
