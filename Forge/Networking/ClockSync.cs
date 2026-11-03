// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// A client's estimate of the round trip to the server and of the server's tick, from pings the server answers with
/// its tick. What the client needs to run its own clock the right distance ahead of the server's.
/// </summary>
/// <remarks>
/// <para>The client notes the local time it sends each ping with <see cref="PingSent"/>. The server answers with the
/// tick it was at when the ping arrived, and the client hands that to <see cref="PongReceived"/> with the local time
/// the answer arrived. The round trip is the difference between the two local times, and the server was at the
/// answered tick about half a round trip ago, so its tick now is the answered tick plus half a round trip in
/// ticks.</para>
/// <para>Individual samples are noisy: a packet held up by jitter reports a round trip it didn't have. The estimate
/// therefore keeps the last few samples and uses the one with the shortest round trip, the one least delayed by
/// anything other than the wire, and smooths the round trip it reports for display. A sample that arrived out of
/// order, or for a ping that was never sent, is ignored.</para>
/// <para>Every time is a local, monotonic time in seconds, from whatever clock the host likes, and every tick is the
/// server's.</para>
/// </remarks>
public sealed class ClockSync
{
	private const int DefaultWindow = 8;

	private const int MaxPendingPings = 64;

	private readonly SimulationClock _clock;
	private readonly int _window;
	private readonly Queue<PendingPing> _pending = new();
	private readonly List<Sample> _samples = [];
	private ushort _nextSequence;

	/// <summary>
	/// Gets a value indicating whether at least one pong has arrived, so the estimates mean something.
	/// </summary>
	public bool HasEstimate => _samples.Count > 0;

	/// <summary>
	/// Gets the round trip to the server, in seconds, smoothed over recent samples. Zero before the first pong.
	/// </summary>
	public double RoundTripTime { get; private set; }

	/// <summary>
	/// Gets the shortest round trip seen in the recent samples, in seconds: the closest to the wire's own delay.
	/// </summary>
	public double BestRoundTripTime { get; private set; }

	/// <summary>
	/// Gets how far the last few samples' estimates of the server's tick spread, in ticks: how steady the link is.
	/// </summary>
	public double Jitter { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ClockSync"/> class.
	/// </summary>
	/// <param name="clock">The clock whose rate turns time into ticks.</param>
	/// <param name="window">How many recent samples the estimate chooses the best from.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the window isn't positive.</exception>
	public ClockSync(SimulationClock clock, int window = DefaultWindow)
	{
		if (window <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(window), window, "The window holds at least one sample.");
		}

		_clock = clock;
		_window = window;
	}

	/// <summary>
	/// Starts a ping, noting when it left.
	/// </summary>
	/// <param name="localTime">The local time, in seconds.</param>
	/// <returns>The ping's sequence number, to send to the server and get back in the pong.</returns>
	public ushort PingSent(double localTime)
	{
		ushort sequence = _nextSequence++;
		_pending.Enqueue(new PendingPing(sequence, localTime));

		// A server that stopped answering shouldn't leave pings piling up forever.
		while (_pending.Count > MaxPendingPings)
		{
			_pending.Dequeue();
		}

		return sequence;
	}

	/// <summary>
	/// Takes the server's answer to a ping.
	/// </summary>
	/// <param name="sequence">The sequence number the ping went out with.</param>
	/// <param name="serverTick">The tick the server was at when the ping reached it.</param>
	/// <param name="localTime">The local time the answer arrived, in seconds.</param>
	/// <returns><see langword="true"/> if the answer was for a ping still waiting; <see langword="false"/> if it was
	/// stale or unknown and ignored.</returns>
	public bool PongReceived(ushort sequence, long serverTick, double localTime)
	{
		double sentAt = double.NaN;

		// Pings before this one that never got an answer are given up on; an answer to one of them later is stale.
		while (_pending.Count > 0)
		{
			PendingPing pending = _pending.Dequeue();
			if (pending.Sequence == sequence)
			{
				sentAt = pending.SentAt;
				break;
			}
		}

		if (double.IsNaN(sentAt) || localTime < sentAt)
		{
			return false;
		}

		double roundTrip = localTime - sentAt;

		// The ping reached the server about half a round trip after it left, so the server was at serverTick at local
		// time sentAt + roundTrip / 2. The offset is what turns a local time in ticks into the server's tick.
		double offsetTicks = serverTick - _clock.TicksIn(sentAt + (roundTrip / 2));
		_samples.Add(new Sample(roundTrip, offsetTicks));

		if (_samples.Count > _window)
		{
			_samples.RemoveAt(0);
		}

		Recompute();

		return true;
	}

	/// <summary>
	/// Estimates the server's tick at a local time, as a fraction of a tick.
	/// </summary>
	/// <param name="localTime">The local time, in seconds.</param>
	/// <returns>The server's tick then, by the best recent sample.</returns>
	public double EstimateServerTick(double localTime)
	{
		return HasEstimate ? _clock.TicksIn(localTime) + BestOffsetTicks() : 0;
	}

	/// <summary>
	/// The tick a client should be simulating at a local time, so input it sends now for that tick arrives before the
	/// server simulates it: the server's estimated tick plus a round trip plus a margin, rounded up.
	/// </summary>
	/// <param name="localTime">The local time, in seconds.</param>
	/// <param name="marginTicks">Extra ticks of lead beyond the round trip, to absorb jitter.</param>
	/// <returns>The tick the client's clock should be at.</returns>
	public long TargetClientTick(double localTime, double marginTicks)
	{
		return (long)Math.Ceiling(EstimateServerTick(localTime) + _clock.TicksIn(BestRoundTripTime) + marginTicks);
	}

	private double BestOffsetTicks()
	{
		Sample best = _samples[0];

		foreach (Sample sample in _samples)
		{
			if (sample.RoundTrip < best.RoundTrip)
			{
				best = sample;
			}
		}

		return best.OffsetTicks;
	}

	private void Recompute()
	{
		double best = double.MaxValue;
		double sum = 0;
		double minOffset = double.MaxValue;
		double maxOffset = double.MinValue;

		foreach (Sample sample in _samples)
		{
			best = Math.Min(best, sample.RoundTrip);
			sum += sample.RoundTrip;
			minOffset = Math.Min(minOffset, sample.OffsetTicks);
			maxOffset = Math.Max(maxOffset, sample.OffsetTicks);
		}

		BestRoundTripTime = best;
		RoundTripTime = sum / _samples.Count;
		Jitter = maxOffset - minOffset;
	}

	// The offset is what to add to a local time in ticks to get the server's tick: constant for a steady link, so
	// a sample from any time is comparable with a sample from any other.
	private readonly record struct Sample(double RoundTrip, double OffsetTicks);

	private readonly record struct PendingPing(ushort Sequence, double SentAt);
}
