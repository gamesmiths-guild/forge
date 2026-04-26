# PlaneFromVerticesResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.PlaneFromVerticesResolver`
> **Output Type:** `Plane`

Creates a plane from three vertices using `Plane.CreateFromVertices`.

## Constructor

```csharp
new PlaneFromVerticesResolver(point1, point2, point3)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| point1 | `IPropertyResolver` | The resolver for the first vertex. |
| point2 | `IPropertyResolver` | The resolver for the second vertex. |
| point3 | `IPropertyResolver` | The resolver for the third vertex. |

## Supported Types

| `point1` | `point2` | `point3` | Result Type |
|----------|----------|----------|-------------|
| `Vector3` | `Vector3` | `Vector3` | `Plane` |

## Behavior

- Resolves all three operands through their respective `IPropertyResolver` instances.
- Delegates to `Plane.CreateFromVertices(point1, point2, point3)`.
- Returns the created plane as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("surfacePlane",
    new PlaneFromVerticesResolver(
        new VariableResolver("pointA", typeof(Vector3)),
        new VariableResolver("pointB", typeof(Vector3)),
        new VariableResolver("pointC", typeof(Vector3))));
```

## Composition

```csharp
// Create and normalize a plane from three vertices
graph.VariableDefinitions.DefineProperty("normalizedPlane",
    new NormalizeResolver(
        new PlaneFromVerticesResolver(
            new VariableResolver("pointA", typeof(Vector3)),
            new VariableResolver("pointB", typeof(Vector3)),
            new VariableResolver("pointC", typeof(Vector3)))));
```

## See Also

- [Resolvers Overview](README.md)
- [NormalizeResolver](normalize-resolver.md)
- [DotCoordinateResolver](dotcoordinate-resolver.md)
