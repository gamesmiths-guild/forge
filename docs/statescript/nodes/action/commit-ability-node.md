# CommitAbilityNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.CommitAbilityNode`

Commits the cost and/or cooldown of the ability driving the current graph, then continues execution.

Costs and cooldowns are only paid when explicitly committed. Place this node early in an ability graph (typically right after Entry) to pay the ability's cost and start its cooldown, or later if the graph tests conditions before committing.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the commit. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the commit. |

## Constructor

```csharp
new CommitAbilityNode(operation = CommitAbilityOperation.CostAndCooldown)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operation | `CommitAbilityOperation` | What to commit. Defaults to `CostAndCooldown`. |

### `CommitAbilityOperation` values

- `CostAndCooldown`: commits both cost and cooldown (`AbilityHandle.CommitAbility()`).
- `CooldownOnly`: commits only the cooldown (`CommitCooldown()`).
- `CostOnly`: commits only the cost (`CommitCost()`).

## Behavior

1. Reads the `AbilityBehaviorContext` from the graph's activation context.
2. Commits the configured operation on that ability's handle.
3. When the graph runs without an ability context (standalone execution), the node does nothing.

## Usage

```csharp
var commit = new CommitAbilityNode(); // cost + cooldown

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    commit.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Action Nodes Overview](README.md)
- [Ability Integration](../../ability-integration.md)
- [CanActivateAbilityResolver](../../resolvers/can-activate-ability-resolver.md)
- [AbilityCooldownResolver](../../resolvers/ability-cooldown-resolver.md)
