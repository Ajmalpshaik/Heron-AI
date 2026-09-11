# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-VAL-009
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

import glob, io, os, re, sys

root = '.'
md = []
for dp, dn, fn in os.walk(root):
    if '.git' in dp:
        continue
    for f in fn:
        if f.endswith('.md'):
            md.append(os.path.join(dp, f).replace(os.sep, '/'))

# The docs carry characters the Windows console codepage cannot encode.
try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    out = sys.stdout.write
except AttributeError:
    out = lambda s: sys.stdout.write(s.encode('ascii', 'replace').decode('ascii'))
out("=== FILES ===\n")
out("markdown files: %d\n\n" % len(md))

# ---------- 1. link integrity ----------
#
# A LINK TO A FILE THAT EXISTS CAN STILL BE DEAD, and this check used to say
# nothing about it. `[the recipe](#9a-continuing-...)` points at a HEADING, not
# a file, and the file-existence test above skipped every one of those - so two
# in-file links written on 2026-08-31 were dead on arrival while this printed
# "BROKEN LOCAL LINKS: 0". They were found by a hand-written probe minutes
# later, which is precisely the reading this checker exists to make unnecessary.
#
# The anchor is derived the way GitHub derives it, and ONE DETAIL DECIDES
# EVERYTHING: each space becomes a hyphen and RUNS ARE NOT COLLAPSED. So a
# heading containing " - " loses the dash and keeps both spaces, giving TWO
# hyphens in the anchor.
#
# That detail was got wrong first, with `\s+` collapsing the run, and the check
# then reported 125 broken links across a repository whose links were fine. The
# number is what gave it away: a checker that suddenly condemns most of the
# corpus is a broken checker, not a broken corpus - which is the rule
# HANDOVER.md 4a states, arriving here the same day it was written.
link = re.compile(r'\[([^\]]*)\]\(([^)]+)\)')
heading = re.compile(r'^#{1,6} (.+)$', re.M)


def anchor_of(text):
    """The GitHub anchor for a heading.

    Each space becomes one hyphen. Runs are NOT collapsed - that is what makes
    " - " produce two hyphens, and getting it wrong condemns every link in the
    repository that points at a heading with a dash in it.
    """
    slug = re.sub(r'[^\w\s-]', '', text.lower())
    return slug.strip().replace(' ', '-')


def anchors_in(path):
    try:
        body = io.open(path, encoding='utf-8').read()
    except (IOError, OSError):
        return set()
    return set(anchor_of(h) for h in heading.findall(body))


bad = []
_anchor_cache = {}
for p in md:
    s = io.open(p, encoding='utf-8').read()
    base = os.path.dirname(p)
    for text, href in link.findall(s):
        if href.startswith(('http://', 'https://', 'mailto:')):
            continue

        tgt, _, fragment = href.partition('#')

        if not tgt:
            # An in-file link: the heading has to exist in THIS file.
            target_file = p
        else:
            target_file = os.path.normpath(os.path.join(base, tgt)).replace(os.sep, '/')
            if not os.path.exists(target_file):
                bad.append((p, href, text[:35]))
                continue

        if not fragment:
            continue
        if target_file not in _anchor_cache:
            _anchor_cache[target_file] = anchors_in(target_file)
        if _anchor_cache[target_file] and fragment not in _anchor_cache[target_file]:
            bad.append((p, href, text[:35]))

out("=== 1. BROKEN LOCAL LINKS: %d ===\n" % len(bad))
for p, h, t in bad:
    out("  %-45s -> %-40s [%s]\n" % (p, h, t))
out("\n")

# ---------- 2. reference targets exist ----------
allsrc = {}
for p in md:
    allsrc[p] = io.open(p, encoding='utf-8').read()
joined = "\n".join(allsrc.values())

failed = False


def defined(pattern, path):
    s = allsrc.get(path, '')
    return set(re.findall(pattern, s))


