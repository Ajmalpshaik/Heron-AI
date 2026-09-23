# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-VAL-009, HERON-DOC-RDM-007, HERON-DOC-ARC-006
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

import glob, io, os, re, sys

root = '.'

# A WORKTREE IS A SECOND COPY OF THIS REPOSITORY, NOT SECOND DOCUMENTATION.
# `.claude/worktrees/<name>/` holds a full checkout that another session is
# working in, at whatever commit it started from - so every count in its README
# is reported as DRIFT the moment the count here moves, and this gate goes red
# for a reason that is not a fault. Measured 2026-09-15: seven DRIFT lines, all
# of them the same four files seen twice.
#
# CI never saw it, because a worktree is not committed - which makes it worse,
# not better: a gate that is red locally and green in CI is the kind people
# learn to skip past, and this one exists to be read.

md = []
for dp, dn, fn in os.walk(root):
    here = dp.replace(os.sep, '/').rstrip('/') + '/'
    if '.git' in dp:
        continue
    if '/.claude/worktrees/' in here or here.startswith('.claude/worktrees/'):
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

# SECTION 1 NOW FAILS, AND DID NOT UNTIL 2026-09-16 (D-77). It had been
# FINDING dead links since 2026-08-31 and exiting 0 on them, which is the
# same defect its own comment above describes one layer up: the check was
# fixed to SEE them and never wired to the exit code, so it printed them
# into a green run nobody had to read.
#
# Three were sitting there when this line was added - a D-30 anchor whose
# heading had been reworded, a docs/24 filename that no longer exists, and
# a `../CLAUDE.md` that is not in this repository. Ten OTHER links to D-30
# were correct, which is why a checker keeping one occurrence per id saw
# nothing: the broken one was tenth of eleven and the eleventh overwrote
# it. Every occurrence is checked here, which is how these were found.
if bad:
    failed = True


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


# ASKED ONCE PER LINE, NOT ONCE PER CHECK. Every check below walks every line
# of every markdown file and asks HISTORY first, so the same line was asked up
# to eight times - measured 2026-09-23, most of this script's run time went to
# regular-expression searches, on a script already taking 30-37 s on a busy
# PC. The answer depends only on the text, so it is remembered.
_HISTORY_SEEN = {}


def is_history(text):
    """HISTORY.search(text), remembered - True when the text is a record."""
    hit = _HISTORY_SEEN.get(text)
    if hit is None:
        hit = _HISTORY_SEEN[text] = HISTORY.search(text) is not None
    return hit

out("\n=== 7. THE SAME CLAIM, EVERYWHERE IT IS MADE ===\n")
drift = []
if answered is None:
    out("  OPEN-QUESTIONS.md not found - nothing to enforce against\n")
