# QuaternionFromEulerAnglesResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.QuaternionFromEulerAnglesResolver`
> **Output Type:** `Quaternion`

Creates a quaternion from Euler angles stored in a `Vector3`. The input vector stores pitch in `X`, yaw in `Y`, and roll in `Z`.

## Constructors

```csharp
new QuaternionFromEulerAnglesResolver(eulerAngles)
new QuaternionFromEulerAnglesResolver(eulerAngles, order)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| eulerAngles | `IPropertyResolver` | The resolver for the Euler angles in radians, stored as pitch, yaw, and roll. |
| order | `IPropertyResolver` | Optional resolver for the `EulerOrder` value, encoded as an `int`. |

## Supported Types

| Euler Angles Type | Order Type | Result Type |
|-------------------|------------|-------------|
| `Vector3` | *(none)* | `Quaternion` |
| `Vector3` | `int` | `Quaternion` |

## Behavior

- Uses `EulerOrder.XYZ` by default.
- `XYZ` means rotate around X, then Y, then Z.
- This default is intentionally different from `QuaternionFromYawPitchRollResolver`, which uses fixed `YXZ` order through `System.Numerics`.
- Resolves `order` as an `int` and maps it to the `EulerOrder` enum.
- Returns the created quaternion as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("rotation",
    new QuaternionFromEulerAnglesResolver(
        new VariableResolver("angles", typeof(Vector3))));
```

## See Also

- [Resolvers Overview](README.md)
- [EulerAnglesFromQuaternionResolver](euleranglesfromquaternion-resolver.md)
- [QuaternionFromYawPitchRollResolver](quaternionfromyawpitchroll-resolver.md)
