# Style Parser - Requirements

**Epic:** style-system
**Created:** 2026-10-21

## Overview

This feature gives the text format of a stylesheet and the parser that makes a `StyleSheet` from it. It adds the tokenizer, the parser, theme variables, and the error report. It lives in `BrutalUi.Markup` and references Core only. It also moves the HelloPanel sample to a stylesheet, so the GPU smoke run shows the cascade.

## Requirements

- The format is the CSS subset of the technical design: a selector list, a block of `name: value;` declarations, and comments.
- The parser accepts each selector form of stylesheet-model and selector-matching, and each property of `StyleProperties`.
- Value forms: a length in `px` or `%`, `auto`, a number, a color as `#rgb`, `#rrggbb`, or `#rrggbbaa`, a keyword for an enum member, and one to four values for an edge set.
- A theme variable `--name: value;` in a `:root` block resolves at parse time, for each sheet. `var(--name)` in a value reads it. An unknown variable is a parse error.
- The parser reports an error with a line, a column, and a message. A sheet with an error produces no `StyleSheet`.
- Parsing runs at load time. The parser can allocate. The sheet it produces allocates nothing in the frame loop.
- The format declares a transition as `transition: <property> <duration> <easing>;`. The parser stores it in the rule for the transitions feature.
- The HelloPanel sample loads a sheet and sets classes instead of inline paint values. The smoke run keeps its 3 lines and its exit codes.
- Tests in `BrutalUi.Markup.Tests` cover each value form, each selector form, the variables, and the error positions.

## Dependencies

- stylesheet-model and selector-matching must be complete. The parser writes types that already match.
- Can run in parallel with transitions.

## Out of Scope

- The XML element loader. The markup epic of Phase 3 does that.
- Runtime theme variables, a cascade across sheets, `@import`, and hot reload.
- Shorthand properties beyond the edge sets, and calc expressions.
