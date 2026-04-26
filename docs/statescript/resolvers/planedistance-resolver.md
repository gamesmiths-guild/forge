# PlaneDistanceResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.PlaneDistanceResolver`
> **Output Type:** `float`

Extracts the distance component from a `Plane`. Use this resolver when a plane is already available and you need only its `D` term for further math or comparisons.

## Constructor

```csharp
new PlaneDistanceResolver(plane)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| plane | `IPropertyResolver` | The resolver for the plane operand. Must resolve to `Plane`. |

## Behavior

- Resolves the plane operand on each call.
- Returns the plane's `D` component as a `float`.
- Requires the operand type to be `Plane` at construction time.
- Throws `ArgumentException` at construction time when given any other type.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("groundPlaneDistance",
    new PlaneDistanceResolver(
        new VariableResolver("groundPlane", typeof(Plane))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("isPlaneOffsetLarge",
    new ComparisonResolver(
        new PlaneDistanceResolver(
            new PlaneFromNormalResolver(
                new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)),
                new VariableResolver("planeOffset", typeof(float)))),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(1.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [PlaneNormalResolver](planenormal-resolver.md)
- [PlaneFromNormalResolver](planefromnormal-resolver.md)
