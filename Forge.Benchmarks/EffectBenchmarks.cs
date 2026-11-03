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
/// <remarks>
/// Each operation builds its own target and its own <see cref="Effect"/>, rather than an iteration setup doing it, so
/// BenchmarkDotNet can run many operations per iteration, and nothing one operation applied stays on an effect the
/// next one shares.
/// </remarks>
[MemoryDiagnoser]
public class EffectBenchmarks
{
	private TagsManager _tagsManager = null!;
	private CuesManager _cuesManager = null!;
	private BenchmarkEntity _source = null!;
	private EffectData _damage;
	private EffectData _buff;
	private EffectData _scaledBuff;
	private EffectData _burning;

	[Params(1_000, 10_000)]
	public int Count { get; set; }

	[GlobalSetup]
	public void GlobalSetup()
	{
		_tagsManager = BenchmarkWorld.CreateTagsManager();
		_cuesManager = new CuesManager();
		_source = new BenchmarkEntity(_tagsManager, _cuesManager, seed: 1);
		_damage = BenchmarkWorld.Damage(1);
		_buff = BenchmarkWorld.Buff(1);
		_scaledBuff = BenchmarkWorld.ScaledBuff(1);
		_burning = BenchmarkWorld.Burning(1, stackLimit: int.MaxValue);
	}

	/// <summary>
	/// Instant damage, the most common effect in any game. Nothing should stay behind on the target.
	/// </summary>
	/// <returns>The target's health after the damage.</returns>
	[Benchmark]
	public int InstantDamage()
	{
		BenchmarkEntity target = Apply(_damage);

		return target.AttributeSet.Health.CurrentValue;
	}

	/// <summary>
	/// Distinct timed applications of one shared <see cref="Effect"/>. Each joins the shared effect's list of live
	/// applications.
	/// </summary>
	/// <returns>The target's speed after the buff.</returns>
	[Benchmark]
	public int TimedBuffs()
	{
		BenchmarkEntity target = Apply(_buff);

		return target.AttributeSet.Speed.CurrentValue;
	}

	/// <summary>
	/// The buffs above, then enough ticks for all of them to expire in the same tick. Mass expiry takes every
	/// application off the shared effect's list.
	/// </summary>
	/// <returns>The target's speed after the buff expires.</returns>
	[Benchmark]
	public int TimedBuffsThenExpire()
	{
		BenchmarkEntity target = Apply(_buff);
		Expire(target);

		return target.AttributeSet.Speed.CurrentValue;
	}

	/// <summary>
	/// Timed buffs whose magnitude follows the source's speed, expiring together. Each application watches that one
	/// attribute through its change event, so a crowd ending at once walks the attribute's subscriber list back down.
	/// </summary>
	/// <returns>The target's speed after the scaled buff expires.</returns>
	[Benchmark]
	public int ScaledBuffsThenExpire()
	{
		BenchmarkEntity target = Apply(_scaledBuff);
		Expire(target);

		return target.AttributeSet.Speed.CurrentValue;
	}

	/// <summary>
	/// One stacking effect applied over and over. Every application looks the existing stack up among the active
	/// effects first.
	/// </summary>
	/// <returns>The target's health after the stacking applications.</returns>
	[Benchmark]
	public int StackingApplications()
	{
		BenchmarkEntity target = Apply(_burning);

		return target.AttributeSet.Health.CurrentValue;
	}

	/// <summary>
	/// The stack above, looked up by every query path a game uses: by data, by handle and by query.
	/// </summary>
	/// <returns>The number of found effects.</returns>
	[Benchmark]
	public int StackingThenQuery()
	{
		BenchmarkEntity target = Apply(_burning);
		int found = 0;

		for (int i = 0; i < 100; i++)
		{
			found += target.EffectsManager.GetEffectStackData(_burning).Count();
			found += target.EffectsManager.GetActiveEffects().Count();
		}

		return found;
	}

	private static void Expire(BenchmarkEntity target)
	{
		const int Ticks = (int)(1 / BenchmarkWorld.Step) + 1;

		for (int i = 0; i < Ticks; i++)
		{
			target.EffectsManager.UpdateEffects(BenchmarkWorld.Step);
		}
	}

	// A fresh target with Count applications of a fresh effect made from the data.
	private BenchmarkEntity Apply(EffectData data)
	{
		var target = new BenchmarkEntity(_tagsManager, _cuesManager, seed: 2);
		Effect effect = BenchmarkWorld.From(data, _source);

		for (int i = 0; i < Count; i++)
		{
			target.EffectsManager.ApplyEffect(effect);
		}

		return target;
	}
}