else:
    n_open = len(open_ids)
    n_total = len(q_def)
    for p in md:
        for i, line in enumerate(allsrc.get(p, '').split('\n'), 1):
            if is_history(line):
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
                if is_history(line) or not pend.search(line):
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
    #
    # The MCP tool count is not a glob, and the row this list carried for it
    # could never fire: it counted tools/*.py - the checkers - against the
    # phrase "N tools mentions", which no sentence has ever used. It is
    # derived from the server's own registry now, further down, under THE
    # MCP TOOL TOTAL.
    countable = [
        (os.path.join(root, 'tests'), 'test_*.py',
         r'(\d+)\s+test\s+suites\b', 'test suites'),
        # A TOTAL CLAIMED WITHOUT THE WORD "TEST", which is how the row above
        # missed two of them for five days. heron-ship said "all 163 suites
        # then pass" and "run the full 163" while there were 199 - measured
        # 2026-09-12, read 2026-09-17, and the rule it broke was four lines up
        # its own page.
        #
        # ANCHORED ON "ALL"/"EVERY"/"THE FULL" RATHER THAN ON ANY NUMBER
        # BEFORE "SUITES", and that is the whole design. The broad pattern was
        # measured first: `(\d+)\s+suites` fires on 24 lines in this
        # repository and is RIGHT ABOUT NONE OF THEM. Every one is a record, a
        # quotation or a dated measurement - tests/README.md's "'41 suites'
        # and what check-gaps ran are two different numbers by design",
        # HANDOVER's "the test row said 41 suites when it", NEEDS-CHECKING's
        # "run, 188 suites, exit 1". A release gate that fails on two dozen
        # correct sentences stops every merge until somebody rewrites prose
        # that was never wrong, and teaches its reader to skim on the way.
        #
        # "All N suites" cannot be a subset. That is the sentence that rots.
        (os.path.join(root, 'tests'), 'test_*.py',
         r'\b(?:all|every|the full|run the full)\s+(\d+)\s+(?:test\s+)?suites?\b',
         'suites claimed as a TOTAL'),
    ]
    for folder, pattern, claim, label in countable:
        real = len(glob.glob(os.path.join(folder, pattern)))
        if not real:
            continue
        for p in md:
            for i, line in enumerate(allsrc.get(p, '').split('\n'), 1):
                if is_history(line):
                    continue
                for m in re.finditer(claim, line):
                    # A QUOTED TOTAL IS A CITATION, NOT A CLAIM. This file and
                    # the registers routinely report what a sentence USED to
                    # say - `heron-ship said "all 163 suites"` is the record
                    # of a fix, and failing on it would make describing a
                    # drift impossible without repeating it. An odd number of
                    # quotes before the match means it opens inside one.
                    if line.count('"', 0, m.start()) % 2 == 1:
                        continue
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
    by_risk = {}
    for fy in glob.glob(os.path.join(root, 'brain', 'fragments', '*',
                                     'fragment.yaml')):
        try:
            with io.open(fy, encoding='utf-8') as fh:
                text = fh.read()
        except IOError:
            continue
        st = re.search(r'^heron-status:\s*(\S+)', text, re.M)
        if not st:
            continue
        status[st.group(1)] = status.get(st.group(1), 0) + 1
        # The risk is the SECOND half of every claim of the form
        # "N MODIFY fragments are PROVEN" - a count of one status within
        # one risk, and a different number from either on its own.
        rk = re.search(r'^\s*risk:\s*(\S+)', text, re.M)
        if rk:
            key = (st.group(1), rk.group(1))
            by_risk[key] = by_risk.get(key, 0) + 1

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
                if is_history(line):
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

    # THE SAME CLAIM, NARROWED TO ONE RISK. "77 MODIFY fragments are PROVEN"
    # is a third number again - not the status total and not the grand total.
    #
    # This was missed the first time round. The two headline numbers were
    # derived and enforced while three sibling claims in the SAME file went
    # unchecked, and all three were wrong: README.md said 55 MODIFY PROVEN on
    # one line and 63 on another, while the fragments said 77. A checker that
    # covers one sentence and leaves its neighbours alone teaches the reader
    # that the file is checked when it is not.
    if by_risk:
        risk_claim = re.compile(r'(\d+)\s+`?([A-Z]{4,8})`?\s+fragments'
                                r'\s+are\s+`?(PROVEN|DRAFT)`?')
        # "... are PROVEN against 77 MODIFY" - the trailing half, and only on
        # a line that already carried the full claim above.
        against = re.compile(r'against\s+(\d+)\s+`?([A-Z]{4,8})`?')
        for p in md:
            if '/work-notes/' in p:
                continue
            for i, line in enumerate(allsrc.get(p, '').split('\n'), 1):
                if is_history(line):
                    continue
                hits = list(risk_claim.finditer(line))
                if not hits:
                    continue
                for m in hits:
                    said, risk, what = m.groups()
                    if (what, risk) not in by_risk:
                        continue
                    real = by_risk[(what, risk)]
                    if int(said) != real:
                        drift.append((p, i, '%s %s %s' % (said, risk, what),
                                      '%d (grep risk: and heron-status: in '
                                      'brain/fragments)' % real))
                what = hits[0].group(3)
                for m in against.finditer(line):
                    said, risk = m.groups()
                    if (what, risk) not in by_risk:
                        continue
                    real = by_risk[(what, risk)]
                    if int(said) != real:
                        drift.append((p, i, '%s %s %s' % (said, risk, what),
                                      '%d (grep risk: and heron-status: in '
                                      'brain/fragments)' % real))


    # ---------- THE SAME CLAIM IN OTHER WORDS ----------
    #
    # Every check above matches ONE wording, deliberately, because a loose
    # pattern fires on the logs. The cost of that is a hole per synonym, and
    # four claims fell through it: CONTRIBUTING.md said "193 fragments have
    # still never met a model" (row 5b-50) while the gate above read the
    # README's "68 of the 395 fragments are `DRAFT`" and passed; docs/32 and
    # docs/33 both said "218 fragments have never met a model"; and README's
    # own "The other 66 have still never met a model" sat two lines under a
    # freshly re-derived 327 of 395.
    #
    # AND ONE OF THEM WAS INVISIBLE TWICE OVER. README's claim breaks across
    # a line ending in "The other 66 have", so a per-LINE check cannot see it
    # even with the right wording - the same shape as row 5b-36, where a
    # refusal's sentence was split across two adjacent string literals and a
    # source grep reported 2 of 3.
    #
    # So these read a SENTENCE, flattened across newlines, and the history
    # and quotation rules are applied to the sentence rather than the line.
    # Sentence, not paragraph: a table is one paragraph, and one historical
    # row in it would switch the check off for every row beside it.
    #
    # THE TERMINATOR IS OFTEN NOT THE LAST CHARACTER. This prose ends
    # sentences with `.**`, `."` and `.)` constantly, and a lookbehind of
    # exactly one character does not break there - so a sentence ran on into
    # the next one and a history word in the FIRST excused a stale claim in
    # the SECOND. That is row 5b-55's own failure one size smaller: found
    # 2026-09-21 building tests/test_library_total.py, which carries the same
    # splitter (row 5b-67).
    BREAK = re.compile(r'[.!?][*_"\'\u2019)\]]*\s+|\s*\|\s*')

    # Split once per file: four checks below read the same sentences, and
    # the split depends only on the text.
    _SENTENCES = {}

    def sentences(text):
        """(first line, sentence, whole block, offset in block) each."""
        if text in _SENTENCES:
            return _SENTENCES[text]
        found, start, held = [], 1, []

        def flush():
            block = ' '.join(held)
            at = 0
            for one in BREAK.split(block):
                where = block.find(one, at)
                if where < 0:
                    where = at
                if one.strip():
                    found.append((start, one, block, where))
                at = where + len(one)

        for n, line in enumerate(text.split('\n'), 1):
            if line.strip():
                if not held:
                    start = n
                held.append(line)
            elif held:
                flush()
                held = []
        if held:
            flush()
        _SENTENCES[text] = found
        return found

    def a_claim(block, at):
        """False when the match opens inside a quotation - a citation.

        COUNTED FROM THE START OF THE TABLE CELL, not of the block. A
        register section is ONE block of several hundred rows, so parity
        taken from its start is the parity of everything above rather than
        of the sentence in hand - and it reported six citations in
        FRAGMENT-ISSUES as claims the first time this ran. A quoted
        sentence never spans a cell boundary: the row format escapes an
        inner pipe as \\|, which is its own gate.
        """
        cell = block.rfind('|', 0, at) + 1
        return block.count('"', cell, at) % 2 == 0

    if status:
        MET = re.compile(r'(\d+)\s+(?:of them\s+|fragments\s+)?'
                         r'(?:have|has)\s+(?:still\s+)?never met a model',
                         re.I)
        PROOF = re.compile(r'(\d+)\s+of\s+the\s+(\d+)\s+carry a recorded '
                           r'proof', re.I)
        for p in md:
            if '/work-notes/' in p or '/handover-archive/' in p:
                continue
            for i, one, block, off in sentences(allsrc.get(p, '')):
                if is_history(one):
                    continue
                for m in MET.finditer(one):
                    if not a_claim(block, off + m.start()):
                        continue
                    real = status.get('DRAFT', 0)
                    if int(m.group(1)) != real:
                        drift.append((p, i, '%s never met a model'
                                      % m.group(1),
                                      '%d DRAFT (grep heron-status: in '
                                      'brain/fragments)' % real))
                for m in PROOF.finditer(one):
                    if not a_claim(block, off + m.start()):
                        continue
                    real = status.get('PROVEN', 0)
                    if int(m.group(1)) != real:
                        drift.append((p, i, '%s carry a recorded proof'
                                      % m.group(1),
                                      '%d PROVEN (grep heron-status: in '
                                      'brain/fragments)' % real))
                    if int(m.group(2)) != total:
                        drift.append((p, i, 'of the %s' % m.group(2),
                                      '%d (ls brain/fragments)' % total))

    # ---------- THE SAME RISK CLAIM, IN A THIRD WORDING ----------
    #
    # The risk gate above matches "N RISK fragments are PROVEN" and its
    # trailing "against N RISK". docs/PROJECT-MAP.md said "106 READ
    # fragments CARRY A PROOF against 63 MODIFY" - the same fact, three
    # words outside both - and it was TRUE WHEN IT WAS WRITTEN. The library
    # moved to 145 against 166, which makes the sentence it supports
    # ("reading is proven far more widely") the opposite of true, in the
    # paragraph written for a BIM modeller deciding whether to trust this
    # near their model. Row 5b-61.
    #
    # Measured before adding: this pattern fires on exactly that one line in
    # the repository and on nothing correct.
    if by_risk:
        CARRY = re.compile(r'(\d+)\s+`?([A-Z]{4,8})`?\s+fragments?\s+carry'
                           r'(?:ing)?\s+a\s+proof'
                           r'(?:\s+against\s+(\d+)\s+`?([A-Z]{4,8})`?)?')
        for p in md:
            if '/work-notes/' in p or '/handover-archive/' in p:
                continue
            for i, one, block, off in sentences(allsrc.get(p, '')):
                if is_history(one):
                    continue
                for m in CARRY.finditer(one):
                    if not a_claim(block, off + m.start()):
                        continue
                    pairs = [(m.group(1), m.group(2))]
                    if m.group(3):
                        pairs.append((m.group(3), m.group(4)))
                    for said, risk in pairs:
                        real = by_risk.get(('PROVEN', risk))
                        if real is None:
                            continue
                        if int(said) != real:
                            drift.append((p, i, '%s %s carry a proof'
                                          % (said, risk),
                                          '%d PROVEN %s (grep risk: and '
                                          'heron-status: in brain/fragments)'
                                          % (real, risk)))

    # ---------- THE MCP TOOL TOTAL, FROM THE REGISTRY ITSELF ----------
    #
    # The countable list above carried a row for this and it could not fire:
    # it counted tools/*.py - the checkers, not the MCP tools - against the
    # phrase "N tools mentions", which no sentence in the repository has ever
    # used. Measured 2026-09-23: zero matches. Meanwhile docs/33 said "Heron
    # has 14 MCP tools" and docs/34 "14 MCP tools against their 314", while
    # the server's own registry held 34.
    #
    # THE TOTAL IS len(heron_tools.TOOLS), imported rather than re-derived:
    # it is the table the server registers from and the add-in's registry is
    # tested against, and a count reached here by any other route would be a
    # second copy of the fact.
    #
    # ONE FORM, "N MCP tools", read by sentence like its neighbours, and only
    # when the number is Heron's. A research document names other projects'
    # totals in the same words - "their 314 MCP tools", "ruflo's 314 MCP
    # tools" - and that is somebody else's count. A quotation is a citation
    # and a history word excuses a record, exactly as above. The better
    # sentence types no number at all: `python mcp/server/heron_tools.py`
    # lists them.
    try:
        _server = os.path.abspath(os.path.join(root, 'mcp', 'server'))
        if _server not in sys.path:
            sys.path.insert(0, _server)
        import heron_tools as _HERON_TOOLS
        mcp_total = len(_HERON_TOOLS.TOOLS)
    except Exception as _exc:                   # noqa: BLE001 - said below
        mcp_total = None
        out("  the MCP tool total could not be derived (%s) - no MCP tool "
            "count was checked\n" % type(_exc).__name__)
    if mcp_total is not None:
        MCP_COUNT = re.compile(r"(?:\b(their|its)\s+"
                               r"|\b([A-Za-z][\w.-]*)(?:'s|\u2019s)\s+)?"
                               r"\b(\d+)\s+MCP\s+tools?\b", re.I)
        for p in md:
            if '/work-notes/' in p or '/handover-archive/' in p:
                continue
            for i, one, block, off in sentences(allsrc.get(p, '')):
                if is_history(one):
                    continue
                for m in MCP_COUNT.finditer(one):
                    if not a_claim(block, off + m.start(3)):
                        continue
                    if m.group(1) or (m.group(2)
                                      and m.group(2).lower() != 'heron'):
                        continue
                    if int(m.group(3)) != mcp_total:
                        drift.append((p, i, '%s MCP tools' % m.group(3),
                                      '%d (len(heron_tools.TOOLS) in '
                                      'mcp/server/heron_tools.py)'
                                      % mcp_total))

    # ---------- THE WRITE PATH, ANSWERED BY THE REGISTER RATHER THAN BY A
    # ---------- SENTENCE
    #
    # "The write path has never been compiled or run" was hunted three times
    # - rows 5b-26, 5b-37 and 5b-44 - and each hunt grepped THE WORDING IT
    # HAD JUST FIXED. Row 26 found four copies and closed with "WHAT WAS NOT
    # CHECKED: whether the same claim appears in the numbered documents under
    # docs/ IN A FORM THE GREP MISSED". Row 44 found the seventh and called
    # itself the last. Three more were sitting in README.md, docs/README.md
    # and docs/27-build-order.md, saying "never loaded into Revit" and "never
    # moved anything" - row 5b-54.
    #
    # A fourth hunt would find the fourth wording and miss the fifth. So this
    # asks the REGISTER instead: while NEEDS-CHECKING records B8 as a dated
    # PASS - Revit 2024, three ducts moved 200 mm, 2026-09-07 - no live
    # sentence may say the write path has never run. Strike B8 through
    # differently and this check turns itself off, which is correct: it is
    # then no longer a settled fact.
    #
    # docs/DECISIONS.md is exempt BY NAME and for a reason: D-19's Context
    # records the conditions the decision was taken under - "written on a
    # machine with no Revit, no Windows and no .NET SDK" - and rewriting that
    # is rewriting history rather than correcting a claim.
    # docs/decisions/ is exempt for the same reason: since 2026-09-23 each
    # decision's full record is its own file there, and DECISIONS.md is the
    # index (tools/split-decisions.py). D-19's Context moved with D-19.
    #
    # SINCE 2026-09-23 THE REGISTER IS ONE FILE PER GROUP, and B8 lives in
    # docs/needs-checking/group-b.md. It is read the way every reader of the
    # register reads it: tools/needs-checking-register.py puts each group back
    # under its heading, served here from the copy of every file loaded above.
    import importlib.util
    _ncr_spec = importlib.util.spec_from_file_location(
        'needs_checking_register',
        os.path.join(os.path.dirname(os.path.abspath(__file__)), 'needs-checking-register.py'))
    _ncr = importlib.util.module_from_spec(_ncr_spec)
    _ncr_spec.loader.exec_module(_ncr)
    register = _ncr.register_text(
        read=lambda rel: allsrc.get(os.path.join(root, *rel.split('/')).replace(os.sep, '/'))) or ''
    b8 = re.search(r'^\|\s*~~\*\*B8\*\*~~.*$', register, re.M)
    moved = re.search(r'\*\*PASSED\s+(\d{4}-\d{2}-\d{2})', b8.group(0)) \
        if b8 else None
    if moved:
        NEVER = re.compile(r'never loaded into revit'
                           r'|never (?:been )?moved anything'
                           r'|never (?:been )?run against a real model', re.I)
        for p in md:
            short = p[len(root):].lstrip('/') if p.startswith(root) else p
            if ('/work-notes/' in p or '/handover-archive/' in p
                    or short == 'docs/DECISIONS.md'
                    or short.startswith('docs/decisions/')):
                continue
            for i, one, block, off in sentences(allsrc.get(p, '')):
                if is_history(one):
                    continue
                for m in NEVER.finditer(one):
                    if not a_claim(block, off + m.start()):
                        continue
                    drift.append((p, i, "'%s'" % m.group(0),
                                  'B8 in NEEDS-CHECKING.md: PASSED %s, three '
                                  'ducts moved 200 mm' % moved.group(1)))

    if drift:
        for p, i, said, real in drift:
            out("  DRIFT: %s:%d says '%s'; the source says %s\n" % (p, i, said, real))
        out("  A stated count is a claim; a derived count is a fact. Fix the claim.\n")
        failed = True
    else:
        out("  %d markdown file(s): every question count, every file count that\n" % len(md))
        out("  can be derived, and every Constitution status claim agrees with\n")
        out("  the source that owns it\n")


