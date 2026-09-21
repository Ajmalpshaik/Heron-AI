#!/bin/bash
# Heron-Agent:  none
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md
#
# ---------------------------------------------------------------------------
# Heron AI - the cloud environment setup script.
#
# This is not run from a checkout. It is PASTED into the Setup script box of
# a Claude Code cloud environment, and docs/38-the-cloud-environment.md is
# the page that says what goes in the other three boxes.
#
# Ubuntu 24.04, runs as root, before Claude Code launches. It MUST exit 0: a
# non-zero exit fails the session. The filesystem is snapshotted afterwards,
# so this runs once and not once per session - which is why it is worth
# warming the two slow caches here rather than paying for them every time.
# ---------------------------------------------------------------------------
say() { echo "[heron] $*"; }

REPO="$PWD"
[ -f "$REPO/HERON_CONSTITUTION.md" ] || REPO="$(find /home /root /workspace \
  -maxdepth 4 -name HERON_CONSTITUTION.md -printf '%h\n' 2>/dev/null | head -1)"
say "repository: ${REPO:-NOT FOUND}"

# --- 1. the two installs, in parallel --------------------------------------
# The .NET 10 SDK, not the 8 one: the 8 SDK omits the WindowsDesktop MSBuild
# targets and the add-in dies MSB4019 on Revit 2025-2027. An SDK builds
# frameworks older than itself, so the newest is the one to install even when
# the release being checked is the oldest. docs/30 s2a measured both.
#
# `apt-get update` first is NOT decoration - without it this failed with ten
# 404s on a fresh 24.04 image, because the shipped index named a version the
# pool had moved past, and the 404s read exactly like a blocked CDN.
#
# python-is-python3 because .mcp.json runs `python` and 24.04 ships `python3`
# only. Without it the MCP server never starts and nothing says why.
(
  apt-get update -qq &&
  apt-get install -y -qq --no-install-recommends dotnet-sdk-10.0 python-is-python3
) > /tmp/heron-apt.log 2>&1 &
apt_pid=$!

# PyYAML is the only REQUIRED one - nothing in brain/ or tools/ runs without
# it. The rest degrade rather than break:
#   mcp         serves .mcp.json's heron server, so the heron_* tools exist
#   model2vec   the trained embedding backend (needs huggingface.co - docs/38)
#   sqlite-vec  vector search inside SQLite instead of in Python
#   pypdf       ingesting PDFs
# sentence-transformers is deliberately LEFT OUT: it pulls torch, 500 MB to
# 2 GB, to re-order a shortlist it already has.
PKGS="pyyaml mcp model2vec sqlite-vec pypdf"
(
  pip install --quiet --upgrade $PKGS ||
  pip install --quiet --upgrade --break-system-packages $PKGS
) > /tmp/heron-pip.log 2>&1 &
pip_pid=$!

wait "$apt_pid" || say "apt did not finish cleanly - /tmp/heron-apt.log"
wait "$pip_pid" || say "pip did not finish cleanly - /tmp/heron-pip.log"
say "dotnet $(dotnet --version 2>/dev/null || echo 'NOT INSTALLED')"
say "python $(python --version 2>&1)"

# --- 2. somewhere to keep knowledge ----------------------------------------
# heron_scope.knowledge_dir() reads %APPDATA% first and returns None on Linux,
# and every brain call then dies with "nowhere to keep knowledge". /opt rather
# than $HOME because this script is root and the session may not be.
export HERON_KNOWLEDGE=/opt/heron-kb
mkdir -p "$HERON_KNOWLEDGE" && chmod 777 "$HERON_KNOWLEDGE"

# --- 3. warm the two slow caches, in parallel ------------------------------
if [ -n "$REPO" ] && cd "$REPO"; then

  # Fills the store with every fragment. Without it the first heron_lookup
  # answers "the knowledge store is EMPTY - this is not a retrieval result."
  # Safe to snapshot: heron_search.library_digest() hashes the fragment files
  # as BYTES, not by mtime, so the fresh clone each session starts from does
  # not invalidate it and a real edit does.
  ( timeout 180 python brain/heron_scope.py --rebuild ) \
    > /tmp/heron-rebuild.log 2>&1 &
  kb_pid=$!

  # The Revit API reference packages for 2024, the release everything is
  # proved on. They land in ~/.nuget/packages, OUTSIDE the repository, so the
  # snapshot keeps them and the first check-compile is minutes shorter.
  # EnableWindowsTargeting is what lets an SDK not running on Windows restore
  # the Windows targeting packs.
  ( timeout 120 dotnet restore revit/Heron.Revit.Addin \
      -p:RevitVersion=2024 -p:EnableWindowsTargeting=true ) \
    > /tmp/heron-restore.log 2>&1 &
  nuget_pid=$!

  wait "$kb_pid"    || say "store did not rebuild - /tmp/heron-rebuild.log"
  wait "$nuget_pid" || say "NuGet warm-up unfinished - check-compile just runs longer"
fi

# NOTHING IS BUILT INTO THE REPOSITORY TREE ON PURPOSE. The bridge test host
# builds and runs here perfectly well - 32 checks, docs/30 s5 - but a host
# left inside the snapshot is a STALE host, and a stale one reads exactly like
# a bridge defect. Build it in-session, fresh:
#
#   dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024 \
#     -p:HeronTfm=net8.0 -p:OutputPath=bin/x64/Debug-net8.0/
#   python tests/test_bridge_roundtrip.py

say "ready"
exit 0
