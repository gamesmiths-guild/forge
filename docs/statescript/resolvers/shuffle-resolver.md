# ShuffleResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ShuffleResolver`
> **Output Type:** *(element array)*
> **Object variant:** `ObjectShuffleResolver<T>`

Produces a random permutation of a nested array resolver using a Fisher-Yates shuffle. Combine with a [Take](take-resolver.md) resolver to pick N random elements without repetition.

## Constructor

```csharp
new ShuffleResolver(source, randomProvider)             // value lane
new ObjectShuffleResolver<T>(source, randomProvider)    // object lane
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The source array. |
| randomProvider | `IRandom` | The random provider used to shuffle. |

## Behavior

- Returns a new array that is a uniformly random permutation of the source (the source is not mutated).

## Usage

```csharp
// A shuffled copy of the enemies array
graph.VariableDefinitions.DefineObjectArrayProperty("shuffledEnemies",
    new ObjectShuffleResolver<IForgeEntity>(
        new EntityArrayVariableResolver("enemies"),
        randomProvider));
```

## Composition

```csharp
// Pick N random targets without repetition: shuffle then take
graph.VariableDefinitions.DefineObjectArrayProperty("threeRandomEnemies",
    new ObjectTakeResolver<IForgeEntity>(
        new ObjectShuffleResolver<IForgeEntity>(
            new EntityArrayVariableResolver("enemies"),
            randomProvider),
        new VariantResolver(new Variant128(3), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [RandomElementResolver](random-element-resolver.md)
- [TakeResolver](take-resolver.md)
