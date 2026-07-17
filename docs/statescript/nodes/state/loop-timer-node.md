# LoopTimerNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.LoopTimerNode`
> **Context:** `LoopTimerNodeContext`

Emits a repeating interval event while active, optionally deactivating after a configured number of loops.

## Ports

Standard state ports, plus:

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 4 | OnInterval | Event | Emits once per completed interval. |
| 5 | OnFinished | Event | Emits when the configured number of loops completes, just before self-deactivation. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Interval | `double` | The interval length in seconds, re-resolved every update. Non-positive intervals pause the timer. |
| 1 | Loop Count | `int` | The number of loops to run. When unbound or non-positive, the timer loops until the node is deactivated externally. |

## Behavior

1. Accumulates elapsed time each update. When more than one interval elapses in a single update, `OnInterval` is emitted once per completed interval.
2. When the configured number of loops completes, it emits a final `OnInterval` followed by `OnFinished`, then self-deactivates.

## Usage

```csharp
// Tick a damage-over-time cue every 0.5s, 6 times
graph.VariableDefinitions.DefineVariable("interval", 0.5);
graph.VariableDefinitions.DefineVariable("ticks", 6);

var loop = new LoopTimerNode();
loop.BindInput(LoopTimerNode.IntervalInput, "interval");
loop.BindInput(LoopTimerNode.LoopCountInput, "ticks");
```

## See Also

- [State Nodes Overview](README.md)
- [TimerNode](timer-node.md)
