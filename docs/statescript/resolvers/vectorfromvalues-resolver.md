# VectorFromValuesResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.VectorFromValuesResolver`
> **Output Type:** `Vector2`, `Vector3`, or `Vector4`

Creates a vector from component resolver values. Use this when each axis value is computed independently and you want to assemble the final `Vector2`, `Vector3`, or `Vector4` inside a Statescript property graph.

## Constructor

```csharp
new VectorFromComponentsValue(x, y)
new VectorFromComponentsValue(x, y, z)
new VectorFromComponentsValue(x, y, z, w)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| x | `IPropertyResolver` | The resolver for the X component. Must resolve to `float`. |
| y | `IPropertyResolver` | The resolver for the Y component. Must resolve to `float`. |
| z | `IPropertyResolver` | The resolver for the Z component. Must resolve to `float`. Required for `Vector3` and `Vector4`. |
| w | `IPropertyResolver` | The resolver for the W component. Must resolve to `float`. Required for `Vector4`. |

## Behavior

- Resolves each component through its `IPropertyResolver` instance.
- Creates a `Vector2`, `Vector3`, or `Vector4` depending on the constructor overload used.
- Requires every component resolver to produce `float`.
- Validates component types at construction time (fail-fast), not at runtime.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("velocity",
    new VectorFromComponentsValue(
        new VariableResolver("velocityX", typeof(float)),
        new VariableResolver("velocityY", typeof(float)),
        new VariantResolver(new Variant128(0.0f), typeof(float))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("offset",
    new VectorFromComponentsValue(
        new MultiplyResolver(
            new VariableResolver("speed", typeof(float)),
            new VariableResolver("directionX", typeof(float))),
        new MultiplyResolver(
            new VariableResolver("speed", typeof(float)),
            new VariableResolver("directionY", typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [VectorComponentResolver](vectorcomponent-resolver.md)
- [PlaneFromNormalResolver](planefromnormal-resolver.md)
