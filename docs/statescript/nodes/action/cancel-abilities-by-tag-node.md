# CancelAbilitiesByTagNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.CancelAbilitiesByTagNode`

Cancels every active ability on an entity whose ability tags match any of the given tags.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the cancel. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the cancel. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Tags | `Tag` or `Tag[]` | The tag(s) selecting which abilities to cancel. |
| 1 | Target | `IForgeEntity` | Optional. The entity whose abilities are canceled. Defaults to the ability context's owner. |

## Behavior

1. Resolves the **Target** entity, falling back to the ability context's owner when unbound.
2. Builds a `TagContainer` from the resolved tag(s).
3. Calls `EntityAbilities.CancelAbilitiesWithTag(tags)`, canceling matching active abilities (each raises `OnAbilityEnded` with `WasCanceled == true`).

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable("interruptTag",
    Tag.RequestTag(tagsManager, "ability.channel"));

var cancel = new CancelAbilitiesByTagNode();
cancel.BindInput(CancelAbilitiesByTagNode.TagInput, "interruptTag");
```

## See Also

- [Action Nodes Overview](README.md)
- [CancelAbilityNode](cancel-ability-node.md)
- [TryActivateAbilitiesByTagNode](../condition/try-activate-abilities-by-tag-node.md)
