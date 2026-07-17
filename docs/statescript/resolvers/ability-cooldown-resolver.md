# AbilityCooldownResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityCooldownResolver`
> **Output Type:** `float`

Reads a cooldown value from an ability. By default it reads the ability driving the current graph (through the activation context); provide an `IObjectResolver<AbilityHandle>` to inspect a different ability.

## Constructor

```csharp
new AbilityCooldownResolver(
    dataType = AbilityCooldownDataType.RemainingTime,
    cooldownTag = null,
    handleResolver = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| dataType | `AbilityCooldownDataType` | Which cooldown value to read. |
| cooldownTag | `Tag?` | When set, only cooldown effects granting that tag are considered. |
| handleResolver | `IObjectResolver<AbilityHandle>?` | The ability to inspect. Defaults to the graph's ability. |

### `AbilityCooldownDataType` values

- `RemainingTime`: remaining cooldown in seconds (`0` when not on cooldown).
- `TotalTime`: total cooldown duration.
- `RemainingFraction`: remaining / total, clamped to 0-1 (`0` when not on cooldown).

## Behavior

- Reads from `AbilityHandle.GetCooldownData()` / `GetRemainingCooldownTime(tag)`.
- When no cooldown tag is given, uses the cooldown entry with the longest remaining time (falling back to the longest total when off cooldown).
- Missing abilities resolve to `0`.

## Usage

```csharp
// Drive a cooldown UI bar from the current ability's cooldown fraction
graph.VariableDefinitions.DefineProperty("cooldownFraction",
    new AbilityCooldownResolver(AbilityCooldownDataType.RemainingFraction));
```

## Composition

```csharp
// Branch on whether the current ability is off cooldown
graph.VariableDefinitions.DefineProperty("offCooldown",
    new ComparisonResolver(
        new AbilityCooldownResolver(AbilityCooldownDataType.RemainingTime),
        ComparisonOperation.LessThanOrEqual,
        new VariantResolver(new Variant128(0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [AbilityCostResolver](ability-cost-resolver.md)
- [CanActivateAbilityResolver](can-activate-ability-resolver.md)
- [GetAbilityHandleResolver](get-ability-handle-resolver.md)
