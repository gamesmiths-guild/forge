# TryCommitAbilityNode

> **Type:** Condition Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Condition.TryCommitAbilityNode`

Tries to commit the cost and/or cooldown of the ability driving the current graph, routing to the **True** port when the commit succeeds.

Costs and cooldowns are only paid when explicitly committed. Place this node early in an ability graph (typically right after Entry) to pay the ability's cost and start its cooldown, or later if the graph tests conditions before committing.

The commit is re-checked when the node runs, which is why this is a condition node: a graph can reach it long after the ability activated, by which point the cooldown may have started or the resources may have been spent elsewhere.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the commit. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits when the commit succeeded. |
| 1 | False | Event | Emits when it did not, having paid nothing. |

## Constructor

```csharp
new TryCommitAbilityNode(operation = CommitAbilityOperation.CostAndCooldown)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operation | `CommitAbilityOperation` | What to commit. Defaults to `CostAndCooldown`. |

### `CommitAbilityOperation` values

- `CostAndCooldown`: commits both cost and cooldown (`AbilityHandle.TryCommitAbility()`).
- `CooldownOnly`: commits only the cooldown (`TryCommitCooldown()`).
- `CostOnly`: commits only the cost (`TryCommitCost()`).

## Behavior

1. Reads the `AbilityBehaviorContext` from the graph's activation context.
2. Tries the configured operation on that ability's handle.
3. Routes to **True** when it committed, otherwise **False**.
4. When the graph runs without an ability context (standalone execution), there is nothing to commit and the node routes to **False**.

`CostAndCooldown` is all-or-nothing: if either half cannot be committed, neither is applied and the node routes to **False**.

## Usage

```csharp
var commit = new TryCommitAbilityNode(); // cost + cooldown
var cancel = new CancelAbilityNode();

graph.AddNode(commit);
graph.AddNode(cancel);

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    commit.InputPorts[ConditionNode.InputPort]));

// Paid: run the ability
graph.AddConnection(new Connection(
    commit.OutputPorts[ConditionNode.TruePort],
    castNode.InputPorts[ActionNode.InputPort]));

// Could not pay: drop the ability instead of casting for free
graph.AddConnection(new Connection(
    commit.OutputPorts[ConditionNode.FalsePort],
    cancel.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Condition Nodes Overview](README.md)
- [CancelAbilityNode](../action/cancel-ability-node.md)
- [Ability Integration](../../ability-integration.md)
- [CanActivateAbilityResolver](../../resolvers/can-activate-ability-resolver.md)
- [AbilityCooldownResolver](../../resolvers/ability-cooldown-resolver.md)
