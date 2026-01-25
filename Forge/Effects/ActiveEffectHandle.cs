// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Components;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Represents a handle to an active effect.
/// </summary>
public class ActiveEffectHandle
{
	/// <summary>
	/// Gets a value indicating whether the effect is currently inhibited.
	/// </summary>
	public bool IsInhibited => ActiveEffect?.IsInhibited ?? false;

	/// <summary>
	/// Gets a value indicating whether the handle is valid.
	/// </summary>
	public bool IsValid => ActiveEffect is not null;

	/// <summary>
	/// Gets the component instances for this active effect.
	/// </summary>
	/// <remarks>
	/// These are the actual component instances used for this specific effect application,
	/// which may hold per-instance state (such as granted abilities for <see cref="GrantAbilityEffectComponent"/>).
	/// </remarks>
	public IReadOnlyList<IEffectComponent> ComponentInstances => ActiveEffect?.ComponentInstances ?? [];

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

	/// <summary>
	/// Gets the first component instance of the specified type.
	/// </summary>
	/// <typeparam name="T">The type of component to find.</typeparam>
	/// <returns>The component instance, or <c>null</c> if not found.</returns>
	public T? GetComponent<T>()
		where T : class, IEffectComponent
	{
		if (ActiveEffect is null)
		{
			return null;
		}

		foreach (IEffectComponent component in ActiveEffect.ComponentInstances)
		{
			if (component is T typed)
			{
				return typed;
			}
		}

		return null;
	}

	internal void Free()
	{
		ActiveEffect = null;
	}
}
