# Cues and Events

Presentation through cues, and gameplay messages through events. Every guide here builds on the same small setup: a `PlayerAttributeSet`, a `Player` entity, and a `TagsManager` and `CuesManager`. It is defined once in [Guides](README.md#the-shared-setup); the code samples assume it.

## Cue Examples

Cues bridge gameplay mechanics with visual and audio feedback. Here's how to define, trigger, and implement them.

### Registering a Cue

For a cue handler to work, you must register it with the CuesManager using the appropriate tag:

```csharp
// Register the cue with the manager
cuesManager.RegisterCue(
    Tag.RequestTag(tagsManager, "cues.damage.fire"),
    new FireDamageCueHandler()
);
```

### Triggering a Cue Through Effects

This example shows how to trigger cues as part of an effect:

```csharp
// Define a burning effect that includes the fire damage cue
var burningEffectData = new EffectData(
    "Burning",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(5.0f)
        )
    ),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Health",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(-5) // Damage per tick
            )
        )
    },
    periodicData: new PeriodicData(
        Period: new ScalableFloat(1.0f), // Ticks every second
        ExecuteOnApplication: true,
        PeriodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    ),
    cues: new[] {
        new CueData(
            CueTags: tagsManager.RequestTagContainer(new[] { "cues.damage.fire" }),
            MinValue: 0,
            MaxValue: 100,
            MagnitudeType: CueMagnitudeType.AttributeValueChange,
            MagnitudeAttribute: "PlayerAttributeSet.Health" // Tracks health changes
        )
    }
);

// Apply the burning effect
var burningEffect = new Effect(burningEffectData, new EffectOwnership(player, player));
player.EffectsManager.ApplyEffect(burningEffect);
player.EffectsManager.UpdateEffects(5f);
```

### Manually Triggering a Cue

You can trigger cues directly through the CuesManager:

```csharp
// Manually trigger a fire damage cue with custom parameters
var cueParameters = new CueParameters(
    Magnitude: 25, // Raw damage value
    NormalizedMagnitude: 0.25f, // Normalized between 0-1
    Source: player,
    CustomParameters: new Dictionary<StringKey, object>
    {
        { "DamageType", "Fire" },
        { "IsCritical", true }
    }
);

cuesManager.ExecuteCue(
    cueTag: Tag.RequestTag(tagsManager, "cues.damage.fire"),
    target: player,
    parameters: cueParameters
);
```

### Implementing a Cue Handler

This example shows a simple cue handler that outputs messages to the console. In a real game, you would use this to trigger visual effects, sounds, or other feedback:

```csharp
public class FireDamageCueHandler : ICueHandler
{
    public void OnExecute(IForgeEntity? target, CueParameters? parameters)
    {
        if (parameters.HasValue)
        {
            Console.WriteLine($"Fire damage executed: {parameters.Value.Magnitude}");
        }
    }

    public void OnApply(IForgeEntity? target, CueParameters? parameters)
    {
        // Logic for when a persistent cue starts (e.g., play fire animation)
        if (target != null)
        {
            Console.WriteLine("Fire damage cue applied to target.");
        }
    }

    public void OnRemove(IForgeEntity? target, bool interrupted)
    {
        // Logic for when a cue ends (e.g., stop fire animation)
        Console.WriteLine("Fire damage cue removed.");
    }

    public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
    {
        // Logic for updating persistent cues (e.g., adjust fire intensity)
        if (parameters.HasValue)
        {
            Console.WriteLine($"Fire damage cue updated with magnitude: {parameters.Value.Magnitude}");
        }
    }
}
```

## Events

The Events system allows entities to communicate and trigger reactions through tagged events.

### Subscribing and Raising an Event

```csharp
// Subscribe to the damage event
var damageTag = Tag.RequestTag(tagsManager, "events.combat.damage");
player.Events.Subscribe(damageTag, eventData =>
{
    Console.WriteLine($"Player took {eventData.EventMagnitude} damage!");
});

// Raise the event
player.Events.Raise(new EventData
{
    EventTags = damageTag.GetSingleTagContainer(),
    Source = null,
    Target = player,
    EventMagnitude = 50f
});
```

You can also instantiate your own `EventManager` and use it in any part of your code, providing a way to handle global or system-specific events independently of entities.

### Strongly Typed Events

You can optimize events to avoid boxing by using generic `EventData`.

```csharp
// Define a strongly typed payload. DamageType stands for your own game enum.
public enum DamageType { Physical, Magical }

public record struct DamageInfo(int Value, DamageType DamageType, bool IsCritical);

var damageTag = Tag.RequestTag(tagsManager, "events.combat.damage");

// Subscribe using the specific payload type.
// The type argument must match the payload the event is raised with, otherwise the handler never fires.
player.Events.Subscribe<DamageInfo>(damageTag, eventData =>
{
    Console.WriteLine(
        $"[Combat Log] Damage: {eventData.Payload.Value}, " +
        $"Type: {eventData.Payload.DamageType}, " +
        $"Critical: {eventData.Payload.IsCritical}"
    );
});

// Raise the event with the typed payload
player.Events.Raise(new EventData<DamageInfo>
{
    EventTags = damageTag.GetSingleTagContainer(),
    Source = null,
    Target = player,
    Payload = new DamageInfo(120, DamageType.Physical, true)
});
```

