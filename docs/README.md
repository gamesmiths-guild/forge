# Forge Framework Documentation

Forge is a C# gameplay framework designed for building rich, modular game mechanics with a focus on flexibility, performance, and developer experience.

## Core Concepts

Forge's architecture is built around several interconnected systems that work together to provide a complete gameplay foundation:

### Entity System

At the center of Forge is the entity system, represented by `IForgeEntity`. Each game entity can have:

- **Attributes**: Numeric values that define an entity's capabilities and state.
- **Tags**: Labels that identify entity characteristics and enable queries.
- **Effects**: Gameplay effects that can modify attributes and behaviors.

Entities serve as containers for these components, allowing for modular construction of game objects.

### Attribute System

[Attributes](attributes.md) manage numeric values and their modifications:

- **Base Values**: The starting point for attribute calculations.
- **Modifiers**: Adjustments that affect attribute values through flat, percentage, or override operations.
- **Channels**: Multiple processing layers that allow for complex calculation pipelines.
- **Attribute Sets**: Collections of related attributes for logical organization.

Attributes can have minimum and maximum limits and support automatic recalculation when dependencies change.

### Tag System

[Tags](tags.md) in Forge are lightweight identifiers that mark entities with specific characteristics:

- **Tag Container**: Holds and manages an entity's tags.
- **Tag Queries**: Allow for efficient entity selection based on tag combinations.
- **Tag Requirements**: Define conditions for effect application.

Tags are central to many Forge systems, enabling contextual application of effects and conditional logic.

### Effect System

[Effects](effects/README.md) are the primary way to implement gameplay mechanics in Forge:

- **Modifiers**: Changes to attribute values.
- **Tags**: Added or removed during effect application.
- **Executions**: Custom logic that runs when effects are applied or removed.
- **Duration**: Controls how long effects remain active.

Effects can be instant or persistent, with various duration types including infinite, timed, and conditional.

### Cue System

[Cues](cues.md) bridge gameplay systems with visual and audio feedback:

- **Cue Events**: Triggered by gameplay events like effect application.
- **Cue Parameters**: Dynamic data that informs how feedback should be presented.
- **Cue Handlers**: Logic for converting gameplay events into sensory output.

This system keeps gameplay logic separate from presentation concerns.

## Architecture Overview

Forge's architecture follows these key principles:

1. **Composition over Inheritance**: Systems are designed to be attached to entities rather than inherited.
2. **Data-Driven Design**: Most gameplay elements are defined as data that can be authored and modified.
3. **Modularity**: Systems can be used independently or together based on project needs.
4. **Performance-Aware**: Optimized for both runtime performance and memory usage.
5. **Deterministic**: Systems prioritize predictable behavior for networking and replay support.

The framework avoids deep inheritance hierarchies, preferring flexible composition patterns that allow for greater code reuse and clearer architecture.

## Validation and Debugging

Forge includes a runtime validation system (`Validation` class) that helps catch misconfigurations and logic errors during development, editor, and testing workflows.

- By default, validation is **disabled**.
- Enable it by setting `Validation.Enabled = true;` in your editor, test, or debug environment.
- When enabled, failed validations will throw a `ValidationException` with an explanatory message.
- For optimal performance, leave validation disabled in production or deployment environments.

**Usage Example:**

```csharp
Validation.Enabled = true; // Enable validation (e.g., in editor)
Validation.Assert(someCondition, "Message if failed.");
```

## Documentation Structure

For more detailed information about specific systems, refer to these documentation files:

### Core Systems

- [Attributes](attributes.md): Details on attribute definition, evaluation, and channels.
- [Effects](effects/README.md): Creating and applying effects to entities.
- [Tags](tags.md): Using the tag system for entity identification.
- [Cues](cues.md): Connecting gameplay events to visual and audio feedback.

### Effect Features

- [Modifiers](effects/modifiers.md): How modifiers affect attribute calculations.
- [Duration](effects/duration.md): Controlling how long effects remain active.
- [Stacking](effects/stacking.md): Configuring how multiple instances of effects combine.
- [Periodic](effects/periodic.md): Creating effects that execute repeatedly over time.
- [Components](effects/components.md): Extending effects with custom behaviors.
- [Custom Calculators](effects/calculators.md): Creating custom calculations for effects.

### Getting Started

- [Quick Start](quick-start.md): Get up and running with Forge quickly.

## Getting Started

To start using Forge in your project, see the [Quick Start Guide](quick-start.md) for basic setup and examples of common gameplay mechanics.

For integrating Forge into your workflow, check the installation instructions and API reference in the main [README](../README.md) file.