# ---------- 8. the generated table in DECISIONS.md ----------
#
# DECISIONS.md's Status summary is generated now, and a generated file that is
# COMMITTED can be stale - which is worse than one that is absent, because it
# is believed. It was hand-written until 2026-09-12 and by then it stopped at
# D-50 while the file had reached D-70: twenty decisions missing from the index
# of decisions, and no gate able to notice.
#
# The check lives HERE rather than in .github/workflows/gates.yml, where the
# other three generators are diffed. THE REASON IT GAVE HAS EXPIRED, and the
# reason it stays has not.
#
# What it used to say: "the repository's gh token carries `repo` but not
# `workflow`, so a session cannot push a change to that file at all." That was
# true when it was written and is not true now - the token carries `workflow`,
# and gates.yml has been edited by sessions several times since, PR #173 among
# them. A reader believing the old sentence would route around a constraint
# that is gone, which is the expensive direction.
#
# WHY IT STILL LIVES HERE. Nothing in gates.yml diffs the decision summary -
# the `generated` job covers the agent registry and the two HTML generators,
# not this - so deleting this section deletes the check. check-docs.py already
# runs inside "The gates that must pass", and it also runs locally, where the
# CI job does not. Giving it its own step would be tidier and would ADD
# coverage rather than move it; taking it out of here would subtract.
out("\n=== 8. THE GENERATED DECISION TABLE ===\n")
_gen = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                    'generate-decision-summary.py')
if not os.path.exists(_gen):
    out("  generate-decision-summary.py is gone - nothing to check\n")
