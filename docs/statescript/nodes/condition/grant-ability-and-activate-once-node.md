# GrantAbilityAndActivateOnceNode

> **Type:** Condition Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Condition.GrantAbilityAndActivateOnceNode`

Grants an ability transiently, activates it once, and routes to the **True** port when the activation succeeds. The granted ability is automatically removed when it ends, the one-shot "proc" pattern.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers evaluation. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits when the activation succeeded. |
| 1 | False | Event | Emits when it failed. |

## Constructor

```csharp
new GrantAbilityAndActivateOnceNode(levelOverridePolicy = LevelComparison.None)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| levelOverridePolicy | `LevelComparison` | When the ability is already granted, which level relationships override the existing level. Defaults to `None`. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Ability Data | `AbilityData` | The ability to grant and activate. |
| 1 | Entity | `IForgeEntity` | Optional. The entity to grant on. Defaults to the ability context's owner. |
| 2 | Level | `int` | Optional. The grant level. Defaults to the context level, or `1`. |
| 3 | Target | `IForgeEntity` | Optional. Passed as the activation target. |
| 4 | Activation Data | `AbilityActivator` | Optional. Custom activation data passed to the ability. |

## Behavior

1. Resolves the ability data, entity (default owner), level, and optional target.
2. Calls `EntityAbilities.GrantAbilityAndActivateOnce(...)`, or `EntityAbilities.GrantAbilityAndActivateOnce<TData>(...)` when the **Activation Data** input is bound.
3. Routes to **True** when the activation succeeded (judged by the returned failure flags), otherwise **False**.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<AbilityData>("procAbility", counterAttackData);

var proc = new GrantAbilityAndActivateOnceNode();
proc.BindInput(GrantAbilityAndActivateOnceNode.AbilityDataInput, "procAbility");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    proc.InputPorts[ConditionNode.InputPort]));
graph.AddConnection(new Connection(
    proc.OutputPorts[ConditionNode.TruePort],
    onProcNode.InputPorts[ActionNode.InputPort]));
```

## Passing custom activation data

Bind the **Activation Data** input to an [AbilityActivatorResolver](../../resolvers/ability-activator-resolver.md) to hand the procced ability a strongly-typed value built from the current graph state:

```csharp
graph.VariableDefinitions.DefineObjectProperty("counterData",
    new AbilityActivatorResolver(new CounterAttackDataProvider()));

proc.BindInput(GrantAbilityAndActivateOnceNode.ActivationDataInput, "counterData");
```

Unlike [TryActivateAbilitiesByTagNode](try-activate-abilities-by-tag-node.md), the ability being activated is fixed by the **Ability Data** input, so the provider's data type can be matched to it up front. An ability that does not implement `IAbilityBehavior<TData>` still activates and simply ignores the data.

## See Also

- [Condition Nodes Overview](README.md)
- [GrantAbilityNode](../state/grant-ability-node.md)
- [GrantAbilityPermanentlyNode](../action/grant-ability-permanently-node.md)
- [AbilityActivatorResolver](../../resolvers/ability-activator-resolver.md)
