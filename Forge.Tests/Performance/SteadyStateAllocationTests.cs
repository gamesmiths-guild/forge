// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

// Retained memory can only be read after a collection.
#pragma warning disable S1215

namespace Gamesmiths.Forge.Tests.Performance;

/// <summary>
/// Guards against regressions in what the steady state costs, not against what it costs today. Ticking an entity
/// whose effects neither expire nor execute must allocate the same from one second to the next, under a ceiling,
/// and a history at capacity must not retain more as it keeps recording. A tick may allocate nothing; the retained
/// ceiling allows for collector slack. Never loosen one without a change-log entry.
/// </summary>
/// <remarks>
/// Allocation is read per thread, so other test classes running in parallel don't count. The suite already runs
/// serially (see <c>AssemblyInfo.cs</c>), which also keeps the garbage collector's timing out of these numbers.
/// </remarks>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class SteadyStateAllocationTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const double Step = 1.0 / 64.0;

	private const int TicksPerWindow = 64;

	private const int EffectsPerEntity = 10;

	/// <summary>
	/// Bytes one entity with <see cref="EffectsPerEntity"/> idle timed effects may allocate per tick: none, since
	/// whatever a tick allocates is allocated again for every tick a reconciliation replays.
	/// </summary>
	private const long BytesPerTickCeiling = 0;

	/// <summary>
	/// Bytes a history at capacity may retain more after recording another window of ticks. Each recording
	/// replaces the oldest snapshot, so retained memory should be flat up to collector slack.
	/// </summary>
	private const long RetainedGrowthCeiling = 256 * 1024;

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Performance", "Allocation")]
	public void Ticking_idle_effects_allocates_the_same_every_second_and_under_the_ceiling()
	{
		TestEntity entity = EntityWithIdleEffects();
		long tick = 0;

		// Warm up: first-call allocations (lazily built lists, tiered JIT) don't count against the steady state.
		tick = Run(entity, tick, TicksPerWindow);

		long first = MeasureWindow(entity, ref tick);
		long second = MeasureWindow(entity, ref tick);
		long third = MeasureWindow(entity, ref tick);

		long perTick = Math.Max(first, Math.Max(second, third)) / TicksPerWindow;

		perTick.Should().BeLessThanOrEqualTo(
			BytesPerTickCeiling,
			$"a tick of {EffectsPerEntity} idle effects allocated {perTick} bytes");

		second.Should().BeLessThanOrEqualTo(first + (first / 10) + 1024, "the second window should not grow");
		third.Should().BeLessThanOrEqualTo(first + (first / 10) + 1024, "the third window should not grow");
	}

	[Fact]
	[Trait("Performance", "Allocation")]
	public void Capturing_a_snapshot_every_tick_allocates_the_same_every_second()
	{
		TestEntity entity = EntityWithIdleEffects();
		var history = new EntityHistory(entity, capacity: TicksPerWindow, Step);
		long tick = 0;

		tick = RunAndRecord(entity, history, tick, TicksPerWindow);

		long first = MeasureWindow(entity, history, ref tick);
		long second = MeasureWindow(entity, history, ref tick);

		second.Should().BeLessThanOrEqualTo(
			first + (first / 10) + 1024,
			"recording into a ring at capacity should allocate the same per window");
	}

	[Fact]
	[Trait("Performance", "Allocation")]
	public void A_history_at_capacity_retains_no_more_as_it_keeps_recording()
	{
		TestEntity entity = EntityWithIdleEffects();
		var history = new EntityHistory(entity, capacity: TicksPerWindow, Step);
		long tick = 0;

		// Reach capacity, then one more window so that every slot has been replaced at least once.
		tick = RunAndRecord(entity, history, tick, TicksPerWindow * 2);

		long before = RetainedBytes();
		tick = RunAndRecord(entity, history, tick, TicksPerWindow * 4);
		long after = RetainedBytes();

		history.OldestTick.Should().Be(tick - TicksPerWindow + 1);
		(after - before).Should().BeLessThanOrEqualTo(
			RetainedGrowthCeiling,
			$"recording {TicksPerWindow * 4} ticks into a full ring retained {after - before} more bytes");

		GC.KeepAlive(history);
	}

	[Fact]
	[Trait("Performance", "Allocation")]
	public void A_world_history_at_capacity_retains_no_more_as_it_keeps_recording()
	{
		var world = new WorldHistory(TicksPerWindow, Step);

		for (int i = 0; i < 8; i++)
		{
			world.Add(EntityWithIdleEffects());
		}

		long tick = 0;
		tick = RunAndRecord(world, tick, TicksPerWindow * 2);

		long before = RetainedBytes();
		tick = RunAndRecord(world, tick, TicksPerWindow * 4);
		long after = RetainedBytes();

		world.OldestTick.Should().Be(tick - TicksPerWindow + 1);
		(after - before).Should().BeLessThanOrEqualTo(
			RetainedGrowthCeiling,
			$"recording {TicksPerWindow * 4} ticks into a full world ring retained {after - before} more bytes");

		GC.KeepAlive(world);
	}

	private static long MeasureWindow(TestEntity entity, ref long tick)
	{
		long start = GC.GetAllocatedBytesForCurrentThread();
		tick = Run(entity, tick, TicksPerWindow);
		return GC.GetAllocatedBytesForCurrentThread() - start;
	}

	private static long MeasureWindow(TestEntity entity, EntityHistory history, ref long tick)
	{
		long start = GC.GetAllocatedBytesForCurrentThread();
		tick = RunAndRecord(entity, history, tick, TicksPerWindow);
		return GC.GetAllocatedBytesForCurrentThread() - start;
	}

	private static long Run(TestEntity entity, long from, int ticks)
	{
		long tick = from;

		for (int i = 0; i < ticks; i++)
		{
			tick++;
			EntitySimulation.Tick(entity, tick, Step);
		}

		return tick;
	}

	private static long RunAndRecord(TestEntity entity, EntityHistory history, long from, int ticks)
	{
		long tick = from;

		for (int i = 0; i < ticks; i++)
		{
			tick++;
			EntitySimulation.Tick(entity, tick, Step);
			history.Record(tick);
		}

		return tick;
	}

	private static long RunAndRecord(WorldHistory world, long from, int ticks)
	{
		long tick = from;

		for (int i = 0; i < ticks; i++)
		{
			tick++;
			EntitySimulation.Tick(world.Entities, tick, Step);
			world.Record(tick);
		}

		return tick;
	}

	private static long RetainedBytes()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		return GC.GetTotalMemory(forceFullCollection: true);
	}

	private TestEntity EntityWithIdleEffects()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var entity = new TestEntity(_tagsManager, _cuesManager);

		// Long enough never to expire during a test, with no period, so a tick only counts time down.
		var data = new EffectData(
			"Idle",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(1_000_000))),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(1)))
			]);

		for (int i = 0; i < EffectsPerEntity; i++)
		{
			entity.EffectsManager.ApplyEffect(new Effect(data, new EffectOwnership(source, source)));
		}

		return entity;
	}
}
