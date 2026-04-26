# PlaneNormalResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.PlaneNormalResolver`
> **Output Type:** `Vector3`

Extracts the normal component from a `Plane`. Use this resolver when only the orientation of a plane matters and you need to feed that normal into other vector or plane calculations.

## Constructor

```csharp
new PlaneNormalResolver(plane)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| plane | `IPropertyResolver` | The resolver for the plane operand. Must resolve to `Plane`. |

## Behavior

- Resolves the plane operand on each call.
- Returns the plane's `Normal` component as a `Vector3`.
- Requires the operand type to be `Plane` at construction time.
- Throws `ArgumentException` at construction time for unsupported operand types.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("surfaceNormal",
    new PlaneNormalResolver(
        new VariableResolver("surfacePlane", typeof(Plane))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("upAlignment",
    new DotResolver(
        new PlaneNormalResolver(
            new VariableResolver("surfacePlane", typeof(Plane))),
        new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3))));
```

## See Also

- [Resolvers Overview](README.md)
- [PlaneDistanceResolver](planedistance-resolver.md)
- [PlaneFromNormalResolver](planefromnormal-resolver.md)
