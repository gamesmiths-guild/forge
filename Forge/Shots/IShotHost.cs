// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Shots;

/// <summary>
/// What an engine does for an entity's lightweight projectiles: tests each tick's stretch of their paths against its
/// world, and shows them.
/// </summary>
/// <remarks>
/// <para>Set it as an entity's <see cref="EntityShots.Host"/>. Forge keeps the flights, rolls them back with the
/// entity, and sends their launches and ends to clients; the host does what needs the engine.</para>
/// <para>What it shows follows the flights by their net change, as a presentation pass does: a flight a correction
/// takes away and brings back as it was is shown once.</para>
/// </remarks>
public interface IShotHost
{
	/// <summary>
	/// Tests a tick's stretch of a flight's path against the world, and applies what the projectile does to what it
	/// hits.
	/// </summary>
	/// <remarks>
	/// It runs in the launching entity's tick, on every machine that simulates the entity: the server, and the client
	/// that predicts it. Apply effects to what it hits as a graph's node would; on that client, those on entities it
	/// mirrors show their cues at once and the server's decide.
	/// </remarks>
	/// <param name="flight">The flight.</param>
	/// <param name="start">Where the stretch starts.</param>
	/// <param name="finish">Where it ends.</param>
	/// <returns>Whether the flight ends in the stretch, and where.</returns>
	ShotStep Fly(in ProjectileFlight flight, Vector3 start, Vector3 finish);

	/// <summary>
	/// Starts showing a flight.
	/// </summary>
	/// <param name="flight">The flight.</param>
	void Show(in ProjectileFlight flight)
	{
	}

	/// <summary>
	/// Shows how a flight ended: an impact, a projectile struck in flight, or one that flew out its lifetime.
	/// </summary>
	/// <remarks>
	/// What <see cref="Show"/> started stops here as well. A flight shown ending isn't shown ending again.
	/// </remarks>
	/// <param name="flight">The flight, with its <see cref="ProjectileFlight.End"/>.</param>
	void ShowEnd(in ProjectileFlight flight)
	{
	}

	/// <summary>
	/// Stops showing a flight that's gone without an end to show, as when a correction takes back its launch.
	/// </summary>
	/// <param name="flight">The flight.</param>
	void Hide(in ProjectileFlight flight)
	{
	}
}
