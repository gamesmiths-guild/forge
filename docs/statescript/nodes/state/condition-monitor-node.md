# ConditionMonitorNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.ConditionMonitorNode`
> **Context:** `ConditionMonitorNodeContext`

Continuously evaluates a boolean condition while active, emitting transition events and routing between a **true** subgraph and a **false** subgraph. This is the general-purpose monitor: tag queries, attribute thresholds, distance checks — anything expressible as a boolean resolver.

## Ports

Standard state ports (Input, Abort, OnActivate, OnDeactivate, OnAbort, Subgraph), plus:

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 4 | OnBecameTrue | Event | Emits when the condition transitions to `true`. |
| 5 | OnBecameFalse | Event | Emits when the condition transitions to `false`. |
| 6 | TrueSubgraph | Subgraph | Active while the condition is `true`. |
| 7 | FalseSubgraph | Subgraph | Active while the condition is `false`. |

## Constructor

```csharp
new ConditionMonitorNode(deactivateWhenTrue = false, initialCheckOnActivate = true)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| deactivateWhenTrue | `bool` | When `true`, the node emits `OnBecameTrue` and deactivates itself the moment the condition becomes true ("wait until" mode). |
| initialCheckOnActivate | `bool` | When `true`, the condition is evaluated immediately on activation instead of waiting for the first update tick. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Condition | `bool` | The monitored condition, evaluated on activation and every update tick. |

## Behavior

1. Evaluates the condition on activation (unless `initialCheckOnActivate` is disabled) and once per update tick.
2. On each transition, emits `OnBecameTrue`/`OnBecameFalse`. The first evaluation after activation counts as a transition into the evaluated value.
3. While the condition holds, the matching subgraph is active; on a flip, the previous subgraph is disabled and the other activated. Both are cleaned up when the node deactivates.
4. With `deactivateWhenTrue`, becoming true emits `OnBecameTrue` and self-deactivates.

## Usage

```csharp
// Wait until the owner's health drops below 25, then continue
graph.VariableDefinitions.DefineProperty("lowHealth",
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Health"),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(25), typeof(int))));

var monitor = new ConditionMonitorNode(deactivateWhenTrue: true);
monitor.BindInput(ConditionMonitorNode.ConditionInput, "lowHealth");
```

## See Also

- [State Nodes Overview](README.md)
- [Subgraphs](../../subgraphs.md)
- [ComparisonResolver](../../resolvers/comparison-resolver.md)
- [TagQueryResolver](../../resolvers/tag-query-resolver.md)
