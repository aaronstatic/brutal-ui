# brutal-ui

A retained-mode game UI framework for the BRUTAL C# framework, shipped as NuGet packages.

## Read first

- `docs/mission-statement.md` for the why and the boundaries.
- `docs/technical-design.md` for the package layout, the frame order, and the phase plan.
- `docs/feasibility.md` for what BRUTAL has and does not have.

## Rules

- Target .NET 10 and C# 14. Nullable on. `AllowUnsafeBlocks` on.
- Pin every `Ahwoo.Brutal.*` package in `Directory.Packages.props`.
- `BrutalUi.Core` references no ImGui package. The ImGui renderer lives in `BrutalUi.ImGui`.
- No managed allocation in update or draw after warm-up. The allocation test guards it.
- Use `Brutal.Numerics` vector types, UTF-8 text, file-scoped namespaces, and `required` create-info structs.
- Add no native dependency without a note in the feature `context.md`.
- Prose in docs and comments follows ASD-STE100: active voice, simple tenses, short sentences, no contractions.

## Workflow

Beast Mode manages features under `docs/features/`. Use `/plan-feature <name>` to plan and `/start-feature <name>` to begin. Tool rules for subagents live in `.beast-mode/tool-rules.md`.
