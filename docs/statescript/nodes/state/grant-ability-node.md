# GrantAbilityNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.GrantAbilityNode`
> **Context:** `GrantAbilityNodeContext`

Grants an ability while active: the ability is granted on activation and the grant is removed on deactivation. Grant lifetime equals node lifetime, the idiomatic Statescript way to grant an ability for the duration of a graph state.

Grants are reference counted per source: if other grant sources (such as effects) also granted the same ability, removing this node's grant only removes its own share.

## Ports

Standard state ports (the grant/revoke happens on activate/deactivate).

## Constructor

```csharp
new GrantAbilityNode(
    removalPolicy = AbilityDeactivationPolicy.CancelImmediately,
    levelOverridePolicy = LevelComparison.None)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| removalPolicy | `AbilityDeactivationPolicy` | How the ability is removed when the node deactivates. |
| levelOverridePolicy | `LevelComparison` | When the ability is already granted, which level relationships override the existing level. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Ability Data | `AbilityData` | The ability to grant. |
| 1 | Entity | `IForgeEntity` | Optional. The entity to grant on. Defaults to the ability context's owner. |
| 2 | Level | `int` | Optional. The grant level. Defaults to the context level, or `1`. |
| 3 | Source | `IForgeEntity` | Optional. The granting source entity; leave unbound for a grant with no source at all (not the owner). |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Ability | `AbilityHandle` | Optional. The granted ability handle. |

## Behavior

1. On activation, grants the ability through the internal grant-source machinery (the same path effects use) and writes the handle to the **Ability** output.
2. On deactivation, removes this node's grant according to `removalPolicy`.

To activate the granted ability, feed the **Ability** output into a [TryActivateAbilityNode](../condition/try-activate-ability-node.md).

## Usage

```csharp
// Grant a temporary ability for as long as this state is active
graph.VariableDefinitions.DefineObjectVariable<AbilityData>("empoweredStrike", empoweredStrikeData);
graph.VariableDefinitions.DefineObjectVariable<AbilityHandle>("grantedStrike");

var grant = new GrantAbilityNode();
grant.BindInput(GrantAbilityNode.AbilityDataInput, "empoweredStrike");
grant.BindOutput(GrantAbilityNode.AbilityOutput, "grantedStrike");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    grant.InputPorts[StateNode<GrantAbilityNodeContext>.InputPort]));
```

## See Also

- [State Nodes Overview](README.md)
- [GrantAbilityPermanentlyNode](../action/grant-ability-permanently-node.md)
- [TryGrantAbilityAndActivateOnceNode](../condition/try-grant-ability-and-activate-once-node.md)