# AN ID DEFINED TWICE IS WORSE THAN ONE NEVER DEFINED, and until 2026-09-09
# this file could not see one. Every registry below was read with
# `set(re.findall(...))`, and a set is exactly the thing that makes a duplicate
# invisible: two `## D-56` headings collapse into one entry, "REFERENCED BUT NOT
# DEFINED" stays empty, and the checker reports a clean file.
#
# THAT IS NOT HYPOTHETICAL. D-67 was first written as D-56, which already
# existed, and this script passed on a DECISIONS.md carrying two of them. The
# duplicate was found by eye, which is the reading this checker exists to make
# unnecessary.
#
# WHY IT FAILS THE RUN when a broken link only prints. A dead link announces
# itself the moment somebody clicks it. A duplicate id is SILENT and it makes
# every reference to that number ambiguous: `[D-56](DECISIONS.md)` now points at
# two different decisions, and nothing - not this script, not a reader, not the
# anchor - can say which was meant. Both entries look correct in isolation.
def repeats(pattern, path, flags=0):
    """The ids defined more than once in `path`, with their counts."""
    found = re.findall(pattern, allsrc.get(path, ''), flags)
    seen, twice = {}, []
    for one in found:
        seen[one] = seen.get(one, 0) + 1
    for one in found:
        if seen[one] > 1 and one not in [t[0] for t in twice]:
            twice.append((one, seen[one]))
    return twice


def report_repeats(twice, what, where):
    """Print any duplicate ids and say the run has failed. Returns True if any."""
    if not twice:
        return False
    for one, count in twice:
        out("  DEFINED %d TIMES: %s in %s\n" % (count, one, where))
    out("  An id defined twice makes every reference to it ambiguous, and\n")
    out("  nothing can say which %s was meant. Renumber one.\n" % what)
    return True

# Golden rules defined in 14
gr_def = set(int(x) for x in re.findall(r'^### (\d+)\.', allsrc.get('./docs/14-golden-rules.md', ''), re.M))
gr_ref = set(int(x) for x in re.findall(r'Golden Rule[s]? (\d+)', joined))
out("=== 2. GOLDEN RULES ===\n")
out("  defined in doc 14: %s\n" % sorted(gr_def))
out("  referenced anywhere: %s\n" % sorted(gr_ref))
out("  REFERENCED BUT NOT DEFINED: %s\n" % sorted(gr_ref - gr_def))
if report_repeats(repeats(r'^### (\d+)\.', './docs/14-golden-rules.md', re.M),
                  'rule', 'docs/14-golden-rules.md'):
    failed = True
out("\n")

# Decisions defined in DECISIONS.md
d_def = set(re.findall(r'^## (D-\d+)', allsrc.get('./docs/DECISIONS.md', ''), re.M))
d_ref = set(re.findall(r'\b(D-\d\d)\b', joined))
out("=== 3. DECISIONS ===\n")
out("  defined: %s\n" % sorted(d_def))
out("  REFERENCED BUT NOT DEFINED: %s\n" % sorted(d_ref - d_def))
if report_repeats(repeats(r'^## (D-\d+)', './docs/DECISIONS.md', re.M),
                  'decision', 'docs/DECISIONS.md'):
    failed = True
out("\n")

# Questions defined in OPEN-QUESTIONS.md
q_def = set(re.findall(r'### (?:[^\n]*?)(Q-\d+[a-z]?)', allsrc.get('./docs/OPEN-QUESTIONS.md', '')))
q_ref = set(re.findall(r'\b(Q-\d+[a-z]?)\b', joined))
out("=== 4. QUESTIONS ===\n")
out("  defined: %s\n" % sorted(q_def))
out("  REFERENCED BUT NOT DEFINED: %s\n" % sorted(q_ref - q_def))
if report_repeats(repeats(r'### (?:[^\n]*?)(Q-\d+[a-z]?)', './docs/OPEN-QUESTIONS.md'),
                  'question', 'docs/OPEN-QUESTIONS.md'):
    failed = True
out("\n")

