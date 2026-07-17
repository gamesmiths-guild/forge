# SmoothStepResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SmoothStepResolver`
> **Output Type:** `float`

Computes the smooth Hermite interpolation of a value between two edges, producing a `float` from 0 to 1.

## Constructor

```csharp
new SmoothStepResolver(edge0, edge1, value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| edge0 | `IPropertyResolver` | The lower edge. |
| edge1 | `IPropertyResolver` | The upper edge. |
| value | `IPropertyResolver` | The value to interpolate. |

## Behavior

- Numeric operands are read as `float`. Returns `0` below `edge0` and `1` above `edge1`, with a smooth `t*t*(3-2t)` curve in between.

## Usage

```csharp
// Ease a 0-1 blend as a value crosses a threshold band
graph.VariableDefinitions.DefineProperty("chargeBlend",
    new SmoothStepResolver(
        new VariantResolver(new Variant128(0.2f), typeof(float)),
        new VariantResolver(new Variant128(0.8f), typeof(float)),
        new VariableResolver("chargeRatio")));
```

## Composition

```csharp
// Use the eased value as the interpolation parameter of a Lerp
graph.VariableDefinitions.DefineProperty("easedScale",
    new LerpResolver(
        new VariantResolver(new Variant128(1f), typeof(float)),
        new VariantResolver(new Variant128(2f), typeof(float)),
        new SmoothStepResolver(
            new VariantResolver(new Variant128(0f), typeof(float)),
            new VariantResolver(new Variant128(1f), typeof(float)),
            new VariableResolver("chargeRatio"))));
```

## See Also

- [Resolvers Overview](README.md)
- [LerpResolver](lerp-resolver.md)
- [InverseLerpResolver](inverse-lerp-resolver.md)
