// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Represents a handle to a granted ability.
/// </summary>
public class AbilityHandle
{
	/// <summary>
	/// Gets a value indicating whether the ability associated with this handle is valid and active.
	/// </summary>
	public bool IsActive => Ability?.IsActive == true;

	/// <summary>
	/// Gets a value indicating whether the ability associated with this handle is currently inhibited.
	/// </summary>
	public bool IsInhibited => Ability?.IsInhibited == true;

	/// <summary>
	/// Gets a value indicating the level of the ability associated with this handle.
	/// </summary>
	public int Level => Ability?.Level ?? 0;

	internal Ability? Ability { get; private set; }

	internal AbilityHandle(Ability ability)
	{
		Ability = ability;
	}

	/// <summary>
	/// Activates the ability associated with this handle.
	/// </summary>
	public void Activate()
	{
		Ability?.Activate();
	}

	/// <summary>
	/// End the ability associated with this handle.
	/// </summary>
	public void End()
	{
		Ability?.Deactivate();
	}

	internal void Free()
	{
		Ability = null;
	}
}
