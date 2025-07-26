# Forge Gameplay Framework

A gameplay framework for developing games using C#.

Forge is an engine-agnostic gameplay framework designed for building robust game systems in C#. Inspired by Unreal Engine's Gameplay Ability System (GAS), Forge provides a centralized and controlled approach to managing attributes, effects, tags, and cues in your games.

The framework eliminates the need to rebuild status systems for every game project by offering a flexible, data-driven architecture that works seamlessly with Unity, Godot, and other C#-compatible engines. With Forge, all attribute changes are handled through effects, ensuring organized and maintainable code even in complex gameplay scenarios.

**Keywords:** gameplay framework, C# game development, Unity framework, Godot framework, attribute system, status effects, gameplay abilities, game engine agnostic, data-driven gameplay

## Quick Start

New to Forge? Check out the [Quick Start Guide](docs/quick-start.md) to build your first Forge-powered entity in minutes.

## Architecture Overview

Forge is built around four core systems that work together to provide comprehensive gameplay functionality:

### Core Systems

- **[Attributes](docs/attributes.md)**: Centralized attribute management with min/max values, channels, and controlled modifications.
- **[Effects](docs/effects/README.md)**: Data-driven system for applying temporary or permanent changes to entities.
- **[Tags](docs/tags.md)**: Hierarchical tagging system for entity classification and effect targeting.
- **[Cues](docs/cues.md)**: Visual and audio feedback system that bridges gameplay with presentation.

### Entity Integration

Every game object that uses Forge implements the `IForgeEntity` interface, providing:

- `EntityAttributes` - Manages all attributes and attribute sets.
- `EntityTags` - Handles base and modifier tags with automatic inheritance.
- `EffectsManager` - Controls effect application, stacking, and lifecycle.

### Advanced Features

Forge supports a variety of gameplay mechanics through specialized subsystems:

- **[Effect Duration](duration.md)**: Control how long effects remain active.
- **[Effect Stacking](stacking.md)**: Configure how multiple instances of effects combine.
- **[Periodic Effects](periodic.md)**: Create effects that execute repeatedly on a schedule.
- **[Modifiers](modifiers.md)**: Define how effects change attribute values.
- **[Effect Components](components.md)**: Extend effects with custom behaviors.
- **[Custom Calculators](calculators.md)**: Flexible logic execution within the effects pipeline.

## Project Status

⚠️ **Work in Progress** - This project is currently under active development and not ready for production use.

### Current Features ✅

- **Tags System**: Complete hierarchical tag system with inheritance.
- **Attributes System**: Full attribute management with modifiers and overrides.
- **Effects System**: Comprehensive effect application with stacking support.
- **Cues System**: Visual feedback system for effect application/removal.
- **Custom Calculators**: Flexible logic execution for effects.

### Planned Features 🚧

- **Abilities System**: Complete ability system similar to GAS abilities.
- **Multiplayer Support**: Network replication for all systems.
- **Events System**: Gameplay event handling and propagation.

## Installation

### Requirements

Forge targets:

- .NET Standard 2.1 (for broad compatibility)
- .NET 8 (for modern features)

### Package Manager

Package manager support coming soon.

### Manual Installation

1. Clone the repository.
2. Reference the Forge project in your solution.
3. Follow the [Quick Start Guide](docs/quick-start.md).

## Documentation

For comprehensive documentation, explore the [docs](docs) directory, starting with the [documentation overview](docs/README.md).

## Contributing

This project is not currently accepting contributions as it's still in early development. However, if you're interested in contributing or have suggestions, feel free to reach out via GitHub issues or discussions.

## License

Copyright © Gamesmiths Guild. See [LICENSE](LICENSE) for details.
