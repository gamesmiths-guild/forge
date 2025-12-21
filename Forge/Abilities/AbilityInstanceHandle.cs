// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Slim handle for controlling a single active ability instance (end / cancel).
/// Additional context (owner, source, level, commits) lives in <see cref="AbilityBehaviorContext"/>.
/// </summary>
public sealed class AbilityInstanceHandle
{
	private AbilityInstance? _instance;

	/// <summary>
	/// Gets the target entity of this ability instance.
	/// </summary>
	public IForgeEntity? Target => _instance?.Target;

	/// <summary>
	/// Gets a value indicating whether this ability instance is currently active.
	/// </summary>
	public bool IsActive => _instance?.IsActive ?? false;

	/// <summary>
	/// Gets a value indicating whether the handle is valid.
	/// </summary>
	public bool IsValid => _instance is not null;

	internal AbilityInstanceHandle(AbilityInstance instance)
	{
		_instance = instance;
	}

	/// <summary>
	/// Ends the ability instance.
	/// </summary>
	public void End()
	{
		_instance?.End();
	}

	internal void Free()
	{
		_instance = null;
	}
}
