# CancelAbilityNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.CancelAbilityNode`

Cancels the ability driving the current graph.

Unlike reaching an [Exit node](../../README.md), which ends the ability instance gracefully, canceling marks the ability as canceled (`AbilityEndedData.WasCanceled == true`) and stops the whole graph immediately.

**Cancel stops what is running; it does not ungrant.** The ability stays granted and can be activated again. To remove the grant itself, use [TryRevokeAbilityNode](../condition/try-revoke-ability-node.md).

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the cancel. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the cancel is requested (the graph stops immediately afterward). |

## Behavior

1. Reads the `AbilityBehaviorContext` from the graph's activation context.
2. Calls `AbilityHandle.Cancel()`, which cancels the active instance and raises `OnAbilityEnded` with `WasCanceled == true`.
3. When the graph runs without an ability context (standalone execution), the node does nothing.

## Usage

```csharp
// Cancel the ability when a monitored condition becomes true
var monitor = new ConditionMonitorNode(deactivateWhenTrue: true);
var cancel = new CancelAbilityNode();

graph.AddConnection(new Connection(
    monitor.OutputPorts[ConditionMonitorNode.OnBecameTruePort],
    cancel.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Action Nodes Overview](README.md)
- [CancelAbilitiesNode](cancel-abilities-node.md)
- [Ability Integration](../../ability-integration.md)
