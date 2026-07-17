# RemapResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RemapResolver`
> **Output Type:** `float` / `double`

Remaps a value from an input range to an output range: `outMin + (value - inMin) / (inMax - inMin) * (outMax - outMin)`.

## Constructor

```csharp
new RemapResolver(value, inMin, inMax, outMin, outMax, clamp = false)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| value | `IPropertyResolver` | The value to remap. |
| inMin, inMax | `IPropertyResolver` | The input range. |
| outMin, outMax | `IPropertyResolver` | The output range. |
| clamp | `bool` | Whether to clamp the result to the output range. |

## Behavior

- Numeric operands are promoted to `float`, or `double` when any operand is a `double`.
- When the input range is degenerate, the result is `outMin`. Values outside the input range extrapolate unless `clamp` is enabled.

## Usage

```csharp
// Map health (0..100) to a camera shake amount (0..1), clamped
graph.VariableDefinitions.DefineProperty("shakeAmount",
    new RemapResolver(
        new AttributeResolver("CombatAttributeSet.Health"),
        new VariantResolver(new Variant128(0f), typeof(float)),
        new VariantResolver(new Variant128(100f), typeof(float)),
        new VariantResolver(new Variant128(1f), typeof(float)),
        new VariantResolver(new Variant128(0f), typeof(float)),
        clamp: true));
```

## Composition

```csharp
// Remap a stat, then clamp-compare it as a branch condition
graph.VariableDefinitions.DefineProperty("overThreshold",
    new ComparisonResolver(
        new RemapResolver(
            new AttributeResolver("CombatAttributeSet.Rage"),
            new VariantResolver(new Variant128(0f), typeof(float)),
            new VariantResolver(new Variant128(100f), typeof(float)),
            new VariantResolver(new Variant128(0f), typeof(float)),
            new VariantResolver(new Variant128(1f), typeof(float)),
            clamp: true),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(0.75f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [InverseLerpResolver](inverse-lerp-resolver.md)
- [LerpResolver](lerp-resolver.md)
