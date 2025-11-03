// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Interface for implementing entities that can be used by the Forge Gameplay System.
/// </summary>
public interface IForgeEntity
{
	/// <summary>
	/// Gets the container with all the attributes from this entity.
	/// </summary>
	EntityAttributes Attributes { get; }

	/// <summary>
	/// Gets the tags manager of this entity.
	/// </summary>
	EntityTags Tags { get; }

	/// <summary>
	/// Gets the effects manager for this entity.
	/// </summary>
	EffectsManager EffectsManager { get; }

	/// <summary>
	/// Gets the abilities manager for this entity.
	/// </summary>
	EntityAbilities Abilities { get; }
}
