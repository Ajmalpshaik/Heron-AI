# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-MEX-006
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Metadata extraction - the author is asked for, never derived.

    python tests/test_metadata_extract.py

WHAT IT PROVES
  1. NO AUTHOR IS EVER INFERRED. A file with no author in a folder
     called `A-Shaik-Tools`, next to files that DO name an author, still
     comes back with author None and a question - the three mechanical
     answers are all available and none is taken.

  2. THE THREE READABLE FIELDS ARE READ - imports, version, releases,
     from real files on disk.

  3. A RELEASE HERON DOES NOT SUPPORT IS REPORTED, NOT DROPPED (D-05),
     and the supported list is heron_fragment's own object.

  4. NAMING NO RELEASE IS NOT NAMING ALL OF THEM.

  5. UNDECLARED IS COMPUTED OVER THE SET, because a project declares for
     its sources - the same import reads as undeclared alone and
     declared once the .csproj is in the list.

  6. A FILE THAT CANNOT BE OPENED IS NAMED, NOT SKIPPED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_metadata as MEX                                   # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(where, at, text):
    full = os.path.join(where, *at.split("/"))
    folder = os.path.dirname(full)
    if folder and not os.path.isdir(folder):
        os.makedirs(folder)
    with io.open(full, "w", encoding="utf-8") as handle:
        handle.write(text)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_metadata.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    yard = tempfile.mkdtemp(prefix="heron-metadata-test-")
    try:
        # THE FOLDER IS NAMED AFTER A PERSON ON PURPOSE. It is the most
        # tempting mechanical answer there is, and it must not be taken.
        where = os.path.join(yard, "A-Shaik-Tools")
        os.makedirs(where)
        write(where, "Signed.cs",
              '// @author: A. Shaik\nusing System;\n'
              '[assembly: AssemblyVersion("2.1.0")]\n'
              '#if REVIT2024_OR_GREATER\n#endif\n#if REVIT2020\n#endif\n')
        write(where, "Unsigned.cs", 'using System;\nusing Newtonsoft.Json;\n')
        write(where, "old.py", '__version__ = "0.4"\nimport os\n')
        write(where, "Ancient.cs", '#if REVIT2018\n#endif\nusing System;\n')
        write(where, "Tools.csproj",
              '<Project><Version>3.0.1</Version>'
              '<PackageReference Include="Newtonsoft.Json" Version="13" />'
              '</Project>\n')

        answer = MEX.read(["Signed.cs", "Unsigned.cs", "old.py",
                           "Ancient.cs", "Tools.csproj"], root=where)
        by_name = dict((card["at"], card) for card in answer["found"])

        print("\n1. no author is ever inferred")
        check(by_name["Signed.cs"]["author"] == "A. Shaik",
              "a file that STATES an author gets it read")
        for name in ("Unsigned.cs", "old.py", "Ancient.cs"):
            check(by_name[name]["author"] is None,
                  "%s states none, and none was invented" % name)
        asked = sorted(one["at"] for one in answer["asks"])
        check(asked == ["Ancient.cs", "Tools.csproj", "Unsigned.cs",
                        "old.py"],
              "every file stating no author is asked about, the .csproj "
              "included: %s" % ", ".join(asked))
        # The three mechanical answers were all sitting there.
        check("A. Shaik" not in str([by_name[n]["author"]
                                     for n in ("Unsigned.cs", "old.py")]),
              "the author NAMED IN A SIBLING FILE was not borrowed")
        check("Shaik" not in str(by_name["old.py"]),
              "and the folder is called A-Shaik-Tools, which was not used")
        # NOT A WORD SEARCH. The module names git blame in its question,
        # correctly, to say why it is not used. The proof is that it
        # cannot run anything: no subprocess, no git, nothing but reading.
        imports = sorted(set(
            line.split()[1].split(".")[0]
            for line in logic.split("\n")
            if line.startswith("import ") or line.startswith("from ")))
        check(imports == ["heron_fragment", "io", "os", "re", "sys"],
              "it imports only %s - it cannot run git, or anything else"
              % ", ".join(imports))
        check(sorted(MEX.ASKED_FOR) == ["author"],
              "author is the ONLY field that is asked for")

        print("\n2. the three readable fields are read")
        check(by_name["Signed.cs"]["version"] == "2.1.0",
              "AssemblyVersion is read")
        check(by_name["old.py"]["version"] == "0.4", "__version__ is read")
        check(by_name["Tools.csproj"]["version"] == "3.0.1",
              "<Version> is read")
        check(by_name["Signed.cs"]["imports"] == ["System"],
              "a `using` is an import")
        check(by_name["old.py"]["imports"] == ["os"],
              "and so is a Python `import`")
        check(by_name["Tools.csproj"]["packages"] == ["Newtonsoft.Json"],
              "a PackageReference is a declaration")

        print("\n3. an unsupported release is reported, not dropped")
        check(MEX.RELEASES is FRAG.REVIT_VERSIONS,
              "MEX.RELEASES IS FRAG.REVIT_VERSIONS - the same object")
        check(by_name["Ancient.cs"]["revitOutsideSupport"] == ["2018"],
              "REVIT2018 is named as outside support")
        check(by_name["Ancient.cs"]["revit"] == [],
              "and it is NOT counted as a supported release")
        check(by_name["Signed.cs"]["revit"] == ["2020", "2024"],
              "while the two real ones are read: %s"
              % ", ".join(by_name["Signed.cs"]["revit"]))

        print("\n4. naming no release is not naming all of them")
        check(by_name["old.py"]["revit"] == [],
              "a file naming none comes back with an empty list")
        check(len(by_name["old.py"]["revit"]) != len(MEX.RELEASES),
              "which is not the same as all %d" % len(MEX.RELEASES))

        print("\n5. undeclared is computed over the set")
        alone = MEX.read(["Unsigned.cs"], root=where)
        check(alone["found"][0]["undeclared"] == ["System", "Newtonsoft.Json"],
              "alone, both its imports read as undeclared")
        with_proj = MEX.read(["Unsigned.cs", "Tools.csproj"], root=where)
        first = with_proj["found"][0]
        check("Newtonsoft.Json" not in first["undeclared"],
              "with the .csproj in the set, the declared one drops out")
        check("System" in first["undeclared"],
              "and the framework import stays - nothing here decides that "
              "System is not a dependency")
        check(with_proj["declared"] == ["Newtonsoft.Json"],
              "the set's declarations are reported once")

        print("\n6. a file that cannot be opened is named")
        gone = MEX.read(["Signed.cs", "no-such-file.cs"], root=where)
        check([c["at"] for c in gone["unreadable"]] == ["no-such-file.cs"],
              "the missing file is named")
        check(len(gone["found"]) == 1,
              "and the readable one still came back")
        check(gone["of"] == 2, "with both counted in `of`")

        print("\n7. every failure is named and reached")
        for these, name in (([], "NOTHING_TO_READ"),
                            (None, "NOTHING_TO_READ"),
                            ([123], "NOT_AN_ITEM"),
                            ([{"kind": "fragment"}], "NOT_AN_ITEM"),
                            ([{"at": "   "}], "NOT_AN_ITEM")):
            said = MEX.read(these, root=where)
            reached.add(said.get("refused"))
            check(said.get("refused") == name, "%s is reached" % name)

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-IMP-MEX-006.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 2, "the contract declares 2 failures")
        for failure in named:
            check(failure in logic, "the code names %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(answer["unjudged"]) == 6, "six things are left unjudged")
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the author is asked for, never derived")
    return 0


if __name__ == "__main__":
    sys.exit(main())
