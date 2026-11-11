# Prediction

A client predicts the entities it owns: it runs their ticks ahead of the server from its own input, so its player answers at once instead of a round trip later. Each state packet from the server checks a tick against what the client predicted for it, and where the server's word differs, the client rolls the entity back, makes it what the server says and runs the ticks since again. This page covers what a client predicts, how a tick is checked, what a correction does, and what stays the server's.

## What a client predicts

An entity the server spawned for a client's peer, the client's player, isn't a mirror on that client. `ReplicationClient.Predicted` lists them. The host makes it as the scene makes it, setup included, with its body where the server's was as the server registered it (`IReplicationHost.SpawnPredicted`), so its setup runs where the server's did. It isn't torn down: its abilities, effects and input are live. Other clients mirror it as any entity. Placed entities are never a client's, and a listen host's own player runs on the server, so neither is predicted.

The host doesn't advance a predicted entity itself. `Predict` does, once per tick the client's clock simulates, and records how each tick left it, `PredictionHistoryTicks` back (by default as many as input may lead the server, `NetworkSession.MaxInputLead`):

```csharp
ReplicationClient mirror = client.Mirror!;
var dilation = new ClockDilation();

// Every fixed step:
client.Update(localTime);                           // packets arrive and put predicted entities right
DilatedStep step = dilation.Steer(client.Clock.Tick, client.ClockSync, localTime, client.ServerInputDepth);
client.Clock.Set(step.FirstTick);
player.Input!.Submit(collectedFrame);

for (int i = 0; i < step.Ticks; i++)
{
    mirror.Predict(client.Clock.Tick, client.Clock.FixedDeltaTime);
    client.SendInput(new InputCommand(client.Clock.Tick, player.Input.Current));
    client.Clock.Advance();
}
```

A tick the clock skipped runs first, with the input held and no presses or releases, as the server runs a tick whose input never arrived.

Before the clock sync's first estimate the client's clock is still at the server's tick, so input it sent would arrive after the server ran it. `Predict` does nothing until then, and the entity follows the server, run with what was last held up to each state packet's tick.

## The clock

The client's clock runs about a round trip and a margin ahead of the server's, so the input it sends for a tick arrives before the server runs that tick. `ClockDilation` steers it toward `ClockSync.TargetClientTick` a tick at a time:

| The clock is | A fixed step simulates |
|---|---|
| Within `ToleranceTicks` (1) of its target | One tick. |
| Behind | Two ticks, at most every `StepsPerCatchUp` (2) steps: input that arrives late runs with what was last held, so the client catches up fast. |
| Ahead | No tick, at most every `StepsPerAdjustment` (10) steps: running ahead only adds latency, so it slows down gently. |
| At its first estimate, as the client joins, or more than `JumpTicks` (30) behind, as after a stall | One tick, from the target: a client behind it sends input too late to be run, and the ticks a jump skips run on both machines with what was last held. |
| More than `JumpTicks` ahead | No tick, every step, until it's within them: the ticks behind the clock were simulated and their input sent, so it never goes back. |

The margin is `MarginTicks` (2), and a tick more while the server's last pong said it holds none of the client's input.

## Checking a tick

