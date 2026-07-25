# EffectStackDataResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EffectStackDataResolver`
> **Output Type:** `int`

Aggregates stack data over the active applications of a given `EffectData` on a resolved entity. `TotalStackCount` is the "current number of stacks" query, it sums the stacks of every active application of the effect on the entity.

## Constructor

```csharp
new EffectStackDataResolver(effectData, dataType, entityResolver = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| effectData | `EffectData` | The effect data to query for. |
| dataType | `EffectStackDataType` | Which aggregate to compute. |
| entityResolver | `IEntityResolver` | Selects which entity to inspect. Defaults to `AbilityOwnerResolver`. |

### `EffectStackDataType` values

- `TotalStackCount`: sum of stack counts across every active application.
- `InstanceCount`: number of active applications.
- `MaxLevel`: highest level among the active applications.

## Behavior

- Resolves the entity; returns `0` when it is not available or the effect is not active.
- Aggregates over `EffectsManager.GetEffectStackData(effectData)`.

## Usage

```csharp
// Current stack count of a poison on the owner
graph.VariableDefinitions.DefineProperty("poisonStacks",
    new EffectStackDataResolver(poisonData, EffectStackDataType.TotalStackCount));
```

## Composition

```csharp
// Branch when a stacking mark reaches its cap (5 stacks)
graph.VariableDefinitions.DefineProperty("markMaxed",
    new ComparisonResolver(
        new EffectStackDataResolver(markData, EffectStackDataType.TotalStackCount),
        ComparisonOperation.GreaterThanOrEqual,
        new VariantResolver(new Variant128(5), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [QueryActiveEffectsResolver](query-active-effects-resolver.md)
- [Effects: Stacking](../../effects/stacking.md)
