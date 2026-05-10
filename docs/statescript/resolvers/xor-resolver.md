# XorResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.XorResolver`
> **Output Type:** `bool`

Combines two boolean resolvers using logical exclusive OR and returns `true` when exactly one operand resolves to `true`.

## Constructor

```csharp
new XorResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the left boolean operand. |
| right | `IPropertyResolver` | The resolver for the right boolean operand. |

## Behavior

- Requires both operands to have `ValueType == typeof(bool)`.
- Resolves both operands on each call.
- Returns `true` when exactly one resolved value is `true`.
- Returns `false` when both resolved values are the same.
- Throws `ArgumentException` if either operand does not resolve to `bool`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("exactlyOneTriggerActive",
    new XorResolver(
        new VariableResolver("leftTriggerPressed", typeof(bool)),
        new VariableResolver("rightTriggerPressed", typeof(bool))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("shouldFlipState",
    new XorResolver(
        new ComparisonResolver(
            new VariableResolver("currentPhase", typeof(int)),
            ComparisonOperation.Equal,
            new VariantResolver(new Variant128(1), typeof(int))),
        new TagQueryResolver(Tag.RequestTag(tagsManager, "state.inverted"))));
```

## See Also

- [Resolvers Overview](README.md)
- [AndResolver](and-resolver.md)
- [OrResolver](or-resolver.md)
- [ExpressionNode](../nodes/condition/expression-node.md)
