# RepeatNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.RepeatNode`
> **Context:** `IterationNodeContext`

Emits an iteration event a fixed number of times, all within the activation frame or spaced by an interval, then deactivates.

Together with [ForEachNode](for-each-node.md) this is the graph's bounded loop. Both derive from `IterationNode<T>` and share their port shape, condition, interval and ending semantics; they differ only in what drives the sequence — a count here, an array there.

## Ports

Standard state ports, plus:

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 4 | OnIteration | Event | Emits once per iteration, after the index output has been written. |
| 5 | OnFinished | Event | Emits when the count is reached, just before self-deactivation. |
| 6 | OnConditionFailed | Event | Emits instead of `OnFinished` when the condition cuts the loop short. |

**Every way the loop can end has its own port, and exactly one of them fires:** `OnFinished` (the count was reached), `OnConditionFailed` (the guard stopped holding, possibly before the first iteration), or the standard `OnAbort` (the node was aborted from outside). `OnDeactivate` still fires for all three, so a graph that only cares that the loop is over routes that instead of wiring each ending.

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Count | `int` | The number of iterations to run, re-resolved before every iteration. Unbound or non-positive runs no iterations at all. |
| 1 | Condition | `bool` | Optional guard, evaluated once per iteration right before that iteration is due. The loop ends through `OnConditionFailed` as soon as it does not hold. Unbound means no early exit. |
| 2 | Interval | `double` | Optional spacing between iterations, in seconds. Unbound or non-positive runs the whole loop on the activation frame. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Index | `int` | The zero-based index of the current iteration, written before `OnIteration` fires. |

## Behavior

1. The first iteration always runs on the **activation frame**, right after `OnActivate` and the Subgraph port have fired.
2. With no interval, the remaining iterations follow immediately, one after another, all in that same frame. This is what makes same-frame chains and burst patterns expressible without unrolling the graph.
3. With a positive interval, the iterations after the first are spaced by it — so a count of 3 at `0.2` runs at `0.0`, `0.2` and `0.4`. The synchronous mode is simply the zero-interval limit of this one. (Contrast [LoopTimerNode](loop-timer-node.md), which never fires on the activation frame and is the right node for an endless heartbeat.)
4. When several intervals elapse in a single update, the loop catches up within that update.
5. The loop finishes on the same tick as its final iteration: `OnFinished` fires, then the node deactivates. A condition that stops holding is discovered when the next iteration comes due, which in a paced loop is one interval later — the same way a guarded loop with a delay in its body behaves; it ends through `OnConditionFailed`.
6. Aborting the node emits `OnAbort` and neither ending event. An iteration that stops the graph or aborts the node drops the iterations that would have followed.

**An unbound count is never an endless loop.** Because the whole loop can run within one frame, a count that resolves to nothing runs zero iterations and finishes immediately, rather than spinning.

**The index output cannot steer the loop.** How far the loop has walked lives in its node context and is never read back from the variable, so an iteration that writes the index variable — including rewinding it — changes nothing: the next iteration simply overwrites it.

## Usage

```csharp
// Fire a 5-pellet shotgun blast, all on the same frame, with per-pellet spread driven by the index.
graph.VariableDefinitions.DefineVariable("pellets", 5);
graph.VariableDefinitions.DefineVariable("pelletIndex", 0);

var repeat = new RepeatNode();
repeat.BindInput(RepeatNode.CountInput, "pellets");
repeat.BindOutput(RepeatNode.IndexOutput, "pelletIndex");

graph.AddNode(repeat);
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    repeat.InputPorts[RepeatNode.InputPort]));
```

```csharp
// A 3-hit combo, 0.15s apart, that stops early if the target dies.
graph.VariableDefinitions.DefineVariable("hits", 3);
graph.VariableDefinitions.DefineVariable("interval", 0.15);

var combo = new RepeatNode();
combo.BindInput(RepeatNode.CountInput, "hits");
combo.BindInput(RepeatNode.IntervalInput, "interval");
combo.BindInput(RepeatNode.ConditionInput, "targetAlive");
```

## See Also

- [State Nodes Overview](README.md)
- [ForEachNode](for-each-node.md) — the same loop, driven by an array instead of a count
- [LoopTimerNode](loop-timer-node.md) — the timed heartbeat, including the endless case
