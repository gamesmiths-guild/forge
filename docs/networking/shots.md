# Shots

A shot that flies takes many ticks to land. As an entity, it would send its body every send while it flies. A **lightweight projectile** sends nothing while it flies: its path follows from its launch, so every machine that hears of the launch flies it along the same points. The server sends each client the launch, and the end once there is one. Forge keeps the flights, rolls them back with the entity that launched them, and replicates them. The engine integration tests the path against its world and shows it.

For a projectile that needs effects, tags or a graph of its own, spawn an entity instead ([Spawning](spawning.md)). A hitscan, a ray that hits at once, needs nothing from Forge but the [lag compensation](lag-compensation.md) its test runs in and the seeded random its spread draws from (below).

## Launching

An entity that launches projectiles has `EntityShots`, returned by `IForgeEntity.Shots`:

```csharp
public sealed class Player : IForgeEntity
{
    public Player()
    {
        // ...
        Shots = new EntityShots(this) { Host = new MyShotHost(this) };
    }

    public EntityShots Shots { get; }

    EntityShots? IForgeEntity.Shots => Shots;
}
```

`ProjectileData` is the kind of projectile: its name, the `Gravity` it falls by, its `Radius`, how long it flies (`Lifetime`), and whether a shot can hit it in flight (`Shootable`). What it does when it hits is the host's.

```csharp
var rocket = new ProjectileData("Rocket", gravity: Vector3.Zero, radius: 0.2f, lifetime: 3);

ProjectileFlight flight = player.Shots.Launch(rocket, origin, direction * 30);
```

`Launch` belongs in one of the entity's ticks, as an ability's behavior runs. It throws before the entity's first tick, and on an entity a client mirrors, which only shows the server's launches. A flight is named by a `ShotId`: the tick it was launched in and how many the entity launched before it in that tick. The server and the client that predicts the entity launch the same projectiles in the same order, so they name them alike. A launch in the entity's tick that its client doesn't predict, as a server-only ability's, counts down from -1 instead, so it doesn't shift the names of those the client does.

## Flying

`ProjectileFlight` holds a launch and, once there is one, its end. Its path is a function of the launch alone:

```
position(t) = Origin + Velocity × t + ½ × Gravity × t²      t = seconds since the end of the launch tick
```

`PositionAt(age)` and `PositionAt(tick, fixedDeltaTime)` give a point of it, `FliesAt(tick)` whether the projectile was in flight then, and `TestRay` whether a ray meets its sphere at a point.

The entity's [tick](../simulation.md#advancing-an-entity) flies its projectiles after its abilities. Each flight launched before the tick has that tick's stretch of path passed to the host's `IShotHost.Fly`, which tests it against the world, applies what the projectile does to what it hits, and says whether the flight ends there:

| `ShotStep` | Means |
|---|---|
| `Flying` | Nothing to end on. |
| `Passed(pierces, strength)` | It went through colliders on the way: how many in all, and the share of its damage left. The flight keeps them as `Pierces` and `Strength`, so they roll back with it. |
| `Impact(point, normal)` | It ends there. |

A flight that has flown its `Lifetime` ends as `FlightEnd.Expired`. `EntityShots.End` ends one in the entity's current tick, as `Impact` for a detonation on command or `Struck` for a projectile a shot hit. An ended flight stays in `Flights` for a second, so a presentation pass sees its end, then goes.

## Showing

`EntityShots.Present`, called from the host's presentation pass, brings what the host shows in line with the flights, by their net change: `IShotHost.Show` for a flight launched since the last pass, `ShowEnd` for one that ended since, and `Hide` for one a correction took away before its end. A flight a correction takes away and brings back as it was is shown once; one it brings back launched otherwise, or flying again after its end, is shown anew.

## Rollback

An [entity snapshot](../rollback.md#entity-snapshots) holds the entity's flights, so a restore brings them back as they were and the ticks run again fly them again, through the host. The ticks a correction runs again show nothing new: only the net change is shown.

## On the network

- **The server** records each launch and each end of a replicated entity's flights as an event, numbered with its executed cues and expiring as they do (see [Replication](replication.md)). A launch the entity's own tick made goes to everyone but the entity's owner, who launched it as it predicted it. Ends go to everyone, the owner included. The projectile travels as its place in the asset registry, `AssetKind.Projectile`, so every machine registers the same ones. A launch of one that isn't registered throws.
- **A mirror** holds the server's launches and shows them. The host shows a mirror's flight at the tick the client shows the world at, so it leaves the shooter's hand as the client saw it and reaches its end where the server ended it. One whose end never arrived ends at its lifetime, once the client's shown tick takes it past, and the server's end takes the place of that if it arrives later. An end for a flight the client never heard of, as when the launch arrived too late, is shown alone.
- **The owner** predicts its entity's projectiles with the rest of it. The server's end holds through corrections: the flight ends where and when the server's did, even if the owner had ended it elsewhere, and the ticks a correction runs again fly it untested to that end. A launch the owner didn't predict, which the server sends it, holds through corrections too. An end for a flight it never heard of is shown alone, as on a mirror. Flights aren't part of the [prediction check](prediction.md#checking-a-tick): the server's word on a flight is its end.

## Spread

`ShotSpread.Spread` spreads directions over a cone around an aim, each equally likely anywhere in the cone's circle; `Spread2D` over an arc. Pass the entity's seeded random, which the server and the client that predicts the entity start the same way each tick, so a shotgun's pellets spread alike on both:

```csharp
Span<Vector3> pellets = stackalloc Vector3[8];
ShotSpread.Spread(entity.Random!, aim, MathF.PI / 30, pellets);
```

## Shots that hit projectiles

A ray tested against the world can test the projectiles that can be shot too, where its shooter saw them. A projectile's place at any tick follows from its launch, so it needs no history: the shooter's own projectiles where its last tick left them, the others' at the tick its client showed the world at, which on the server is the [lag compensation](lag-compensation.md)'s `ShownTick`. The engine integration does this for its hitscans, and ends the projectile hit with `End(id, FlightEnd.Struck, point)`. On a client, only the projectiles of its own entity end there; another's are the server's.
