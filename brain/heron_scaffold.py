# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-NCR-020
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
.NET project creation - the layer decides what it may reference, and the
layer table is read.

    python brain/heron_scaffold.py Heron.Reporting platform

WHAT IT IS FOR (docs/28, HERON-DEV-NCR-020)
--------------------------------------------
"CREATES NEW. Authors project files, target frameworks, references,
build configuration for a new component." T1, risk MODIFY.

THREE OF THOSE FOUR ARE ALREADY DECIDED SOMEWHERE
---------------------------------------------------
    TARGET FRAMEWORK   `$(HeronTfm)`. Every existing project writes
                       exactly that and nothing else, because
                       Directory.Build.props maps the Revit release to a
                       runtime and D-05 says never guess one. A new
                       project that named a framework directly would be
                       the ninth place a release lives
                       (HERON-DEV-NUP-019 counts eight already).

    REFERENCES         D-48's layering, which tools/check-structure.py
                       holds as `ALLOWED`. A project may reference the
                       layers its own row names and no others.

    BUILD CONFIG       Directory.Build.props again. It is why the
                       existing project files are nine lines long.

So almost nothing here is a choice, and the agent's job is to make the
one file that follows from the three - and to REFUSE a reference the
layer forbids before anybody compiles it.

THE LAYER TABLE IS READ OUT OF THE GATE THAT ENFORCES IT
----------------------------------------------------------
Parsed from tools/check-structure.py rather than copied, because brain
may not import tools/ (D-48 again) and a second copy of a layering table
is the thing layering exists to prevent. If a row there changes, this
follows it with no edit.

THE SHAPE COMES FROM A PROJECT THAT EXISTS
--------------------------------------------
Not invented. `Heron.Core.csproj` is read and its structure followed -
the SDK attribute, the three properties, the comment - so a new project
looks like the ones beside it rather than like whatever was fashionable
the day it was written.

