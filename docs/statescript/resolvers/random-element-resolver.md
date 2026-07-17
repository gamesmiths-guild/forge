# RandomElementResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RandomElementResolver`
> **Output Type:** *(element type)*
> **Object variant:** `ObjectRandomElementResolver<T>`

Picks a random element from a nested array resolver — the "pick a random target" staple.

## Constructor

```csharp
new RandomElementResolver(source, randomProvider)              // value lane
new ObjectRandomElementResolver<T>(source, randomProvider)     // object lane
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The source array. |
| randomProvider | `IRandom` | The random provider used to pick the element. |

## Behavior

- Picks a uniformly random element. Empty arrays resolve to a default value (`0`) or `null` (object lane).

## Usage

```csharp
// Pick a random enemy in range as the ability target
new ObjectRandomElementResolver<IForgeEntity>(
    new EntityArrayVariableResolver("enemiesInRange"),
    randomProvider);
```

## Composition

```csharp
// Pick a random target from those in range, then read its health
graph.VariableDefinitions.DefineProperty("randomTargetHealth",
    new AttributeResolver(
        "CombatAttributeSet.Health",
        new ObjectRandomElementResolver<IForgeEntity>(
            new EntityArrayVariableResolver("enemiesInRange"),
            randomProvider)));
```

## See Also

- [Resolvers Overview](README.md)
- [ShuffleResolver](shuffle-resolver.md)
- [RandomResolver](random-resolver.md)
