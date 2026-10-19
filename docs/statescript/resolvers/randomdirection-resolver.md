# RandomDirectionResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RandomDirectionResolver`
> **Output Type:** `Vector2`

Returns a random normalized 2D direction. This is useful for spread, wandering, directional noise, or choosing a heading without bias toward any axis.

## Constructor

```csharp
new RandomDirectionResolver(random)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| random | `IRandom` | The random provider used when the graph has no `GraphContext.Random`; see [Simulation Tick](../../simulation.md#deterministic-randomness). |

## Behavior

- Draws from `GraphContext.Random` when the graph has one, which an ability graph gets from its owner, and otherwise from the injected `IRandom`, each time the resolver is evaluated.
- Returns a normalized `Vector2` direction.
- Samples the angle in `[0, 2π)`, with an exclusive upper bound, so `2π` is never returned and the seam direction is not double-counted.
- Does not read any values from `GraphContext`.
- Produces a different result on each resolve unless the random it draws from is deterministic or seeded.

## Usage

```csharp
IPropertyResolver randomDirection = new RandomDirectionResolver(myRandomProvider);
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("wanderOffset",
    new ScaleResolver(
        new RandomDirectionResolver(myRandomProvider),
        new VariableResolver("wanderRadius", typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [RandomInsideCircleResolver](randominsidecircle-resolver.md)
- [RandomOnSphereResolver](randomonsphere-resolver.md)
