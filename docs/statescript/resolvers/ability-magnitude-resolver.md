# AbilityMagnitudeResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityMagnitudeResolver`
> **Output Type:** `float`

Reads the current ability activation magnitude from `AbilityBehaviorContext.Magnitude`.

## Constructor

```csharp
new AbilityMagnitudeResolver()
```

## Behavior

- Reads the active `AbilityBehaviorContext` from `GraphContext.ActivationContext`.
- Returns the current activation magnitude as a `float`.
- Returns `0f` when no compatible ability activation context is available.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("magnitude",
    new AbilityMagnitudeResolver());

timerNode.BindInput(TimerNode.DurationInput, "magnitude");
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("scaledMagnitude",
    new AddResolver(
        new AbilityMagnitudeResolver(),
        new VariantResolver(new Variant128(10.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [AbilityActivationDataResolver](ability-activation-data-resolver.md)
- [Ability Integration](../ability-integration.md)
