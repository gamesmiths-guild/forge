// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// How finely a session rounds body states for the wire. Finer costs bytes, coarser shows: both ends of a session must
/// use the same, and a client whose precision differs is refused.
/// </summary>
/// <param name="PositionSteps">How many steps positions are rounded to in a unit.</param>
/// <param name="VelocitySteps">How many steps velocities are rounded to in a unit per second.</param>
/// <param name="RotationBits">How many bits each of a rotation's three smallest components takes, from 2 to 16.
/// </param>
public readonly record struct BodyPrecision(int PositionSteps, int VelocitySteps, int RotationBits)
{
	/// <summary>
	/// Gets the precision a session uses unless told otherwise: a thousandth of a unit, a hundredth of a unit per
	/// second, and 9 bits a rotation component.
	/// </summary>
	public static BodyPrecision Default { get; } = new(1000, 100, 9);

	// Even, so zero is a level: a body turned only about one axis, or not at all, comes back upright.
	internal int RotationLevels => (1 << RotationBits) - 2;

	internal void Validate()
	{
		if (PositionSteps < 1 || VelocitySteps < 1 || RotationBits is < 2 or > 16)
		{
			throw new ArgumentException(
				$"A body precision needs at least a step in a unit and 2 to 16 rotation bits, not {this}.");
		}
	}
}
