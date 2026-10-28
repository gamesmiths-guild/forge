// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Attributes;

// An attribute as an entity snapshot took it: its values, and its modifiers per channel as they were summed, so a
// restore adds nothing up again.
internal sealed record AttributeSnapshot(
	int BaseValue,
	int CurrentValue,
	int Min,
	int Max,
	int Modifier,
	int Overflow,
	int PendingValueChange,
	ChannelData[] Channels,
	AttributeOverride[] Overrides);
