// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// A <see cref="BodyState"/> as it travels: the position in thousandths of a unit, the velocity in hundredths, the
/// rotation as its three smallest components in 9 bits each, and whether the body stands on the ground. Two states that
/// quantize the same are the same on the wire, so a body that hasn't moved costs nothing.
/// </summary>
/// <param name="X">The position's X, in steps of <see cref="BodyCodec.PositionStep"/>.</param>
/// <param name="Y">The position's Y, in steps of <see cref="BodyCodec.PositionStep"/>.</param>
/// <param name="Z">The position's Z, in steps of <see cref="BodyCodec.PositionStep"/>.</param>
/// <param name="Largest">Which of the rotation's four components is the largest, left out and rebuilt from the other
/// three.</param>
/// <param name="A">The first of the other three components, as a level.</param>
/// <param name="B">The second of the other three components, as a level.</param>
/// <param name="C">The third of the other three components, as a level.</param>
/// <param name="VelocityX">The velocity's X, in steps of <see cref="BodyCodec.VelocityStep"/>.</param>
/// <param name="VelocityY">The velocity's Y, in steps of <see cref="BodyCodec.VelocityStep"/>.</param>
/// <param name="VelocityZ">The velocity's Z, in steps of <see cref="BodyCodec.VelocityStep"/>.</param>
/// <param name="Grounded">Whether the body stands on the ground.</param>
public readonly record struct QuantizedBody(
	long X,
	long Y,
	long Z,
	byte Largest,
	ushort A,
	ushort B,
	ushort C,
	long VelocityX,
	long VelocityY,
	long VelocityZ,
	bool Grounded);
