# AbilityLevelResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityLevelResolver`
> **Output Type:** `int`

Reads the current ability level from `AbilityBehaviorContext.Level`.

## Constructor

```csharp
new AbilityLevelResolver()
```

## Behavior

- Reads the active `AbilityBehaviorContext` from `GraphContext.ActivationContext`.
- Returns the current ability level when the graph is executing inside an ability behavior.
- Returns `0` when no compatible ability activation context is available.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("level",
    new AbilityLevelResolver());
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("bonusLevel",
    new AddResolver(
        new AbilityLevelResolver(),
        new VariantResolver(new Variant128(1), typeof(int))));
```

## See Also

- [AbilityOwnershipResolver](ability-ownership-resolver.md)
- [OwnershipResolver](ownership-resolver.md)
- [EffectFromDataResolver](effect-from-data-resolver.md)
- [ApplyEffectNode](../nodes/action/apply-effect-node.md)
- [EffectNode](../nodes/state/effect-node.md)
