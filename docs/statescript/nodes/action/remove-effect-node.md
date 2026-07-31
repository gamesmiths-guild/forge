# RemoveEffectNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.RemoveEffectNode`

Removes one or more active effects through their `ActiveEffectHandle`.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the removal. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the removal. |

## Constructor

```csharp
new RemoveEffectNode(forceRemoval = false)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| forceRemoval | `bool` | Whether to force removal of the entire active effect regardless of its stacking expiration policy. Defaults to `false`. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Active Effect | `ActiveEffectHandle` or `ActiveEffectHandle[]` | The handle(s) of the effect(s) to remove. |

## Behavior

1. Resolves the handle input as a single handle or an array of handles. Invalid handles are skipped silently.
2. For each valid handle, calls `RemoveEffect(handle, forceRemoval)` on the effect's target manager.
3. For stackable effects with `RemoveSingleStackAndRefreshDuration`, a non-forced removal removes a single stack; a forced removal removes the entire active effect.

Handles come from the **Active Effect** output of an [ApplyEffectNode](apply-effect-node.md)/[EffectNode](../state/effect-node.md), or from a [QueryActiveEffectsResolver](../../resolvers/query-active-effects-resolver.md) for a dispel pattern.

## Usage

```csharp
// Dispel: query every application of one effect on the target, then remove them
graph.VariableDefinitions.DefineObjectArrayProperty("debuffs",
    new QueryActiveEffectsResolver(
        new EffectQuery(EffectDefinition: debuffData),
        new AbilityTargetResolver()));

var remove = new RemoveEffectNode(forceRemoval: true);
remove.BindInput(RemoveEffectNode.HandleInput, "debuffs");
```

```csharp
// Dispel by category: every curse, whatever effect it happens to be
graph.VariableDefinitions.DefineObjectArrayProperty("curses",
    new QueryActiveEffectsResolver(
        new EffectQuery(
            EffectTagQuery: TagQuery.MakeQueryMatchTag(Tag.RequestTag(tagsManager, "effect.curse"))),
        new AbilityTargetResolver()));

var dispel = new RemoveEffectNode(forceRemoval: true);
dispel.BindInput(RemoveEffectNode.HandleInput, "curses");
```

For predicates an `EffectQuery` cannot express, filter the unfiltered query through an [ObjectWhereResolver](../../resolvers/where-resolver.md) with an [ActiveEffectTagQueryResolver](../../resolvers/active-effect-tag-query-resolver.md) instead.

## See Also

- [Action Nodes Overview](README.md)
- [QueryActiveEffectsResolver](../../resolvers/query-active-effects-resolver.md)
- [ActiveEffectTagQueryResolver](../../resolvers/active-effect-tag-query-resolver.md)
- [SetEffectInhibitionNode](set-effect-inhibition-node.md)
