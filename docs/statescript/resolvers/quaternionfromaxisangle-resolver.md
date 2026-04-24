# QuaternionFromAxisAngleResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.QuaternionFromAxisAngleResolver`
> **Output Type:** `Quaternion`

Creates a quaternion from an axis and angle using `Quaternion.CreateFromAxisAngle`.

## Constructor

```csharp
new QuaternionFromAxisAngleResolver(axis, angle)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| axis | `IPropertyResolver` | The resolver for the axis vector. |
| angle | `IPropertyResolver` | The resolver for the angle in radians. |

## Supported Types

| Axis Type | Angle Type | Result Type |
|-----------|------------|-------------|
| `Vector3` | `float` | `Quaternion` |

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Delegates to `Quaternion.CreateFromAxisAngle(axis, angle)`.
- Returns the created quaternion as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("rotation",
    new QuaternionFromAxisAngleResolver(
        new VariableResolver("axis", typeof(Vector3)),
        new VariableResolver("angleRadians", typeof(float))));
```

## Composition

```csharp
// Create a quaternion from an axis-angle pair and transform a vector
graph.VariableDefinitions.DefineProperty("rotatedDirection",
    new TransformResolver(
        new VariableResolver("direction", typeof(Vector3)),
        new QuaternionFromAxisAngleResolver(
            new VariableResolver("axis", typeof(Vector3)),
            new VariableResolver("angleRadians", typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [TransformResolver](transform-resolver.md)
- [QuaternionFromPitchYawRollResolver](quaternionfrompitchyawroll-resolver.md)
