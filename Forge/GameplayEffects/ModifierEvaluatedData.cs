// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Attribute = Gamesmiths.Forge.Core.Attribute;

namespace Gamesmiths.Forge.GameplayEffects;

/// <summary>
/// Represents the precomputed static data for a modifier.
/// </summary>
/// <remarks>
/// Optimizes performance by avoiding repeated complex calculations and serves as data for event arguments.
/// </remarks>
public readonly struct ModifierEvaluatedData
{
	/// <summary>
	/// Gets the target attribute of this modifier.
	/// </summary>
	public Attribute Attribute { get; }

	/// <summary>
	/// Gets the type of modifier operation for this modifier.
	/// </summary>
	public ModifierOperation ModifierOperation { get; }

	/// <summary>
	/// Gets the evaluated magnitude for this modifier at the moment of evaluation.
	/// </summary>
	public float Magnitude { get; }

	/// <summary>
	/// Gets the channel used for this modifier.
	/// </summary>
	public int Channel { get; }

	/// <summary>
	/// Gets the attribute override data if this modifier is an override.
	/// </summary>
	public AttributeOverride? AttributeOverride { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ModifierEvaluatedData"/> struct.
	/// </summary>
	/// <param name="attribute">The target attribute of this modifier.</param>
	/// <param name="modifierOperation">The type of modifier operation.</param>
	/// <param name="magnitude">The final evaluated magnitude.</param>
	/// <param name="channel">The final channel for this modifier.</param>
	public ModifierEvaluatedData(
		Attribute attribute,
		ModifierOperation modifierOperation,
		float magnitude,
		int channel)
	{
		Attribute = attribute;
		ModifierOperation = modifierOperation;
		Magnitude = magnitude;
		Channel = channel;

		if (modifierOperation == ModifierOperation.Override)
		{
			AttributeOverride = new AttributeOverride((int)magnitude, channel);
		}
	}
}
