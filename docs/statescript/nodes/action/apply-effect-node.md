# ApplyEffectNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.ApplyEffectNode`

Applies one or more `Effect` instances to one or more targets, then immediately continues execution.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the action. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after all applications are attempted. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Effect | `Effect` or `Effect[]` | The effect instance(s) to apply. |
| 1 | Target | `IForgeEntity` or `IForgeEntity[]` | The entity or entities that receive the effect(s). |
| 2 | Context Data | `EffectApplicationContext` | Optional. Custom context data passed through the effect pipeline. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Active Effect | `ActiveEffectHandle` or `ActiveEffectHandle[]` | Optional. The active effect handle(s) produced by the application. |

## Behavior

1. The node resolves the **Effect** input as either a single `Effect` or an array of `Effect` instances.
2. It resolves the **Target** input as either a single `IForgeEntity` or an array of entities.
3. Every resolved effect is applied to every resolved target, forming a full `effect[] x target[]` cross-product.
4. Level and ownership are baked into each resolved `Effect`; configure them on the resolver that produces the effect (typically [EffectFromDataResolver](../../resolvers/effect-from-data-resolver.md)) rather than on the node.
5. Instant, duration, and infinite effects are all supported.
6. The node is fire-and-forget, so it does not keep handles for later removal.
7. If the **Context Data** input is bound, it is resolved once and passed as the `EffectApplicationContext` for every application, so custom calculators and executions can read it through `EffectEvaluatedData.TryGetContextData<TData>`. When the input is unbound, effects are applied without context data. See [EffectContextDataResolver](../../resolvers/effect-context-data-resolver.md).
8. If the **Active Effect** output is bound, the produced `ActiveEffectHandle`(s) are written to that variable. The write shape follows the bound variable: a scalar handle variable receives the single handle (or `null` when the effect was instant), while an array handle variable receives the compact list of produced handles (instant applications contribute nothing). Bind it to save handles for later manipulation.
9. The output port emits after all applications are attempted.

Because effects are passed as instances, the same `Effect` can be reused across applications. Store it in a variable with [SetVariableNode](set-variable-node.md) and an [EffectFromDataResolver](../../resolvers/effect-from-data-resolver.md), then re-read it through an [EffectVariableResolver](../../resolvers/effect-variable-resolver.md). Mutating that instance later (for example `Effect.LevelUp()`) updates any non-snapshot active applications on their targets live.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "slowEffect",
    new EffectFromDataResolver(slowEffectData));
graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>(
    "targets",
    targetA,
    targetB);

var applyEffect = new ApplyEffectNode();
applyEffect.BindInput(ApplyEffectNode.EffectInput, "slowEffect");
applyEffect.BindInput(ApplyEffectNode.TargetInput, "targets");
```

To override level or ownership, configure them on the `EffectFromDataResolver`:

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "slowEffect",
    new EffectFromDataResolver(
        slowEffectData,
        new AbilityLevelResolver(),
        new OwnershipResolver(new AbilityOwnerResolver(), new AbilitySourceResolver())));
```

When no level or ownership resolver is supplied, the effect defaults to the current ability level and owner/source, or to level `1` and `null`/`null` ownership without an ability context.

## See Also

- [Action Nodes Overview](README.md)
- [EffectNode](../state/effect-node.md)
- [EffectFromDataResolver](../../resolvers/effect-from-data-resolver.md)
- [EffectVariableResolver](../../resolvers/effect-variable-resolver.md)
- [EffectContextDataResolver](../../resolvers/effect-context-data-resolver.md)
