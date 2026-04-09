# TimerNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.TimerNode`
> **Context:** `TimerNodeContext`

Remains active for a configured duration, then deactivates. The duration is read from a bound input property, so it can be a fixed variable value, driven by an entity attribute, or any other property resolver that produces a `double`.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Activates the state node. |
| 1 | Abort | Forcefully deactivates and fires OnAbort. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | OnActivate | Event | Emits when the node activates. |
| 1 | OnDeactivate | Event | Emits when the node deactivates (any reason). |
| 2 | OnAbort | Event | Emits only when aborted via the Abort port. |
| 3 | Subgraph | Subgraph | Active while the timer is running; sends disable signal on deactivation. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Duration | `double` | Seconds to remain active. |

## Behavior

1. **Activation:** The elapsed time counter is reset to 0.
2. **Update:** Each frame, the elapsed time is incremented by `deltaTime`. When the elapsed time reaches or exceeds the bound duration, the node deactivates.
3. **Deactivation:** Standard state node deactivation, `OnDeactivate` port emits and the subgraph receives a disable signal.

## Usage

```csharp
graph.VariableDefinitions.DefineVariable("duration", 2.0);

var timer = new TimerNode();
timer.BindInput(TimerNode.DurationInput, "duration");
graph.AddNode(timer);

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
```

## See Also

- [State Nodes Overview](README.md)
