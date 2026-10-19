# RandomOnSphereResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RandomOnSphereResolver`
> **Output Type:** `Vector3`

Returns a random normalized 3D direction on the unit sphere. This is useful for unbiased directional sampling in 3D, such as projectile spread, steering jitter, or environment probes.

## Constructor

```csharp
new RandomOnSphereResolver(random)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| random | `IRandom` | The random provider used when the graph has no `GraphContext.Random`; see [Simulation Tick](../../simulation.md#deterministic-randomness). |

## Behavior

- Draws from `GraphContext.Random` when the graph has one, which an ability graph gets from its owner, and otherwise from the injected `IRandom`, each time the resolver is evaluated.
- Returns a normalized `Vector3` direction on the unit sphere surface, with the poles included by default.
- Samples the seam angle in `[0, 2π)` so `0` and `2π` are not counted twice as the same direction.
- Does not read any values from `GraphContext`.
- Produces a different result on each resolve unless the random it draws from is deterministic or seeded.

## Usage

```csharp
IPropertyResolver randomDirection3D = new RandomOnSphereResolver(myRandomProvider);
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("probeTarget",
    new AddResolver(
        new VariableResolver("probeOrigin", typeof(Vector3)),
        new ScaleResolver(
            new RandomOnSphereResolver(myRandomProvider),
            new VariableResolver("probeDistance", typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [RandomInsideSphereResolver](randominsidesphere-resolver.md)
- [RandomDirectionResolver](randomdirection-resolver.md)
