// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
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
	/// Gets a value indicating whether this modifier is set to snapshot.
	/// </summary>
	public bool Snapshot { get; }

	/// <summary>
	/// Gets the backing attribute for this modifier in case there is one.
	/// </summary>
	public Attribute? BackingAttribute { get; }

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
	/// <param name="snapshot">Wheter this modifier snapshots or not.</param>
	/// <param name="backingAttribute">A target backing attribute if there is one.</param>
	internal ModifierEvaluatedData(
		Attribute attribute,
		ModifierOperation modifierOperation,
		float magnitude,
		int channel,
		bool snapshot,
		Attribute? backingAttribute = null)
	{
		Debug.Assert(
			snapshot || backingAttribute is not null,
			"All non-snapshots modifiers should have a BackingAttribute set.");

		Attribute = attribute;
		ModifierOperation = modifierOperation;
		Magnitude = magnitude;
		Channel = channel;
		Snapshot = snapshot;
		BackingAttribute = backingAttribute;

		if (modifierOperation == ModifierOperation.Override)
		{
			AttributeOverride = new AttributeOverride((int)magnitude, channel);
		}
	}
}
