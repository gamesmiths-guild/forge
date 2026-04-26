# VectorComponentResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.VectorComponentResolver`
> **Output Type:** `float`

Extracts a single component from a vector. Use this resolver when only one axis value is needed from a `Vector2`, `Vector3`, or `Vector4` for scalar math, comparisons, or conversions.

## Constructor

```csharp
new VectorComponentResolver(vector, component)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| vector | `IPropertyResolver` | The resolver for the vector operand. |
| component | `VectorComponent` | The component to extract, such as `X`, `Y`, `Z`, or `W`. |

## Supported Types

| Vector Type | Components |
|-------------|------------|
| `Vector2` | `X`, `Y` |
| `Vector3` | `X`, `Y`, `Z` |
| `Vector4` | `X`, `Y`, `Z`, `W` |

## Behavior

- Resolves the vector operand on each call.
- Returns the selected component as a `float`.
- Validates the component against the vector dimensionality at construction time.
- Supports `Vector2`, `Vector3`, and `Vector4` only.
- Throws `ArgumentException` at construction time for unsupported type/component combinations.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("velocityY",
    new VectorComponentResolver(
        new VariableResolver("velocity", typeof(Vector3)),
        VectorComponent.Y));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("isMovingUp",
    new ComparisonResolver(
        new VectorComponentResolver(
            new VariableResolver("velocity", typeof(Vector3)),
            VectorComponent.Y),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(0.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [LengthResolver](length-resolver.md)
- [NormalizeResolver](normalize-resolver.md)
