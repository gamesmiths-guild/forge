# EResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EResolver`
> **Output Type:** `float` or `double`

Returns the mathematical constant `e` (Euler's number, the base of the natural logarithm). Defaults to `double` precision. Use this instead of magic numbers to communicate intent clearly.

## Constructor

```csharp
new EResolver()                // returns double
new EResolver(typeof(float))   // returns float
```

| Parameter | Type | Description |
|-----------|------|-------------|
| valueType | `Type?` | The desired output type. Must be `float` or `double`. Defaults to `double`. |

**Invalid types** (throw `ArgumentException` at construction time):
- `int`, `decimal`, or any non-floating-point type.

## Behavior

- Returns `Math.E` for `double` or `MathF.E` for `float`.
- The value is always constant: `2.71828182845904...`.

## Usage

```csharp
// Natural exponential: e^x
graph.VariableDefinitions.DefineProperty("naturalExp",
    new PowResolver(
        new EResolver(),
        new VariableResolver("exponent", typeof(double))));
```

## See Also

- [Resolvers Overview](README.md)
- [PiResolver](pi-resolver.md)
- [ExpResolver](exp-resolver.md)
- [LogResolver](log-resolver.md)
