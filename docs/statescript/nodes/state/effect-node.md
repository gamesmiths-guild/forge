# EffectNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.EffectNode`
> **Context:** `EffectNodeContext`

Applies one or more effects, stays active while any applied instance remains active, then removes any still-active instances when the node deactivates.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Activates the state node. |
| 1 | Abort | Forcefully deactivates and fires OnAbort. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | OnActivate | Event | Emits when the node activates. |
| 1 | OnDeactivate | Event | Emits when the node deactivates (any reason). |
| 2 | OnAbort | Event | Emits only when aborted via the Abort port. |
| 3 | Subgraph | Subgraph | Remains active while the node is active; sends disable signal on deactivation. |
| 4 | OnEffectEnd | Event | Emits only when all applied effects ended without this node removing them during external deactivation. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Effect | `Effect` or `Effect[]` | The effect instance(s) to apply on activation. |
| 1 | Target | `IForgeEntity` or `IForgeEntity[]` | The entity or entities that receive the effect(s). |
| 2 | Context Data | `EffectApplicationContext` | Optional. Custom context data passed through the effect pipeline. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Active Effect | `ActiveEffectHandle` or `ActiveEffectHandle[]` | Optional. The active effect handle(s) produced on activation. |

## Behavior

1. On activation, the node resolves the effect input and the target input.
2. It applies every resolved effect to every resolved target, forming a full `effect[] x target[]` cross-product.
3. Level and ownership are baked into each resolved `Effect`; configure them on the resolver that produces the effect (typically [EffectFromDataResolver](../../resolvers/effect-from-data-resolver.md)) rather than on the node.
4. If the **Context Data** input is bound, it is resolved once and passed as the `EffectApplicationContext` for every application, so custom calculators and executions can read it through `EffectEvaluatedData.TryGetContextData<TData>`. When unbound, effects are applied without context data. See [EffectContextDataResolver](../../resolvers/effect-context-data-resolver.md).
5. Any non-instant applications that return an `ActiveEffectHandle` are stored in `EffectNodeContext`. If the **Active Effect** output is bound, the produced handle(s) are also written to that variable on activation (scalar variable receives the single handle, or `null` when instant; array variable receives the compact list of produced handles).
6. If no active handles remain after activation (for example, all effects were instant), the node deactivates in the same frame and emits **OnEffectEnd**.
7. While active, the node checks its stored handles and naturally deactivates once all of them became invalid because the effects expired or were removed by something else. This natural shutdown also emits **OnEffectEnd**.
8. On deactivation, the node iterates its stored handles and removes only the ones that are still valid.
9. External deactivation paths such as parent subgraph shutdown or explicit graph stop do **not** emit **OnEffectEnd**, because the node itself is ending the effects in those cases.
10. Instant effects and duration effects that already expired before deactivation are ignored.
11. This is useful for buffs, auras, toggles, and other state-owned effects that should end when the state ends.
12. Removal is best-effort: if an effect already ended naturally, the node leaves it alone.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectArrayProperty(
    "effects",
    new EffectArrayFromDataResolver([effectA, effectB]));
graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

var effectNode = new EffectNode();
effectNode.BindInput(EffectNode.EffectInput, "effects");
effectNode.BindInput(EffectNode.TargetInput, "target");
```

Configure level and ownership on the [EffectFromDataResolver](../../resolvers/effect-from-data-resolver.md)/`EffectArrayFromDataResolver` that produces the effects. When omitted, they default to the current ability level and owner/source, or to level `1` and `null`/`null` ownership without an ability context.

## See Also

- [State Nodes Overview](README.md)
- [ApplyEffectNode](../action/apply-effect-node.md)
- [EffectFromDataResolver](../../resolvers/effect-from-data-resolver.md)
- [EffectVariableResolver](../../resolvers/effect-variable-resolver.md)
- [EffectContextDataResolver](../../resolvers/effect-context-data-resolver.md)
