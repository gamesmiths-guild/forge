// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Runtime context for a single ability activation.
/// </summary>
public class AbilityBehaviorContext
{
	// What the server said of its activation, for a graph a client runs for the server's entity.
	private readonly IForgeEntity? _remoteTarget;
	private readonly int _remoteLevel;

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
	public IForgeEntity? Target => InstanceHandle.IsValid ? InstanceHandle.Target : _remoteTarget;

	/// <summary>
	/// Gets the level of the ability at the time of execution.
	/// </summary>
	public int Level => AbilityHandle.IsValid ? AbilityHandle.Level : _remoteLevel;

	/// <summary>
	/// Gets the ownership configuration derived from the current ability activation.
	/// </summary>
	public EffectOwnership Ownership => new(Owner, Source);

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

	/// <summary>
	/// Gets the type of the activation data carried by <see cref="AbilityBehaviorContext{TData}"/>, or
	/// <see langword="null"/> when the ability was activated without data.
	/// </summary>
	public virtual Type? DataType => null;

	internal AbilityBehaviorContext(Ability ability, AbilityInstance instance, float magnitude)
	{
		AbilityHandle = ability.Handle;
		InstanceHandle = instance.Handle;
		Owner = ability.Owner;
		Source = ability.SourceEntity;
		Magnitude = magnitude;
	}

	// The activation the server ran, for the copy of its graph a client runs on the server's entity, where the entity
	// has no ability of its own.
	internal AbilityBehaviorContext(
		IForgeEntity owner,
		IForgeEntity? source,
		IForgeEntity? target,
		int level,
		float magnitude)
	{
		AbilityHandle = new AbilityHandle(null);
		InstanceHandle = new AbilityInstanceHandle();
		Owner = owner;
		Source = source;
		Magnitude = magnitude;
		_remoteTarget = target;
		_remoteLevel = level;
	}

	internal virtual TResult AcceptData<TResult>(IActivationDataVisitor<TResult> visitor)
	{
		return default!;
	}
}
