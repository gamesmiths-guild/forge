// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Benchmarks;

/// <summary>
/// The entities and effects the benchmarks share. They mirror what a game does rather than what is cheapest: a
/// vital attribute set, a few tags, instant damage, timed buffs, a stacking periodic effect and a graph ability.
/// </summary>
internal static class BenchmarkWorld
{
	/// <summary>
	/// The fixed step every benchmark ticks with. A power of two, so durations authored in seconds are whole ticks.
	/// </summary>
	public const double Step = 1.0 / 64.0;

	public const string Health = "BenchmarkAttributeSet.Health";

	public const string Speed = "BenchmarkAttributeSet.Speed";

	public static TagsManager CreateTagsManager()
	{
		return new TagsManager(
		[
			"enemy",
			"enemy.undead",
			"player",
			"status.burning",
			"status.slowed",
		]);
	}

	public static EffectData Damage(float amount)
	{
		return new EffectData("Damage", new DurationData(DurationType.Instant), [Flat(Health, -amount)]);
	}

	public static EffectData Buff(float seconds)
	{
		return new EffectData("Haste", Seconds(seconds), [Flat(Speed, 1)]);
	}

	/// <summary>
	/// A timed buff worth the source's speed, read live rather than when applied, so each application watches the
	/// source's speed for changes.
	/// </summary>
	/// <param name="seconds">The duration of the buff.</param>
	public static EffectData ScaledBuff(float seconds)
	{
		return new EffectData(
			"Tailwind",
			Seconds(seconds),
			[
				new Modifier(
					Speed,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(Speed, AttributeCaptureSource.Source, Snapshot: false),
							AttributeCalculationType.CurrentValue,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			]);
	}

	/// <summary>
	/// A timed effect that also executes every eighth of a second, without stacking, so ten applications are ten
	/// live effects that each tick a period.
	/// </summary>
	/// <param name="seconds">The duration of the effect.</param>
	public static EffectData Bleeding(float seconds)
	{
		return new EffectData(
			"Bleeding",
			Seconds(seconds),
			[Flat(Health, -1)],
			periodicData: new PeriodicData(new ScalableFloat(0.125f), false, PeriodInhibitionRemovedPolicy.NeverReset));
	}

	public static EffectData Burning(float seconds, int stackLimit)
	{
		return new EffectData(
			"Burning",
			Seconds(seconds),
			[Flat(Health, -1)],
			new StackingData(
				new ScalableInt(stackLimit),
				new ScalableInt(1),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.AggregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.ClearEntireStack,
				ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
				ApplicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.NeverReset),
			new PeriodicData(new ScalableFloat(0.125f), false, PeriodInhibitionRemovedPolicy.NeverReset));
	}

	/// <summary>
	/// A graph ability that hits its owner every eighth of a second for as long as it runs, so a snapshot has a
	/// running graph with a timer and an applied effect to capture.
	/// </summary>
	public static AbilityData PulseAbility()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectProperty("owner", new AbilityOwnerResolver());
		graph.VariableDefinitions.DefineVariable("interval", 0.125);
		graph.VariableDefinitions.DefineObjectProperty("pulseHit", new EffectFromDataResolver(Damage(1)));

		var loop = new LoopTimerNode();
		loop.BindInput(LoopTimerNode.IntervalInput, "interval");
		graph.AddNode(loop);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			loop.InputPorts[ActionNode.InputPort]));

		var hit = new ApplyEffectNode();
		hit.BindInput(ApplyEffectNode.EffectInput, "pulseHit");
		hit.BindInput(ApplyEffectNode.TargetInput, "owner");
		graph.AddNode(hit);
		graph.AddConnection(new Connection(
			loop.OutputPorts[LoopTimerNode.OnIntervalPort],
			hit.InputPorts[ActionNode.InputPort]));

		return new AbilityData("Pulse", behaviorFactory: () => new GraphAbilityBehavior(graph));
	}

	public static Effect From(EffectData data, IForgeEntity source)
	{
		return new Effect(data, new EffectOwnership(source, source));
	}

	private static DurationData Seconds(float seconds)
	{
		return new DurationData(
			DurationType.HasDuration,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(seconds)));
	}

	private static Modifier Flat(string attribute, float value)
	{
		return new Modifier(
			attribute,
			ModifierOperation.FlatBonus,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(value)));
	}
}
