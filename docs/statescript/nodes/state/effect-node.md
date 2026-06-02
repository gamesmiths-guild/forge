# EffectNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.EffectNode`
> **Context:** `EffectNodeContext`

Applies one or more effects, stays active while any applied instance remains active, then removes any still-active
instances when the node deactivates.

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
| 0 | Effect | `EffectData` or `EffectData[]` | The effect configuration(s) to apply on activation. |
| 1 | Target | `IForgeEntity` or `IForgeEntity[]` | The entity or entities that receive the effect(s). |
| 2 | Level | `int` | Optional effect level override. Defaults to the current ability level, or `1` without ability context. |
| 3 | Ownership | `EffectOwnership` | Optional ownership override. Defaults to the current ability owner/source, or `null`/`null` without ability context. |

## Behavior

1. On activation, the node resolves the effect input and the target input.
2. It applies every resolved effect to every resolved target, forming a full `effect[] x target[]` cross-product.
3. It resolves the optional **Level** and **Ownership** inputs when bound; otherwise it uses the current ability level
   and owner/source defaults.
4. Any non-instant applications that return an `ActiveEffectHandle` are stored in `EffectNodeContext`.
5. If no active handles remain after activation (for example, all effects were instant), the node deactivates in the
   same frame and emits **OnEffectEnd**.
6. While active, the node checks its stored handles and naturally deactivates once all of them became invalid because
   the effects expired or were removed by something else. This natural shutdown also emits **OnEffectEnd**.
7. On deactivation, the node iterates its stored handles and removes only the ones that are still valid.
8. External deactivation paths such as parent subgraph shutdown or explicit graph stop do **not** emit
   **OnEffectEnd**, because the node itself is ending the effects in those cases.
9. Instant effects and duration effects that already expired before deactivation are ignored.
10. This is useful for buffs, auras, toggles, and other state-owned effects that should end when the state ends.
11. Removal is best-effort: if an effect already ended naturally, the node leaves it alone.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectArrayProperty(
    "effects",
    new EffectDataArrayResolver(effectA, effectB));
graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

var effectNode = new EffectNode();
effectNode.BindInput(EffectNode.EffectInput, "effects");
effectNode.BindInput(EffectNode.TargetInput, "target");
effectNode.BindInput(EffectNode.LevelInput, "level");
effectNode.BindInput(EffectNode.OwnershipInput, "ownership");
```

Use `AbilityLevelResolver`, `AbilityOwnershipResolver`, or `OwnershipResolver` when you want the application context to
be explicit and reusable across graphs.

## See Also

- [State Nodes Overview](README.md)
- [ApplyEffectNode](../action/apply-effect-node.md)