else:
    import subprocess
    _r = subprocess.run([sys.executable, _gen, '--check'],
                        capture_output=True, text=True)
    for _line in (_r.stdout or '').rstrip('\n').split('\n'):
        out("  %s\n" % _line)
    if _r.returncode != 0:
        failed = True

# ---------- 9. the signature gate, riding in for the same reason as 8 ----------
#
# check-signatures.py has existed since 2026-09-13 and NOTHING RAN IT. It was
# written because thirteen fragments the owner had already signed were sitting
# at DRAFT, so the next proving round offered them to him to prove AGAIN - the
# complaint that started that session. A gate nobody runs is the same as no
# gate, and it stayed unrun because a session was believed unable to push a
# change to .github/workflows/gates.yml at all.
#
# THAT HAS SINCE HAPPENED, and this section's own closing instruction used to
# read "if the workflow is ever edited by hand, give it its own step and
# delete this section". Half of it is done: gates.yml HAS a check-signatures
# step, and `ci/run-check-signatures` - the branch this said could not be
# pushed - is gone from origin, its work landed.
#
# THE OTHER HALF IS NOT DONE, DELIBERATELY. Deleting this would leave
# check-signatures running in CI and nowhere else, and the pre-push routine
# the heron-ship skill describes runs check-docs.py on a laptop, before a
# pull request exists. A stale signature is worth catching there rather than
# ten minutes later. It costs about a second and it is NOT a documentation
# check and does not pretend to be.
out("\n=== 9. SIGNATURES NOT LEFT UNUSED ===\n")
_sig = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                    'check-signatures.py')
if not os.path.exists(_sig):
    out("  check-signatures.py is gone - nothing to check\n")
