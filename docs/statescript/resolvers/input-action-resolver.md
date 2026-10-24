# InputActionResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.InputActionResolver`
> **Output Type:** `bool`

Resolves whether an input action is held, or was pressed or released since the last tick, in an entity's [input](../../input.md). It's the composable half of [InputActionNode](../nodes/state/input-action-node.md): a bool an expression or a condition monitor can gate on, where the node is what a graph waits on.

## Constructor

```csharp
new InputActionResolver(actionName)
new InputActionResolver(actionName, state)
new InputActionResolver(inputSource, actionName)
new InputActionResolver(inputSource, actionName, state)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| inputSource | `IEntityResolver` | Selects the entity whose input is read. Defaults to `AbilityOwnerResolver`. |
| actionName | `string` | The input action to read. |
| state | `InputActionState` | `Held` (the default), `Pressed` or `Released`. |

## Behavior

- Reads the current frame of the entity's input: the frame of the tick being simulated.
- `Held` reads the same on every tick the button is down. `Pressed` and `Released` are true only in the tick of the edge, so read them on the fixed rail.
- Returns `false` when the entity has no input, or its action set doesn't carry the action.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("blocking",
    new InputActionResolver("block"));
```

## See Also

- [Resolvers Overview](README.md#input-resolvers)
- [InputActionNode](../nodes/state/input-action-node.md)
- [Input](../../input.md)
