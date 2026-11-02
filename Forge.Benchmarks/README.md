# Forge.Benchmarks

Performance baselines for the core, built on [BenchmarkDotNet](https://benchmarkdotnet.org/). They measure the paths networking multiplies: applying effects, ticking a crowd of entities, and snapshotting, restoring and replaying them.

They are **not** part of CI. Hosted runners are too noisy for numbers to compare across runs, so run them on your own machine and record the results in the milestone's explanation.

## Running

From the repository root:

```bash
dotnet run -c Release --project Forge.Benchmarks -- --filter '*'
```

One suite, or one benchmark:

```bash
dotnet run -c Release --project Forge.Benchmarks -- --filter '*EffectBenchmarks*'
```

```bash
dotnet run -c Release --project Forge.Benchmarks -- --filter '*ReplayOneEntity*'
```

A quick, less precise pass while iterating on a change:

```bash
dotnet run -c Release --project Forge.Benchmarks -- --filter '*' --job short
```

Results land in `BenchmarkDotNet.Artifacts/results/` as Markdown, HTML and CSV. The folder is ignored by git.

## What each suite measures

| Suite | Scenario | Watch for |
|---|---|---|
| `EffectBenchmarks` | 1,000 and 10,000 effects on one entity: instant damage, timed buffs, timed buffs expiring together, a stacking effect, and the stack queried | Time growing faster than the count (a linear scan or subscriber list walked per application), and bytes per application |
| `TickBenchmarks` | 100 and 1,000 entities with 10 effects each, 600 ticks, with and without periods | Bytes allocated divided by `Entities × 600`: the per-entity, per-tick figure. The target is for it to come from the effects' own work, not from the tick itself |
| `RollbackBenchmarks` | A world of 1 and 50 entities with effects and a running graph: one snapshot, one restore, a 12-tick replay of one entity, a 12-tick replay of the world, and one checksum | The replay figures are what a client pays per server update at 200 ms. The world figures are what a listen host pays |

## Reading the numbers

- **Mean** is the time per operation; **Allocated** is managed memory per operation. Both matter. Steady-state allocation feeds the garbage collector, and a collection in the middle of a 12-tick replay is a hitch.
- Compare against the previous baseline for the same machine, not across machines.
- A change that moves a figure by less than the run's error column didn't move it.

## Adding a benchmark

Keep the scenario shaped like a game rather than like the cheapest call: real attribute sets, a few tags, effects with durations. Put shared setup in `BenchmarkWorld`, mark the class `[MemoryDiagnoser]`, and return something the benchmark computed so the work isn't optimized away.
