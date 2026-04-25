# SignResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SignResolver`
> **Output Type:** `int`

Returns the sign of a numeric operand as `-1`, `0`, or `1`. Supports all signed numeric types. The result is always `int` regardless of the operand type. Unsigned types, vector types, and quaternion types are not supported.

## Constructor

```csharp
new SignResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand. |

## Type Promotion

| Operand Type | Result Type |
|--------------|-------------|
| Any signed numeric type | `int` |

**Invalid types** (throw `ArgumentException` at construction time):
- Unsigned types (`byte`, `ushort`, `uint`, `ulong`).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Returns `-1` for negative values, `0` for zero, and `1` for positive values.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Determine movement direction toward a target
graph.VariableDefinitions.DefineProperty("moveDirection",
    new SignResolver(
        new SubtractResolver(
            new VariableResolver("targetX", typeof(float)),
            new VariableResolver("currentX", typeof(float)))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("turnDirection",
    new CopySignResolver(
        new VariableResolver("turnSpeed", typeof(double)),
        new SignResolver(
            new VariableResolver("turnError", typeof(double)))));
```

## See Also

- [Resolvers Overview](README.md)
- [AbsResolver](abs-resolver.md)
- [CopySignResolver](copysign-resolver.md)
