@echo off
cd /d "%~dp0"
start "" "ngrok.exe" --ngrok http 5170 -url https://seldom-pampered-fringe.ngrok-free.dev