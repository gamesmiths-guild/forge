# MoveTowardsResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.MoveTowardsResolver`
> **Output Type:** `float`, `Vector2`, `Vector3`, or `Vector4`

Moves a scalar or vector toward a target by a maximum delta. Use this resolver for stepwise movement where you want to approach a target without overshooting it.

## Constructor

```csharp
new MoveTowardsResolver(current, target, maxDelta)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| current | `IPropertyResolver` | The resolver for the current value. |
| target | `IPropertyResolver` | The resolver for the target value. |
| maxDelta | `IPropertyResolver` | The resolver for the maximum step size. |

## Supported Types

| Current Type | Target Type | Max Delta Type | Result Type |
|--------------|-------------|----------------|-------------|
| `float` | `float` | `float` | `float` |
| `Vector2` | `Vector2` | `float` | `Vector2` |
| `Vector3` | `Vector3` | `float` | `Vector3` |
| `Vector4` | `Vector4` | `float` | `Vector4` |

## Behavior

- Resolves the current value, target value, and maximum delta on each call.
- Requires `current` and `target` to have matching types.
- Requires `maxDelta` to resolve to `float`.
- Moves the current value toward the target by at most `maxDelta` for that resolve.
- Returns the target directly when the remaining distance is less than or equal to `maxDelta`, preventing overshoot.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("nextPosition",
    new MoveTowardsResolver(
        new VariableResolver("currentPosition", typeof(Vector3)),
        new VariableResolver("targetPosition", typeof(Vector3)),
        new VariableResolver("speedPerTick", typeof(float))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("smoothedInput",
    new MoveTowardsResolver(
        new VariableResolver("currentInput", typeof(Vector2)),
        new ClampMagnitudeResolver(
            new VariableResolver("desiredInput", typeof(Vector2)),
            new VariantResolver(new Variant128(1.0f), typeof(float))),
        new VariableResolver("inputAcceleration", typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [LerpResolver](lerp-resolver.md)
- [ClampMagnitudeResolver](clampmagnitude-resolver.md)
