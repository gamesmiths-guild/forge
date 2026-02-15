// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;

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

	/// <summary>
	/// Gets the event bus for this entity.
	/// </summary>
	EventManager Events { get; }

	/// <summary>
	/// Gets the shared variables for this entity. Shared variables are accessible by all graph instances running on
	/// this entity, providing a communication channel between abilities and scripts besides tags and attributes.
	/// </summary>
	Variables SharedVariables { get; }
}
