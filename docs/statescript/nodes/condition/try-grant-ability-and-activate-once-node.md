# TryGrantAbilityAndActivateOnceNode

> **Type:** Condition Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Condition.TryGrantAbilityAndActivateOnceNode`

Grants an ability transiently, tries to activate it once, and routes to the **True** port when the activation succeeds. The granted ability is automatically removed when it ends, the one-shot "proc" pattern.

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
new TryGrantAbilityAndActivateOnceNode(levelOverridePolicy = LevelComparison.None)
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
| 3 | Target | `IForgeEntity` | Optional. Passed as the activation target; leave unbound to activate with no target. |
| 4 | Activation Data | `AbilityActivator` | Optional. Custom activation data passed to the ability; leave unbound to activate without custom data. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Ability | `AbilityHandle` | Optional. The procced ability's handle while it is still running, otherwise `null`. |

## Behavior

1. Resolves the ability data, entity (default owner), level, and optional target.
2. Calls `EntityAbilities.TryGrantAbilityAndActivateOnce(...)`, or `EntityAbilities.TryGrantAbilityAndActivateOnce<TData>(...)` when the **Activation Data** input is bound.
3. Writes the granted `AbilityHandle` to the **Ability** output when bound, on both outcomes — so a failed proc clears a handle left by an earlier one.
4. Routes to **True** when the call reports the ability activated, otherwise **False**. An ability that activates and ends immediately still routes to **True**.

The **Ability** output is not a success signal. A one-shot proc that finishes as it activates takes its transient grant with it, leaving the output `null` on the **True** branch. Bind it when the graph needs to reach a proc that keeps running — to cancel it later, or to read its cooldown — and branch on True/False for everything else.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<AbilityData>("procAbility", counterAttackData);
graph.VariableDefinitions.DefineObjectVariable<AbilityHandle>("procHandle");

var proc = new TryGrantAbilityAndActivateOnceNode();
proc.BindInput(TryGrantAbilityAndActivateOnceNode.AbilityDataInput, "procAbility");
proc.BindOutput(TryGrantAbilityAndActivateOnceNode.AbilityOutput, "procHandle");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    proc.InputPorts[ConditionNode.InputPort]));
graph.AddConnection(new Connection(
    proc.OutputPorts[ConditionNode.TruePort],
    onProcNode.InputPorts[ActionNode.InputPort]));
```

`procHandle` now holds the procced ability for as long as it runs, so later nodes and the handle-taking resolvers ([AbilityCooldownResolver](../../resolvers/ability-cooldown-resolver.md), [AbilityCostResolver](../../resolvers/ability-cost-resolver.md), [AbilityStateResolver](../../resolvers/ability-state-resolver.md)) can act on that specific proc rather than on the ability driving the graph.

## Passing custom activation data

Bind the **Activation Data** input to an [AbilityActivatorResolver](../../resolvers/ability-activator-resolver.md) to hand the procced ability a strongly-typed value built from the current graph state:

```csharp
graph.VariableDefinitions.DefineObjectProperty("counterData",
    new AbilityActivatorResolver(new CounterAttackDataProvider()));

proc.BindInput(TryGrantAbilityAndActivateOnceNode.ActivationDataInput, "counterData");
```

Unlike [TryActivateAbilitiesByTagNode](try-activate-abilities-by-tag-node.md), the ability being activated is fixed by the **Ability Data** input, so the provider's data type can be matched to it up front. An ability that does not implement `IAbilityBehavior<TData>` still activates and simply ignores the data.

## See Also

- [Condition Nodes Overview](README.md)
- [GrantAbilityNode](../state/grant-ability-node.md)
- [GrantAbilityPermanentlyNode](../action/grant-ability-permanently-node.md)
- [AbilityActivatorResolver](../../resolvers/ability-activator-resolver.md)
