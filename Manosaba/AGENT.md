# AGENT.md

Project-specific rules for coding agents working in this repository.

## Project

- Project: `Manosaba`, a C# Slay the Spire 2 mod.
- Solution: `Manosaba.sln`
- Main project: `Manosaba.csproj`

## Build

Use the standard project build flow:

```bash
dotnet build -nologo
```

If a local `ModsPath` is required, pass it explicitly from the local environment.

Do not modify `.csproj` files unless explicitly requested.

## Repository Layout

- Character content: `Characters/<CharacterName>/`
  - Cards: `Characters/<CharacterName>/Cards`
  - Powers: `Characters/<CharacterName>/Powers`
- Shared character content: `Characters/Common`
- Runtime patches: `Patches`
- Localization assets: `Manosaba/localization/<lang>/`
- Card images: `Manosaba/images/cards/<snake_case_card_id>.png`
- Power images: `Manosaba/images/powers/<snake_case_power_id>.png`

## Localization

For every new card, power, or keyword, update all supported languages:

- `eng`
- `zhs`
- `zht`
- `jpn`
- `kor`

Rules:

- Use UTF-8 without BOM.
- Preserve existing line endings.
- Place new entries near related same-character or same-feature entries.
- Use readable localized strings, not Unicode escape sequences.
- Apply `[gold]...[/gold]` to keywords, power names, card names, card pile names, and `Block`.
- Card keywords do not need to be repeated in card description text.
- Upgrade effects usually do not need explicit localization text unless requested.
- Prefer `DynamicVars` for tunable values.
- In `powers.json`, variables belong only in `smartDescription`, not `description`.
- In `cards.json`, dynamic card variables such as `{...:diff()}` are allowed and preferred when values are tunable.
- For non-English localization, do not manually invent translations for existing card/power names.
- Reuse the exact localized name from the existing corresponding `*.title` entry when referencing card/power names in descriptions.

## Cards & Powers

Follow existing project patterns for:

- `PathCustomCardModel`
- `PathCustomPowerModel`
- `CanonicalVars`
- `CanonicalKeywords`
- `ExtraHoverTips`

Rules:

- Keep implementation behavior and localization text aligned.
- Avoid hardcoded numbers when the value already exists as a dynamic or tunable variable.
- If text references a keyword, mechanic, card, or power with hover-tip support, add the matching `ExtraHoverTips`.
- Warn when a card has no effective upgrade behavior. If this is intentional, mention it in change notes.
- Prefer deterministic ordering for multiplayer-visible selection, replay, or collection logic.
- If a card targets or affects another player, teammates, or ally-player effects, set `MultiplayerConstraint` by default.
- If such a card is intentionally usable in solo/singleplayer, document the exception in change notes.