# ---------- 5. count claims ----------
out("=== 5. COUNT CLAIMS - context for the eye. Section 7 is what ENFORCES ===\n")
# The heading used to read "verify by hand", and section 7 exists because
# nobody ever did. What prints here is now context around checks that
# actually fail: question counts and the Constitution's status are
# enforced below, so a line here is a prompt to read, not a duty to audit.
for p in ['./README.md', './docs/README.md']:
    s = allsrc.get(p, '')
    for m in re.finditer(r'[^\n]*(?:answered|proposed|Articles|official rules)[^\n]*', s):
        line = m.group(0).strip()
        if len(line) < 220:
            out("  %s | %s\n" % (p, line))
out("\n")

# actual counts
q_answered = len(re.findall(r'### \S+ (Q-\d+[a-z]?)', allsrc.get('./docs/OPEN-QUESTIONS.md', '')))
answered_section = allsrc.get('./docs/OPEN-QUESTIONS.md', '').split('## Answered')
n_answered = len(re.findall(r'### ', answered_section[1])) if len(answered_section) > 1 else 0
articles = len(re.findall(r'^\*\*(\d+[a-c]?)\.', allsrc.get('./HERON_CONSTITUTION.md', ''), re.M))
out("  ACTUAL: questions defined=%d, in Answered section=%d, constitution articles=%d\n"
    % (len(q_def), n_answered, articles))
# All 21 are official since 2026-08-28 (Q-19). This line used to print
# "official 1-15, proposed [...]" and would have become the stale claim itself -
# a checker that reports a superseded split is worse than one that reports
# nothing, because it is believed.
out("  ACTUAL: golden rules defined=%d (all official; 16-21 accepted 2026-08-28)\n"
    % len(gr_def))

# ---------- 6. the Progress line, derived rather than believed ----------
#
# Section 5 above prints count claims and says "verify by hand". Nobody does,
# which is how the Progress line at the top of OPEN-QUESTIONS.md came to say
# "14 answered - 26 open" while the file held 20 and 20. Six questions had been
# answered and the sentence stayed still.
#
# So this one is derived and compared, and it is the ONLY part of this script
# that can fail. Everything above is a report; a report cannot be wrong, which
# is also why it cannot catch anything.
oq = allsrc.get('./docs/OPEN-QUESTIONS.md', '')
answered = open_ids = None
if oq:
    answered, open_ids = 0, []
    for block in re.split(r'\n(?=### )', oq):
        head = re.match(r'### (.*?)(Q-\d+[a-z]?)\s*—', block)
        if not head:
            continue
        if '✅' in head.group(1):
            answered += 1
            continue
        # An answered question has real text after its **Answer:** marker; an
        # open one has the bare placeholder and nothing before the block ends.
        mark = re.search(r'\*\*Answer:\*?\*?', block)
        body = re.split(r'\n---', block[mark.end():])[0].strip() if mark else ''
        if len(body) > 5:
            answered += 1
        else:
            open_ids.append(head.group(2))

out("\n=== 6. THE PROGRESS LINE ===\n")
if answered is None:
    out("  OPEN-QUESTIONS.md not found - nothing to check\n")
else:
    out("  ACTUAL: %d answered, %d open\n" % (answered, len(open_ids)))
    out("  still open: %s\n" % ", ".join(open_ids))
    stated = re.search(r'\*\*Progress:\s*(\d+)\s*answered\s*·\s*(\d+)\s*open', oq)
    if not stated:
        out("  DRIFT: no '**Progress: N answered - M open' line to check against.\n")
        failed = True
    elif (int(stated.group(1)), int(stated.group(2))) != (answered, len(open_ids)):
        out("  DRIFT: the file says %s answered / %s open. The questions say %d / %d.\n"
            % (stated.group(1), stated.group(2), answered, len(open_ids)))
        out("  A stated count is a claim; a derived count is a fact. Fix the line.\n")
        failed = True
    else:
        out("  agrees with the stated Progress line\n")

