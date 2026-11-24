// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Shots;

/// <summary>
/// A kind of lightweight projectile: a shot that flies along a path every machine works out from its launch, rather
/// than an entity whose state is sent as it flies.
/// </summary>
/// <remarks>
/// <para>What the projectile does when it hits is the host's: <see cref="IShotHost.Fly"/> sweeps each tick's stretch
/// of the path and applies its effects. This holds what every machine needs to agree on the path and how long it lasts.
/// </para>
/// <para>A networked session finds it by its place in the asset registry
/// (<see cref="Networking.AssetKind.Projectile"/>), so every machine registers the same ones.</para>
/// </remarks>
/// <param name="name">The projectile's name, for debugging.</param>
/// <param name="gravity">The acceleration the projectile falls by, in units per second squared. Zero flies straight.
/// </param>
/// <param name="radius">The projectile's radius, which a shot that hits projectiles tests against.</param>
/// <param name="lifetime">How long the projectile flies before it ends on its own, in seconds.</param>
/// <param name="shootable">Whether a shot can hit the projectile in flight.</param>
public sealed class ProjectileData(
	string name,
	Vector3 gravity,
	float radius,
	double lifetime,
	bool shootable = false)
{
	/// <summary>
	/// Gets the projectile's name, for debugging.
	/// </summary>
	public string Name { get; } = name;

	/// <summary>
	/// Gets the acceleration the projectile falls by, in units per second squared.
	/// </summary>
	public Vector3 Gravity { get; } = gravity;

	/// <summary>
	/// Gets the projectile's radius.
	/// </summary>
	public float Radius { get; } = radius;

	/// <summary>
	/// Gets how long the projectile flies before it ends on its own, in seconds.
	/// </summary>
	public double Lifetime { get; } = lifetime;

	/// <summary>
	/// Gets a value indicating whether a shot can hit the projectile in flight.
	/// </summary>
	public bool Shootable { get; } = shootable;

	/// <inheritdoc/>
	public override string ToString()
	{
		return Name;
	}
}
