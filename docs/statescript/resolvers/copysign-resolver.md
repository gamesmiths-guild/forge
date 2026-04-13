# CopySignResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.CopySignResolver`
> **Output Type:** `float` or `double`

Returns a value with the magnitude of the first operand and the sign of the second operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new CopySignResolver(magnitude, sign)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| magnitude | `IPropertyResolver` | The resolver for the magnitude operand. |
| sign | `IPropertyResolver` | The resolver for the sign operand. |

## Type Promotion

| Operand Types | Result Type |
|---------------|-------------|
| Both `float` | `float` |
| Any `double` | `double` |
| Any integer type | `double` |

**Invalid types** (throw `ArgumentException` at construction time):
- `decimal` (use `double` instead).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Returns `|magnitude|` with the sign of `sign`.
- `CopySign(5, -3) = -5`, `CopySign(-5, 3) = 5`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Apply direction to a speed value
graph.VariableDefinitions.DefineProperty("directedSpeed",
    new CopySignResolver(
        new AbsResolver(
            new VariableResolver("speed", typeof(float))),
        new VariableResolver("direction", typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [SignResolver](sign-resolver.md)
- [AbsResolver](abs-resolver.md)
