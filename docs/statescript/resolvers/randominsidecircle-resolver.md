# RandomInsideCircleResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RandomInsideCircleResolver`
> **Output Type:** `Vector2`

Returns a random point inside the unit circle. Use this when you need a position offset or sample point with both random direction and random radius in 2D.

## Constructor

```csharp
new RandomInsideCircleResolver(random)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| random | `IRandom` | The random provider to use. |

## Behavior

- Uses the injected `IRandom` instance each time the resolver is evaluated.
- Returns a `Vector2` whose magnitude is less than or equal to `1`, with the unit-circle boundary included by default.
- Samples the angle with an exclusive `[0, 2π)` range so `0` and `2π` are not double-counted as the same direction.
- Does not read any values from `GraphContext`.
- Produces a different sample on each resolve unless the supplied `IRandom` implementation is deterministic or seeded.

## Usage

```csharp
IPropertyResolver randomOffset2D = new RandomInsideCircleResolver(myRandomProvider);
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("spawnOffset2D",
    new ScaleResolver(
        new RandomInsideCircleResolver(myRandomProvider),
        new VariableResolver("spawnRadius", typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [RandomDirectionResolver](randomdirection-resolver.md)
- [RandomInsideSphereResolver](randominsidesphere-resolver.md)
