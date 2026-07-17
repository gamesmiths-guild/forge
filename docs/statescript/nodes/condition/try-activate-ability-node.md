# TryActivateAbilityNode

> **Type:** Condition Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Condition.TryActivateAbilityNode`

Tries to activate an ability through its `AbilityHandle`, routing to the **True** port when the activation succeeds.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers evaluation. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits when the ability activated. |
| 1 | False | Event | Emits when it did not. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Ability | `AbilityHandle` | The ability handle to activate. |
| 1 | Target | `IForgeEntity` | Optional. Passed as the activation target. |
| 2 | Magnitude | `double` | Optional. The activation magnitude (defaults to `0`). |

## Behavior

1. Resolves the `AbilityHandle` (typically from a [GetAbilityHandleResolver](../../resolvers/get-ability-handle-resolver.md) or the output of a grant node), the optional target, and the magnitude.
2. Calls `AbilityHandle.Activate(out _, target, magnitude)`.
3. Routes to **True** when it activated, otherwise **False**.

## Usage

```csharp
// Look up another granted ability, then try to activate it
graph.VariableDefinitions.DefineObjectProperty("dashAbility",
    new GetAbilityHandleResolver(dashAbilityData));

var tryActivate = new TryActivateAbilityNode();
tryActivate.BindInput(TryActivateAbilityNode.AbilityInput, "dashAbility");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    tryActivate.InputPorts[ConditionNode.InputPort]));
graph.AddConnection(new Connection(
    tryActivate.OutputPorts[ConditionNode.TruePort],
    onDashNode.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Condition Nodes Overview](README.md)
- [TryActivateAbilitiesByTagNode](try-activate-abilities-by-tag-node.md)
- [GetAbilityHandleResolver](../../resolvers/get-ability-handle-resolver.md)
