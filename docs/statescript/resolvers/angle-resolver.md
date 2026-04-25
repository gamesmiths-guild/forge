# AngleResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AngleResolver`
> **Output Type:** `float`

Computes the unsigned angle in radians between two vectors or two quaternions. Use this resolver when only the angular separation matters and you do not need a clockwise/counterclockwise sign or explicit rotation axis.

## Constructor

```csharp
new AngleResolver(from, to)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| from | `IPropertyResolver` | The resolver for the first operand. Must resolve to `Vector2`, `Vector3`, or `Quaternion`. |
| to | `IPropertyResolver` | The resolver for the second operand. Must match the `from` operand type. |

## Supported Types

| From Type | To Type | Result Type |
|-----------|---------|-------------|
| `Vector2` | `Vector2` | `float` |
| `Vector3` | `Vector3` | `float` |
| `Quaternion` | `Quaternion` | `float` |

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Requires both operands to have the same type at construction time.
- For `Vector2` and `Vector3`, returns the unsigned angle between the two vectors in radians.
- For `Quaternion`, returns the angular difference between the two rotations in radians.
- Throws `ArgumentException` at construction time if the operand types do not match or are not supported.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("turnError",
    new AngleResolver(
        new VariableResolver("currentForward", typeof(Vector3)),
        new VariableResolver("desiredForward", typeof(Vector3))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("isFacingTarget",
    new ComparisonResolver(
        new AngleResolver(
            new VariableResolver("currentRotation", typeof(Quaternion)),
            new LookAtResolver(
                new VariableResolver("selfPosition", typeof(Vector3)),
                new VariableResolver("targetPosition", typeof(Vector3)),
                new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)))),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(0.15f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [SignedAngleResolver](signedangle-resolver.md)
- [DotResolver](dot-resolver.md)
