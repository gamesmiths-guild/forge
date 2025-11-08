// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Runtime context for a single ability activation. Provides data and helpers for user behaviors.
/// </summary>
public sealed class AbilityBehaviorContext
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
	public IForgeEntity? Target => Instance.Target;

	/// <summary>
	/// Gets the level of the ability at the time of execution.
	/// </summary>
	public int Level => Ability.Level;

	/// <summary>
	/// Gets the handle to the ability being executed.
	/// </summary>
	/// <remarks>
	/// Ability-level control (affects the granted ability as a whole: commit, cancel all, end last).
	/// </remarks>
	public AbilityHandle Ability { get; }

	/// <summary>
	/// Gets the public instance handle for per-instance operations.
	/// </summary>
	/// <remarks>
	/// This instance control (affects only this execution: end/cancel).
	/// </remarks>
	public AbilityInstanceHandle Instance { get; }

	internal AbilityBehaviorContext(Ability ability, AbilityInstance instance)
	{
		Ability = ability.Handle;
		Instance = new AbilityInstanceHandle(instance);

		Owner = ability.Owner;
		Source = ability.SourceEntity;
	}
}
