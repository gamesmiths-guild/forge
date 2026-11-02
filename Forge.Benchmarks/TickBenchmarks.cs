// Copyright © Gamesmiths Guild.

using BenchmarkDotNet.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Benchmarks;

/// <summary>
/// The steady state: a crowd of entities each carrying a few long-lived effects, ticked for ten seconds at 60 Hz.
/// Divide the allocated bytes by <c>Entities × Ticks</c> for the per-entity, per-tick figure. That number is paid
/// once per tick offline, and once per replayed tick on every reconciliation once prediction lands.
/// </summary>
[MemoryDiagnoser]
public class TickBenchmarks
{
	private const int Ticks = 600;

	private const int EffectsPerEntity = 10;

	private TagsManager _tagsManager = null!;
	private CuesManager _cuesManager = null!;
	private BenchmarkEntity[] _entities = null!;

	[Params(100, 1_000)]
	public int Entities { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the effects tick a period. Periodic effects execute every eighth of a
	/// second, so they exercise the attribute change path on top of the duration countdown.
	/// </summary>
	[Params(false, true)]
	public bool Periodic { get; set; }

	[GlobalSetup]
	public void GlobalSetup()
	{
		_tagsManager = BenchmarkWorld.CreateTagsManager();
		_cuesManager = new CuesManager();
	}

	[IterationSetup]
	public void IterationSetup()
	{
		_entities = new BenchmarkEntity[Entities];
		var source = new BenchmarkEntity(_tagsManager, _cuesManager, seed: 1);

		// Long enough that nothing expires during the run: this measures ticking, not expiry.
		EffectData data = Periodic
			? BenchmarkWorld.Bleeding(seconds: 1_000)
			: BenchmarkWorld.Buff(seconds: 1_000);

		for (int i = 0; i < Entities; i++)
		{
			var entity = new BenchmarkEntity(_tagsManager, _cuesManager, seed: i + 2);

			for (int j = 0; j < EffectsPerEntity; j++)
			{
				entity.EffectsManager.ApplyEffect(BenchmarkWorld.From(data, source));
			}

			_entities[i] = entity;
		}
	}

	[Benchmark]
	public long TickWorld()
	{
		long checksum = 0;

		for (long tick = 1; tick <= Ticks; tick++)
		{
			EntitySimulation.Tick(_entities, tick, BenchmarkWorld.Step);
		}

		foreach (BenchmarkEntity entity in _entities)
		{
			checksum += entity.AttributeSet.Health.CurrentValue;
		}

		return checksum;
	}
}
