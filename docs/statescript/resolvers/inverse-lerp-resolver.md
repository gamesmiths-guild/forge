# InverseLerpResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.InverseLerpResolver`
> **Output Type:** `float` / `double`

Computes the normalized position of a value within a range — the inverse of a [Lerp](lerp-resolver.md). Computes `(value - a) / (b - a)` clamped to 0-1.

## Constructor

```csharp
new InverseLerpResolver(a, b, value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| a | `IPropertyResolver` | The range start. |
| b | `IPropertyResolver` | The range end. |
| value | `IPropertyResolver` | The value to normalize. |

## Behavior

- Numeric operands are promoted to `float`, or `double` when any operand is a `double`.
- When `a` and `b` are equal, the result is `0`.

> **Scalar only (by design).** Unlike [LerpResolver](lerp-resolver.md), this resolver does not accept vector or quaternion operands. A lerp is a forward map `(a, b, t) → point` whose parameter `t` is always scalar; its inverse `(a, b, value) → t` also produces a scalar, so a per-component "inverse lerp" is not the true inverse of a single-parameter lerp (the per-axis parameters disagree once `value` leaves the segment). This matches the scalar-only `InverseLerp` in Unity, Unreal, and Godot. For the parameter of a point `v` along a vector segment `a → b`, compose the projection directly: `t = Dot(v - a, b - a) / LengthSquared(b - a)` using [DotResolver](dot-resolver.md), [SubtractResolver](subtract-resolver.md), and [LengthSquaredResolver](lengthsquared-resolver.md).

## Usage

```csharp
// Normalize current health to a 0-1 fraction
graph.VariableDefinitions.DefineProperty("healthFraction",
    new InverseLerpResolver(
        new VariantResolver(new Variant128(0f), typeof(float)),
        new AttributeResolver("CombatAttributeSet.MaxHealth"),
        new AttributeResolver("CombatAttributeSet.Health")));
```

## Composition

```csharp
// Feed the normalized value into a Lerp to drive a scaled magnitude
graph.VariableDefinitions.DefineProperty("scaledSpeed",
    new LerpResolver(
        new VariantResolver(new Variant128(2f), typeof(float)),
        new VariantResolver(new Variant128(6f), typeof(float)),
        new InverseLerpResolver(
            new VariantResolver(new Variant128(0f), typeof(float)),
            new VariantResolver(new Variant128(100f), typeof(float)),
            new AttributeResolver("CombatAttributeSet.Rage"))));
```

## See Also

- [Resolvers Overview](README.md)
- [LerpResolver](lerp-resolver.md)
- [RemapResolver](remap-resolver.md)
