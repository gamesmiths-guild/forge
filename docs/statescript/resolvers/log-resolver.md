# LogResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.LogResolver`
> **Output Type:** `float` or `double`

Computes the natural logarithm (base `e`) of an operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new LogResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand (must be positive). |

## Type Promotion

| Operand Type | Result Type |
|--------------|-------------|
| `float` | `float` |
| `double` | `double` |
| Any integer type | `double` |

**Invalid types** (throw `ArgumentException` at construction time):
- `decimal` (use `double` instead).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Computes `Math.Log` (or `MathF.Log` for `float`) and returns the result as a `Variant128`.
- `Log(1) = 0`, `Log(e) = 1`.
- Negative operands produce `NaN`. Zero produces `-∞`.
- `Log` and `Exp` are inverses: `Log(Exp(x)) = x`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Logarithmic difficulty scaling
graph.VariableDefinitions.DefineProperty("difficulty",
    new AddResolver(
        new VariableResolver("baseDifficulty", typeof(double)),
        new MultiplyResolver(
            new VariableResolver("scaleFactor", typeof(double)),
            new LogResolver(
                new VariableResolver("level", typeof(double))))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("expRoundTrip",
    new LogResolver(
        new ExpResolver(
            new VariableResolver("input", typeof(double)))));
```

## See Also

- [Resolvers Overview](README.md)
- [ExpResolver](exp-resolver.md)
- [Log2Resolver](log2-resolver.md)
- [Log10Resolver](log10-resolver.md)
