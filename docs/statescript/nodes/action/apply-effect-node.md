# ApplyEffectNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.ApplyEffectNode`

Applies one or more `EffectData` values to one or more targets, then immediately continues execution.

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
| 0 | Effect | `EffectData` or `EffectData[]` | The effect configuration(s) to apply. |
| 1 | Target | `IForgeEntity` or `IForgeEntity[]` | The entity or entities that receive the effect(s). |
| 2 | Level | `int` | Optional effect level override. Defaults to the current ability level, or `1` without ability context. |
| 3 | Ownership | `EffectOwnership` | Optional ownership override. Defaults to the current ability owner/source, or `null`/`null` without ability context. |

## Behavior

1. The node resolves the **Effect** input as either a single `EffectData` or an array of `EffectData`.
2. It resolves the **Target** input as either a single `IForgeEntity` or an array of entities.
3. Every resolved effect is instantiated and applied to every resolved target, forming a full `effect[] x target[]`
   cross-product.
4. It resolves the optional **Level** input when bound; otherwise it uses the current ability level or `1`.
5. It resolves the optional **Ownership** input when bound; otherwise it uses the current ability owner/source or
   `null`/`null`.
6. Instant, duration, and infinite effects are all supported.
7. The node is fire-and-forget, so it does not keep handles for later removal.
8. The output port emits after all applications are attempted.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "slowEffect",
    new EffectDataResolver(effectData));
graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>(
    "targets",
    targetA,
    targetB);

var applyEffect = new ApplyEffectNode();
applyEffect.BindInput(ApplyEffectNode.EffectInput, "slowEffect");
applyEffect.BindInput(ApplyEffectNode.TargetInput, "targets");
applyEffect.BindInput(ApplyEffectNode.LevelInput, "level");
applyEffect.BindInput(ApplyEffectNode.OwnershipInput, "ownership");
```

Use `AbilityLevelResolver`, `AbilityOwnershipResolver`, or `OwnershipResolver` when you want the application context to
be explicit and reusable across graphs.

## See Also

- [Action Nodes Overview](README.md)
- [EffectNode](../state/effect-node.md)
