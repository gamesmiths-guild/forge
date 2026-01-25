// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects;

internal readonly record struct AttributeSnapshotKey(
	StringKey Attribute,
	AttributeCaptureSource Source,
	AttributeCalculationType CalculationType,
	int FinalChannel);
