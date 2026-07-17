# RandomBranchNode

> **Type:** Condition Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Condition.RandomBranchNode`

Routes to the **True** port with a resolved probability — ergonomic sugar for random branching.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers evaluation. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits with the configured probability. |
| 1 | False | Event | Emits otherwise. |

## Constructor

```csharp
new RandomBranchNode(randomProvider = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| randomProvider | `IRandom?` | The random provider used to roll the branch. When `null`, a non-deterministic `SystemRandom` is used; inject a seeded `IRandom` for deterministic behavior. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Chance | `double` | The probability of routing to **True** (0 to 1). Unresolvable chances route to **False**. |

## Behavior

1. Resolves the **Chance** value.
2. Routes to **True** when `randomProvider.NextDouble() < chance`, otherwise **False**.

## Usage

```csharp
graph.VariableDefinitions.DefineVariable("critChance", 0.25);

var branch = new RandomBranchNode(randomProvider);
branch.BindInput(RandomBranchNode.ChanceInput, "critChance");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    branch.InputPorts[ConditionNode.InputPort]));
graph.AddConnection(new Connection(
    branch.OutputPorts[ConditionNode.TruePort],
    critNode.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Condition Nodes Overview](README.md)
- [ComparisonResolver](../../resolvers/comparison-resolver.md)
- [RandomResolver](../../resolvers/random-resolver.md)
