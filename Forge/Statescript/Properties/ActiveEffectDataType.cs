// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Selects which value an <see cref="ActiveEffectDataResolver"/> reads from an active effect.
/// </summary>
public enum ActiveEffectDataType : byte
{
	/// <summary>
	/// The remaining duration in seconds (<see langword="double"/>). Returns <c>-1</c> for infinite effects.
	/// </summary>
	RemainingDuration = 0,

	/// <summary>
	/// The total evaluated duration in seconds (<see langword="double"/>). Returns <c>-1</c> for infinite effects.
	/// </summary>
	TotalDuration = 1,

	/// <summary>
	/// The remaining duration as a fraction of the total duration (<see langword="double"/>, 0 to 1). Returns
	/// <c>1</c> for infinite effects.
	/// </summary>
	RemainingFraction = 2,

	/// <summary>
	/// The current stack count (<see langword="int"/>).
	/// </summary>
	StackCount = 3,

	/// <summary>
	/// The current evaluated level (<see langword="int"/>).
	/// </summary>
	Level = 4,

	/// <summary>
	/// The number of executions so far (<see langword="int"/>).
	/// </summary>
	ExecutionCount = 5,

	/// <summary>
	/// The evaluated period in seconds (<see langword="double"/>). Returns <c>0</c> for non-periodic effects.
	/// </summary>
	Period = 6,

	/// <summary>
	/// Whether the effect is currently inhibited (<see langword="bool"/>).
	/// </summary>
	IsInhibited = 7,

	/// <summary>
	/// Whether the handle still points to an active effect (<see langword="bool"/>).
	/// </summary>
	IsValid = 8,
}