Every send, the server sends an entity's owner a **prediction checksum** of the entity as the tick left it: its attributes, its tags, the effects the owner hears of, its abilities with the runs the owner predicts, and its random. Times count in whole ticks. Shared variables, input and state outside Forge aren't in it, since they aren't sent and couldn't be put right. The body is compared on its own: within one step of its [quantization](replication.md#what-replicates) on each axis counts as the same, since a body put back can settle that far off at a contact.

The client hashes its record of that tick the same way. When they agree, the tick is confirmed (`Confirmations`). When they don't, or the server says an effect landed from elsewhere, the client corrects the entity.

## A correction

1. The entity rolls back to the packet's tick, or to an earlier tick an effect or a run from elsewhere came after.
2. What came from elsewhere and the client lacks goes in at the tick each came after, the effects before the runs they may have started, and the ticks run again to the packet's. A run goes in as the server started it: the ability activates here, on the target the server's run has.
3. At the packet's tick, the entity becomes what the server says: its body, unless within a step; its base values and ranges; the effects from elsewhere as the server has them now, the ones gone going; the runs from elsewhere the server ended, cancelled; and its own effects that the owner hears of. One the server has and the client lacks is taken in, one the client predicted that the server never applied goes, and the rest take the server's level, stacks, time and executions. Then its tags: one the server has that nothing gives the entity here becomes a base tag, as one the server's own code added, and a base tag the server lacks goes.
4. The ticks since run again, each with the input it first ran with.

`Corrections` counts them, `LastCorrectionTicks` says how many ticks the last one ran again, and `Corrected` is raised with the entity after each, so what shows it can ease from where it was shown to where it is. One that an exception stopped part way, from a node or a cue handler, is neither counted nor raised, and the exception comes out of the session's update. A frame submitted for a tick not simulated yet, as in a step the clock waited through, is kept across a correction.

What the entity shows changes only by the net change. Executed cues don't play again: every tick a correction runs again played them once already. A persistent cue taken off and put back on the same target shows nothing, or an update when it came back with other parameters. The [presentation pass](../simulation.md#presentation-pass) shows what changed in what the graphs run. A presentation action that shows something as it executes, such as a sound played once, reads `GraphContext.IsCorrecting` and shows nothing while it's true.

## What the owner predicts

The owner predicts what the entity's own tick does. What comes from elsewhere stays the server's:

| What | On the owner's machine |
|---|---|
| Its input, its movement, and the abilities its own tick activates, from an input binding or a trigger its own tick sets off, with their graphs | Predicted. |
| Effects its own tick applies to it: costs, cooldowns, a dash's speed | Predicted, executions included. |
| Effects from elsewhere: another entity's, an area's, the host's between ticks | The server's, taken in at the tick they landed after. Their duration, modifiers, tags, components and cues run here, and their executions don't: what an execution does to base values arrives as base values. Damage and death are always the server's. |
| Effects it applies to other entities | Refused: other entities are mirrors. |
| A run of an ability it predicts, started from elsewhere: by another entity's event, or a trigger an effect from elsewhere sets off | Taken in at the tick it started after, then predicted. What it showed in the ticks a correction ran again doesn't show. |
| A `ServerOnly` ability, a coded behavior that isn't an [`ISnapshotAbilityBehavior`](../rollback.md#coded-behaviors) | The server's. A graph's run shows as a copy, as it does on a mirror ([Graphs on the network](statescript.md)). Activating one here fails with `AbilityActivationFailures.ServerOnly`. |
| An `Authority` action node | Skipped: the graph goes on as if it had run. |
| A [`ServerOnly` region](statescript.md#regions) of a graph it predicts | Not run: no link is followed into it, and the checksum leaves out its nodes and the graph variables they write as outputs, whatever else writes them too. What it does to the entity arrives as the entity's state. |
| The other entities and the world | Mirrors, shown behind the server. The predicted body moves against them where they're shown. |

An effect counts as from elsewhere when it lands while the entity isn't advancing through its own tick, or comes from what the owner doesn't predict. One applied before the entity's first tick is part of how it starts, which the owner's copy runs too as the host makes it. A run counts the same way, by when it started.

## Limits

- A run the entity's own tick started is checked, not put right. One the server didn't have, or had otherwise, as when a press arrived too late to run, corrects only through what it changes, and ends as it ends here.
- A run that reads other entities, through an overlap or a query, finds them where this client shows them, behind the server, and what it finds and keeps isn't put right: while it differs from the server's, every packet corrects the entity. Make such an ability `ServerOnly`, as one that spreads fire to whoever is near.
- A run taken in from elsewhere starts without activation data.
- Only a graph's run is taken in. A coded `ISnapshotAbilityBehavior`'s run that started from elsewhere doesn't reach the owner, which is corrected at every packet until the run ends.
- A predicted graph that goes on from a `ServerOnly` region, through a link out of it or a variable it writes, goes on only on the server, and isn't put right. Keep such a region a dead end, or make the ability `ServerOnly`.
- Shared variables and state outside Forge aren't checked.
- An effect from elsewhere that the owner doesn't hear of, `ServerOnly` or unlisted, changes what the owner predicts without the owner seeing why: while it lasts, its modifiers are corrected at every packet. Its tags reach the owner as base tags, as those of a `ServerOnly` ability's run do. Let the owner hear of such effects.
- An effect applied to a new entity before its first tick by something other than its setup reaches the owner only if the owner hears of it, through the first correction.
- A body that was put right goes on from the server's body as sent, to a thousandth of a unit.

## The wire

For an entity's owner, an effect's "what it is" says whether it landed from outside the entity's ticks and, if so, how many ticks before the packet's it landed after. A run's says the same of one the owner takes in, which goes to the owner as its start and its end alone. The spawn of the owner's entity carries the body the server's had as it was registered, unrounded. The entry ends with the 64-bit prediction checksum. The runs the owner predicts, the cues its own tick executed and the persistent cues it shows itself go only to everyone else.
