# ASinHResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ASinHResolver`
> **Output Type:** `float` or `double`

Computes the inverse hyperbolic sine of an operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new ASinHResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand. |

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
- Computes `Math.Asinh` (or `MathF.Asinh` for `float`) and returns the result as a `Variant128`.
- `ASinH(0) = 0`. ASinH is an odd function.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("compressedValue",
    new ASinHResolver(
        new VariableResolver("rawMagnitude", typeof(double))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("centeredCompression",
    new ASinHResolver(
        new SubtractResolver(
            new VariableResolver("sample", typeof(double)),
            new VariableResolver("sampleCenter", typeof(double)))));
```

## See Also

- [Resolvers Overview](README.md)
- [SinHResolver](sinh-resolver.md)
- [ASinResolver](asin-resolver.md)
