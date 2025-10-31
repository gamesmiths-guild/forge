// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Key used for identifying captured attribute snapshots.
/// </summary>
/// <param name="Attribute">The attribute being captured.</param>
/// <param name="Source">The source from which the attribute is being captured.</param>
/// <param name="CalculationType">The type of calculation used for capturing the attribute.</param>
/// <param name="FinalChannel">The final channel to which the attribute is being captured.</param>
public readonly record struct AttributeSnapshotKey(
	StringKey Attribute,
	AttributeCaptureSource Source,
	AttributeCalculationType CalculationType,
	int FinalChannel);
