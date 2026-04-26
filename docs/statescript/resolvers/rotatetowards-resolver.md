# RotateTowardsResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RotateTowardsResolver`
> **Output Type:** `Quaternion`

Rotates one quaternion toward another by a maximum angular delta in radians. Use this resolver for bounded rotational movement where you need to approach a target orientation over time without overshooting.

## Constructor

```csharp
new RotateTowardsResolver(current, target, maxRadiansDelta)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| current | `IPropertyResolver` | The resolver for the current quaternion. |
| target | `IPropertyResolver` | The resolver for the target quaternion. |
| maxRadiansDelta | `IPropertyResolver` | The resolver for the maximum angular delta in radians. |

## Supported Types

| Current Type | Target Type | Max Delta Type | Result Type |
|--------------|-------------|----------------|-------------|
| `Quaternion` | `Quaternion` | `float` | `Quaternion` |

## Behavior

- Resolves the current quaternion, target quaternion, and maximum angular delta on each call.
- Requires `current` and `target` to resolve to `Quaternion`.
- Requires `maxRadiansDelta` to resolve to `float`.
- Returns a quaternion moved toward the target by at most the configured radians delta.
- Throws `ArgumentException` at construction time for unsupported operand types.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("nextRotation",
    new RotateTowardsResolver(
        new VariableResolver("currentRotation", typeof(Quaternion)),
        new VariableResolver("targetRotation", typeof(Quaternion)),
        new VariableResolver("maxTurnRadians", typeof(float))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("trackingRotation",
    new RotateTowardsResolver(
        new VariableResolver("currentRotation", typeof(Quaternion)),
        new LookAtResolver(
            new VariableResolver("selfPosition", typeof(Vector3)),
            new VariableResolver("targetPosition", typeof(Vector3)),
            new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3))),
        new VariableResolver("turnSpeedRadians", typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [SlerpResolver](slerp-resolver.md)
- [LookAtResolver](lookat-resolver.md)
