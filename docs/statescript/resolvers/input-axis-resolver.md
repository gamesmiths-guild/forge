# InputAxisResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.InputAxisResolver`
> **Output Type:** `float`

Resolves the signed strength of a pair of opposed input actions in an entity's [input](../../input.md), from -1 to 1.

## Constructor

```csharp
new InputAxisResolver(negativeActionName, positiveActionName)
new InputAxisResolver(inputSource, negativeActionName, positiveActionName)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| inputSource | `IEntityResolver` | Selects the entity whose input is read. Defaults to `AbilityOwnerResolver`. |
| negativeActionName | `string` | The action driving the value towards -1. |
| positiveActionName | `string` | The action driving the value towards 1. |

## Behavior

- When a vector in the input's action set has the pair as one of its axes, the value is that axis of the vector.
- Otherwise it's the positive action's [strength](input-strength-resolver.md) minus the negative one's, so holding both reads zero.
- Returns `0` when the entity has no input.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("turn",
    new InputAxisResolver("turn_left", "turn_right"));
```

## See Also

- [Resolvers Overview](README.md#input-resolvers)
- [InputVectorResolver](input-vector-resolver.md)
- [Input](../../input.md)
