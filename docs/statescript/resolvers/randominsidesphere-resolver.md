# RandomInsideSphereResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RandomInsideSphereResolver`
> **Output Type:** `Vector3`

Returns a random point inside the unit sphere. Use this resolver for volumetric scatter, 3D spawn offsets, and other cases where you need a uniformly sampled position around an origin.

## Constructor

```csharp
new RandomInsideSphereResolver(random)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| random | `IRandom` | The random provider to use. |

## Behavior

- Uses the injected `IRandom` instance each time the resolver is evaluated.
- Returns a `Vector3` whose magnitude is less than or equal to `1`.
- Does not read any values from `GraphContext`.
- Produces a different sample on each resolve unless the supplied `IRandom` implementation is deterministic or seeded.

## Usage

```csharp
IPropertyResolver randomOffset3D = new RandomInsideSphereResolver(myRandomProvider);
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("impactOffset",
    new ScaleResolver(
        new RandomInsideSphereResolver(myRandomProvider),
        new VariableResolver("impactRadius", typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [RandomOnSphereResolver](randomonsphere-resolver.md)
- [RandomInsideCircleResolver](randominsidecircle-resolver.md)
