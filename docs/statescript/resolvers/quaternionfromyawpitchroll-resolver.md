# QuaternionFromYawPitchRollResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.QuaternionFromYawPitchRollResolver`
> **Output Type:** `Quaternion`

Creates a quaternion from yaw, pitch, and roll angles using `Quaternion.CreateFromYawPitchRoll`.

## Constructor

```csharp
new QuaternionFromYawPitchRollResolver(yaw, pitch, roll)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| yaw | `IPropertyResolver` | The resolver for the yaw angle in radians. |
| pitch | `IPropertyResolver` | The resolver for the pitch angle in radians. |
| roll | `IPropertyResolver` | The resolver for the roll angle in radians. |

## Supported Types

| Yaw Type | Pitch Type | Roll Type | Result Type |
|----------|------------|-----------|-------------|
| `float` | `float` | `float` | `Quaternion` |

## Behavior

- Resolves all three operands through their respective `IPropertyResolver` instances.
- Delegates directly to `Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll)`.
- Uses the fixed `YXZ` order from `System.Numerics`.
- Returns the created quaternion as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("rotation",
    new QuaternionFromYawPitchRollResolver(
        new VariableResolver("yaw", typeof(float)),
        new VariableResolver("pitch", typeof(float)),
        new VariableResolver("roll", typeof(float))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("forward",
    new TransformResolver(
        new VariantResolver(new Variant128(Vector3.UnitZ), typeof(Vector3)),
        new QuaternionFromYawPitchRollResolver(
            new VariableResolver("yaw", typeof(float)),
            new VariableResolver("pitch", typeof(float)),
            new VariableResolver("roll", typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [QuaternionFromEulerAnglesResolver](quaternionfromeulerangles-resolver.md)
- [TransformResolver](transform-resolver.md)
- [QuaternionFromAxisAngleResolver](quaternionfromaxisangle-resolver.md)
