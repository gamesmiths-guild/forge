// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Shots;

/// <summary>
/// The lightweight projectiles an entity has launched, flown in its ticks.
/// </summary>
/// <remarks>
/// <para>A flight is a launch and, once it has one, an end. Its path follows from its launch alone
/// (<see cref="ProjectileFlight.PositionAt(double)"/>), so nothing is sent while it flies: the server sends each client
/// the launch, and the end once there is one.</para>
/// <para>The entity's tick flies its projectiles after its abilities: each flight launched before the tick has that
/// tick's stretch of its path tested by the <see cref="Host"/>, which ends it on an impact, and one that has flown its
/// lifetime ends there. A snapshot of the entity holds its flights, so a rollback and the ticks run again bring them
/// back as they were, and a client predicts its own entity's projectiles as it predicts the rest of it.</para>
/// <para>On a client, a flight of an entity it mirrors is shown from the server's launch, at the client's shown tick,
/// and ends where the server says.</para>
/// </remarks>
/// <param name="owner">The entity that launches the projectiles.</param>
public sealed class EntityShots(IForgeEntity owner)
{
	// How long a flight stays after its end, so a presentation pass sees the end before it goes.
	private const double KeepEndedSeconds = 1;

	private readonly List<ProjectileFlight> _flights = [];
	private readonly Dictionary<ShotId, ProjectileFlight> _presented = [];
	private readonly Dictionary<ShotId, ProjectileFlight> _confirmed = [];
	private readonly Dictionary<ShotId, ProjectileFlight> _received = [];
	private readonly List<ShotId> _gone = [];

	/// <summary>
	/// Gets the entity that launches the projectiles.
	/// </summary>
	public IForgeEntity Owner { get; } = owner;

	/// <summary>
	/// Gets or sets what tests the flights against the world and shows them.
	/// </summary>
	public IShotHost? Host { get; set; }

	/// <summary>
	/// Gets the flights, in the order they were launched: those in flight and those that ended a moment ago.
	/// </summary>
	public IReadOnlyList<ProjectileFlight> Flights => _flights;

	internal IShotObserver? Observer { get; set; }

	internal bool IsMirrored { get; set; }

	/// <summary>
	/// Launches a projectile in the entity's current tick. Its first stretch of path is flown in the next tick.
	/// </summary>
	/// <param name="projectile">What kind of projectile it is.</param>
	/// <param name="origin">Where it starts.</param>
	/// <param name="velocity">Its velocity, in units per second.</param>
	/// <returns>The flight.</returns>
	/// <exception cref="InvalidOperationException">Thrown before the entity's first tick, or on an entity a client
	/// mirrors, which only shows the server's launches.</exception>
	public ProjectileFlight Launch(ProjectileData projectile, Vector3 origin, Vector3 velocity)
	{
		long tick = Owner.EffectsManager.Tick;

		if (tick < 0)
		{
			throw new InvalidOperationException("An entity launches a projectile in one of its ticks.");
		}

		if (IsMirrored)
		{
			throw new InvalidOperationException("A client shows the server's launches for an entity it mirrors.");
		}

		// A launch in the entity's tick that its client doesn't predict counts down, so it doesn't shift the names of
		// those the client does.
		bool ownersOwn = Owner.EffectsManager.RaisesOwnTick;
		int step = ownersOwn || !Owner.EffectsManager.IsAdvancing ? 1 : -1;
		int sequence = step > 0 ? 0 : -1;

		foreach (ProjectileFlight launched in _flights)
		{
			if (launched.Tick == tick && (launched.Id.Sequence >= 0) == (step > 0))
			{
				sequence += step;
			}
		}

		var flight = new ProjectileFlight(new ShotId(tick, sequence), projectile, Owner, origin, velocity);
		_flights.Add(flight);
		Observer?.Launched(this, flight, ownersOwn);
		return flight;
	}

