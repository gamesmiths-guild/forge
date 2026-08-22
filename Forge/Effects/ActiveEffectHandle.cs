// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Represents a handle to an active effect.
/// </summary>
public class ActiveEffectHandle : IValidatable
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

	/// <summary>
	/// Gets the remaining duration of the effect, in seconds.
	/// </summary>
	/// <remarks>
	/// Returns <c>-1</c> for infinite effects and <c>0</c> for invalid handles.
	/// </remarks>
	public double RemainingDuration
	{
		get
		{
			if (ActiveEffect is null)
			{
				return 0;
			}

			if (ActiveEffect.EffectData.DurationData.DurationType == DurationType.Infinite)
			{
				return -1;
			}

			return ActiveEffect.RemainingDuration;
		}
	}

	/// <summary>
	/// Gets the total evaluated duration of the effect, in seconds.
	/// </summary>
	/// <remarks>
	/// Returns <c>-1</c> for infinite effects and <c>0</c> for invalid handles.
	/// </remarks>
	public double TotalDuration
	{
		get
		{
			if (ActiveEffect is null)
			{
				return 0;
			}

			if (ActiveEffect.EffectData.DurationData.DurationType == DurationType.Infinite)
			{
				return -1;
			}

			return ActiveEffect.EffectEvaluatedData.Duration;
		}
	}

	/// <summary>
	/// Gets the current stack count of the effect, or <c>0</c> for invalid handles.
	/// </summary>
	public int StackCount => ActiveEffect?.StackCount ?? 0;

	/// <summary>
	/// Gets the current evaluated level of the effect, or <c>0</c> for invalid handles.
	/// </summary>
	public int Level => ActiveEffect?.EffectEvaluatedData.Level ?? 0;

	/// <summary>
	/// Gets the number of times the effect has executed, or <c>0</c> for invalid handles.
	/// </summary>
	/// <remarks>
	/// Only periodic and instant executions increase this count; non-periodic duration effects never execute.
	/// </remarks>
	public int ExecutionCount => ActiveEffect?.ExecutionCount ?? 0;

	/// <summary>
	/// Gets the evaluated period of the effect, in seconds.
	/// </summary>
	/// <remarks>
	/// Returns <c>0</c> for non-periodic effects and invalid handles.
	/// </remarks>
	public double Period => ActiveEffect?.EffectEvaluatedData.Period ?? 0;

	/// <summary>
	/// Gets the entity this effect is applied to, or <c>null</c> for invalid handles.
	/// </summary>
	public IForgeEntity? Target => ActiveEffect?.EffectEvaluatedData.Target;

	/// <summary>
	/// Gets the effect instance behind this active effect, or <c>null</c> for invalid handles.
	/// </summary>
	public Effect? Effect => ActiveEffect?.Effect;

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
	/// Resets the remaining duration of the effect back to its total evaluated duration.
	/// </summary>
	/// <remarks>
	/// Does nothing for infinite effects or invalid handles.
	/// </remarks>
	public void RefreshDuration()
	{
		if (ActiveEffect is null
			|| ActiveEffect.EffectData.DurationData.DurationType != DurationType.HasDuration)
		{
			return;
		}

		ActiveEffect.RemainingDuration = ActiveEffect.EffectEvaluatedData.Duration;
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
