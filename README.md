# Forge Gameplay Framework
## A gameplay framework for developing games using C#.

Forge is an engine-agnostic gameplay framework designed for building robust game systems in C#. Inspired by Unreal Engine's Gameplay Ability System (GAS), Forge provides a centralized and controlled approach to managing attributes, effects, tags, and cues in your games.

The framework eliminates the need to rebuild status systems for every game project by offering a flexible, data-driven architecture that works seamlessly with both Unity and Godot engines. With Forge, all attribute changes are handled through effects, ensuring organized and maintainable code even in complex gameplay scenarios.

**Keywords:** gameplay framework, C# game development, Unity framework, Godot framework, attribute system, status effects, gameplay abilities, game engine agnostic, data-driven gameplay

## Architecture Overview

Forge is built around five core systems that work together to provide comprehensive gameplay functionality:

### Core Components

- **Attributes**: Centralized attribute management with min/max values and controlled modifications
- **Effects**: Data-driven system for applying temporary or permanent changes to entities
- **Tags**: Hierarchical tagging system for entity classification and effect targeting
- **Cues**: Visual and audio feedback system with multiplayer replication support
- **Executions**: Custom logic execution within the effects pipeline

### Entity System

Every game object that uses Forge implements the `IForgeEntity` interface, providing:
- `EntityAttributes` - Manages all attributes and attribute sets
- `EntityTags` - Handles base and modifier tags with automatic inheritance
- `EffectsManager` - Controls effect application, stacking, and lifecycle

### Effects Pipeline

Effects support multiple application types:
- **Instant Effects**: Execute immediately and modify attributes
- **Duration Effects**: Apply over time with automatic cleanup
- **Periodic Effects**: Execute at regular intervals during their lifetime
- **Stackable Effects**: Controls multiple instances with configurable stacking rules

## Project Status

⚠️ **Work in Progress** - This project is currently under active development and not ready for production use.

### Current Features ✅

- **Tags System**: Complete hierarchical tag system with inheritance
- **Attributes System**: Full attribute management with modifiers and overrides
- **Effects System**: Comprehensive effect application with stacking support
- **Cues System**: Visual feedback system for effect application/removal
- **Executions**: Custom effect logic execution

### Planned Features 🚧

- **Abilities System**: Complete ability system similar to GAS abilities
- **Multiplayer Support**: Network replication for all systems
- **Events System**: Gameplay event handling and propagation

### Known Limitations

- Abilities system not yet implemented
- No multiplayer/networking support currently
- Events system pending implementation

## Related Projects

A Godot-specific plugin version of this framework is in development in a separate repository. The core Forge library is compatible with Unity out of the box.

## Contributing

This project is not currently accepting contributions as it's still in early development. However, if you're interested in contributing or have suggestions, feel free to reach out via GitHub issues or discussions.

## License

Copyright © Gamesmiths Guild. See [LICENSE](LICENSE) for details.
