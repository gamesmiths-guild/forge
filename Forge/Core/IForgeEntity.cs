// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Cues;
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
	/// Gets the cues manager used to trigger cues for this entity. This is typically the game-wide shared manager.
	/// </summary>
	CuesManager CuesManager { get; }

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

	/// <summary>
	/// Gets the random source this entity's simulation draws from, or <see langword="null"/> when the entity provides
	/// none.
	/// </summary>
	/// <remarks>
	/// <see cref="EntitySimulation.BeginTick"/> restarts it at the start of every tick, so the rolls a graph or an
	/// effect component makes depend only on the entity's seed and the tick. Random nodes, random resolvers and
	/// <see cref="Effects.Components.ChanceToApplyEffectComponent"/> prefer it to the provider they were built with;
	/// without one they fall back to that provider.
	/// </remarks>
	DeterministicRandom? Random => null;
}
