// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Interface for implementing entities that can be used by the Forge Gameplay Framework.
/// </summary>
public interface IForgeEntity
{
	/// <summary>
	/// Gets the container with all the attributes from this entity.
	/// </summary>
	Attributes Attributes { get; }

	/// <summary>
	/// Gets the tags manager of this entity.
	/// </summary>
	GameplayTags GameplayTags { get; }

	/// <summary>
	/// Gets the effects manager for this entity.
	/// </summary>
	GameplayEffectsManager EffectsManager { get; }
}
