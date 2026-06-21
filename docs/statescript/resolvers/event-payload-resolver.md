# Event Payload Resolvers

> **Types:** `Gamesmiths.Forge.Statescript.Properties.EventPayloadResolver` (raise side) and
> `Gamesmiths.Forge.Statescript.Properties.EventPayloadOutputResolver` (listener side)
> **Output Types:** `EventPayloadRaiser` (raise side) / `EventPayloadWriter` (listener side)

Authors the custom typed payload of an event for the event nodes. A single `IEventPayloadProvider` serves both directions:

- On [RaiseEventNode](../nodes/action/raise-event-node.md), an `EventPayloadResolver` produces an `EventPayloadRaiser` that calls the provider's `CreatePayload` to build the payload and raises a typed (non-boxing) `EventData<TPayload>`.
- On [EventListenerNode](../nodes/state/event-listener-node.md), an `EventPayloadOutputResolver` produces an `EventPayloadWriter` that calls the provider's `WriteOutputs` to decompose a received payload into bound graph variables.

Both directions go through the typed `Raise<TPayload>` / `Subscribe<TPayload>` path, so the payload is never boxed. (A provider-less listener instead uses the non-generic catch-all and receives generic raises with the payload boxed into `EventData.Payload`.)

This is the event-side analog of [EffectContextDataResolver](effect-context-data-resolver.md) and [CueCustomParametersResolver](cue-custom-parameters-resolver.md), extended with the decompose-to-outputs direction the listener needs.

## Defining a provider

Derive from `EventPayloadProvider<TPayload>` and override `CreatePayload` (build) and `WriteOutputs` (decompose). Declare `Inputs` to author values on the raise node, and `Outputs` to bind graph variables on the listener node.

```csharp
public sealed record HitEventPayload(int Damage, bool IsCritical);

public sealed class HitEventPayloadProvider : EventPayloadProvider<HitEventPayload>
{
    public override IReadOnlyList<EventPayloadInput> Inputs =>
        [new EventPayloadInput("Damage", typeof(int)), new EventPayloadInput("IsCritical", typeof(bool))];

    public override IReadOnlyList<EventPayloadOutput> Outputs =>
        [new EventPayloadOutput("Damage", typeof(int)), new EventPayloadOutput("IsCritical", typeof(bool))];

    public override HitEventPayload CreatePayload(GraphContext graphContext, EventPayloadInputs inputs)
    {
        return new HitEventPayload(inputs.Get<int>("Damage"), inputs.Get<bool>("IsCritical"));
    }

    public override void WriteOutputs(HitEventPayload payload, EventPayloadOutputs outputs)
    {
        outputs.Set("Damage", payload.Damage);
        outputs.Set("IsCritical", payload.IsCritical);
    }
}
```

`EventPayloadInputs.Get<T>` reads a declared input (`default` when unbound); `EventPayloadOutputs.Set<T>` writes an unmanaged value, and `SetObject` writes a reference value, to the variable bound to the named output (skipped when the output has no binding). Declared input/output value types must be supported by `Variant128`; a provider that needs object-lane values can read them directly from `graphContext` (build) or write them with `SetObject` (decompose).

## Raise side: `EventPayloadResolver`

> **Output Type:** `EventPayloadRaiser`, bind to the `Payload` input of [RaiseEventNode](../nodes/action/raise-event-node.md).

`EventPayloadResolver.Resolve` returns an `EventPayloadRaiser`; for each target the node calls `raiser.Raise(...)`, which builds the payload via `provider.CreatePayload(graphContext, inputs)` and raises a typed `EventData<TPayload>` (`EventManager.Raise<TPayload>`), no boxing.

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "hitPayload",
    new EventPayloadResolver(new HitEventPayloadProvider()));

var raiseEvent = new RaiseEventNode();
raiseEvent.BindInput(RaiseEventNode.PayloadInput, "hitPayload");
```

## Listener side: `EventPayloadOutputResolver`

> **Output Type:** `EventPayloadWriter`, bind to the `Payload` input of [EventListenerNode](../nodes/state/event-listener-node.md).

`EventPayloadOutputResolver.Resolve` returns an `EventPayloadWriter`; the listener subscribes through the provider's typed `Subscribe<TPayload>` path, and the writer invokes `provider.WriteOutputs` against the authored output-to-variable bindings on each matching event.

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "hitPayloadOut",
    new EventPayloadOutputResolver(
        new HitEventPayloadProvider(),
        new Dictionary<string, EventOutputBinding>
        {
            ["Damage"] = new EventOutputBinding("lastDamage", VariableScope.Graph),
        }));

var listener = new EventListenerNode();
listener.BindInput(EventListenerNode.PayloadOutputInput, "hitPayloadOut");
```

## See Also

- [Resolvers Overview](README.md)
- [EffectContextDataResolver](effect-context-data-resolver.md)
- [CueCustomParametersResolver](cue-custom-parameters-resolver.md)
- [RaiseEventNode](../nodes/action/raise-event-node.md)
- [EventListenerNode](../nodes/state/event-listener-node.md)
