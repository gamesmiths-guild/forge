// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Contains information about the cost associated with an ability activation.
/// </summary>
/// <param name="Attribute">The attribute used to pay the cost.</param>
/// <param name="Cost">The amount of the cost.</param>
public record struct CostData(StringKey Attribute, int Cost);
