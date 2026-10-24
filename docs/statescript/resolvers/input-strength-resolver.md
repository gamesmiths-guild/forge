# InputStrengthResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.InputStrengthResolver`
> **Output Type:** `float`

Resolves how far an input action is pressed in an entity's [input](../../input.md), from 0 to 1. A stick pushed part way can scale a speed, for example.

## Constructor

```csharp
new InputStrengthResolver(actionName)
new InputStrengthResolver(inputSource, actionName)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| inputSource | `IEntityResolver` | Selects the entity whose input is read. Defaults to `AbilityOwnerResolver`. |
| actionName | `string` | The input action to read. |

## Behavior

- An action that is one of the four directions of a vector in the input's action set reads that direction of the vector, so it has values in between.
- Any other action is a button: 1 while held and 0 otherwise.
- Returns `0` when the entity has no input, or its action set doesn't carry the action.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("forwardPush",
    new InputStrengthResolver("move_forward"));
```

## See Also

- [Resolvers Overview](README.md#input-resolvers)
- [InputAxisResolver](input-axis-resolver.md)
- [Input](../../input.md)
