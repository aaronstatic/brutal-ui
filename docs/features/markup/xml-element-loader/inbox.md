# markup/xml-element-loader - Inbox

> The loop adds one line for each item that reaches a run at any point: a bug, a change, or a note. It acts on the open lines after each phase. A done line stays. Section 12 of `.beast-mode/phase-handoff.md` holds the rules and the line shape.

## Items
- [x] bug 2026-11-13 04:35 | Verifier FAIL: gate 9 (public-type grep over `git diff cf13f19 -- src`) read empty because the phase 1 files were untracked; `git diff` never shows untracked files. | done: parent recorded intent-to-add (`git add -N`, no commit); re-verifier PASS — 13 lines, the 13 public names; gate 9 PASS.
- [x] bug 2026-11-13 05:58 | Verifier FAIL: gate 7 docs grep — `grep -n "text-key" docs/technical-design.md` expects no output in phase 2; line 1324 holds the forward note "- The localised text row `text-key` comes with the localized-text work." | done: phase-2-fix1 brief; verifier round 2 PASS on all 10 gates