# ---------- 7. the same claim, everywhere it is made ----------
#
# Section 6 derives the truth and enforces it against ONE sentence in ONE file.
# That is where "14 answered - 26 open" was caught and corrected on 2026-08-28.
# The identical sentence sat in README.md for two more days, printed by section
# 5 on every run under "verify by hand". Nobody verified it by hand - which is
# the whole finding, because section 5's own comment had already predicted it
# about a different file and the prediction was not acted on.
#
# A claim is not safer for being made somewhere else. So the truth section 6
# derives is now enforced against EVERY markdown file in the repository, and
# this section can fail.
#
# Two false positives, both found by running it rather than by reasoning:
#
#   * "Q-24 answered" is a question id, not a count of twenty-four. Hence the
#     lookbehind; and \b stops the pattern re-entering the number at its
#     second digit and reading "4 answered".
#   * "N open" alone is not a question claim - "Revit 2023 and Revit 2025 open
#     at the same time" is two release numbers. So the open count is only ever
#     read from a line that already carries an "answered" claim, which is the
#     shape a progress sentence actually has.
#
# And one deliberate exemption. A superseded figure QUOTED AS HISTORY is
# correct writing, not drift: OPEN-QUESTIONS.md records what its line used to
# say, and DECISIONS.md records what the Constitution's status used to be.
# Lines carrying a history marker are skipped - which makes the marker
# load-bearing. Write "it said 14 answered" and this stays quiet; write
# "14 answered" and it fails. That is the intended bargain, and it is cheaper
# than the alternative, which is a checker nobody can leave green.
HISTORY = re.compile(r'(used to|it said|until 20\d\d|no longer|superseded'
                     r'|was wrong|had stood|had been|stopped saying)', re.I)

out("\n=== 7. THE SAME CLAIM, EVERYWHERE IT IS MADE ===\n")
drift = []
if answered is None:
    out("  OPEN-QUESTIONS.md not found - nothing to enforce against\n")
