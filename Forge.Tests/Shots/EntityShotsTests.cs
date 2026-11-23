// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Shots;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Shots;

/// <summary>
/// An entity's lightweight projectiles fly in its ticks along a path their launch decides, end on what the host says
/// they hit or at their lifetime, roll back with the entity, and show by their net change.
/// </summary>
public class EntityShotsTests
{
	private const double Step = 1.0 / 64;

	private static readonly ProjectileData _ball = new("Ball", Vector3.Zero, 0.5f, 1);

	private static readonly ProjectileData _grenade = new("Grenade", new Vector3(0, -10, 0), 0.25f, 2);

	[Fact]
	[Trait("Shots", "Flight")]
	public void A_projectile_flies_its_path_one_stretch_a_tick_after_its_launch()
	{
		var entity = new Shooter();
		entity.Tick(0);
		ProjectileFlight flight = entity.Shots.Launch(_ball, Vector3.Zero, new Vector3(64, 0, 0));

		entity.Tick(1);
		entity.Tick(2);

		flight.Id.Should().Be(new ShotId(0, 0));
		entity.Host.Stretches.Should().Equal(
			(new Vector3(0, 0, 0), new Vector3(1, 0, 0)),
			(new Vector3(1, 0, 0), new Vector3(2, 0, 0)));
	}

	[Fact]
	[Trait("Shots", "Flight")]
	public void A_falling_projectile_follows_its_launch_and_gravity()
	{
		var flight = new ProjectileFlight(
			new ShotId(10, 0),
			_grenade,
			new Shooter(),
			Vector3.Zero,
			new Vector3(4, 5, 0));

		flight.PositionAt(1).Should().Be(new Vector3(4, 0, 0));
		flight.PositionAt(12, 0.5).Should().Be(new Vector3(4, 0, 0));
	}

	[Fact]
	[Trait("Shots", "Flight")]
	public void Launches_in_one_tick_are_named_in_order()
	{
		var entity = new Shooter();
		entity.Tick(5);

		ProjectileFlight first = entity.Shots.Launch(_ball, Vector3.Zero, Vector3.UnitX);
		ProjectileFlight second = entity.Shots.Launch(_ball, Vector3.Zero, Vector3.UnitY);
		entity.Tick(6);
		ProjectileFlight third = entity.Shots.Launch(_ball, Vector3.Zero, Vector3.UnitZ);

		first.Id.Should().Be(new ShotId(5, 0));
		second.Id.Should().Be(new ShotId(5, 1));
		third.Id.Should().Be(new ShotId(6, 0));
	}

	[Fact]
	[Trait("Shots", "Flight")]
	public void A_projectile_ends_where_the_host_says_it_hit_or_at_its_lifetime()
	{
		var entity = new Shooter();
		entity.Tick(0);
		entity.Shots.Launch(_ball, Vector3.Zero, new Vector3(64, 0, 0));
		entity.Shots.Launch(_ball, new Vector3(0, 5, 0), new Vector3(64, 0, 0));
		entity.Host.ImpactAtX = 2.5f;

		for (int tick = 1; tick <= 64; tick++)
		{
			entity.Tick(tick);
		}

		ProjectileFlight hit = entity.Shots.Flights[0];
		ProjectileFlight expired = entity.Shots.Flights[1];
		hit.End.Should().Be(FlightEnd.Impact);
		hit.EndTick.Should().Be(3);
		hit.EndPoint.Should().Be(new Vector3(2.5f, 0, 0));
		expired.End.Should().Be(FlightEnd.Expired);
		expired.EndTick.Should().Be(64);
		expired.EndPoint.Should().Be(new Vector3(64, 5, 0));
	}

	[Fact]
	[Trait("Shots", "Flight")]
	public void Ended_flights_go_a_second_after_their_end()
	{
		var entity = new Shooter();
		entity.Tick(0);
		entity.Shots.Launch(_ball, Vector3.Zero, Vector3.UnitX);
		entity.Tick(1);
		entity.Shots.End(new ShotId(0, 0), FlightEnd.Struck, Vector3.One).Should().BeTrue();

		entity.Tick(65);
		entity.Shots.Flights.Should().ContainSingle();
		entity.Tick(66);
		entity.Shots.Flights.Should().BeEmpty();
	}

