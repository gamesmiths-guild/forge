// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Attribute = Gamesmiths.Forge.Attributes.Attribute;

namespace Gamesmiths.Forge.GameplayEffects;

internal readonly struct ModifierEvaluatedData
{
	public Attribute Attribute { get; }

	public ModifierOperation ModifierOperation { get; }

	public float Magnitude { get; }

	public int Channel { get; }

	public bool Snapshot { get; }

	public Attribute? BackingAttribute { get; }

	public ModifierEvaluatedData(
		Attribute attribute,
		ModifierOperation modifierOperation,
		float magnitude,
		int channel,
		bool snapshot,
		Attribute? backingAttribute)
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
	}
}
