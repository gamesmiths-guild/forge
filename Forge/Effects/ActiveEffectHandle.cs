// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Represents a handle to an active effect.
/// </summary>
public class ActiveEffectHandle
{
	internal ActiveEffect? ActiveEffect { get; private set; }

	internal ActiveEffectHandle(ActiveEffect activeEffect)
	{
		ActiveEffect = activeEffect;
	}

	/// <summary>
	/// Sets the inhibition of the effect.
	/// </summary>
	/// <param name="value">The desired inhibition value.</param>
	public void SetInhibit(bool value)
	{
		ActiveEffect?.SetInhibit(value);
	}

	internal void Free()
	{
		ActiveEffect = null;
	}
}
