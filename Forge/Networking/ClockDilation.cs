// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// Steers a client's clock toward the tick it should simulate, ahead of the server, a tick at a time: a fixed step
/// simulates one tick as a rule, two to catch up or none to let the server come closer, so what the client simulates
/// runs a little faster or slower instead of jumping. The clock jumps to its target with the first estimate, so a
/// client that just joined runs ahead from its first input on, and again whenever it's far behind, as after a stall.
/// It never goes back: far ahead, it waits for its target.
/// </summary>
/// <remarks>
/// <para>The target is <see cref="ClockSync.TargetClientTick"/>'s: a round trip and <see cref="MarginTicks"/> ahead
/// of the server's estimated tick, with a tick more while the server says it holds none of the client's input, as when
/// the input arrives too late to be run.</para>
/// <para>Call <see cref="Steer"/> once per fixed step, before simulating, and simulate the ticks it hands back, from
/// its first, sending each tick's input; the clock then continues from the tick after the last.</para>
/// </remarks>
public sealed class ClockDilation
{
	private int _stepsSinceAdjustment;
	private bool _placed;

	/// <summary>
	/// Gets or sets how many ticks beyond a round trip the client runs ahead, to absorb jitter.
	/// </summary>
	public int MarginTicks { get; set; } = 2;

	/// <summary>
	/// Gets or sets how many fixed steps pass at least between two that simulate no tick, while the client runs ahead
	/// of its target: 10 lets it run up to a tenth slower. A client ahead only adds latency to its own input.
	/// </summary>
	public int StepsPerAdjustment { get; set; } = 10;

	/// <summary>
	/// Gets or sets how many fixed steps pass at least between two that simulate two ticks, while the client is behind
	/// its target: 2 lets it run up to half again as fast. A client behind sends input that arrives too late to be run,
	/// so it catches up faster than it slows down.
	/// </summary>
	public int StepsPerCatchUp { get; set; } = 2;

	/// <summary>
	/// Gets or sets how far from the target the clock may be, in ticks, before it's steered.
	/// </summary>
	public int ToleranceTicks { get; set; } = 1;

	/// <summary>
	/// Gets or sets how far from the target the clock may be, in ticks, before it jumps there from behind, or waits
	/// every step for it from ahead.
	/// </summary>
	public int JumpTicks { get; set; } = 30;

	/// <summary>
	/// Decides which ticks the next fixed step simulates.
	/// </summary>
	/// <param name="clockTick">The tick the client's clock is at.</param>
	/// <param name="sync">The client's clock sync.</param>
	/// <param name="localTime">The local time, in seconds.</param>
	/// <param name="serverInputDepth">How many of the client's input ticks the server held, the last it said:
	/// <see cref="NetworkSession.ServerInputDepth"/>.</param>
	/// <returns>The ticks to simulate.</returns>
	public DilatedStep Steer(long clockTick, ClockSync sync, double localTime, int serverInputDepth)
	{
		if (!sync.HasEstimate)
		{
			return new DilatedStep(clockTick, 1);
		}

		long target = sync.TargetClientTick(localTime, MarginTicks + (serverInputDepth == 0 ? 1 : 0));
		long error = target - clockTick;

		if (!_placed || Math.Abs(error) > JumpTicks)
		{
			_stepsSinceAdjustment = 0;

			// The ticks behind the clock were simulated and their input sent, so a clock ahead waits for its target.
			if (error < 0)
			{
				return new DilatedStep(clockTick, 0);
			}

			// A client behind its target sends input too late to be run, and the ticks a jump skips run on both
			// machines with what was last held.
			_placed = true;
			return new DilatedStep(target, 1);
		}

		if (Math.Abs(error) <= ToleranceTicks
			|| ++_stepsSinceAdjustment < (error > 0 ? StepsPerCatchUp : StepsPerAdjustment))
		{
			return new DilatedStep(clockTick, 1);
		}

		_stepsSinceAdjustment = 0;
		return new DilatedStep(clockTick, error > 0 ? 2 : 0);
	}
}

/// <summary>
/// The ticks a fixed step simulates, chosen by <see cref="ClockDilation.Steer"/>.
/// </summary>
/// <param name="FirstTick">The first tick to simulate.</param>
/// <param name="Ticks">How many ticks to simulate from it: one as a rule, two to catch up, none to wait.</param>
public readonly record struct DilatedStep(long FirstTick, int Ticks)
{
	/// <summary>
	/// Gets the tick the clock continues from after the step.
	/// </summary>
	public long NextTick => FirstTick + Ticks;
}
