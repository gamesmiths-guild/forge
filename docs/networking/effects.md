# Effects on the network

[Replication](replication.md) sends a client what an entity's effects did: attribute values, tags and cues. It also sends the effects themselves, so a client can ask what's on an entity: is it burning, how many stacks, for how much longer, and who did it. This page covers what a client holds, who hears about which effect, what the owner gets on top, and how custom data travels.

## What a client holds

A client's mirror of an entity holds the server's active effects in its `EffectsManager`. They answer the way the server's do, and nothing of them runs.

| Works as on the server | How it works on a mirror |
|---|---|
| `GetActiveEffects`, `HasAnyActiveEffect`, `GetEffectStackData`, every `EffectQuery` | The effects are there, built from the same `EffectData`, with their level, stacks and owner. |
| `ActiveEffectHandle`: `IsValid`, `Effect`, `Target`, `Level`, `StackCount`, `TotalDuration`, `RemainingDuration`, `Period`, `ExecutionCount`, `IsInhibited`, `TryGetContextData` | What the server last said. `RemainingDuration` counts down with the server's ticks, as of the newest state this client holds. |
| `Effect.DataTag` | The server's set-by-caller values. A mirror's `Effect` is this client's copy: changing its `DataTag` or level changes nothing else, and lasts until the server next changes that effect. |
| `OnActiveEffectAdded`, `OnActiveEffectChanged`, `OnActiveEffectRemoved` | Raised as the server's word arrives, once per change however many copies of it arrive. The removal says whether the effect expired or was removed, or `Destroyed` when the entity itself went. |
| The Statescript resolvers that read effects | They read the same queries. |

