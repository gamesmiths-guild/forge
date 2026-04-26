# SignedAngleResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SignedAngleResolver`
> **Output Type:** `float`

Computes the signed angle in radians between two vectors. Use this resolver when the direction of rotation matters: `Vector2` operands infer the sign from the 2D plane, while `Vector3` operands require an explicit axis to define the positive rotation direction.

## Constructors

```csharp
new SignedAngleResolver(from, to)
new SignedAngleResolver(from, to, axis)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| from | `IPropertyResolver` | The resolver for the first vector operand. |
| to | `IPropertyResolver` | The resolver for the second vector operand. Must match the `from` operand type. |
| axis | `IPropertyResolver` | The resolver for the signed axis when using `Vector3` operands. Do not provide this for `Vector2`. |

## Supported Types

| From Type | To Type | Axis Type | Result Type |
|-----------|---------|-----------|-------------|
| `Vector2` | `Vector2` | *(none)* | `float` |
| `Vector3` | `Vector3` | `Vector3` | `float` |

## Behavior

- Resolves the `from` and `to` operands on each call.
- For `Vector2`, returns the signed 2D angle in radians and does not accept an axis resolver.
- For `Vector3`, requires an axis resolver and returns the signed angle in radians around that axis.
- Requires `from` and `to` to have matching types at construction time.
- Throws `ArgumentException` at construction time if operand types are unsupported, mismatched, or if axis usage does not match the vector dimensionality.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("steeringAngle",
    new SignedAngleResolver(
        new VariableResolver("currentForward2D", typeof(Vector2)),
        new VariableResolver("desiredForward2D", typeof(Vector2))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("yawToTarget",
    new SignedAngleResolver(
        new TransformResolver(
            new VariantResolver(new Variant128(Vector3.UnitZ), typeof(Vector3)),
            new VariableResolver("currentRotation", typeof(Quaternion))),
        new RejectResolver(
            new VariableResolver("targetDirection", typeof(Vector3)),
            new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3))),
        new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3))));
```

## See Also

- [Resolvers Overview](README.md)
- [AngleResolver](angle-resolver.md)
- [CrossResolver](cross-resolver.md)
