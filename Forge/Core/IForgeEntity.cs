// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Interface for implementing entities that can be used by the Forge Gameplay Framework.
/// </summary>
public interface IForgeEntity
{
	/// <summary>
	/// Gets the effects manager for this entity.
	/// </summary>
	public GameplayEffectsManager GameplayEffectsManager { get; }

	/// <summary>
	/// Gets the container with all the attributes from this entity.
	/// </summary>
	public Attributes Attributes { get; }

	/// <summary>
	/// Gets the gameplay tags of this entity.
	/// </summary>
	public GameplayTagContainer GameplayTags { get; }
}
