// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Shots;

/// <summary>
/// How a projectile's flight ended.
/// </summary>
public enum FlightEnd : byte
{
	/// <summary>
	/// The projectile is still flying.
	/// </summary>
	None = 0,

	/// <summary>
	/// It hit something, or detonated where it was.
	/// </summary>
	Impact = 1,

	/// <summary>
	/// A shot hit it in flight.
	/// </summary>
	Struck = 2,

	/// <summary>
	/// It flew its whole lifetime.
	/// </summary>
	Expired = 3,
}
