# MagnitudeResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.MagnitudeResolver`
> **Output Type:** `float`

Reads the `Magnitude` float from the ability's activation context. This is the numeric value passed during `AbilityHandle.Activate()` or propagated from an event trigger's `EventMagnitude`.

## Constructor

```csharp
new MagnitudeResolver()
```

No parameters required.

## Behavior

- Retrieves the `AbilityBehaviorContext` from `GraphContext.ActivationContext`.
- Returns the `Magnitude` property as a `float`.
- Returns `0f` if no activation context is available.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("magnitude",
    new MagnitudeResolver());

// Use as a timer duration driven by the activation magnitude
timerNode.BindInput(TimerNode.DurationInput, "magnitude");
```

## Composition

```csharp
// Scale the magnitude by a multiplier
graph.VariableDefinitions.DefineProperty("scaledMagnitude",
    new AddResolver(
        new MagnitudeResolver(),
        new VariantResolver(new Variant128(10.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [AttributeResolver](attribute-resolver.md)
- [Ability Integration](../ability-integration.md)
