# Log2Resolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.Log2Resolver`
> **Output Type:** `float` or `double`

Computes the base-2 logarithm of an operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new Log2Resolver(operand)
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
- Computes the base-2 logarithm and returns the result as a `Variant128`.
- `Log2(1) = 0`, `Log2(2) = 1`, `Log2(8) = 3`, `Log2(1024) = 10`.
- Negative operands produce `NaN`. Zero produces `-∞`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Calculate bits needed to represent a number of values
graph.VariableDefinitions.DefineProperty("bitsNeeded",
    new CeilResolver(
        new Log2Resolver(
            new VariableResolver("numValues", typeof(double)))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("mipLevel",
    new FloorResolver(
        new Log2Resolver(
            new MaxResolver(
                new VariableResolver("textureSize", typeof(double)),
                new VariantResolver(new Variant128(1.0), typeof(double))))));
```

## See Also

- [Resolvers Overview](README.md)
- [LogResolver](log-resolver.md)
- [Log10Resolver](log10-resolver.md)
- [ExpResolver](exp-resolver.md)
