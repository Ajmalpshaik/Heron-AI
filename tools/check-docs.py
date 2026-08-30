# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-VAL-009
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

import io, os, re, sys

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
link = re.compile(r'\[([^\]]*)\]\(([^)]+)\)')
bad = []
for p in md:
    s = io.open(p, encoding='utf-8').read()
    base = os.path.dirname(p)
    for text, href in link.findall(s):
        if href.startswith(('http://', 'https://', '#', 'mailto:')):
            continue
        tgt = href.split('#')[0]
        if not tgt:
            continue
        full = os.path.normpath(os.path.join(base, tgt)).replace(os.sep, '/')
        if not os.path.exists(full):
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

def defined(pattern, path):
    s = allsrc.get(path, '')
    return set(re.findall(pattern, s))

# Golden rules defined in 14
gr_def = set(int(x) for x in re.findall(r'^### (\d+)\.', allsrc.get('./docs/14-golden-rules.md', ''), re.M))
gr_ref = set(int(x) for x in re.findall(r'Golden Rule[s]? (\d+)', joined))
out("=== 2. GOLDEN RULES ===\n")
out("  defined in doc 14: %s\n" % sorted(gr_def))
out("  referenced anywhere: %s\n" % sorted(gr_ref))
out("  REFERENCED BUT NOT DEFINED: %s\n\n" % sorted(gr_ref - gr_def))

# Decisions defined in DECISIONS.md
d_def = set(re.findall(r'^## (D-\d+)', allsrc.get('./docs/DECISIONS.md', ''), re.M))
d_ref = set(re.findall(r'\b(D-\d\d)\b', joined))
out("=== 3. DECISIONS ===\n")
out("  defined: %s\n" % sorted(d_def))
out("  REFERENCED BUT NOT DEFINED: %s\n\n" % sorted(d_ref - d_def))

# Questions defined in OPEN-QUESTIONS.md
q_def = set(re.findall(r'### (?:[^\n]*?)(Q-\d+[a-z]?)', allsrc.get('./docs/OPEN-QUESTIONS.md', '')))
q_ref = set(re.findall(r'\b(Q-\d+[a-z]?)\b', joined))
out("=== 4. QUESTIONS ===\n")
out("  defined: %s\n" % sorted(q_def))
out("  REFERENCED BUT NOT DEFINED: %s\n\n" % sorted(q_ref - q_def))

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
failed = False
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

    if drift:
        for p, i, said, real in drift:
            out("  DRIFT: %s:%d says '%s'; the source says %s\n" % (p, i, said, real))
        out("  A stated count is a claim; a derived count is a fact. Fix the claim.\n")
        failed = True
    else:
        out("  %d markdown file(s): every question count and every Constitution\n" % len(md))
        out("  status claim agrees with the document that owns it\n")

sys.exit(1 if failed else 0)