else:
    import subprocess
    _r = subprocess.run([sys.executable, _sig],
                        capture_output=True, text=True)
    for _line in (_r.stdout or '').rstrip('\n').split('\n'):
        out("  %s\n" % _line)
    if _r.returncode != 0:
        failed = True

# ---------- 10. every tool and every skill, named where it is described ----------
#
# tools/README.md has carried a one-line check for this since 2026-09-16 -
# "every tool on disk that this page never names" - and nothing ran it. By
# 2026-09-22 nine files in tools/ were described nowhere on the page, three of
# them the scripts a cloud environment, a deploy and an install go through
# (cloud-setup.sh, deploy-addin.ps1, HeronRevit.ps1). The one-liner looked at
# *.py only, so those three were outside even the check nobody ran.
#
# ONE DIRECTION ON PURPOSE, for the reason that page gives: a name on the page
# with no file behind it is a broken link, which section 1 already catches.
# The direction that matters is ON DISK AND UNNAMED - a tool nobody can find
# is a tool nobody runs.
#
# "Named" means the whole name, with a boundary on each side, so
# check-routing.py is not found inside check-skill-routing.py. The DISK is
# read rather than git, so a new tool is caught before it is ever committed.
out("\n=== 10. EVERY TOOL AND EVERY SKILL NAMED IN ITS README ===\n")


