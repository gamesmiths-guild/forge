# NotResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.NotResolver`
> **Output Type:** `bool`

Negates a boolean resolver and returns the logical inverse of its resolved value.

## Constructor

```csharp
new NotResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the boolean operand to negate. |

## Behavior

- Requires the operand to have `ValueType == typeof(bool)`.
- Resolves the operand on each call.
- Returns `true` when the operand resolves to `false`.
- Returns `false` when the operand resolves to `true`.
- Throws `ArgumentException` if the operand does not resolve to `bool`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("isNotStunned",
    new NotResolver(
        new TagQueryResolver(Tag.RequestTag(tagsManager, "status.stunned"))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("canInteract",
    new AndResolver(
        new NotResolver(
            new TagQueryResolver(Tag.RequestTag(tagsManager, "status.disabled"))),
        new ComparisonResolver(
            new VariableResolver("distanceToTarget", typeof(float)),
            ComparisonOperation.LessThanOrEqual,
            new VariantResolver(new Variant128(3.0f), typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [AndResolver](and-resolver.md)
- [OrResolver](or-resolver.md)
- [ExpressionNode](../nodes/condition/expression-node.md)