	/// <summary>
	/// Ends a flight in the entity's current tick, as when a shot hits it in flight, or it detonates on command.
	/// </summary>
	/// <param name="id">The flight's name.</param>
	/// <param name="end">How it ended: <see cref="FlightEnd.Impact"/> or <see cref="FlightEnd.Struck"/>.</param>
	/// <param name="point">Where it ended.</param>
	/// <param name="normal">The normal of the surface it hit, or zero.</param>
	/// <returns><see langword="true"/> if it was flying and ended; <see langword="false"/> if it wasn't there or had
	/// ended.</returns>
	public bool End(ShotId id, FlightEnd end, Vector3 point, Vector3 normal = default)
	{
		int index = IndexOf(id);

		if (index < 0 || !_flights[index].IsFlying || end == FlightEnd.None || IsMirrored)
		{
			return false;
		}

		Finish(index, end, Owner.EffectsManager.Tick, point, normal);
		return true;
	}

	/// <summary>
	/// Finds a flight by its name.
	/// </summary>
	/// <param name="id">The name.</param>
	/// <param name="flight">The flight.</param>
	/// <returns><see langword="true"/> if the entity holds it.</returns>
	public bool TryGet(ShotId id, out ProjectileFlight flight)
	{
		int index = IndexOf(id);
		flight = index < 0 ? default : _flights[index];
		return index >= 0;
	}

	/// <summary>
	/// Brings what <see cref="Host"/> shows in line with the flights: shows those launched since the last pass and the
	/// ends since, and hides those a correction took away. Call it from the host's presentation pass.
	/// </summary>
	public void Present()
	{
		if (Host is not IShotHost host)
		{
			return;
		}

		foreach (ProjectileFlight flight in _flights)
		{
			if (!_presented.TryGetValue(flight.Id, out ProjectileFlight shown))
			{
				host.Show(flight);

				if (!flight.IsFlying)
				{
					host.ShowEnd(flight);
				}

				_presented[flight.Id] = flight;
				continue;
			}

			// A correction that changed a flying flight's launch, or brought an ended flight back, shows it anew.
			if ((shown.IsFlying && (shown.Origin != flight.Origin || shown.Velocity != flight.Velocity
				|| shown.Projectile != flight.Projectile)) || (!shown.IsFlying && flight.IsFlying))
			{
				if (shown.IsFlying)
				{
					host.Hide(shown);
				}

				host.Show(flight);

				if (!flight.IsFlying)
				{
					host.ShowEnd(flight);
				}

				_presented[flight.Id] = flight;
			}
			else if (shown.IsFlying && !flight.IsFlying)
			{
				host.ShowEnd(flight);
				_presented[flight.Id] = flight;
			}
		}

		_gone.Clear();

		foreach (KeyValuePair<ShotId, ProjectileFlight> shown in _presented)
		{
			if (IndexOf(shown.Key) >= 0)
			{
				continue;
			}

			if (shown.Value.IsFlying)
			{
				host.Hide(shown.Value);
			}

			_gone.Add(shown.Key);
		}

		foreach (ShotId id in _gone)
		{
			_presented.Remove(id);
		}
	}

	// Flies each projectile launched before the tick over its stretch of the tick, then lets go of those that ended
	// long enough ago. A flight the server ended flies untested to the server's end.
	internal void Advance(double fixedDeltaTime)
	{
		long tick = Owner.EffectsManager.Tick;

		foreach (ProjectileFlight received in _received.Values)
		{
			if (received.Tick < tick && IndexOf(received.Id) < 0)
			{
				_flights.Add(received);
			}
		}

		for (int i = 0; i < _flights.Count; i++)
		{
			ProjectileFlight flight = _flights[i];

			if (flight.Tick >= tick)
			{
				continue;
			}

			if (_confirmed.TryGetValue(flight.Id, out ProjectileFlight confirmed))
			{
				if (confirmed.EndTick <= tick)
				{
					_flights[i] = confirmed;
				}

				continue;
			}

			if (!flight.IsFlying)
			{
				continue;
			}

			double age = (tick - flight.Tick) * fixedDeltaTime;
			Vector3 to = flight.PositionAt(Math.Min(age, flight.Projectile.Lifetime));
			Vector3 from = flight.PositionAt(Math.Min(age - fixedDeltaTime, flight.Projectile.Lifetime));
			ShotStep step = Host?.Fly(flight, from, to) ?? ShotStep.Flying;

			if (step.Pierces >= 0 || step.Strength >= 0)
			{
				_flights[i] = flight with
				{
					Pierces = step.Pierces >= 0 ? step.Pierces : flight.Pierces,
					Strength = step.Strength >= 0 ? step.Strength : flight.Strength,
				};
			}

			if (step.Ended)
			{
				Finish(i, FlightEnd.Impact, tick, step.Point, step.Normal);
			}
			else if (age >= flight.Projectile.Lifetime)
			{
				Finish(i, FlightEnd.Expired, tick, to, Vector3.Zero);
			}
		}

		long keep = tick - (long)Math.Ceiling(KeepEndedSeconds / fixedDeltaTime);

		for (int i = _flights.Count - 1; i >= 0; i--)
		{
			if (!_flights[i].IsFlying && _flights[i].EndTick < keep)
			{
				_received.Remove(_flights[i].Id);
				_flights.RemoveAt(i);
			}
		}
	}

