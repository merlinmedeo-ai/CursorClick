VOLANTES CLICK EFFECTS - EVENT OPTIMIZED

Based on the confirmed-working v7 visual.

Changes:
- removed 12 ms mouse polling loop
- uses WH_MOUSE_LL event hook
- hook callback only queues the click and returns immediately
- three 120x120 WPF windows are pre-created at startup and reused
- WPF native DoubleAnimation drives the rings; PowerShell no longer updates each frame
- animation objects are allocated once and reused instead of recreated on every click
- expensive DropShadowEffect is disabled by default; colors, sizes and timing are preserved

LEFT CLICK  = two BLUE waves
RIGHT CLICK = two RED waves

Start: 00_START_HERE.bat
Stop: 01_STOP.bat
Emergency stop: 02_FORCE_STOP_ALL.bat
Debug: RUN_VISIBLE_DEBUG.cmd

Optional glow (more GPU load): start CursorClick.ps1 with -EnableGlow.
For unusually fast clicking, WaveSlotCount can be set from 1 to 6 (default: 3).

The exact original v7 script is included as CursorClick_ORIGINAL_V7.ps1 for rollback/reference.
