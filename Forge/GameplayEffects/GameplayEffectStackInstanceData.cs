// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayEffects;

/// <summary>
/// Data with information about an applied gameplay effect stack.
/// </summary>
/// <param name="owner">The owner of the effect stack.</param>
/// <param name="effectLevel">The level of the effect stack.</param>
/// <param name="stackCount">The number of stacks in the effect stack.</param>
public readonly struct GameplayEffectStackInstanceData(IForgeEntity owner, int effectLevel, int stackCount)
{
	/// <summary>
	/// Gets the owner of this effect stack.
	/// </summary>
	public IForgeEntity Owner { get; } = owner;

	/// <summary>
	/// Gets the level of this effect stack.
	/// </summary>
	public int EffectLevel { get; } = effectLevel;

	/// <summary>
	/// Gets the stack count of this effect stack.
	/// </summary>
	public int StackCount { get; } = stackCount;
}
