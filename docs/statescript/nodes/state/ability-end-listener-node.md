# AbilityEndListenerNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.AbilityEndListenerNode`
> **Context:** `AbilityEndListenerNodeContext`

Listens for abilities ending on an entity while active, emitting an event with the ended ability and whether it was canceled.

## Ports

Standard state ports, plus:

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 4 | OnAbilityEnded | Event | Emits each time a (matching) ability ends. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Entity | `IForgeEntity` | Optional. The entity whose abilities are observed. Defaults to the ability context's owner. |
| 1 | Ability Data | `AbilityData` | Optional. When bound, only that granted ability's ends are reported. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Ability | `AbilityHandle` | The ended ability's handle. May already be invalid (`IsValid == false`) when the ability was removed on end; treat it as an identity for comparison/logging rather than for further queries, unless the ability remains granted. |
| 1 | Was Canceled | `bool` | Whether the ability was canceled (vs. ended gracefully). |

## Behavior

1. On activation, subscribes to `EntityAbilities.OnAbilityEnded` on the resolved entity. When an ability-data filter is bound, it is captured as the filter value.
2. On each end, if a filter is bound only ends whose ability shares that `AbilityData` are reported (matched per event by data, independent of the grant's source entity and of whether the ability was granted before or after this node activated). Matching ends write **Ability** and **Was Canceled**, then emit `OnAbilityEnded`.
3. When the grant is removed on end (e.g. `AbilityDeactivationPolicy.RemoveOnEnd`), the removal happens before listeners run, so the emitted **Ability** handle may already be freed. The end is still reported (the filter matches by data, not through the handle), but the handle is primarily useful for identity/logging in that case.
4. Unsubscribes on deactivation.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<AbilityHandle>("endedAbility");
graph.VariableDefinitions.DefineVariable("wasCanceled", false);

var listener = new AbilityEndListenerNode();
listener.BindOutput(AbilityEndListenerNode.AbilityOutput, "endedAbility");
listener.BindOutput(AbilityEndListenerNode.WasCanceledOutput, "wasCanceled");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    listener.InputPorts[StateNode<AbilityEndListenerNodeContext>.InputPort]));
graph.AddConnection(new Connection(
    listener.OutputPorts[AbilityEndListenerNode.OnAbilityEndedPort],
    onAbilityEndedNode.InputPorts[ActionNode.InputPort]));
```

## See Also

- [State Nodes Overview](README.md)
- [Ability Integration](../../ability-integration.md)
- [CancelAbilityNode](../action/cancel-ability-node.md)
