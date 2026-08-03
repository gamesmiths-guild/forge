# State Nodes

State nodes **persist over time**. They activate when receiving a message, remain active across frames, and deactivate based on internal logic. State nodes are what give Statescript its "state-based" nature and they represent ongoing conditions that own [subgraphs](../../subgraphs.md).

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

## Creating Custom State Nodes

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

        if (abilityContext.Owner.Tags.AllTags.HasTag(_tag))
        {
            DeactivateNode(graphContext);
        }
    }
}
```

Use `DeactivateNode(graphContext)` for simple deactivation, or `DeactivateNodeAndEmitMessage(graphContext, portIds)` to emit custom event port messages before deactivation.

**Emitting on the activation frame.** Messages emitted from `OnActivate` are *deferred* and flushed as a batch once activation completes, so `OnActivate` and Subgraph always fire first. That is fine for a fixed set of events, but it means any per-emission state you write alongside them already holds its final value by the time they fire. When you need emissions **interleaved** with state changes on the activation frame — a loop writing an iteration variable before each event — override `OnActivated` instead. It runs once activation is fully complete, and only if the node is still active. Anything you reach from there can deactivate the node or stop the graph, so a method that emits more than once must re-check `IsNodeActive(graphContext)` between emissions rather than trust a node context it captured earlier. The same caution applies to an `OnUpdate` that emits in a loop. `IterationNode<T>` (the base of [RepeatNode](repeat-node.md) and [ForEachNode](for-each-node.md)) is the worked example.

If your state node defines additional event or subgraph ports, override `DefinePorts`, call `base.DefinePorts(...)`, and create each custom port with an explicit label:

```csharp
protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
{
    base.DefinePorts(inputPorts, outputPorts);
    outputPorts.Add(CreatePort<EventPort>(OnFinishedPort, "OnFinished"));
}
```

That label becomes the canonical port name surfaced by editor integrations such as Forge for Godot.

## Built-in State Nodes

| Node | Description |
|------|-------------|
| [AbilityEndListenerNode](ability-end-listener-node.md) | Listens for abilities ending on an entity and emits OnAbilityEnded with the ability and cancel state. |
| [AttributeListenerNode](attribute-listener-node.md) | Listens for attribute value changes and emits OnChanged with the new value and delta. |
| [ConditionMonitorNode](condition-monitor-node.md) | Monitors a boolean condition, emitting transition events and routing between a true and false subgraph. |
| [CueNode](cue-node.md) | Applies cues on activation and removes them on deactivation, with an optional interrupted flag. |
| [EffectLevelListenerNode](effect-level-listener-node.md) | Listens for effect level changes and emits OnLevelChanged with the new level. |
| [EffectNode](effect-node.md) | Applies effects on activation, emits OnEffectEnd on natural completion, and removes still-active instances on deactivation. |
| [EventListenerNode](event-listener-node.md) | Listens for events while active and emits OnEvent each time a matching event fires. |
| [ForEachNode](for-each-node.md) | Walks an array, publishing each element to a variable, on the activation frame or spaced by an interval. |
| [GrantAbilityNode](grant-ability-node.md) | Grants an ability while active, removing the grant on deactivation. |
| [LoopTimerNode](loop-timer-node.md) | Emits an interval event every period while active, optionally finishing after a number of loops. |
| [RepeatNode](repeat-node.md) | Emits an iteration event a fixed number of times, on the activation frame or spaced by an interval. |
| [StateMachineNode](state-machine-node.md) | Keeps exactly one state subgraph active, selected by an integer input. |
| [TagListenerNode](tag-listener-node.md) | Listens for watched tags being added to or removed from an entity. |
| [TimerNode](timer-node.md) | Remains active for a configured duration and emits OnTimerEnd when it finishes naturally. |
