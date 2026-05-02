# RadToDegResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RadToDegResolver`
> **Output Type:** `float`, `double`, `Vector2`, `Vector3`, or `Vector4`

Converts an angle from radians to degrees. Computes `radians * (180 / π)`. Supports `float` and `double` types, as well as `Vector2`, `Vector3`, and `Vector4` for component-wise conversion. Integer operand types are promoted to `double`. Decimal and quaternion types are not supported.

## Constructor

```csharp
new RadToDegResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand (angle in radians). |

## Type Promotion

| Operand Type | Result Type |
|--------------|-------------|
| `float` | `float` |
| `double` | `double` |
| Any integer type | `double` |
| `Vector2` | `Vector2` |
| `Vector3` | `Vector3` |
| `Vector4` | `Vector4` |

**Invalid types** (throw `ArgumentException` at construction time):
- `decimal` (use `double` instead).
- `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Multiplies the value by `180 / π` and returns the result as a `Variant128`.
- For vector types, conversion is applied component-wise.
- `RadToDeg(0) = 0`, `RadToDeg(π/2) = 90`, `RadToDeg(π) = 180`, `RadToDeg(2π) = 360`.
- `RadToDeg` and `DegToRad` are inverses: `RadToDeg(DegToRad(x)) = x`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Convert ATan2 result to degrees for display
graph.VariableDefinitions.DefineProperty("aimAngleDegrees",
    new RadToDegResolver(
        new ATan2Resolver(
            new VariableResolver("targetY", typeof(double)),
            new VariableResolver("targetX", typeof(double)))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("yawDegrees",
    new RadToDegResolver(
        new SignedAngleResolver(
            new VariantResolver(new Variant128(Vector3.UnitZ), typeof(Vector3)),
            new VariableResolver("targetDirection", typeof(Vector3)),
            new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)))));

graph.VariableDefinitions.DefineProperty("eulerDegrees",
    new RadToDegResolver(
        new VariableResolver("eulerRadians", typeof(Vector3))));
```

## See Also

- [Resolvers Overview](README.md)
- [DegToRadResolver](degtorad-resolver.md)
- [ATan2Resolver](atan2-resolver.md)
