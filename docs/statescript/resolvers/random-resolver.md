# RandomResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RandomResolver`
> **Output Type:** `int`, `float`, or `double`

Generates a random value within a range defined by two operands, using a provided `IRandom` implementation. This design allows users to inject any random provider — standard, seeded, blue noise, or network-synchronized — making the resolver deterministic and testable. By default, the maximum bound is inclusive, but it can be configured to be exclusive.

## Constructor

```csharp
new RandomResolver(random, min, max, maxInclusive)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| random | `IRandom` | The random provider to use for generating values. |
| min | `IPropertyResolver` | The resolver for the inclusive minimum bound. |
| max | `IPropertyResolver` | The resolver for the maximum bound. |
| maxInclusive | `bool` | Whether the maximum bound is inclusive. Defaults to `true`. |

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
- For `int`: defaults to returning a value in `[min, max]` via `IRandom.NextIntInclusive(min, max)`; when `maxInclusive` is `false`, returns a value in `[min, max)` via `IRandom.NextInt(min, max)`.
- For `float`: defaults to returning a value in `[min, max]` via `IRandom.NextSingleInclusive()`; when `maxInclusive` is `false`, returns a value in `[min, max)` via `IRandom.NextSingle()`.
- For `double`: defaults to returning a value in `[min, max]` via `IRandom.NextDoubleInclusive()`; when `maxInclusive` is `false`, returns a value in `[min, max)` via `IRandom.NextDouble()`.
- The `IRandom` instance is injected at construction time, not resolved from `GraphContext`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Random damage in a range
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
// Random integer for loot table index
graph.VariableDefinitions.DefineProperty("lootIndex",
    new RandomResolver(
        myRandomProvider,
        new VariantResolver(new Variant128(0), typeof(int)),
        new VariableResolver("lootTableSize", typeof(int))));
```

```csharp
// Exclusive max when using a size/count upper bound
graph.VariableDefinitions.DefineProperty("lootIndex",
    new RandomResolver(
        myRandomProvider,
        new VariantResolver(new Variant128(0), typeof(int)),
        new VariableResolver("lootTableSize", typeof(int)),
        maxInclusive: false));
```

## See Also

- [Resolvers Overview](README.md)
- [ClampResolver](clamp-resolver.md)
- [FloorResolver](floor-resolver.md)
