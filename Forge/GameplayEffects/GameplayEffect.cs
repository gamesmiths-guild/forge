// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Modifiers;

namespace Gamesmiths.Forge.GameplayEffects;

/// <summary>
/// A runtime version of a <see cref="GameplayEffectData"/> used to apply the effects with level and ownership
/// information.
/// </summary>
/// <param name="effectData">The configuration data for this effect.</param>
/// <param name="ownership">The ownership info for this effect.</param>
/// <param name="level">The initial level for this effect.</param>
public class GameplayEffect(GameplayEffectData effectData, GameplayEffectOwnership ownership, int level = 1)
{
	private int _level = level;

	/// <summary>
	/// Event triggered when the level of this effect changes.
	/// </summary>
	public event Action<int>? OnLevelChanged;

	/// <summary>
	/// Gets the configuration data for this effect.
	/// </summary>
	public GameplayEffectData EffectData { get; } = effectData;

	/// <summary>
	/// Gets information about the owership and source of this effect.
	/// </summary>
	public GameplayEffectOwnership Ownership { get; } = ownership;

	/// <summary>
	/// Gets the current level o this effect.
	/// </summary>
	public int Level { get; }

	/// <summary>
	/// Level up this effect by exactly one level.
	/// </summary>
	public void LevelUp()
	{
		_level++;
		OnLevelChanged?.Invoke(_level);
	}

	/// <summary>
	/// Sets the current level of this effect.
	/// </summary>
	/// <param name="level">The level for the effect to be set at.</param>
	public void SetLevel(int level)
	{
		_level = level;
		OnLevelChanged?.Invoke(_level);
	}

	internal static void Execute(in GameplayEffectEvaluatedData effectEvaluatedData)
	{
		foreach (ModifierEvaluatedData modifier in effectEvaluatedData.ModifiersEvaluatedData)
		{
			switch (modifier.ModifierOperation)
			{
				case ModifierOperation.Flat:
					modifier.Attribute.ExecuteFlatModifier((int)modifier.Magnitude);
					break;

				case ModifierOperation.Percent:
					modifier.Attribute.ExecutePercentModifier(modifier.Magnitude);
					break;

				case ModifierOperation.Override:
					modifier.Attribute.ExecuteOverride((int)modifier.Magnitude);
					break;
			}
		}
	}

	internal void Execute(IForgeEntity target)
	{
		Execute(new GameplayEffectEvaluatedData(this, target));
	}
}
