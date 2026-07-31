# ActiveEffectTagQueryResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ActiveEffectTagQueryResolver`
> **Output Type:** `bool`

Evaluates a `TagQuery` against the tags of the effect behind an `ActiveEffectHandle`. This is the predicate that makes [`ObjectWhereResolver`](where-resolver.md) able to filter effect arrays by category, so `QueryActiveEffects → ObjectWhere → RemoveEffect` dispels by kind with no dedicated node.

## Constructors

```csharp
new ActiveEffectTagQueryResolver(handleResolver, query, effectTagSource = EffectTagSource.OwningTags)
new ActiveEffectTagQueryResolver(handleResolver, queryExpression, effectTagSource = EffectTagSource.OwningTags)
new ActiveEffectTagQueryResolver(handleResolver, tag, effectTagSource = EffectTagSource.OwningTags)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| handleResolver | `IObjectResolver<ActiveEffectHandle>` | Produces the active effect handle to inspect. |
| query | `TagQuery` | A prebuilt query to evaluate. |
| queryExpression | `TagQueryExpression` | An expression the resolver builds into a query. |
| tag | `Tag` | Shorthand for the common single-tag match. |
| effectTagSource | `EffectTagSource` | Which set of the effect's tags to evaluate against. |

## EffectTagSource

| Value | Evaluates against |
|-------|-------------------|
| `OwningTags` *(default)* | The effect's own tags **and** the tags it grants to its target. |
| `EffectTags` | `EffectData.EffectTags` only — the effect's identity. |
| `GrantedTags` | The tags granted through `ModifierTagsEffectComponent` only. |

## Behavior

- Resolves the handle and reads the selected tag container from its effect.
- Invalid or missing handles resolve to `false`.
- An effect carrying no container of its own is evaluated as an *empty* one, so negative queries such as `NoTagsMatch` still match it.
- Tag matching is hierarchical: an effect tagged `effect.debuff.poison` matches a query for `effect.debuff`.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<ActiveEffectHandle>("debuff");

graph.VariableDefinitions.DefineProperty("debuffIsCurse",
    new ActiveEffectTagQueryResolver(
        new ObjectVariableResolver<ActiveEffectHandle>("debuff"),
        Tag.RequestTag(tagsManager, "effect.curse"),
        EffectTagSource.EffectTags));
```

## Composition

```csharp
// Dispel every curse on the ability target
graph.VariableDefinitions.DefineObjectArrayProperty("cursesToDispel",
    new ObjectWhereResolver<ActiveEffectHandle>(
        new QueryActiveEffectsResolver(default, new AbilityTargetResolver()),
        new ActiveEffectTagQueryResolver(
            new ElementResolver<ActiveEffectHandle>(),
            Tag.RequestTag(tagsManager, "effect.curse"),
            EffectTagSource.EffectTags)));

var removeNode = new RemoveEffectNode(forceRemoval: true);
removeNode.BindInput(RemoveEffectNode.HandleInput, "cursesToDispel");
```

## See Also

- [Resolvers Overview](README.md)
- [EffectQueryMatchResolver](effect-query-match-resolver.md)
- [QueryActiveEffectsResolver](query-active-effects-resolver.md)
- [WhereResolver](where-resolver.md)
- [TagQueryResolver](tag-query-resolver.md)
- [RemoveEffectNode](../nodes/action/remove-effect-node.md)
