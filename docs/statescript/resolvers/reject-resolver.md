# RejectResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RejectResolver`
> **Output Type:** `Vector2`, `Vector3`, or `Vector4`

Rejects one vector from another by removing the projected component. The result is the portion of the input vector that is perpendicular to the reference vector.

## Constructor

```csharp
new RejectResolver(value, onto)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| value | `IPropertyResolver` | The resolver for the source vector. |
| onto | `IPropertyResolver` | The resolver for the vector to reject from. |

## Supported Types

| Value Type | Onto Type | Result Type |
|------------|-----------|-------------|
| `Vector2` | `Vector2` | `Vector2` |
| `Vector3` | `Vector3` | `Vector3` |
| `Vector4` | `Vector4` | `Vector4` |

## Behavior

- Resolves both vector operands on each call.
- Requires both operands to have matching vector types.
- Computes the vector rejection by subtracting the projection onto the reference vector from the source vector.
- Supports `Vector2`, `Vector3`, and `Vector4` operands only.
- Throws `ArgumentException` at construction time for mismatched or unsupported types.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("surfaceSlideVelocity",
    new RejectResolver(
        new VariableResolver("velocity", typeof(Vector3)),
        new VariableResolver("surfaceNormal", typeof(Vector3))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("planarTargetDirection",
    new NormalizeResolver(
        new RejectResolver(
            new VariableResolver("targetDirection", typeof(Vector3)),
            new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)))));
```

## See Also

- [Resolvers Overview](README.md)
- [ProjectResolver](project-resolver.md)
- [NormalizeResolver](normalize-resolver.md)
