// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Networking;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// The session clock counts ticks, and a client's clock sync finds the server's tick and the round trip from pings,
/// through jitter and stale answers.
/// </summary>
public class ClockTests
{
	private const int TickRate = 60;

	// The server started ten seconds before the client's clock did.
	private const double ServerLead = 10;

	[Fact]
	[Trait("Networking", "Clock")]
	public void The_clock_counts_ticks_and_knows_their_length()
	{
		var clock = new SimulationClock(TickRate, startTick: 5);

		clock.TickRate.Should().Be(TickRate);
		clock.FixedDeltaTime.Should().BeApproximately(1.0 / TickRate, 1e-12);
		clock.Tick.Should().Be(5);

		clock.Advance().Should().Be(5, "advancing returns the tick that was simulated");
		clock.Tick.Should().Be(6);

		clock.Set(100);
		clock.Tick.Should().Be(100);
		clock.TicksIn(0.5).Should().Be(30);
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void A_tick_rate_must_be_positive()
	{
		FluentActions.Invoking(() => new SimulationClock(0)).Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void One_pong_gives_the_round_trip_and_the_server_s_tick()
	{
		var sync = new ClockSync(new SimulationClock(TickRate));

		sync.HasEstimate.Should().BeFalse();

		ushort sequence = sync.PingSent(1.0);
		sync.PongReceived(sequence, ServerTickAt(1.1), 1.2).Should().BeTrue();

		sync.HasEstimate.Should().BeTrue();
		sync.RoundTripTime.Should().BeApproximately(0.2, 1e-9);
		sync.BestRoundTripTime.Should().BeApproximately(0.2, 1e-9);
		sync.EstimateServerTick(1.2).Should().BeApproximately(ServerTickAt(1.2), 1.0);
		sync.EstimateServerTick(5.0).Should().BeApproximately(ServerTickAt(5.0), 1.0, "the estimate follows time");
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void The_estimate_follows_the_sample_with_the_shortest_round_trip()
	{
		var sync = new ClockSync(new SimulationClock(TickRate));

		// A clean sample, then one held up on the way back by a 300 ms stall, which would put the server 9 ticks
		// further ahead than it is if believed.
		Exchange(sync, sentAt: 1.0, oneWay: 0.05, returnDelay: 0);
		Exchange(sync, sentAt: 2.0, oneWay: 0.05, returnDelay: 0.3);

		sync.BestRoundTripTime.Should().BeApproximately(0.1, 1e-9);
		sync.RoundTripTime.Should().BeApproximately(0.25, 1e-9, "the smoothed round trip averages the samples");
		sync.EstimateServerTick(3.0).Should().BeApproximately(ServerTickAt(3.0), 1.0);
		sync.Jitter.Should().BeApproximately(9, 1.0, "the stalled sample disagrees by its stall in ticks");
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void Old_samples_leave_the_window()
	{
		var sync = new ClockSync(new SimulationClock(TickRate), window: 2);

		Exchange(sync, sentAt: 1.0, oneWay: 0.01, returnDelay: 0);
		Exchange(sync, sentAt: 2.0, oneWay: 0.1, returnDelay: 0);
		Exchange(sync, sentAt: 3.0, oneWay: 0.1, returnDelay: 0);

		sync.BestRoundTripTime.Should().BeApproximately(0.2, 1e-9, "the 20 ms sample is two pings old");
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void A_stale_or_unknown_pong_is_ignored()
	{
		var sync = new ClockSync(new SimulationClock(TickRate));

		ushort first = sync.PingSent(1.0);
		ushort second = sync.PingSent(1.5);

		sync.PongReceived(second, ServerTickAt(1.55), 1.6).Should().BeTrue();
		sync.PongReceived(first, ServerTickAt(1.05), 1.7).Should().BeFalse("an answer to an older ping is stale");
		sync.PongReceived(999, ServerTickAt(2), 2.0).Should().BeFalse("nobody sent that ping");
		sync.PongReceived(second, ServerTickAt(1.55), 1.8).Should().BeFalse("a pong is taken once");
		sync.RoundTripTime.Should().BeApproximately(0.1, 1e-9);
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void A_stale_pong_leaves_newer_pings_waiting()
	{
		var sync = new ClockSync(new SimulationClock(TickRate));

		ushort first = sync.PingSent(1.0);
		ushort second = sync.PingSent(1.1);
		ushort third = sync.PingSent(1.2);

		sync.PongReceived(second, ServerTickAt(1.15), 1.2).Should().BeTrue();
		sync.PongReceived(first, ServerTickAt(1.05), 1.25).Should().BeFalse("an answer to an older ping is stale");
		sync.PongReceived(third, ServerTickAt(1.25), 1.3).Should().BeTrue("the stale pong left it waiting");
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void The_target_client_tick_leads_the_server_by_a_round_trip_and_the_margin()
	{
		var sync = new ClockSync(new SimulationClock(TickRate));
		Exchange(sync, sentAt: 1.0, oneWay: 0.1, returnDelay: 0);

		long target = sync.TargetClientTick(2.0, marginTicks: 2);

		// A round trip of 200 ms is 12 ticks, plus 2 of margin, rounded up from the fractional estimate.
		target.Should().BeInRange(ServerTickAt(2.0) + 14, ServerTickAt(2.0) + 15);
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void A_clock_behind_its_target_catches_up_a_tick_at_a_time()
	{
		var sync = new ClockSync(new SimulationClock(TickRate));
		Exchange(sync, sentAt: 1.0, oneWay: 0.05, returnDelay: 0);
		var dilation = new ClockDilation();
		long tick = sync.TargetClientTick(1.1, dilation.MarginTicks) - 8;
		dilation.Steer(tick + 8, sync, 1.1, serverInputDepth: 2);
		var steps = new List<int>();

		for (int step = 0; step < 120; step++)
		{
			double time = 1.1 + (step / (double)TickRate);
			DilatedStep dilated = dilation.Steer(tick, sync, time, serverInputDepth: 2);

			dilated.FirstTick.Should().Be(tick, "it never jumps this close");
			steps.Add(dilated.Ticks);
			tick = dilated.NextTick;
		}

		steps.Should().OnlyContain(x => x == 1 || x == 2);
		steps.Take(6).Should().Equal(1, 2, 1, 2, 1, 2);
		long target = sync.TargetClientTick(1.1 + (120 / (double)TickRate), dilation.MarginTicks);
		Math.Abs(target - tick).Should().BeLessThanOrEqualTo(dilation.ToleranceTicks + 1);
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void The_first_estimate_puts_the_clock_on_its_target()
	{
		var sync = new ClockSync(new SimulationClock(TickRate));
		var dilation = new ClockDilation();

		dilation.Steer(5, sync, 1.0, serverInputDepth: 2).Should().Be(new DilatedStep(5, 1), "it has nothing to steer by");

		Exchange(sync, sentAt: 1.0, oneWay: 0.05, returnDelay: 0);
		long target = sync.TargetClientTick(1.1, dilation.MarginTicks);

		dilation.Steer(target - 8, sync, 1.1, serverInputDepth: 2).Should().Be(new DilatedStep(target, 1));
		dilation.Steer(target - 7, sync, 1.1, serverInputDepth: 2).FirstTick
			.Should().Be(target - 7, "from then on it's steered");
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void A_clock_ahead_of_its_target_waits_and_one_far_off_jumps()
	{
		var sync = new ClockSync(new SimulationClock(TickRate));
		Exchange(sync, sentAt: 1.0, oneWay: 0.05, returnDelay: 0);
		var dilation = new ClockDilation { StepsPerAdjustment = 1, StepsPerCatchUp = 1 };
		long target = sync.TargetClientTick(1.1, dilation.MarginTicks);
		dilation.Steer(target, sync, 1.1, serverInputDepth: 2);

		dilation.Steer(target + 5, sync, 1.1, serverInputDepth: 2).Should().Be(new DilatedStep(target + 5, 0));
		dilation.Steer(target - 100, sync, 1.1, serverInputDepth: 2).Should().Be(new DilatedStep(target, 1));
		dilation.Steer(target - 1, sync, 1.1, serverInputDepth: 2).Ticks.Should().Be(1, "a tick off is close enough");
		dilation.Steer(target - 1, sync, 1.1, serverInputDepth: 0).Ticks
			.Should().Be(2, "a server that ran out of the client's input asks for a tick more");
	}

	[Fact]
	[Trait("Networking", "Clock")]
	public void Pings_over_a_conditioned_loopback_find_the_server_tick()
	{
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;
		var client = new ConditionedTransport(network.Connect(), NetworkConditions.Poor, new DeterministicRandom(11));
		var sync = new ClockSync(new SimulationClock(TickRate));
		var writer = new BitWriter();
		double time = 0;

		// A second of pings at 60 Hz. The server answers pings as they arrive, with its tick at that moment, and the
		// answers come straight back: the client's side is the conditioned one, so only its sends are delayed.
		for (int step = 0; step < 60; step++)
		{
			time = step / (double)TickRate;
			client.Update(time);

			writer.Clear();
			writer.WriteUInt16(sync.PingSent(time));
			client.Send(PeerId.Server, 0, reliable: false, writer.WrittenBytes);

			while (server.TryReceive(out TransportEvent received))
			{
				if (received.Kind != TransportEventKind.Packet)
				{
					continue;
				}

				ushort sequence = new BitReader(received.Payload).ReadUInt16();
				writer.Clear();
				writer.WriteUInt16(sequence);
				writer.WriteVarInt(ServerTickAt(time));
				server.Send(received.Peer, 0, reliable: false, writer.WrittenBytes);
			}

			while (client.TryReceive(out TransportEvent received))
			{
				if (received.Kind != TransportEventKind.Packet)
				{
					continue;
				}

				var reader = new BitReader(received.Payload);
				sync.PongReceived(reader.ReadUInt16(), reader.ReadVarInt(), time);
			}
		}

		sync.HasEstimate.Should().BeTrue();
		sync.BestRoundTripTime.Should().BeInRange(0.15, 0.25, "one way is 200 ms plus jitter, the way back is free");

		// The estimate uses the sample closest to the wire's delay, but that sample's trip was still one way only,
		// which the estimate halves: the server tick is off by about half the one-way latency, and no more.
		double estimate = sync.EstimateServerTick(time);
		Math.Abs(estimate - ServerTickAt(time)).Should().BeLessThan(TickRate * 0.15);
	}

	// The server's tick at a local time: it runs at the tick rate and started ServerLead seconds earlier.
	private static long ServerTickAt(double localTime)
	{
		return (long)Math.Floor((localTime + ServerLead) * TickRate);
	}

	// A ping that takes oneWay to the server and oneWay plus returnDelay back.
	private static void Exchange(ClockSync sync, double sentAt, double oneWay, double returnDelay)
	{
		ushort sequence = sync.PingSent(sentAt);
		sync.PongReceived(sequence, ServerTickAt(sentAt + oneWay), sentAt + oneWay + oneWay + returnDelay);
	}
}
