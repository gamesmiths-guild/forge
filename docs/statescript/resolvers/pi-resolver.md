# PiResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.PiResolver`
> **Output Type:** `float` or `double`

Returns the mathematical constant π (pi). Defaults to `double` precision. Use this instead of magic numbers to communicate intent clearly.

## Constructor

```csharp
new PiResolver()                // returns double
new PiResolver(typeof(float))   // returns float
```

| Parameter | Type | Description |
|-----------|------|-------------|
| valueType | `Type?` | The desired output type. Must be `float` or `double`. Defaults to `double`. |

**Invalid types** (throw `ArgumentException` at construction time):
- `int`, `decimal`, or any non-floating-point type.

## Behavior

- Returns `Math.PI` for `double` or `MathF.PI` for `float`.
- The value is always constant: `3.14159265358979...`.

## Usage

```csharp
// Circle area: Pi * r^2
graph.VariableDefinitions.DefineProperty("circleArea",
    new MultiplyResolver(
        new PiResolver(),
        new PowResolver(
            new VariableResolver("radius", typeof(double)),
            new VariantResolver(new Variant128(2.0), typeof(double)))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("fullTurnRadians",
    new MultiplyResolver(
        new VariantResolver(new Variant128(2.0), typeof(double)),
        new PiResolver()));
```

## See Also

- [Resolvers Overview](README.md)
- [EResolver](e-resolver.md)
- [DegToRadResolver](degtorad-resolver.md)
