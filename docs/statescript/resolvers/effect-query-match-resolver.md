# EffectQueryMatchResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EffectQueryMatchResolver`
> **Output Type:** `bool`

Matches a full [`EffectQuery`](../../effects/README.md#effectquery) against the effect behind an `ActiveEffectHandle`. Use it when the filter needs more than tags — a specific `EffectData`, the source entity, or a modified attribute. For the common tag-only case, [`ActiveEffectTagQueryResolver`](active-effect-tag-query-resolver.md) is cheaper to configure.

## Constructor

```csharp
new EffectQueryMatchResolver(handleResolver, query)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| handleResolver | `IObjectResolver<ActiveEffectHandle>` | Produces the active effect handle to inspect. |
| query | `EffectQuery` | The query the effect must match. An empty query matches every effect. |

## Behavior

- Resolves the handle and matches the query against its effect.
- Invalid or missing handles resolve to `false`.
- All defined query fields are combined with AND.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<ActiveEffectHandle>("debuff");

graph.VariableDefinitions.DefineProperty("debuffCameFromTheBoss",
    new EffectQueryMatchResolver(
        new ObjectVariableResolver<ActiveEffectHandle>("debuff"),
        new EffectQuery(EffectSource: bossEntity)));
```

## Composition

```csharp
// Strip only the health debuffs applied by whoever cast this ability
graph.VariableDefinitions.DefineObjectArrayProperty("myHealthDebuffs",
    new ObjectWhereResolver<ActiveEffectHandle>(
        new QueryActiveEffectsResolver(effectData: null, new AbilityTargetResolver()),
        new EffectQueryMatchResolver(
            new ElementResolver<ActiveEffectHandle>(),
            new EffectQuery(
                ModifyingAttribute: "CombatAttributeSet.CurrentHealth",
                EffectTagQuery: TagQuery.MakeQueryMatchTag(
                    Tag.RequestTag(tagsManager, "effect.debuff"))))));
```

## See Also

- [Resolvers Overview](README.md)
- [ActiveEffectTagQueryResolver](active-effect-tag-query-resolver.md)
- [QueryActiveEffectsResolver](query-active-effects-resolver.md)
- [WhereResolver](where-resolver.md)
- [EffectQuery](../../effects/README.md#effectquery)
