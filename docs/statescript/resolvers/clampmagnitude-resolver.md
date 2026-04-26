# ClampMagnitudeResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ClampMagnitudeResolver`
> **Output Type:** `Vector2`, `Vector3`, or `Vector4`

Clamps a vector to a maximum magnitude while preserving direction. If the input vector is already shorter than the configured limit, it is returned unchanged.

## Constructor

```csharp
new ClampMagnitudeResolver(value, maxLength)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| value | `IPropertyResolver` | The resolver for the vector to clamp. |
| maxLength | `IPropertyResolver` | The resolver for the maximum allowed length. Must resolve to `float`. |

## Supported Types

| Value Type | Max Length Type | Result Type |
|------------|-----------------|-------------|
| `Vector2` | `float` | `Vector2` |
| `Vector3` | `float` | `Vector3` |
| `Vector4` | `float` | `Vector4` |

## Behavior

- Resolves the input vector and maximum length on each call.
- Preserves the input direction and limits only the magnitude.
- Returns the original vector unchanged when its magnitude is already less than or equal to `maxLength`.
- Requires `maxLength` to be `float` and the input to be `Vector2`, `Vector3`, or `Vector4`.
- Throws `ArgumentException` at construction time for unsupported type combinations.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("clampedVelocity",
    new ClampMagnitudeResolver(
        new VariableResolver("desiredVelocity", typeof(Vector3)),
        new VariantResolver(new Variant128(6.0f), typeof(float))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("nextPositionOffset",
    new MoveTowardsResolver(
        new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
        new ClampMagnitudeResolver(
            new VariableResolver("targetOffset", typeof(Vector3)),
            new VariableResolver("maxRange", typeof(float))),
        new VariableResolver("stepDistance", typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [LengthResolver](length-resolver.md)
- [MoveTowardsResolver](movetowards-resolver.md)
