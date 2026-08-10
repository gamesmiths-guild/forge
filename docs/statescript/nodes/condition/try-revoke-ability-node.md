# TryRevokeAbilityNode

> **Type:** Condition Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Condition.TryRevokeAbilityNode`

Tries to revoke granted abilities through their `AbilityHandle`, routing to **True** when at least one revocation succeeds.

Revoking removes the **grant**. It is not the same as canceling, which only stops the instances an ability is currently running and leaves it granted — for that, use [CancelAbilityNode](../action/cancel-ability-node.md) or [CancelAbilitiesNode](../action/cancel-abilities-node.md).

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the revocation. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits when at least one ability was revoked. |
| 1 | False | Event | Emits when there was nothing to revoke. |

## Constructor

```csharp
new TryRevokeAbilityNode(
    scope = AbilityRevokeScope.PermanentGrants,
    removalPolicy = AbilityDeactivationPolicy.CancelImmediately)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| scope | `AbilityRevokeScope` | Which grant sources to remove. Defaults to `PermanentGrants`. |
| removalPolicy | `AbilityDeactivationPolicy` | How active instances are treated once the last grant source is gone. Defaults to `CancelImmediately`. `Ignore` is not valid. |

### AbilityRevokeScope

| Value | Description |
|-------|-------------|
| `PermanentGrants` | Removes only the permanent grants, leaving grants owned by effects and by graphs in place. The ability goes away only when nothing else is granting it. |
| `AllGrants` | Removes every grant source, so the ability always goes away. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Ability | `AbilityHandle` or `AbilityHandle[]` | The handle(s) of the ability/abilities to revoke. |

## Behavior

1. Resolves the handle input as a single handle or an array of handles. Invalid handles are skipped.
2. For each valid handle, calls `RevokeAbility(handle, removalPolicy)` on the owning entity's ability manager, or `ClearAbility(handle, removalPolicy)` when the scope is `AllGrants`.
3. **Every handle is attempted before the result is reported** — the node does not stop at the first success or failure.
4. Emits **True** when any of them had something to revoke, **False** otherwise.

Handles come from the **Ability** output of a [GrantAbilityPermanentlyNode](../action/grant-ability-permanently-node.md)/[GrantAbilityNode](../state/grant-ability-node.md), or from a [GetAbilityHandleResolver](../../resolvers/get-ability-handle-resolver.md).

### Why the False port matters

**False** means the entity had nothing to revoke — no such grant, or none of the kind this node removes. That is precisely what a respec-and-refund flow needs to know, and **a resolver cannot answer it**: [GetAbilityHandleResolver](../../resolvers/get-ability-handle-resolver.md) tells you the ability is granted, not whether it holds a *permanent* grant as opposed to one an item's effect is providing. Branching on this node's result is the only way to distinguish them.

A graph may revoke the ability driving it. The node tears down its own execution context mid-message and the condition still resolves, exactly like [CancelAbilityNode](../action/cancel-ability-node.md).

> **`AllGrants` is a teardown, not a temporary removal.** An effect that was granting a cleared ability keeps its now-invalid handle and **will not grant the ability back when it ends** — an ability cleared while an item was providing it does not return when the item is unequipped and re-equipped. For a reversible removal, inhibit instead with a [BlockAbilityTagsEffectComponent](../../../effects/components/block-ability-tags-effect-component.md).

Cooldowns are unaffected: they live in effects on the owner and are checked by tag, so revoking and re-granting an ability cannot be used to skip one.

## Usage

```csharp
// Respec: drop the unlock this skill node granted, and refund only if it was really there
graph.VariableDefinitions.DefineObjectProperty("learnedSkill",
    new GetAbilityHandleResolver(fireballData));

var revoke = new TryRevokeAbilityNode();
revoke.BindInput(TryRevokeAbilityNode.AbilityInput, "learnedSkill");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    revoke.InputPorts[ConditionNode.InputPort]));

// True -> refund a skill point; False -> the player never had it
graph.AddConnection(new Connection(
    revoke.OutputPorts[ConditionNode.TruePort],
    refundNode.InputPorts[ActionNode.InputPort]));
```

```csharp
// Let running instances finish rather than cutting them off
var revoke = new TryRevokeAbilityNode(
    AbilityRevokeScope.PermanentGrants,
    AbilityDeactivationPolicy.RemoveOnEnd);
```

## See Also

- [Condition Nodes Overview](README.md)
- [GrantAbilityPermanentlyNode](../action/grant-ability-permanently-node.md)
- [CancelAbilityNode](../action/cancel-ability-node.md)
- [GetAbilityHandleResolver](../../resolvers/get-ability-handle-resolver.md)
- [Abilities](../../../abilities.md)
