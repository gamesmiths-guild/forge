// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// The body states the server sent for one remote entity, by tick, and where the body is at any time between them. A
/// client shows remote bodies a little behind the server, so that it has the state on either side of the moment it
/// shows and moves the body smoothly from one to the next.
/// </summary>
public sealed class RemoteBody
{
	/// <summary>
	/// How many states are kept at most.
	/// </summary>
	public const int Capacity = 64;

	private readonly List<Sample> _samples = [];

	/// <summary>
	/// Gets the newest state the server sent, or <see langword="null"/> before any.
	/// </summary>
	public BodyState? Latest => _samples.Count > 0 ? _samples[^1].State : null;

	/// <summary>
	/// Gets the tick of the newest state, or -1 before any.
	/// </summary>
	public long LatestTick => _samples.Count > 0 ? _samples[^1].Tick : -1;

	/// <summary>
	/// Gets how many states are held.
	/// </summary>
	public int Count => _samples.Count;

	/// <summary>
	/// Adds the state the server sent for a tick. A state for a tick at or before the newest is ignored: it arrived
	/// late, and what came after it already says where the body went.
	/// </summary>
	/// <param name="tick">The server tick the state is of.</param>
	/// <param name="state">The state.</param>
	public void Add(long tick, in BodyState state)
	{
		if (_samples.Count > 0 && tick <= _samples[^1].Tick)
		{
			return;
		}

		if (_samples.Count == Capacity)
		{
			_samples.RemoveAt(0);
		}

		_samples.Add(new Sample(tick, state));
	}

	/// <summary>
	/// Finds where the body is at a tick: between the states on either side of it, at the oldest state before any, and
	/// at the newest state after all of them.
	/// </summary>
	/// <param name="tick">The tick to show, which may fall between two ticks.</param>
	/// <param name="state">The state at that tick.</param>
	/// <returns><see langword="false"/> before any state arrived.</returns>
	public bool TrySample(double tick, out BodyState state)
	{
		if (_samples.Count == 0)
		{
			state = default;
			return false;
		}

		if (tick <= _samples[0].Tick)
		{
			state = _samples[0].State;
			return true;
		}

		if (tick >= _samples[^1].Tick)
		{
			state = _samples[^1].State;
			return true;
		}

		int after = 1;

		while (_samples[after].Tick < tick)
		{
			after++;
		}

		Sample from = _samples[after - 1];
		Sample to = _samples[after];
		float t = (float)((tick - from.Tick) / (to.Tick - from.Tick));

		state = new BodyState(
			Vector3.Lerp(from.State.Position, to.State.Position, t),
			Quaternion.Slerp(from.State.Rotation, to.State.Rotation, t),
			Vector3.Lerp(from.State.Velocity, to.State.Velocity, t),
			t < 0.5f ? from.State.Grounded : to.State.Grounded);
		return true;
	}

	/// <summary>
	/// Forgets every state.
	/// </summary>
	public void Clear()
	{
		_samples.Clear();
	}

	private readonly record struct Sample(long Tick, BodyState State);
}
