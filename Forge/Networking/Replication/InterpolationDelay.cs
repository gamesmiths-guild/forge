// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// How far behind the newest state it holds a client should show the server's entities, worked out from how the state
/// packets arrive. A client shows them between two states, so it needs the next one to be there before it gets to it:
/// one send interval behind is enough on a steady link, and it needs more for every packet that comes late or not at
/// all.
/// </summary>
/// <remarks>
/// <para>Each packet's lateness is how far the server's tick had gone, by this client's estimate, past the packet's own
/// tick when it arrived. Its smoothed mean deviation is the jitter. The send interval is the smallest gap between the
/// ticks of recent packets, and the loss the share of recent packets that never came.</para>
/// <para>The delay is one send interval, plus <see cref="JitterScale"/> times the jitter, plus up to one more interval
/// as the loss grows to 5%, kept between <see cref="MinSeconds"/> and <see cref="MaxSeconds"/>. It rises at once
/// and falls by <see cref="FallTicksPerSecond"/>, so a single good moment doesn't take a bad link's margin away.</para>
/// </remarks>
public sealed class InterpolationDelay
{
	private const double Smoothing = 1.0 / 16;
	private const double LossSmoothing = 1.0 / 32;
	private const double FullLossAllowance = 0.05;
	private const int IntervalWindow = 32;

	private readonly SimulationClock _clock;
	private readonly Queue<long> _gaps = new();
	private long _previousTick = -1;
	private uint _previousSequence;
	private double _meanLateness;
	private bool _hasLateness;
	private double _delayTicks = -1;
	private double _lastLocalTime = double.NaN;

	/// <summary>
	/// Gets or sets how many times the jitter the delay leaves room for. The default is 2.5: about two standard
	/// deviations of normally spread lateness, and all of an evenly spread one.
	/// </summary>
	public double JitterScale { get; set; } = 2.5;

	/// <summary>
	/// Gets or sets the shortest delay, in seconds. One send interval is the shortest whatever this says. The default
	/// is 0.
	/// </summary>
	public double MinSeconds { get; set; }

	/// <summary>
	/// Gets or sets the longest delay, in seconds. The default is 0.25.
	/// </summary>
	public double MaxSeconds { get; set; } = 0.25;

	/// <summary>
	/// Gets or sets how fast the delay falls toward a lower target, in ticks per second. The default is 4.
	/// </summary>
	public double FallTicksPerSecond { get; set; } = 4;

	/// <summary>
	/// Gets the smoothed mean deviation of the packets' lateness, in ticks.
	/// </summary>
	public double Jitter { get; private set; }

	/// <summary>
	/// Gets the share of recent state packets that never arrived, from 0 to 1.
	/// </summary>
	public double Loss { get; private set; }

	/// <summary>
	/// Gets the server's send interval as measured here, in ticks, or 0 before two packets arrived.
	/// </summary>
	public long SendInterval => _gaps.Count == 0 ? 0 : _gaps.Min();

	/// <summary>
	/// Gets the delay to show the server's entities at, in ticks, as of the last <see cref="Update"/>. 0 before two
	/// packets arrived.
	/// </summary>
	public double Ticks => Math.Max(_delayTicks, 0);

	internal InterpolationDelay(SimulationClock clock)
	{
		_clock = clock;
	}

	/// <summary>
	/// Moves the delay toward what the packets so far call for: up at once, down by <see cref="FallTicksPerSecond"/>.
	/// </summary>
	/// <param name="localTime">This machine's time, in seconds.</param>
	public void Update(double localTime)
	{
		long interval = SendInterval;

		if (interval == 0)
		{
			return;
		}

		double lossAllowance = interval * Math.Min(Loss / FullLossAllowance, 1);
		double target = interval + (JitterScale * Jitter) + lossAllowance;
		target = Math.Clamp(
			target,
			Math.Max(interval, _clock.TicksIn(MinSeconds)),
			Math.Max(interval, _clock.TicksIn(MaxSeconds)));

		double elapsed = double.IsNaN(_lastLocalTime) ? 0 : Math.Max(localTime - _lastLocalTime, 0);
		_lastLocalTime = localTime;

		_delayTicks = _delayTicks < 0 || target >= _delayTicks
			? target
			: Math.Max(target, _delayTicks - (FallTicksPerSecond * elapsed));
	}

	/// <summary>
	/// Forgets every packet, as when the client leaves.
	/// </summary>
	public void Clear()
	{
		_gaps.Clear();
		_previousTick = -1;
		_previousSequence = 0;
		_meanLateness = 0;
		_hasLateness = false;
		_delayTicks = -1;
		_lastLocalTime = double.NaN;
		Jitter = 0;
		Loss = 0;
	}

	// A state packet that arrived in order, with the server's tick as this client estimated it on arrival, or null
	// before the clock had an estimate.
	internal void NoteArrival(uint sequence, long tick, double? serverTickNow)
	{
		if (_previousSequence != 0)
		{
			long missing = sequence - _previousSequence - 1;

			for (long i = 0; i < Math.Min(missing, IntervalWindow); i++)
			{
				Loss += (1 - Loss) * LossSmoothing;
			}

			Loss -= Loss * LossSmoothing;
		}

		_previousSequence = sequence;

		if (_previousTick >= 0 && tick > _previousTick)
		{
			_gaps.Enqueue(tick - _previousTick);

			if (_gaps.Count > IntervalWindow)
			{
				_gaps.Dequeue();
			}
		}

		_previousTick = tick;

		if (serverTickNow is not double now)
		{
			return;
		}

		double lateness = now - tick;

		if (!_hasLateness)
		{
			_meanLateness = lateness;
			_hasLateness = true;
			return;
		}

		double deviation = lateness - _meanLateness;
		_meanLateness += deviation * Smoothing;
		Jitter += (Math.Abs(deviation) - Jitter) * Smoothing;
	}
}
