# GetAbilityHandleResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.GetAbilityHandleResolver`
> **Output Type:** `AbilityHandle`

Looks up the `AbilityHandle` of a granted ability on a resolved entity, identified by its `AbilityData`. This is the entry point for cross-ability queries like "the cooldown of my *other* ability", feed the result into the ability data resolvers or activation nodes.

## Constructor

```csharp
new GetAbilityHandleResolver(abilityData, entityResolver = null, sourceResolver = null, exactSourceMatch = false)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| abilityData | `AbilityData` | The ability data identifying the granted ability. |
| entityResolver | `IEntityResolver` | Selects which entity to inspect. Defaults to `AbilityOwnerResolver`. |
| sourceResolver | `IEntityResolver?` | Optional granting source used to filter the lookup. When omitted, the lookup matches any granting source. |
| exactSourceMatch | `bool` | When `true`, only the instance whose granting source is exactly the resolved source matches — including `null` for abilities granted without a source. |

## Behavior

- Resolves the entity; returns `null` when it is not available or the ability is not granted.
- Calls `EntityAbilities.TryGetAbility(abilityData, out handle, source, exactSourceMatch)`.
- Without a source resolver the lookup matches the ability regardless of its granting source; with one, only the instance granted by that source matches. If the same ability data was granted by multiple sources, the sourceless lookup returns an unspecified instance.
- With `exactSourceMatch: true`, the resolved source (or `null` when there is no source resolver) must match the instance's granting source exactly, so the instance granted without a source can be targeted specifically.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectProperty("dashAbility",
    new GetAbilityHandleResolver(dashAbilityData));

// Read the other ability's remaining cooldown
graph.VariableDefinitions.DefineProperty("dashCooldown",
    new AbilityCooldownResolver(
        AbilityCooldownDataType.RemainingTime,
        handleResolver: new GetAbilityHandleResolver(dashAbilityData)));
```

## Composition

```csharp
// Feed the looked-up handle into an activation node
var activate = new TryActivateAbilityNode();
graph.VariableDefinitions.DefineObjectProperty("dashAbility",
    new GetAbilityHandleResolver(dashAbilityData));
activate.BindInput(TryActivateAbilityNode.AbilityInput, "dashAbility");
```

## See Also

- [Resolvers Overview](README.md)
- [AbilityCooldownResolver](ability-cooldown-resolver.md)
- [TryActivateAbilityNode](../nodes/condition/try-activate-ability-node.md)
