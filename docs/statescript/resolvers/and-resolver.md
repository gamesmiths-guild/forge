# AndResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AndResolver`
> **Output Type:** `bool`

Combines two boolean resolvers using logical AND and returns `true` only when both operands resolve to `true`.

## Constructor

```csharp
new AndResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the left boolean operand. |
| right | `IPropertyResolver` | The resolver for the right boolean operand. |

## Behavior

- Requires both operands to have `ValueType == typeof(bool)`.
- Resolves both operands on each call.
- Returns `true` only when both resolved values are `true`.
- Throws `ArgumentException` if either operand does not resolve to `bool`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("canAttack",
    new AndResolver(
        new TagResolver(Tag.RequestTag(tagsManager, "state.ready")),
        new ComparisonResolver(
            new AttributeResolver("CombatAttributeSet.Stamina"),
            ComparisonOperation.GreaterThan,
            new VariantResolver(new Variant128(0), typeof(int)))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("shouldUseHeavyAttack",
    new AndResolver(
        new ComparisonResolver(
            new VariableResolver("comboCount", typeof(int)),
            ComparisonOperation.GreaterThanOrEqual,
            new VariantResolver(new Variant128(3), typeof(int))),
        new NotResolver(
            new TagResolver(Tag.RequestTag(tagsManager, "status.silenced")))));
```

## See Also

- [Resolvers Overview](README.md)
- [OrResolver](or-resolver.md)
- [NotResolver](not-resolver.md)
- [ExpressionNode](../nodes/condition/expression-node.md)