def named(text, name):
    """True when `name` appears in `text` as a whole name."""
    return re.search(r'(?<![\w.-])' + re.escape(name) + r'(?![\w-])',
                     text) is not None


def unnamed(folder, want_dirs):
    """(how many were looked at, the names on disk its README never names)."""
    where = os.path.join(root, folder)
    page = os.path.join(where, 'README.md')
    if not os.path.isdir(where) or not os.path.isfile(page):
        return None, []
    text = io.open(page, encoding='utf-8').read()
    seen, missing = 0, []
    for name in sorted(os.listdir(where)):
        if name.startswith('.') or name in ('__pycache__', 'README.md'):
            continue
        if os.path.isdir(os.path.join(where, name)) != want_dirs:
            continue
        if name.endswith('.pyc'):
            continue
        seen += 1
        if not named(text, name):
            missing.append(name)
    return seen, missing


for _folder, _dirs, _what in (('tools', False, 'file'),
                              ('.claude/skills', True, 'skill folder')):
    _seen, _missing = unnamed(_folder, _dirs)
    if _seen is None:
        out("  %s/README.md not found - nothing to check\n" % _folder)
        continue
    for _name in _missing:
        out("  NOT NAMED: %s/%s - on disk, and %s/README.md never names it\n"
            % (_folder, _name, _folder))
    if _missing:
        failed = True
    else:
        out("  %s/: all %d of its %ss are named in its README\n"
            % (_folder, _seen, _what))

