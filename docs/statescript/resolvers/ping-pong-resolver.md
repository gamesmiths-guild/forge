# PingPongResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.PingPongResolver`
> **Output Type:** `float`

Bounces a value back and forth between 0 and a length.

## Constructor

```csharp
new PingPongResolver(value, length)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| value | `IPropertyResolver` | The value to bounce. |
| length | `IPropertyResolver` | The bounce length. |

## Behavior

- Numeric operands are read as `float`. Non-positive lengths resolve to `0`.

## Usage

```csharp
// Oscillate a value between 0 and 1 from an increasing time input
graph.VariableDefinitions.DefineProperty("pulse",
    new PingPongResolver(
        new VariableResolver("elapsed"),
        new VariantResolver(new Variant128(1f), typeof(float))));
```

## Composition

```csharp
// Drive a Lerp'd intensity that bounces over time
graph.VariableDefinitions.DefineProperty("glowIntensity",
    new LerpResolver(
        new VariantResolver(new Variant128(0.5f), typeof(float)),
        new VariantResolver(new Variant128(1.5f), typeof(float)),
        new PingPongResolver(
            new VariableResolver("elapsed"),
            new VariantResolver(new Variant128(1f), typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [WrapResolver](wrap-resolver.md)
