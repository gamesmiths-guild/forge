# GrantAbilityPermanentlyNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.GrantAbilityPermanentlyNode`

Permanently grants an ability to an entity.

Permanent grants **cannot be revoked or inhibited**, use them for unlock-style progression. For grants tied to a graph state's lifetime, use the [GrantAbilityNode](../state/grant-ability-node.md) state node instead; for data-driven grants, use effects with a grant-ability component.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the grant. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the grant. |

## Constructor

```csharp
new GrantAbilityPermanentlyNode(levelOverridePolicy = LevelComparison.None)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| levelOverridePolicy | `LevelComparison` | When the ability is already granted, which level relationships override the existing level. Defaults to `None`. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Ability Data | `AbilityData` | The ability to grant. |
| 1 | Target | `IForgeEntity` | Optional. The entity to grant on. Defaults to the ability context's owner. |
| 2 | Level | `int` | Optional. The grant level. Defaults to the ability context's level, or `1`. |
| 3 | Source | `IForgeEntity` | Optional. The granting source entity. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Ability | `AbilityHandle` | Optional. The granted ability handle. |

## Behavior

1. Resolves the ability data, target (default owner), level (default context level), and optional source.
2. Calls `EntityAbilities.GrantAbilityPermanently(...)`.
3. Writes the resulting `AbilityHandle` to the **Ability** output when bound.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<AbilityData>("unlockedAbility", fireballData);

var grant = new GrantAbilityPermanentlyNode();
grant.BindInput(GrantAbilityPermanentlyNode.AbilityDataInput, "unlockedAbility");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    grant.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Action Nodes Overview](README.md)
- [GrantAbilityNode](../state/grant-ability-node.md)
- [GrantAbilityAndActivateOnceNode](../condition/grant-ability-and-activate-once-node.md)
- [GetAbilityHandleResolver](../../resolvers/get-ability-handle-resolver.md)
