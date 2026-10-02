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

1. Message on **Input** → node activates → `OnActivate()` is called. A message that arrives while the node is already active is a [retrigger](#retriggers) instead.
2. **OnActivate** and **Subgraph** ports emit regular messages.
3. Each frame, `OnUpdate(deltaTime)` is called by the graph processor. On each fixed step, `OnFixedUpdate(deltaTime)` is called instead — see [Two update rails](#two-update-rails).
4. When internal logic completes → `OnDeactivate` emits, Subgraph ports send disable signals.
5. If **Abort** receives a message → `OnAbort` emits, then node deactivates normally.

**Deferred actions:** If activation logic triggers immediate deactivation (e.g., a timer with duration 0), the deactivation is **deferred** until activation completes. This guarantees that OnActivate and Subgraph ports fire before any deactivation processing begins.

## Retriggers

A message that reaches **Input** while the node is already active is a **retrigger**: a Loop Timer re-kicking a walk, an event listener feeding the same Effect node on every hit. By default a retrigger is **ignored** — nothing is called, no port emits, and whatever the running activation holds (an applied effect, a subscription, a spawned instance) carries on untouched. Only a node that has ended can be activated again: a message that reaches one still deactivating, from something its OnDeactivate set off, is ignored too, whether or not the node can restart.

Some nodes can instead **restart**, and do so when built with `restartOnRetrigger: true`:

| Node | What a restart does |
|------|---------------------|
| [TimerNode](timer-node.md) | The elapsed time starts again from zero, so the timer runs a full duration from the retrigger. |
| [LoopTimerNode](loop-timer-node.md) | The interval and the loop count both start again from zero. |
| [RepeatNode](repeat-node.md), [ForEachNode](for-each-node.md) | The walk starts over from the first iteration, which runs on the restart frame. ForEachNode snapshots its array again. |
| [EffectNode](effect-node.md) | The effects it applied are removed and applied again with the inputs re-resolved — a refresh. |
| [CueNode](cue-node.md) | The cues it applied are removed, as interrupted, and applied again with the inputs re-resolved. |

The rest have nothing a restart would mean — they re-evaluate their inputs every update anyway, or hold something that starting over would only drop and take back — so a retrigger stays ignored for them.

A restart runs in place of an activation, and the node stays active throughout:

1. `OnRestart()` is called instead of `OnActivate()`. `OnDeactivate()` is **not** called first.
2. **OnActivate** and **Subgraph** emit again; **OnDeactivate** does not.
3. `OnActivated()` runs once the restart is complete, as it does after an activation. Messages emitted from `OnRestart()` are deferred until then, and so is a deactivation it asks for.
4. The node is first updated on the next pass, as an activated node is, even when the retrigger came from a node updated before it in the current one. No time from before the retrigger counts toward the new run.

The subgraph is **retriggered, not rebuilt**: its nodes are already active, so each one follows its own `restartOnRetrigger`. See [Subgraphs](../../subgraphs.md#retriggering-a-parent).

## Two update rails

A state node can be advanced on either of two independent rails, and it should override exactly one of them.

| Hook | Driven by | Delta | Use it for |
|---|---|---|---|
| `OnUpdate(deltaTime, graphContext)` | `GraphProcessor.UpdateGraph` | The frame's elapsed time | Timers, animations, input, anything counting wall-clock time |
| `OnFixedUpdate(deltaTime, graphContext)` | `GraphProcessor.FixedUpdateGraph` | The fixed step's length | Moving a body, steering a character, querying the physics world, anything a networked peer must reproduce step for step |

The two rates differ and neither substitutes for the other. The frame rate is whatever the machine manages and drifts far above or below the fixed rate, so a body driven from `OnUpdate` is pushed a different amount per second on a fast machine than on a slow one, and a query asked from there is asked several times about a world that has not changed, or not at all in the step where it did.

The hook is called **fixed** rather than **physics** because the interval is the guarantee and physics is only the most common reason to want one. A dedicated server with physics switched off still runs this rail, and a networked simulation drives it from its own clock rather than from the engine's.

**A host drives whichever rails it has.** Godot calls `UpdateGraph` from `_Process` and `FixedUpdateGraph` from `_PhysicsProcess`; a turn-based game may call only the first. A host with no fixed step never calls `FixedUpdateGraph`, and nodes that override `OnFixedUpdate` simply do not run — which is the honest failure, since falling back to the frame would reintroduce exactly what overriding it avoids.

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

**Emitting on the activation frame.** Messages emitted from `OnActivate` are *deferred* and flushed as a batch once activation completes, so `OnActivate` and Subgraph always fire first. That is fine for a fixed set of events, but it means any per-emission state you write alongside them already holds its final value by the time they fire. When you need emissions **interleaved** with state changes on the activation frame — a loop writing an iteration variable before each event — override `OnActivated` instead. It runs once activation is fully complete, and only if the node is still active. Anything you reach from there can deactivate the node or stop the graph, so a method that emits more than once must re-check between emissions that the node context it holds is still `Active`. Check that context rather than `IsNodeActive(graphContext)`: it reads inactive once the node ends or its run does, while a node started again or a graph started over from there gets a new one, which is the one `IsNodeActive` finds. The same caution applies to an `OnUpdate` that emits in a loop. `IterationNode<T>` (the base of [RepeatNode](repeat-node.md) and [ForEachNode](for-each-node.md)) is the worked example.

If your state node defines additional event or subgraph ports, override `DefinePorts`, call `base.DefinePorts(...)`, and create each custom port with an explicit label:

```csharp
protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
{
    base.DefinePorts(inputPorts, outputPorts);
    outputPorts.Add(CreatePort<EventPort>(OnFinishedPort, "OnFinished"));
}
```

That label becomes the canonical port name surfaced by editor integrations such as Forge for Godot.

**Supporting restarts.** A node that can start over when [retriggered](#retriggers) overrides `OnRestart` and takes a `restartOnRetrigger` constructor parameter, passing it on to the base constructor. The parameter is what tells an editor to offer the choice, so a node without it is never asked to restart, and one with it but no `OnRestart` would offer a choice that changes nothing:

```csharp
public class WaitForTagNode(Tag tag, bool restartOnRetrigger = false)
    : StateNode<WaitForTagNodeContext>(restartOnRetrigger)
{
    // ...

    protected override void OnRestart(GraphContext graphContext)
    {
        // Activation only records the tag, so starting over is activating again.
        OnActivate(graphContext);
    }
}
```

No deactivation runs before `OnRestart`: the node stays active, so one that holds something — an applied effect, a subscription, a spawned instance — releases it there before acquiring it again, or the first one is left behind. Releasing can reach the rest of the graph — an effect's removal, a cue handler, a subgraph that ends — and end the node or stop the graph along the way, so check that the node context it released from is still `Active` before acquiring again. A node whose subgraph depends on what the restart replaces, such as a spawned instance that the subgraph moves around, disables that subgraph first (`((SubgraphPort)OutputPorts[SubgraphPort]).EmitDisableSubgraphMessage(graphContext)`), so it comes back fresh when the Subgraph port emits again.

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
