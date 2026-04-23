# ScaleResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ScaleResolver`
> **Output Type:** `Vector2`, `Vector3`, or `Vector4`

Resolves the scalar multiplication (scaling) of a vector by a `float` value. Returns a vector of the same type as the vector operand. Supports `Vector2`, `Vector3`, and `Vector4`. The scalar operand must be `float`. This is distinct from [MultiplyResolver](multiply-resolver.md), which performs component-wise multiplication between two matching vectors.

## Constructor

```csharp
new ScaleResolver(vector, scalar)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| vector | `IPropertyResolver` | The resolver for the vector operand. |
| scalar | `IPropertyResolver` | The resolver for the scalar (`float`) operand. |

## Supported Types

| Vector Type | Scalar Type | Result Type |
|-------------|-------------|-------------|
| `Vector2` | `float` | `Vector2` |
| `Vector3` | `float` | `Vector3` |
| `Vector4` | `float` | `Vector4` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- Non-vector type for the vector parameter.
- Non-`float` type for the scalar parameter.
- `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Multiplies the vector by the scalar value using the `*` operator.
- A scalar of `0` produces a zero vector. A scalar of `1` returns the original vector. Negative scalars reverse the direction.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Scale a direction vector by a speed value
graph.VariableDefinitions.DefineProperty("velocity",
    new ScaleResolver(
        new VariableResolver("direction", typeof(Vector3)),
        new VariableResolver("speed", typeof(float))));
```

## Composition

```csharp
// Apply a normalized direction scaled by a clamped speed
graph.VariableDefinitions.DefineProperty("clampedVelocity",
    new ScaleResolver(
        new NormalizeResolver(
            new VariableResolver("direction", typeof(Vector3))),
        new ClampResolver(
            new VariableResolver("speed", typeof(float)),
            new VariantResolver(new Variant128(0.0f), typeof(float)),
            new VariantResolver(new Variant128(20.0f), typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [MultiplyResolver](multiply-resolver.md)
- [NormalizeResolver](normalize-resolver.md)
- [NegateResolver](negate-resolver.md)
