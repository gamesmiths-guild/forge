// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// The server tick a client shows its mirrors at, as a fraction of a tick: it runs with the client's own time, and
/// follows the target the host gives it, a delay behind the newest state, by running a little faster or slower rather
/// than jumping.
/// </summary>
/// <remarks>
/// The target comes from estimates of the server's tick and the round trip, which move whenever a ping changes them, by
/// as much as the link's jitter. Shown at the target itself, every body would hitch at each move.
/// </remarks>
/// <param name="clock">The clock whose rate turns time into ticks.</param>
public sealed class InterpolationClock(SimulationClock clock)
{
	/// <summary>
	/// How much faster or slower than real time the clock runs at most to reach its target: a tenth.
	/// </summary>
	public const double MaxDilation = 0.1;

	/// <summary>
	/// How far from its target the clock may be before it jumps there instead, in seconds.
	/// </summary>
	public const double SnapSeconds = 0.25;

	private bool _started;

	/// <summary>
	/// Gets the tick shown now.
	/// </summary>
	public double Tick { get; private set; }

	/// <summary>
	/// Moves the clock on by the time since the last advance, toward the target. The first advance, and one that finds
	/// the target more than <see cref="SnapSeconds"/> away, jumps to it.
	/// </summary>
	/// <param name="deltaSeconds">The time since the last advance.</param>
	/// <param name="targetTick">The tick the clock should be at now.</param>
	public void Advance(double deltaSeconds, double targetTick)
	{
		double ticks = clock.TicksIn(deltaSeconds);
		double error = targetTick - (Tick + ticks);

		if (!_started || Math.Abs(error) > clock.TicksIn(SnapSeconds))
		{
			Tick = targetTick;
			_started = true;
			return;
		}

		// A tick behind runs the most faster, a tick ahead the most slower.
		double dilation = Math.Clamp(error * MaxDilation, -MaxDilation, MaxDilation);
		Tick += ticks * (1 + dilation);
	}

	/// <summary>
	/// Forgets the clock, so the next advance jumps to its target.
	/// </summary>
	public void Reset()
	{
		_started = false;
		Tick = 0;
	}
}
