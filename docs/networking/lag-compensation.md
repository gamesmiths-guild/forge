# Hits on the network

A client sees the entities it mirrors behind the server. It runs its own player a few ticks ahead, and shows everything else half a round trip and its interpolation delay behind. What its player does is predicted, so a shot, a sword swing or a blast happens at once and on the same ticks as the server's. What it hits isn't where the server has it, though. This page covers the two things that make a hit feel right anyway:

- the server tests what a client's actions hit against the world as that client saw it, which is lag compensation;
- the client shows what its action hit the moment it hits, without waiting for the server's word.

## Testing an action as its instigator saw it

Every `InputCommand` carries a `ViewDelay`: how many ticks behind the command's tick the client showed the entities it mirrors as it simulated that tick. It travels in eighths of a tick, as a single bit while it doesn't change. On a local link it's already about 10 to 12 ticks at 64 Hz: the client runs about 5 ticks ahead, shows the world its interpolation delay behind the newest state it holds (see [Replication](replication.md)), and the newest state is half a round trip old. The engine integration sets it as it sends the command.

On the server, `ReplicationServer.LagCompensation` keeps:

- **The bodies as the clients were shown them.** Each send's bodies go into a `RemoteBody` per entity, rounded as the clients received them, so `TryRewind` finds a body between sends the way a client's interpolation does, snap distance included.
- **Each client's newest view delay,** from the commands `NetworkSession.TakeInput` takes. A lost command's stand-in leaves it unchanged. `ViewDelay(peer)` is capped at `MaxRewindSeconds`, 0.5 by default, which covers about a 250 ms round trip in full. A client further behind is compensated that far and leads the rest.

A hit is tested inside a scope:

```csharp
LagCompensation compensation = server.Replication!.LagCompensation;
double shown = compensation.ShownTickOf(instigator, tick);

compensation.Begin(instigator, shown);

try
{
    compensation.Rewound(instigator, shown, bodies);
    // Put each body where it was, query, put it back.
}
finally
{
    compensation.End();
}
```

`Rewound` lists every entity with a body that's `ReplicatedEntity.LagCompensated` (on by default) and that the instigator doesn't own. The instigator's own player and what it spawned are predicted, so its client showed them where they are. Forge doesn't own physics, so the engine integration puts the bodies in place for its queries. Forge for Godot does it with a stand-in body for each.

An action that lasts, as a projectile in flight or an area that lingers, tests the world on many ticks. `LagCompensation.Remaining(elapsed, fade)` fades its compensation out: all of it on its first tick, none once it has run for the fade time. Pass the result to `ShownTickOf`. A fade of 0 compensates the first tick alone, close to engines that don't compensate projectiles at all. An infinite one keeps the instigator's view the whole time. The fade is the defender's side of the trade: a target that dodged a slow projectile on its own screen isn't hit by where it was a while ago.

### What else counts as it was

Only bodies are rewound. An entity's tags, attributes and effects are read as they are when the server tests the hit, so a shield raised after the attack still protects.

`EffectData.LagCompensation` changes that for one effect. `EffectLagCompensation.Rewound` makes it count as it was at the tick the instigator was shown, for whether a hit lands. While a scope is open, an application's checks read the tags the effect grants, through `ModifierTagsEffectComponent`, and its `ImmunityEffectComponent` as they were then:

- an invulnerability that ended after the attack still turns the hit away;
- a dodge started after it doesn't.

The tags are put as they were and back without raising `OnTagsChanged`. What the hit then does reads the target as it is: an armor buff's modifiers count as they are now. Inhibited time isn't told apart from active time.

```csharp
var dodge = new EffectData(
    "Dodge",
    new DurationData(DurationType.HasDuration, new ModifierMagnitude(
        MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.3f))),
    effectComponents: [new ModifierTagsEffectComponent(invulnerableTags)],
    lagCompensation: EffectLagCompensation.Rewound);
```

## Showing a hit at once

A client's predicted action that meets an entity the client mirrors, a shot or a blast, applies its effect there, as on the server. A mirror refuses every effect, so the effect is still the server's. The client shows its cues early, though: when a run the client predicts applies an effect to an entity it mirrors, the effect's executed cues play there at once. When the server's executed cue for the same hit arrives, it isn't played again. It matches by target, cue tag and source, the source only when it's still there to match, since a projectile or a blast is often gone by then.

`CueData.PredictOnOthers` decides it per cue:

| `CuePrediction` | Shown early |
|---|---|
| `Default` | An executed cue, while `ReplicationClient.PredictCuesOnOthers` is on, as it is by default. A persistent cue waits for the server. |
| `Always` | Always, a persistent cue too. When the server applies the same cue, it takes over as an update. When it doesn't within 0.25 s past the hit's tick, shown, the cue is removed as interrupted. |
| `Never` | Never: it waits for the server, as a damage number that shouldn't show a value the server didn't deal. |

A cue shown early has the magnitude the effect's flat modifiers would change, as the client holds the target. What a custom execution would add isn't known on the client, so a cue whose magnitude depends on one shows 0: make it `Never`. A hit the server doesn't confirm leaves its executed cues shown, which is the cost of showing them early. The ticks a correction runs again show nothing new.

A graph's hit has to run on the client for any of this: keep the effect's application out of a [`ServerOnly` region](statescript.md#regions), and keep in one what only the server should compute.
