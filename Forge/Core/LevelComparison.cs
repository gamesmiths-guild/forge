// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Flags for comparing levels.
/// </summary>
[Flags]
public enum LevelComparison : byte
{
	/// <summary>
	/// No value has been set.
	/// </summary>
	None = 0,

	/// <summary>
	/// Equal values.
	/// </summary>
	Equal = 1 << 0,

	/// <summary>
	/// Higher values.
	/// </summary>
	Higher = 1 << 1,

	/// <summary>
	/// Lower values.
	/// </summary>
	Lower = 1 << 2,
}
