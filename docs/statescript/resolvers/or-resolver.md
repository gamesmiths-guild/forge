# OrResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.OrResolver`
> **Output Type:** `bool`

Combines two boolean resolvers using logical OR and returns `true` when either operand resolves to `true`.

## Constructor

```csharp
new OrResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the left boolean operand. |
| right | `IPropertyResolver` | The resolver for the right boolean operand. |

## Behavior

- Requires both operands to have `ValueType == typeof(bool)`.
- Resolves both operands on each call.
- Returns `true` when either resolved value is `true`.
- Returns `false` only when both resolved values are `false`.
- Throws `ArgumentException` if either operand does not resolve to `bool`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("shouldRetreat",
    new OrResolver(
        new ComparisonResolver(
            new AttributeResolver("CombatAttributeSet.Health"),
            ComparisonOperation.LessThanOrEqual,
            new VariantResolver(new Variant128(20), typeof(int))),
        new TagResolver(Tag.RequestTag(tagsManager, "status.fleeing"))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("canBypassLock",
    new OrResolver(
        new TagResolver(Tag.RequestTag(tagsManager, "key.master")),
        new AndResolver(
            new TagResolver(Tag.RequestTag(tagsManager, "skill.lockpicking")),
            new ComparisonResolver(
                new VariableResolver("lockpickLevel", typeof(int)),
                ComparisonOperation.GreaterThanOrEqual,
                new VariantResolver(new Variant128(5), typeof(int))))));
```

## See Also

- [Resolvers Overview](README.md)
- [AndResolver](and-resolver.md)
- [XorResolver](xor-resolver.md)
- [ExpressionNode](../nodes/condition/expression-node.md)
