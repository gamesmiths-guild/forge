# ACosResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ACosResolver`
> **Output Type:** `float` or `double`

Computes the arc cosine (inverse cosine) of an operand, returning the angle in radians. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new ACosResolver(operand)
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
- Computes `Math.Acos` (or `MathF.Acos` for `float`) and returns the result as a `Variant128`.
- `ACos(1) = 0`, `ACos(0) = π/2`, `ACos(-1) = π`.
- Values outside `[-1, 1]` produce `NaN`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("slopeAngle",
    new ACosResolver(
        new DotResolver(
            new VariableResolver("surfaceNormal", typeof(Vector3)),
            new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("coneHalfAngleRadians",
    new ACosResolver(
        new ClampResolver(
            new VariableResolver("alignment", typeof(float)),
            new VariantResolver(new Variant128(-1.0f), typeof(float)),
            new VariantResolver(new Variant128(1.0f), typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [CosResolver](cos-resolver.md)
- [ASinResolver](asin-resolver.md)
- [ACosHResolver](acosh-resolver.md)
