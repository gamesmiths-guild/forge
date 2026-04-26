# TransformResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.TransformResolver`
> **Output Type:** `Vector2`, `Vector3`, `Vector4`, or `Plane`

Transforms a vector or plane by a quaternion rotation. Supports `Vector2`, `Vector3`, `Vector4`, and `Plane` as the value operand, and requires a `Quaternion` as the rotation operand.

## Constructor

```csharp
new TransformResolver(value, rotation)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| value | `IPropertyResolver` | The resolver for the vector or plane value to transform. |
| rotation | `IPropertyResolver` | The resolver for the quaternion rotation. |

## Supported Types

| Value Type | Rotation Type | Result Type |
|------------|---------------|-------------|
| `Vector2` | `Quaternion` | `Vector2` |
| `Vector3` | `Quaternion` | `Vector3` |
| `Vector4` | `Quaternion` | `Vector4` |
| `Plane` | `Quaternion` | `Plane` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- Non-`Quaternion` rotation operand.
- Unsupported value types.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Delegates to `Vector2.Transform`, `Vector3.Transform`, `Vector4.Transform`, or `Plane.Transform` depending on the value type.
- Applies quaternion-based rotation to the value and returns the transformed result as a `Variant128`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Rotate a direction vector by an orientation quaternion
graph.VariableDefinitions.DefineProperty("worldDirection",
    new TransformResolver(
        new VariableResolver("localDirection", typeof(Vector3)),
        new VariableResolver("rotation", typeof(Quaternion))));
```

## Composition

```csharp
// Transform a plane and normalize the result
graph.VariableDefinitions.DefineProperty("normalizedPlane",
    new NormalizeResolver(
        new TransformResolver(
            new VariableResolver("clipPlane", typeof(Plane)),
            new VariableResolver("rotation", typeof(Quaternion)))));
```

## See Also

- [Resolvers Overview](README.md)
- [NormalizeResolver](normalize-resolver.md)
- [QuaternionFromAxisAngleResolver](quaternionfromaxisangle-resolver.md)
- [QuaternionFromYawPitchRollResolver](quaternionfromyawpitchroll-resolver.md)
