# ArrayResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ArrayResolver`
> **Output Type:** *(configured array element type)*

Builds an array by evaluating a nested resolver for each element in order.

## Constructors

```csharp
new ArrayResolver(elementResolvers)
new ArrayResolver(elementType, elementResolvers)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| elementType | `Type` | Optional explicit type for the array elements. Use this when creating an empty array or when you want the constructor to validate against a specific element type. |
| elementResolvers | `IPropertyResolver[]` | The nested resolvers that produce each array element. |

## Behavior

- Resolves each nested resolver in order and returns the results as `Variant128[]`.
- Infers `ElementType` from the first nested resolver when no explicit `elementType` is provided.
- The explicit `ArrayResolver(Type, ...)` overload can create an empty array because the element type is supplied up front.
- The inferred `ArrayResolver(...)` overload requires at least one nested resolver so the element type can be determined.
- Requires every nested resolver to produce the same element type.
- Useful when each element should come from its own resolver rather than a variable or inline constant array.

## Usage

```csharp
graph.VariableDefinitions.DefineArrayProperty("constants",
    new ArrayResolver(
        new PiResolver(),
        new EResolver()));

graph.VariableDefinitions.DefineArrayProperty("floatConstants",
    new ArrayResolver(
        typeof(float),
        new PiResolver(typeof(float)),
        new EResolver(typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [ArrayVariableResolver](array-variable-resolver.md)
- [EntityArrayResolver](entity-array-resolver.md)
- [VariantResolver](variant-resolver.md)
- [Custom Resolvers](../custom-resolvers.md)
