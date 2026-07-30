# AbilityActivationDataResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityActivationDataResolver`
> **Output Type:** *(configured from the selected activation-data member type)*

Reads a public field or property directly from `AbilityBehaviorContext<TData>.Data`.

This is the read end of the channel [AbilityActivatorResolver](ability-activator-resolver.md) sends on. In a graph editor the two are driven by the same `IAbilityActivationDataProvider`: its `Members` are the fields this resolver offers, and they are the very same declarations the sending side authors — so one provider covers the round trip. Constructed directly in C#, this resolver needs no provider at all — just the type and member name.

## Constructor

```csharp
new AbilityActivationDataResolver(activationDataType, memberName)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| activationDataType | `Type` | The activation-data type carried by `GraphAbilityBehavior<TData>`. |
| memberName | `string` | The public field or readable public property name to read from the activation data. |

## Behavior

- Reads `GraphContext.ActivationContext`.
- Requires that activation context to be an `AbilityBehaviorContext<TData>` whose `TData` matches the configured `activationDataType`.
- Reads the configured member directly from `context.Data` and converts it to `Variant128`.
- Returns a default `Variant128` when no compatible activation context is available.
- Throws during construction when the selected member does not exist, is unreadable, or is not supported by `Variant128`.

## Usage

```csharp
public record struct DashData(float Distance, float Speed);

graph.VariableDefinitions.DefineProperty("distance",
    new AbilityActivationDataResolver(typeof(DashData), nameof(DashData.Distance)));

graph.VariableDefinitions.DefineProperty("speed",
    new AbilityActivationDataResolver(typeof(DashData), nameof(DashData.Speed)));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("scaledDistance",
    new MultiplyResolver(
        new AbilityActivationDataResolver(typeof(DashData), nameof(DashData.Distance)),
        new VariantResolver(new Variant128(2.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [AbilityActivatorResolver](ability-activator-resolver.md) — the other end: builds the activation data a graph sends, from the same provider
- [AbilityMagnitudeResolver](ability-magnitude-resolver.md)
- [Ability Integration](../ability-integration.md)
- [Custom Resolvers](../custom-resolvers.md)
