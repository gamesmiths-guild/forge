# RaiseEventNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.RaiseEventNode`

Raises an event (`EventManager.Raise`) on one or more target entities' event buses, then continues execution. Events do not persist, so the raise side is fire-and-forget; use [EventListenerNode](../state/event-listener-node.md) to react to them.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the action. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the event is raised. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Event Tags | `Tag` or `Tag[]` | The tag(s) combined into the event's `EventData.EventTags` container. |
| 1 | Target | `IForgeEntity` or `IForgeEntity[]` | The entity or entities whose `Events` bus the event is raised on. |
| 2 | Source | `IForgeEntity` | Optional. The source entity carried in `EventData.Source`. |
| 3 | Magnitude | `float` | Optional. The `EventData.EventMagnitude`. |
| 4 | Payload | `EventPayloadRaiser` | Optional. A typed payload built and raised by an `IEventPayloadProvider` (see [EventPayloadResolver](../../resolvers/event-payload-resolver.md)). |

This node has no output variables.

## Behavior

1. Resolves the **Event Tags** input as a single `Tag` or an array of tags and combines them into one `TagContainer`.
2. Resolves the **Target** input as a single `IForgeEntity` or an array of entities.
3. Resolves the optional **Source**, **Magnitude**, and **Payload** inputs once and shares them across all targets.
4. For each target, raises one event through that target's `IForgeEntity.Events` bus. When a payload provider is bound, it raises a typed `EventData<TPayload>` (`EventManager.Raise<TPayload>`, no boxing); otherwise a non-generic `EventData` with no payload. A single raise carries all selected tags, so a subscriber to any matching tag is notified once.

When a payload provider is bound the node raises through the typed `Raise<TPayload>` path, so typed (`Subscribe<TPayload>`) listeners (including an [EventListenerNode](../state/event-listener-node.md) with the same provider) receive the payload with no boxing. Non-generic subscribers still receive it (with the payload boxed) via the event manager's catch-all forwarding.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<Tag>("hitEvent", Tag.RequestTag(tagsManager, "event.hit"));
graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

var raiseEvent = new RaiseEventNode();
raiseEvent.BindInput(RaiseEventNode.EventTagInput, "hitEvent");
raiseEvent.BindInput(RaiseEventNode.TargetInput, "target");
```

To attach a payload, bind the optional payload input to an `EventPayloadResolver`:

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "hitPayload",
    new EventPayloadResolver(new HitEventPayloadProvider()));
raiseEvent.BindInput(RaiseEventNode.PayloadInput, "hitPayload");
```

## See Also

- [Action Nodes Overview](README.md)
- [EventListenerNode](../state/event-listener-node.md)
- [EventPayloadResolver](../../resolvers/event-payload-resolver.md)
