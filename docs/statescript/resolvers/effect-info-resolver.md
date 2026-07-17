# EffectInfoResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EffectInfoResolver`
> **Output Type:** `int`

Aggregates information over the active applications of a given `EffectData` on a resolved entity. `TotalStackCount` is the "current number of stacks" query, it sums the stacks of every active application of the effect on the entity.

## Constructor

```csharp
new EffectInfoResolver(effectData, infoType, entityResolver = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| effectData | `EffectData` | The effect data to query for. |
| infoType | `EffectInfoType` | Which aggregate to compute. |
| entityResolver | `IEntityResolver` | Selects which entity to inspect. Defaults to `AbilityOwnerResolver`. |

### `EffectInfoType` values

- `TotalStackCount`: sum of stack counts across every active application.
- `InstanceCount`: number of active applications.
- `MaxLevel`: highest level among the active applications.

## Behavior

- Resolves the entity; returns `0` when it is not available or the effect is not active.
- Aggregates over `EffectsManager.GetEffectInfo(effectData)`.

## Usage

```csharp
// Current stack count of a poison on the owner
graph.VariableDefinitions.DefineProperty("poisonStacks",
    new EffectInfoResolver(poisonData, EffectInfoType.TotalStackCount));
```

## Composition

```csharp
// Branch when a stacking mark reaches its cap (5 stacks)
graph.VariableDefinitions.DefineProperty("markMaxed",
    new ComparisonResolver(
        new EffectInfoResolver(markData, EffectInfoType.TotalStackCount),
        ComparisonOperation.GreaterThanOrEqual,
        new VariantResolver(new Variant128(5), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [QueryActiveEffectsResolver](query-active-effects-resolver.md)
- [Effects: Stacking](../../effects/stacking.md)