else:
    n_open = len(open_ids)
    n_total = len(q_def)
    for p in md:
        for i, line in enumerate(allsrc.get(p, '').split('\n'), 1):
            if HISTORY.search(line):
                continue

            for m in re.finditer(r'(?<!Q-)\b(\d+)\s+of\s+(\d+)\s+questions?\s+answered', line):
                if int(m.group(1)) != answered or int(m.group(2)) != n_total:
                    drift.append((p, i, '%s of %s questions answered' % m.group(1, 2),
                                  '%d of %d' % (answered, n_total)))

            if not re.search(r'(?<!Q-)\b\d+\s+answered', line):
                continue
            for m in re.finditer(r'(?<!Q-)\b(\d+)\s+answered', line):
                if int(m.group(1)) != answered:
                    drift.append((p, i, '%s answered' % m.group(1), '%d answered' % answered))
            for m in re.finditer(r'(?<!Q-)\b(\d+)\s+open\b', line):
                if int(m.group(1)) != n_open:
                    drift.append((p, i, '%s open' % m.group(1), '%d open' % n_open))

    # The Constitution's own status line is the only thing entitled to say what
    # it is. Anything else describing it as unconfirmed is repeating a claim
    # that the document itself has already moved past.
    con = allsrc.get('./HERON_CONSTITUTION.md', '')
    if re.search(r'Status:\s*\*\*ACCEPTED', con):
        pend = re.compile(r'pending (?:confirmation|acceptance)|not yet accepted'
                          r'|awaiting confirmation', re.I)
        for p in md:
            for i, line in enumerate(allsrc.get(p, '').split('\n'), 1):
                if HISTORY.search(line) or not pend.search(line):
                    continue
                if 'onstitution' in line or 'HERON_CONSTITUTION' in line:
                    drift.append((p, i, 'the Constitution is pending confirmation',
                                  'ACCEPTED - the Constitution says so itself'))

    # THE COUNTS THAT DRIFT BECAUSE THE WORK ITSELF MOVES THEM.
    #
    # A question count drifts when somebody edits a document. These drift when
    # somebody adds a FILE - which is the more dangerous kind, because the
    # person adding the file is not reading the sentence that counts it.
    #
    # Both were wrong on 2026-09-09 and neither was caught: README.md said
    # "33 test suites" while four had been added in the same week by the same
    # hand, and the MCP tool count moved 13 -> 14 the day heron_context was
    # served. Deriving them costs one glob each.
    countable = [
        (os.path.join(root, 'tests'), 'test_*.py',
         r'(\d+)\s+test\s+suites\b', 'test suites'),
        (os.path.join(root, 'tools'), '*.py',
         r"(\d+)\s+tools\s+mentions\b", 'tools'),
    ]
    for folder, pattern, claim, label in countable:
        real = len(glob.glob(os.path.join(folder, pattern)))
        if not real:
            continue
        for p in md:
            for i, line in enumerate(allsrc.get(p, '').split('\n'), 1):
                if HISTORY.search(line):
                    continue
                for m in re.finditer(claim, line):
                    if int(m.group(1)) != real:
                        drift.append((p, i, '%s %s' % (m.group(1), label),
                                      '%d (ls %s)' % (real, pattern)))

    # THE COUNTS THAT DRIFT FASTEST OF ALL - THE FRAGMENT STATUSES.
    #
    # These are not a file count, so the countable list above cannot reach
    # them: the number of fragment.yaml files never moves, the status INSIDE
    # them does, and it moves hourly while a proving session runs. README.md
    # warns the reader in as many words not to trust the two numbers it then
    # states - which is an apology, not a check.
    #
    # It was already wrong. On 2026-09-11 README.md said 175 PROVEN and 185
    # DRAFT while the fragments said 197 and 163. Nothing caught it because
    # nothing was looking. Deriving it costs one glob.
    status = {}
    for fy in glob.glob(os.path.join(root, 'brain', 'fragments', '*',
                                     'fragment.yaml')):
        try:
            with io.open(fy, encoding='utf-8') as fh:
                for line in fh:
                    m = re.match(r'heron-status:\s*(\S+)', line)
                    if m:
                        status[m.group(1)] = status.get(m.group(1), 0) + 1
                        break
        except IOError:
            continue

    if status:
        total = sum(status.values())
        # ONE FORM ONLY, AND NOT IN THE RECORDS.
        #
        # "N of the M fragments are PROVEN" is a claim about today. A looser
        # pattern - any "N are PROVEN" - was tried first and matched every
        # line of docs/HANDOVER.md and every execution record, which are logs:
        # they are SUPPOSED to hold the number that was true that morning.
        # Flagging those trains the reader to ignore the checker.
        claim = re.compile(r'(\d+)\s+of\s+the\s+(\d+)\s+fragments'
                           r'\s+are\s+`?(PROVEN|DRAFT)`?')
        for p in md:
            if '/work-notes/' in p:
                continue
            for i, line in enumerate(allsrc.get(p, '').split('\n'), 1):
                if HISTORY.search(line):
                    continue
                hits = list(claim.finditer(line))
                if not hits:
                    continue
                for m in hits:
                    said_n, said_total, what = m.groups()
                    real_n = status.get(what, 0)
                    if int(said_n) != real_n:
                        drift.append((p, i, '%s %s' % (said_n, what),
                                      '%d (grep heron-status: in '
                                      'brain/fragments)' % real_n))
                    if int(said_total) != total:
                        drift.append((p, i, '%s fragments' % said_total,
                                      '%d (ls brain/fragments)' % total))
                # The trailing half of the same sentence: "185 of the 360
                # fragments are DRAFT AND 175 ARE PROVEN". Only ever read on
                # a line that already made the claim above - on its own this
                # pattern matches every log entry in the repository.
                for m in re.finditer(r'(\d+)\s+are\s+`?(PROVEN|DRAFT)`?',
                                     line):
                    said_n, what = m.groups()
                    if any(h.start() <= m.start() < h.end() for h in hits):
                        continue
                    real_n = status.get(what, 0)
                    if int(said_n) != real_n:
                        drift.append((p, i, '%s %s' % (said_n, what),
                                      '%d (grep heron-status: in '
                                      'brain/fragments)' % real_n))

    if drift:
        for p, i, said, real in drift:
            out("  DRIFT: %s:%d says '%s'; the source says %s\n" % (p, i, said, real))
        out("  A stated count is a claim; a derived count is a fact. Fix the claim.\n")
        failed = True
    else:
        out("  %d markdown file(s): every question count, every file count that\n" % len(md))
        out("  can be derived, and every Constitution status claim agrees with\n")
        out("  the source that owns it\n")

sys.exit(1 if failed else 0)
