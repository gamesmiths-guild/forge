// Copyright © Gamesmiths Guild.

using BenchmarkDotNet.Attributes;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Benchmarks;

/// <summary>
/// What prediction pays. A snapshot is taken every tick for every predicted entity, and a reconciliation restores
/// one and replays every tick since: about 12 of them at 200 ms, all inside one frame.
/// </summary>
[MemoryDiagnoser]
public class RollbackBenchmarks
{
	/// <summary>
	/// About one round trip at 200 ms and 60 Hz.
	/// </summary>
	private const int ReplayDepth = 12;

	private const int Capacity = 64;

	private TagsManager _tagsManager = null!;
	private CuesManager _cuesManager = null!;
	private BenchmarkEntity _entity = null!;
	private EntityHistory _history = null!;
	private WorldHistory _world = null!;
	private long _tick;

	[Params(1, 50)]
	public int WorldSize { get; set; }

	[GlobalSetup]
	public void GlobalSetup()
	{
		_tagsManager = BenchmarkWorld.CreateTagsManager();
		_cuesManager = new CuesManager();
	}

	[IterationSetup]
	public void IterationSetup()
	{
		var source = new BenchmarkEntity(_tagsManager, _cuesManager, seed: 1);

		_world = new WorldHistory(Capacity, BenchmarkWorld.Step);

		for (int i = 0; i < WorldSize; i++)
		{
			BenchmarkEntity entity = Populate(new BenchmarkEntity(_tagsManager, _cuesManager, seed: i + 2), source);
			_world.Add(entity);

			if (i == 0)
			{
				_entity = entity;
			}
		}

		_history = new EntityHistory(_entity, Capacity, BenchmarkWorld.Step);

		// Fill the window, so every measured rollback finds a snapshot and the ring is at capacity.
		for (_tick = 1; _tick <= Capacity; _tick++)
		{
			EntitySimulation.Tick(_world.Entities, _tick, BenchmarkWorld.Step);
			_history.Record(_tick);
			_world.Record(_tick);
		}

		_tick--;
	}

	/// <summary>
	/// One tick's snapshot of one entity: the per-tick cost of being predicted, before any rollback.
	/// </summary>
	[Benchmark]
	public EntitySnapshot CaptureSnapshot()
	{
		return EntitySimulation.CaptureSnapshot(_entity);
	}

	/// <summary>
	/// Restoring the snapshot taken a round trip ago, without replaying: the fixed cost of every correction.
	/// </summary>
	[Benchmark]
	public long RestoreOneEntity()
	{
		_history.Rollback(_tick - ReplayDepth);
		return _tick;
	}

	/// <summary>
	/// A full reconciliation of one entity: restore the tick a round trip ago and replay to the present.
	/// </summary>
	[Benchmark]
	public long ReplayOneEntity()
	{
		_history.Rollback(_tick - ReplayDepth);
		_history.Replay(_tick);
		return _tick;
	}

	/// <summary>
	/// The whole world rolled back a round trip and replayed, as a listen host with every entity predicted would.
	/// </summary>
	[Benchmark]
	public int ReplayWorld()
	{
		_world.Rollback(_tick - ReplayDepth);
		return _world.Replay(_tick).Count;
	}

	/// <summary>
	/// The checksum of one tick of the whole world: what a desync check costs per tick.
	/// </summary>
	[Benchmark]
	public ulong ChecksumWorld()
	{
		return _world.ChecksumAt(_tick);
	}

	private static BenchmarkEntity Populate(BenchmarkEntity entity, BenchmarkEntity source)
	{
		entity.EffectsManager.ApplyEffect(BenchmarkWorld.From(BenchmarkWorld.Buff(seconds: 1_000), source));
		entity.EffectsManager.ApplyEffect(BenchmarkWorld.From(BenchmarkWorld.Buff(seconds: 1_000), source));
		entity.EffectsManager.ApplyEffect(
			BenchmarkWorld.From(BenchmarkWorld.Burning(seconds: 1_000, stackLimit: 3), source));
		entity.EffectsManager.ApplyEffect(
			BenchmarkWorld.From(BenchmarkWorld.Burning(seconds: 1_000, stackLimit: 3), source));

		AbilityHandle pulse = entity.Abilities.GrantAbilityPermanently(
			BenchmarkWorld.PulseAbility(),
			1,
			LevelComparison.None,
			entity);
		pulse.TryActivate(out _);

		return entity;
	}
}
