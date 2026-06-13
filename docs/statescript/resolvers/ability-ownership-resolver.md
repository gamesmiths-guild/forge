# AbilityOwnershipResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityOwnershipResolver`
> **Output Type:** `EffectOwnership`

Reads the current ability owner/source pair from `AbilityBehaviorContext`.

## Constructor

```csharp
new AbilityOwnershipResolver()
```

## Behavior

- Reads the active `AbilityBehaviorContext` from `GraphContext.ActivationContext`.
- Returns `abilityContext.Ownership`, which combines the current ability owner and source entities.
- Returns `new EffectOwnership(null, null)` when no compatible ability activation context is available.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "ownership",
    new AbilityOwnershipResolver());
```

## Composition

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "effect",
    new EffectFromDataResolver(
        effectData,
        ownershipResolver: new AbilityOwnershipResolver()));
```

## See Also

- [AbilityLevelResolver](ability-level-resolver.md)
- [AbilityOwnerResolver](ability-owner-resolver.md)
- [AbilitySourceResolver](ability-source-resolver.md)
- [OwnershipResolver](ownership-resolver.md)
- [EffectFromDataResolver](effect-from-data-resolver.md)
- [ApplyEffectNode](../nodes/action/apply-effect-node.md)
- [EffectNode](../nodes/state/effect-node.md)
