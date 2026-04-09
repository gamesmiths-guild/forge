# Statescript Nodes

This page documents the node categories and core structural nodes in Statescript. For an overview of the execution model, see the [Statescript overview](../README.md).

## Node Categories

| Category | Purpose | Behavior |
|----------|---------|----------|
| **Entry** | Starts the graph | Emits a message when the graph starts. One per graph. |
| **Exit** | Stops the graph | Stops all execution when reached. Optional. |
| **[Action](action/README.md)** | Instant operation | Executes immediately and passes the message forward. |
| **[Condition](condition/README.md)** | Branches execution | Evaluates a test and routes the message to True or False. |
| **[State](state/README.md)** | Maintains active state over time | Activates on input, remains active, deactivates based on internal logic. |

---

## Entry Node

The entry point of every graph. Exactly one per graph, created automatically.

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Subgraph | Emits when the graph starts. Carries the subgraph lifetime of the entire graph. |

Because the Entry node's output is a **Subgraph port**, everything downstream is owned by the graph's lifetime. When the graph is stopped externally, the disable-subgraph signal propagates through this port to clean up all active state nodes.

```csharp
var graph = new Graph();

// The EntryNode is always available
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    someNode.InputPorts[ActionNode.InputPort]));
```

---

## Exit Node

Forces the graph to stop when a message reaches it. All active state nodes are disabled, node contexts are removed, and `OnGraphCompleted` fires.

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Receiving a message stops the entire graph. |

A graph may have zero or more Exit nodes. Place them at any point where you want to force an early termination.

```csharp
var exitNode = new ExitNode();
graph.AddNode(exitNode);

// Connect to some event that should end the graph early
graph.AddConnection(new Connection(
    timer.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
    exitNode.InputPorts[ExitNode.InputPort]));
```

---

## Port Types

### EventPort

Carries regular messages. Does **not** propagate disable-subgraph signals. Used by Action node outputs, Condition node outputs, and State node event outputs (OnActivate, OnDeactivate, OnAbort).

### SubgraphPort

Carries **both** regular messages and disable-subgraph signals. Used by the Entry node's output and State node Subgraph outputs. A Subgraph port **owns** the downstream nodes connected to it: when it sends a disable signal, everything downstream is cleaned up. Custom state nodes can define additional Subgraph ports and control each one independently. See [Subgraphs](../subgraphs.md) for details on the lifetime implications.

### InputPort

Receives messages from connected output ports and notifies the owning node.

## Graph Construction

Nodes are added to a graph with `AddNode()` and wired together with `AddConnection()`:

```csharp
var graph = new Graph();

// Define variables
graph.VariableDefinitions.DefineVariable("duration", 3.0);

// Create nodes
var action = new MyActionNode();
var timer = new TimerNode();
timer.BindInput(TimerNode.DurationInput, "duration");

// Add to graph
graph.AddNode(action);
graph.AddNode(timer);

// Wire connections (output port → input port)
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    action.InputPorts[ActionNode.InputPort]));

graph.AddConnection(new Connection(
    action.OutputPorts[ActionNode.OutputPort],
    timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
```

**Note:** Adding a connection that would create a loop in the graph is rejected at construction time with a validation error.

## Fan-Out (One-to-Many)

A single output port can connect to multiple input ports. All connected nodes receive the message:

```csharp
// Entry fires both action nodes simultaneously
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    action1.InputPorts[ActionNode.InputPort]));

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    action2.InputPorts[ActionNode.InputPort]));
```

## Graph Completion

A graph completes when **no state nodes remain active**:

1. **Natural completion**: All state nodes deactivate through their lifecycle.
2. **Forced termination**: An Exit node is reached, or `GraphProcessor.StopGraph()` is called.

> If a graph has no state nodes at all (only actions and conditions), it completes immediately after the initial message propagation during `StartGraph()`.
