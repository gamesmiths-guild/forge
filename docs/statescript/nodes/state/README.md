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

## Built-in State Nodes

| Node | Description |
|------|-------------|
| [TimerNode](timer-node.md) | Remains active for a configured duration, then deactivates. |
