// Copyright © Gamesmiths Guild.

using System.Numerics;
using System.Runtime.ExceptionServices;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;

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

	// Writes the state a checksum covers, value by value. Node contexts and the state of effect components are covered
	// through what they change, since their shape is their node's or component's own. The body is written at the
	// network's precision, a thousandth, so float noise below it isn't a difference.
	internal void Write(IStateWriter writer)
	{
		writer.Open("Attributes");

		foreach (KeyValuePair<EntityAttribute, AttributeSnapshot> attribute in Attributes)
		{
			AttributeSnapshot value = attribute.Value;
			writer.Open(attribute.Key.Key.ToString());
			writer.Write("Base", value.BaseValue);
			writer.Write("Current", value.CurrentValue);
			writer.Write("Min", value.Min);
			writer.Write("Max", value.Max);
			writer.Write("Modifier", value.Modifier);
			writer.Write("Overflow", value.Overflow);
			writer.Close();
		}

		writer.Close();
		WriteTags(writer, "Tags", Tags);
		WriteTags(writer, "Blocked ability tags", BlockedAbilityTags);

		for (int i = 0; i < Effects.Length; i++)
		{
			ActiveEffectSnapshot effect = Effects[i].Value;
			EffectOwnership ownership = effect.EffectEvaluatedData.Effect.Ownership;
			writer.Open("Effects", i);
			writer.Write("Effect", Effects[i].Key.EffectData.Name);
			writer.Write("Level", effect.EffectLevel);
			writer.Write("Stacks", effect.StackCount);
			writer.Write("Remaining duration", effect.RemainingDuration);
			writer.Write("Next periodic tick", effect.NextPeriodicTick);
			writer.Write("Internal time", effect.InternalTime);
			writer.Write("Executions", effect.ExecutionCount);
			writer.Write("Inhibited", effect.IsInhibited ? 1 : 0);
			writer.WriteEntity("Owner", ownership.Owner);
			writer.WriteEntity("Source", ownership.Source);
			writer.OpenSet("Set by caller");

			foreach (KeyValuePair<Tag, float> value in effect.EffectDataTags)
			{
				writer.Write(value.Key.ToString(), value.Value);
			}

			writer.Close();
			writer.Close();
		}

		for (int i = 0; i < Abilities.Abilities.Length; i++)
		{
			GrantedAbilitySnapshot ability = Abilities.Abilities[i];
			writer.Open("Abilities", i);
			writer.Write("Ability", ability.Ability.AbilityData.Name);
			writer.Write("Level", ability.State.Level);
			writer.Write("Inhibited", ability.State.IsInhibited ? 1 : 0);
			writer.Write("Instances", ability.State.ActiveInstances.Length);

			for (int j = 0; j < ability.State.Behaviors.Length; j++)
			{
				if (ability.State.Behaviors[j].State is GraphSnapshot graph)
				{
					writer.Open("Graphs", j);
					graph.Write(writer);
					writer.Close();
				}
			}

			writer.Close();
		}

		SharedVariables.Write(writer, "Shared variables");

		if (Input is EntityInputSnapshot input)
		{
			writer.Open("Input");
			input.Current.Write(writer);
			writer.Write("Frame number", unchecked((long)input.FrameNumber));
			writer.Close();
		}

		if (Random is DeterministicRandom.State random)
		{
			writer.Open("Random");
			writer.Write("S0", unchecked((long)random.S0));
			writer.Write("S1", unchecked((long)random.S1));
			writer.Write("S2", unchecked((long)random.S2));
			writer.Write("S3", unchecked((long)random.S3));
			writer.Close();
		}

		if (Body is BodyState body)
		{
			writer.Open("Body");
			WriteQuantized(writer, "Position", body.Position);
			writer.Open("Rotation");
			writer.Write("x", Quantized(body.Rotation.X));
			writer.Write("y", Quantized(body.Rotation.Y));
			writer.Write("z", Quantized(body.Rotation.Z));
			writer.Write("w", Quantized(body.Rotation.W));
			writer.Close();
			WriteQuantized(writer, "Velocity", body.Velocity);
			writer.Write("Grounded", body.Grounded ? 1 : 0);
			writer.Close();
		}

		if (ExternalState is Variant128 externalState)
		{
			writer.Write("Outside state", externalState);
		}
	}

	// Puts the entity's state back, silently, between the restore's letting go and taking hold again. An effect
	// component, a mover or a state hook that throws doesn't keep the rest from coming back, and the first exception is
	// handed back.
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
			try
			{
				Entity.Mover?.RestoreState(body);
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		if (ExternalState is Variant128 externalState)
		{
			try
			{
				Entity.StateHook?.RestoreState(externalState);
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		return failure;
	}

	private static void WriteTags(IStateWriter writer, string name, EntityTagsSnapshot tags)
	{
		writer.Open(name);
		writer.OpenSet("Base");

		foreach (Tag tag in tags.BaseTags)
		{
			writer.Write(tag.ToString(), 1);
		}

		writer.Close();
		writer.OpenSet("From effects");

		foreach (KeyValuePair<Tag, int> tag in tags.ModifierTagCounts)
		{
			writer.Write(tag.Key.ToString(), tag.Value);
		}

		writer.Close();
		writer.Close();
	}

	private static void WriteQuantized(IStateWriter writer, string name, Vector3 value)
	{
		writer.Open(name);
		writer.Write("x", Quantized(value.X));
		writer.Write("y", Quantized(value.Y));
		writer.Write("z", Quantized(value.Z));
		writer.Close();
	}

	// Adding zero turns the negative zero a small negative value rounds to into positive zero, which has other bits.
	private static double Quantized(float value)
	{
		return (Math.Round(value * 1000.0, MidpointRounding.AwayFromZero) / 1000.0) + 0.0;
	}
}
