# QuaternionFromPitchYawRollResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.QuaternionFromPitchYawRollResolver`
> **Output Type:** `Quaternion`

Creates a quaternion from pitch, yaw, and roll angles using `Quaternion.CreateFromYawPitchRoll`.

## Constructor

```csharp
new QuaternionFromPitchYawRollResolver(pitch, yaw, roll)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| pitch | `IPropertyResolver` | The resolver for the pitch angle in radians. |
| yaw | `IPropertyResolver` | The resolver for the yaw angle in radians. |
| roll | `IPropertyResolver` | The resolver for the roll angle in radians. |

## Supported Types

| Pitch Type | Yaw Type | Roll Type | Result Type |
|------------|----------|-----------|-------------|
| `float` | `float` | `float` | `Quaternion` |

## Behavior

- Resolves all three operands through their respective `IPropertyResolver` instances.
- Delegates to `Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll)`.
- Returns the created quaternion as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("rotation",
    new QuaternionFromPitchYawRollResolver(
        new VariableResolver("pitch", typeof(float)),
        new VariableResolver("yaw", typeof(float)),
        new VariableResolver("roll", typeof(float))));
```

## Composition

```csharp
// Create a rotation from pitch, yaw, and roll and transform a vector
graph.VariableDefinitions.DefineProperty("rotatedDirection",
    new TransformResolver(
        new VariableResolver("direction", typeof(Vector3)),
        new QuaternionFromPitchYawRollResolver(
            new VariableResolver("pitch", typeof(float)),
            new VariableResolver("yaw", typeof(float)),
            new VariableResolver("roll", typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [TransformResolver](transform-resolver.md)
- [QuaternionFromAxisAngleResolver](quaternionfromaxisangle-resolver.md)
