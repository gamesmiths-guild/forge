# EulerAnglesFromQuaternionResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EulerAnglesFromQuaternionResolver`
> **Output Type:** `Vector3`

Extracts Euler angles in radians from a quaternion. The returned `Vector3` stores pitch in `X`, yaw in `Y`, and roll in `Z`.

## Constructors

```csharp
new EulerAnglesFromQuaternionResolver(quaternion)
new EulerAnglesFromQuaternionResolver(quaternion, order)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| quaternion | `IPropertyResolver` | The resolver for the quaternion operand. |
| order | `IPropertyResolver` | Optional resolver for the `EulerOrder` value, encoded as an `int`. |

## Supported Types

| Quaternion Type | Order Type | Result Type |
|-----------------|------------|-------------|
| `Quaternion` | *(none)* | `Vector3` |
| `Quaternion` | `int` | `Vector3` |

## Behavior

- Uses `EulerOrder.XYZ` by default.
- `XYZ` means rotate around X, then Y, then Z.
- Resolves `order` as an `int` and maps it to the `EulerOrder` enum.
- Returns a `Vector3` containing pitch, yaw, and roll in radians.
- Throws `ArgumentOutOfRangeException` during resolution if the provided `order` value is not a defined `EulerOrder`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("angles",
    new EulerAnglesFromQuaternionResolver(
        new VariableResolver("rotation", typeof(Quaternion))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("yawDegrees",
    new RadToDegResolver(
        new VectorComponentResolver(
            new EulerAnglesFromQuaternionResolver(
                new VariableResolver("rotation", typeof(Quaternion))),
            VectorComponent.Y)));
```

## See Also

- [Resolvers Overview](README.md)
- [QuaternionFromEulerAnglesResolver](quaternionfromeulerangles-resolver.md)
- [QuaternionFromYawPitchRollResolver](quaternionfromyawpitchroll-resolver.md)
