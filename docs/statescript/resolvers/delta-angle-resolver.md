# DeltaAngleResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.DeltaAngleResolver`
> **Output Type:** `float`

Computes the shortest signed angle difference between two angles in radians, producing a `float` in `(-π, π]`.

## Constructor

```csharp
new DeltaAngleResolver(current, target)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| current | `IPropertyResolver` | The current angle in radians. |
| target | `IPropertyResolver` | The target angle in radians. |

## Behavior

- Numeric operands are read as `float`. Returns the wrapped, shortest signed difference from `current` to `target`.

## Usage

```csharp
// Shortest turn from the current facing to the desired facing
graph.VariableDefinitions.DefineProperty("turnDelta",
    new DeltaAngleResolver(
        new VariableResolver("currentYaw"),
        new VariableResolver("desiredYaw")));
```

## Composition

```csharp
// Only fire when roughly aligned with the target (|delta| < 0.1 rad)
graph.VariableDefinitions.DefineProperty("aligned",
    new ComparisonResolver(
        new AbsResolver(
            new DeltaAngleResolver(
                new VariableResolver("currentYaw"),
                new VariableResolver("desiredYaw"))),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(0.1f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [WrapResolver](wrap-resolver.md)
- [RotateTowardsResolver](rotatetowards-resolver.md)
