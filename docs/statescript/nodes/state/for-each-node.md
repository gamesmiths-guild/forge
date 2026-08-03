# ForEachNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.ForEachNode`
> **Context:** `ForEachNodeContext`

Iterates an array, publishing each element to a variable, all within the activation frame or spaced by an interval, then deactivates.

Together with [RepeatNode](repeat-node.md) this is the graph's bounded loop. Both derive from `IterationNode<T>` and share their port shape, condition, interval and ending semantics; they differ only in what drives the sequence — an array here, a count there.

> **Prefer array inputs where they exist.** Most nodes already take arrays directly — `ApplyEffectNode` applies its effects to every target, `RaiseEventNode` raises on every entity — and the [array resolvers](../../resolvers/README.md) filter, sort and project without a loop. Reach for `ForEachNode` when the work genuinely differs per element: a magnitude that depends on the element, a per-element delay, or a chain that walks its targets one at a time.

## Ports

Standard state ports, plus:

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 4 | OnIteration | Event | Emits once per element, after the element and index outputs have been written. |
| 5 | OnFinished | Event | Emits when the array runs out, just before self-deactivation. |
| 6 | OnConditionFailed | Event | Emits instead of `OnFinished` when the condition cuts the loop short. |

**Every way the loop can end has its own port, and exactly one of them fires:** `OnFinished` (the array ran out), `OnConditionFailed` (the guard stopped holding, possibly before the first element), or the standard `OnAbort` (the node was aborted from outside). `OnDeactivate` still fires for all three, so a graph that only cares that the loop is over can route that instead of wiring each ending.

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Array | `object[]` | The array to walk. Any array works — value-typed or object-backed — and the bound element variable decides how it is read. |
| 1 | Condition | `bool` | Optional guard, evaluated once per iteration right before that iteration is due. The loop ends through `OnConditionFailed` as soon as it does not hold. Unbound means no early exit. |
| 2 | Interval | `double` | Optional spacing between iterations, in seconds. Unbound or non-positive walks the whole array on the activation frame. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Element | `object` | The current element, written before `OnIteration` fires. Its bound variable also types the read (see below). May be left unbound. |
| 1 | Index | `int` | The zero-based index of the current element, written before `OnIteration` fires. |

## The element variable types the read

Statescript keeps value-typed and object-backed data in separate lanes, and nothing converts between them. `ForEachNode` therefore takes its element type from the **variable bound to the Element output**, exactly like [SetVariableNode](../action/set-variable-node.md) takes its type from its target:

| Bound element variable | Source read as | Notes |
|------------------------|----------------|-------|
| Object-backed (`IForgeEntity`, `Effect`, `ActiveEffectHandle`, `Tag`, …) | An array of that same declared type | A source of any other element type resolves nothing, so the loop runs zero iterations rather than writing a mismatched element. |
| Value-typed (`int`, `double`, `bool`, …) | A value array | An object-backed source is still walked, but only its index is published. |
| Unbound | Either lane, value first | The array is walked for its length and index alone. |

So: to iterate entities, declare an `IForgeEntity` graph variable and bind it to Element. Binding the wrong type is not an error — it simply yields an empty loop, which is the failure you want to see in the editor rather than a wrong-typed write at runtime.

## Behavior

1. The array is **snapshotted on activation**. An iteration that reassigns the source variable does not change the sequence being walked, and a loop spread over several frames keeps iterating the array it started with.
2. The first iteration always runs on the **activation frame**, right after `OnActivate` and the Subgraph port have fired.
3. With no interval, the rest of the array follows immediately, all in that same frame.
4. With a positive interval, the elements after the first are spaced by it — so a 3-element array at `0.2` runs at `0.0`, `0.2` and `0.4`. When several intervals elapse in a single update, the loop catches up within that update.
5. The loop finishes on the same tick as its final element: `OnFinished` fires, then the node deactivates. An empty or unresolvable source finishes immediately, having emitted nothing.
6. Aborting the node emits `OnAbort` and neither ending event. An iteration that stops the graph or aborts the node drops the elements that would have followed.

**Neither output can steer the loop.** Position lives in the node context and the elements come from the activation snapshot, so an iteration that writes the element or index variable — including rewinding the index — changes nothing: the next iteration simply overwrites both.

## Usage

```csharp
// Chain lightning: hop to the next target every 0.2s, applying a bolt to each.
graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>("chainTargets");
graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("currentTarget");
graph.VariableDefinitions.DefineVariable("hopDelay", 0.2);

var forEach = new ForEachNode();
forEach.BindInput(ForEachNode.ArrayInput, "chainTargets");
forEach.BindInput(ForEachNode.IntervalInput, "hopDelay");
forEach.BindOutput(ForEachNode.ElementOutput, "currentTarget");

graph.AddNode(forEach);
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    forEach.InputPorts[ForEachNode.InputPort]));

// Downstream of OnIteration, an ApplyEffectNode bound to "currentTarget" hits one target per hop, and a
// SetByCallerMagnitudeNode driven by the index can fall off with each jump.
var bolt = new ApplyEffectNode();
bolt.BindInput(ApplyEffectNode.TargetInput, "currentTarget");
graph.AddNode(bolt);
graph.AddConnection(new Connection(
    forEach.OutputPorts[ForEachNode.OnIterationPort],
    bolt.InputPorts[ActionNode.InputPort]));
```

## See Also

- [State Nodes Overview](README.md)
- [RepeatNode](repeat-node.md) — the same loop, driven by a count instead of an array
- [SetVariableNode](../action/set-variable-node.md) — the same "the bound variable types the read" rule
- [Array Resolvers](../../resolvers/README.md) — filtering, sorting and projecting arrays without a loop
