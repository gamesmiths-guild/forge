# AbilityStateResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityStateResolver`
> **Output Type:** `bool`

Reads a state flag from an ability. By default it reads the ability driving the current graph; provide an `IObjectResolver<AbilityHandle>` to inspect a different ability.

## Constructor

```csharp
new AbilityStateResolver(stateType, handleResolver = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| stateType | `AbilityStateType` | Which state flag to read. |
| handleResolver | `IObjectResolver<AbilityHandle>?` | The ability to inspect. Defaults to the graph's ability. |

### `AbilityStateType` values

- `IsActive`: whether the ability is currently active.
- `IsInhibited`: whether the ability is currently inhibited.
- `IsValid`: whether the handle points to a valid granted ability.

## Behavior

- Reads the corresponding flag on the resolved handle. Missing abilities resolve to `false`.

## Usage

```csharp
// Is the current ability still active?
graph.VariableDefinitions.DefineProperty("isActive",
    new AbilityStateResolver(AbilityStateType.IsActive));
```

## Composition

```csharp
// Only continue while a companion ability is inhibited
graph.VariableDefinitions.DefineProperty("companionSuppressed",
    new AbilityStateResolver(
        AbilityStateType.IsInhibited,
        new GetAbilityHandleResolver(companionAbilityData)));
```

## See Also

- [Resolvers Overview](README.md)
- [GetAbilityHandleResolver](get-ability-handle-resolver.md)
- [AbilityCooldownResolver](ability-cooldown-resolver.md)
