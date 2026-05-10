# ActivationDataResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ActivationDataResolver`
> **Output Type:** *(configured from activation-data member type)*

Reads a public field or property directly from the typed activation data stored in `AbilityBehaviorContext<TData>.Data`.

## Constructor

```csharp
new ActivationDataResolver(activationDataType, memberName)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| activationDataType | `Type` | The activation-data type used with `GraphAbilityBehavior<TData>`. |
| memberName | `string` | The public field or property name to read from the activation data. |

## Behavior

- Retrieves the current activation context from `GraphContext.ActivationContext`.
- Requires that activation context to be an `AbilityBehaviorContext<TData>` whose `TData` matches the configured `activationDataType`.
- Reads the configured public field or readable public property from `context.Data`.
- Returns the member as a `Variant128`.
- Returns a default `Variant128` if no activation context is available or if the current activation-data type does not match.
- Throws during construction if the member does not exist or its type is not supported by `Variant128`.

## Supported Member Types

The member type must be directly supported by `Variant128`, such as:

- `bool`
- integer types (`byte`, `short`, `int`, `long`, etc.)
- `float`, `double`, `decimal`
- `Vector2`, `Vector3`, `Vector4`
- `Plane`
- `Quaternion`

For engine-specific or custom types, keep using a graph-variable data binder or write a custom resolver that performs the necessary conversion.

## Usage

```csharp
public record struct DashData(float Distance, float Speed);

var graph = new Graph();

graph.VariableDefinitions.DefineProperty("distance",
    new ActivationDataResolver(typeof(DashData), nameof(DashData.Distance)));

graph.VariableDefinitions.DefineProperty("speed",
    new ActivationDataResolver(typeof(DashData), nameof(DashData.Speed)));

var behavior = new GraphAbilityBehavior<DashData>(graph);
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("scaledDistance",
    new MultiplyResolver(
        new ActivationDataResolver(typeof(DashData), nameof(DashData.Distance)),
        new VariantResolver(new Variant128(2.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [MagnitudeResolver](magnitude-resolver.md)
- [Ability Integration](../ability-integration.md)
