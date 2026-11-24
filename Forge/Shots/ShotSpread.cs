// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Shots;

/// <summary>
/// Spreads shots in a cone around an aim, from a random source, so a seeded source gives every machine the same
/// pellets.
/// </summary>
public static class ShotSpread
{
	/// <summary>
	/// Gives directions spread evenly over a cone around an aim: each one equally likely anywhere in the cone's circle.
	/// </summary>
	/// <remarks>
	/// Pass the graph's or the entity's seeded random, which the server and the client that predicts the shot both
	/// start the same way each tick, so they spread the shot alike.
	/// </remarks>
	/// <param name="random">The random source.</param>
	/// <param name="aim">The aim, of unit length.</param>
	/// <param name="angle">The cone's half angle, in radians. Zero gives the aim every time.</param>
	/// <param name="directions">Where to write the directions, one per shot.</param>
	public static void Spread(IRandom random, Vector3 aim, float angle, Span<Vector3> directions)
	{
		var side = Vector3.Normalize(Vector3.Cross(aim, MathF.Abs(aim.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX));
		var up = Vector3.Cross(side, aim);
		float maxRadius = MathF.Tan(angle);

		for (int i = 0; i < directions.Length; i++)
		{
			float radius = maxRadius * MathF.Sqrt(random.NextSingle());
			float turn = 2 * MathF.PI * random.NextSingle();
			Vector3 offset = (side * (radius * MathF.Cos(turn))) + (up * (radius * MathF.Sin(turn)));
			directions[i] = Vector3.Normalize(aim + offset);
		}
	}

	/// <summary>
	/// Gives directions spread evenly over an arc around an aim in the plane, for a 2D shot.
	/// </summary>
	/// <param name="random">The random source.</param>
	/// <param name="aim">The aim, of unit length, with Z zero.</param>
	/// <param name="angle">The arc's half angle, in radians.</param>
	/// <param name="directions">Where to write the directions, one per shot.</param>
	public static void Spread2D(IRandom random, Vector2 aim, float angle, Span<Vector2> directions)
	{
		for (int i = 0; i < directions.Length; i++)
		{
			float turn = angle * ((2 * random.NextSingle()) - 1);
			float cos = MathF.Cos(turn);
			float sin = MathF.Sin(turn);
			directions[i] = new Vector2((aim.X * cos) - (aim.Y * sin), (aim.X * sin) + (aim.Y * cos));
		}
	}
}
