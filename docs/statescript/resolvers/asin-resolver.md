# ASinResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ASinResolver`
> **Output Type:** `float` or `double`

Computes the arc sine (inverse sine) of an operand, returning the angle in radians. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new ASinResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand (value in the range [-1, 1]). |

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
- Computes `Math.Asin` (or `MathF.Asin` for `float`) and returns the result as a `Variant128`.
- `ASin(0) = 0`, `ASin(1) = π/2`, `ASin(-1) = -π/2`.
- Values outside `[-1, 1]` produce `NaN`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("elevationAngle",
    new ASinResolver(
        new VariableResolver("normalizedHeight", typeof(float))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("pitchFromDirection",
    new ASinResolver(
        new ClampResolver(
            new VectorComponentResolver(
                new VariableResolver("forward", typeof(Vector3)),
                VectorComponent.Y),
            new VariantResolver(new Variant128(-1.0f), typeof(float)),
            new VariantResolver(new Variant128(1.0f), typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [SinResolver](sin-resolver.md)
- [ACosResolver](acos-resolver.md)
- [ASinHResolver](asinh-resolver.md)
