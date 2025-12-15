# Events System

The Events system in Forge provides a flexible event bus for triggering gameplay reactions, driving ability activation, and propagating tagged event data. 

## Core Concepts

- Events carry tags for filtering `EventTags` plus optional source, target, magnitude, and payload data. 
- Handlers subscribe by tag and run in priority order (higher priority first).
- Generic events avoid boxing by using typed payloads. 
- Generic raises do **not** forward to non-generic handlers. 

## Event Data

### EventData

```csharp
public readonly record struct EventData
{
    public TagContainer EventTags { get; init; }
    public IForgeEntity?  Source { get; init; }
    public IForgeEntity?  Target { get; init; }
    public float EventMagnitude { get; init; }
    public object?  Payload { get; init; }
}
```

### EventData<TPayload>

```csharp
public readonly record struct EventData<TPayload>
{
    public TagContainer EventTags { get; init; }
    public IForgeEntity? Source { get; init; }
    public IForgeEntity? Target { get; init; }
    public float EventMagnitude { get; init; }
    public TPayload Payload { get; init; }
}
```

- **EventTags**:  Tag-based filtering key (uses `TagContainer. HasTag`).
- **Source/Target**:  Originator and intended recipient of the event.
- **EventMagnitude**: Optional numeric intensity. 
- **Payload**: Optional opaque object, or a typed payload for generic events.

## Event Manager

`EventManager` manages subscriptions and dispatch.  While every `IForgeEntity` has an `EventManager` instance, you can create and manage additional instances for any purpose: global event buses, subsystem-specific channels, or custom scopes.

```csharp
// Per-entity event manager (built-in)
entity.Events. Raise(eventData);

// Custom event manager for a subsystem
var combatEvents = new EventManager();
combatEvents. Subscribe(damageTag, OnDamageDealt);

// Global event bus
public static class GlobalEvents
{
    public static EventManager Instance { get; } = new EventManager();
}
```

### API Reference

```csharp
public sealed class EventManager
{
    public void Raise(in EventData data);
    public void Raise<TPayload>(in EventData<TPayload> data);

    public EventSubscriptionToken Subscribe(Tag eventTag, Action<EventData> handler, int priority = 0);
    public EventSubscriptionToken Subscribe<TPayload>(Tag eventTag, Action<EventData<TPayload>> handler, int priority = 0);

    public bool Unsubscribe(EventSubscriptionToken token);
}
```

- Subscriptions are sorted by `priority` (higher first).
- A handler is invoked when `data.EventTags. HasTag(eventTag)` is true.
- Generic subscriptions are stored per `TPayload` type and are only invoked for matching generic raises. 

## Usage Examples

### Subscribe and Raise (non-generic)

```csharp
var eventTag = Tag.RequestTag(tagsManager, "events.combat. hit");
EventSubscriptionToken token = entity.Events.Subscribe(eventTag, data =>
{
    // React to combat hit
    var source = data.Source;
    var target = data.Target;
    float magnitude = data.EventMagnitude;
});

// Raise the event
entity.Events. Raise(new EventData
{
    EventTags = eventTag.GetSingleTagContainer(),
    Source = attacker,
    Target = victim,
    EventMagnitude = 25f
});
```

### Generic Payload

```csharp
public record struct HitPayload(int Damage, bool Critical);

var hitTag = Tag.RequestTag(tagsManager, "events.combat.hit");

EventSubscriptionToken token = entity.Events.Subscribe<HitPayload>(hitTag, data =>
{
    HitPayload payload = data. Payload;
    int damage = payload. Damage;
    bool crit = payload.Critical;
});

entity.Events. Raise(new EventData<HitPayload>
{
    EventTags = hitTag.GetSingleTagContainer(),
    Source = attacker,
    Target = victim,
    EventMagnitude = 25f,
    Payload = new HitPayload(25, critical: true)
});
```

### Unsubscribe

```csharp
entity.Events. Unsubscribe(token);
```

## Tagging and Filtering

- Use dedicated event tags (e.g., `events.combat.hit`, `events.status.applied`) registered in `TagsManager`.
- Matching uses hierarchy:  `EventTags.HasTag(subscriptionTag)` supports parent/child tag relationships. 

## Integration Notes

- Abilities can use event tags as triggers (see ability trigger configurations in the Abilities system).
- Event tags can align with cues or effects to coordinate cross-system reactions. 
- When an event should trigger visual feedback, raise the event for gameplay logic, then trigger the corresponding cue separately for presentation.

## Events vs Cues

Events and [Cues](cues.md) serve distinct purposes:

- **Events** are part of the core simulation.  They drive gameplay logic, trigger abilities, and propagate state changes.  In a networked context (planned), events would require reliable replication. 
- **Cues** are for the presentation layer.  They handle visual effects, audio, and player feedback. In a networked context (planned), cues can use unreliable replication since they don't affect game state.

Use Events when the outcome affects game state or triggers other gameplay systems. Use Cues when you need to communicate changes to the player through feedback.

## Best Practices

1. **Define Tag Conventions**:  Use consistent prefixes (e.g., `events.*`) for clarity.
2. **Prefer Typed Payloads**: Use `EventData<TPayload>` to avoid boxing and improve safety.
3. **Use Priorities Sparingly**: Reserve high priorities for critical handlers; keep most at default. 
4. **Unsubscribe When Done**: Store tokens and call `Unsubscribe` to avoid stale handlers. 
5. **Keep Handlers Lightweight**:  Avoid heavy work inside handlers; defer long tasks if needed.
6. **Validate Tags**:  Ensure tags are registered in `TagsManager` before use.
7. **Separate Events from Cues**: Use events for gameplay-affecting logic; trigger cues separately for presentation.
8. **Consider Scope**: Use entity-level `EventManager` for entity-specific events; create custom instances for broader or specialized scopes.
