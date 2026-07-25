// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Selects which aggregate an <see cref="EffectStackDataResolver"/> computes over the active applications of an
/// effect.
/// </summary>
public enum EffectStackDataType : byte
{
	/// <summary>
	/// The sum of the stack counts across every active application.
	/// </summary>
	TotalStackCount = 0,

	/// <summary>
	/// The number of active applications.
	/// </summary>
	InstanceCount = 1,

	/// <summary>
	/// The highest level among the active applications.
	/// </summary>
	MaxLevel = 2,
}
