# InputVectorResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.InputVectorResolver`
> **Output Type:** `Vector2`

Resolves the direction four input actions point in, in an entity's [input](../../input.md), as a vector no longer than 1. It's the movement stick, or a dash steered by the movement keys.

## Constructor

```csharp
new InputVectorResolver(negativeXActionName, positiveXActionName, negativeYActionName, positiveYActionName)
new InputVectorResolver(inputSource, negativeXActionName, positiveXActionName, negativeYActionName, positiveYActionName)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| inputSource | `IEntityResolver` | Selects the entity whose input is read. Defaults to `AbilityOwnerResolver`. |
| negativeXActionName | `string` | The action pushing X towards -1. |
| positiveXActionName | `string` | The action pushing X towards 1. |
| negativeYActionName | `string` | The action pushing Y towards -1. |
| positiveYActionName | `string` | The action pushing Y towards 1. |

## Behavior

- When a vector in the input's action set is made of these four actions, the value is that vector, so a stick stays analog.
- Otherwise the two axes are read as [InputAxisResolver](input-axis-resolver.md) reads them, and a diagonal is shortened to length 1.
- Returns `Vector2.Zero` when the entity has no input.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("move",
    new InputVectorResolver("move_left", "move_right", "move_forward", "move_back"));
```

## See Also

- [Resolvers Overview](README.md#input-resolvers)
- [InputAxisResolver](input-axis-resolver.md)
- [Input](../../input.md)