	[Fact]
	[Trait("Shots", "Rollback")]
	public void A_restore_brings_the_flights_back_and_the_ticks_run_again_end_them_alike()
	{
		var entity = new Shooter();
		entity.Tick(0);
		entity.Shots.Launch(_ball, Vector3.Zero, new Vector3(64, 0, 0));
		entity.Host.ImpactAtX = 2.5f;
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

		entity.Tick(1);
		entity.Tick(2);
		entity.Tick(3);
		ProjectileFlight ended = entity.Shots.Flights.Single();
		entity.Shots.Launch(_ball, Vector3.Zero, Vector3.UnitY);

		EntitySimulation.RestoreSnapshot(entity, snapshot);
		entity.Shots.Flights.Should().ContainSingle().Which.IsFlying.Should().BeTrue();

		entity.Tick(1);
		entity.Tick(2);
		entity.Tick(3);
		entity.Shots.Flights.Should().Equal(ended);
	}

	[Fact]
	[Trait("Shots", "Presentation")]
	public void What_shows_follows_the_net_change()
	{
		var entity = new Shooter();
		entity.Tick(0);
		entity.Shots.Launch(_ball, Vector3.Zero, new Vector3(64, 0, 0));
		EntitySnapshot beforeSecond = EntitySimulation.CaptureSnapshot(entity);
		entity.Shots.Launch(_ball, Vector3.Zero, Vector3.UnitY);
		entity.Shots.Present();

		entity.Host.ImpactAtX = 1.5f;
		EntitySimulation.RestoreSnapshot(entity, beforeSecond);
		entity.Tick(1);
		entity.Tick(2);
		entity.Shots.Present();
		entity.Shots.Present();

		entity.Host.Log.Should().Equal(
			"show 0:0",
			"show 0:1",
			"end 0:0 Impact",
			"hide 0:1");
	}

	[Fact]
	[Trait("Shots", "Presentation")]
	public void A_flight_that_launched_and_ended_between_passes_shows_both()
	{
		var entity = new Shooter();
		entity.Tick(0);
		entity.Shots.Launch(_ball, Vector3.Zero, Vector3.UnitX);
		entity.Shots.End(new ShotId(0, 0), FlightEnd.Impact, Vector3.Zero);

		entity.Shots.Present();

		entity.Host.Log.Should().Equal("show 0:0", "end 0:0 Impact");
	}

	[Fact]
	[Trait("Shots", "Flight")]
	public void A_ray_meets_a_projectile_where_its_sphere_starts()
	{
		var flight = new ProjectileFlight(new ShotId(0, 0), _ball, new Shooter(), Vector3.Zero, Vector3.Zero);
		var center = new Vector3(10, 0.3f, 0);

		flight.TestRay(Vector3.Zero, Vector3.UnitX, 20, center, out float distance).Should().BeTrue();
		distance.Should().BeApproximately(10 - MathF.Sqrt(0.25f - 0.09f), 1e-5f);
		flight.TestRay(Vector3.Zero, Vector3.UnitX, 9, center, out _).Should().BeFalse();
		flight.TestRay(Vector3.Zero, Vector3.UnitY, 20, center, out _).Should().BeFalse();
		flight.TestRay(new Vector3(10, 0, 0), Vector3.UnitX, 1, center, out distance).Should().BeTrue();
		distance.Should().Be(0);
	}

	[Fact]
	[Trait("Shots", "Spread")]
	public void A_seeded_spread_gives_the_same_pellets_inside_the_cone()
	{
		Span<Vector3> first = stackalloc Vector3[8];
		Span<Vector3> second = stackalloc Vector3[8];
		var aim = Vector3.Normalize(new Vector3(1, 0.2f, -0.5f));
		const float angle = MathF.PI / 18;

		ShotSpread.Spread(new DeterministicRandom(42), aim, angle, first);
		ShotSpread.Spread(new DeterministicRandom(42), aim, angle, second);

		first.ToArray().Should().Equal(second.ToArray());

		foreach (Vector3 direction in first)
		{
			direction.Length().Should().BeApproximately(1, 1e-5f);
			MathF.Acos(Math.Clamp(Vector3.Dot(direction, aim), -1, 1)).Should().BeLessThanOrEqualTo(angle + 1e-4f);
		}

		first.ToArray().Distinct().Should().HaveCount(8);
	}

