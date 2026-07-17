# WrapResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.WrapResolver`
> **Output Type:** `float`

Wraps a value into a `[min, max)` range.

## Constructor

```csharp
new WrapResolver(value, min, max)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| value | `IPropertyResolver` | The value to wrap. |
| min | `IPropertyResolver` | The range start (inclusive). |
| max | `IPropertyResolver` | The range end (exclusive). |

## Behavior

- Numeric operands are read as `float`. Non-positive ranges resolve to `min`.

## Usage

```csharp
// Wrap an accumulating angle into [0, 2π)
graph.VariableDefinitions.DefineProperty("wrappedAngle",
    new WrapResolver(
        new VariableResolver("rawAngle"),
        new VariantResolver(new Variant128(0f), typeof(float)),
        new VariantResolver(new Variant128(MathF.Tau), typeof(float))));
```

## Composition

```csharp
// Wrap a cycling index, then use it to select from an array
graph.VariableDefinitions.DefineProperty("cycledIndex",
    new WrapResolver(
        new VariableResolver("tick"),
        new VariantResolver(new Variant128(0f), typeof(float)),
        new VariantResolver(new Variant128(4f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [PingPongResolver](ping-pong-resolver.md)
- [DeltaAngleResolver](delta-angle-resolver.md)
