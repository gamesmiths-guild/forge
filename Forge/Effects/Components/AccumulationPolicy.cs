// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Specifies which attribute movements an <see cref="AttributeAccumulatorEffectComponent"/> adds to its running total.
/// </summary>
public enum AccumulationPolicy : byte
{
	/// <summary>
	/// Counts only the executions that lowered the attribute, as a positive total. A curse tallying the damage it
	/// dealt, or a shield reporting what it absorbed.
	/// </summary>
	Losses = 0,

	/// <summary>
	/// Counts only the executions that raised the attribute, as a positive total. A regeneration effect tallying how
	/// much it actually restored, which is not what it tried to restore once the attribute hits its maximum.
	/// </summary>
	Gains = 1,

	/// <summary>
	/// Counts every execution, signed, so that gains and losses cancel out. Positive means the attribute ended up
	/// higher than it started.
	/// </summary>
	Net = 2,
}
