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
| random | `IRandom` | The random provider to use. |

## Behavior

- Uses the injected `IRandom` instance each time the resolver is evaluated.
- Returns a normalized `Vector3` direction on the unit sphere surface.
- Does not read any values from `GraphContext`.
- Produces a different result on each resolve unless the supplied `IRandom` implementation is deterministic or seeded.

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
