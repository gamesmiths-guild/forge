# DotCoordinateResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.DotCoordinateResolver`
> **Output Type:** `float`

Computes the dot product of a plane and a 3D coordinate using `Plane.DotCoordinate`.

## Constructor

```csharp
new DotCoordinateResolver(plane, coordinate)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| plane | `IPropertyResolver` | The resolver for the plane operand. |
| coordinate | `IPropertyResolver` | The resolver for the coordinate operand. |

## Supported Types

| Plane Type | Coordinate Type | Result Type |
|------------|-----------------|-------------|
| `Plane` | `Vector3` | `float` |

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Delegates to `Plane.DotCoordinate(plane, coordinate)`.
- Returns the result as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("signedDistance",
    new DotCoordinateResolver(
        new VariableResolver("plane", typeof(Plane)),
        new VariableResolver("point", typeof(Vector3))));
```

## Composition

```csharp
// Check whether a point is in front of a plane
graph.VariableDefinitions.DefineProperty("isInFront",
    new ComparisonResolver(
        new DotCoordinateResolver(
            new VariableResolver("plane", typeof(Plane)),
            new VariableResolver("point", typeof(Vector3))),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(0.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [DotNormalResolver](dotnormal-resolver.md)
- [PlaneFromVerticesResolver](planefromvertices-resolver.md)
