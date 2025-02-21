// Copyright © 2025 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.GameplayEffects;
using Attribute = Gamesmiths.Forge.Core.Attribute;

namespace Gamesmiths.Forge.GameplayCues;

internal static class GameplayCueUtils
{
	internal static Dictionary<Attribute, int> InitializeAttributeChanges(
	in GameplayEffectEvaluatedData effectEvaluatedData)
	{
		var attributeChanges = new Dictionary<Attribute, int>();

		foreach (Attribute? attribute in
			effectEvaluatedData.GameplayEffect.EffectData.GameplayCues.Select(x => x.MagnitudeAttribute))
		{
			if (attribute is null)
			{
				continue;
			}

			attributeChanges.TryAdd(attribute, attribute.CurrentValue);
		}

		return attributeChanges;
	}

	internal static void UpdateAttributeChanges(Dictionary<Attribute, int> attributeChanges)
	{
		foreach (Attribute attribute in attributeChanges.Keys)
		{
			attributeChanges[attribute] = attribute.CurrentValue - attributeChanges[attribute];
		}
	}

	internal static void AddCues(
		in GameplayCuesManager cuesManager,
		in GameplayEffectEvaluatedData effectEvaluatedData,
		in Dictionary<Attribute, int> attributeChanges)
	{
		GameplayEffectData effectData = effectEvaluatedData.GameplayEffect.EffectData;

		if (!ShouldTriggerCue(in effectData, in attributeChanges))
		{
			return;
		}

		foreach (GameplayCueData cueData in effectData.GameplayCues)
		{
			var magnitude = CalculateMagnitude(in cueData, in effectEvaluatedData, in attributeChanges);

			cuesManager.AddCue(
				  cueData.CueKey,
				  effectEvaluatedData.Target,
				  new GameplayCueParameters(
					  magnitude,
					  cueData.NormalizedMagnitude(magnitude),
					  effectEvaluatedData.GameplayEffect.Ownership.Source));
		}
	}

	internal static void RemoveCues(
		in GameplayCuesManager cuesManager,
		in GameplayEffectEvaluatedData effectEvaluatedData,
		bool interrupted)
	{
		GameplayEffectData effectData = effectEvaluatedData.GameplayEffect.EffectData;

		foreach (GameplayCueData cueData in effectData.GameplayCues)
		{
			cuesManager.RemoveCue(cueData.CueKey, effectEvaluatedData.Target, interrupted);
		}
	}

	internal static void ExecuteCues(
		in GameplayCuesManager cuesManager,
		in GameplayEffectEvaluatedData effectEvaluatedData,
		in Dictionary<Attribute, int> attributeChanges)
	{
		GameplayEffectData effectData = effectEvaluatedData.GameplayEffect.EffectData;

		if (!ShouldTriggerCue(in effectData, in attributeChanges))
		{
			return;
		}

		foreach (GameplayCueData cueData in effectData.GameplayCues)
		{
			var magnitude = CalculateMagnitude(in cueData, in effectEvaluatedData, in attributeChanges);

			cuesManager.ExecuteCue(
				  cueData.CueKey,
				  effectEvaluatedData.Target,
				  new GameplayCueParameters(
					  magnitude,
					  cueData.NormalizedMagnitude(magnitude),
					  effectEvaluatedData.GameplayEffect.Ownership.Source));
		}
	}

	private static bool ShouldTriggerCue(
		in GameplayEffectData effectData,
		in Dictionary<Attribute, int> attributeChanges)
	{
		return !effectData.RequireModifierSuccessToTriggerCue || attributeChanges.Values.Any(x => x != 0);
	}

	private static int CalculateMagnitude(
		in GameplayCueData cueData,
		in GameplayEffectEvaluatedData effectEvaluatedData,
		in Dictionary<Attribute, int> attributeChanges)
	{
		var magnitude = effectEvaluatedData.Level;

		if (cueData.MagnitudeAttribute is not null)
		{
			Debug.Assert(
				attributeChanges.ContainsKey(cueData.MagnitudeAttribute),
				"attributeChanges should always contains a configured MagnitudeAttribute.");

			magnitude = attributeChanges[cueData.MagnitudeAttribute];
		}

		return magnitude;
	}
}
