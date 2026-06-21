# EventListenerNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.EventListenerNode`
> **Context:** `EventListenerNodeContext`

Subscribes to one or more event tags on a chosen entity's event bus while active and emits the **OnEvent** port every time a matching event is raised. It writes the event's data to graph variables and unsubscribes on deactivation.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Activates the state node (subscribes). |
| 1 | Abort | Forcefully deactivates and fires OnAbort (unsubscribes). |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | OnActivate | Event | Emits when the node activates. |
| 1 | OnDeactivate | Event | Emits when the node deactivates (any reason). |
| 2 | OnAbort | Event | Emits only when aborted via the Abort port. |
| 3 | Subgraph | Subgraph | Remains active while the node is active. |
| 4 | OnEvent | Event | Emits each time a subscribed event is raised. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Event Tags | `Tag` or `Tag[]` | The tag(s) to subscribe to. The node subscribes to each. |
| 1 | Listen On | `IForgeEntity` | The entity whose `Events` bus is observed. |
| 2 | Payload | `EventPayloadWriter` | Optional. Decomposes the received `EventData.Payload` into graph variables via an `IEventPayloadProvider` (see [EventPayloadResolver](../../resolvers/event-payload-resolver.md)). |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Source | `IForgeEntity` | The received `EventData.Source`. |
| 1 | Target | `IForgeEntity` | The received `EventData.Target`. |
| 2 | Magnitude | `float` | The received `EventData.EventMagnitude`. |

## Behavior

1. On activation, resolves the **Listen On** entity and subscribes a handler to each resolved **Event Tag** on that entity's `IForgeEntity.Events` bus, storing the subscription tokens in `EventListenerNodeContext`. When a payload provider is bound, the subscription uses the provider's typed payload type (`EventManager.Subscribe<TPayload>`), so generic raises are received with **no boxing** and the typed payload is decomposed directly. When no provider is bound, the subscription is non-generic and acts as a catch-all that also receives generic raises with the payload boxed into `EventData.Payload`.
2. Each time a matching event is raised, the handler writes the bound built-in outputs (Source, Target, Magnitude), decomposes the payload into the provider's bound graph variables (when a payload provider is bound and a payload is present), and emits the **OnEvent** port.
3. The node has **no timer**: it stays active until deactivated externally (typically as a subgraph of another state node). On deactivation it unsubscribes every stored token.

> The handler emits synchronously from the `EventManager.Raise` call. Keep downstream graphs of **OnEvent** lightweight, and be aware that deactivating the listener from within its own `OnEvent` reaction re-enters the event manager's dispatch.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<Tag>("hitEvent", Tag.RequestTag(tagsManager, "event.hit"));
graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("self", owner);

var listener = new EventListenerNode();
listener.BindInput(EventListenerNode.EventTagInput, "hitEvent");
listener.BindInput(EventListenerNode.ListenOnInput, "self");
listener.BindOutput(EventListenerNode.MagnitudeOutput, "lastHitMagnitude", VariableScope.Graph);
```

Place the node as a subgraph of another state node so it lives for that state's duration, and route the **OnEvent** port into the reaction you want each time the event fires.

## See Also

- [State Nodes Overview](README.md)
- [RaiseEventNode](../action/raise-event-node.md)
- [EventPayloadResolver](../../resolvers/event-payload-resolver.md)
