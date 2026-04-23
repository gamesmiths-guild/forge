# ReflectResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ReflectResolver`
> **Output Type:** `Vector2` or `Vector3`

Resolves the reflection of a vector off a surface defined by a normal vector. Returns a vector of the same type as the operands. Both operands must be the same vector type. Supports `Vector2` and `Vector3`. `Vector4`, scalar, quaternion, and plane types are not supported.

## Constructor

```csharp
new ReflectResolver(incident, normal)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| incident | `IPropertyResolver` | The resolver for the incident (incoming) vector. |
| normal | `IPropertyResolver` | The resolver for the surface normal vector. |

## Supported Types

| Operand Types | Result Type |
|---------------|-------------|
| `Vector2`, `Vector2` | `Vector2` |
| `Vector3`, `Vector3` | `Vector3` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- Mismatched vector types (e.g., `Vector2` and `Vector3`).
- `Vector4`.
- Scalar types (`int`, `float`, `double`, etc.).
- `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Delegates to `Vector2.Reflect` or `Vector3.Reflect` depending on the operand type.
- Computes the reflection using the formula: `incident - 2 * dot(incident, normal) * normal`.
- The normal vector should be unit-length for correct results.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Reflect a projectile direction off a wall
graph.VariableDefinitions.DefineProperty("reflectedDirection",
    new ReflectResolver(
        new VariableResolver("projectileDirection", typeof(Vector3)),
        new VariableResolver("wallNormal", typeof(Vector3))));
```

## Composition

```csharp
// Reflect and normalize the result
graph.VariableDefinitions.DefineProperty("reflectedUnit",
    new NormalizeResolver(
        new ReflectResolver(
            new VariableResolver("incomingDirection", typeof(Vector3)),
            new VariableResolver("surfaceNormal", typeof(Vector3)))));
```

## See Also

- [Resolvers Overview](README.md)
- [NormalizeResolver](normalize-resolver.md)
- [DotResolver](dot-resolver.md)
- [NegateResolver](negate-resolver.md)
