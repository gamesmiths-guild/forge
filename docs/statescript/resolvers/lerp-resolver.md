# LerpResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.LerpResolver`
> **Output Type:** `float`, `double`, `Vector2`, `Vector3`, `Vector4`, or `Quaternion`

Computes a linear interpolation between two values. For scalar types, computes `a + (b - a) * t`. For `Vector2`, `Vector3`, `Vector4`, and `Quaternion`, uses the built-in `Lerp` methods from `System.Numerics`. Supports `float` and `double` scalar types. For vector and quaternion types, `a` and `b` must match exactly, and `t` must be `float`. Integer and decimal types are not supported.

## Constructor

```csharp
new LerpResolver(a, b, t)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| a | `IPropertyResolver` | The resolver for the start value. |
| b | `IPropertyResolver` | The resolver for the end value. |
| t | `IPropertyResolver` | The resolver for the interpolation parameter (typically 0 to 1). |

## Type Promotion

### Scalar Types

| Operand Types | Result Type |
|---------------|-------------|
| All `float` | `float` |
| Any `double` | `double` |

### Vector and Quaternion Types

| `a` / `b` Type | `t` Type | Result Type |
|-----------------|----------|-------------|
| `Vector2` | `float` | `Vector2` |
| `Vector3` | `float` | `Vector3` |
| `Vector4` | `float` | `Vector4` |
| `Quaternion` | `float` | `Quaternion` |

**Invalid types** (throw `ArgumentException` at construction time):
- `int`, `long`, `decimal`, and all other integer types.
- Mismatched `a` and `b` types (e.g., `Vector2` and `Vector3`).
- Non-`float` `t` for vector/quaternion types.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves all three operands through their respective `IPropertyResolver` instances.
- For scalar types, computes `a + (b - a) * t` and returns the result as a `Variant128`.
- For vector types, delegates to `Vector2.Lerp`, `Vector3.Lerp`, or `Vector4.Lerp`.
- For quaternion types, delegates to `Quaternion.Lerp` (normalized linear interpolation).
- When `t = 0`, returns `a`. When `t = 1`, returns `b`.
- Values of `t` outside `[0, 1]` will extrapolate for scalar types. Vector and quaternion built-in Lerp methods may clamp `t`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Smoothly blend between walk and run speed
graph.VariableDefinitions.DefineProperty("currentSpeed",
    new LerpResolver(
        new VariableResolver("walkSpeed", typeof(float)),
        new VariableResolver("runSpeed", typeof(float)),
        new VariableResolver("blendFactor", typeof(float))));

// Interpolate between two positions
graph.VariableDefinitions.DefineProperty("currentPosition",
    new LerpResolver(
        new VariableResolver("startPosition", typeof(Vector3)),
        new VariableResolver("endPosition", typeof(Vector3)),
        new VariableResolver("progress", typeof(float))));

// Smoothly rotate between two orientations
graph.VariableDefinitions.DefineProperty("currentRotation",
    new LerpResolver(
        new VariableResolver("startRotation", typeof(Quaternion)),
        new VariableResolver("targetRotation", typeof(Quaternion)),
        new VariableResolver("rotationProgress", typeof(float))));
```

## Composition

```csharp
// Safe lerp with clamped t parameter
graph.VariableDefinitions.DefineProperty("safeLerp",
    new LerpResolver(
        new VariableResolver("startValue", typeof(float)),
        new VariableResolver("endValue", typeof(float)),
        new ClampResolver(
            new VariableResolver("rawT", typeof(float)),
            new VariantResolver(new Variant128(0.0f), typeof(float)),
            new VariantResolver(new Variant128(1.0f), typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [ClampResolver](clamp-resolver.md)
- [AddResolver](add-resolver.md)
- [MultiplyResolver](multiply-resolver.md)
