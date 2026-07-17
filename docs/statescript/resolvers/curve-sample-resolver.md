# CurveSampleResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.CurveSampleResolver`
> **Output Type:** `float`

Samples an `ICurve` at a resolved position. Engine curve assets (such as Godot curves) plug in through their existing `ICurve` adapters — the same abstraction scalable magnitudes use.

## Constructor

```csharp
new CurveSampleResolver(curve, time)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| curve | `ICurve` | The curve to sample. |
| time | `IPropertyResolver` | The sample position. |

## Behavior

- Reads the **time** as `float` and returns `curve.Evaluate(time)`.

## Usage

```csharp
// Sample a falloff curve at a normalized distance
graph.VariableDefinitions.DefineProperty("falloff",
    new CurveSampleResolver(falloffCurve, new VariableResolver("normalizedDistance")));
```

## Composition

```csharp
// Scale a base magnitude by the sampled curve value
graph.VariableDefinitions.DefineProperty("scaledMagnitude",
    new MultiplyResolver(
        new AbilityMagnitudeResolver(),
        new CurveSampleResolver(falloffCurve, new VariableResolver("normalizedDistance"))));
```

## See Also

- [Resolvers Overview](README.md)
- [Effects: Curves and scaling](../../effects/README.md#effect-levels-and-scaling)
