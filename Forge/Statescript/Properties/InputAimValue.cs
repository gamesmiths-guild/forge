// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Which part of an input's aim an <see cref="InputAimResolver"/> resolves.
/// </summary>
public enum InputAimValue
{
	/// <summary>
	/// The point aimed at, in 3D.
	/// </summary>
	Point3D = 0,

	/// <summary>
	/// The point aimed at, in 2D: its X and Y.
	/// </summary>
	Point2D = 1,

	/// <summary>
	/// The direction aimed in, in 3D.
	/// </summary>
	Direction3D = 2,

	/// <summary>
	/// The direction aimed in, in 2D: its X and Y.
	/// </summary>
	Direction2D = 3,
}
