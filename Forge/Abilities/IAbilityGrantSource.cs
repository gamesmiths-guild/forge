// Copyright © Gamesmiths Guild.
namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Interface for sources that can grant abilities.
/// </summary>
internal interface IAbilityGrantSource
{
	/// <summary>
	/// Gets a value indicating whether the ability grant source is currently inhibited.
	/// </summary>
	bool IsInhibited { get; }

	/// <summary>
	/// Gets the policy for removing the ability when the grant source is removed.
	/// </summary>
	AbilityDeactivationPolicy RemovalPolicy { get; }

	/// <summary>
	/// Gets the policy for inhibiting the ability when the grant source is inhibited.
	/// </summary>
	AbilityDeactivationPolicy InhibitionPolicy { get; }
}
