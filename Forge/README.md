# Forge Gameplay System

A gameplay framework for C# built around the concepts of Unreal's Gameplay Ability System: attributes, effects, abilities, gameplay tags, events, cues and visual ability scripting, with no engine dependency.

Every game rebuilds the same machinery: Health that can't go below zero, a poison that ticks, a buff that stacks, a fireball with a mana cost and a cooldown. Forge gives you that machinery once, as data and small interfaces, so it works the same in Unity, Godot, a custom engine, a server or a unit test. Attributes and tags only change through effects, so every change can be timed, stacked, blocked, reacted to and shown to the player in the same way.

## Features

- **Attributes**: integer stats with min/max, layered modifiers and calculation channels.
- **Effects**: instant, timed and infinite effects with periodic execution, stacking, components and custom calculators.
- **Tags**: hierarchical gameplay tags with queries, requirements and immunity.
- **Abilities**: granting, activation, costs, cooldowns, tag rules, triggers and instancing.
- **Events**: tag-routed gameplay events that can trigger abilities.
- **Cues**: presentation hooks that keep visuals and audio out of gameplay code.
- **Statescript**: ability behavior as a graph of nodes, built in code or in an engine's visual editor.

## Getting started

Follow the [Quick Start](https://github.com/gamesmiths-guild/forge/blob/main/docs/quick-start.md): a hero with Health and Mana, a burning effect and a heal ability with a cost and a cooldown, in about ten minutes. The full [documentation](https://github.com/gamesmiths-guild/forge/tree/main/docs) covers every system.

Using Godot? [Forge for Godot](https://github.com/gamesmiths-guild/forge-godot) adds nodes, Inspector resources and a visual Statescript editor.

Forge targets **.NET Standard 2.1** (broad compatibility) and **.NET 8**. The package ships with Source Link and symbols.

## License

Forge is licensed under the [MIT License](https://github.com/gamesmiths-guild/forge/blob/main/LICENSE).
