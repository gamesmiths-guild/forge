// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// A runtime version of a <see cref="Effects.EffectData"/> used to apply the effects with level and ownership
/// information.
/// </summary>
public class Effect
{
	private TagContainer? _owningTags;
	private bool _owningTagsResolved;

	/// <summary>
	/// Event triggered when the level of this effect changes.
	/// </summary>
	public event Action<int>? OnLevelChanged;

	/// <summary>
	/// Event triggered when a <see cref="SetByCallerFloat"/> magnitude changes.
	/// </summary>
	public event Action<Tag, float>? OnSetByCallerFloatChanged;

	/// <summary>
	/// Gets the configuration data for this effect.
	/// </summary>
	public EffectData EffectData { get; }

	/// <summary>
	/// Gets information about the ownership and source of this effect.
	/// </summary>
	public EffectOwnership Ownership { get; }

	/// <summary>
	/// Gets the current level o this effect.
	/// </summary>
	public int Level { get; private set; }

	/// <summary>
	/// Gets the mapping for custom <see cref="SetByCallerFloat"/> magnitudes.
	/// </summary>
	public Dictionary<Tag, float> DataTag { get; } = [];

	internal TagContainer? CachedGrantedTags { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Effect"/> class.
	/// </summary>
	/// <param name="effectData">The configuration data for this effect.</param>
	/// <param name="ownership">The ownership info for this effect.</param>
	/// <param name="level">The initial level for this effect.</param>
	public Effect(EffectData effectData, EffectOwnership ownership, int level = 1)
	{
		EffectData = effectData;
		Ownership = ownership;
		Level = level;

		foreach (IEffectComponent component in EffectData.EffectComponents)
		{
			if (component is ModifierTagsEffectComponent modifierTagsComponent)
			{
				if (CachedGrantedTags is null)
				{
					CachedGrantedTags = new TagContainer(modifierTagsComponent.TagsToAdd);
					continue;
				}

				CachedGrantedTags.AppendTags(modifierTagsComponent.TagsToAdd);
			}
		}
	}

	/// <summary>
	/// Creates an effect that continues another one: same ownership, same level, and the same
	/// <see cref="SetByCallerFloat"/> magnitudes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is how a component spawns a child effect that should read as the parent's doing rather than as an unrelated
	/// application — a debuff riding a damage effect, or the aftermath a buff leaves behind when it ends. Ownership
	/// carries over so the child credits the same source, and the magnitudes carry over so a child keyed on the same
	/// <see cref="Tag"/> resolves to the value the caller set on the parent.
	/// </para>
	/// <para>
	/// The magnitudes are copied, not shared: setting one on the parent afterwards does not reach the child.
	/// </para>
	/// </remarks>
	/// <param name="effectData">The configuration data for the new effect.</param>
	/// <param name="parentEffect">The effect whose context the new one inherits.</param>
	/// <param name="level">The level for the new effect, usually the parent's evaluated level.</param>
	/// <returns>A new effect carrying the parent's context.</returns>
	public static Effect CreateLinkedEffect(EffectData effectData, Effect parentEffect, int level)
	{
		var linkedEffect = new Effect(effectData, parentEffect.Ownership, level);

		foreach (KeyValuePair<Tag, float> setByCaller in parentEffect.DataTag)
		{
			linkedEffect.DataTag[setByCaller.Key] = setByCaller.Value;
		}

		return linkedEffect;
	}

	/// <summary>
	/// Level up this effect by exactly one level.
	/// </summary>
	public void LevelUp()
	{
		Level++;
		OnLevelChanged?.Invoke(Level);
	}

	/// <summary>
	/// Sets the current level of this effect.
	/// </summary>
	/// <param name="level">The level for the effect to be set at.</param>
	public void SetLevel(int level)
	{
		Level = level;
		OnLevelChanged?.Invoke(Level);
	}

	/// <summary>
	/// Sets a custom magnitude to be used by <see cref="SetByCallerFloat"/> magnitudes.
	/// </summary>
	/// <param name="identifierTag">Tag used to identify a custom value to be used.</param>
	/// <param name="magnitude">The magnitude to be set for the given tag.</param>
	public void SetSetByCallerMagnitude(Tag identifierTag, float magnitude)
	{
		if (DataTag.ContainsKey(identifierTag))
		{
			DataTag[identifierTag] = magnitude;
			OnSetByCallerFloatChanged?.Invoke(identifierTag, magnitude);
			return;
		}

		DataTag.Add(identifierTag, magnitude);
		OnSetByCallerFloatChanged?.Invoke(identifierTag, magnitude);
	}

	internal static void Execute(
		in EffectEvaluatedData effectEvaluatedData,
		IEffectComponent[]? componentInstances)
	{
		CustomExecution[] customExecutions = effectEvaluatedData.Effect.EffectData.CustomExecutions;

		if (customExecutions.Length > 0)
		{
			var allModifiers = new List<ModifierEvaluatedData>(effectEvaluatedData.ModifierCount);

			for (int i = 0; i < effectEvaluatedData.ModifierCount; i++)
			{
				allModifiers.Add(effectEvaluatedData.ModifiersEvaluatedData[i]);
			}

			foreach (CustomExecution execution in customExecutions)
			{
				if (CustomExecution.ExecutionHasInvalidAttributeCaptures(
					execution,
					effectEvaluatedData.Effect,
					effectEvaluatedData.Target))
				{
					continue;
				}

				allModifiers.AddRange(execution.EvaluateExecution(
					effectEvaluatedData.Effect,
					effectEvaluatedData.Target,
					effectEvaluatedData));
			}

			effectEvaluatedData.RefreshCustomCueParameters();

			foreach (ModifierEvaluatedData modifier in allModifiers)
			{
				switch (modifier.ModifierOperation)
				{
					case ModifierOperation.FlatBonus:
						modifier.Attribute.ExecuteFlatModifier((int)modifier.Magnitude);
						break;

					case ModifierOperation.PercentBonus:
						modifier.Attribute.ExecutePercentModifier(modifier.Magnitude);
						break;

					case ModifierOperation.Override:
						modifier.Attribute.ExecuteOverride((int)modifier.Magnitude);
						break;
				}
			}
		}
		else
		{
			foreach (ModifierEvaluatedData modifier in effectEvaluatedData.ModifiersEvaluatedData)
			{
				switch (modifier.ModifierOperation)
				{
					case ModifierOperation.FlatBonus:
						modifier.Attribute.ExecuteFlatModifier((int)modifier.Magnitude);
						break;

					case ModifierOperation.PercentBonus:
						modifier.Attribute.ExecutePercentModifier(modifier.Magnitude);
						break;

					case ModifierOperation.Override:
						modifier.Attribute.ExecuteOverride((int)modifier.Magnitude);
						break;
				}
			}
		}

		effectEvaluatedData.Target.EffectsManager.OnEffectExecuted_InternalCall(
			effectEvaluatedData,
			componentInstances);

		effectEvaluatedData.Target.Attributes.ApplyPendingValueChanges();
	}

	/// <summary>
	/// Gets the union of this effect's own tags and the tags it grants to its target.
	/// </summary>
	/// <remarks>
	/// Both inputs are fixed at construction, so the union is built once on first use and cached. Query filtering calls
	/// this once per effect per pass, and the union is the only case that has to allocate.
	/// </remarks>
	/// <returns>A container with both sets of tags, or <see langword="null"/> when the effect has neither.</returns>
	internal TagContainer? GetOwningTags()
	{
		if (_owningTagsResolved)
		{
			return _owningTags;
		}

		TagContainer? effectTags = EffectData.EffectTags;

		if (effectTags is null)
		{
			_owningTags = CachedGrantedTags;
		}
		else if (CachedGrantedTags is null)
		{
			_owningTags = effectTags;
		}
		else
		{
			var owningTags = new TagContainer(effectTags);
			owningTags.AppendTags(CachedGrantedTags);
			_owningTags = owningTags;
		}

		_owningTagsResolved = true;
		return _owningTags;
	}

	/// <summary>
	/// Evaluates a query against one of this effect's tag containers.
	/// </summary>
	/// <remarks>
	/// An effect that carries no container of its own is still evaluated, as an empty one, so that negative queries
	/// behave the way they read. The tags manager only supplies tag indexing, so any reachable one gives the same
	/// answer; when none is reachable the effect has no tag context at all and never matches.
	/// </remarks>
	/// <param name="query">The query to evaluate.</param>
	/// <param name="container">The container to evaluate against, or <see langword="null"/> when the effect carries
	/// none.</param>
	/// <returns><see langword="true"/> if the query matches; <see langword="false"/> otherwise.</returns>
	internal bool MatchesTagQuery(TagQuery query, TagContainer? container)
	{
		if (container is not null)
		{
			return query.Matches(container);
		}

		TagsManager? tagsManager = ResolveTagsManager();

		return tagsManager is not null && query.Matches(new TagContainer(tagsManager));
	}

	internal TagsManager? ResolveTagsManager()
	{
		return EffectData.EffectTags?.TagsManager
			?? CachedGrantedTags?.TagsManager
			?? Ownership.Source?.Tags.BaseTags.TagsManager
			?? Ownership.Owner?.Tags.BaseTags.TagsManager;
	}

	/// <summary>
	/// Gets the tags of the entity that applied this effect, for requirements evaluated against the source.
	/// </summary>
	/// <remarks>
	/// A missing source falls back to an empty container rather than to no evaluation at all, which keeps the semantics
	/// honest: it cannot satisfy required tags, but it trivially satisfies ignored ones. Building even an empty
	/// container needs a tags manager, so an effect with no tag context anywhere returns <see langword="null"/> and its
	/// caller decides what a source it cannot see means.
	/// </remarks>
	/// <returns>The source's tags, an empty container when there is no source, or <see langword="null"/> when the
	/// effect has no reachable <see cref="TagsManager"/>.</returns>
	internal TagContainer? ResolveSourceTags()
	{
		TagContainer? sourceTags = Ownership.Source?.Tags.AllTags;

		if (sourceTags is not null)
		{
			return sourceTags;
		}

		TagsManager? tagsManager = ResolveTagsManager();

		return tagsManager is null ? null : new TagContainer(tagsManager);
	}

	internal bool CanApply(IForgeEntity target)
	{
		foreach (IEffectComponent component in EffectData.EffectComponents)
		{
			Effect effect = this;
			if (!component.CanApplyEffect(in target, in effect))
			{
				return false;
			}
		}

		return true;
	}
}
