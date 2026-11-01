// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Core;

/// <summary>
/// A world replay runs recorded ticks again from the recording and reports the ones that come out differently, and the
/// world's checksum says whether two ticks left a world the same, whichever objects make it up.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class WorldReplayTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const double Step = 1.0 / 64.0;

	private const string Attribute90 = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = fixture.TagsManager;

	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Simulation", "Replay")]
	public void A_replay_of_a_world_that_runs_the_same_finds_nothing_and_leaves_it_as_recorded()
	{
		(WorldHistory world, TestEntity walker, TestEntity[] all) = BurningWorld();
		Run(world, walker, 1, 24);
		ulong[] checksums = [.. Enumerable.Range(1, 24).Select(x => world.ChecksumAt(x))];
		string end = Describe(all);

		world.Rollback(3);

		// What the host submits live gives way to the recorded frame.
		walker.Input!.Submit(default);
		world.Replay(24).Should().BeEmpty();

		world.CurrentTick.Should().Be(24);
		Describe(all).Should().Be(end);
		Enumerable.Range(1, 24).Select(x => world.ChecksumAt(x)).Should().Equal(checksums, "the history doesn't change");
	}

	[Fact]
	[Trait("Simulation", "Replay")]
	public void A_replay_reports_each_tick_that_ran_differently_and_carries_on_from_the_recording()
	{
		(WorldHistory world, TestEntity walker, TestEntity[] all) = BurningWorld();

		for (long tick = 1; tick <= 24; tick++)
		{
			// Applied from outside the simulation, which a replay can't run again.
			if (tick == 10)
			{
				Burn(all[1], all[2]);
			}

			RunTick(world, walker, tick);
		}

		world.Rollback(5);
		IReadOnlyList<WorldDesync> desyncs = world.Replay(24);

		desyncs.Select(x => x.Tick).Should().Equal(10);
		world.CurrentTick.Should().Be(24);

		string description = desyncs[0].Describe(x => x == all[2] ? "c" : "?");
		description.Should().Contain("c (entity 2):");
		description.Should().Contain("Effects[1]/Effect: Burning recorded, missing replayed");
		description.Should().NotContain("entity 0", "the others ran the same");
	}

	[Fact]
	[Trait("Simulation", "Replay")]
	public void A_replay_compares_an_entity_spawned_again_with_the_one_spawned_first_and_keeps_the_first()
	{
		TestEntity a = Entity();
		TestEntity spawned = Entity();
		TestEntity? spawnedAgain = null;
		var world = new WorldHistory(32, Step);
		world.Add(a);
		Burn(a, a);
		var released = new List<IForgeEntity>();
		world.Released += released.Add;

		for (long tick = 1; tick <= 12; tick++)
		{
			EntitySimulation.Tick(world.Entities, tick, Step);

			if (tick == 6)
			{
				world.Add(spawned);
			}

			world.Record(tick);
		}

		world.Rollback(3);
		IReadOnlyList<WorldDesync> desyncs = world.Replay(12, tick =>
		{
			EntitySimulation.Tick(world.Entities, tick, Step);

			if (tick == 6)
			{
				spawnedAgain = Entity();
				world.Add(spawnedAgain);
			}
		});

		desyncs.Should().BeEmpty();
		released.Should().Equal(spawnedAgain!);
		world.Entities.Should().Equal(a, spawned);
	}

	[Fact]
	[Trait("Simulation", "Replay")]
	public void An_entity_that_left_in_a_tick_runs_it_again_with_the_frame_it_left_with()
	{
		(WorldHistory world, TestEntity walker, _) = BurningWorld();
		InputFrame? leftWith = null;

		for (long tick = 1; tick <= 12; tick++)
		{
			RunTick(world, walker, tick, leaveOn: 8);
		}

		InputFrame recorded = Frame(walker, 8);
		world.Rollback(4);
		world.Replay(12, tick =>
		{
			EntitySimulation.Tick(world.Entities, tick, Step);

			if (tick == 8)
			{
				leftWith = walker.Input!.Current;
				world.Remove(walker);
			}
		}).Should().BeEmpty();

		leftWith.Should().Be(recorded);
	}

	[Fact]
	[Trait("Simulation", "Replay")]
	public void Worlds_left_the_same_have_the_same_checksum_whichever_objects_make_them_up()
	{
		(WorldHistory first, TestEntity firstWalker, _) = BurningWorld();
		(WorldHistory second, TestEntity secondWalker, _) = BurningWorld();
		Run(first, firstWalker, 1, 16);
		Run(second, secondWalker, 1, 16);

		for (long tick = 1; tick <= 16; tick++)
		{
			second.ChecksumAt(tick).Should().Be(first.ChecksumAt(tick), $"both worlds ran tick {tick} the same");
			first.ChecksumAt(tick).Should().Be(first.ChecksumAt(tick), "a checksum doesn't change");
		}

		first.ChecksumAt(9).Should().NotBe(first.ChecksumAt(8), "burns and steps changed the world");
	}

	[Fact]
	[Trait("Simulation", "Replay")]
	public void A_body_counts_to_a_thousandth_of_a_unit()
	{
		ulong At(float x)
		{
			TestEntity entity = Entity();
			entity.Mover = new StillMover(new Vector3(x, 0, 0));
			var world = new WorldHistory(2, Step);
			world.Add(entity);
			world.Record(1);
			return world.ChecksumAt(1);
		}

		At(1.0001f).Should().Be(At(1f), "float noise below the network's precision isn't a difference");
		At(1.002f).Should().NotBe(At(1f));
		At(-0.0001f).Should().Be(At(0f), "a value that rounds to zero is zero from either side");
	}

	[Fact]
	[Trait("Simulation", "Replay")]
	public void A_desync_names_each_variable_that_differs_even_one_sharing_its_name_or_an_empty_array()
	{
		TestEntity entity = Entity();
		entity.SharedVariables.DefineVariable("score", 0);
		entity.SharedVariables.DefineObjectVariable("score", "high");
		var world = new WorldHistory(16, Step);
		world.Add(entity);

		for (long tick = 1; tick <= 6; tick++)
		{
			// Changed from outside the simulation, which a replay can't do again.
			if (tick == 3)
			{
				entity.SharedVariables.SetVar("score", 5);
			}

			if (tick == 5)
			{
				entity.SharedVariables.DefineArrayVariable("hits", []);
			}

			EntitySimulation.Tick(world.Entities, tick, Step);
			world.Record(tick);
		}

		world.Rollback(1);
		IReadOnlyList<WorldDesync> desyncs = world.Replay(6);

		desyncs.Select(x => x.Tick).Should().Equal(3, 5);
		desyncs[0].Describe().Should().Contain("score");
		desyncs[1].Describe().Should().Contain("hits");
	}

	[Fact]
	[Trait("Simulation", "Replay")]
	public void A_replay_refuses_ticks_it_does_not_hold()
	{
		(WorldHistory world, TestEntity walker, _) = BurningWorld();
		Run(world, walker, 1, 6);

		world.Invoking(x => x.Replay(6)).Should().Throw<ArgumentOutOfRangeException>("the world is at tick 6");
		world.Invoking(x => x.ChecksumAt(7)).Should().Throw<ArgumentOutOfRangeException>();

		world.Rollback(2);

		world.Invoking(x => x.Replay(7)).Should().Throw<ArgumentOutOfRangeException>();
	}

	private static void Run(WorldHistory world, TestEntity walker, long from, long to)
	{
		for (long tick = from; tick <= to; tick++)
		{
			RunTick(world, walker, tick);
		}
	}

	private static void RunTick(WorldHistory world, TestEntity walker, long tick, long leaveOn = -1)
	{
		walker.Input!.Submit(Frame(walker, tick));
		EntitySimulation.Tick(world.Entities, tick, Step);

		if (tick == leaveOn)
		{
			world.Remove(walker);
		}

		world.Record(tick);
	}

	// Holds "right" for a few ticks at a time.
	private static InputFrame Frame(TestEntity walker, long tick)
	{
		ulong bit = 1UL << walker.Input!.Actions.IndexOf("right");
		return tick % 6 < 3 ? new InputFrame { Held = bit } : default;
	}

	private static string Describe(params IForgeEntity[] entities)
	{
		return string.Join(
			$"{Environment.NewLine}--{Environment.NewLine}",
			entities.Select(x => EntityState.Describe(x)));
	}

	// Stacks up to three, a quarter of a second each, taking health every eighth of a second.
	private static void Burn(IForgeEntity source, IForgeEntity target)
	{
		var burning = new EffectData(
			"Burning",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.25f))),
			[
				new Modifier(
					Attribute90,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-1))),
			],
			periodicData: new PeriodicData(new ScalableFloat(0.125f), false, PeriodInhibitionRemovedPolicy.NeverReset));

		target.EffectsManager.ApplyEffect(new Effect(burning, new EffectOwnership(source, source)));
	}

	// Three entities burning each other, and a walker that walks right while its input holds "right".
	private (WorldHistory World, TestEntity Walker, TestEntity[] All) BurningWorld()
	{
		TestEntity a = Entity();
		TestEntity b = Entity();
		TestEntity c = Entity();
		a.Input = new EntityInput(new InputActionSet(["right"]));
		a.Mover = new WalkingMover(a);
		Burn(a, b);
		Burn(b, c);
		Burn(c, a);

		var world = new WorldHistory(32, Step);
		world.Add(a);
		world.Add(b);
		world.Add(c);
		return (world, a, [a, b, c]);
	}

	private TestEntity Entity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}

	// Walks right at two units a second while "right" is held.
	private sealed class WalkingMover(TestEntity entity) : IEntityMover
	{
		private BodyState _body = new(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Grounded: true);

		public void Move(double fixedDeltaTime)
		{
			var velocity = new Vector3(entity.Input!.IsHeld("right") ? 2 : 0, 0, 0);
			_body = _body with { Position = _body.Position + (velocity * (float)fixedDeltaTime), Velocity = velocity };
		}

		public BodyState CaptureState()
		{
			return _body;
		}

		public void RestoreState(in BodyState state)
		{
			_body = state;
		}
	}

	private sealed class StillMover(Vector3 position) : IEntityMover
	{
		public void Move(double fixedDeltaTime)
		{
		}

		public BodyState CaptureState()
		{
			return new BodyState(position, Quaternion.Identity, Vector3.Zero, Grounded: true);
		}

		public void RestoreState(in BodyState state)
		{
		}
	}
}
