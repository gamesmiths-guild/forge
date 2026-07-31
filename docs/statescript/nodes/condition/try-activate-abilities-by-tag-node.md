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
| 2 | Target | `IForgeEntity` | Optional. Passed as the activation target; leave unbound to activate with no target. |
| 3 | Activation Data | `AbilityActivator` | Optional. Custom activation data passed to the abilities; leave unbound to activate without custom data. |

## Behavior

1. Resolves the entity (default owner), builds a `TagContainer` from the tag(s), and resolves the optional target.
2. Calls `EntityAbilities.TryActivateAbilitiesByTag(tags, target, out _)`, or `EntityAbilities.TryActivateAbilitiesByTag<TData>(tags, target, data, out _)` when the **Activation Data** input is bound.
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

## Passing custom activation data

Bind the **Activation Data** input to an [AbilityActivatorResolver](../../resolvers/ability-activator-resolver.md) to hand the activated abilities a strongly-typed value built from the current graph state.

A single tag usually selects **several** abilities, and they need not share an activation-data type. That is not a problem: only abilities whose behavior implements `IAbilityBehavior<TData>` for the provider's type receive the data. Every other matching ability still activates, starting through the untyped path and ignoring it. Nothing is skipped and nothing throws — so a broad tag can safely carry data meant for a subset of the abilities it triggers.

If you need each ability to receive a *different* payload, activate them individually with [TryActivateAbilityNode](try-activate-ability-node.md) instead.

## See Also

- [Condition Nodes Overview](README.md)
- [TryActivateAbilityNode](try-activate-ability-node.md)
- [CancelAbilitiesNode](../action/cancel-abilities-node.md)
- [AbilityActivatorResolver](../../resolvers/ability-activator-resolver.md)