# ---------- 11. text hygiene, over every tracked file ----------
#
# Three faults no reader sees and every tool trips over:
#
#   * A CONTROL CHARACTER other than tab, CR and LF. Two sat in a row of the
#     defect register - a backspace and a form feed, typed where the row meant
#     to write their escapes - and rendered as nothing at all. A byte no diff
#     shows and no grep expects.
#   * DOUBLE-ENCODED TEXT: UTF-8 read in the Windows code page and written
#     back, so one em dash arrives as three characters. Heron is written on
#     Windows, where a redirected console encodes in that code page; the
#     heron-ship skill records the two times it bit.
#   * A MERGE-CONFLICT MARKER left in a file - rare, and the most expensive of
#     the three, because the file still reads as prose.
#
# THE FORBIDDEN CHARACTERS ARE BUILT, NEVER TYPED. This file is tracked, so it
# is read by the section it defines: a literal backspace in a pattern here
# would be found here - the reason heron_guard.py builds the vendor namespace
# from parts.
#
# OVER `git ls-files`, because what matters is what is committed; a build
# folder or a scratch file on somebody's disk is not this repository. With no
# git to ask - a copy of the tree, a download - it says NOT RUN and does not
# fail the run. NOT RUN is its own state, and it is never a pass.
#
# A HIT INSIDE A PROVEN FRAGMENT'S impl/ WAITS rather than failing. The proof
# is one hash over impl/ (D-30), so fixing the byte makes the proof stale and
# owes a re-proof on a real model - the fix goes with that re-proof, and
# failing every pull request until then would teach people to skip this.
out("\n=== 11. TEXT HYGIENE ===\n")
import subprocess
import unicodedata

_CONTROL = (''.join(chr(c) for c in range(32) if c not in (9, 10, 13))
            + chr(127) + ''.join(chr(c) for c in range(0x80, 0xA0)))
_CONTROL_RE = re.compile('[' + re.escape(_CONTROL) + ']')


def _as_code_page(b):
    """The character one byte becomes when read in the Windows code page.

    Five bytes are undefined there; Windows reads those as the matching C1
    control, and so does this.
    """
    try:
        return bytes([b]).decode('cp1252')
    except UnicodeDecodeError:
        return chr(b)


# A UTF-8 lead byte read in the code page is one of these, and wants this many
# continuation bytes after it; a continuation byte read the same way is one of
# _CONT's keys.
_LEAD = dict((chr(c), 1 if c < 0xE0 else 2 if c < 0xF0 else 3)
             for c in range(0xC2, 0xF5))
_CONT = dict((_as_code_page(b), b) for b in range(0x80, 0xC0))
_MOJI_RE = re.compile('[' + re.escape(''.join(_LEAD)) + ']['
                      + re.escape(''.join(_CONT)) + ']')
_MARK = dict((ch * 7, ch) for ch in '<>=|')


def double_encoded(line):
    """(column, the character it was meant to be) for the first hit, or None.

    Only a run whose bytes put back together are valid UTF-8 counts: a lead
    character followed by exactly the continuation characters it needs. A
    French quotation or a price in pounds does not make that shape.
    """
    for m in _MOJI_RE.finditer(line):
        at = m.start()
        need = _LEAD[line[at]]
        tail = line[at + 1:at + 1 + need]
        if len(tail) != need or any(ch not in _CONT for ch in tail):
            continue
        try:
            meant = bytes([ord(line[at])] + [_CONT[ch] for ch in tail]) \
                .decode('utf-8')
        except UnicodeDecodeError:
            continue
        return at, meant
    return None


