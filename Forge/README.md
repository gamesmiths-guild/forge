# Forge Gameplay System

A gameplay framework for developing games using C#.

Forge is an engine-agnostic gameplay framework designed for building robust game systems in C#. Inspired by Unreal Engine's Gameplay Ability System (GAS), Forge provides a centralized and controlled approach to managing attributes, effects, tags, abilities, events, and cues in your games.

The framework eliminates the need to rebuild status systems for every game project by offering a flexible, data-driven architecture that works seamlessly with Unity, Godot, and other C#-compatible engines. With Forge, all attribute changes are handled through effects, ensuring organized and maintainable code even in complex gameplay scenarios.

## Features

- **Attributes**: Centralized attribute management with min/max values, channels, and controlled modifications.
- **Effects**: Data-driven system for applying temporary or permanent changes to entities.
- **Tags**: Hierarchical tagging system for entity classification and effect targeting.
- **Abilities**: Creation, granting, activation, cooldowns, costs, and instancing rules for gameplay abilities.
- **Events**: Gameplay event handling and propagation used for ability triggers and game logic reactions.
- **Cues**: Visual and audio feedback system that bridges gameplay with presentation.

## Installation

Forge supports:

- **.NET Standard 2.1** (broad compatibility)
- **.NET 8** (modern features)

Install via NuGet, reference the Forge project directly, or download the precompiled `.dll` from the Releases page.

## Documentation

Comprehensive documentation is available in the [docs](https://github.com/gamesmiths-guild/forge/tree/main/docs) directory.

## License

Forge is licensed under the [MIT License](https://github.com/gamesmiths-guild/forge/blob/main/LICENSE).
