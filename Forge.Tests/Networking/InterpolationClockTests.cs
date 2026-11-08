// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Replication;

namespace Gamesmiths.Forge.Tests.Networking;

public class InterpolationClockTests
{
	private const double Frame = 1.0 / 60;

	[Fact]
	[Trait("Networking", "Interpolation")]
	public void The_first_advance_jumps_to_the_target_and_then_the_clock_runs_with_time()
	{
		var clock = new InterpolationClock(new SimulationClock(60));

		clock.Advance(Frame, 100);
		clock.Tick.Should().Be(100);

		clock.Advance(Frame, 101);
		clock.Tick.Should().BeApproximately(101, 1e-9);
	}

	[Fact]
	[Trait("Networking", "Interpolation")]
	public void A_target_that_jumps_a_tick_is_reached_gradually_never_faster_than_a_tenth_over_time()
	{
		var clock = new InterpolationClock(new SimulationClock(60));
		clock.Advance(Frame, 100);
		double target = 100;
		double previous = clock.Tick;

		target += 2;
		clock.Advance(Frame, target);
		(clock.Tick - previous).Should().BeApproximately(1.1, 1e-9, "a tick behind runs a tenth faster");

		for (int i = 0; i < 120; i++)
		{
			previous = clock.Tick;
			target++;
			clock.Advance(Frame, target);
			(clock.Tick - previous)
				.Should().BeInRange(1 - InterpolationClock.MaxDilation, 1 + InterpolationClock.MaxDilation);
		}

		clock.Tick.Should().BeApproximately(target, 0.01);
	}

	[Fact]
	[Trait("Networking", "Interpolation")]
	public void A_target_far_away_is_jumped_to()
	{
		var clock = new InterpolationClock(new SimulationClock(60));
		clock.Advance(Frame, 100);

		clock.Advance(Frame, 130);

		clock.Tick.Should().Be(130);
	}
}
