<!--
Thank you for looking at Heron. The README asks for exactly this.

Fill in what applies and delete what does not. A one-line fix does not need
a long description - but it does need the last section.
-->

## What this changes

<!-- One or two sentences. What is different after this is merged? -->

## Why

<!-- The problem, not the patch. Link an issue if there is one. -->

## How it was checked

<!--
The repository's rule is that a stated count is a claim and a derived count
is a fact. The same applies to "it works".

Tick what you actually ran. Leave the rest unticked - an honest gap is worth
more than a tick nobody verified.
-->

- [ ] `python tools/check-docs.py`
- [ ] `python tools/check-metadata.py`
- [ ] `python tools/check-structure.py`
- [ ] The test suites — `for t in tests/test_*.py; do python "$t"; done`
- [ ] Ran against a real Revit model (say which release, and which model)
- [ ] Not checked — and here is why:

## Anything a reviewer should know

<!--
Something you were unsure about, a decision that could have gone another way,
or a number you could not derive. This section is the useful one.
-->
