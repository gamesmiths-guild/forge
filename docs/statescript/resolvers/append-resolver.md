# AppendResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AppendResolver` (value arrays), `ObjectAppendResolver<T>` (reference arrays)
> **Output Type:** *(array of the source's element type)*

Appends additional elements to the end of a nested array resolver. Each appended element is produced by its own nested resolver, allowing constants, variables, or computed values to be added.

## Constructors

```csharp
new AppendResolver(source, params elements)              // Variant128 arrays
new ObjectAppendResolver<T>(source, params elements)     // reference arrays
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |
| elements | `IPropertyResolver[]` / `IObjectResolver<T>[]` | The nested resolvers producing the elements to append. Value-lane elements must resolve to the source element type. |

## Behavior

- Returns the source elements followed by one element per appended resolver, evaluated in order.
- Throws `ArgumentException` at construction for null element resolvers, or (value lane) for element resolvers whose value type does not match the source element type.

## Usage

```csharp
new AppendResolver(
    new ArrayVariableResolver("damageRolls", typeof(int)),
    new VariantResolver(new Variant128(10), typeof(int)))
```

## Composition

```csharp
// Add the current ability target to a stored target list
new ObjectAppendResolver<IForgeEntity>(
    new EntityArrayVariableResolver("targets"),
    new AbilityTargetResolver());
```

## See Also

- [Resolvers Overview](README.md)
- [ConcatResolver](concat-resolver.md)
- [RemoveAtResolver](remove-at-resolver.md)
