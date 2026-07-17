// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Shared level and ownership resolution helpers used by <see cref="EffectFromDataResolver"/> and
/// <see cref="EffectArrayFromDataResolver"/>.
/// </summary>
internal static class EffectResolverUtilities
{
	public static void ApplySetByCallerMagnitudes(
		GraphContext graphContext,
		Effect effect,
		IReadOnlyList<KeyValuePair<Tag, IPropertyResolver>>? setByCallerMagnitudes)
	{
		if (setByCallerMagnitudes is null)
		{
			return;
		}

		for (int i = 0; i < setByCallerMagnitudes.Count; i++)
		{
			IPropertyResolver magnitudeResolver = setByCallerMagnitudes[i].Value;
			float magnitude = MathTypeUtils.ResolveAsFloat(
				magnitudeResolver.ValueType,
				magnitudeResolver.Resolve(graphContext));

			effect.SetSetByCallerMagnitude(setByCallerMagnitudes[i].Key, magnitude);
		}
	}

	public static int ResolveLevel(GraphContext graphContext, IPropertyResolver? levelResolver)
	{
		if (levelResolver is not null)
		{
			return levelResolver.Resolve(graphContext).Get<int>();
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			? abilityContext.Level
			: 1;
	}

	public static EffectOwnership ResolveOwnership(
		GraphContext graphContext,
		IObjectResolver<EffectOwnership>? ownershipResolver)
	{
		if (ownershipResolver is not null)
		{
			return ownershipResolver.Resolve(graphContext);
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			? abilityContext.Ownership
			: new EffectOwnership(null, null);
	}
}
