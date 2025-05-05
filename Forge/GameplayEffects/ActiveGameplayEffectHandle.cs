// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects;

/// <summary>
/// Represents a handle to an active gameplay effect.
/// </summary>
public class ActiveGameplayEffectHandle
{
	internal ActiveGameplayEffect? ActiveGameplayEffect { get; private set; }

	internal ActiveGameplayEffectHandle(ActiveGameplayEffect activeGameplayEffect)
	{
		ActiveGameplayEffect = activeGameplayEffect;
	}

	/// <summary>
	/// Sets the inhibition of the gameplay effect.
	/// </summary>
	/// <param name="value">The desired inhibition value.</param>
	public void SetInhibit(bool value)
	{
		ActiveGameplayEffect?.SetInhibit(value);
	}

	internal void Free()
	{
		ActiveGameplayEffect = null;
	}
}
