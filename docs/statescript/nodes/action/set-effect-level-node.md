# SetEffectLevelNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.SetEffectLevelNode`

Levels up one or more `Effect` instances, or sets their level to a resolved value.

Level changes on effects that are already active re-evaluate non-snapshot-level applications through `Effect.OnLevelChanged`.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the change. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the change. |

## Constructor

```csharp
new SetEffectLevelNode(operation = SetEffectLevelOperation.LevelUp)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operation | `SetEffectLevelOperation` | How to change the level. Defaults to `LevelUp`. |

### `SetEffectLevelOperation` values

- `LevelUp`: increases the effect's level by one (`Effect.LevelUp()`).
- `SetLevel`: sets the level to the resolved **Level** input (`Effect.SetLevel(int)`).

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Effect | `Effect` or `Effect[]` | The effect instance(s) to level. |
| 1 | Level | `int` | Only read for `SetLevel`. |

## Behavior

1. Resolves the effect input as a single effect or an array of effects.
2. For `LevelUp`, calls `LevelUp()` on each; for `SetLevel`, resolves the level and calls `SetLevel(level)` on each.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable("stanceBuff", stanceBuffInstance);

// Bump the buff's level by one each time this runs
var levelUp = new SetEffectLevelNode(); // operation: LevelUp (default)
levelUp.BindInput(SetEffectLevelNode.EffectInput, "stanceBuff");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    levelUp.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Action Nodes Overview](README.md)
- [EffectLevelListenerNode](../state/effect-level-listener-node.md)
- [ActiveEffectEffectResolver](../../resolvers/active-effect-effect-resolver.md)
