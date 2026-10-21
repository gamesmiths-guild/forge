# RandomResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RandomResolver`
> **Output Type:** `int`, `float`, or `double`

Generates a random value within a range defined by two operands, using a provided `IRandom` implementation. This design allows users to inject any random provider — standard, seeded, blue noise, or network-synchronized — making the resolver deterministic and testable. When the graph has a `GraphContext.Random`, as an ability graph does through its owner, that random is used instead; see [Simulation Tick](../../simulation.md#deterministic-randomness). By default, the maximum bound is exclusive, but it can be configured to be inclusive.

## Constructor

```csharp
new RandomResolver(random, min, max, maxInclusive)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| random | `IRandom` | The random provider used when the graph has no `GraphContext.Random`; see [Simulation Tick](../../simulation.md#deterministic-randomness). |
| min | `IPropertyResolver` | The resolver for the inclusive minimum bound. |
| max | `IPropertyResolver` | The resolver for the maximum bound. |
| maxInclusive | `bool` | Whether the maximum bound is inclusive. Defaults to `false`. |

## Type Promotion

| Operand Types | Result Type |
|---------------|-------------|
| Both `int` (or sub-int) | `int` |
| Both `float` | `float` |
| Mixed `int`/`float` | `float` |
| Any `double` | `double` |

**Invalid types** (throw `ArgumentException` at construction time):
- `long`, `uint`, `ulong` (use `int` or `double` instead).
- `decimal` (use `double` instead).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both bound operands through their respective `IPropertyResolver` instances.
- For `int`: defaults to returning a value in `[min, max)` via `IRandom.NextInt(min, max)`; when `maxInclusive` is `true`, returns a value in `[min, max]` via `IRandom.NextIntInclusive(min, max)`.
- For `float`: defaults to returning a value in `[min, max)` via `IRandom.NextSingle()`; when `maxInclusive` is `true`, returns a value in `[min, max]` via `IRandom.NextSingleInclusive()`.
- For `double`: defaults to returning a value in `[min, max)` via `IRandom.NextDouble()`; when `maxInclusive` is `true`, returns a value in `[min, max]` via `IRandom.NextDoubleInclusive()`.
- For `float` and `double`, if `max <= min`, the resolver returns `min` without consuming a random value.
- Values come from `GraphContext.Random` when the graph has one, which an ability graph gets from its owner, and otherwise from the `IRandom` injected at construction.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Random damage in a default exclusive range
graph.VariableDefinitions.DefineProperty("damage",
    new AddResolver(
        new VariableResolver("baseDamage", typeof(float)),
        new RandomResolver(
            myRandomProvider,
            new VariantResolver(new Variant128(0.0f), typeof(float)),
            new VariableResolver("bonusDamageRange", typeof(float)))));
```

## Composition

```csharp
// Random integer for loot table index in a default exclusive range
graph.VariableDefinitions.DefineProperty("lootIndex",
    new RandomResolver(
        myRandomProvider,
        new VariantResolver(new Variant128(0), typeof(int)),
        new VariableResolver("lootTableSize", typeof(int))));
```

```csharp
// Inclusive max when the upper bound should be reachable
graph.VariableDefinitions.DefineProperty("lootIndex",
    new RandomResolver(
        myRandomProvider,
        new VariantResolver(new Variant128(0), typeof(int)),
        new VariableResolver("lootTableSize", typeof(int)),
        maxInclusive: true));
```

## See Also

- [Resolvers Overview](README.md)
- [ClampResolver](clamp-resolver.md)
- [FloorResolver](floor-resolver.md)
