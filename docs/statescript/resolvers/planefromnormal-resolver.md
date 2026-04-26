# PlaneFromNormalResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.PlaneFromNormalResolver`
> **Output Type:** `Plane`

Creates a plane from a normal vector and distance. This is useful when plane data needs to be assembled from separate graph values before being passed into plane-aware resolvers.

## Constructor

```csharp
new PlaneFromNormalResolver(normal, distance)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| normal | `IPropertyResolver` | The resolver for the plane normal. Must resolve to `Vector3`. |
| distance | `IPropertyResolver` | The resolver for the plane distance. Must resolve to `float`. |

## Behavior

- Resolves the normal and distance operands on each call.
- Constructs a new `Plane` from the resolved `Vector3` normal and `float` distance.
- Requires `normal` to be `Vector3` and `distance` to be `float` at construction time.
- Throws `ArgumentException` at construction time for unsupported operand types.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("groundPlane",
    new PlaneFromNormalResolver(
        new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)),
        new VariableResolver("groundOffset", typeof(float))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("groundNormal",
    new PlaneNormalResolver(
        new PlaneFromNormalResolver(
            new VariableResolver("surfaceNormal", typeof(Vector3)),
            new VariableResolver("surfaceDistance", typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [PlaneNormalResolver](planenormal-resolver.md)
- [PlaneDistanceResolver](planedistance-resolver.md)
