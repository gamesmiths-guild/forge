# QueryActiveEffectsResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.QueryActiveEffectsResolver`
> **Output Type:** `ActiveEffectHandle[]`

Resolves the handles of the active effects on a resolved entity, optionally filtered by an `EffectData`. Use this to reach active effects the current graph did not apply itself, for example a "dispel" that feeds the result into a [RemoveEffectNode](../nodes/action/remove-effect-node.md).

## Constructor

```csharp
new QueryActiveEffectsResolver(effectData, entityResolver = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| effectData | `EffectData?` | The effect data to filter by, or `null` to return every active effect. |
| entityResolver | `IEntityResolver` | Selects which entity to inspect. Defaults to `AbilityOwnerResolver`. |

## Behavior

- Resolves the entity; returns an empty array when it is not available.
- Returns `EffectsManager.GetActiveEffects(effectData)` (or `GetActiveEffects()` when unfiltered) as an array.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectArrayProperty("targetDebuffs",
    new QueryActiveEffectsResolver(poisonData, new AbilityTargetResolver()));
```

## Composition

```csharp
// Only the shortest-lived matching debuff, via the array pipeline
graph.VariableDefinitions.DefineObjectProperty("shortestDebuff",
    new ObjectFirstResolver<ActiveEffectHandle>(
        new ObjectOrderByResolver<ActiveEffectHandle>(
            new QueryActiveEffectsResolver(poisonData, new AbilityTargetResolver()),
            new ActiveEffectDataResolver(
                new ElementResolver<ActiveEffectHandle>(),
                ActiveEffectDataType.RemainingDuration))));
```

## See Also

- [Resolvers Overview](README.md)
- [RemoveEffectNode](../nodes/action/remove-effect-node.md)
- [ActiveEffectDataResolver](active-effect-data-resolver.md)
- [EffectInfoResolver](effect-info-resolver.md)
