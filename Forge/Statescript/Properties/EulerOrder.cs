// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Defines the order used when converting between Euler angles and quaternions.
/// </summary>
public enum EulerOrder
{
	/// <summary>
	/// Rotate around X, then Y, then Z.
	/// </summary>
	XYZ = 0,

	/// <summary>
	/// Rotate around X, then Z, then Y.
	/// </summary>
	XZY = 1,

	/// <summary>
	/// Rotate around Y, then X, then Z.
	/// </summary>
	YXZ = 2,

	/// <summary>
	/// Rotate around Y, then Z, then X.
	/// </summary>
	YZX = 3,

	/// <summary>
	/// Rotate around Z, then X, then Y.
	/// </summary>
	ZXY = 4,

	/// <summary>
	/// Rotate around Z, then Y, then X.
	/// </summary>
	ZYX = 5,
}
