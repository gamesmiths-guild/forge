# DegToRadResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.DegToRadResolver`
> **Output Type:** `float` or `double`

Converts an angle from degrees to radians. Computes `degrees * (π / 180)`. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new DegToRadResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand (angle in degrees). |

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
- Multiplies the value by `π / 180` and returns the result as a `Variant128`.
- `DegToRad(0) = 0`, `DegToRad(90) = π/2`, `DegToRad(180) = π`, `DegToRad(360) = 2π`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Use degrees with trig functions that expect radians
graph.VariableDefinitions.DefineProperty("sinOfAngle",
    new SinResolver(
        new DegToRadResolver(
            new VariableResolver("angleDegrees", typeof(double)))));
```

## See Also

- [Resolvers Overview](README.md)
- [RadToDegResolver](radtodeg-resolver.md)
- [SinResolver](sin-resolver.md)
- [CosResolver](cos-resolver.md)
