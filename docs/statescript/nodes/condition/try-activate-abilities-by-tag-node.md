# TryActivateAbilitiesByTagNode

> **Type:** Condition Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Condition.TryActivateAbilitiesByTagNode`

Tries to activate every granted ability on an entity whose ability tags match any of the given tags, routing to the **True** port when at least one activation succeeds.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers evaluation. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits when at least one ability activated. |
| 1 | False | Event | Emits when none activated. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Tags | `Tag` or `Tag[]` | The tag(s) selecting which abilities to activate. |
| 1 | Entity | `IForgeEntity` | Optional. The entity whose abilities are activated. Defaults to the ability context's owner. |
| 2 | Target | `IForgeEntity` | Optional. Passed as the activation target. |

## Behavior

1. Resolves the entity (default owner), builds a `TagContainer` from the tag(s), and resolves the optional target.
2. Calls `EntityAbilities.TryActivateAbilitiesByTag(tags, target, out _)`.
3. Routes to **True** when any ability activated, otherwise **False**.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable("comboTag",
    Tag.RequestTag(tagsManager, "ability.combo"));

var tryActivate = new TryActivateAbilitiesByTagNode();
tryActivate.BindInput(TryActivateAbilitiesByTagNode.TagInput, "comboTag");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    tryActivate.InputPorts[ConditionNode.InputPort]));

// True branch runs only if at least one ability activated
graph.AddConnection(new Connection(
    tryActivate.OutputPorts[ConditionNode.TruePort],
    onComboNode.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Condition Nodes Overview](README.md)
- [TryActivateAbilityNode](try-activate-ability-node.md)
- [CancelAbilitiesByTagNode](../action/cancel-abilities-by-tag-node.md)
