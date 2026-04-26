# LookAtResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.LookAtResolver`
> **Output Type:** `Quaternion`

Creates a quaternion that rotates from one position to face a target position using an up vector. This is useful for aiming, turret orientation, billboard alignment, and any case where a rotation must be derived from positions rather than authored directly.

## Constructor

```csharp
new LookAtResolver(from, to, up)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| from | `IPropertyResolver` | The resolver for the source position. |
| to | `IPropertyResolver` | The resolver for the target position. |
| up | `IPropertyResolver` | The resolver for the up vector. |

## Supported Types

| From Type | To Type | Up Type | Result Type |
|-----------|---------|---------|-------------|
| `Vector3` | `Vector3` | `Vector3` | `Quaternion` |

## Behavior

- Resolves all three operands through their respective `IPropertyResolver` instances.
- Builds a look rotation from the source position, target position, and up vector.
- Returns the quaternion as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("aimRotation",
    new LookAtResolver(
        new VariableResolver("selfPosition", typeof(Vector3)),
        new VariableResolver("targetPosition", typeof(Vector3)),
        new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("remainingAimError",
    new AngleResolver(
        new VariableResolver("currentRotation", typeof(Quaternion)),
        new LookAtResolver(
            new VariableResolver("selfPosition", typeof(Vector3)),
            new VariableResolver("targetPosition", typeof(Vector3)),
            new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)))));
```

## See Also

- [Resolvers Overview](README.md)
- [QuaternionFromYawPitchRollResolver](quaternionfromyawpitchroll-resolver.md)
- [TransformResolver](transform-resolver.md)
