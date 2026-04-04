# Statescript Nodes

This page documents all node types in Statescript. For an overview of the execution model, see the [Statescript overview](README.md).

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

## Action Nodes

Action nodes perform an **instant operation** then pass the message forward. They are the workhorses of imperative logic in Statescript.

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the action. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after execution. |

**Behavior:**

1. A message arrives on the input port.
2. The node's `Execute` method runs.
3. The output port emits a message.

Action nodes are stateless and instantaneous. They do not persist between frames.

### Creating Custom Action Nodes

Extend `ActionNode` and override `Execute`:

```csharp
public class ApplyEffectActionNode : ActionNode
{
    private readonly EffectData _effectData;

    public ApplyEffectActionNode(EffectData effectData)
    {
        _effectData = effectData;
    }

    protected override void Execute(GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            return;
        }

        var effect = new Effect(_effectData, new EffectOwnership(context.Owner, context.Source));
        context.Owner.EffectsManager.ApplyEffect(effect);
    }
}
```

### Built-in Action Nodes

#### SetVariableNode

Reads a value from an input property and writes it to a graph or shared variable. This is the primary way to copy data between variables or write computed values.

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Source | `Variant128` | The value to read. |

**Output Variables:**

| Index | Label | Scope | Description |
|-------|-------|-------|-------------|
| 0 | Target | Graph or Shared | The variable to write to. |

```csharp
var setVar = new SetVariableNode();
setVar.BindInput(SetVariableNode.SourceInput, "sourceProperty");
setVar.BindOutput(SetVariableNode.TargetOutput, "targetVariable");

// Write to a shared variable instead
setVar.BindOutput(SetVariableNode.TargetOutput, "comboCounter", VariableScope.Shared);
```

---

## Condition Nodes

Condition nodes evaluate a boolean test and route the message to one of two outputs.

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers evaluation. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits if test is `true`. |
| 1 | False | Event | Emits if test is `false`. |

**Behavior:**

1. A message arrives on the input port.
2. The node's `Test` method evaluates.
3. Either the True or False port emits a message (never both).

### Creating Custom Condition Nodes

Extend `ConditionNode` and override `Test`:

```csharp
public class HasTargetConditionNode : ConditionNode
{
    protected override bool Test(GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            return false;
        }

        return context.Target is not null;
    }
}
```

### Built-in Condition Nodes

#### ExpressionNode

Evaluates a boolean input property to choose the output. This eliminates the need to create custom `ConditionNode` subclasses for data-driven conditions. Instead, compose an expression from [property resolvers](variables.md#property-resolvers) at graph construction time.

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Condition | `bool` | The expression to evaluate. |

The condition can be bound to any resolver producing a `bool`: a `TagResolver`, `ComparisonResolver`, `VariableResolver` (reading a bool variable), or a nested chain.

```csharp
// Define a comparison property
graph.VariableDefinitions.DefineProperty("healthAboveHalf",
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Health"),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(50), typeof(int))));

// Create and bind the expression node
var expression = new ExpressionNode();
expression.BindInput(ExpressionNode.ConditionInput, "healthAboveHalf");

graph.AddNode(expression);

// Wire it up
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    expression.InputPorts[ConditionNode.InputPort]));
graph.AddConnection(new Connection(
    expression.OutputPorts[ConditionNode.TruePort],
    strongEffectNode.InputPorts[ActionNode.InputPort]));
graph.AddConnection(new Connection(
    expression.OutputPorts[ConditionNode.FalsePort],
    weakEffectNode.InputPorts[ActionNode.InputPort]));
```

---

## State Nodes

State nodes **persist over time**. They activate when receiving a message, remain active across frames, and deactivate based on internal logic. State nodes are what give Statescript its "state-based" nature and they represent ongoing conditions that own [subgraphs](subgraphs.md).

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
| 3 | Subgraph | Subgraph | Emits on activate; sends disable-subgraph signal on node deactivation. |
| 4+ | Custom | Event or Subgraph | Additional ports defined by subclasses (e.g., custom event or subgraph ports). |

**Lifecycle:**

1. Message on **Input** → node activates → `OnActivate()` is called.
2. **OnActivate** and **Subgraph** ports emit regular messages.
3. Each frame, `OnUpdate(deltaTime)` is called by the graph processor.
4. When internal logic completes → `OnDeactivate` emits, Subgraph ports send disable signals.
5. If **Abort** receives a message → `OnAbort` emits, then node deactivates normally.

**Deferred actions:** If activation logic triggers immediate deactivation (e.g., a timer with duration 0), the deactivation is **deferred** until activation completes. This guarantees that OnActivate and Subgraph ports fire before any deactivation processing begins.

### Creating Custom State Nodes

Extend `StateNode<T>` where `T` is a context class inheriting from `StateNodeContext`:

```csharp
// Custom context to hold node-specific state
public class WaitForTagNodeContext : StateNodeContext
{
    public Tag? WatchedTag { get; set; }
}

// Custom state node that waits until a tag is present
public class WaitForTagNode : StateNode<WaitForTagNodeContext>
{
    private readonly Tag _tag;

    public WaitForTagNode(Tag tag)
    {
        _tag = tag;
    }

    protected override void OnActivate(GraphContext graphContext)
    {
        var context = graphContext.GetNodeContext<WaitForTagNodeContext>(NodeID);
        context.WatchedTag = _tag;
    }

    protected override void OnDeactivate(GraphContext graphContext)
    {
        // Cleanup if needed
    }

    protected override void OnUpdate(double deltaTime, GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var abilityContext))
        {
            return;
        }

        if (abilityContext.Owner.Tags.CombinedTags.HasTag(_tag))
        {
            DeactivateNode(graphContext);
        }
    }
}
```

Use `DeactivateNode(graphContext)` for simple deactivation, or `DeactivateNodeAndEmitMessage(graphContext, portIds)` to emit custom event port messages before deactivation.

### Built-in State Nodes

#### TimerNode

Remains active for a configured duration, then deactivates. The duration is read from a bound input property, so it can be a fixed variable value, driven by an entity attribute, or any other property resolver that produces a `double`.

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Duration | `double` | Seconds to remain active. |

```csharp
graph.VariableDefinitions.DefineVariable("duration", 2.0);

var timer = new TimerNode();
timer.BindInput(TimerNode.DurationInput, "duration");
graph.AddNode(timer);

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
```

The timer accumulates elapsed time during `OnUpdate` calls. When elapsed time reaches or exceeds the duration, the node deactivates.

---

## Port Types

### EventPort

Carries regular messages. Does **not** propagate disable-subgraph signals. Used by Action node outputs, Condition node outputs, and State node event outputs (OnActivate, OnDeactivate, OnAbort).

### SubgraphPort

Carries **both** regular messages and disable-subgraph signals. Used by the Entry node's output and State node Subgraph outputs. A Subgraph port **owns** the downstream nodes connected to it: when it sends a disable signal, everything downstream is cleaned up. Custom state nodes can define additional Subgraph ports and control each one independently. See [Subgraphs](subgraphs.md) for details on the lifetime implications.

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