| Doesn't run on a mirror | Why |
|---|---|
| Modifiers | What they did arrives as the attribute's value, so applying them again would count them twice. |
| Periodic executions | Same. `ExecutionCount` follows the server's. |
| Components | They change the entity, and the server already did. `ComponentInstances` is empty, and `GetComponent` finds nothing. |
| Cues | They arrive on their own, as [cue state and events](replication.md#what-replicates), whatever the effect's visibility. |
| `OnEffectApplied`, `OnEffectExecuted` | An instant effect never reaches a client, so these couldn't mean what they mean on the server. Watch the attribute's `OnValueChanged` or a cue instead. |
| Applying, removing, inhibiting, refreshing | A mirror holds what the server says. `ApplyEffect` returns `null` and the others do nothing. |

An effect ends on a mirror only when the server says so. One whose time ran out here keeps a `RemainingDuration` of zero until the word arrives.

## Who hears about an effect

`EffectData.Visibility` says who hears about an effect while it's active:

| `EffectVisibility` | Who hears |
|---|---|
| `Public` | Every client that has the entity. |
| `OwnerOnly` | Only the client that [owns](replication.md#the-server) the entity. On an entity no client owns, nobody. |
| `ServerOnly` | No client. |
| `Default` | Whatever `ReplicationServer.DefaultEffectVisibility` says, `Public` unless the host changes it. It's read as each effect first goes active. |

```csharp
var cooldown = new EffectData(
    "Dash Cooldown",
    new DurationData(DurationType.HasDuration, new ModifierMagnitude(
        MagnitudeCalculationType.ScalableFloat, new ScalableFloat(3))),
    effectComponents: [new ModifierTagsEffectComponent(cooldownTags)],
    visibility: EffectVisibility.OwnerOnly);
```

Visibility decides who gets the **effect**. What it did is the entity's state, and reaches every client that has the entity whatever this says: the attribute values it changed, the tags it granted and the cues it fired. An effect that's `ServerOnly` and grants a tag still shows that tag on every mirror. To hide what an effect does, don't give it a cue, and keep it from changing what others can see.

Choose `ServerOnly` for what no client needs, such as an effect that grants abilities: nothing about it goes on the wire. Choose `OwnerOnly` for what only its player reads, such as a cooldown: others' copies are spared the record, and the cooldown's tag still reaches them.

### Effects no client can hear about

An effect travels as its index in the [asset registry](README.md#identity), so one the registry doesn't list stays on the server. An instant effect never travels at all: it's over before anything could be said about it.

`ReplicationServer.EffectNotListed` is raised the first time an effect the registry doesn't list goes active on a replicated entity, unless it's `ServerOnly`, which says that's on purpose.

## What the owner gets

Everyone's mirror has an attribute's current value as its base value, with no modifier, since that's all anyone needs to show it. The client that owns the entity [predicts](prediction.md) it instead of mirroring it: its effects are live, so `BaseValue`, `CurrentValue`, `Modifier` and `Overflow` read as they do on the server, and a health bar can show what a buff adds.

- **Effects its own tick applies**, a cooldown or a dash's speed, it applies itself as it predicts the tick, executions included.
- **Effects from elsewhere**, another entity's or an area's, it takes from the server, at the tick each landed after. Their duration, modifiers, tags, components and cues run on its entity; their executions don't, and what they do arrives as base values, which the owner is sent with the current ones.
- **An effect it doesn't hear of** stays the server's, as on a mirror.

## Custom data

An effect applied with custom data, through `ApplyEffect<TData>`, can take the data along. Say how each type travels in the registry, on every machine, under a key that means the same everywhere:

```csharp
registry.Register(
    AssetKind.EffectContext,
    "hit",
    EffectContextCodec.For<HitData>(
        static (writer, hit) =>
        {
            writer.WriteVarInt(hit.Damage);
            writer.WriteString(hit.Direction);
        },
        static reader => new HitData((int)reader.ReadVarInt(), reader.ReadString())));

// On the server.
target.EffectsManager.ApplyEffect(effect, new HitData(7, "north"));

// On a client, or the server.
if (handle.TryGetContextData(out HitData? hit))
{
    ShowHitFrom(hit.Direction);
}
```

The codecs are part of the registry, so a client with other keys is refused. The hash covers the keys and not what's behind them, so keep each key's codec the same on every machine: a client with another codec under the same key joins and misreads the data. Data of a type nothing is registered for stays on the server: `TryGetContextData` finds nothing on a client. A packet is untrusted, so a `read` that meets something that can't be right throws `MalformedPacketException`. A `write` that throws costs that effect its data on every client that hasn't got it yet. The client it was being written for hears nothing of that entity in that send, and from then on the effect travels without the data. Other clients and entities carry on, and `ReplicationServer.Send` then rethrows the exception. That happens once per effect, and if several writes fail in the same send, only the first exception comes out.

The data goes once, as the effect first reaches a client. It never changes on the server either.

## Who did it

An effect's owner and source are sent as their `NetworkId`s, and `Effect.Ownership` holds whatever mirrors the client has of them. A client that doesn't have the entity, because it isn't relevant to that client or isn't networked at all, sees `null`, and sees the entity as soon as it has it, such as when the source comes into range. A source that leaves goes back to `null`, so nothing holds on to an entity that's gone.

## Time

An effect's end is sent as a tick, so an effect that's only running sends nothing at all, however long it runs. A client works out what's left from the newest state it holds. A client that joins late, or an entity that comes back into relevancy, hears the same end, so what's left is right from the start.

A periodic effect's period follows from its level and data, and its next execution isn't sent. Each execution changes only `ExecutionCount`, which travels on its own in a few bytes.

## The wire

An effect is a record in the entity's entry, after its executed cues: its instance number, then whichever of these the client hasn't acknowledged.

- **What it is**, once: its index in the registry, for the entity's owner whether it landed from outside the entity's ticks and after which tick, and its custom data.
- **How it is**, when any of it changes: level, stacks, whether it's inhibited, for a timed effect its evaluated duration and how many ticks after this packet's it ends, owner and source ids, and set-by-caller values.
- **How often it executed**, when that changes.

A removal is the instance number and whether the effect expired. A record costs about 20 bytes the first time, and an effect in steady state costs nothing.

The instance numbers are the entity's own and never come back, so a removal can't be mistaken for another application of the same effect.
