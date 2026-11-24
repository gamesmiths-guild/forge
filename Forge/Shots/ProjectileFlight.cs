// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Shots;

/// <summary>
/// A lightweight projectile's flight: where and when it was launched, and how it ended once it has.
/// </summary>
/// <remarks>
/// Its path is a function of its launch alone, so every machine that hears of the launch flies it along the same
/// points: see <see cref="PositionAt(double)"/>.
/// </remarks>
/// <param name="Id">The name the launching entity gave it.</param>
/// <param name="Projectile">What kind of projectile it is.</param>
/// <param name="Instigator">The entity that launched it.</param>
/// <param name="Origin">Where it was at the end of the tick it was launched in.</param>
/// <param name="Velocity">Its velocity at launch, in units per second.</param>
/// <param name="End">How its flight ended, or <see cref="FlightEnd.None"/> while it flies.</param>
/// <param name="EndTick">The tick its flight ended in, or -1 while it flies.</param>
/// <param name="EndPoint">Where it ended.</param>
/// <param name="EndNormal">The normal of the surface it hit, or zero.</param>
/// <param name="Pierces">How many colliders it has gone through.</param>
/// <param name="Strength">The share of its strength it has left, 1 at launch, as going through colliders costs it.
/// </param>
public readonly record struct ProjectileFlight(
	ShotId Id,
	ProjectileData Projectile,
	IForgeEntity Instigator,
	Vector3 Origin,
	Vector3 Velocity,
	FlightEnd End = FlightEnd.None,
	long EndTick = -1,
	Vector3 EndPoint = default,
	Vector3 EndNormal = default,
	int Pierces = 0,
	float Strength = 1)
{
	/// <summary>
	/// Gets the tick it was launched in.
	/// </summary>
	public long Tick => Id.Tick;

	/// <summary>
	/// Gets a value indicating whether it still flies.
	/// </summary>
	public bool IsFlying => End == FlightEnd.None;

	/// <summary>
	/// Gives where the projectile is a time after its launch tick, along its path, whether or not it still flies then.
	/// </summary>
	/// <param name="age">The time since the end of its launch tick, in seconds.</param>
	/// <returns>The point.</returns>
	public Vector3 PositionAt(double age)
	{
		float seconds = (float)age;
		return Origin + (Velocity * seconds) + (Projectile.Gravity * (0.5f * seconds * seconds));
	}

	/// <summary>
	/// Gives where the projectile is at a tick, which may lie between two, along its path.
	/// </summary>
	/// <param name="tick">The tick.</param>
	/// <param name="fixedDeltaTime">The length of a tick, in seconds.</param>
	/// <returns>The point.</returns>
	public Vector3 PositionAt(double tick, double fixedDeltaTime)
	{
		return PositionAt((tick - Id.Tick) * fixedDeltaTime);
	}

	/// <summary>
	/// Gives whether the projectile is in flight at a tick, which may lie between two: launched by then, and not yet
	/// ended.
	/// </summary>
	/// <param name="tick">The tick.</param>
	/// <returns><see langword="true"/> if it flies then.</returns>
	public bool FliesAt(double tick)
	{
		return tick >= Id.Tick && (IsFlying || tick < EndTick);
	}

	/// <summary>
	/// Tests a ray against the projectile, a sphere of its <see cref="ProjectileData.Radius"/> at a point of its path.
	/// </summary>
	/// <param name="from">Where the ray starts.</param>
	/// <param name="direction">The ray's direction, of unit length.</param>
	/// <param name="length">How far the ray reaches.</param>
	/// <param name="center">Where the projectile is, from <see cref="PositionAt(double, double)"/>.</param>
	/// <param name="distance">How far along the ray it meets the projectile.</param>
	/// <returns><see langword="true"/> if the ray meets it within its length.</returns>
	public bool TestRay(Vector3 from, Vector3 direction, float length, Vector3 center, out float distance)
	{
		Vector3 toCenter = center - from;
		float along = Vector3.Dot(toCenter, direction);
		float radius = Projectile.Radius;
		float apart = (toCenter - (direction * along)).LengthSquared();
		distance = 0;

		if (apart > radius * radius)
		{
			return false;
		}

		float half = MathF.Sqrt((radius * radius) - apart);
		distance = along - half;

		if (distance < 0)
		{
			distance = along + half < 0 ? -1 : 0;
		}

		return distance >= 0 && distance <= length;
	}
}
