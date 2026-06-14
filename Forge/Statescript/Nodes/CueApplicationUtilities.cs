// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Shared resolution and firing logic for the cue nodes. Resolves the cue-tag and target inputs as single values or
/// arrays (the cue-tag x target matrix) and routes each pair to the target entity's <see cref="CuesManager"/>.
/// </summary>
internal static class CueApplicationUtilities
{
	private enum CueOperation
	{
		Execute = 0,
		Apply = 1,
		Update = 2,
	}

	public static void ExecuteCues(
		GraphContext graphContext,
		StringKey cueTagInputName,
		StringKey targetInputName,
		StringKey magnitudeInputName,
		StringKey normalizedMagnitudeInputName,
		StringKey sourceInputName,
		StringKey customParametersInputName)
	{
		FireCues(
			graphContext,
			cueTagInputName,
			targetInputName,
			magnitudeInputName,
			normalizedMagnitudeInputName,
			sourceInputName,
			customParametersInputName,
			CueOperation.Execute,
			appliedCues: null);
	}

	public static void ApplyCues(
		GraphContext graphContext,
		StringKey cueTagInputName,
		StringKey targetInputName,
		StringKey magnitudeInputName,
		StringKey normalizedMagnitudeInputName,
		StringKey sourceInputName,
		StringKey customParametersInputName,
		ICollection<AppliedCue>? appliedCues = null)
	{
		FireCues(
			graphContext,
			cueTagInputName,
			targetInputName,
			magnitudeInputName,
			normalizedMagnitudeInputName,
			sourceInputName,
			customParametersInputName,
			CueOperation.Apply,
			appliedCues);
	}

	public static void UpdateCues(
		GraphContext graphContext,
		StringKey cueTagInputName,
		StringKey targetInputName,
		StringKey magnitudeInputName,
		StringKey normalizedMagnitudeInputName,
		StringKey sourceInputName,
		StringKey customParametersInputName)
	{
		FireCues(
			graphContext,
			cueTagInputName,
			targetInputName,
			magnitudeInputName,
			normalizedMagnitudeInputName,
			sourceInputName,
			customParametersInputName,
			CueOperation.Update,
			appliedCues: null);
	}

	public static void RemoveCues(IReadOnlyList<AppliedCue> appliedCues, bool interrupted)
	{
		for (int i = 0; i < appliedCues.Count; i++)
		{
			AppliedCue applied = appliedCues[i];
			applied.Target.CuesManager.RemoveCue(applied.Tag, applied.Target, interrupted);
		}
	}

	private static void FireCues(
		GraphContext graphContext,
		StringKey cueTagInputName,
		StringKey targetInputName,
		StringKey magnitudeInputName,
		StringKey normalizedMagnitudeInputName,
		StringKey sourceInputName,
		StringKey customParametersInputName,
		CueOperation operation,
		ICollection<AppliedCue>? appliedCues)
	{
		if (!ResolveCueTags(graphContext, cueTagInputName, out IReadOnlyList<Tag> cueTags)
			|| !ResolveTargets(graphContext, targetInputName, out IReadOnlyList<IForgeEntity> targets))
		{
			return;
		}

		// Resolved once per node execution and shared across the whole cue-tag x target matrix.
		CueParameters? parameters = BuildParameters(
			graphContext,
			magnitudeInputName,
			normalizedMagnitudeInputName,
			sourceInputName,
			customParametersInputName);

		for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
		{
			IForgeEntity target = targets[targetIndex];

			for (int tagIndex = 0; tagIndex < cueTags.Count; tagIndex++)
			{
				Tag cueTag = cueTags[tagIndex];

				switch (operation)
				{
					case CueOperation.Execute:
						target.CuesManager.ExecuteCue(cueTag, target, parameters);
						break;

					case CueOperation.Apply:
						target.CuesManager.ApplyCue(cueTag, target, parameters);
						appliedCues?.Add(new AppliedCue(cueTag, target));
						break;

					case CueOperation.Update:
						target.CuesManager.UpdateCue(cueTag, target, parameters);
						break;
				}
			}
		}
	}

	private static CueParameters? BuildParameters(
		GraphContext graphContext,
		StringKey magnitudeInputName,
		StringKey normalizedMagnitudeInputName,
		StringKey sourceInputName,
		StringKey customParametersInputName)
	{
		bool hasMagnitude = magnitudeInputName != StringKey.Empty;
		bool hasNormalizedMagnitude = normalizedMagnitudeInputName != StringKey.Empty;
		bool hasSource = sourceInputName != StringKey.Empty;
		bool hasCustomParameters = customParametersInputName != StringKey.Empty;

		if (!hasMagnitude && !hasNormalizedMagnitude && !hasSource && !hasCustomParameters)
		{
			return null;
		}

		int magnitude = 0;
		if (hasMagnitude)
		{
			graphContext.TryResolve(magnitudeInputName, out magnitude);
		}

		float normalizedMagnitude = 0f;
		if (hasNormalizedMagnitude)
		{
			graphContext.TryResolve(normalizedMagnitudeInputName, out normalizedMagnitude);
		}

		IForgeEntity? source = null;
		if (hasSource
			&& graphContext.TryResolveObject(sourceInputName, typeof(IForgeEntity), out object? resolvedSource))
		{
			source = resolvedSource as IForgeEntity;
		}

		Dictionary<StringKey, object>? customParameters = null;
		if (hasCustomParameters
			&& graphContext.TryResolveObject(
				customParametersInputName,
				typeof(Dictionary<StringKey, object>),
				out object? resolvedCustomParameters))
		{
			customParameters = resolvedCustomParameters as Dictionary<StringKey, object>;
		}

		return new CueParameters(magnitude, normalizedMagnitude, source, customParameters);
	}

	private static bool ResolveCueTags(
		GraphContext graphContext,
		StringKey cueTagInputName,
		out IReadOnlyList<Tag> cueTags)
	{
		if (graphContext.TryResolveObjectArray(cueTagInputName, typeof(Tag), out object?[]? resolvedTagArray))
		{
			var resolvedTags = new List<Tag>(resolvedTagArray.Length);

			for (int i = 0; i < resolvedTagArray.Length; i++)
			{
				if (resolvedTagArray[i] is Tag tag)
				{
					resolvedTags.Add(tag);
				}
			}

			cueTags = resolvedTags;
			return resolvedTags.Count > 0;
		}

		if (graphContext.TryResolveObject(cueTagInputName, typeof(Tag), out object? resolvedTag)
			&& resolvedTag is Tag singleTag)
		{
			cueTags = [singleTag];
			return true;
		}

		cueTags = [];
		return false;
	}

	private static bool ResolveTargets(
		GraphContext graphContext,
		StringKey targetInputName,
		out IReadOnlyList<IForgeEntity> targets)
	{
		if (graphContext.TryResolveObjectArray(
			targetInputName,
			typeof(IForgeEntity),
			out object?[]? resolvedTargetArray))
		{
			var resolvedTargets = new List<IForgeEntity>(resolvedTargetArray.Length);

			for (int i = 0; i < resolvedTargetArray.Length; i++)
			{
				if (resolvedTargetArray[i] is IForgeEntity target)
				{
					resolvedTargets.Add(target);
				}
			}

			targets = resolvedTargets;
			return resolvedTargets.Count > 0;
		}

		if (graphContext.TryResolveObject(targetInputName, typeof(IForgeEntity), out object? resolvedTarget)
			&& resolvedTarget is IForgeEntity singleTarget)
		{
			targets = [singleTarget];
			return true;
		}

		targets = [];
		return false;
	}
}
