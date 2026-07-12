# ActiveEffectDataResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ActiveEffectDataResolver`
> **Output Type:** `double` / `int` / `bool` (depends on the selected data type)

Reads a selected runtime value from an `ActiveEffectHandle` produced by a nested resolver (typically an Active Effect variable, or the handle output of an apply/effect node).

## Constructor

```csharp
new ActiveEffectDataResolver(handleResolver, dataType)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| handleResolver | `IObjectResolver<ActiveEffectHandle>` | Produces the active effect handle to inspect. |
| dataType | `ActiveEffectDataType` | Which value to read. |

### `ActiveEffectDataType` values

| Value | Output | Notes |
|-------|--------|-------|
| `RemainingDuration` | `double` | `-1` for infinite effects. |
| `TotalDuration` | `double` | `-1` for infinite effects. |
| `RemainingFraction` | `double` | Remaining / total, clamped to 0-1; `1` for infinite. |
| `StackCount` | `int` | |
| `Level` | `int` | |
| `ExecutionCount` | `int` | |
| `Period` | `double` | `0` for non-periodic effects. |
| `IsInhibited` | `bool` | |
| `IsValid` | `bool` | |

## Behavior

- Resolves the handle; invalid or missing handles resolve to a default value (`0` or `false`).
- Reads the selected value from the handle's public getters.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<ActiveEffectHandle>("buff");

graph.VariableDefinitions.DefineProperty("buffRemaining",
    new ActiveEffectDataResolver(
        new ObjectVariableResolver<ActiveEffectHandle>("buff"),
        ActiveEffectDataType.RemainingDuration));
```

## Composition

```csharp
// Deactivate a channel when the applied buff's remaining time runs out
graph.VariableDefinitions.DefineProperty("buffExpired",
    new ComparisonResolver(
        new ActiveEffectDataResolver(
            new ObjectVariableResolver<ActiveEffectHandle>("buff"),
            ActiveEffectDataType.RemainingDuration),
        ComparisonOperation.LessThanOrEqual,
        new VariantResolver(new Variant128(0.0), typeof(double))));
```

## See Also

- [Resolvers Overview](README.md)
- [ActiveEffectTargetResolver](active-effect-target-resolver.md)
- [QueryActiveEffectsResolver](query-active-effects-resolver.md)
- [ApplyEffectNode](../nodes/action/apply-effect-node.md)
