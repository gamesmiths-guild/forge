# SlerpResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SlerpResolver`
> **Output Type:** `Quaternion`

Resolves a spherical linear interpolation between two `Quaternion` operands. This produces a smooth constant-speed rotation between two orientations, which is preferred over [LerpResolver](lerp-resolver.md) for rotation interpolation when uniform angular velocity is needed. The `t` parameter must be `float`.

## Constructor

```csharp
new SlerpResolver(a, b, t)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| a | `IPropertyResolver` | The resolver for the start quaternion. |
| b | `IPropertyResolver` | The resolver for the end quaternion. |
| t | `IPropertyResolver` | The resolver for the interpolation parameter (typically 0 to 1). |

## Supported Types

| `a` / `b` Type | `t` Type | Result Type |
|-----------------|----------|-------------|
| `Quaternion` | `float` | `Quaternion` |

**Invalid types** (throw `ArgumentException` at construction time):
- Non-`Quaternion` types for `a` or `b`.
- Non-`float` type for `t`.
- Scalar types, vector types.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves all three operands through their respective `IPropertyResolver` instances.
- Delegates to `Quaternion.Slerp(a, b, t)`.
- Produces constant angular velocity interpolation along the shortest arc between two rotations.
- When `t = 0`, returns `a`. When `t = 1`, returns `b`.
- Unlike `Lerp`, `Slerp` maintains uniform rotation speed throughout the interpolation, making it ideal for animation and camera rotations.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Smoothly rotate between two orientations with constant angular velocity
graph.VariableDefinitions.DefineProperty("currentRotation",
    new SlerpResolver(
        new VariableResolver("startRotation", typeof(Quaternion)),
        new VariableResolver("targetRotation", typeof(Quaternion)),
        new VariableResolver("rotationProgress", typeof(float))));
```

## Composition

```csharp
// Slerp with clamped t parameter
graph.VariableDefinitions.DefineProperty("safeRotation",
    new SlerpResolver(
        new VariableResolver("fromRotation", typeof(Quaternion)),
        new VariableResolver("toRotation", typeof(Quaternion)),
        new ClampResolver(
            new VariableResolver("rawT", typeof(float)),
            new VariantResolver(new Variant128(0.0f), typeof(float)),
            new VariantResolver(new Variant128(1.0f), typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [LerpResolver](lerp-resolver.md)
- [NormalizeResolver](normalize-resolver.md)
