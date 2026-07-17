# ApproximatelyResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ApproximatelyResolver`
> **Output Type:** `bool`

Returns `true` when two numeric values are equal within a tolerance. Use this instead of an equality comparison for floating-point values, where exact equality is a footgun.

## Constructor

```csharp
new ApproximatelyResolver(a, b, tolerance = 1e-6)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| a | `IPropertyResolver` | The first value. |
| b | `IPropertyResolver` | The second value. |
| tolerance | `double` | The maximum absolute difference considered equal. Must be non-negative. |

## Behavior

- Operands are read as `double`. Returns `Math.Abs(a - b) <= tolerance`.

## Usage

```csharp
// True when a normalized charge is (approximately) full
graph.VariableDefinitions.DefineProperty("chargeFull",
    new ApproximatelyResolver(
        new VariableResolver("chargeRatio"),
        new VariantResolver(new Variant128(1.0), typeof(double))));
```

## Composition

```csharp
// Use as a branch condition in an ExpressionNode
var expression = new ExpressionNode();
graph.VariableDefinitions.DefineProperty("atRest",
    new ApproximatelyResolver(
        new VariableResolver("velocity"),
        new VariantResolver(new Variant128(0.0), typeof(double)),
        tolerance: 0.01));
expression.BindInput(ExpressionNode.ConditionInput, "atRest");
```

## See Also

- [Resolvers Overview](README.md)
- [ComparisonResolver](comparison-resolver.md)
