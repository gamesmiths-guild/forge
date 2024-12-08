// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;

namespace Gamesmiths.Forge.GameplayEffects.Modifiers;

/// <summary>
/// A modifier that affects an attribute with the given configuration.
/// </summary>
/// <param name="attribute">The target attribute to be modified.</param>
/// <param name="operation">The type of operation to be used.</param>
/// <param name="magnitude">The magnitude calculation and configurations to be used.</param>
/// <param name="channel">The channel to be affected by this modifier.</param>
public readonly struct Modifier(
	StringKey attribute,
	ModifierOperation operation,
	ModifierMagnitude magnitude,
	int channel = 0)
{
	/// <summary>
	/// Gets the attribute being modified by this modifier.
	/// </summary>
	public StringKey Attribute { get; } = attribute;

	/// <summary>
	/// Gets the type of operation this modifier is using.
	/// </summary>
	public ModifierOperation Operation { get; } = operation;

	/// <summary>
	/// Gets the magnitude calculation and configurations this modifier is using.
	/// </summary>
	public ModifierMagnitude Magnitude { get; } = magnitude;

	/// <summary>
	/// Gets which channel this modifier is being applied to.
	/// </summary>
	public int Channel { get; } = channel;
}
