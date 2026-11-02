// Copyright © Gamesmiths Guild.

using BenchmarkDotNet.Attributes;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Benchmarks;

/// <summary>
/// Many effects on one entity: the apply path, the stacking lookup, and letting a crowd of timed effects expire at
/// once. The allocation column matters as much as the time: anything allocated per application is allocated again
/// on every replayed tick once prediction lands.
/// </summary>
[MemoryDiagnoser]
public class EffectBenchmarks
{
	private TagsManager _tagsManager = null!;
	private CuesManager _cuesManager = null!;
	private BenchmarkEntity _target = null!;
	private Effect _damage = null!;
	private Effect _buff = null!;
	private Effect _burning = null!;

	[Params(1_000, 10_000)]
	public int Count { get; set; }

	[GlobalSetup]
	public void GlobalSetup()
	{
		_tagsManager = BenchmarkWorld.CreateTagsManager();
		_cuesManager = new CuesManager();
		var source = new BenchmarkEntity(_tagsManager, _cuesManager, seed: 1);
		_damage = BenchmarkWorld.From(BenchmarkWorld.Damage(1), source);
		_buff = BenchmarkWorld.From(BenchmarkWorld.Buff(1), source);
		_burning = BenchmarkWorld.From(BenchmarkWorld.Burning(1, stackLimit: int.MaxValue), source);
	}

	[IterationSetup]
	public void IterationSetup()
	{
		_target = new BenchmarkEntity(_tagsManager, _cuesManager, seed: 2);
	}

	/// <summary>
	/// Instant damage, the most common effect in any game. Nothing should stay behind on the target.
	/// </summary>
	[Benchmark]
	public int InstantDamage()
	{
		for (int i = 0; i < Count; i++)
		{
			_target.EffectsManager.ApplyEffect(_damage);
		}

		return _target.AttributeSet.Health.CurrentValue;
	}

	/// <summary>
	/// Distinct timed applications of one shared <see cref="Effect"/>. Each subscribes to the effect's level and
	/// set-by-caller events, so this grows the shared effect's subscriber lists.
	/// </summary>
	[Benchmark]
	public int TimedBuffs()
	{
		for (int i = 0; i < Count; i++)
		{
			_target.EffectsManager.ApplyEffect(_buff);
		}

		return _target.AttributeSet.Speed.CurrentValue;
	}

	/// <summary>
	/// The buffs above, then enough ticks for all of them to expire in the same tick. Mass expiry walks the
	/// subscriber lists back down.
	/// </summary>
	[Benchmark]
	public int TimedBuffsThenExpire()
	{
		for (int i = 0; i < Count; i++)
		{
			_target.EffectsManager.ApplyEffect(_buff);
		}

		const int Ticks = (int)(1 / BenchmarkWorld.Step) + 1;

		for (int i = 0; i < Ticks; i++)
		{
			_target.EffectsManager.UpdateEffects(BenchmarkWorld.Step);
		}

		return _target.AttributeSet.Speed.CurrentValue;
	}

	/// <summary>
	/// One stacking effect applied over and over. Every application looks the existing stack up among the active
	/// effects first.
	/// </summary>
	[Benchmark]
	public int StackingApplications()
	{
		for (int i = 0; i < Count; i++)
		{
			_target.EffectsManager.ApplyEffect(_burning);
		}

		return _target.AttributeSet.Health.CurrentValue;
	}

	/// <summary>
	/// The stack above, looked up by every query path a game uses: by data, by handle and by query.
	/// </summary>
	[Benchmark]
	public int StackingThenQuery()
	{
		for (int i = 0; i < Count; i++)
		{
			_target.EffectsManager.ApplyEffect(_burning);
		}

		int found = 0;

		for (int i = 0; i < 100; i++)
		{
			found += _target.EffectsManager.GetEffectStackData(_burning.EffectData).Count();
			found += _target.EffectsManager.GetActiveEffects().Count();
		}

		return found;
	}
}
