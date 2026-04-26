# DotNormalResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.DotNormalResolver`
> **Output Type:** `float`

Computes the dot product of a plane normal and a vector using `Plane.DotNormal`.

## Constructor

```csharp
new DotNormalResolver(plane, normal)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| plane | `IPropertyResolver` | The resolver for the plane operand. |
| normal | `IPropertyResolver` | The resolver for the vector operand. |

## Supported Types

| Plane Type | Normal Type | Result Type |
|------------|-------------|-------------|
| `Plane` | `Vector3` | `float` |

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Delegates to `Plane.DotNormal(plane, normal)`.
- Returns the result as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("normalAlignment",
    new DotNormalResolver(
        new VariableResolver("plane", typeof(Plane)),
        new VariableResolver("direction", typeof(Vector3))));
```

## Composition

```csharp
// Check if a direction points toward the plane normal
graph.VariableDefinitions.DefineProperty("isFacingPlaneNormal",
    new ComparisonResolver(
        new DotNormalResolver(
            new VariableResolver("plane", typeof(Plane)),
            new VariableResolver("direction", typeof(Vector3))),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(0.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [DotCoordinateResolver](dotcoordinate-resolver.md)
- [DotResolver](dot-resolver.md)
