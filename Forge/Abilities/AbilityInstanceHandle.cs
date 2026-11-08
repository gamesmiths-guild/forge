// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Slim handle for controlling a single active ability instance (end / cancel).
/// Additional context (owner, source, level, commits) lives in <see cref="AbilityBehaviorContext"/>.
/// </summary>
public sealed class AbilityInstanceHandle : IValidatable
{
	/// <summary>
	/// Gets the target entity of this ability instance.
	/// </summary>
	public IForgeEntity? Target => AbilityInstance?.Target;

	/// <summary>
	/// Gets a value indicating whether this ability instance is currently active.
	/// </summary>
	public bool IsActive => AbilityInstance?.IsActive ?? false;

	/// <summary>
	/// Gets a value indicating whether the handle is valid.
	/// </summary>
	public bool IsValid => AbilityInstance is not null;

	internal AbilityInstance? AbilityInstance { get; private set; }

	internal AbilityInstanceHandle(AbilityInstance instance)
	{
		AbilityInstance = instance;
	}

	// For a graph a client runs for the server's entity, whose ability has no instance there.
	internal AbilityInstanceHandle()
	{
	}

	/// <summary>
	/// Ends the ability instance.
	/// </summary>
	public void End()
	{
		AbilityInstance?.End();
	}

	internal void Free()
	{
		AbilityInstance = null;
	}

	// An entity restore brings back the instance this handle was made for.
	internal void Restore(AbilityInstance instance)
	{
		AbilityInstance = instance;
	}
}
