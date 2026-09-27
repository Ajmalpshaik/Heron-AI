@echo off
rem Heron-Agent:  none
rem Heron-Step:   18
rem Heron-Status: DRAFT
rem Heron-Since:  0.1.0
rem Heron-Layer:  bridge
rem See docs/29-metadata-standard.md
rem
rem HERON TALK (D-104). Starts Claude Code in the Heron folder so that what the
rem modeller types or says with Revit's Talk button reaches THIS chat.
rem
rem   HERON_TALK=on   tells Heron's MCP server to listen - .mcp.json passes it on.
rem
rem   --dangerously-load-development-channels server:heron
rem                   is how Claude Code accepts a channel that is not on
rem                   Anthropic's approved list. It shows a warning screen
rem                   every time: choose "I am using this for local development".
rem
rem   --allowedTools mcp__heron
rem                   lets Heron's own tools run without asking in the terminal,
rem                   for this chat only - a request from Revit must not stop
rem                   at a prompt nobody is looking at. Changing the model
rem                   still needs Changes ON in Revit (D-19).
rem
rem Double-click it, or run it from a command prompt. Anything after it goes to
rem Claude Code as well, for example:   heron-talk.cmd --continue

setlocal
cd /d "%~dp0.."

where claude >nul 2>nul
if errorlevel 1 (
    echo Claude Code was not found on this PC, so Heron Talk cannot start.
    echo Install Claude Code and sign in once, then run this again.
    pause
    exit /b 1
)

set HERON_TALK=on
claude --dangerously-load-development-channels server:heron --allowedTools mcp__heron %*
endlocal
