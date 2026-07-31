# QueryActiveEffectsResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.QueryActiveEffectsResolver`
> **Output Type:** `ActiveEffectHandle[]`

Resolves the handles of the active effects on a resolved entity, optionally filtered by an `EffectData` or by a full [`EffectQuery`](../../effects/README.md#effectquery). Use this to reach active effects the current graph did not apply itself, for example a "dispel" that feeds the result into a [RemoveEffectNode](../nodes/action/remove-effect-node.md).

## Constructors

```csharp
new QueryActiveEffectsResolver(effectData, entityResolver = null)
new QueryActiveEffectsResolver(query, entityResolver = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| effectData | `EffectData?` | The effect data to filter by, or `null` to return every active effect. |
| query | `EffectQuery` | The query the active effects must match. An empty query returns every active effect. |
| entityResolver | `IEntityResolver` | Selects which entity to inspect. Defaults to `AbilityOwnerResolver`. |

## Behavior

- Resolves the entity; returns an empty array when it is not available.
- Returns `EffectsManager.GetActiveEffects(effectData)`, `GetActiveEffects(query)`, or `GetActiveEffects()` as an array, depending on which constructor was used.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectArrayProperty("targetDebuffs",
    new QueryActiveEffectsResolver(poisonData, new AbilityTargetResolver()));

// Dispel-by-category with no Where lambda
graph.VariableDefinitions.DefineObjectArrayProperty("targetCurses",
    new QueryActiveEffectsResolver(
        new EffectQuery(
            EffectTagQuery: TagQuery.MakeQueryMatchTag(Tag.RequestTag(tagsManager, "effect.curse"))),
        new AbilityTargetResolver()));
```

Reach for the `EffectQuery` overload when the filter is expressible as a query, and for [`ObjectWhereResolver`](where-resolver.md) with [`ActiveEffectTagQueryResolver`](active-effect-tag-query-resolver.md) when the predicate has to read per-element state the query cannot express.

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
- [ActiveEffectTagQueryResolver](active-effect-tag-query-resolver.md)
- [EffectQueryMatchResolver](effect-query-match-resolver.md)
- [EffectStackDataResolver](effect-stack-data-resolver.md)
