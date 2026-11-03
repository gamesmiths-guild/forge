// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// The session's tick: which tick is simulated next, and how long a tick is. One per machine, owned by the host, read
/// by everything that numbers a tick.
/// </summary>
/// <remarks>
/// <para>The server's clock is the session's time. A client's clock runs ahead of it by about a round trip plus a
/// margin, so the input a client sends for a tick reaches the server before the server simulates that tick; how far
/// ahead comes from <see cref="ClockSync"/>. Offline, the one clock is both.</para>
/// <para>The clock only counts. The host advances it once per fixed step, after the tick it names has been simulated,
/// and sets it when a rollback or a correction puts the simulation at another tick.</para>
/// </remarks>
public sealed class SimulationClock
{
	/// <summary>
	/// Gets how many ticks make a second.
	/// </summary>
	public int TickRate { get; }

	/// <summary>
	/// Gets how long a tick is, in seconds.
	/// </summary>
	public double FixedDeltaTime { get; }

	/// <summary>
	/// Gets the tick to simulate next.
	/// </summary>
	public long Tick { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SimulationClock"/> class.
	/// </summary>
	/// <param name="tickRate">How many ticks make a second.</param>
	/// <param name="startTick">The first tick to simulate.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the tick rate isn't positive.</exception>
	public SimulationClock(int tickRate, long startTick = 0)
	{
		if (tickRate <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(tickRate), tickRate, "A tick rate is positive.");
		}

		TickRate = tickRate;
		FixedDeltaTime = 1.0 / tickRate;
		Tick = startTick;
	}

	/// <summary>
	/// Moves to the next tick, once the current one has been simulated.
	/// </summary>
	/// <returns>The tick that was just simulated.</returns>
	public long Advance()
	{
		return Tick++;
	}

	/// <summary>
	/// Puts the clock at a tick, after a rollback or a correction.
	/// </summary>
	/// <param name="tick">The tick to simulate next.</param>
	public void Set(long tick)
	{
		Tick = tick;
	}

	/// <summary>
	/// Converts seconds to ticks at this clock's rate.
	/// </summary>
	/// <param name="seconds">A length of time.</param>
	/// <returns>The same length in ticks, fractional.</returns>
	public double TicksIn(double seconds)
	{
		return seconds * TickRate;
	}
}
