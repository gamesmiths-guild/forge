// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Represents a handle to a granted ability.
/// </summary>
public class AbilityHandle
{
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

	internal void Free()
	{
		Ability = null;
	}
}
