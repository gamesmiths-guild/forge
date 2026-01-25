// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Runtime context for a single ability activation.
/// </summary>
public class AbilityBehaviorContext
{
	/// <summary>
	/// Gets the owner of this ability.
	/// </summary>
	public IForgeEntity Owner { get; }

	/// <summary>
	/// Gets the source entity that granted this ability.
	/// </summary>
	public IForgeEntity? Source { get; }

	/// <summary>
	/// Gets the target entity of this ability instance.
	/// </summary>
	public IForgeEntity? Target => InstanceHandle.Target;

	/// <summary>
	/// Gets the level of the ability at the time of execution.
	/// </summary>
	public int Level => AbilityHandle.Level;

	/// <summary>
	/// Gets the handle to the ability being executed (ability-level operations).
	/// </summary>
	public AbilityHandle AbilityHandle { get; }

	/// <summary>
	/// Gets the per-instance handle for this execution (end/cancel this instance).
	/// </summary>
	public AbilityInstanceHandle InstanceHandle { get; }

	/// <summary>
	/// Gets the magnitude associated with the activation of this ability, if applicable.
	/// </summary>
	public float Magnitude { get; }

	internal AbilityBehaviorContext(Ability ability, AbilityInstance instance, float magnitude)
	{
		AbilityHandle = ability.Handle;
		InstanceHandle = instance.Handle;
		Owner = ability.Owner;
		Source = ability.SourceEntity;
		Magnitude = magnitude;
	}
}
