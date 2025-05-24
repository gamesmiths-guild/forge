// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// A runtime version of a <see cref="Effects.EffectData"/> used to apply the effects with level and ownership
/// information.
/// </summary>
/// <param name="effectData">The configuration data for this effect.</param>
/// <param name="ownership">The ownership info for this effect.</param>
/// <param name="level">The initial level for this effect.</param>
public class Effect(EffectData effectData, EffectOwnership ownership, int level = 1)
{
	/// <summary>
	/// Event triggered when the level of this effect changes.
	/// </summary>
	public event Action<int>? OnLevelChanged;

	/// <summary>
	/// Gets the configuration data for this effect.
	/// </summary>
	public EffectData EffectData { get; } = effectData;

	/// <summary>
	/// Gets information about the owership and source of this effect.
	/// </summary>
	public EffectOwnership Ownership { get; } = ownership;

	/// <summary>
	/// Gets the current level o this effect.
	/// </summary>
	public int Level { get; private set; } = level;

	/// <summary>
	/// Gets the mapping for custom <see cref="SetByCallerFloat"/> magnitudes.
	/// </summary>
	public Dictionary<Tag, float> DataTag { get; } = [];

	/// <summary>
	/// Level up this effect by exactly one level.
	/// </summary>
	public void LevelUp()
	{
		Level++;
		OnLevelChanged?.Invoke(Level);
	}

	/// <summary>
	/// Sets the current level of this effect.
	/// </summary>
	/// <param name="level">The level for the effect to be set at.</param>
	public void SetLevel(int level)
	{
		Level = level;
		OnLevelChanged?.Invoke(Level);
	}

	/// <summary>
	/// Sets a custom magnitude to be used by <see cref="SetByCallerFloat"/> magnitudes.
	/// </summary>
	/// <param name="identifierTag">Tag used to identify a custom value to be used.</param>
	/// <param name="magnitude">The magnitude to be set for the given tag.</param>
	public void SetSetByCallerMagnitude(Tag identifierTag, float magnitude)
	{
		DataTag.Add(identifierTag, magnitude);
	}

	internal static void Execute(in EffectEvaluatedData effectEvaluatedData)
	{
		foreach (ModifierEvaluatedData modifier in effectEvaluatedData.ModifiersEvaluatedData)
		{
			switch (modifier.ModifierOperation)
			{
				case ModifierOperation.FlatBonus:
					modifier.Attribute.ExecuteFlatModifier((int)modifier.Magnitude);
					break;

				case ModifierOperation.PercentBonus:
					modifier.Attribute.ExecutePercentModifier(modifier.Magnitude);
					break;

				case ModifierOperation.Override:
					modifier.Attribute.ExecuteOverride((int)modifier.Magnitude);
					break;
			}
		}

		effectEvaluatedData.Target.EffectsManager.OnEffectExecuted_InternalCall(effectEvaluatedData);

		effectEvaluatedData.Target.Attributes.ApplyPendingValueChanges();
	}

	internal bool CanApply(IForgeEntity target)
	{
		foreach (IEffectComponent component in EffectData.EffectComponents)
		{
			Effect effect = this;
			if (!component.CanApplyEffect(in target, in effect))
			{
				return false;
			}
		}

		return true;
	}
}
