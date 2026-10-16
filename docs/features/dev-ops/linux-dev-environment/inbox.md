# dev-ops/linux-dev-environment - Inbox

> The loop adds one line for each item that reaches a run at any point: a bug, a change, or a note. It acts on the open lines after each phase. A done line stays. Section 12 of `.beast-mode/phase-handoff.md` holds the rules and the line shape.

## Items
- [x] change medium 2026-10-16 12:20 | Plan fix for task 1.4: do not use `ldconfig -p` to find the unversioned `libvulkan.so`. The author installed `libvulkan-dev`. `/usr/lib/x86_64-linux-gnu/libvulkan.so -> libvulkan.so.1` now exists. `ldconfig -p` lists only SONAMEs (`libvulkan.so.1`), never the unversioned dev link, so the planned check always fails. Do this instead: take the directory of the `libvulkan.so.1` x86-64 entry from `ldconfig -p`, then test `-e "<that dir>/libvulkan.so"`. When it is missing, fail with the fix `sudo apt install libvulkan-dev`. Update implementation.md (table row 5, task 1.4, Decision 2, runbook row of task 3.2) to match. Name this change in the Notes section of your report. | done: applied on the direction of the coordinator. Plan edited (step table row 5, task 1.4, Decision 2, task 3.2 runbook row). Code fix by phase-1-fix1.md. Verifier PASS with no shim.
