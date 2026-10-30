// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// An entity's simulation at one moment, taken by <see cref="EntitySimulation.CaptureSnapshot"/> to be put back by
/// <see cref="EntitySimulation.RestoreSnapshot"/>.
/// </summary>
/// <remarks>
/// <para>It holds the entity's attributes, tags, active effects, granted abilities with their running instances and
/// graphs, shared variables, input, random source, body and the state its <see cref="IForgeEntity.StateHook"/> keeps
/// outside Forge. Active effects, abilities and instances are kept as the
/// objects they are, with copies of their state, so a restore brings back the very handles that point at them.</para>
/// <para>A snapshot never changes, so it can be restored any number of times.</para>
/// </remarks>
public sealed class EntitySnapshot
{
	internal IForgeEntity Entity { get; }

	internal AttributeSet[] AttributeSets { get; }

	internal KeyValuePair<EntityAttribute, AttributeSnapshot>[] Attributes { get; }

	internal EntityTagsSnapshot Tags { get; }

	internal EntityTagsSnapshot BlockedAbilityTags { get; }

	internal KeyValuePair<ActiveEffect, ActiveEffectSnapshot>[] Effects { get; }

	internal EntityAbilitiesSnapshot Abilities { get; }

	internal Variables SharedVariables { get; } = new();

	internal EntityInputSnapshot? Input { get; }

	internal DeterministicRandom.State? Random { get; }

	internal BodyState? Body { get; }

	internal Variant128? ExternalState { get; }

	internal EntitySnapshot(IForgeEntity entity)
	{
		Entity = entity;
		AttributeSets = [.. entity.Attributes.AttributeSets];
		Attributes = entity.Attributes.CaptureSnapshot();
		Tags = entity.Tags.CaptureSnapshot();
		BlockedAbilityTags = entity.Abilities.BlockedAbilityTags.CaptureSnapshot();
		Effects = entity.EffectsManager.CaptureSnapshot();
		Abilities = entity.Abilities.CaptureSnapshot();
		entity.SharedVariables.CopyTo(SharedVariables);
		Input = entity.Input?.CaptureSnapshot();
		Random = entity.Random?.CaptureSnapshot();
		Body = entity.Mover?.CaptureState();
		ExternalState = entity.StateHook?.CaptureState();
	}

	// Puts the entity's state back, silently, between the restore's letting go and taking hold again. An effect
	// component that throws doesn't keep the rest from coming back, and the first exception is handed back.
	internal ExceptionDispatchInfo? RestoreState()
	{
		Entity.Attributes.RestoreSnapshot(AttributeSets, Attributes);
		Entity.Tags.RestoreSnapshot(Tags);
		Entity.Abilities.BlockedAbilityTags.RestoreSnapshot(BlockedAbilityTags);
		ExceptionDispatchInfo? failure = Entity.EffectsManager.RestoreSnapshot(Effects);
		Entity.Abilities.RestoreSnapshot(Abilities);
		SharedVariables.CopyTo(Entity.SharedVariables);

		if (Input is EntityInputSnapshot input)
		{
			Entity.Input?.RestoreSnapshot(input);
		}

		if (Random is { } random)
		{
			Entity.Random?.RestoreSnapshot(random);
		}

		if (Body is BodyState body)
		{
			Entity.Mover?.RestoreState(body);
		}

		if (ExternalState is Variant128 externalState)
		{
			Entity.StateHook?.RestoreState(externalState);
		}

		return failure;
	}
}
