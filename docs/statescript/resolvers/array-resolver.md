# ArrayVariableResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ArrayVariableResolver`
> **Output Type:** *(configured at construction time)*

A mutable property resolver that stores an array of `Variant128` values. This enables graph variables that hold multiple values, such as a list of entity IDs returned by a query node or an array of projectile positions.

## Constructor

```csharp
new ArrayVariableResolver(initialValues, elementType)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| initialValues | `Variant128[]` | The initial values for the array elements. |
| elementType | `Type` | The type of each element in the array (e.g., `typeof(int)`, `typeof(float)`). |

## Behavior

- `Resolve` returns the **first element** of the array, or a default `Variant128` (zero) if the array is empty.
- Supports indexed access via `GetElement(index)` and `SetElement(index, value)`.
- Supports mutation via `Add(value)`, `RemoveAt(index)`, and `Clear()`.
- At graph initialization time, a fresh copy of the array is created for each execution instance so that multiple processors sharing the same graph have independent array state.

## Usage

```csharp
// Define an array variable
var resolver = new ArrayVariableResolver(
    [new Variant128(10), new Variant128(20), new Variant128(30)],
    typeof(int));

// Indexed access
Variant128 second = resolver.GetElement(1); // 20

// Mutation
resolver.SetElement(1, new Variant128(99));
resolver.Add(new Variant128(40));
resolver.RemoveAt(0);
resolver.Clear();
```

## See Also

- [Resolvers Overview](README.md)
- [VariableResolver](variable-resolver.md)
- [VariantResolver](variant-resolver.md)
