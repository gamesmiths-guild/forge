# NegateResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.NegateResolver`
> **Output Type:** *(same as operand, promoted for sub-int types)*

Negates a single operand (unary minus). Supports all numeric types in `Variant128` as well as `Vector2`, `Vector3`, `Vector4`, and `Quaternion`. Sub-int types (`byte`, `sbyte`, `short`, `ushort`) are promoted to `int`.

## Constructor

```csharp
new NegateResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand to negate. |

## Type Promotion

The result type matches the operand type, with sub-int types promoted to `int`:

| Operand Type | Result Type |
|--------------|-------------|
| `byte`, `sbyte`, `short`, `ushort` | `int` |
| `int` | `int` |
| `uint` | `long` |
| `long` | `long` |
| `ulong` | `double` |
| `float` | `float` |
| `double` | `double` |
| `decimal` | `decimal` |
| `Vector2` | `Vector2` |
| `Vector3` | `Vector3` |
| `Vector4` | `Vector4` |
| `Quaternion` | `Quaternion` |

**Invalid types** (throw `ArgumentException` at construction time):
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Applies unary negation (`-value`) and returns the result as a `Variant128`.
- For vectors and quaternions, negates all components.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("invertedSpeed",
    new NegateResolver(
        new VariableResolver("speed", typeof(float))));

graph.VariableDefinitions.DefineProperty("oppositeDirection",
    new NegateResolver(
        new VariableResolver("direction", typeof(Vector3))));
```

## Composition

```csharp
// Reverse a velocity and add gravity: -velocity + gravity
graph.VariableDefinitions.DefineProperty("bouncedVelocity",
    new AddResolver(
        new NegateResolver(
            new VariableResolver("velocity", typeof(Vector3))),
        new VariableResolver("gravity", typeof(Vector3))));
```

## See Also

- [Resolvers Overview](README.md)
- [AbsResolver](abs-resolver.md)
- [SubtractResolver](subtract-resolver.md)