	[Fact]
	[Trait("Shots", "Spread")]
	public void A_2D_spread_stays_within_its_arc()
	{
		Span<Vector2> directions = stackalloc Vector2[16];
		var aim = Vector2.Normalize(new Vector2(1, 1));

		ShotSpread.Spread2D(new DeterministicRandom(3), aim, 0.2f, directions);

		foreach (Vector2 direction in directions)
		{
			direction.Length().Should().BeApproximately(1, 1e-5f);
			MathF.Acos(Math.Clamp(Vector2.Dot(direction, aim), -1, 1)).Should().BeLessThanOrEqualTo(0.2f + 1e-4f);
		}
	}

	[Fact]
	[Trait("Shots", "Behaviors")]
	public void A_rollback_behavior_s_context_comes_back_as_it_was_and_unshared()
	{
		var behavior = new CountingBehavior();
		behavior.OnStarted(null!);
		behavior.OnFixedUpdate(Step);
		behavior.OnFixedUpdate(Step);
		object? snapshot = behavior.CaptureSnapshot();

		behavior.OnFixedUpdate(Step);
		behavior.State.Ticks.Should().Be(3);
		behavior.State.Seen.Should().Equal(1, 2, 3);

		behavior.RestoreSnapshot(snapshot);
		behavior.State.Ticks.Should().Be(2);
		behavior.State.Seen.Should().Equal(1, 2);

		behavior.OnFixedUpdate(Step);
		behavior.RestoreSnapshot(snapshot);
		behavior.State.Seen.Should().Equal(1, 2);

		behavior.DropRun();
		behavior.State.Ticks.Should().Be(0);
	}

	private sealed class CountingContext : BehaviorContext
	{
		public int Ticks { get; set; }

		public List<int> Seen { get; private set; } = [];

		protected override void OnCopied()
		{
			base.OnCopied();
			Seen = [.. Seen];
		}
	}

	private sealed class CountingBehavior : RollbackBehavior<CountingContext>
	{
		public CountingContext State => Context;

		public override void OnEnded(AbilityBehaviorContext context)
		{
		}

		public override void OnFixedUpdate(double deltaTime)
		{
			Context.Ticks++;
			Context.Seen.Add(Context.Ticks);
		}
	}

	private sealed class RecordingHost : IShotHost
	{
		public List<(Vector3 From, Vector3 To)> Stretches { get; } = [];

		public List<string> Log { get; } = [];

		public float? ImpactAtX { get; set; }

		public ShotStep Fly(in ProjectileFlight flight, Vector3 start, Vector3 finish)
		{
			Stretches.Add((start, finish));

			if (ImpactAtX is float x && flight.Origin.Y < 1 && start.X < x && finish.X >= x)
			{
				return ShotStep.Impact(start with { X = x });
			}

			return ShotStep.Flying;
		}

		public void Show(in ProjectileFlight flight)
		{
			Log.Add($"show {flight.Tick}:{flight.Id.Sequence}");
		}

		public void ShowEnd(in ProjectileFlight flight)
		{
			Log.Add($"end {flight.Tick}:{flight.Id.Sequence} {flight.End}");
		}

		public void Hide(in ProjectileFlight flight)
		{
			Log.Add($"hide {flight.Tick}:{flight.Id.Sequence}");
		}
	}

	private sealed class Shooter : IForgeEntity
	{
		public RecordingHost Host { get; } = new();

		public EntityShots Shots { get; }

		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public CuesManager CuesManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; } = new();

		public Variables SharedVariables { get; } = new();

		EntityShots? IForgeEntity.Shots => Shots;

		public Shooter()
		{
			var cues = new CuesManager();
			Attributes = new EntityAttributes(this, new TestAttributeSet());
			Tags = new EntityTags(new TagContainer(new TagsManager([])));
			EffectsManager = new EffectsManager(this, cues);
			CuesManager = cues;
			Abilities = new EntityAbilities(this);
			Shots = new EntityShots(this) { Host = Host };
		}

		public void Tick(long tick)
		{
			EntitySimulation.Tick(this, tick, Step);
		}
	}
}
