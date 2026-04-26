# CbrtResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.CbrtResolver`
> **Output Type:** `float` or `double`

Computes the cube root of an operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new CbrtResolver(operand)
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
- Computes `Math.Cbrt` (or `MathF.Cbrt` for `float`) and returns the result as a `Variant128`.
- `Cbrt(0) = 0`, `Cbrt(1) = 1`, `Cbrt(27) = 3`.
- Unlike `Sqrt`, cube root handles negative values: `Cbrt(-8) = -2`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Convert volume to side length of a cube
graph.VariableDefinitions.DefineProperty("sideLength",
    new CbrtResolver(
        new VariableResolver("volume", typeof(double))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("scaledCubeSide",
    new MultiplyResolver(
        new VariableResolver("voxelScale", typeof(double)),
        new CbrtResolver(
            new VariableResolver("volume", typeof(double)))));
```

## See Also

- [Resolvers Overview](README.md)
- [SqrtResolver](sqrt-resolver.md)
- [PowResolver](pow-resolver.md)