IT WRITES NOTHING
-------------------
The register gives this row MODIFY and what comes back is the file's
TEXT and where it would go. Creating it is the caller's act, the same
line every other MODIFY row in this session draws - and the reason here
is that a project file appearing in the tree changes what the whole
solution builds.
"""

from __future__ import annotations

import ast
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The gate that enforces the layering, read rather than imported.
STRUCTURE = os.path.join(ROOT, "tools", "check-structure.py")

# The project that already exists, used as the shape.
TEMPLATE = os.path.join(ROOT, "platform", "Heron.Core", "Heron.Core.csproj")

# The one thing every project's TargetFramework says. Never a literal:
# Directory.Build.props maps the release to a runtime, and a project
# naming a framework directly would be a ninth place a release lives.
TFM = "$(HeronTfm)"

# A component name, as this project spells one.
_NAME = re.compile(r"^Heron(\.[A-Z][A-Za-z0-9]*)+$")

# Where each layer's projects live, read from where the existing ones are
# rather than declared.
FOLDERS = {"platform": "platform", "revit": "revit", "mcp": "mcp",
           "brain": "brain", "tests": "tests", "tools": "tools"}


def layers(path=None):
    """
    D-48's table, parsed out of the gate that enforces it.

    Not imported - brain may not import tools/ - and not copied, because
    a second copy of a layering table is the thing layering exists to
    prevent.
    """
    try:
        tree = ast.parse(io.open(path or STRUCTURE,
                                 encoding="utf-8").read())
    except (IOError, OSError, SyntaxError):
        return {}
    for node in tree.body:
        if not isinstance(node, ast.Assign):
            continue
        if not any(getattr(one, "id", None) == "ALLOWED"
                   for one in node.targets):
            continue
        try:
            return ast.literal_eval(node.value)
        except (ValueError, SyntaxError):
            return {}
    return {}


def _shape(path=None):
    """The SDK attribute and property names an existing project uses."""
    try:
        text = io.open(path or TEMPLATE, encoding="utf-8").read()
    except (IOError, OSError):
        return None
    sdk = re.search(r'<Project\s+Sdk="([^"]+)"', text)
    return {"sdk": sdk.group(1) if sdk else None,
            "properties": re.findall(r"<(\w+)>\$?\(?[^<]*</\1>", text)}


def projects_in(layer, root=None):
    """
    Every .csproj under one layer's folder, relative to the repository.

    FOUND, NEVER SPELLED. The first version wrote
    `..\\..\\brain\\Heron.Brain\\Heron.Brain.csproj` from the layer name -
    and no such file exists. brain/ is PYTHON and has no assembly at all,
    and platform's project is called Heron.Core. A reference to a project
    that is not there is a build error the agent handed somebody.
    """
    here = os.path.join(root or ROOT, FOLDERS.get(layer, layer))
    out = []
    if not os.path.isdir(here):
        return out
    for folder, _folders, names in os.walk(here):
        for name in sorted(names):
            if name.endswith(".csproj"):
                out.append(os.path.relpath(os.path.join(folder, name),
                                           root or ROOT))
    return sorted(out)


def author(name, layer, references=None, why=None, root=None):
    """
    {authored, path, text, refused} - or a refusal. Nothing is written.
    """
    name = str(name or "").strip()
    if not name:
        return {"authored": False, "refused": "NO_NAME",
                "why": "no component name was given."}
    if not _NAME.match(name):
        return {"authored": False, "refused": "NOT_A_NAME",
                "why": "%r is not how this project names a component. They "
                       "are Heron.Something, in PascalCase - Heron.Core, "
                       "Heron.Bridge, Heron.Revit.Addin." % name}

    layer = str(layer or "").strip().lower()
    table = layers()
    if not table:
        return {"authored": False, "refused": "NO_LAYER_TABLE",
                "why": "D-48's layering table could not be read out of %s. "
                       "Authoring a project without knowing what its layer "
                       "may reference is how the boundary gets crossed."
                       % os.path.relpath(STRUCTURE, ROOT)}
    if layer not in table:
        return {"authored": False, "refused": "NOT_A_LAYER",
                "why": "%r is not one of Heron's layers. They are %s, and a "
                       "ninth is not invented here."
                       % (layer, ", ".join(sorted(table)))}

    wanted = [str(one).strip().lower() for one in (references or [])
              if str(one).strip()]
    allowed = set(table[layer])
    forbidden = [one for one in wanted if one not in allowed]
    if forbidden:
        return {"authored": False, "refused": "LAYER_FORBIDS",
                "why": "%s may reference %s. It may not reference %s - "
                       "D-48, and tools/check-structure.py would refuse "
                       "the result."
                       % (layer, ", ".join(sorted(allowed)) or "nothing",
                          ", ".join(forbidden))}

    # EVERY REFERENCE HAS TO RESOLVE TO A PROJECT THAT EXISTS. D-48's
    # table is about imports in BOTH languages; a C# ProjectReference can
    # only point at a C# project, and brain/ is Python.
    resolved, missing, several = {}, [], []
    for one in wanted:
        found = projects_in(one, root)
        if not found:
            missing.append(one)
        elif len(found) > 1:
            several.append((one, found))
        else:
            resolved[one] = found[0]
    if missing:
        return {"authored": False, "refused": "NO_PROJECT_IN_LAYER",
                "why": "%s holds no .csproj, so a C# project cannot "
                       "reference it. D-48's table is about imports in "
                       "BOTH languages - brain/ is Python and has no "
                       "assembly at all - and a reference to a project "
                       "that is not there is a build error rather than a "
                       "layering one."
                       % ", ".join(sorted(missing))}
    if several:
        return {"authored": False, "refused": "WHICH_PROJECT",
                "why": "%s holds more than one project: %s. Which one is a "
                       "choice, and picking the first would be a guess "
                       "(D-33)."
                       % (several[0][0], ", ".join(several[0][1]))}

    shape = _shape()
    if not shape or not shape.get("sdk"):
        return {"authored": False, "refused": "NO_TEMPLATE",
                "why": "%s could not be read, so there is no existing "
                       "project to take the shape from. Inventing one "
                       "would make the new file look like nothing beside "
                       "it." % os.path.relpath(TEMPLATE, ROOT)}

    folder = FOLDERS.get(layer, layer)
    where = os.path.join(folder, name, "%s.csproj" % name)

    lines = ['<Project Sdk="%s">' % shape["sdk"], ""]
    lines.append("  <!--")
    lines.append("    %s" % (why or "A new %s component." % layer))
    lines.append("")
    lines.append("    Layer %s. It may reference %s."
                 % (layer, ", ".join(sorted(allowed)) or "nothing"))
    lines.append("    tools/check-structure.py enforces that; this file "
                 "does not restate it.")
    lines.append("  -->")
    lines.append("")
    lines.append("  <PropertyGroup>")
    lines.append("    <TargetFramework>%s</TargetFramework>" % TFM)
    lines.append("    <RootNamespace>%s</RootNamespace>" % name)
    lines.append("    <AssemblyName>%s</AssemblyName>" % name)
    lines.append("  </PropertyGroup>")
    if wanted:
        lines.append("")
        lines.append("  <ItemGroup>")
        for one in sorted(wanted):
            # RELATIVE TO WHERE THIS PROJECT WOULD SIT, computed from the
            # real path rather than spelled out of the layer name.
            here = os.path.dirname(where)
            step = os.path.relpath(resolved[one], here).replace("/", "\\")
            lines.append('    <ProjectReference Include="%s" />' % step)
        lines.append("  </ItemGroup>")
    lines.append("")
    lines.append("</Project>")
    text = "\n".join(lines) + "\n"

    return {
        "authored": True,
        "name": name,
        "layer": layer,
        "path": where,
        "text": text,
        "targetFramework": TFM,
        "mayReference": sorted(allowed),
        "references": sorted(wanted),
        "referencePaths": dict(resolved),
        "written": False,
        "why": "%s would go at %s, targeting %s, referencing %s. Nothing "
               "was written."
               % (name, where, TFM,
                  ", ".join(sorted(wanted)) or "nothing"),
        "unjudged": [
            "THE TARGET FRAMEWORK IS %s AND NEVER A LITERAL. "
            "Directory.Build.props maps the Revit release to a runtime and "
            "D-05 says never guess one; a project naming a framework "
            "directly would be a ninth place a release lives, and "
            "HERON-DEV-NUP-019 counts eight already." % TFM,
            "THE LAYER TABLE WAS READ OUT OF tools/check-structure.py, not "
            "copied. brain may not import tools/ (D-48), and a second copy "
            "of a layering table is the thing layering exists to prevent - "
            "so a row changing there is followed here with no edit.",
            "THE SHAPE CAME FROM A PROJECT THAT EXISTS. %s was read and "
            "its SDK attribute and properties followed, so the new file "
            "looks like the ones beside it."
            % os.path.relpath(TEMPLATE, ROOT),
            ("IT MAY REFERENCE %s, AND THAT WAS CHECKED BEFORE ANYTHING "
             "WAS AUTHORED. A reference the layer forbids is refused here "
             "rather than by a compiler, which is cheaper and says why."
             % (", ".join(sorted(allowed)) or "NOTHING")),
            "EVERY REFERENCE RESOLVES TO A PROJECT THAT EXISTS. D-48's "
            "table is about imports in BOTH languages, and a C# "
            "ProjectReference can only point at a C# project - brain/ is "
            "Python and has no assembly, so `mcp may reference brain` is "
            "true of the Python and impossible in a .csproj.",
            "NOTHING WAS WRITTEN. The text and the path come back; "
            "creating the file is the caller's act, because a project "
            "appearing in the tree changes what the whole solution "
            "builds.",
            "WHETHER THIS COMPONENT SHOULD EXIST AT ALL. "
            "HERON-AHR-WFP-015 is the row that asks whether an existing "
            "one could be extended instead, and it is the guard against "
            "exactly the kind of growth a project-creation agent makes "
            "easy.",
        ],
    }


def main(argv):
    print("DOTNET PROJECT CREATION   the layer decides what it may "
          "reference")
    print("=" * 72)

    table = layers()
    print("\nD-48's table, read out of tools/check-structure.py")
    for layer in sorted(table):
        print("  %-10s may reference %s"
              % (layer, ", ".join(sorted(table[layer])) or "nothing"))

    name = argv[0] if argv else "Heron.Reporting"
    layer = argv[1] if len(argv) > 1 else "mcp"
    answer = author(name, layer, references=["platform"],
                    why="Reports, rendered away from Revit.")
    print("\n%s" % answer["why"])
    print("\n--- %s ---" % answer["path"])
    print(answer["text"])

    print("refused")
    for these, layer, refs in ((None, "platform", None),
                               ("reporting", "platform", None),
                               ("Heron.X", "middleware", None),
                               ("Heron.X", "platform", ["revit"]),
                               ("Heron.X", "brain", ["revit", "mcp"]),
                               ("Heron.X", "mcp", ["brain"]),
                               ("Heron.X", "tests", ["revit"])):
        bad = author(these, layer, references=refs)
        print("  %-18s %s" % (bad["refused"], bad["why"][:48]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
