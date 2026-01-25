// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// Channel configuration for Attributes.
/// </summary>
/// <remarks>
/// Attributes have channels for calculating its modifiers. Multiple channels can be used to calculate in sequence,
/// granting it's possible to have various kinds of formulas combining flat and percentage modifiers.
/// </remarks>
internal struct ChannelData
{
	/// <summary>
	/// Gets or sets an override value at this channel.
	/// </summary>
	/// <remarks>
	/// Can be null in case there are no overrides.
	/// </remarks>
	public int? Override { get; set; }

	/// <summary>
	/// Gets or sets a flat value modifier for this channel.
	/// </summary>
	public int FlatModifier { get; set; }

	/// <summary>
	/// Gets or sets a percent modifier for this channel.
	/// </summary>
	public double PercentModifier { get; set; }
}
