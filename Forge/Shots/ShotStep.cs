// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Shots;

/// <summary>
/// What a tick's stretch of a projectile's path met, from <see cref="IShotHost.Fly"/>.
/// </summary>
/// <param name="Ended">Whether the flight ends here.</param>
/// <param name="Point">Where it ends.</param>
/// <param name="Normal">The normal of the surface it hit, or zero.</param>
/// <param name="Pierces">How many colliders the projectile has gone through by the end of the stretch, or -1 for as
/// many as before.</param>
/// <param name="Strength">The share of its strength it has left by then, or -1 for as much as before.</param>
public readonly record struct ShotStep(bool Ended, Vector3 Point, Vector3 Normal, int Pierces = -1, float Strength = -1)
{
	/// <summary>
	/// Gets a step that met nothing to end on: the projectile flies on.
	/// </summary>
	public static ShotStep Flying => new(false, Vector3.Zero, Vector3.Zero);

	/// <summary>
	/// Gives a step the projectile flew on from, having gone through colliders on the way.
	/// </summary>
	/// <param name="pierces">How many colliders it has gone through in all.</param>
	/// <param name="strength">The share of its strength it has left.</param>
	/// <returns>The step.</returns>
	public static ShotStep Passed(int pierces, float strength)
	{
		return new ShotStep(false, Vector3.Zero, Vector3.Zero, pierces, strength);
	}

	/// <summary>
	/// Gives a step that ends the flight.
	/// </summary>
	/// <param name="point">Where it ends.</param>
	/// <param name="normal">The normal of the surface it hit, or zero.</param>
	/// <returns>The step.</returns>
	public static ShotStep Impact(Vector3 point, Vector3 normal = default)
	{
		return new ShotStep(true, point, normal);
	}
}
