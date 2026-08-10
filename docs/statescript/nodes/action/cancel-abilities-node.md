# CancelAbilitiesNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.CancelAbilitiesNode`

Cancels active abilities on an entity, selected by the ability tags they carry. An ability is canceled when it carries any of the **With Tags** and none of the **Without Tags**.

**Cancel stops what is running; it does not ungrant.** The abilities stay granted and can be activated again. To remove a grant itself, use [TryRevokeAbilityNode](../condition/try-revoke-ability-node.md).

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
| 0 | With Tags | `Tag` or `Tag[]` | Optional. Abilities must carry any of these tags to be canceled. Unbound drops this side of the filter. |
| 1 | Without Tags | `Tag` or `Tag[]` | Optional. Abilities carrying any of these tags are spared. Unbound drops this side of the filter. |
| 2 | Target | `IForgeEntity` | Optional. The entity whose abilities are canceled. Defaults to the ability context's owner. |

## Behavior

1. Resolves the **Target** entity, falling back to the ability context's owner when unbound.
2. Builds a `TagContainer` from each bound tag input.
3. If **both** tag inputs are unbound, does nothing and returns.
4. Otherwise calls `EntityAbilities.CancelAbilities(withTags, withoutTags)`, canceling matching active abilities (each raises `OnAbilityEnded` with `WasCanceled == true`).

Binding only **Without Tags** cancels everything *except* the abilities carrying them. Leaving both unbound cancels nothing: wiping every active ability is available through `EntityAbilities.CancelAbilities(null, null)`, but it should be asked for explicitly rather than being what an unconfigured node happens to do.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable("interruptTag",
    Tag.RequestTag(tagsManager, "ability.channel"));
graph.VariableDefinitions.DefineObjectVariable("unstoppableTag",
    Tag.RequestTag(tagsManager, "ability.unstoppable"));

// Interrupt channeled abilities, except the ones flagged unstoppable.
var cancel = new CancelAbilitiesNode();
cancel.BindInput(CancelAbilitiesNode.WithTagsInput, "interruptTag");
cancel.BindInput(CancelAbilitiesNode.WithoutTagsInput, "unstoppableTag");
```

## See Also

- [Action Nodes Overview](README.md)
- [CancelAbilityNode](cancel-ability-node.md)
- [TryActivateAbilitiesByTagNode](../condition/try-activate-abilities-by-tag-node.md)