def conflict_lines(lines):
    """Line numbers of merge-conflict markers.

    Seven '=' or '|' alone can be a heading's underline, so those count only
    in a file that also has a '<<<<<<<' or '>>>>>>>' line.
    """
    found, strong = [], False
    for n, line in enumerate(lines, 1):
        head = line[:7]
        if head not in _MARK or line[7:8] not in ('', ' ', '\r'):
            continue
        found.append(n)
        strong = strong or _MARK[head] in '<>'
    return found if strong else []


_proven = {}


def proven_fragment(path):
    """The fragment's name when `path` is inside a PROVEN fragment's impl/."""
    parts = path.split('/')
    if len(parts) < 5 or parts[:2] != ['brain', 'fragments'] \
            or parts[3] != 'impl':
        return None
    if parts[2] not in _proven:
        card = os.path.join(root, 'brain', 'fragments', parts[2],
                            'fragment.yaml')
        try:
            st = re.search(r'^heron-status:\s*(\S+)',
                           io.open(card, encoding='utf-8').read(), re.M)
        except (IOError, OSError):
            st = None
        _proven[parts[2]] = bool(st and st.group(1) == 'PROVEN')
    return parts[2] if _proven[parts[2]] else None


try:
    _ls = subprocess.run(['git', 'ls-files', '-z'], cwd=root,
                         capture_output=True, timeout=60)
    if _ls.returncode == 0:
        _tracked, _why = [n for n in _ls.stdout.decode(
            'utf-8', 'surrogateescape').split('\0') if n], ''
    else:
        _tracked = None
        _why = (_ls.stderr.decode('utf-8', 'replace').strip().splitlines()
                or ['git ls-files exited %d' % _ls.returncode])[0]
except (OSError, subprocess.SubprocessError) as _exc:
    _tracked, _why = None, '%s: %s' % (type(_exc).__name__, _exc)

if _tracked is None:
    out("  NOT RUN: %s - this section reads what git tracks, and there is\n"
        "  no git here to ask. Not a pass: nothing was read.\n" % _why)
else:
    _fail, _wait, _binary, _other = [], [], 0, 0
    _conflict = '<' * 7, '>' * 7
    for _name in _tracked:
        try:
            with io.open(os.path.join(root, _name), 'rb') as _handle:
                _data = _handle.read()
        except (IOError, OSError):
            continue
        if b'\0' in _data[:8000]:
            _binary += 1
            continue
        try:
            _text = _data.decode('utf-8')
        except UnicodeDecodeError:
            _other += 1
            continue
        _control = _CONTROL_RE.search(_text) is not None
        _moji = not _data.isascii() and _MOJI_RE.search(_text) is not None
        _marks = _conflict[0] in _text or _conflict[1] in _text
        if not (_control or _moji or _marks):
            continue
        _lines = _text.split('\n')
        _found = []
        for _n, _line in enumerate(_lines, 1):
            if _control:
                for _ch in sorted(set(_CONTROL_RE.findall(_line))):
                    _found.append((_n, 'a control character, U+%04X'
                                   % ord(_ch)))
            if _moji:
                _hit = double_encoded(_line)
                if _hit:
                    _found.append((_n, 'double-encoded text: %s written as its '
                                       'UTF-8 bytes read in the Windows code '
                                       'page' % unicodedata.name(
                                           _hit[1][0], 'U+%04X' % ord(_hit[1][0]))))
        if _marks:
            for _n in conflict_lines(_lines):
                _found.append((_n, 'a merge-conflict marker'))
        _fragment = proven_fragment(_name)
        for _n, _what in sorted(_found):
            (_wait if _fragment else _fail).append((_name, _n, _what, _fragment))
    for _name, _n, _what, _ in _fail:
        out("  %s:%d - %s\n" % (_name, _n, _what))
    for _name, _n, _what, _fragment in _wait:
        out("  WAITING: %s:%d - %s, inside PROVEN %s's impl/. Fixing it makes\n"
            "  the proof stale (D-30), so it is fixed with the re-proof.\n"
            % (_name, _n, _what, _fragment))
    out("  %d tracked text file(s) read, %d binary and %d not UTF-8 skipped: "
        "%d finding(s) that fail, %d waiting\n"
        % (len(_tracked) - _binary - _other, _binary, _other, len(_fail),
           len(_wait)))
    if _fail:
        failed = True

sys.exit(1 if failed else 0)