	internal ProjectileFlight[] Capture()
	{
		return [.. _flights];
	}

	internal void Restore(ProjectileFlight[] flights)
	{
		_flights.Clear();
		_flights.AddRange(flights);
	}

	// A launch the server sent: for an entity this client mirrors, or one it predicts that didn't predict the launch,
	// which holds through corrections.
	internal void ShowLaunch(in ProjectileFlight flight)
	{
		if (!IsMirrored)
		{
			_received[flight.Id] = flight;
		}

		if (IndexOf(flight.Id) < 0)
		{
			_flights.Add(flight);
		}
	}

	// An end the server sent, which takes the place of one this client gave the flight. On an entity it predicts, it
	// holds through corrections: the ticks run again end the flight where the server did. A flight the client never
	// heard of is shown ending alone.
	internal void ShowEnd(in ProjectileFlight ended)
	{
		int index = IndexOf(ended.Id);

		if (!IsMirrored)
		{
			_confirmed[ended.Id] = ended;
		}

		if (index < 0)
		{
			_flights.Add(ended);
			return;
		}

		_flights[index] = ended;
	}

	// Ends the flights of a mirrored entity that the shown tick takes past their lifetime, which a lost end leaves
	// flying, and lets go of those that ended long enough ago.
	internal void ExpireShown(double shownTick, double fixedDeltaTime)
	{
		for (int i = 0; i < _flights.Count; i++)
		{
			ProjectileFlight flight = _flights[i];
			double age = (shownTick - flight.Tick) * fixedDeltaTime;

			if (flight.IsFlying && age >= flight.Projectile.Lifetime)
			{
				_flights[i] = flight with
				{
					End = FlightEnd.Expired,
					EndTick = flight.Tick + (long)Math.Ceiling(flight.Projectile.Lifetime / fixedDeltaTime),
					EndPoint = flight.PositionAt(flight.Projectile.Lifetime),
				};
			}
		}

		double keep = shownTick - (KeepEndedSeconds / fixedDeltaTime);
		_flights.RemoveAll(x => !x.IsFlying && x.EndTick < keep);
	}

	// Forgets the server's ends older than a tick, which no correction runs again.
	internal void ForgetConfirmedBefore(long tick)
	{
		_gone.Clear();

		foreach (KeyValuePair<ShotId, ProjectileFlight> confirmed in _confirmed)
		{
			if (confirmed.Value.EndTick < tick)
			{
				_gone.Add(confirmed.Key);
			}
		}

		foreach (ShotId id in _gone)
		{
			_confirmed.Remove(id);
		}
	}

	private void Finish(int index, FlightEnd end, long tick, Vector3 point, Vector3 normal)
	{
		ProjectileFlight flight = _flights[index] with
		{
			End = end,
			EndTick = tick,
			EndPoint = point,
			EndNormal = normal,
		};

		_flights[index] = flight;
		Observer?.Ended(this, flight);
	}

	private int IndexOf(ShotId id)
	{
		for (int i = 0; i < _flights.Count; i++)
		{
			if (_flights[i].Id == id)
			{
				return i;
			}
		}

		return -1;
	}
}
